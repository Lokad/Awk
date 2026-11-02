using System.Threading.Tasks;

namespace Lokad.Awk;

internal enum AwkPendingDiagnosticPolicy
{
    Preserve,
    Flush
}

internal static class AwkGetlineRuntime
{
    internal static void ApplyResult(AwkRuntimeState state, AwkResolvedGetlineTarget target,
        bool readsMainInput, AwkGetlineResult result)
    {
        if (result.HasRecord)
        {
            if (target.IsCurrentRecord)
            {
                state.PublishInputRecord(AwkInputRecordResult.Record(result.RecordText,
                    result.Terminator, result.FieldSplittingMode), readsMainInput);
                return;
            }
            state.ThrowIfCancellationRequested();
            if (readsMainInput)
            {
                state.AdvanceMainInputRecord(result.Terminator);
            }
            else
            {
                state.SetRecordTerminator(result.Terminator);
            }

            target.AssignRecord(state, result.RecordText, result.FieldSplittingMode);
        }
        else if (result.ClearsRecordTerminator)
        {
            state.SetRecordTerminator(AwkRecordTerminator.Empty);
        }
        else if (result.Failed)
        {
            state.SetErrno(result.Error.Errno);
        }
    }

    public static async ValueTask<AwkGetlineResult> ExecuteAsync(
        AwkGetlineTarget target,
        AwkGetlineSource source,
        AwkEvaluationContext context,
        AwkExecutionContext execution,
        AwkPendingDiagnosticPolicy pendingDiagnosticPolicy)
    {
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
        using var resolvedTarget = await target.ResolveAsync(context).ConfigureAwait(false);
        using var targetLease = context.State.MemoryAccounting.ReserveTransient(resolvedTarget.RetainedKeyBytes);
        var result = await resolvedSource.ReadAsync(execution).ConfigureAwait(false);

        ApplyResult(context.State, resolvedTarget, source.ReadsMainInput, result);
        return result;
    }
}
