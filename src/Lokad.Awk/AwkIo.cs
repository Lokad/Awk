using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIo
{
    private readonly AwkIoDescriptorTable _descriptors;
    private readonly AwkIoHostAdapter _host;
    private readonly AwkPipeRuntime _pipes;
    private readonly AwkSpecialPathPolicy _specialPaths;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkRuntimeState _state;
    private readonly AwkRecordDescriptorReader _descriptorReader;
    private readonly AwkFileDescriptor _stdIn;
    private readonly AwkFileDescriptor _stdOut;
    private readonly AwkFileDescriptor _stdErr;
    private readonly int _maxOutputFiles;
    private readonly int _maxInputFiles;
    private readonly bool _sandbox;
    private readonly bool _allowFileRedirections;
    private readonly bool _allowCommandPipes;
    private AwkRecordCursor? _standardInputReader;
    private long _standardInputOrder;
    private long _redirectionOrder;
    private MainInputReaderBinding _mainInputReader = MainInputReaderBinding.Unbound;
    private Action? _preserveMainStandardInput;

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
        Func<AwkMemoryLease, IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment,
        bool sandbox = false,
        bool allowFileRedirections = true,
        bool allowCommandPipes = true,
        AwkPath? accessiblePathRoot = null,
        bool forwardCommandEnvironment = true,
        bool exposeHostErrorDetails = true,
        int maxHostErrorBytes = 4 << 10)
    {
        _stdIn = stdIn;
        _stdOut = stdOut;
        _stdErr = stdErr;
        _host = new AwkIoHostAdapter(
            host,
            currentDirectory,
            stdIn,
            stdOut,
            stdErr,
            cancellationToken,
            snapshotEnvironment,
            state.Resources,
            state.MemoryAccounting,
            accessiblePathRoot,
            forwardCommandEnvironment,
            exposeHostErrorDetails,
            maxHostErrorBytes);
        _maxOutputFiles = limits.MaxOutputFiles;
        _maxInputFiles = limits.MaxInputFiles;
        _sandbox = sandbox;
        _allowFileRedirections = allowFileRedirections;
        _allowCommandPipes = allowCommandPipes;
        _state = state;
        _descriptors = new AwkIoDescriptorTable(state.MemoryAccounting);
        _descriptorReader = new AwkRecordDescriptorReader(
            host,
            cancellationToken,
            state.Resources,
            exposeHostErrorDetails,
            maxHostErrorBytes);
        _pipes = new AwkPipeRuntime(
            _host,
            state.MemoryAccounting,
            _maxOutputFiles,
            limits.MaxPipes,
            () => _descriptors.OutputFileCount,
            CreatePipeRecordCursor,
            NextRedirectionOrder);
        _specialPaths = new AwkSpecialPathPolicy(stdIn, stdOut, stdErr);
        _counters = counters;
    }

    public Task WriteStandardOutputAsync(ReadOnlyMemory<byte> content)
    {
        ReleaseStandardInputBorrow(_stdOut);
        return _host.WriteStandardOutputAsync(content);
    }

    internal AwkMemoryPeakAdmission AdmitPathRequest(string path, bool hasDisplay = false) =>
        _host.AdmitPathRequest(path, hasDisplay);

    public Task WriteStandardErrorAsync(ReadOnlyMemory<byte> content)
    {
        ReleaseStandardInputBorrow(_stdErr);
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
            ReleaseStandardInputBorrow(specialDescriptor);
            await _host.AppendAsync(specialDescriptor, output.Content)
                .ConfigureAwait(false);
            return;
        }

        if (!_descriptors.TryGetOutput(path.Text, out var descriptor))
        {
            using var resolution = _host.AdmitPathRequest(path.Text);
            var targetPath = _host.PreparePath(path.Text);
            if (!_descriptors.HasOutput(path.Text) &&
                _descriptors.OutputFileCount + _pipes.OutputPipeCount >= _maxOutputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorOutputFileLimit, AwkDiagnosticId.HostIoFailure);

            var mode = _descriptors.ResolveOutputOpenMode(path.Text, output.Kind);
            using var pending = _state.MemoryAccounting.CreateTransientLease();
            _descriptors.PrepareOutput(path.Text, pending);
            var opened = await _host.OpenFileAsync(targetPath.Request, mode).ConfigureAwait(false);
            if (!opened.Success)
            {
                throw new AwkRuntimeException(opened.Failed
                    ? AwkMessages.FormatPathError(targetPath.Display, _host.ResolveError(opened.Error))
                    : AwkMessages.FormatPathError(targetPath.Display),
                    AwkDiagnosticId.HostIoFailure);
            }

            descriptor = opened.FileDescriptor;
            _descriptors.AddOutput(path.Text, descriptor, NextRedirectionOrder(), pending);
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
                _preserveMainStandardInput?.Invoke();
                if (_standardInputReader is null)
                {
                    _standardInputReader = CreateRecordCursor(specialDescriptor);
                    _standardInputOrder = NextRedirectionOrder();
                }
                _standardInputReader.PreserveBorrowedInput(releaseDescriptor: true);
                return AwkInputReaderResult.Success(_standardInputReader);
            }

            if (_descriptors.TryGetInput(path, out var existing))
                return AwkInputReaderResult.Success(existing);

            using var resolution = _host.AdmitPathRequest(path);
            var targetPath = _host.PreparePath(path);
            if (_descriptors.InputFileCount >= _maxInputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorInputFileLimit, AwkDiagnosticId.RuntimeResourceLimit);

            using var pending = _state.MemoryAccounting.CreateTransientLease();
            _descriptors.PrepareInput(path, pending);
            var opened = await _host.OpenFileAsync(targetPath.Request, AwkFileOpenMode.Read).ConfigureAwait(false);
            if (!opened.Success)
            {
                var error = AwkIoError.FromErrno(opened.Failed
                    ? AwkErrno.FromHostMessage(_host.ResolveError(opened.Error))
                    : AwkErrno.NoSuchFileOrDirectory);
                return AwkInputReaderResult.Failure(error);
            }

            var reader = CreateRecordCursor(opened.FileDescriptor);
            _descriptors.AddInput(path, opened.FileDescriptor, reader, NextRedirectionOrder(), pending);
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
        // An input-pipe command borrows parent stdin as well as its new output pipe.
        ReleaseStandardInputBorrow(_stdIn);
        var reader = await _pipes.GetInputReaderAsync(command).ConfigureAwait(false);
        return await ReadGetlineRecordAsync(reader).ConfigureAwait(false);
    }

    public void BindMainInputReader(IAwkInputRecordReader reader, Action? preserveStandardInput = null)
    {
        _mainInputReader = MainInputReaderBinding.Bind(reader);
        _preserveMainStandardInput = preserveStandardInput;
    }

    public void ClearMainInputReader()
    {
        _mainInputReader = MainInputReaderBinding.Unbound;
        _preserveMainStandardInput = null;
    }

    public void PreserveStandardInputIfRedirected(AwkRecordCursor cursor)
    {
        if (_standardInputReader is not null) cursor.PreserveBorrowedInput(releaseDescriptor: true);
    }

    public void PreserveRedirectedStandardInput() =>
        _standardInputReader?.PreserveBorrowedInput(releaseDescriptor: true);

    private void ReleaseStandardInputBorrow(AwkFileDescriptor descriptor)
    {
        if (descriptor != _stdIn) return;
        _preserveMainStandardInput?.Invoke();
        PreserveRedirectedStandardInput();
    }

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
                AwkIoError.FromErrno(AwkErrno.FromHostMessage(exception.Error.Message)));
        }
    }

    private AwkRecordCursor CreateRecordCursor(AwkFileDescriptor descriptor) =>
        CreateRecordCursor(descriptor, AwkParagraphLeaderPolicy.Discard);

    private AwkRecordCursor CreatePipeRecordCursor(AwkFileDescriptor descriptor) =>
        CreateRecordCursor(descriptor, AwkParagraphLeaderPolicy.Retain);

    private AwkRecordCursor CreateRecordCursor(
        AwkFileDescriptor descriptor, AwkParagraphLeaderPolicy paragraphLeaderPolicy) =>
        new(
            _state,
            _descriptorReader.CreateByteReader(descriptor),
            paragraphLeaderPolicy);

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
        var redirection = _pipes.GetRedirection(path).Newer(_descriptors.GetRedirection(path));
        if (_specialPaths.TryResolveInput(path, out _))
        {
            return _standardInputReader is null ? redirection : redirection.Newer(
                new AwkOpenRedirection(AwkOpenRedirectionKind.StandardInput, _standardInputOrder));
        }
        return redirection;
    }

    public async Task<AwkFlushStatus> FlushAllAsync()
    {
        await _descriptors.FlushAllAsync(_host).ConfigureAwait(false);
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
                await _descriptors.FlushOutputAsync(_host, path).ConfigureAwait(false);
                return AwkFlushStatus.Success;
        }

        AwkSpecialPathPolicy.ThrowIfUnsupported(path);
        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkFlushStatus.Success;

        return AwkFlushStatus.Unopened;
    }

    public async Task CloseAsync()
    {
        await _pipes.CloseAllAsync().ConfigureAwait(false);
        await _descriptors.CloseAllAsync(_host).ConfigureAwait(false);
        _standardInputReader?.Dispose();
        _standardInputReader = null;
        _host.DisposeCommandCancellation();
    }

    public void CancelCommandsForCleanup() => _host.CancelCommandsForCleanup();

    public async Task CloseForCleanupAsync()
    {
        CancelCommandsForCleanup();
        try
        {
            await _pipes.CloseAllForCleanupAsync().ConfigureAwait(false);
            await _descriptors.CloseAllForCleanupAsync(_host).ConfigureAwait(false);
            _standardInputReader?.Dispose();
            _standardInputReader = null;
        }
        finally { _host.DisposeCommandCancellation(); }
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
                return AwkIoOperationResult.SuccessWithStatusCode(written.Value);
            case AwkOpenRedirectionKind.InputPipe:
                var read = await _pipes.CloseInputAsync(path).ConfigureAwait(false);
                return AwkIoOperationResult.SuccessWithStatusCode(read.Value);
            case AwkOpenRedirectionKind.OutputFile:
                await _descriptors.CloseOutputAsync(_host, path).ConfigureAwait(false);
                return AwkIoOperationResult.Success;
            case AwkOpenRedirectionKind.InputFile:
                await _descriptors.CloseInputAsync(_host, path).ConfigureAwait(false);
                return AwkIoOperationResult.Success;
            case AwkOpenRedirectionKind.StandardInput:
                _standardInputReader?.Dispose();
                _standardInputReader = null;
                return AwkIoOperationResult.Success;
        }
        AwkSpecialPathPolicy.ThrowIfUnsupported(path);
        if (_specialPaths.TryResolveOutput(path, out _))
            return AwkIoOperationResult.Success;

        return AwkIoOperationResult.Failure(AwkIoError.RedirectionNeverOpened);
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
