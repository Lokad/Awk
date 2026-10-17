using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRuntimeActionExecutor : IDisposable
{
    private readonly AwkProgram _program;
    private readonly AwkExecutableProgram? _executableProgram;
    private readonly AwkOutputSink _output;

    public AwkRuntimeActionExecutor(
        AwkProgram program,
        AwkExecutableProgram? executableProgram,
        AwkRuntimeState state)
    {
        _program = program;
        _executableProgram = executableProgram;
        _output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
    }

    public bool HasBeginFileActions => _program.BeginFileActions.Length > 0;

    public AwkOutputSink Output => _output;

    public ValueTask<AwkStatementFlow> ExecuteBeginActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.BeginActions,
            _executableProgram?.BeginActions,
            _executableProgram?.Tables,
            AwkActionPhase.Begin,
            state,
            io);

    public ValueTask<AwkStatementFlow> ExecuteEndActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.EndActions,
            _executableProgram?.EndActions,
            _executableProgram?.Tables,
            AwkActionPhase.End,
            state,
            io);

    public ValueTask<AwkStatementFlow> ExecuteBeginFileActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.BeginFileActions,
            _executableProgram?.BeginFileActions,
            _executableProgram?.Tables,
            AwkActionPhase.BeginFile,
            state,
            io);

    public ValueTask<AwkStatementFlow> ExecuteEndFileActionsAsync(AwkRuntimeState state, AwkIo io) =>
        ExecutePhaseActionsAsync(
            _program.EndFileActions,
            _executableProgram?.EndFileActions,
            _executableProgram?.Tables,
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
            if (_executableProgram == null)
            {
                for (var index = 0; index < _program.MainActions.Length; index++)
                {
                    var action = _program.MainActions[index];
                    if (!await action.MatchesAsync(state).ConfigureAwait(false))
                        continue;

                    var flow = await action.ExecuteAsync(state, io, _output).ConfigureAwait(false);
                    var decision = ResolveRecordActionFlow(flow);
                    if (decision.ShouldStop)
                        return decision.Flow;
                }

                return AwkStatementFlow.Continue;
            }

            for (var index = 0; index < _executableProgram.MainActions.Length; index++)
            {
                var action = _executableProgram.MainActions[index];
                if (!await MatchesExecutableActionAsync(action, state).ConfigureAwait(false))
                    continue;

                var flow = await AwkExecutableDispatcher.ExecuteActionAsync(
                        action,
                        _executableProgram.Tables,
                        state,
                        io,
                        _output)
                    .ConfigureAwait(false);
                var decision = ResolveRecordActionFlow(flow);
                if (decision.ShouldStop)
                    return decision.Flow;
            }

            return AwkStatementFlow.Continue;

            static ValueTask<bool> MatchesExecutableActionAsync(
                AwkExecutableAction action,
                AwkRuntimeState state)
            {
                if (!action.FallbackAction.IsPatternless)
                    state.Counters.RecordVmPatternFallback();

                return action.FallbackAction.MatchesAsync(state);
            }

            static AwkActionFlowDecision ResolveRecordActionFlow(AwkStatementFlow flow)
            {
                if (flow == AwkStatementFlow.ExitProgram)
                    return AwkActionFlowDecision.Stop(AwkStatementFlow.ExitProgram);

                if (flow == AwkStatementFlow.NextFile)
                    return AwkActionFlowDecision.Stop(AwkStatementFlow.NextFile);

                if (flow == AwkStatementFlow.NextRecord)
                    return AwkActionFlowDecision.Stop(AwkStatementFlow.Continue);

                return AwkActionFlowDecision.ContinueActions();
            }
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
        AwkAction[] interpreterActions,
        AwkExecutableAction[]? executableActions,
        AwkExecutableTables? executableTables,
        AwkActionPhase phase,
        AwkRuntimeState state,
        AwkIo io)
    {
        using var actionPhase = state.EnterActionPhase(phase);
        using var globalBindings = state.EnterGlobalBindings();
        try
        {
            if (executableActions == null)
            {
                for (var index = 0; index < interpreterActions.Length; index++)
                {
                    var flow = await interpreterActions[index].ExecuteAsync(state, io, _output).ConfigureAwait(false);
                    var decision = ResolvePhaseActionFlow(flow, phase);
                    if (decision.ShouldStop)
                        return decision.Flow;
                }

                return AwkStatementFlow.Continue;
            }

            if (executableTables is not AwkExecutableTables tables)
                throw new InvalidOperationException("Executable action tables are required for executable actions.");

            for (var index = 0; index < executableActions.Length; index++)
            {
                var flow = await AwkExecutableDispatcher.ExecuteActionAsync(
                        executableActions[index],
                        tables,
                        state,
                        io,
                        _output)
                    .ConfigureAwait(false);
                var decision = ResolvePhaseActionFlow(flow, phase);
                if (decision.ShouldStop)
                    return decision.Flow;
            }

            return AwkStatementFlow.Continue;

        }
        catch (AwkFunctionFlowSignal signal)
        {
            return ResolvePhaseActionFlow(signal.Flow, phase).Flow;
        }

        static AwkActionFlowDecision ResolvePhaseActionFlow(
            AwkStatementFlow flow,
            AwkActionPhase phase)
        {
            if (flow == AwkStatementFlow.ExitProgram)
                return AwkActionFlowDecision.Stop(AwkStatementFlow.ExitProgram);

            if (flow == AwkStatementFlow.NextRecord)
                throw new AwkRuntimeException(AwkMessages.ErrorNextOutsideInput);

            if (flow == AwkStatementFlow.NextFile)
            {
                if (phase != AwkActionPhase.BeginFile)
                    throw new AwkRuntimeException(AwkMessages.ErrorNextFileOutsideInput);

                return AwkActionFlowDecision.Stop(AwkStatementFlow.NextFile);
            }

            return AwkActionFlowDecision.ContinueActions();
        }
    }

    private readonly record struct AwkActionFlowDecision
    {
        private AwkActionFlowDecision(AwkActionFlowDecisionKind kind, AwkStatementFlow flow)
        {
            Kind = kind;
            Flow = flow;
        }

        public AwkActionFlowDecisionKind Kind { get; }

        public AwkStatementFlow Flow { get; }

        public bool ShouldStop => Kind == AwkActionFlowDecisionKind.StopActions;

        public static AwkActionFlowDecision ContinueActions() =>
            new(AwkActionFlowDecisionKind.ContinueActions, AwkStatementFlow.Continue);

        public static AwkActionFlowDecision Stop(AwkStatementFlow flow) =>
            new(AwkActionFlowDecisionKind.StopActions, flow);
    }

    private enum AwkActionFlowDecisionKind
    {
        ContinueActions,
        StopActions
    }
}
