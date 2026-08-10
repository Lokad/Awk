using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIo
{
    internal const int InputReadBatchLineCount = AwkRuntimeLimits.DefaultInputReadBatchLineCount;

    private readonly AwkIoDescriptorTable _descriptors = new();
    private readonly AwkIoHostAdapter _host;
    private readonly AwkPipeRuntime _pipes;
    private readonly AwkSpecialPathPolicy _specialPaths;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkRuntimeState _state;
    private readonly AwkRecordDescriptorReader _descriptorReader;
    private readonly int _maxOutputFiles;
    private AwkRecordCursor? _standardInputReader;
    private MainInputReaderBinding _mainInputReader = MainInputReaderBinding.Unbound;

    internal AwkIo(
        IAwkHost host,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr,
        CancellationToken cancellationToken,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits,
        AwkRuntimeState state,
        Func<IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment)
    {
        _host = new AwkIoHostAdapter(
            host,
            currentDirectory,
            stdIn,
            stdOut,
            stdErr,
            cancellationToken,
            snapshotEnvironment,
            counters,
            state.Resources);
        _maxOutputFiles = limits.MaxOutputFiles;
        _state = state;
        _descriptorReader = new AwkRecordDescriptorReader(
            host,
            cancellationToken,
            counters,
            state.Resources);
        _pipes = new AwkPipeRuntime(
            _host,
            _maxOutputFiles,
            () => _descriptors.OutputFileCount,
            CreateRecordCursor);
        _specialPaths = new AwkSpecialPathPolicy(stdIn, stdOut, stdErr);
        _counters = counters;
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
        var absolutePath = targetPath.Absolute;
        if (!_descriptors.TryGetOutput(absolutePath, out var descriptor))
        {
            if (_descriptors.OutputFileCount >= _maxOutputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorOutputFileLimit, AwkDiagnosticId.HostIoFailure);

            var mode = _descriptors.ResolveOutputOpenMode(absolutePath, output.Kind);
            var opened = await _host.OpenFileAsync(absolutePath, mode).ConfigureAwait(false);
            if (!opened.Success)
            {
                throw new AwkRuntimeException(opened.Failed
                    ? AwkMessages.FormatPathError(targetPath.Display, opened.Error.Message)
                    : AwkMessages.FormatPathError(targetPath.Display),
                    AwkDiagnosticId.HostIoFailure);
            }

            descriptor = opened.FileDescriptor;
            _descriptors.AddOutput(absolutePath, descriptor);
        }

        await _host.AppendAsync(descriptor, output.Content).ConfigureAwait(false);
    }

    public async Task<AwkGetlineResult> ReadRedirectedRecordAsync(string path)
    {
        async Task<AwkInputReaderResult> ResolveInputReaderAsync()
        {
            AwkSpecialPathPolicy.ThrowIfUnsupported(path);

            if (_specialPaths.TryResolveInput(path, out var specialDescriptor))
            {
                _standardInputReader ??= CreateRecordCursor(specialDescriptor);
                return AwkInputReaderResult.Success(_standardInputReader);
            }

            var targetPath = _host.ResolvePath(path);
            var absolutePath = targetPath.Absolute;
            if (_descriptors.TryGetInput(absolutePath, out var existing))
                return AwkInputReaderResult.Success(existing);

            var opened = await _host.OpenFileAsync(targetPath.Absolute, AwkFileOpenMode.Read).ConfigureAwait(false);
            if (!opened.Success)
            {
                var error = opened.Failed
                    ? AwkIoError.FromOpenFailure(targetPath.Display, AwkErrno.FromHostMessage(opened.Error.Message))
                    : AwkIoError.FromMissingPath(targetPath.Display);
                return AwkInputReaderResult.Failure(error);
            }

            var reader = CreateRecordCursor(opened.FileDescriptor);
            _descriptors.AddInput(absolutePath, opened.FileDescriptor, reader);
            return AwkInputReaderResult.Success(reader);
        }

        var resolved = await ResolveInputReaderAsync().ConfigureAwait(false);
        if (resolved is AwkInputReaderResult.Failed failed)
            return AwkGetlineResult.Failure(failed.Error);

        if (resolved is not AwkInputReaderResult.Resolved resolvedReader)
            throw new InvalidOperationException("Unexpected awk input reader result.");

        return await ReadGetlineRecordAsync(resolvedReader.Reader).ConfigureAwait(false);
    }

    public async Task<AwkGetlineResult> ReadPipeRecordAsync(string command)
    {
        var reader = await _pipes.GetInputReaderAsync(command).ConfigureAwait(false);
        return await ReadGetlineRecordAsync(reader).ConfigureAwait(false);
    }

    public void BindMainInputReader(IAwkInputRecordReader reader) =>
        _mainInputReader = MainInputReaderBinding.Bind(reader);

    public void ClearMainInputReader() =>
        _mainInputReader = MainInputReaderBinding.Unbound;

    public Task<AwkGetlineResult> ReadMainRecordAsync()
    {
        if (_mainInputReader is not MainInputReaderBinding.Bound boundReader)
            return Task.FromResult(AwkGetlineResult.EndOfFile);

        return ReadGetlineRecordAsync(boundReader.Reader);
    }

    private async Task<AwkGetlineResult> ReadGetlineRecordAsync(IAwkInputRecordReader reader)
    {
        try
        {
            var input = await reader.ReadAsync().ConfigureAwait(false);
            if (!input.HasRecord)
                return AwkGetlineResult.EndOfFile;

            _counters.RecordRecordRead();
            return AwkGetlineResult.Record(
                input.RecordText,
                input.Terminator,
                input.FieldSplittingMode);
        }
        catch (AwkHostReadException exception)
        {
            return AwkGetlineResult.Failure(
                AwkIoError.FromReadFailure(exception.Descriptor, exception.Error));
        }
    }

    private AwkRecordCursor CreateRecordCursor(AwkFileDescriptor descriptor) =>
        new(
            _state,
            _descriptorReader,
            _host.CreateLineReader(descriptor, InputReadBatchLineCount));

    public async Task<AwkIoOperationResult> FlushAllAsync()
    {
        var paths = _descriptors.SnapshotOutputPaths();
        for (var index = 0; index < paths.Length; index++)
        {
            await _descriptors.FlushOutputAsync(_host, paths[index]).ConfigureAwait(false);
        }

        return AwkIoOperationResult.Success;
    }

    public async Task<AwkIoOperationResult> FlushPathAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A targeted flush requires a non-empty path.", nameof(path));

        if (_pipes.HasOutputPipe(path))
            return AwkIoOperationResult.Success;

        AwkSpecialPathPolicy.ThrowIfUnsupported(path);

        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkIoOperationResult.Success;

        var targetPath = _host.ResolvePath(path);
        var flushed = await _descriptors.FlushOutputAsync(_host, targetPath.Absolute)
            .ConfigureAwait(false);
        return AwkIoOperationResult.FromSuccess(flushed, AwkIoError.RedirectionNeverOpened);
    }

    public async Task CloseAsync()
    {
        await _pipes.CloseAllAsync().ConfigureAwait(false);
        await _descriptors.CloseAllAsync(_host).ConfigureAwait(false);
        _standardInputReader?.Dispose();
        _standardInputReader = null;
    }

    public async Task CloseForCleanupAsync()
    {
        await _pipes.CloseAllForCleanupAsync().ConfigureAwait(false);
        await _descriptors.CloseAllForCleanupAsync(_host).ConfigureAwait(false);
        _standardInputReader?.Dispose();
        _standardInputReader = null;
    }

    public async Task<AwkIoOperationResult> CloseAsync(string path)
    {
        var pipeClosed = await _pipes.CloseAnyAsync(path).ConfigureAwait(false);
        if (pipeClosed.ClosedPipe)
            return AwkIoOperationResult.SuccessWithStatusCode(pipeClosed.ExitCode.Value);

        AwkSpecialPathPolicy.ThrowIfUnsupported(path);

        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkIoOperationResult.Success;

        var targetPath = _host.ResolvePath(path);
        var closed = await _descriptors.CloseAnyAsync(_host, targetPath.Absolute)
            .ConfigureAwait(false);
        return AwkIoOperationResult.FromSuccess(closed, AwkIoError.RedirectionNeverOpened);
    }

    public async Task<AwkIoOperationResult> CloseAsync(string path, AwkCloseDirection direction)
    {
        if (direction == AwkCloseDirection.To)
        {
            var pipeClosed = await _pipes.CloseOutputAsync(path).ConfigureAwait(false);
            if (pipeClosed.ClosedPipe)
                return AwkIoOperationResult.SuccessWithStatusCode(pipeClosed.ExitCode.Value);

            if (_pipes.HasInputPipe(path))
                return AwkIoOperationResult.Failure(AwkIoError.RedirectionNeverOpened);
        }
        else if (direction == AwkCloseDirection.From)
        {
            var pipeClosed = await _pipes.CloseInputAsync(path).ConfigureAwait(false);
            if (pipeClosed.ClosedPipe)
                return AwkIoOperationResult.SuccessWithStatusCode(pipeClosed.ExitCode.Value);

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
        var absolutePath = targetPath.Absolute;
        if (direction == AwkCloseDirection.To)
        {
            var closed = await _descriptors.CloseOutputAsync(_host, absolutePath).ConfigureAwait(false);
            return AwkIoOperationResult.FromSuccess(closed, AwkIoError.RedirectionNeverOpened);
        }

        if (direction == AwkCloseDirection.From)
        {
            var closed = await _descriptors.CloseInputAsync(_host, absolutePath).ConfigureAwait(false);
            return AwkIoOperationResult.FromSuccess(closed, AwkIoError.RedirectionNeverOpened);
        }

        throw new ArgumentOutOfRangeException(nameof(direction));
    }

    private abstract class AwkInputReaderResult
    {
        public static Resolved Success(AwkRecordCursor reader) => new(reader);

        public static Failed Failure(AwkIoError error) => new(error);

        public sealed class Resolved(AwkRecordCursor reader) : AwkInputReaderResult
        {
            public AwkRecordCursor Reader { get; } = reader;
        }

        public sealed class Failed(AwkIoError error) : AwkInputReaderResult
        {
            public AwkIoError Error { get; } = error;
        }
    }

    private abstract class MainInputReaderBinding
    {
        public static MainInputReaderBinding Unbound { get; } = new Empty();

        public static Bound Bind(IAwkInputRecordReader reader) => new(reader);

        public sealed class Empty : MainInputReaderBinding
        {
        }

        public sealed class Bound(IAwkInputRecordReader reader) : MainInputReaderBinding
        {
            public IAwkInputRecordReader Reader { get; } = reader;
        }
    }
}
