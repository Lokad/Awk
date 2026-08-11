using System.Threading.Tasks;

namespace Lokad.Awk;

internal enum AwkPendingDiagnosticPolicy
{
    Preserve,
    Flush
}

internal static class AwkGetlineRuntime
{
    public static async ValueTask<AwkGetlineResult> ExecuteAsync(
        AwkGetlineTarget target,
        AwkGetlineSource source,
        AwkEvaluationContext context,
        AwkExecutionContext execution,
        AwkPendingDiagnosticPolicy pendingDiagnosticPolicy)
    {
        static async ValueTask ApplyResultAsync(
            AwkGetlineTarget target,
            AwkGetlineSource source,
            AwkEvaluationContext context,
            AwkExecutionContext execution,
            AwkGetlineResult result)
        {
            if (result.HasRecord)
            {
                if (source.ReadsMainInput)
                {
                    context.State.AdvanceMainInputRecord(result.Terminator);
                }
                else
                {
                    context.State.SetRecordTerminator(result.Terminator);
                }

                await target.AssignRecordAsync(
                        context,
                        result.RecordText,
                        result.FieldSplittingMode)
                    .ConfigureAwait(false);
            }
            else if (result.Failed)
            {
                context.State.SetErrno(result.Error.Errno);
                if (result.Error.HasDiagnostic)
                {
                    await execution.Io.WriteStandardErrorAsync(result.Error.Diagnostic).ConfigureAwait(false);
                }
            }
        }

        var readMainInput = source.ReadsMainInput;
        if (readMainInput)
        {
            await execution.Output.FlushAsync(execution.Io).ConfigureAwait(false);
        }
        else if (pendingDiagnosticPolicy is AwkPendingDiagnosticPolicy.Flush)
        {
            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        }

        var result = await source.ReadAsync(context, execution).ConfigureAwait(false);

        await ApplyResultAsync(target, source, context, execution, result)
            .ConfigureAwait(false);
        return result;
    }
}
