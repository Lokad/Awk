using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>Runs an interpreter statement block with its shared budget, cancellation, and flush boundaries.</summary>
internal static class AwkStatementBlockExecutor
{
    internal static Action<AwkEvaluationContext>? TryPrepareCalls(AwkStatement[] statements,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? functions)
    {
        if (statements.Length is 0 or > 16 || functions is not { Count: > 0 }) return null;
        var actions = new Action<AwkEvaluationContext>[statements.Length];
        var hasCalls = false;
        for (var index = 0; index < statements.Length; index++)
        {
            var statement = statements[index].PrepareSynchronousCalls(functions);
            hasCalls |= !ReferenceEquals(statement, statements[index]);
            if (!statement.TryPrepareSynchronousAction(out actions[index])) return null;
        }
        return hasCalls ? Execute : null;

        void Execute(AwkEvaluationContext context)
        {
            foreach (var action in actions)
            {
                context.State.RecordExecutionStep();
                action(context);
            }
        }
    }

    public static async ValueTask<AwkStatementFlow> ExecuteAsync(
        AwkStatement[] statements,
        AwkEvaluationContext context)
    {
        if (!context.TryGetExecutionContext(out var execution))
            throw new InvalidOperationException("Statement execution requires an awk I/O context.");

        var state = context.State;
        for (var index = 0; index < statements.Length; index++)
        {
            state.RecordExecutionStep();
            var flow = await statements[index].ExecuteAsync(context).ConfigureAwait(false);
            await execution.Output.FlushAsync(execution.Io).ConfigureAwait(false);
            if (flow != AwkStatementFlow.Continue)
                return flow;
        }

        return AwkStatementFlow.Continue;
    }
}
