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

    public ValueTask<bool> MatchesAsync(AwkRuntimeState state) =>
        _pattern == null
            ? ValueTask.FromResult(true)
            : _pattern.MatchesAsync(state);

    public async ValueTask<AwkStatementFlow> ExecuteAsync(
        AwkRuntimeState state,
        AwkIo io,
        AwkOutputSink output)
    {
        if (_statements.Count == 0)
            return AwkStatementFlow.Continue;

        using var executionContext = state.PushExecutionContext(output, io);
        var evaluationContext = new AwkEvaluationContext(state.RecordNumber, state, output, io);
        for (var index = 0; index < _statements.Count; index++)
        {
            state.RecordExecutionStep();
            state.ThrowIfCancellationRequested();
            var flow = await _statements[index].ExecuteAsync(evaluationContext).ConfigureAwait(false);
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
