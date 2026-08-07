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
    CancellationToken cancellationToken) : AwkInputRecordReader
{
    private readonly HashSet<AwkFileDescriptor> _ownedDescriptors = [];
    private readonly AwkRecordProcessor _recordProcessor = new(state);
    private OpenMainInput? _currentInput;
    private int _nextInputPlanIndex;
    private bool _selectedInputSource;
    private bool _usedStandardInputFallback;
    private MainInputReadProhibition _mainInputReadProhibition;

    public async ValueTask<AwkInputRecordResult> ReadAsync()
    {
        ThrowIfMainInputReadIsProhibited();

        while (!state.ExitRequested)
        {
            if (_currentInput is null && !await TryOpenNextInputAsync().ConfigureAwait(false))
                return AwkInputRecordResult.EndOfFile;

            var input = _currentInput ??
                throw new InvalidOperationException("Main input was not initialized after a successful open.");
            var record = await input.Cursor.ReadAsync().ConfigureAwait(false);
            if (record.HasRecord)
                return record;

            var endFileFlow = await CompleteCurrentInputAsync(executeEndFile: true).ConfigureAwait(false);
            if (endFileFlow == AwkStatementFlow.ExitProgram)
                return AwkInputRecordResult.EndOfFile;
        }

        return AwkInputRecordResult.EndOfFile;
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
                await CompleteCurrentInputAsync(executeEndFile: false).ConfigureAwait(false);
                return;
            }

            if (flow == AwkStatementFlow.NextFile)
            {
                var endFileFlow = await CompleteCurrentInputAsync(executeEndFile: true).ConfigureAwait(false);
                if (endFileFlow == AwkStatementFlow.ExitProgram)
                    return;
            }
        }
    }

    public async Task CloseRemainingDescriptorsAsync()
    {
        await CompleteCurrentInputAsync(executeEndFile: false).ConfigureAwait(false);
        var descriptors = SnapshotOwnedDescriptors(clear: false);
        for (var index = 0; index < descriptors.Length; index++)
            await CloseOwnedDescriptorAsync(descriptors[index]).ConfigureAwait(false);

        io.ClearMainInputReader();
    }

    public async Task CloseRemainingDescriptorsForCleanupAsync()
    {
        DisposeCurrentInput();
        var descriptors = SnapshotOwnedDescriptors(clear: true);
        for (var index = 0; index < descriptors.Length; index++)
        {
            try
            {
                state.Counters.RecordHostCall();
                _ = await host.CloseDescriptorAsync(descriptors[index], CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cleanup must preserve the exception that interrupted execution.
            }
        }

        io.ClearMainInputReader();
    }

    private async ValueTask<bool> TryOpenNextInputAsync()
    {
        while (_nextInputPlanIndex < invocation.InputPlan.InputArguments.Count)
        {
            var inputArgument = invocation.InputPlan.InputArguments[_nextInputPlanIndex++];
            if (IsDeletedRuntimeArgument(inputArgument))
                continue;

            if (inputArgument is AwkInputArgument.VariableAssignmentArgument assignmentArgument)
            {
                ApplyVariableAssignment(assignmentArgument.VariableAssignment);
                continue;
            }

            _selectedInputSource = true;
            var runtimeArgument = GetRuntimeArgumentValue(inputArgument);
            if (runtimeArgument.IsStandardInput)
            {
                state.BeginInputFile(runtimeArgument.Value, inputArgument.ArgvIndex);
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

            var path = ResolveRuntimePath(inputArgument, runtimeArgument);
            state.BeginInputFile(AwkUtf8Codec.Decode(path.Display), inputArgument.ArgvIndex);
            state.Resources.RecordHostCall();
            var opened = await host
                .OpenFileAsync(path.Absolute, AwkFileOpenMode.Read, cancellationToken)
                .ConfigureAwait(false);
            if (!opened.Success)
            {
                if (await HandleOpenFailureAsync(path, opened).ConfigureAwait(false))
                    return false;

                continue;
            }

            TrackOwnedDescriptor(opened.FileDescriptor);
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
        state.BeginInputFile("-", new AwkArgvIndex(0));
        return await TryActivateInputAsync(
                CreateRecordCursor(invocation.Stdio.StdIn),
                OwnedInputDescriptor.None)
            .ConfigureAwait(false);
    }

    private async ValueTask<bool> TryActivateInputAsync(
        AwkRecordCursor cursor,
        OwnedInputDescriptor descriptor)
    {
        _currentInput = new OpenMainInput(cursor, descriptor);
        var beginFileFlow = await ExecuteBeginFileActionsAsync().ConfigureAwait(false);
        if (beginFileFlow == AwkStatementFlow.Continue)
            return true;

        await CompleteCurrentInputAsync(executeEndFile: false).ConfigureAwait(false);
        return false;
    }

    private async ValueTask<bool> HandleOpenFailureAsync(AwkResolvedPath path, AwkOpenedFile opened)
    {
        if (!actionExecutor.HasBeginFileActions)
            throw CreateOpenFailure(path, opened);

        state.SetErrno(opened.Failed
            ? AwkErrno.FromHostMessage(opened.Error.Message)
            : AwkErrno.NoSuchFileOrDirectory);
        var flow = await ExecuteBeginFileActionsAsync().ConfigureAwait(false);
        if (flow == AwkStatementFlow.ExitProgram)
            return true;

        if (flow != AwkStatementFlow.NextFile)
            throw CreateOpenFailure(path, opened);

        return false;
    }

    private async ValueTask<AwkStatementFlow> CompleteCurrentInputAsync(bool executeEndFile)
    {
        if (_currentInput is null)
            return AwkStatementFlow.Continue;

        var flow = executeEndFile
            ? await ExecuteEndFileActionsAsync().ConfigureAwait(false)
            : AwkStatementFlow.Continue;
        var input = _currentInput;
        _currentInput = null;
        input.Cursor.Dispose();
        if (input.Descriptor.HasValue)
            await CloseOwnedDescriptorAsync(input.Descriptor.Value).ConfigureAwait(false);

        return flow;
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

    private async ValueTask<AwkStatementFlow> ExecuteEndFileActionsAsync()
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

    private void ThrowIfMainInputReadIsProhibited()
    {
        if (_mainInputReadProhibition == MainInputReadProhibition.BeginFile)
            throw new AwkRuntimeException(AwkMessages.ErrorMainInputGetlineInBeginFile);

        if (_mainInputReadProhibition == MainInputReadProhibition.EndFile)
            throw new AwkRuntimeException(AwkMessages.ErrorMainInputGetlineInEndFile);
    }

    private void DisposeCurrentInput()
    {
        _currentInput?.Cursor.Dispose();
        _currentInput = null;
    }

    private AwkFileDescriptor[] SnapshotOwnedDescriptors(bool clear)
    {
        var descriptors = new AwkFileDescriptor[_ownedDescriptors.Count];
        _ownedDescriptors.CopyTo(descriptors);
        if (clear)
            _ownedDescriptors.Clear();

        return descriptors;
    }

    private void TrackOwnedDescriptor(AwkFileDescriptor descriptor) =>
        _ownedDescriptors.Add(descriptor);

    private AwkRecordCursor CreateRecordCursor(AwkFileDescriptor descriptor)
    {
        var descriptorReader = new AwkRecordDescriptorReader(
            host,
            cancellationToken,
            state.Counters,
            state.Resources);
        return new AwkRecordCursor(
            state,
            descriptorReader,
            descriptorReader.CreateLineReader(descriptor));
    }

    private async Task CloseOwnedDescriptorAsync(AwkFileDescriptor descriptor)
    {
        if (!_ownedDescriptors.Remove(descriptor))
            return;

        try
        {
            state.Resources.RecordHostCall();
            var closed = await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
            if (!closed)
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

    private void ApplyVariableAssignment(AwkVariableAssignment assignment) =>
        state.AssignVariableFromExternalText(assignment);

    private bool IsDeletedRuntimeArgument(AwkInputArgument inputArgument)
    {
        var index = inputArgument.ArgvIndex;
        return !state.ContainsArgumentVectorValue(index) ||
            state.GetArgumentVectorByteText(index).ByteLength == 0;
    }

    private RuntimeArgumentValue GetRuntimeArgumentValue(AwkInputArgument inputArgument)
    {
        var text = state.GetArgumentVectorByteText(inputArgument.ArgvIndex);

        if (inputArgument is AwkInputArgument.StandardInputArgument)
        {
            if (text.EqualsAscii("-"))
                return new RuntimeArgumentValue(text, "-");

            if (text.EqualsAscii("/dev/stdin"))
                return new RuntimeArgumentValue(text, "/dev/stdin");
        }

        if (inputArgument is AwkInputArgument.FileArgument file &&
            text.AsSpan().SequenceEqual(file.Path.Display.Span))
        {
            return new RuntimeArgumentValue(text, AwkUtf8Codec.Decode(file.Path.Display));
        }

        state.Counters.RecordStringInteropFallback();
        return new RuntimeArgumentValue(text, text.AsString(state.Counters));
    }

    private AwkResolvedPath ResolveRuntimePath(
        AwkInputArgument inputArgument,
        RuntimeArgumentValue runtimeArgument) =>
        inputArgument is AwkInputArgument.FileArgument file &&
        runtimeArgument.Text.AsSpan().SequenceEqual(file.Path.Display.Span)
            ? file.Path
            : AwkPathResolution.ResolveArgument(
                runtimeArgument.Value,
                invocation.RuntimeArgumentsPlan.CurrentDirectory);

    private static AwkRuntimeException CreateOpenFailure(AwkResolvedPath path, AwkOpenedFile opened) =>
        new(
            opened.Failed
                ? AwkMessages.FormatPathError(path.Display, opened.Error.Message)
                : AwkMessages.FormatPathError(path.Display),
            AwkDiagnosticId.HostIoFailure);

    private readonly record struct RuntimeArgumentValue(AwkText Text, string Value)
    {
        public bool IsStandardInput => Value is "-" or "/dev/stdin";
    }

    private readonly record struct OwnedInputDescriptor(bool HasValue, AwkFileDescriptor Value)
    {
        public static OwnedInputDescriptor None => default;

        public static OwnedInputDescriptor Owned(AwkFileDescriptor descriptor) => new(true, descriptor);
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
}
