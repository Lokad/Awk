namespace Lokad.Awk;

internal sealed class AwkIo
{
    private readonly AwkIoDescriptorTable _descriptors;
    private readonly AwkIoHostAdapter _host;
    private readonly AwkPipeRuntime _pipes;
    private readonly AwkSpecialPathPolicy _specialPaths;
    private readonly AwkRuntimeState _state;
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
    private AwkInputScheduler? _mainInput;

    internal AwkIo(
        IAwkHost host,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr,
        CancellationToken cancellationToken,
        AwkRuntimeLimits limits,
        AwkRuntimeState state,
        Func<AwkMemoryLease, IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment,
        bool sandbox = false,
        bool allowFileRedirections = true,
        bool allowCommandPipes = true,
        AwkPath? accessiblePathRoot = null,
        bool forwardCommandEnvironment = true)
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
            state.Options,
            state.MemoryAccounting,
            accessiblePathRoot,
            forwardCommandEnvironment);
        _maxOutputFiles = limits.MaxOutputFiles;
        _maxInputFiles = limits.MaxInputFiles;
        _sandbox = sandbox;
        _allowFileRedirections = allowFileRedirections;
        _allowCommandPipes = allowCommandPipes;
        _state = state;
        _descriptors = new AwkIoDescriptorTable(state.MemoryAccounting);
        _pipes = new AwkPipeRuntime(
            _host,
            state.MemoryAccounting,
            _maxOutputFiles,
            limits.MaxPipes,
            () => _descriptors.OutputFileCount,
            CreatePipeRecordCursor,
            NextRedirectionOrder);
        _specialPaths = new AwkSpecialPathPolicy(stdIn, stdOut, stdErr);
    }

    internal AwkIoHostAdapter HostAdapter => _host;

    public Task WriteStandardOutputAsync(ReadOnlyMemory<byte> content)
    {
        ReleaseStandardInputBorrow(_stdOut);
        return _host.WriteStandardOutputAsync(content);
    }

    internal AwkMemoryLease AdmitPathRequest(string path, bool hasDisplay = false) =>
        _host.AdmitPathRequest(path, hasDisplay);

    public Task WriteStandardErrorAsync(ReadOnlyMemory<byte> content)
    {
        ReleaseStandardInputBorrow(_stdErr);
        return _host.WriteStandardErrorAsync(content);
    }

    public async Task WriteAsync(AwkRedirectedOutput output)
    {
        var target = output.Target;
        if (output.Kind == AwkOutputRedirectionKind.Pipe)
        {
            ThrowIfCommandPipeDisabled();
            await _pipes.WriteOutputAsync(target, output.Content).ConfigureAwait(false);
            return;
        }

        ThrowIfFileRedirectionDisabled();

        AwkSpecialPathPolicy.ThrowIfUnsupported(target);

        if (_specialPaths.TryResolveOutput(target, out var specialDescriptor))
        {
            ReleaseStandardInputBorrow(specialDescriptor);
            await _host.AppendAsync(specialDescriptor, output.Content)
                .ConfigureAwait(false);
            return;
        }

        if (!_descriptors.TryGetOutput(target, out var descriptor))
        {
            using var resolution = _host.AdmitPathRequest(target);
            var targetPath = _host.PreparePath(target);
            if (!_descriptors.HasOutput(target) &&
                _descriptors.OutputFileCount + _pipes.OutputPipeCount >= _maxOutputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorOutputFileLimit, AwkDiagnosticId.HostIoFailure);

            var mode = _descriptors.ResolveOutputOpenMode(target, output.Kind);
            using var pending = _state.MemoryAccounting.CreateTransientLease();
            _descriptors.PrepareOutput(target, pending);
            var opened = await _host.OpenFileAsync(targetPath.Request, mode).ConfigureAwait(false);
            if (!opened.Success)
            {
                throw new AwkRuntimeException(AwkMessages.FormatPathError(targetPath.Display, _host.ResolveError(opened.Error)),
                    AwkDiagnosticId.HostIoFailure);
            }

            descriptor = opened.FileDescriptor;
            _descriptors.AddOutput(target, descriptor, NextRedirectionOrder(), pending);
        }

        await _host.AppendAsync(descriptor, output.Content).ConfigureAwait(false);
    }

    public ValueTask<AwkGetlineCompletion> ReadRedirectedRecordAsync(string path)
    {
        ThrowIfFileRedirectionDisabled();
        AwkSpecialPathPolicy.ThrowIfUnsupported(path);

        if (_specialPaths.TryResolveInput(path, out var specialDescriptor))
        {
            if (_standardInputReader is null)
            {
                _standardInputReader = CreateRecordCursor(specialDescriptor, beforeHostRead: PreserveMainStandardInput);
                _standardInputOrder = NextRedirectionOrder();
            }
            return ReadGetlineRecordAsync(_standardInputReader);
        }

        return _descriptors.TryGetInput(path, out var existing)
            ? ReadGetlineRecordAsync(existing)
            : OpenAndReadAsync(path);
    }

    private async ValueTask<AwkGetlineCompletion> OpenAndReadAsync(string path)
    {
        AwkRecordCursor reader;
        {
            using var resolution = _host.AdmitPathRequest(path);
            var targetPath = _host.PreparePath(path);
            if (_descriptors.InputFileCount >= _maxInputFiles)
                throw new AwkRuntimeException(AwkMessages.ErrorInputFileLimit, AwkDiagnosticId.RuntimeResourceLimit);

            using var pending = _state.MemoryAccounting.CreateTransientLease();
            _descriptors.PrepareInput(path, pending);
            var opened = await _host.OpenFileAsync(targetPath.Request, AwkFileOpenMode.Read).ConfigureAwait(false);
            if (!opened.Success)
                return AwkGetlineCompletion.Failure(AwkErrno.FromHostMessage(_host.ResolveError(opened.Error)));

            reader = CreateRecordCursor(opened.FileDescriptor);
            _descriptors.AddInput(path, opened.FileDescriptor, reader, NextRedirectionOrder(), pending);
        }

        return await ReadGetlineRecordAsync(reader).ConfigureAwait(false);
    }

    public async ValueTask<AwkGetlineCompletion> ReadPipeRecordAsync(string command)
    {
        ThrowIfCommandPipeDisabled();
        // An input-pipe command borrows parent stdin as well as its new output pipe.
        ReleaseStandardInputBorrow(_stdIn);
        var reader = await _pipes.GetInputReaderAsync(command).ConfigureAwait(false);
        return await ReadGetlineRecordAsync(reader).ConfigureAwait(false);
    }

    public void BindMainInputReader(AwkInputScheduler input) => _mainInput = input;

    public void ClearMainInputReader() => _mainInput = null;

    private void PreserveMainStandardInput() => _mainInput?.PreserveStandardInput();

    private void PreserveRedirectedStandardInput() =>
        _standardInputReader?.PreserveBorrowedInput(releaseDescriptor: true);

    private void ReleaseStandardInputBorrow(AwkFileDescriptor descriptor)
    {
        if (descriptor != _stdIn) return;
        PreserveMainStandardInput();
        PreserveRedirectedStandardInput();
    }

    public ValueTask<AwkGetlineCompletion> ReadMainRecordAsync()
    {
        if (_mainInput is not { } input)
            return ValueTask.FromResult(AwkGetlineCompletion.EndOfFile);

        return ReadGetlineRecordAsync(input);
    }

    private static ValueTask<AwkGetlineCompletion> ReadGetlineRecordAsync(IAwkInputRecordReader reader)
    {
        try
        {
            var read = reader.MoveNextAsync();
            if (!read.IsCompletedSuccessfully)
                return CompleteAsync(reader, read);

            // Consume the completed read. EOF and any RT change come from TakeRecord.
            _ = read.Result;
            return ValueTask.FromResult(AwkGetlineCompletion.Read(reader));
        }
        catch (AwkHostReadException exception)
        {
            return ValueTask.FromResult(AwkGetlineCompletion.Failure(AwkErrno.FromHostMessage(exception.Error.Message)));
        }

        static async ValueTask<AwkGetlineCompletion> CompleteAsync(IAwkInputRecordReader reader, ValueTask<bool> read)
        {
            try
            {
                await read.ConfigureAwait(false);
                return AwkGetlineCompletion.Read(reader);
            }
            catch (AwkHostReadException exception)
            {
                return AwkGetlineCompletion.Failure(AwkErrno.FromHostMessage(exception.Error.Message));
            }
        }
    }

    internal AwkRecordCursor CreateMainInputCursor(AwkFileDescriptor descriptor) =>
        CreateRecordCursor(descriptor,
            beforeHostRead: descriptor == _stdIn ? PreserveRedirectedStandardInput : null);

    private AwkRecordCursor CreatePipeRecordCursor(AwkFileDescriptor descriptor) =>
        CreateRecordCursor(descriptor, AwkParagraphLeaderPolicy.Retain);

    private AwkRecordCursor CreateRecordCursor(
        AwkFileDescriptor descriptor,
        AwkParagraphLeaderPolicy paragraphLeaderPolicy = AwkParagraphLeaderPolicy.Discard,
        Action? beforeHostRead = null) =>
        new(
            _state,
            _host.CreateByteReader(descriptor, beforeHostRead),
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
            return AwkIoOperationResult.Failure(AwkErrno.RedirectionNeverOpened);
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

        return AwkIoOperationResult.Failure(AwkErrno.RedirectionNeverOpened);
    }
}
