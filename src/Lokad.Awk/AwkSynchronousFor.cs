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
            (!HasPreparedCalls || (state.FieldsCannotReportRegexDiagnostics && state.Limits.MaxSyntaxNestingDepth <= AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth &&
                System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())) &&
            context.TryGetExecutionContext(out var execution) && !execution.Output.HasPendingOutput;
    }

    public static AwkSynchronousFor? TryPrepare(AwkStatement initializer, AwkCondition condition,
        AwkStatement increment, AwkStatement[] body,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? functions = null, Action? preparationCheckpoint = null)
    {
        var ordinary = TryPrepareBody(initializer, condition, increment, body, false, preparationCheckpoint);
        if (ordinary is not null) return ordinary;
        functions ??= AwkSynchronousFunction.Empty;
        var preparedBody = new AwkStatement[body.Length];
        for (var index = 0; index < body.Length; index++)
        {
            preparationCheckpoint?.Invoke();
            preparedBody[index] = body[index].PrepareSynchronousCalls(functions, preparationCheckpoint);
        }
        return TryPrepareBody(initializer.PrepareSynchronousCalls(functions, preparationCheckpoint), condition.PrepareSynchronousCalls(functions, preparationCheckpoint),
            increment.PrepareSynchronousCalls(functions, preparationCheckpoint), preparedBody, true, preparationCheckpoint);
    }

    private static AwkSynchronousFor? TryPrepareBody(AwkStatement initializer, AwkCondition condition,
        AwkStatement increment, AwkStatement[] body, bool hasPreparedCalls, Action? preparationCheckpoint)
    {
        preparationCheckpoint?.Invoke();
        if (!initializer.TryPrepareSynchronousAction(out var initialize) ||
            !condition.TryGetSynchronousExpression(out var test) ||
            !increment.TryPrepareSynchronousAction(out var step)) return null;
        var actions = new Action<AwkEvaluationContext>[body.Length];
        for (var index = 0; index < body.Length; index++)
        {
            preparationCheckpoint?.Invoke();
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
            state.Checkpoint();
            foreach (var action in body)
            {
                state.Checkpoint();
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
            state.Checkpoint();
            foreach (var action in body)
            {
                state.Checkpoint();
                action(context);
            }
        } while (condition.EvaluateSynchronous(context).IsTruthy());
        return AwkStatementFlow.Continue;
    }
}
