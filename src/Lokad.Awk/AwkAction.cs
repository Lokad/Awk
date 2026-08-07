using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkAction
{
    private readonly AwkPattern? _pattern;
    private readonly IReadOnlyList<AwkStatement> _statements;

    public AwkAction(AwkPattern? pattern, IReadOnlyList<AwkStatement> statements)
    {
        _pattern = pattern;
        _statements = AwkExecutableCollections.Copy(statements);
    }

    internal bool IsPatternless => _pattern == null;

    internal IReadOnlyList<AwkStatement> Statements => _statements;

    public async ValueTask<bool> MatchesAsync(AwkRuntimeState state)
    {
        if (_pattern == null)
            return true;

        return await _pattern.MatchesAsync(state).ConfigureAwait(false);
    }

    public async ValueTask<AwkStatementFlow> ExecuteAsync(
        AwkRuntimeState state,
        AwkIo io,
        AwkOutputSink output)
    {
        if (_statements.Count == 0)
            return AwkStatementFlow.Continue;

        using var executionContext = state.PushExecutionContext(output, io);
        var evaluationContext = new AwkEvaluationContext(state.RecordNumber, state, output, io);
        foreach (var statement in _statements)
        {
            state.RecordExecutionStep();
            state.ThrowIfCancellationRequested();
            var flow = await statement.ExecuteAsync(evaluationContext).ConfigureAwait(false);
            await output.FlushAsync(io).ConfigureAwait(false);
            if (flow is AwkStatementFlow.NextRecord or AwkStatementFlow.NextFile or AwkStatementFlow.ExitProgram)
                return flow;

            if (flow == AwkStatementFlow.BreakLoop)
                throw new AwkRuntimeException(AwkMessages.ErrorBreakOutsideLoop);

            if (flow == AwkStatementFlow.ContinueLoop)
                throw new AwkRuntimeException(AwkMessages.ErrorContinueOutsideLoop);
        }

        return AwkStatementFlow.Continue;
    }

}
