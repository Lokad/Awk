using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>
/// Owns the lazy, ordered main-input stream shared by ordinary record processing and plain
/// <c>getline</c>. Command-line assignments and input selection take effect only when the stream
/// reaches them, so <c>BEGIN</c> can still change <c>ARGV</c> before the first read.
/// </summary>
internal sealed class AwkInputScheduler(
    IAwkHost host,
    AwkInvocation invocation,
    AwkRuntimeActionExecutor actionExecutor,
    AwkRuntimeState state,
    AwkIo io,
    CancellationToken cancellationToken,
    AwkPath? accessiblePathRoot = null,
    bool allowDynamicMainInputPaths = true,
    bool exposeHostErrorDetails = true,
    int maxHostErrorBytes = 4 << 10) : IAwkInputRecordReader
{
    private static readonly AwkText StandardInputFileName = AwkText.FromSourceLiteralString("-");
    private OwnedInputDescriptor _ownedDescriptor;
    private ReadOnlyMemory<byte> _ownedInputDisplay;
    private readonly AwkMemoryLease _inputDisplayMemory = state.MemoryAccounting.CreateTransientLease();
    private readonly AwkRecordProcessor _recordProcessor = new(state);
    private OpenMainInput? _currentInput;
    private long _nextArgvIndex = 1;
    private bool _selectedInputSource;
    private bool _usedStandardInputFallback;
    private MainInputReadProhibition _mainInputReadProhibition;

    public void PreserveStandardInput()
    {
        if (_currentInput is { } input && !_ownedDescriptor.TryGetValue(out _))
            input.Cursor.PreserveBorrowedInput();
    }

    public async ValueTask<AwkInputRecordResult> ReadAsync()
    {
        if (_mainInputReadProhibition == MainInputReadProhibition.BeginFile)
            throw new AwkRuntimeException(AwkMessages.ErrorMainInputGetlineInBeginFile);

        if (_mainInputReadProhibition == MainInputReadProhibition.EndFile)
            throw new AwkRuntimeException(AwkMessages.ErrorMainInputGetlineInEndFile);

        while (!state.ExitRequested)
        {
            if (_currentInput is null && !await TryOpenNextInputAsync().ConfigureAwait(false))
                return AwkInputRecordResult.EndOfFile;

            var input = _currentInput ??
                throw new InvalidOperationException("Main input was not initialized after a successful open.");
            var record = await input.Cursor.ReadAsync().ConfigureAwait(false);
            if (record.HasRecord)
            {
                state.Checkpoint();
                return record;
            }

            if (record.ClearsRecordTerminator)
                state.SetRecordTerminator(AwkRecordTerminator.Empty);

            var endFileFlow = await CompleteCurrentInputAsync(EndFileAction.Execute).ConfigureAwait(false);
            if (endFileFlow == AwkStatementFlow.ExitProgram)
                return AwkInputRecordResult.EndOfFile;
        }

        return AwkInputRecordResult.EndOfFile;

        async ValueTask<bool> TryOpenNextInputAsync()
        {
            while (_nextArgvIndex < state.MainInputArgumentCount)
            {
                state.Checkpoint();
                var argumentIndex = new AwkArgvIndex((int)_nextArgvIndex++);
                if (!state.TryGetArgumentVectorByteText(argumentIndex, out var argumentText) ||
                    argumentText.ByteLength == 0)
                {
                    continue;
                }

                var inputArgument = GetOriginalInputArgument(argumentIndex);
                var isUnchangedFile = inputArgument is AwkInputArgument.FileArgument originalFile &&
                    argumentText.AsSpan(state.CancellationCheckpoint).SequenceEqual(originalFile.Path.Display.Span);
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
                if (!allowDynamicMainInputPaths && !IsOriginalInputArgument(inputArgument, argumentText))
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

                var path = ResolveRuntimePath(inputArgument, argumentText);
                AwkHostPathPolicy.ThrowIfDenied(path.Absolute, accessiblePathRoot);
                var fileName = argumentText.AsSpan(state.CancellationCheckpoint).SequenceEqual(path.Display.Span)
                    ? argumentText
                    : AwkText.CopyTrustedBytes(path.Display.Span, state.Counters, state.MemoryAccounting, state.CancellationCheckpoint);
                state.BeginInputFile(fileName, argumentIndex, actionExecutor.HasBeginFileActions);
                var displayBytes = AwkRuntimeMemory.CheckedAdd(32, path.Display.Length);
                using var pendingDisplay = state.MemoryAccounting.ReserveTransient(displayBytes);
                state.Resources.RecordHostCall();
                var opened = await host.OpenFileAsync(path.Absolute, AwkFileOpenMode.Read, cancellationToken)
                    .ConfigureAwait(false);
                if (!opened.Success)
                {
                    if (await HandleOpenFailureAsync(path, opened).ConfigureAwait(false))
                        return false;

                    continue;
                }

                _ownedDescriptor = OwnedInputDescriptor.Owned(opened.FileDescriptor);
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
                if (planIndex >= invocation.InputPlan.InputArguments.Count)
                    return null;

                var original = invocation.InputPlan.InputArguments[planIndex];
                return original.ArgvIndex == index ? original : null;
            }

            string ResolveArgumentString(AwkText text)
            {
                state.Counters.RecordStringInteropFallback();
                return text.AsString(state.Counters, state.CancellationCheckpoint);
            }

            bool IsOriginalInputArgument(AwkInputArgument? inputArgument, AwkText text) =>
                inputArgument switch
                {
                    AwkInputArgument.StandardInputArgument => AwkSpecialPathPolicy.IsStandardInput(text),
                    AwkInputArgument.FileArgument file => text.AsSpan(state.CancellationCheckpoint).SequenceEqual(file.Path.Display.Span),
                    _ => false
                };

            AwkResolvedPath ResolveRuntimePath(
                AwkInputArgument? inputArgument,
                AwkText argumentText) =>
                inputArgument is AwkInputArgument.FileArgument file &&
                argumentText.AsSpan(state.CancellationCheckpoint).SequenceEqual(file.Path.Display.Span)
                    ? file.Path
                    : AwkPathResolution.ResolveRuntimeArgument(
                        ResolveArgumentString(argumentText),
                        invocation.RuntimeArgumentsPlan.CurrentDirectory);

            async ValueTask<bool> TryActivateInputAsync(
                AwkRecordCursor cursor)
            {
                _currentInput = new OpenMainInput(cursor);
                var beginFileFlow = await ExecuteBeginFileActionsAsync().ConfigureAwait(false);
                if (beginFileFlow == AwkStatementFlow.Continue)
                    return true;

                await CompleteCurrentInputAsync(
                        beginFileFlow == AwkStatementFlow.NextFile ? EndFileAction.Execute : EndFileAction.Skip)
                    .ConfigureAwait(false);
                return false;
            }

            async ValueTask<bool> HandleOpenFailureAsync(AwkResolvedPath path, AwkOpenedFile opened)
            {
                if (!actionExecutor.HasBeginFileActions)
                    throw CreateOpenFailure(path, opened);

                state.SetErrno(opened.Failed
                    ? AwkErrno.FromHostMessage(ResolveHostError(opened.Error))
                    : AwkErrno.NoSuchFileOrDirectory);
                var flow = await ExecuteBeginFileActionsAsync().ConfigureAwait(false);
                if (flow == AwkStatementFlow.ExitProgram)
                    return true;

                if (flow != AwkStatementFlow.NextFile)
                    throw CreateOpenFailure(path, opened);

                return false;

                AwkRuntimeException CreateOpenFailure(AwkResolvedPath path, AwkOpenedFile opened) =>
                    new(
                        opened.Failed
                            ? AwkMessages.FormatPathError(path.Display, ResolveHostError(opened.Error))
                            : AwkMessages.FormatPathError(path.Display),
                        AwkDiagnosticId.HostIoFailure);
            }

            AwkRecordCursor CreateRecordCursor(AwkFileDescriptor descriptor)
            {
                var descriptorReader = new AwkRecordDescriptorReader(
                    host,
                    cancellationToken,
                    state.Resources,
                    exposeHostErrorDetails,
                    maxHostErrorBytes);
                var cursor = new AwkRecordCursor(
                    state,
                    descriptorReader,
                    descriptorReader.CreateLineReader(descriptor));
                if (descriptor == invocation.Stdio.StdIn)
                    io.PreserveStandardInputIfRedirected(cursor);
                return cursor;
            }
        }
    }

    public async Task ProcessRemainingInputAsync()
    {
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync =
            record => actionExecutor.ProcessRecordAsync(state, io, record);
        while (!state.ExitRequested)
        {
            var input = await ReadAsync().ConfigureAwait(false);
            if (!input.HasRecord)
                return;

            var flow = input.FieldSplittingMode == AwkRecordFieldSplittingMode.Paragraph
                ? await _recordProcessor.ProcessParagraphAsync(
                        input.RecordText,
                        input.Terminator,
                        processRecordAsync)
                    .ConfigureAwait(false)
                : await _recordProcessor.ProcessAsync(
                        input.RecordText,
                        input.Terminator,
                        processRecordAsync)
                    .ConfigureAwait(false);
            if (flow == AwkStatementFlow.ExitProgram)
            {
                await CompleteCurrentInputAsync(EndFileAction.Skip).ConfigureAwait(false);
                return;
            }

            if (flow == AwkStatementFlow.NextFile)
            {
                var endFileFlow = await CompleteCurrentInputAsync(EndFileAction.Execute).ConfigureAwait(false);
                if (endFileFlow == AwkStatementFlow.ExitProgram)
                    return;
            }
        }
    }

    public async Task CloseRemainingDescriptorsAsync()
    {
        await CompleteCurrentInputAsync(EndFileAction.Skip).ConfigureAwait(false);
        await CloseOwnedDescriptorAsync().ConfigureAwait(false);

        io.ClearMainInputReader();
        _inputDisplayMemory.Dispose();
    }

    public async Task CloseRemainingDescriptorsForCleanupAsync()
    {
        _currentInput?.Cursor.Dispose();
        _currentInput = null;
        if (_ownedDescriptor.TryGetValue(out var descriptor))
        {
            try
            {
                state.Resources.RecordCleanupHostCall();
                await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
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
        input.Cursor.Dispose();
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
        if (!_ownedDescriptor.TryGetValue(out var descriptor))
            return;

        // Keep ownership through pending or failed closes; cleanup can retry a failure.
        state.Resources.RecordHostCall();
        var closed = await host.CloseDescriptorAsync(descriptor, CancellationToken.None)
            .ConfigureAwait(false);
        if (closed.DescriptorRetired) _ownedDescriptor = OwnedInputDescriptor.None;
        try
        {
            if (!closed.Success)
                throw AwkHostBoundaryPolicy.CloseFailure(descriptor, closed, exposeHostErrorDetails, maxHostErrorBytes, _ownedInputDisplay);
        }
        finally { if (closed.DescriptorRetired) RetireOwnedDescriptor(); }
    }

    private void RetireOwnedDescriptor()
    {
        if (!_ownedInputDisplay.IsEmpty)
            _inputDisplayMemory.Release(AwkRuntimeMemory.CheckedAdd(32, _ownedInputDisplay.Length));
        _ownedInputDisplay = default;
        _ownedDescriptor = OwnedInputDescriptor.None;
    }

    private static string ResolveHostErrorCore(
        AwkOperationError error,
        bool exposeDetails,
        int maxErrorBytes) =>
        AwkHostBoundaryPolicy.ResolveError(error, exposeDetails, maxErrorBytes);

    private string ResolveHostError(AwkOperationError error) =>
        ResolveHostErrorCore(error, exposeHostErrorDetails, maxHostErrorBytes);

    private readonly struct OwnedInputDescriptor
    {
        private readonly AwkFileDescriptor _value;
        private readonly OwnedInputDescriptorKind _kind;

        private OwnedInputDescriptor(AwkFileDescriptor value)
        {
            _value = value;
            _kind = OwnedInputDescriptorKind.Owned;
        }

        public static OwnedInputDescriptor None => default;

        public static OwnedInputDescriptor Owned(AwkFileDescriptor descriptor) => new(descriptor);

        public bool TryGetValue(out AwkFileDescriptor descriptor)
        {
            descriptor = _value;
            return _kind == OwnedInputDescriptorKind.Owned;
        }
    }

    private sealed class OpenMainInput(AwkRecordCursor cursor)
    {
        public AwkRecordCursor Cursor { get; } = cursor;

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

    private enum OwnedInputDescriptorKind
    {
        None,
        Owned
    }
}
