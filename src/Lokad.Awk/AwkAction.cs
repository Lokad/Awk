using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkAction
{
    private readonly AwkPattern? _pattern;
    private readonly AwkStatement[]? _statements;
    private readonly Action<AwkEvaluationContext>? _synchronous;

    public AwkAction(AwkPattern? pattern, IReadOnlyList<AwkStatement> statements,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? functions = null)
    {
        _pattern = pattern;
        var snapshot = AwkCollectionSnapshots.Copy(statements);
        _statements = snapshot;
        _synchronous = AwkStatementBlockExecutor.TryPrepareActions(snapshot, functions);
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
        if (_synchronous is { } prepared && !state.UsesExecutableEngine && state.LintMode == AwkLintMode.Off &&
            state.FieldsCannotReportRegexDiagnostics &&
            state.Limits.MaxSyntaxNestingDepth <= AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth &&
            !output.HasPendingOutput && System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            state.Counters.RecordSynchronousAction();
            prepared(evaluationContext);
            return AwkStatementFlow.Continue;
        }
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
