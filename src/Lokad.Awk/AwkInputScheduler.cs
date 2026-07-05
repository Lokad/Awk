using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkInputScheduler(
    IAwkHost host,
    AwkInvocation invocation,
    AwkRuntimeActionExecutor actionExecutor,
    AwkRuntimeState state,
    AwkIo io,
    CancellationToken cancellationToken)
{
    public bool HasExplicitInputSource
    {
        get
        {
            for (var index = 0; index < invocation.InputPlan.InputArguments.Count; index++)
            {
                if (invocation.InputPlan.InputArguments[index] is
                    AwkInputArgument.FileArgument or AwkInputArgument.StandardInputArgument)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public async Task<MainInputPreparation> TryPrepareFirstMainInputAsync()
    {
        if (!HasExplicitInputSource)
        {
            var standardInput = new PreparedMainInput(
                host,
                actionExecutor,
                state,
                io,
                cancellationToken,
                AwkInputPlanIndex.None,
                AwkText.FromSourceLiteralString("-"),
                "-",
                new AwkArgvIndex(0),
                invocation.Stdio.StdIn,
                new AsyncLineReader(
                    host,
                    invocation.Stdio.StdIn,
                    cancellationToken,
                    AwkIo.InputReadBatchLineCount,
                    state.Counters,
                    state.Resources),
                closeDescriptor: false);
            io.BindMainInputReader(
                standardInput.Reader,
                async () => await standardInput.EnsureBeginFileAsync().ConfigureAwait(false));
            return MainInputPreparation.FromPrepared(standardInput);
        }

        for (var index = 0; index < invocation.InputPlan.InputArguments.Count; index++)
        {
            var inputArgument = invocation.InputPlan.InputArguments[index];
            if (inputArgument is AwkInputArgument.VariableAssignmentArgument)
                continue;

            var runtimeArgument = GetRuntimeArgumentValue(inputArgument);
            if (runtimeArgument.IsStandardInput)
            {
                var standardInput = new PreparedMainInput(
                    host,
                    actionExecutor,
                    state,
                    io,
                    cancellationToken,
                    AwkInputPlanIndex.FromZeroBased(index),
                    runtimeArgument.Text,
                    runtimeArgument.Value,
                    inputArgument.ArgvIndex,
                    invocation.Stdio.StdIn,
                    new AsyncLineReader(
                        host,
                        invocation.Stdio.StdIn,
                        cancellationToken,
                        AwkIo.InputReadBatchLineCount,
                        state.Counters,
                        state.Resources),
                    closeDescriptor: false);
                io.BindMainInputReader(
                    standardInput.Reader,
                    async () => await standardInput.EnsureBeginFileAsync().ConfigureAwait(false));
                return MainInputPreparation.FromPrepared(standardInput);
            }

            var path = ResolveRuntimePath(inputArgument, runtimeArgument);
            state.Resources.RecordHostCall();
            var opened = await host.OpenFileAsync(path.Absolute, AwkFileOpenMode.Read, cancellationToken)
                .ConfigureAwait(false);
            if (!opened.Success)
                return MainInputPreparation.DeferredFailureResult;

            var prepared = new PreparedMainInput(
                host,
                actionExecutor,
                state,
                io,
                cancellationToken,
                AwkInputPlanIndex.FromZeroBased(index),
                runtimeArgument.Text,
                AwkUtf8Codec.Decode(path.Display),
                inputArgument.ArgvIndex,
                opened.FileDescriptor,
                new AsyncLineReader(
                    host,
                    opened.FileDescriptor,
                    cancellationToken,
                    AwkIo.InputReadBatchLineCount,
                    state.Counters,
                    state.Resources),
                closeDescriptor: true);
            io.BindMainInputReader(
                prepared.Reader,
                async () => await prepared.EnsureBeginFileAsync().ConfigureAwait(false));
            return MainInputPreparation.FromPrepared(prepared);
        }

        return MainInputPreparation.MissingResult;
    }

    public async Task ProcessDefaultInputAsync(MainInputPreparation preparation)
    {
        ApplyInputAssignments();
        if (preparation is MainInputPreparation.Prepared prepared &&
            state.ResolveRecordSeparator() == "\n")
        {
            await ProcessPreparedInputAsync(prepared.Input).ConfigureAwait(false);
            return;
        }

        if (preparation is MainInputPreparation.Prepared preparedToClose)
            await preparedToClose.Input.CloseAsync().ConfigureAwait(false);

        state.BeginInputFile("-", 0);
        await ProcessInputDescriptorAsync(invocation.Stdio.StdIn).ConfigureAwait(false);
    }

    public async Task ProcessExplicitInputsAsync(MainInputPreparation preparation)
    {
        for (var inputPlanIndex = 0; inputPlanIndex < invocation.InputPlan.InputArguments.Count; inputPlanIndex++)
        {
            var inputArgument = invocation.InputPlan.InputArguments[inputPlanIndex];
            if (IsDeletedRuntimeArgument(inputArgument))
            {
                if (preparation is MainInputPreparation.Prepared prepared &&
                    prepared.Input.IsForInputPlanIndex(inputPlanIndex))
                {
                    await prepared.Input.CloseAsync().ConfigureAwait(false);
                }

                continue;
            }

            if (inputArgument is AwkInputArgument.VariableAssignmentArgument assignmentArgument)
            {
                ApplyVariableAssignment(assignmentArgument.VariableAssignment);
                continue;
            }

            if (preparation is MainInputPreparation.Prepared preparedForArgument &&
                preparedForArgument.Input.IsForInputPlanIndex(inputPlanIndex) &&
                state.ResolveRecordSeparator() == "\n" &&
                preparedForArgument.Input.CanUseCurrentArgument())
            {
                var preparedFlow = await ProcessPreparedInputAsync(preparedForArgument.Input).ConfigureAwait(false);
                if (preparedFlow == AwkStatementFlow.ExitProgram)
                    break;

                continue;
            }

            if (preparation is MainInputPreparation.Prepared preparedToClose &&
                preparedToClose.Input.IsForInputPlanIndex(inputPlanIndex))
            {
                await preparedToClose.Input.CloseAsync().ConfigureAwait(false);
            }

            var runtimeArgument = GetRuntimeArgumentValue(inputArgument);
            if (runtimeArgument.IsStandardInput)
            {
                state.BeginInputFile(runtimeArgument.Value, inputArgument.ArgvIndex);
                var standardInputFlow = await ProcessInputDescriptorAsync(invocation.Stdio.StdIn).ConfigureAwait(false);
                if (standardInputFlow == AwkStatementFlow.ExitProgram)
                    break;

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
                if (actionExecutor.HasBeginFileActions)
                {
                    state.SetErrno(opened.Failed
                        ? AwkErrno.FromHostMessage(opened.Error.Message)
                        : AwkErrno.NoSuchFileOrDirectory);
                    var missingFileFlow = await actionExecutor.ExecuteBeginFileActionsAsync(state, io)
                        .ConfigureAwait(false);
                    if (missingFileFlow == AwkStatementFlow.ExitProgram)
                        break;

                    if (missingFileFlow == AwkStatementFlow.NextFile)
                        continue;

                    throw CreateOpenFailure(path, opened);
                }

                throw CreateOpenFailure(path, opened);
            }

            var inputFlow = await ProcessInputDescriptorAsync(opened.FileDescriptor).ConfigureAwait(false);
            state.Resources.RecordHostCall();
            await host.CloseDescriptorAsync(opened.FileDescriptor, cancellationToken).ConfigureAwait(false);
            if (inputFlow == AwkStatementFlow.ExitProgram)
                break;
        }
    }

    private void ApplyInputAssignments()
    {
        for (var index = 0; index < invocation.InputPlan.InputArguments.Count; index++)
        {
            var inputArgument = invocation.InputPlan.InputArguments[index];
            if (IsDeletedRuntimeArgument(inputArgument))
                continue;

            if (inputArgument is AwkInputArgument.VariableAssignmentArgument assignmentArgument)
            {
                ApplyVariableAssignment(assignmentArgument.VariableAssignment);
            }
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
            if (TextEqualsAscii(text, "-"))
                return new RuntimeArgumentValue(text, "-");

            if (TextEqualsAscii(text, "/dev/stdin"))
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

    private AwkResolvedPath ResolveRuntimePath(AwkInputArgument inputArgument, RuntimeArgumentValue runtimeArgument) =>
        inputArgument is AwkInputArgument.FileArgument file &&
        runtimeArgument.Text.AsSpan().SequenceEqual(file.Path.Display.Span)
            ? file.Path
            : AwkPathResolution.ResolveArgument(
                runtimeArgument.Value,
                invocation.RuntimeArgumentsPlan.CurrentDirectory);

    private static bool TextEqualsAscii(AwkText text, string value)
    {
        var span = text.AsSpan();
        if (span.Length != value.Length)
            return false;

        for (var index = 0; index < span.Length; index++)
        {
            if (span[index] != (byte)value[index])
                return false;
        }

        return true;
    }

    private readonly record struct RuntimeArgumentValue(AwkText Text, string Value)
    {
        public bool IsStandardInput => Value is "-" or "/dev/stdin";
    }

    private static AwkRuntimeException CreateOpenFailure(AwkResolvedPath path, AwkOpenedFile opened) =>
        new(
            opened.Failed
                ? AwkMessages.FormatPathError(path.Display, opened.Error.Message)
                : AwkMessages.FormatPathError(path.Display),
            AwkDiagnosticId.HostIoFailure);

    private async ValueTask<AwkStatementFlow> ProcessInputDescriptorAsync(AwkFileDescriptor descriptor)
    {
        var beginFileFlow = await actionExecutor.ExecuteBeginFileActionsAsync(state, io)
            .ConfigureAwait(false);
        if (beginFileFlow is AwkStatementFlow.ExitProgram or AwkStatementFlow.NextFile)
            return beginFileFlow;

        var inputFlow = await ProcessDescriptorAsync(descriptor).ConfigureAwait(false);
        if (inputFlow == AwkStatementFlow.ExitProgram)
            return AwkStatementFlow.ExitProgram;

        var endFileFlow = await actionExecutor.ExecuteEndFileActionsAsync(state, io)
            .ConfigureAwait(false);
        if (endFileFlow == AwkStatementFlow.ExitProgram)
            return AwkStatementFlow.ExitProgram;

        return inputFlow == AwkStatementFlow.NextFile || endFileFlow == AwkStatementFlow.NextFile
            ? AwkStatementFlow.NextFile
            : AwkStatementFlow.Continue;
    }

    private async ValueTask<AwkStatementFlow> ProcessPreparedInputAsync(PreparedMainInput preparedInput)
    {
        var beginFileFlow = await preparedInput.EnsureBeginFileAsync().ConfigureAwait(false);
        if (beginFileFlow is AwkStatementFlow.ExitProgram or AwkStatementFlow.NextFile)
        {
            await preparedInput.CloseAsync().ConfigureAwait(false);
            return beginFileFlow;
        }

        var recordReader = new AwkRecordReader(host, state, io, cancellationToken);
        var inputFlow = await recordReader.ProcessReaderAsync(
                preparedInput.Reader,
                record => actionExecutor.ProcessRecordAsync(state, io, record))
            .ConfigureAwait(false);
        if (inputFlow == AwkStatementFlow.ExitProgram)
        {
            await preparedInput.CloseAsync().ConfigureAwait(false);
            return AwkStatementFlow.ExitProgram;
        }

        var endFileFlow = await actionExecutor.ExecuteEndFileActionsAsync(state, io)
            .ConfigureAwait(false);
        await preparedInput.CloseAsync().ConfigureAwait(false);
        if (endFileFlow == AwkStatementFlow.ExitProgram)
            return AwkStatementFlow.ExitProgram;

        return inputFlow == AwkStatementFlow.NextFile || endFileFlow == AwkStatementFlow.NextFile
            ? AwkStatementFlow.NextFile
            : AwkStatementFlow.Continue;
    }

    private async ValueTask<AwkStatementFlow> ProcessDescriptorAsync(AwkFileDescriptor descriptor)
    {
        var recordReader = new AwkRecordReader(host, state, io, cancellationToken);
        return await recordReader.ProcessDescriptorAsync(
                descriptor,
                record => actionExecutor.ProcessRecordAsync(state, io, record))
            .ConfigureAwait(false);
    }

    internal abstract class MainInputPreparation
    {
        public static Missing MissingResult { get; } = new();

        public static DeferredFailure DeferredFailureResult { get; } = new();

        public static Prepared FromPrepared(PreparedMainInput input) => new(input);

        public sealed class Prepared(PreparedMainInput input) : MainInputPreparation
        {
            public PreparedMainInput Input { get; } = input;
        }

        public sealed class Missing : MainInputPreparation
        {
        }

        public sealed class DeferredFailure : MainInputPreparation
        {
        }
    }

    internal sealed class PreparedMainInput(
        IAwkHost host,
        AwkRuntimeActionExecutor actionExecutor,
        AwkRuntimeState state,
        AwkIo io,
        CancellationToken cancellationToken,
        AwkInputPlanIndex inputPlanIndex,
        AwkText runtimeArgumentText,
        string fileName,
        AwkArgvIndex argvIndex,
        AwkFileDescriptor descriptor,
        AsyncLineReader reader,
        bool closeDescriptor)
    {
        private bool _beginFileExecuted;
        private bool _closed;
        private readonly AwkInputPlanIndex _inputPlanIndex = inputPlanIndex;
        private readonly AwkText _runtimeArgumentText = runtimeArgumentText;

        public AsyncLineReader Reader { get; } = reader;

        public bool IsForInputPlanIndex(int inputPlanIndex) =>
            _inputPlanIndex.HasValue && _inputPlanIndex.Value == inputPlanIndex;

        public bool CanUseCurrentArgument()
        {
            if (!_inputPlanIndex.HasValue)
                return true;

            return state.ContainsArgumentVectorValue(argvIndex) &&
                state.GetArgumentVectorByteText(argvIndex).Equals(_runtimeArgumentText);
        }

        public async ValueTask<AwkStatementFlow> EnsureBeginFileAsync()
        {
            if (_beginFileExecuted)
                return AwkStatementFlow.Continue;

            _beginFileExecuted = true;
            state.BeginInputFile(fileName, argvIndex);
            return await actionExecutor.ExecuteBeginFileActionsAsync(state, io)
                .ConfigureAwait(false);
        }

        public async Task CloseAsync()
        {
            if (_closed || !closeDescriptor)
                return;

            _closed = true;
            state.Resources.RecordHostCall();
            await host.CloseDescriptorAsync(descriptor, cancellationToken).ConfigureAwait(false);
        }
    }
}
