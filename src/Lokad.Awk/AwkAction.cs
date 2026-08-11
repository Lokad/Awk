using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkAction
{
    private readonly AwkPattern? _pattern;
    private readonly AwkStatement[]? _statements;

    public AwkAction(AwkPattern? pattern, IReadOnlyList<AwkStatement> statements)
    {
        _pattern = pattern;
        _statements = AwkCollectionSnapshots.Copy(statements);
    }

    internal bool IsPatternless => _pattern == null;

    internal AwkStatement[] Statements => _statements ?? [];

    public ValueTask<bool> MatchesAsync(AwkRuntimeState state) =>
        _pattern == null
            ? ValueTask.FromResult(true)
            : _pattern.MatchesAsync(state);

    public async ValueTask<AwkStatementFlow> ExecuteAsync(
        AwkRuntimeState state,
        AwkIo io,
        AwkOutputSink output)
    {
        var statements = Statements;
        if (statements.Length == 0)
            return AwkStatementFlow.Continue;

        using var executionContext = state.PushExecutionContext(output, io);
        var evaluationContext = new AwkEvaluationContext(state, output, io);
        var flow = await AwkStatementBlockExecutor
            .ExecuteAsync(statements, evaluationContext)
            .ConfigureAwait(false);

        if (flow == AwkStatementFlow.BreakLoop)
            throw new AwkRuntimeException(AwkMessages.ErrorBreakOutsideLoop);

        if (flow == AwkStatementFlow.ContinueLoop)
            throw new AwkRuntimeException(AwkMessages.ErrorContinueOutsideLoop);

        return flow;
    }

}
