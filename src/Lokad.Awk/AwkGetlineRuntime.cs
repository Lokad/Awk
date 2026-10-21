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
        static void ApplyResult(
            AwkResolvedGetlineTarget target,
            AwkGetlineSource source,
            AwkEvaluationContext context,
            AwkGetlineResult result)
        {
            if (result.HasRecord)
            {
                using var admission = target.IsCurrentRecord
                    ? context.State.PreflightCurrentRecordAdmission(
                        result.RecordText, result.Terminator, result.FieldSplittingMode)
                    : default;
                if (source.ReadsMainInput)
                {
                    context.State.AdvanceMainInputRecord(result.Terminator);
                }
                else
                {
                    context.State.SetRecordTerminator(result.Terminator);
                }

                target.AssignRecord(context.State, result.RecordText, result.FieldSplittingMode);
            }
            else if (result.ClearsRecordTerminator)
            {
                context.State.SetRecordTerminator(AwkRecordTerminator.Empty);
            }
            else if (result.Failed)
            {
                context.State.SetErrno(result.Error.Errno);
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

        var resolvedSource = await source.ResolveAsync(context).ConfigureAwait(false);
        using var sourceLease = context.State.MemoryAccounting.ReserveTransient(resolvedSource.RetainedNameBytes);
        var resolvedTarget = await target.ResolveAsync(context).ConfigureAwait(false);
        using var targetLease = context.State.MemoryAccounting.ReserveTransient(resolvedTarget.RetainedKeyBytes);
        var result = await resolvedSource.ReadAsync(execution).ConfigureAwait(false);

        ApplyResult(resolvedTarget, source, context, result);
        return result;
    }
}
