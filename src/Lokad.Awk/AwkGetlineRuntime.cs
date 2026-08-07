using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkGetlineRuntime
{
    public static async ValueTask<AwkGetlineResult> ExecuteAsync(
        AwkGetlineTarget target,
        AwkGetlineSource source,
        AwkEvaluationContext context,
        AwkExecutionContext execution,
        bool flushPendingDiagnostics)
    {
        var readMainInput = source.ReadsMainInput;
        if (readMainInput)
        {
            await execution.Output.FlushAsync(execution.Io).ConfigureAwait(false);
        }
        else if (flushPendingDiagnostics)
        {
            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        }

        var result = await source.ReadAsync(context, execution).ConfigureAwait(false);

        await ApplyResultAsync(target, context, execution, readMainInput, result)
            .ConfigureAwait(false);
        return result;
    }

    private static async ValueTask ApplyResultAsync(
        AwkGetlineTarget target,
        AwkEvaluationContext context,
        AwkExecutionContext execution,
        bool readMainInput,
        AwkGetlineResult result)
    {
        if (result.HasRecord)
        {
            if (readMainInput)
            {
                context.State.AdvanceMainInputRecord(result.Terminator);
            }
            else
            {
                context.State.SetRecordTerminator(result.Terminator);
            }

            await target.AssignLineAsync(context, result.LineText).ConfigureAwait(false);
        }
        else if (result.Failed)
        {
            context.State.SetErrno(result.Error.Errno);
            if (result.Error.HasDiagnostic)
                await execution.Io.WriteStandardErrorAsync(result.Error.Diagnostic).ConfigureAwait(false);
        }
    }
}
