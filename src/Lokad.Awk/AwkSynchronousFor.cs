namespace Lokad.Awk;

// Reuses the interpreter's scalar descriptors and setters. Calls, fields, arrays,
// control-flow statements, diagnostics and host work retain their normal executor.
internal sealed class AwkSynchronousFor(
    Action<AwkEvaluationContext> initialize,
    AwkSynchronousExpression condition,
    Action<AwkEvaluationContext> increment,
    Action<AwkEvaluationContext>[] body)
{
    public static AwkSynchronousFor? TryPrepare(AwkStatement initializer, AwkCondition condition,
        AwkStatement increment, AwkStatement[] body)
    {
        if (body.Length > 16 || !initializer.TryPrepareSynchronousAction(out var initialize) ||
            !condition.TryGetSynchronousExpression(out var test) ||
            !increment.TryPrepareSynchronousAction(out var step)) return null;
        var actions = new Action<AwkEvaluationContext>[body.Length];
        for (var index = 0; index < body.Length; index++)
        {
            if (!body[index].TryPrepareSynchronousAction(out actions[index])) return null;
        }
        return new(initialize, test, step, actions);
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
}
