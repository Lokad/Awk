using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>Runs an interpreter statement block with its shared budget, cancellation, and flush boundaries.</summary>
internal static class AwkStatementBlockExecutor
{
    internal static bool TryPrepareComputation(AwkStatement[] statements,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions, int remainingDepth,
        out AwkStatement[] prepared, out int depth, out int calls, Action? checkpoint)
    {
        prepared = [];
        depth = 1;
        calls = 0;
        if (remainingDepth <= 0) return false;
        if (statements.Length == 0) return true;
        var nodes = new AwkStatement[statements.Length];
        for (var index = 0; index < nodes.Length; index++)
        {
            checkpoint?.Invoke();
            if (!statements[index].TryPrepareComputation(functions, remainingDepth, out var node, checkpoint))
                return false;
            nodes[index] = node.Statement;
            depth = Math.Max(depth, node.Depth);
            calls = Math.Max(calls, node.Calls);
        }
        prepared = nodes;
        return true;
    }

    internal static AwkStatementFlow ExecuteComputation(AwkStatement[] statements,
        AwkEvaluationContext context, ref AwkValue returned)
    {
        foreach (var statement in statements)
        {
            context.State.Checkpoint();
            var flow = statement.ExecuteComputation(context, ref returned);
            if (flow != AwkStatementFlow.Continue) return flow;
        }
        return AwkStatementFlow.Continue;
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
