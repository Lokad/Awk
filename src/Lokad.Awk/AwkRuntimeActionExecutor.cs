using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRuntimeActionExecutor
{
    private readonly AwkProgram _program;
    private readonly AwkExecutableProgram? _executableProgram;

    public AwkRuntimeActionExecutor(AwkProgram program, AwkExecutableProgram? executableProgram)
    {
        _program = program;
        _executableProgram = executableProgram;
    }

    public bool HasBeginFileActions => _program.BeginFileActions.Count > 0;

    public async ValueTask<AwkStatementFlow> ExecuteBeginActionsAsync(AwkRuntimeState state, AwkIo io) =>
        await ExecutePhaseActionsAsync(
                _program.BeginActions,
                _executableProgram?.BeginActions,
                _executableProgram?.Tables,
                AwkActionPhase.Begin,
                state,
                io)
            .ConfigureAwait(false);

    public async ValueTask<AwkStatementFlow> ExecuteEndActionsAsync(AwkRuntimeState state, AwkIo io) =>
        await ExecutePhaseActionsAsync(
                _program.EndActions,
                _executableProgram?.EndActions,
                _executableProgram?.Tables,
                AwkActionPhase.End,
                state,
                io)
            .ConfigureAwait(false);

    public async ValueTask<AwkStatementFlow> ExecuteBeginFileActionsAsync(AwkRuntimeState state, AwkIo io) =>
        await ExecutePhaseActionsAsync(
                _program.BeginFileActions,
                _executableProgram?.BeginFileActions,
                _executableProgram?.Tables,
                AwkActionPhase.BeginFile,
                state,
                io)
            .ConfigureAwait(false);

    public async ValueTask<AwkStatementFlow> ExecuteEndFileActionsAsync(AwkRuntimeState state, AwkIo io) =>
        await ExecutePhaseActionsAsync(
                _program.EndFileActions,
                _executableProgram?.EndFileActions,
                _executableProgram?.Tables,
                AwkActionPhase.EndFile,
                state,
                io)
            .ConfigureAwait(false);

    public async ValueTask<AwkStatementFlow> ProcessRecordAsync(AwkRuntimeState state, AwkIo io, AwkText record)
    {
        state.SetCurrentRecord(record);
        if (_executableProgram == null)
        {
            return await ProcessRecordActionsAsync(
                    _program.MainActions,
                    action => action.MatchesAsync(state),
                    action => action.ExecuteAsync(state, io))
                .ConfigureAwait(false);
        }

        return await ProcessRecordActionsAsync(
                _executableProgram.MainActions,
                action => MatchesExecutableActionAsync(action, state),
                action => AwkExecutableDispatcher.ExecuteActionAsync(action, _executableProgram.Tables, state, io))
            .ConfigureAwait(false);
    }

    private static async ValueTask<AwkStatementFlow> ExecutePhaseActionsAsync(
        IReadOnlyList<AwkAction> interpreterActions,
        IReadOnlyList<AwkExecutableAction>? executableActions,
        AwkExecutableTables? executableTables,
        AwkActionPhase phase,
        AwkRuntimeState state,
        AwkIo io)
    {
        if (executableActions == null)
        {
            return await ExecutePhaseActionsAsync(
                    interpreterActions,
                    phase,
                    action => action.ExecuteAsync(state, io))
                .ConfigureAwait(false);
        }

        if (executableTables is not AwkExecutableTables tables)
            throw new InvalidOperationException("Executable action tables are required for executable actions.");

        return await ExecutePhaseActionsAsync(
                executableActions,
                phase,
                action => AwkExecutableDispatcher.ExecuteActionAsync(action, tables, state, io))
            .ConfigureAwait(false);
    }

    private static async ValueTask<AwkStatementFlow> ExecutePhaseActionsAsync<TAction>(
        IReadOnlyList<TAction> actions,
        AwkActionPhase phase,
        Func<TAction, ValueTask<AwkStatementFlow>> executeActionAsync)
    {
        for (var index = 0; index < actions.Count; index++)
        {
            var flow = await executeActionAsync(actions[index]).ConfigureAwait(false);
            var decision = ResolvePhaseActionFlow(flow, phase);
            if (decision.ShouldStop)
                return decision.Flow;
        }

        return AwkStatementFlow.Continue;
    }

    private static async ValueTask<AwkStatementFlow> ProcessRecordActionsAsync<TAction>(
        IReadOnlyList<TAction> actions,
        Func<TAction, ValueTask<bool>> matchesAsync,
        Func<TAction, ValueTask<AwkStatementFlow>> executeActionAsync)
    {
        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            if (!await matchesAsync(action).ConfigureAwait(false))
                continue;

            var flow = await executeActionAsync(action).ConfigureAwait(false);
            var decision = ResolveRecordActionFlow(flow);
            if (decision.ShouldStop)
                return decision.Flow;
        }

        return AwkStatementFlow.Continue;
    }

    private static async ValueTask<bool> MatchesExecutableActionAsync(
        AwkExecutableAction action,
        AwkRuntimeState state)
    {
        if (!action.FallbackAction.IsPatternless)
            state.Counters.RecordVmPatternFallback();

        return await action.FallbackAction.MatchesAsync(state).ConfigureAwait(false);
    }

    private static AwkActionFlowDecision ResolvePhaseActionFlow(
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

    private static AwkActionFlowDecision ResolveRecordActionFlow(AwkStatementFlow flow)
    {
        if (flow == AwkStatementFlow.ExitProgram)
            return AwkActionFlowDecision.Stop(AwkStatementFlow.ExitProgram);

        if (flow == AwkStatementFlow.NextFile)
            return AwkActionFlowDecision.Stop(AwkStatementFlow.NextFile);

        if (flow == AwkStatementFlow.NextRecord)
            return AwkActionFlowDecision.Stop(AwkStatementFlow.Continue);

        return AwkActionFlowDecision.ContinueActions();
    }

    private readonly record struct AwkActionFlowDecision(bool ShouldStop, AwkStatementFlow Flow)
    {
        public static AwkActionFlowDecision ContinueActions() =>
            new(false, AwkStatementFlow.Continue);

        public static AwkActionFlowDecision Stop(AwkStatementFlow flow) =>
            new(true, flow);
    }
}
