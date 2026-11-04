namespace Lokad.Awk;

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

}
