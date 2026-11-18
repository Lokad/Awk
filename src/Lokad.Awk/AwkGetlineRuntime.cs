namespace Lokad.Awk;

internal static class AwkGetlineRuntime
{
    internal static int ApplyResult(AwkRuntimeState state, AwkResolvedGetlineTarget target,
        bool readsMainInput, AwkGetlineCompletion completion)
    {
        var record = AwkInputRecordResult.EndOfFile;
        completion.Reader?.TakeRecord(out record);
        var hasRecord = record.HasRecord;
        if (hasRecord) state.Counters.Add(AwkCounter.RecordsRead);
        state.ThrowIfCancellationRequested();

        if (hasRecord)
        {
            if (target.IsCurrentRecord)
            {
                state.PublishInputRecord(in record, readsMainInput);
                return 1;
            }
            state.ThrowIfCancellationRequested();
            if (readsMainInput)
            {
                state.AdvanceMainInputRecord(record.Terminator);
            }
            else
            {
                state.SetRecordTerminator(record.Terminator);
            }

            target.AssignRecord(state, record.RecordText);
            return 1;
        }
        if (record.ClearsRecordTerminator)
        {
            state.SetRecordTerminator(AwkRecordTerminator.Empty);
        }
        else if (completion.Error is { } error)
        {
            state.SetErrno(error);
            return -1;
        }
        return 0;
    }
}
