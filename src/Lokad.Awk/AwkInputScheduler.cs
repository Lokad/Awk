using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>
/// Owns the lazy, ordered main-input stream shared by ordinary record processing and plain
/// <c>getline</c>. Command-line assignments and paths are resolved only when the stream reaches
/// them, so <c>BEGIN</c> can still change <c>ARGV</c> before the first read.
/// </summary>
internal sealed class AwkInputScheduler(
    IAwkHost host,
    AwkInvocation invocation,
    AwkRuntimeActionExecutor actionExecutor,
    AwkRuntimeState state,
    AwkIo io,
    CancellationToken cancellationToken,
    AwkPath? accessiblePathRoot = null,
    TimeSpan? cleanupTimeout = null,
    bool allowDynamicMainInputPaths = true,
    TimeSpan? hostOperationTimeout = null,
    bool exposeHostErrorDetails = true,
    int maxHostErrorBytes = 4 << 10,
    AwkCleanupDeadline? cleanupDeadline = null) : IAwkInputRecordReader
{
    private static readonly AwkText StandardInputFileName = AwkText.FromSourceLiteralString("-");
    private readonly TimeSpan _hostOperationTimeout = hostOperationTimeout ?? TimeSpan.FromMinutes(5);
    private readonly TimeSpan _closeOperationTimeout =
        Min(hostOperationTimeout ?? TimeSpan.FromMinutes(5), cleanupTimeout ?? TimeSpan.FromSeconds(30));
    private readonly AwkCleanupDeadline _cleanupDeadline =
        cleanupDeadline ?? new AwkCleanupDeadline(cleanupTimeout ?? TimeSpan.FromSeconds(30));
    private readonly HashSet<AwkFileDescriptor> _ownedDescriptors = [];
    private readonly AwkRecordProcessor _recordProcessor = new(state);
    private OpenMainInput? _currentInput;
    private long _nextArgvIndex = 1;
    private bool _selectedInputSource;
    private bool _usedStandardInputFallback;
    private MainInputReadProhibition _mainInputReadProhibition;

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
                state.Resources.RecordInputRecord();
                state.RecordExecutionStep();
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
                state.RecordExecutionStep();
                var argumentIndex = new AwkArgvIndex((int)_nextArgvIndex++);
                if (!state.TryGetArgumentVectorByteText(argumentIndex, out var argumentText) ||
                    argumentText.ByteLength == 0)
                {
                    continue;
                }

                var inputArgument = GetOriginalInputArgument(argumentIndex);
                var isUnchangedFile = inputArgument is AwkInputArgument.FileArgument originalFile &&
                    argumentText.AsSpan().SequenceEqual(originalFile.Path.Display.Span);
                if (!isUnchangedFile && argumentText.AsSpan().Contains((byte)'=') &&
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

                if (argumentText.EqualsAscii("-") || argumentText.EqualsAscii("/dev/stdin"))
                {
                    state.BeginInputFile(argumentText, argumentIndex, actionExecutor.HasBeginFileActions);
                    if (await TryActivateInputAsync(
                            CreateRecordCursor(invocation.Stdio.StdIn),
                            OwnedInputDescriptor.None)
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
                var fileName = argumentText.AsSpan().SequenceEqual(path.Display.Span)
                    ? argumentText
                    : AwkText.CopyTrustedBytes(path.Display.Span, state.Counters, state.MemoryGovernor);
                state.BeginInputFile(fileName, argumentIndex, actionExecutor.HasBeginFileActions);
                state.Resources.RecordHostCall();
                var opened = await AwkHostBoundaryPolicy.AwaitAsync(
                        host.OpenFileAsync(path.Absolute, AwkFileOpenMode.Read, cancellationToken),
                        _hostOperationTimeout,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!opened.Success)
                {
                    if (await HandleOpenFailureAsync(path, opened).ConfigureAwait(false))
                        return false;

                    continue;
                }

                _ownedDescriptors.Add(opened.FileDescriptor);
                if (await TryActivateInputAsync(
                        CreateRecordCursor(opened.FileDescriptor),
                        OwnedInputDescriptor.Owned(opened.FileDescriptor))
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
                    CreateRecordCursor(invocation.Stdio.StdIn),
                    OwnedInputDescriptor.None)
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
                return text.AsString(state.Counters);
            }

            static bool IsOriginalInputArgument(AwkInputArgument? inputArgument, AwkText text) =>
                inputArgument switch
                {
                    AwkInputArgument.StandardInputArgument => text.EqualsAscii("-") || text.EqualsAscii("/dev/stdin"),
                    AwkInputArgument.FileArgument file => text.AsSpan().SequenceEqual(file.Path.Display.Span),
                    _ => false
                };

            AwkResolvedPath ResolveRuntimePath(
                AwkInputArgument? inputArgument,
                AwkText argumentText) =>
                inputArgument is AwkInputArgument.FileArgument file &&
                argumentText.AsSpan().SequenceEqual(file.Path.Display.Span)
                    ? file.Path
                    : AwkPathResolution.ResolveRuntimeArgument(
                        ResolveArgumentString(argumentText),
                        invocation.RuntimeArgumentsPlan.CurrentDirectory);

            async ValueTask<bool> TryActivateInputAsync(
                AwkRecordCursor cursor,
                OwnedInputDescriptor descriptor)
            {
                _currentInput = new OpenMainInput(cursor, descriptor);
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
                    _hostOperationTimeout,
                    exposeHostErrorDetails,
                    maxHostErrorBytes);
                return new AwkRecordCursor(
                    state,
                    descriptorReader,
                    descriptorReader.CreateLineReader(descriptor));
            }
        }
    }

    public async Task ProcessRemainingInputAsync()
    {
        while (!state.ExitRequested)
        {
            var input = await ReadAsync().ConfigureAwait(false);
            if (!input.HasRecord)
                return;

            var flow = input.FieldSplittingMode == AwkRecordFieldSplittingMode.Paragraph
                ? await _recordProcessor.ProcessParagraphAsync(
                        input.RecordText,
                        input.Terminator,
                        record => actionExecutor.ProcessRecordAsync(state, io, record))
                    .ConfigureAwait(false)
                : await _recordProcessor.ProcessAsync(
                        input.RecordText,
                        input.Terminator,
                        record => actionExecutor.ProcessRecordAsync(state, io, record))
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
        var descriptors = SnapshotOwnedDescriptors(OwnedDescriptorRetention.Retain);
        for (var index = 0; index < descriptors.Length; index++)
            await CloseOwnedDescriptorAsync(descriptors[index]).ConfigureAwait(false);

        io.ClearMainInputReader();
    }

    public async Task CloseRemainingDescriptorsForCleanupAsync()
    {
        _currentInput?.Cursor.Dispose();
        _currentInput = null;
        var descriptors = SnapshotOwnedDescriptors(OwnedDescriptorRetention.Clear);
        for (var index = 0; index < descriptors.Length; index++)
        {
            try
            {
                state.Resources.RecordCleanupHostCall();
                await _cleanupDeadline.AwaitAsync(
                    host.CloseDescriptorAsync(descriptors[index], CancellationToken.None)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cleanup must preserve the exception that interrupted execution.
            }
        }

        io.ClearMainInputReader();
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
        if (input.Descriptor.TryGetValue(out var descriptor))
            await CloseOwnedDescriptorAsync(descriptor).ConfigureAwait(false);

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

    private AwkFileDescriptor[] SnapshotOwnedDescriptors(OwnedDescriptorRetention retention)
    {
        var descriptors = new AwkFileDescriptor[_ownedDescriptors.Count];
        _ownedDescriptors.CopyTo(descriptors);
        if (retention == OwnedDescriptorRetention.Clear)
            _ownedDescriptors.Clear();

        return descriptors;
    }

    private async Task CloseOwnedDescriptorAsync(AwkFileDescriptor descriptor)
    {
        if (!_ownedDescriptors.Remove(descriptor))
            return;

        try
        {
            state.Resources.RecordHostCall();
            var closed = await AwkHostBoundaryPolicy.AwaitAsync(
                    host.CloseDescriptorAsync(descriptor, CancellationToken.None),
                    _closeOperationTimeout,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (closed != AwkOperationStatus.Success)
            {
                throw new AwkRuntimeException(
                    AwkMessages.FormatDescriptorCloseError(descriptor),
                    AwkDiagnosticId.HostIoFailure);
            }
        }
        catch
        {
            _ownedDescriptors.Add(descriptor);
            throw;
        }
    }

    private static string ResolveHostErrorCore(
        AwkOperationError error,
        bool exposeDetails,
        int maxErrorBytes) =>
        AwkHostBoundaryPolicy.ResolveError(error, exposeDetails, maxErrorBytes);

    private string ResolveHostError(AwkOperationError error) =>
        ResolveHostErrorCore(error, exposeHostErrorDetails, maxHostErrorBytes);

    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left <= right ? left : right;

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

    private sealed class OpenMainInput(
        AwkRecordCursor cursor,
        OwnedInputDescriptor descriptor)
    {
        public AwkRecordCursor Cursor { get; } = cursor;

        public OwnedInputDescriptor Descriptor { get; } = descriptor;
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

    private enum OwnedDescriptorRetention
    {
        Retain,
        Clear
    }

    private enum OwnedInputDescriptorKind
    {
        None,
        Owned
    }
}
