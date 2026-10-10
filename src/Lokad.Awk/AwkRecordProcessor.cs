using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordProcessor(AwkRuntimeState state)
{
    public ValueTask<AwkStatementFlow> ProcessAsync(
        AwkText record,
        AwkRecordTerminator terminator,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync) =>
        ProcessAsync(
            record,
            terminator,
            AwkRecordFieldSplittingMode.Ordinary,
            processRecordAsync);

    public ValueTask<AwkStatementFlow> ProcessParagraphAsync(
        AwkText record,
        AwkRecordTerminator terminator,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync) =>
        ProcessAsync(
            record,
            terminator,
            AwkRecordFieldSplittingMode.Paragraph,
            processRecordAsync);

    private ValueTask<AwkStatementFlow> ProcessAsync(
        AwkText record,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode,
        Func<AwkText, ValueTask<AwkStatementFlow>> processRecordAsync)
    {
        state.ThrowIfRecordBytesExceeded(record);
        state.PreflightCurrentRecordAdmission(record, terminator, fieldSplittingMode);
        state.Counters.RecordRecordRead();
        state.AdvanceMainInputRecord(terminator);
        state.SetRecordFieldSplittingMode(fieldSplittingMode);
        return processRecordAsync(record);
    }

}

internal enum AwkRecordFieldSplittingMode
{
    Ordinary,
    Paragraph
}
