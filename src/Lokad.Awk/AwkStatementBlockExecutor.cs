using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>Runs an interpreter statement block with its shared budget, cancellation, and flush boundaries.</summary>
internal static class AwkStatementBlockExecutor
{
    internal static Action<AwkEvaluationContext>? TryPrepareActions(AwkStatement[] statements,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? functions, Action? preparationCheckpoint = null)
    {
        if (statements.Length is 0 or > 16) return null;
        var remaining = 16;
        return TryPrepareActions(statements, functions, 4, ref remaining, out var execute, preparationCheckpoint) ? execute : null;
    }

    internal static bool TryPrepareActions(AwkStatement[] statements,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? functions, int conditionalDepth,
        ref int remainingStatements, out Action<AwkEvaluationContext> execute, Action? preparationCheckpoint = null)
    {
        execute = static _ => { };
        if (statements.Length > remainingStatements) return false;
        if (statements.Length == 0) return true;
        var actions = new Action<AwkEvaluationContext>[statements.Length];
        for (var index = 0; index < statements.Length; index++)
        {
            if (--remainingStatements < 0) return false;
            var statement = functions is { Count: > 0 }
                ? statements[index].PrepareSynchronousCalls(functions, preparationCheckpoint) : statements[index];
            if (!statement.TryPrepareTopLevelSynchronousAction(conditionalDepth,
                    ref remainingStatements, out actions[index])) return false;
        }
        execute = PrepareExecution(actions);
        return true;
    }

    private static Action<AwkEvaluationContext> PrepareExecution(Action<AwkEvaluationContext>[] actions)
    {
        return Execute;
        void Execute(AwkEvaluationContext context)
        {
            foreach (var action in actions)
            {
                context.State.Checkpoint();
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
            state.Checkpoint();
            var flow = await statements[index].ExecuteAsync(context).ConfigureAwait(false);
            if (execution.Output.HasPendingOutput)
                await execution.Output.FlushAsync(execution.Io).ConfigureAwait(false);
            else
                state.Counters.RecordSkippedEmptyStatementFlush();
            if (flow != AwkStatementFlow.Continue)
                return flow;
        }

        return AwkStatementFlow.Continue;
    }
}
