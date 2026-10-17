namespace Lokad.Awk;

// Reuses interpreter scalar setters and bounded synchronous expressions, including eligible
// field reads. Host-capable indices, arrays, control flow and diagnostics retain ordinary execution.
// Known return-function calls reuse existing frames behind the loop's policy/stack guard.
internal sealed class AwkSynchronousFor(
    Action<AwkEvaluationContext> initialize,
    AwkSynchronousExpression condition,
    Action<AwkEvaluationContext> increment,
    Action<AwkEvaluationContext>[] body,
    bool hasPreparedCalls)
{
    public bool HasPreparedCalls { get; } = hasPreparedCalls;

    public bool CanExecute(AwkEvaluationContext context)
    {
        var state = context.State;
        return state.LintMode == AwkLintMode.Off &&
            (!HasPreparedCalls || (!state.UsesExecutableEngine &&
                state.Limits.MaxSyntaxNestingDepth <= AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth &&
                System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())) &&
            context.TryGetExecutionContext(out var execution) && !execution.Output.HasPendingOutput;
    }

    public static AwkSynchronousFor? TryPrepare(AwkStatement initializer, AwkCondition condition,
        AwkStatement increment, AwkStatement[] body,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? functions = null)
    {
        var ordinary = TryPrepareBody(initializer, condition, increment, body, false);
        if (ordinary is not null || body.Length > 16 || functions is not { Count: > 0 }) return ordinary;
        var preparedBody = new AwkStatement[body.Length];
        for (var index = 0; index < body.Length; index++)
            preparedBody[index] = body[index].PrepareSynchronousCalls(functions);
        return TryPrepareBody(initializer.PrepareSynchronousCalls(functions), condition.PrepareSynchronousCalls(functions),
            increment.PrepareSynchronousCalls(functions), preparedBody, true);
    }

    private static AwkSynchronousFor? TryPrepareBody(AwkStatement initializer, AwkCondition condition,
        AwkStatement increment, AwkStatement[] body, bool hasPreparedCalls)
    {
        if (body.Length > 16 || !initializer.TryPrepareSynchronousAction(out var initialize) ||
            !condition.TryGetSynchronousExpression(out var test) ||
            !increment.TryPrepareSynchronousAction(out var step)) return null;
        var actions = new Action<AwkEvaluationContext>[body.Length];
        for (var index = 0; index < body.Length; index++)
        {
            if (!body[index].TryPrepareSynchronousAction(out actions[index])) return null;
        }
        return new(initialize, test, step, actions, hasPreparedCalls);
    }

    public AwkStatementFlow Execute(AwkEvaluationContext context)
    {
        var state = context.State;
        state.Counters.RecordSynchronousLoop();
        initialize(context);
        while (condition.EvaluateSynchronous(context).IsTruthy())
        {
            state.RecordExecutionStep();
            foreach (var action in body)
            {
                state.RecordExecutionStep();
                action(context);
            }
            increment(context);
        }
        return AwkStatementFlow.Continue;
    }

    public AwkStatementFlow ExecuteDoWhile(AwkEvaluationContext context)
    {
        var state = context.State;
        state.Counters.RecordSynchronousLoop();
        state.Counters.RecordSynchronousDoWhileLoop();
        do
        {
            state.RecordExecutionStep();
            foreach (var action in body)
            {
                state.RecordExecutionStep();
                action(context);
            }
        } while (condition.EvaluateSynchronous(context).IsTruthy());
        return AwkStatementFlow.Continue;
    }
}
