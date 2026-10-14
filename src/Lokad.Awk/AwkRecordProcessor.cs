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
        var admission = state.PreflightCurrentRecordAdmission(record, terminator, fieldSplittingMode);
        try
        {
            state.Counters.RecordRecordRead();
            state.AdvanceMainInputRecord(terminator);
            state.SetRecordFieldSplittingMode(fieldSplittingMode);
            var result = processRecordAsync(record);
            if (admission.IsActive && !result.IsCompleted)
                return AwaitAdmissionAsync(result, admission);
            admission.Dispose();
            return result;
        }
        catch
        {
            admission.Dispose();
            throw;
        }
    }

    private static async ValueTask<AwkStatementFlow> AwaitAdmissionAsync(
        ValueTask<AwkStatementFlow> result, AwkMemoryMutationAdmission admission)
    {
        using (admission)
            return await result.ConfigureAwait(false);
    }

}

internal enum AwkRecordFieldSplittingMode
{
    Ordinary,
    Paragraph
}
