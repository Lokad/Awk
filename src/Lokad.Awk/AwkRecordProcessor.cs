using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordProcessor(AwkRuntimeState state)
{
    public async ValueTask<AwkStatementFlow> ProcessAsync(
        AwkText record,
        AwkRecordTerminator terminator,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync) =>
        await ProcessAsync(
                record,
                terminator,
                AwkRecordFieldSplittingMode.Ordinary,
                processRecordAsync)
            .ConfigureAwait(false);

    public async ValueTask<AwkStatementFlow> ProcessParagraphAsync(
        AwkText record,
        AwkRecordTerminator terminator,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync) =>
        await ProcessAsync(
                record,
                terminator,
                AwkRecordFieldSplittingMode.Paragraph,
                processRecordAsync)
            .ConfigureAwait(false);

    private async ValueTask<AwkStatementFlow> ProcessAsync(
        AwkText record,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        state.ThrowIfRecordBytesExceeded(record);
        state.Counters.RecordRecordRead();
        state.AdvanceMainInputRecord(terminator);
        state.SetRecordFieldSplittingMode(fieldSplittingMode);
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

internal enum AwkRecordFieldSplittingMode
{
    Ordinary,
    Paragraph
}
