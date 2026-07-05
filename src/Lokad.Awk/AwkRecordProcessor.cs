using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordProcessor(AwkRuntimeState state)
{
    public async ValueTask<AwkStatementFlow> ProcessAsync(
        AwkText record,
        AwkRecordTerminator terminator,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        state.ThrowIfRecordBytesExceeded(record);
        state.Counters.RecordRecordRead();
        state.AdvanceMainInputRecord(terminator);
        return await processRecordAsync(record).ConfigureAwait(false);
    }

    public async ValueTask<AwkStatementFlow> ProcessAsync(
        string record,
        AwkRecordTerminator terminator,
        Func<string, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        state.ThrowIfRecordBytesExceeded(record);
        state.Counters.RecordRecordRead();
        state.AdvanceMainInputRecord(terminator);
        return await processRecordAsync(record).ConfigureAwait(false);
    }
}
