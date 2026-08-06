using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIo
{
    internal const int InputReadBatchLineCount = AwkRuntimeLimits.DefaultInputReadBatchLineCount;
    internal const int DefaultMaxOutputFiles = AwkRuntimeLimits.DefaultMaxOutputFiles;

    private readonly AwkIoDescriptorTable _descriptors = new();
    private readonly AwkIoHostAdapter _host;
    private readonly AwkPipeRuntime _pipes;
    private readonly AwkSpecialPathPolicy _specialPaths;
    private readonly AwkRuntimeCounters _counters;
    private readonly int _maxOutputFiles;
    private AsyncLineReader? _standardInputReader;
    private MainInputReaderBinding _mainInputReader = MainInputReaderBinding.Unbound;

    public AwkIo(
        IAwkHost host,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr,
        CancellationToken cancellationToken)
        : this(
            host,
            currentDirectory,
            stdIn,
            stdOut,
            stdErr,
            cancellationToken,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault())
    {
    }

    internal AwkIo(
        IAwkHost host,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr,
        CancellationToken cancellationToken,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits)
    {
        _host = new AwkIoHostAdapter(
            host,
            currentDirectory,
            stdIn,
            stdOut,
            stdErr,
            cancellationToken,
            counters,
            new AwkRuntimeResourceMonitor(limits, counters));
        _maxOutputFiles = limits.MaxOutputFiles;
        _pipes = new AwkPipeRuntime(_host, _maxOutputFiles, () => _descriptors.OutputFileCount);
        _specialPaths = new AwkSpecialPathPolicy(stdIn, stdOut, stdErr);
        _counters = counters;
    }

    internal AwkIo(
        IAwkHost host,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr,
        CancellationToken cancellationToken,
        int maxOutputFiles)
        : this(
            host,
            currentDirectory,
            stdIn,
            stdOut,
            stdErr,
            cancellationToken,
            maxOutputFiles,
            AwkRuntimeCounters.Disabled)
    {
    }

    internal AwkIo(
        IAwkHost host,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr,
        CancellationToken cancellationToken,
        int maxOutputFiles,
        AwkRuntimeCounters counters)
        : this(
            host,
            currentDirectory,
            stdIn,
            stdOut,
            stdErr,
            cancellationToken,
            counters,
            AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(maxOutputFiles))
    {
    }

    public Task WriteStandardOutputAsync(string content)
    {
        return _host.WriteStandardOutputAsync(content);
    }

    public Task WriteStandardOutputAsync(ReadOnlyMemory<byte> content)
    {
        return _host.WriteStandardOutputAsync(content);
    }

    public Task WriteStandardErrorAsync(ReadOnlyMemory<byte> content)
    {
        return _host.WriteStandardErrorAsync(content);
    }

    public async Task WriteAsync(AwkRedirectedOutput output)
    {
        var path = output.RedirectionPath;
        if (output.Kind == AwkOutputRedirectionKind.Pipe)
        {
            await _pipes.WriteOutputAsync(path, output.Content).ConfigureAwait(false);
            return;
        }

        AwkSpecialPathPolicy.ThrowIfUnsupported(path.Text);

        if (_specialPaths.TryResolveOutput(path.Text, out var specialDescriptor))
        {
            await _host.AppendAsync(specialDescriptor, output.Content)
                .ConfigureAwait(false);
            return;
        }

        var targetPath = _host.ResolvePath(path.Text);
        var key = targetPath.Absolute.Path;
        if (!_descriptors.TryGetOutput(key, out var descriptor))
        {
            if (_descriptors.OutputFileCount >= _maxOutputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorOutputFileLimit, AwkDiagnosticId.HostIoFailure);

            var mode = _descriptors.ResolveOutputOpenMode(key, output.Kind);
            var opened = await _host.OpenFileAsync(targetPath.Absolute, mode).ConfigureAwait(false);
            if (!opened.Success)
            {
                throw new AwkRuntimeException(opened.Failed
                    ? AwkMessages.FormatPathError(targetPath.Display, opened.Error.Message)
                    : AwkMessages.FormatPathError(targetPath.Display),
                    AwkDiagnosticId.HostIoFailure);
            }

            descriptor = opened.FileDescriptor;
            _descriptors.AddOutput(key, descriptor);
        }

        await _host.AppendAsync(descriptor, output.Content).ConfigureAwait(false);
    }

    public async Task<AwkGetlineResult> ReadLineAsync(string path)
    {
        var resolved = await ResolveInputReaderAsync(path).ConfigureAwait(false);
        if (resolved is AwkInputReaderResult.Failed failed)
            return AwkGetlineResult.Failure(failed.Error);

        if (resolved is not AwkInputReaderResult.Resolved resolvedReader)
            throw new InvalidOperationException("Unexpected awk input reader result.");

        try
        {
            if (await resolvedReader.Reader.ReachedEof().ConfigureAwait(false))
                return AwkGetlineResult.EndOfFile;
        }
        catch (AwkHostReadException exception)
        {
            return AwkGetlineResult.Failure(
                AwkIoError.FromReadFailure(exception.Descriptor, exception.Error));
        }

        var line = AwkRecordLine.ReadUtf8DefaultNewline(resolvedReader.Reader.ReadLine(), _counters);
        _counters.RecordRecordRead();
        return AwkGetlineResult.Record(line.Record, line.Terminator);
    }

    public async Task<AwkGetlineResult> ReadPipeLineAsync(string command)
    {
        var reader = await _pipes.GetInputReaderAsync(command, InputReadBatchLineCount).ConfigureAwait(false);
        try
        {
            if (await reader.ReachedEof().ConfigureAwait(false))
                return AwkGetlineResult.EndOfFile;
        }
        catch (AwkHostReadException exception)
        {
            return AwkGetlineResult.Failure(
                AwkIoError.FromReadFailure(exception.Descriptor, exception.Error));
        }

        var line = AwkRecordLine.ReadUtf8DefaultNewline(reader.ReadLine(), _counters);
        _counters.RecordRecordRead();
        return AwkGetlineResult.Record(line.Record, line.Terminator);
    }

    public void BindMainInputReader(AsyncLineReader reader) =>
        _mainInputReader = MainInputReaderBinding.Bind(reader);

    public void BindMainInputReader(AsyncLineReader reader, Func<Task> beforeReadAsync) =>
        _mainInputReader = MainInputReaderBinding.Bind(reader, beforeReadAsync);

    public void ClearMainInputReader() =>
        _mainInputReader = MainInputReaderBinding.Unbound;

    public async Task<AwkGetlineResult> ReadMainLineAsync()
    {
        if (_mainInputReader is not MainInputReaderBinding.Bound boundReader)
            return AwkGetlineResult.EndOfFile;

        await boundReader.ExecuteBeforeReadAsync().ConfigureAwait(false);

        try
        {
            if (await boundReader.Reader.ReachedEof().ConfigureAwait(false))
                return AwkGetlineResult.EndOfFile;
        }
        catch (AwkHostReadException exception)
        {
            return AwkGetlineResult.Failure(
                AwkIoError.FromReadFailure(exception.Descriptor, exception.Error));
        }

        var line = AwkRecordLine.ReadUtf8DefaultNewline(boundReader.Reader.ReadLine(), _counters);
        _counters.RecordRecordRead();
        return AwkGetlineResult.Record(line.Record, line.Terminator);
    }

    private async Task<AwkInputReaderResult> ResolveInputReaderAsync(string path)
    {
        AwkSpecialPathPolicy.ThrowIfUnsupported(path);

        if (_specialPaths.TryResolveInput(path, out var specialDescriptor))
        {
            _standardInputReader ??= _host.CreateLineReader(specialDescriptor, InputReadBatchLineCount);
            return AwkInputReaderResult.Success(_standardInputReader);
        }

        var targetPath = _host.ResolvePath(path);
        var key = targetPath.Absolute.Path;
        if (_descriptors.TryGetInput(key, out var existing))
            return AwkInputReaderResult.Success(existing);

        var opened = await _host.OpenFileAsync(targetPath.Absolute, AwkFileOpenMode.Read).ConfigureAwait(false);
        if (!opened.Success)
        {
            var error = opened.Failed
                ? AwkIoError.FromOpenFailure(targetPath.Display, AwkErrno.FromHostMessage(opened.Error.Message))
                : AwkIoError.FromMissingPath(targetPath.Display);
            return AwkInputReaderResult.Failure(error);
        }

        var reader = _host.CreateLineReader(opened.FileDescriptor, InputReadBatchLineCount);
        _descriptors.AddInput(key, opened.FileDescriptor, reader);
        return AwkInputReaderResult.Success(reader);
    }

    public async Task<AwkIoOperationResult> FlushAsync(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            var keys = _descriptors.SnapshotOutputKeys();
            for (var index = 0; index < keys.Length; index++)
            {
                await _descriptors.FlushOutputAsync(_host, keys[index]).ConfigureAwait(false);
            }

            return AwkIoOperationResult.Success;
        }

        if (_pipes.HasOutputPipe(path))
            return AwkIoOperationResult.Success;

        AwkSpecialPathPolicy.ThrowIfUnsupported(path);

        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkIoOperationResult.Success;

        var targetPath = _host.ResolvePath(path);
        var flushed = await _descriptors.FlushOutputAsync(_host, targetPath.Absolute.Path)
            .ConfigureAwait(false);
        return AwkIoOperationResult.FromSuccess(flushed, AwkIoError.RedirectionNeverOpened);
    }

    public async Task CloseAsync()
    {
        await _pipes.CloseAllAsync().ConfigureAwait(false);
        await _descriptors.CloseAllAsync(_host).ConfigureAwait(false);
    }

    public async Task CloseForCleanupAsync()
    {
        await _pipes.CloseAllForCleanupAsync().ConfigureAwait(false);
        await _descriptors.CloseAllForCleanupAsync(_host).ConfigureAwait(false);
    }

    public async Task<AwkIoOperationResult> CloseAsync(string path)
    {
        var pipeClosed = await _pipes.CloseAnyAsync(path).ConfigureAwait(false);
        if (pipeClosed.ClosedPipe)
            return AwkIoOperationResult.SuccessWithStatusCode(pipeClosed.ExitCode);

        AwkSpecialPathPolicy.ThrowIfUnsupported(path);

        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkIoOperationResult.Success;

        var targetPath = _host.ResolvePath(path);
        var closed = await _descriptors.CloseAnyAsync(_host, targetPath.Absolute.Path)
            .ConfigureAwait(false);
        return AwkIoOperationResult.FromSuccess(closed, AwkIoError.RedirectionNeverOpened);
    }

    public async Task<AwkIoOperationResult> CloseAsync(string path, AwkCloseDirection direction)
    {
        if (direction == AwkCloseDirection.To)
        {
            var pipeClosed = await _pipes.CloseOutputAsync(path).ConfigureAwait(false);
            if (pipeClosed.ClosedPipe)
                return AwkIoOperationResult.SuccessWithStatusCode(pipeClosed.ExitCode);

            if (_pipes.HasInputPipe(path))
                return AwkIoOperationResult.Failure(AwkIoError.RedirectionNeverOpened);
        }
        else if (direction == AwkCloseDirection.From)
        {
            var pipeClosed = await _pipes.CloseInputAsync(path).ConfigureAwait(false);
            if (pipeClosed.ClosedPipe)
                return AwkIoOperationResult.SuccessWithStatusCode(pipeClosed.ExitCode);

            if (_pipes.HasOutputPipe(path))
                return AwkIoOperationResult.Failure(AwkIoError.RedirectionNeverOpened);
        }
        else if (direction != AwkCloseDirection.From)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        AwkSpecialPathPolicy.ThrowIfUnsupported(path);

        if (_specialPaths.TryResolveOutput(path, out _))
        {
            return direction == AwkCloseDirection.To
                ? AwkIoOperationResult.Success
                : AwkIoOperationResult.Failure(AwkIoError.RedirectionNeverOpened);
        }

        var targetPath = _host.ResolvePath(path);
        var key = targetPath.Absolute.Path;
        if (direction == AwkCloseDirection.To)
        {
            var closed = await _descriptors.CloseOutputAsync(_host, key).ConfigureAwait(false);
            return AwkIoOperationResult.FromSuccess(closed, AwkIoError.RedirectionNeverOpened);
        }

        if (direction == AwkCloseDirection.From)
        {
            var closed = await _descriptors.CloseInputAsync(_host, key).ConfigureAwait(false);
            return AwkIoOperationResult.FromSuccess(closed, AwkIoError.RedirectionNeverOpened);
        }

        throw new ArgumentOutOfRangeException(nameof(direction));
    }

    private abstract class AwkInputReaderResult
    {
        public static Resolved Success(AsyncLineReader reader) => new(reader);

        public static Failed Failure(AwkIoError error) => new(error);

        public sealed class Resolved(AsyncLineReader reader) : AwkInputReaderResult
        {
            public AsyncLineReader Reader { get; } = reader;
        }

        public sealed class Failed(AwkIoError error) : AwkInputReaderResult
        {
            public AwkIoError Error { get; } = error;
        }
    }

    private abstract class MainInputReaderBinding
    {
        public static MainInputReaderBinding Unbound { get; } = new Empty();

        public static Bound Bind(AsyncLineReader reader) => new(reader, MainInputReadHook.None);

        public static Bound Bind(AsyncLineReader reader, Func<Task> beforeReadAsync) =>
            new(reader, MainInputReadHook.Once(beforeReadAsync));

        public sealed class Empty : MainInputReaderBinding
        {
        }

        public sealed class Bound(
            AsyncLineReader reader,
            MainInputReadHook beforeRead) : MainInputReaderBinding
        {
            private MainInputReadHook _beforeRead = beforeRead;

            public AsyncLineReader Reader { get; } = reader;

            public async Task ExecuteBeforeReadAsync()
            {
                var beforeRead = _beforeRead;
                _beforeRead = MainInputReadHook.None;
                await beforeRead.ExecuteAsync().ConfigureAwait(false);
            }
        }
    }

    private abstract class MainInputReadHook
    {
        public static MainInputReadHook None { get; } = new Empty();

        public static MainInputReadHook Once(Func<Task> callback) => new Callback(callback);

        public abstract Task ExecuteAsync();

        public sealed class Empty : MainInputReadHook
        {
            public override Task ExecuteAsync() => Task.CompletedTask;
        }

        public sealed class Callback(Func<Task> callback) : MainInputReadHook
        {
            public override Task ExecuteAsync() => callback();
        }
    }
}
