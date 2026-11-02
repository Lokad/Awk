using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRuntimeActionExecutor : IDisposable
{
    private readonly AwkProgram _program;
    private readonly AwkOutputSink _output;
    private readonly AwkRegionStorage _regionStorage;

    public AwkRuntimeActionExecutor(
        AwkProgram program,
        AwkRuntimeState state)
    {
        _program = program;
        _output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        _regionStorage = new(state);
    }

    public bool HasBeginFileActions => _program.BeginFileActions.Length > 0;

    public AwkOutputSink Output => _output;

    public ValueTask<AwkStatementFlow> ExecuteBeginActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.BeginActions,
            AwkActionPhase.Begin,
            state,
            io);

    public ValueTask<AwkStatementFlow> ExecuteEndActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.EndActions,
            AwkActionPhase.End,
            state,
            io);

    public ValueTask<AwkStatementFlow> ExecuteBeginFileActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.BeginFileActions,
            AwkActionPhase.BeginFile,
            state,
            io);

    public ValueTask<AwkStatementFlow> ExecuteEndFileActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.EndFileActions,
            AwkActionPhase.EndFile,
            state,
            io);

    public ValueTask<AwkStatementFlow> ProcessRecordAsync(AwkRuntimeState state, AwkIo io, AwkInputRecordResult record)
    {
        var phase = state.EnterActionPhase(AwkActionPhase.Main);
        var execution = default(AwkExecutionContextScope);
        var pending = false;
        try
        {
            execution = state.PushExecutionContext(_output, io);
            // Header preflight ends once its actual owners are published. Later
            // actions must be able to refund relinquished storage to the shared root.
            using (var admission = state.PreflightCurrentRecordAdmission(record))
            {
                state.ThrowIfCancellationRequested();
                state.Counters.RecordRecordRead();
                state.AdvanceMainInputRecord(record.Terminator);
                state.SetRecordFieldSplittingMode(record.FieldSplittingMode);
                state.SetCurrentRecord(record);
            }
            var result = RunMainActions(state, io);
            if (result.IsCompletedSuccessfully)
                return ValueTask.FromResult(RecordFlow(result.Result));
            pending = true;
            return CompleteRecordAsync(result, phase, execution);
        }
        catch (AwkFunctionFlowSignal signal)
        {
            return ValueTask.FromResult(RecordFlow(signal.Flow));
        }
        finally
        {
            if (!pending)
            {
                execution.Dispose();
                phase.Dispose();
            }
        }
    }

    private static async ValueTask<AwkStatementFlow> CompleteRecordAsync(ValueTask<AwkStatementFlow> pending,
        AwkActionPhaseScope phase, AwkExecutionContextScope execution)
    {
        try { return RecordFlow(await pending.ConfigureAwait(false)); }
        catch (AwkFunctionFlowSignal signal) { return RecordFlow(signal.Flow); }
        finally
        {
            execution.Dispose();
            phase.Dispose();
        }
    }

    private static AwkStatementFlow RecordFlow(AwkStatementFlow flow) =>
        flow == AwkStatementFlow.NextRecord ? AwkStatementFlow.Continue : flow;

    // Completed operations stay in this loop. Only an actual pending match/action
    // transfers its index and scope to the asynchronous continuation below.
    private ValueTask<AwkStatementFlow> RunMainActions(AwkRuntimeState state, AwkIo io)
    {
        for (var index = 0; index < _program.MainActions.Length; index++)
        {
            var matches = _program.MainActions[index].MatchesAsync(state);
            if (!matches.IsCompletedSuccessfully)
                return ResumeMainActionsAsync(state, io, index, matches, default, true);
            if (!matches.Result) continue;
            var action = ExecuteMainAction(state, io, index);
            if (!action.IsCompletedSuccessfully)
                return ResumeMainActionsAsync(state, io, index, default, action, false);
            if (action.Result != AwkStatementFlow.Continue) return action;
        }
        return ValueTask.FromResult(AwkStatementFlow.Continue);
    }

    private ValueTask<AwkStatementFlow> ExecuteMainAction(AwkRuntimeState state, AwkIo io, int index)
    {
        var action = _program.MainActions[index];
        return action.Region is { } region
            ? region.ExecuteAsync(state, _regionStorage, _output, io)
            : action.ExecuteAsync(state, io, _output);
    }

    private async ValueTask<AwkStatementFlow> ResumeMainActionsAsync(AwkRuntimeState state, AwkIo io,
        int index, ValueTask<bool> match, ValueTask<AwkStatementFlow> action, bool waitingForMatch)
    {
        while (true)
        {
            if (!waitingForMatch || await match.ConfigureAwait(false))
            {
                if (waitingForMatch) action = ExecuteMainAction(state, io, index);
                var flow = await action.ConfigureAwait(false);
                if (flow != AwkStatementFlow.Continue) return flow;
            }
            if (++index == _program.MainActions.Length) return AwkStatementFlow.Continue;
            match = _program.MainActions[index].MatchesAsync(state);
            waitingForMatch = true;
        }
    }

    public void Dispose()
    {
        try { _output.Dispose(); }
        finally { _regionStorage.Dispose(); }
    }

    private async ValueTask<AwkStatementFlow> ExecutePhaseActionsAsync(
        AwkAction[] actions,
        AwkActionPhase phase,
        AwkRuntimeState state,
        AwkIo io)
    {
        using var actionPhase = state.EnterActionPhase(phase);
        using var globalBindings = state.EnterGlobalBindings();
        try
        {
            for (var index = 0; index < actions.Length; index++)
            {
                var flow = await actions[index].ExecuteAsync(state, io, _output).ConfigureAwait(false);
                var result = ResolvePhaseActionFlow(flow, phase);
                if (result != AwkStatementFlow.Continue)
                    return result;
            }

            return AwkStatementFlow.Continue;

        }
        catch (AwkFunctionFlowSignal signal)
        {
            return ResolvePhaseActionFlow(signal.Flow, phase);
        }

        static AwkStatementFlow ResolvePhaseActionFlow(
            AwkStatementFlow flow,
            AwkActionPhase phase)
        {
            if (flow == AwkStatementFlow.ExitProgram)
                return AwkStatementFlow.ExitProgram;

            if (flow == AwkStatementFlow.NextRecord)
                throw new AwkRuntimeException(AwkMessages.ErrorNextOutsideInput);

            if (flow == AwkStatementFlow.NextFile)
            {
                if (phase != AwkActionPhase.BeginFile)
                    throw new AwkRuntimeException(AwkMessages.ErrorNextFileOutsideInput);

                return AwkStatementFlow.NextFile;
            }

            return AwkStatementFlow.Continue;
        }
    }

}
