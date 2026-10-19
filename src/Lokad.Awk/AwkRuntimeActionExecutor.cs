using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRuntimeActionExecutor : IDisposable
{
    private readonly AwkProgram _program;
    private readonly AwkOutputSink _output;

    public AwkRuntimeActionExecutor(
        AwkProgram program,
        AwkRuntimeState state)
    {
        _program = program;
        _output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
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

    public async ValueTask<AwkStatementFlow> ProcessRecordAsync(AwkRuntimeState state, AwkIo io, AwkText record)
    {
        using var phase = state.EnterActionPhase(AwkActionPhase.Main);
        using var execution = state.PushExecutionContext(_output, io);
        try
        {
            state.SetCurrentRecord(record);
            for (var index = 0; index < _program.MainActions.Length; index++)
            {
                var action = _program.MainActions[index];
                if (!await action.MatchesAsync(state).ConfigureAwait(false))
                    continue;

                var flow = await action.ExecuteAsync(state, io, _output).ConfigureAwait(false);
                if (flow is AwkStatementFlow.ExitProgram or AwkStatementFlow.NextFile)
                    return flow;
                if (flow == AwkStatementFlow.NextRecord)
                    return AwkStatementFlow.Continue;
            }

            return AwkStatementFlow.Continue;

        }
        catch (AwkFunctionFlowSignal signal)
        {
            return signal.Flow == AwkStatementFlow.NextRecord ? AwkStatementFlow.Continue : signal.Flow;
        }
    }

    public void Dispose()
    {
        _output.Dispose();
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
