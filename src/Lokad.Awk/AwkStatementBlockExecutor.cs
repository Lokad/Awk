using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>Runs an interpreter statement block with its shared budget, cancellation, and flush boundaries.</summary>
internal static class AwkStatementBlockExecutor
{
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
            state.ThrowIfCancellationRequested();
            var flow = await statements[index].ExecuteAsync(context).ConfigureAwait(false);
            await execution.Output.FlushAsync(execution.Io).ConfigureAwait(false);
            if (flow != AwkStatementFlow.Continue)
                return flow;
        }

        return AwkStatementFlow.Continue;
    }
}
