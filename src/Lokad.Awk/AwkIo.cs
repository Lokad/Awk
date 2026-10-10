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
    private readonly int _maxInputFiles;
    private readonly bool _sandbox;
    private readonly bool _allowFileRedirections;
    private readonly bool _allowCommandPipes;
    private AwkRecordCursor? _standardInputReader;
    private long _standardInputOrder;
    private long _redirectionOrder;
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
        Func<IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment,
        bool sandbox = false,
        bool allowFileRedirections = true,
        bool allowCommandPipes = true,
        AwkPath? accessiblePathRoot = null,
        TimeSpan? commandTimeout = null,
        TimeSpan? cleanupTimeout = null,
        bool forwardCommandEnvironment = true,
        TimeSpan? hostOperationTimeout = null,
        bool exposeHostErrorDetails = true,
        int maxHostErrorBytes = 4 << 10,
        AwkCleanupDeadline? cleanupDeadline = null)
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
            state.Resources,
            accessiblePathRoot,
            commandTimeout,
            cleanupTimeout,
            forwardCommandEnvironment,
            hostOperationTimeout,
            exposeHostErrorDetails,
            maxHostErrorBytes,
            cleanupDeadline);
        _maxOutputFiles = limits.MaxOutputFiles;
        _maxInputFiles = limits.MaxInputFiles;
        _sandbox = sandbox;
        _allowFileRedirections = allowFileRedirections;
        _allowCommandPipes = allowCommandPipes;
        _state = state;
        _descriptorReader = new AwkRecordDescriptorReader(
            host,
            cancellationToken,
            state.Resources,
            hostOperationTimeout,
            exposeHostErrorDetails,
            maxHostErrorBytes);
        _pipes = new AwkPipeRuntime(
            _host,
            _maxOutputFiles,
            limits.MaxPipes,
            () => _descriptors.OutputFileCount,
            CreateRecordCursor,
            NextRedirectionOrder);
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
            ThrowIfCommandPipeDisabled();
            await _pipes.WriteOutputAsync(path, output.Content).ConfigureAwait(false);
            return;
        }

        ThrowIfFileRedirectionDisabled();

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
            if (!_descriptors.HasOutput(absolutePath) &&
                _descriptors.OutputFileCount + _pipes.OutputPipeCount >= _maxOutputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorOutputFileLimit, AwkDiagnosticId.HostIoFailure);

            var mode = _descriptors.ResolveOutputOpenMode(absolutePath, output.Kind);
            var opened = await _host.OpenFileAsync(absolutePath, mode).ConfigureAwait(false);
            if (!opened.Success)
            {
                throw new AwkRuntimeException(opened.Failed
                    ? AwkMessages.FormatPathError(targetPath.Display, _host.ResolveError(opened.Error))
                    : AwkMessages.FormatPathError(targetPath.Display),
                    AwkDiagnosticId.HostIoFailure);
            }

            descriptor = opened.FileDescriptor;
            _descriptors.AddOutput(absolutePath, descriptor, NextRedirectionOrder());
        }

        await _host.AppendAsync(descriptor, output.Content).ConfigureAwait(false);
    }

    public async Task<AwkGetlineResult> ReadRedirectedRecordAsync(string path)
    {
        ThrowIfFileRedirectionDisabled();
        async Task<AwkInputReaderResult> ResolveInputReaderAsync()
        {
            AwkSpecialPathPolicy.ThrowIfUnsupported(path);

            if (_specialPaths.TryResolveInput(path, out var specialDescriptor))
            {
                if (_standardInputReader is null)
                {
                    _standardInputReader = CreateRecordCursor(specialDescriptor);
                    _standardInputOrder = NextRedirectionOrder();
                }
                return AwkInputReaderResult.Success(_standardInputReader);
            }

            var targetPath = _host.ResolvePath(path);
            var absolutePath = targetPath.Absolute;
            if (_descriptors.TryGetInput(absolutePath, out var existing))
                return AwkInputReaderResult.Success(existing);

            if (_descriptors.InputFileCount >= _maxInputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorInputFileLimit, AwkDiagnosticId.RuntimeResourceLimit);

            var opened = await _host.OpenFileAsync(targetPath.Absolute, AwkFileOpenMode.Read).ConfigureAwait(false);
            if (!opened.Success)
            {
                var error = opened.Failed
                    ? AwkIoError.FromOpenFailure(
                        targetPath.Display,
                        AwkErrno.FromHostMessage(_host.ResolveError(opened.Error)))
                    : AwkIoError.FromMissingPath(targetPath.Display);
                return AwkInputReaderResult.Failure(error);
            }

            var reader = CreateRecordCursor(opened.FileDescriptor);
            _descriptors.AddInput(absolutePath, opened.FileDescriptor, reader, NextRedirectionOrder());
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
        ThrowIfCommandPipeDisabled();
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
                return input.ClearsRecordTerminator
                    ? AwkGetlineResult.EndOfFileClearingTerminator
                    : AwkGetlineResult.EndOfFile;

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

    private void ThrowIfFileRedirectionDisabled()
    {
        if (_sandbox)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorSandboxRedirection,
                AwkDiagnosticId.UnsupportedPolicy);
        }

        if (!_allowFileRedirections)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorFileRedirectionsDisabled,
                AwkDiagnosticId.UnsupportedPolicy);
        }
    }

    private void ThrowIfCommandPipeDisabled()
    {
        if (_sandbox)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorSandboxRedirection,
                AwkDiagnosticId.UnsupportedPolicy);
        }

        if (!_allowCommandPipes)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorCommandPipesDisabled,
                AwkDiagnosticId.UnsupportedPolicy);
        }
    }

    private long NextRedirectionOrder() => ++_redirectionOrder;

    private AwkOpenRedirection GetRedirection(string path)
    {
        if (path.Length == 0)
            return default;
        var pipe = _pipes.GetRedirection(path);
        if (_specialPaths.TryResolveInput(path, out _))
        {
            return _standardInputReader is null ? pipe : pipe.Newer(
                new AwkOpenRedirection(AwkOpenRedirectionKind.StandardInput, _standardInputOrder));
        }
        if (_specialPaths.TryResolveOutput(path, out _))
            return pipe;

        return pipe.Newer(_descriptors.GetRedirection(_host.ResolveExistingPath(path)));
    }

    public async Task<AwkFlushStatus> FlushAllAsync()
    {
        var paths = _descriptors.SnapshotOutputPaths();
        for (var index = 0; index < paths.Length; index++)
            await _descriptors.FlushOutputAsync(_host, paths[index]).ConfigureAwait(false);
        return AwkFlushStatus.Success;
    }

    public async Task<AwkFlushStatus> FlushPathAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A targeted flush requires a non-empty path.", nameof(path));

        var redirection = GetRedirection(path);
        switch (redirection.Kind)
        {
            case AwkOpenRedirectionKind.InputPipe:
                return AwkFlushStatus.InputPipe;
            case AwkOpenRedirectionKind.InputFile:
            case AwkOpenRedirectionKind.StandardInput:
                return AwkFlushStatus.InputFile;
            case AwkOpenRedirectionKind.OutputPipe:
                return AwkFlushStatus.Success;
            case AwkOpenRedirectionKind.OutputFile:
                await _descriptors.FlushOutputAsync(_host, _host.ResolveExistingPath(path)).ConfigureAwait(false);
                return AwkFlushStatus.Success;
        }

        AwkSpecialPathPolicy.ThrowIfUnsupported(path);
        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkFlushStatus.Success;

        _ = _host.ResolvePath(path);
        return AwkFlushStatus.Unopened;
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
        if (path.Length == 0)
            return AwkIoOperationResult.Failure(AwkIoError.RedirectionNeverOpened);
        var redirection = GetRedirection(path);
        switch (redirection.Kind)
        {
            case AwkOpenRedirectionKind.OutputPipe:
                var written = await _pipes.CloseOutputAsync(path).ConfigureAwait(false);
                return AwkIoOperationResult.SuccessWithStatusCode(written.ExitCode.Value);
            case AwkOpenRedirectionKind.InputPipe:
                var read = await _pipes.CloseInputAsync(path).ConfigureAwait(false);
                return AwkIoOperationResult.SuccessWithStatusCode(read.ExitCode.Value);
            case AwkOpenRedirectionKind.OutputFile:
                await _descriptors.CloseOutputAsync(_host, _host.ResolveExistingPath(path)).ConfigureAwait(false);
                return AwkIoOperationResult.Success;
            case AwkOpenRedirectionKind.InputFile:
                await _descriptors.CloseInputAsync(_host, _host.ResolveExistingPath(path)).ConfigureAwait(false);
                return AwkIoOperationResult.Success;
            case AwkOpenRedirectionKind.StandardInput:
                _standardInputReader?.Dispose();
                _standardInputReader = null;
                return AwkIoOperationResult.Success;
        }
        AwkSpecialPathPolicy.ThrowIfUnsupported(path);
        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkIoOperationResult.Success;

        _ = _host.ResolvePath(path);
        return AwkIoOperationResult.Failure(AwkIoError.RedirectionNeverOpened);
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
