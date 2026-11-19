namespace Lokad.Awk;

/// <summary>
/// Owns the lazy, ordered main-input stream shared by ordinary record processing and plain
/// <c>getline</c>. Command-line assignments and input selection take effect only when the stream
/// reaches them, so <c>BEGIN</c> can still change <c>ARGV</c> before the first read.
/// </summary>
internal sealed class AwkInputScheduler(
    AwkInvocation invocation,
    AwkRuntimeActionExecutor actionExecutor,
    AwkRuntimeState state,
    AwkIo io,
    CancellationToken cancellationToken,
    AwkPath? accessiblePathRoot = null,
    bool allowDynamicMainInputPaths = true) : IAwkInputRecordReader
{
    private static readonly AwkText StandardInputFileName = AwkText.FromSourceLiteralString("-");
    private AwkFileDescriptor? _ownedDescriptor;
    private ReadOnlyMemory<byte> _ownedInputDisplay;
    private readonly AwkMemoryLease _inputDisplayMemory = state.MemoryAccounting.CreateTransientLease();
    private AwkRecordCursor? _currentInput;
    private long _nextArgvIndex = 1;
    private bool _selectedInputSource;
    private bool _usedStandardInputFallback;
    private MainInputReadProhibition _mainInputReadProhibition;

    public void PreserveStandardInput()
    {
        if (_currentInput is { } input && _ownedDescriptor is null)
            input.PreserveBorrowedInput(releaseDescriptor: true);
    }

    public void TakeRecord(out AwkInputRecordResult record)
    {
        if (_currentInput is { } input) input.TakeRecord(out record);
        else record = AwkInputRecordResult.EndOfFile;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        // An explicit getline can reenter the scheduler through a file action.
        // Processing reads resume only after that action returns.
        if (_mainInputReadProhibition == MainInputReadProhibition.BeginFile)
            throw new AwkRuntimeException(AwkMessages.ErrorMainInputGetlineInBeginFile);

        if (_mainInputReadProhibition == MainInputReadProhibition.EndFile)
            throw new AwkRuntimeException(AwkMessages.ErrorMainInputGetlineInEndFile);

        ValueTask<bool> pending = default;
        var status = MoveNext(null, ref pending);
        return status == AwkRecordReadStatus.Pending ? pending : ValueTask.FromResult(status == AwkRecordReadStatus.Record);
    }

    private AwkRecordReadStatus MoveNext(AwkRecordStorage? storage, ref ValueTask<bool> pending)
    {
        if (state.ExitRequested) return AwkRecordReadStatus.EndOfFile;
        if (_currentInput is not { } input)
        {
            pending = AdvanceInputAsync(storage, default, hasRead: false);
            return AwkRecordReadStatus.Pending;
        }

        if (_ownedDescriptor is null) io.PreserveRedirectedStandardInput();
        var status = input.MoveNext(storage, ref pending);
        // The cursor checks host/admission completion; language publication owns
        // its post-admission, pre-effect check. This handoff adds no new boundary.
        if (status == AwkRecordReadStatus.Record) return status;
        if (status == AwkRecordReadStatus.EndOfFile) pending = ValueTask.FromResult(false);
        pending = AdvanceInputAsync(storage, pending, hasRead: true);
        return AwkRecordReadStatus.Pending;
    }

    private async ValueTask<bool> AdvanceInputAsync(AwkRecordStorage? storage,
        ValueTask<bool> pending, bool hasRead)
    {
        while (!state.ExitRequested)
        {
            if (!hasRead)
            {
                if (_currentInput is null && !await TryOpenNextInputAsync().ConfigureAwait(false))
                    return false;
                var input = _currentInput ??
                    throw new InvalidOperationException("Main input was not initialized after a successful open.");
                if (_ownedDescriptor is null) io.PreserveRedirectedStandardInput();
                pending = input.MoveNextAsync(storage);
            }
            if (await pending.ConfigureAwait(false))
                return true;

            var completedInput = _currentInput ??
                throw new InvalidOperationException("A completed read requires an input cursor.");
            completedInput.TakeRecord(out var record);
            if (record.ClearsRecordTerminator)
                state.SetRecordTerminator(AwkRecordTerminator.Empty);

            var endFileFlow = await CompleteCurrentInputAsync(EndFileAction.Execute).ConfigureAwait(false);
            if (endFileFlow == AwkStatementFlow.ExitProgram)
                return false;
            hasRead = false;
        }

        return false;

        async ValueTask<bool> TryOpenNextInputAsync()
        {
            while (_nextArgvIndex < state.MainInputArgumentCount)
            {
                state.ThrowIfCancellationRequested();
                var argumentIndex = new AwkArgvIndex((int)_nextArgvIndex++);
                if (!state.TryGetArgumentVectorByteText(argumentIndex, out var argumentText) ||
                    argumentText.ByteLength == 0)
                {
                    continue;
                }

                var inputArgument = GetOriginalInputArgument(argumentIndex);
                var originalFile = inputArgument as AwkInputArgument.FileArgument;
                if (originalFile is not null &&
                    !argumentText.AsSpan(state.CancellationCheckpoint).SequenceEqual(originalFile.Display.Span))
                    originalFile = null;
                var isUnchangedFile = originalFile is not null;
                if (!isUnchangedFile && argumentText.AsSpan(state.CancellationCheckpoint).Contains((byte)'=') &&
                    AwkVariableAssignment.TryParse(
                        ResolveArgumentString(argumentText),
                        AwkVariableAssignmentSource.InputArgument,
                        out var assignment))
                {
                    state.SetArgumentIndex(argumentIndex);
                    state.AssignVariableFromExternalText(assignment);
                    await actionExecutor.Output.FlushDiagnosticsAsync(io).ConfigureAwait(false);
                    continue;
                }

                _selectedInputSource = true;
                if (!allowDynamicMainInputPaths && !isUnchangedFile &&
                    !(inputArgument is AwkInputArgument.StandardInputArgument &&
                        AwkSpecialPathPolicy.IsStandardInput(argumentText)))
                    throw new AwkRuntimeException(AwkMessages.ErrorDynamicMainInputPathsDisabled);

                if (AwkSpecialPathPolicy.IsStandardInput(argumentText))
                {
                    state.BeginInputFile(argumentText, argumentIndex, actionExecutor.HasBeginFileActions);
                    if (await TryActivateInputAsync(
                            CreateRecordCursor(invocation.Stdio.StdIn))
                        .ConfigureAwait(false))
                    {
                        return true;
                    }

                    if (state.ExitRequested)
                        return false;

                    continue;
                }

                var rawPath = originalFile?.RawPath ?? ResolveArgumentString(argumentText);
                using var resolution = io.AdmitPathRequest(rawPath, hasDisplay: originalFile is not null);
                state.ThrowIfCancellationRequested();
                var path = AwkPathResolution.PrepareRuntimeArgument(rawPath,
                    invocation.RuntimeArgumentsPlan.CurrentDirectory, originalFile?.Display ?? default,
                    accessiblePathRoot);
                state.BeginInputFile(argumentText, argumentIndex, actionExecutor.HasBeginFileActions);
                var displayBytes = AwkRuntimeMemory.EstimateInputDisplayBytes(path.Display.Length);
                using var pendingDisplay = state.MemoryAccounting.ReserveTransient(displayBytes);
                var opened = await io.HostAdapter.OpenFileAsync(path.Request, AwkFileOpenMode.Read)
                    .ConfigureAwait(false);
                if (!opened.Success)
                {
                    if (await HandleOpenFailureAsync(path, opened).ConfigureAwait(false))
                        return false;

                    continue;
                }

                _ownedDescriptor = opened.FileDescriptor;
                _ownedInputDisplay = path.Display;
                pendingDisplay.MoveTo(_inputDisplayMemory, displayBytes);
                if (await TryActivateInputAsync(
                        CreateRecordCursor(opened.FileDescriptor))
                    .ConfigureAwait(false))
                {
                    return true;
                }

                if (state.ExitRequested)
                    return false;
            }

            if (_selectedInputSource || _usedStandardInputFallback)
                return false;

            _usedStandardInputFallback = true;
            state.BeginInputFile(StandardInputFileName, new AwkArgvIndex(0), actionExecutor.HasBeginFileActions);
            return await TryActivateInputAsync(
                    CreateRecordCursor(invocation.Stdio.StdIn))
                .ConfigureAwait(false);

            AwkInputArgument? GetOriginalInputArgument(AwkArgvIndex index)
            {
                var planIndex = index.Value - 1;
                if (planIndex >= invocation.InputArguments.Count)
                    return null;

                var original = invocation.InputArguments[planIndex];
                return original.ArgvIndex == index ? original : null;
            }

            string ResolveArgumentString(AwkText text)
            {

                return state.DecodeText(text);
            }

            async ValueTask<bool> TryActivateInputAsync(
                AwkRecordCursor cursor)
            {
                _currentInput = cursor;
                var beginFileFlow = await ExecuteBeginFileActionsAsync().ConfigureAwait(false);
                if (beginFileFlow == AwkStatementFlow.Continue)
                    return true;

                await CompleteCurrentInputAsync(
                        beginFileFlow == AwkStatementFlow.NextFile ? EndFileAction.Execute : EndFileAction.Skip)
                    .ConfigureAwait(false);
                return false;
            }

            async ValueTask<bool> HandleOpenFailureAsync(AwkFileOperand path, AwkOpenedFile opened)
            {
                if (!actionExecutor.HasBeginFileActions)
                    throw CreateOpenFailure(path, opened);

                state.SetErrno(AwkErrno.FromHostMessage(io.HostAdapter.ResolveError(opened.Error)));
                var flow = await ExecuteBeginFileActionsAsync().ConfigureAwait(false);
                if (flow == AwkStatementFlow.ExitProgram)
                    return true;

                if (flow != AwkStatementFlow.NextFile)
                    throw CreateOpenFailure(path, opened);

                return false;

                AwkRuntimeException CreateOpenFailure(AwkFileOperand path, AwkOpenedFile opened) =>
                    new(
                        AwkMessages.FormatPathError(path.Display, io.HostAdapter.ResolveError(opened.Error)),
                        AwkDiagnosticId.HostIoFailure);
            }

            AwkRecordCursor CreateRecordCursor(AwkFileDescriptor descriptor)
            {
                var cursor = io.CreateRecordCursor(descriptor);
                if (descriptor == invocation.Stdio.StdIn)
                    io.PreserveStandardInputIfRedirected(cursor);
                return cursor;
            }
        }
    }

    internal AwkRecordReadStatus ReadForProcessing(ref ValueTask<bool> pending) =>
        MoveNext(state.InputRecordStorage, ref pending);

    internal ValueTask<AwkStatementFlow> CompleteActionAsync(AwkStatementFlow flow) =>
        CompleteCurrentInputAsync(flow == AwkStatementFlow.ExitProgram ? EndFileAction.Skip : EndFileAction.Execute);

    public Task ProcessRemainingInputAsync() => actionExecutor.ExecuteMainInputAsync(state, io, this);

    public async Task CloseRemainingDescriptorsAsync()
    {
        await CompleteCurrentInputAsync(EndFileAction.Skip).ConfigureAwait(false);
        await CloseOwnedDescriptorAsync().ConfigureAwait(false);

        io.ClearMainInputReader();
        _inputDisplayMemory.Dispose();
    }

    public async Task CloseRemainingDescriptorsForCleanupAsync()
    {
        _currentInput?.Dispose();
        _currentInput = null;
        if (_ownedDescriptor is { } descriptor)
        {
            try
            {
                await io.HostAdapter.CloseDescriptorWithoutCancellationAsync(descriptor).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cleanup must preserve the exception that interrupted execution.
            }
            finally { RetireOwnedDescriptor(); }
        }

        io.ClearMainInputReader();
        _inputDisplayMemory.Dispose();
    }

    private async ValueTask<AwkStatementFlow> CompleteCurrentInputAsync(EndFileAction endFileAction)
    {
        if (_currentInput is null)
            return AwkStatementFlow.Continue;

        var flow = endFileAction == EndFileAction.Execute
            ? await ExecuteEndFileActionsAsync().ConfigureAwait(false)
            : AwkStatementFlow.Continue;
        var input = _currentInput;
        _currentInput = null;
        input.Dispose();
        await CloseOwnedDescriptorAsync().ConfigureAwait(false);

        return flow;

        async ValueTask<AwkStatementFlow> ExecuteEndFileActionsAsync()
        {
            _mainInputReadProhibition = MainInputReadProhibition.EndFile;
            try
            {
                return await actionExecutor.ExecuteEndFileActionsAsync(state, io).ConfigureAwait(false);
            }
            finally
            {
                _mainInputReadProhibition = MainInputReadProhibition.None;
            }
        }
    }

    private async ValueTask<AwkStatementFlow> ExecuteBeginFileActionsAsync()
    {
        _mainInputReadProhibition = MainInputReadProhibition.BeginFile;
        try
        {
            return await actionExecutor.ExecuteBeginFileActionsAsync(state, io).ConfigureAwait(false);
        }
        finally
        {
            _mainInputReadProhibition = MainInputReadProhibition.None;
        }
    }

    private async Task CloseOwnedDescriptorAsync()
    {
        if (_ownedDescriptor is not { } descriptor)
            return;

        // Keep ownership through pending or failed closes; cleanup can retry a failure.
        cancellationToken.ThrowIfCancellationRequested();
        var closed = await io.HostAdapter.CloseDescriptorWithoutCancellationAsync(descriptor)
            .ConfigureAwait(false);
        if (closed.DescriptorRetired) _ownedDescriptor = null;
        try
        {
            if (!closed.Success)
                throw AwkHostBoundaryPolicy.CloseFailure(descriptor, closed, state.Options.ExposeHostErrorDetails, state.Options.MaxHostErrorBytes, _ownedInputDisplay);
        }
        finally { if (closed.DescriptorRetired) RetireOwnedDescriptor(); }
    }

    private void RetireOwnedDescriptor()
    {
        if (!_ownedInputDisplay.IsEmpty)
            _inputDisplayMemory.Release(AwkRuntimeMemory.EstimateInputDisplayBytes(_ownedInputDisplay.Length));
        _ownedInputDisplay = default;
        _ownedDescriptor = null;
    }

    private enum MainInputReadProhibition
    {
        None,
        BeginFile,
        EndFile
    }

    private enum EndFileAction
    {
        Skip,
        Execute
    }

}
