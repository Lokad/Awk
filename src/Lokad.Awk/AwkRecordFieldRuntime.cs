using System;

namespace Lokad.Awk;

internal sealed class AwkRecordFieldRuntime(
    AwkFieldSplitter fieldSplitter,
    Func<string, AwkValue> createStringValue,
    Func<AwkText, AwkValue> createStringByteText,
    Func<AwkText, AwkValue> createNumericByteText,
    Func<AwkValue, AwkText> formatStringByteText,
    Func<AwkText> resolveOutputFieldSeparator,
    Func<AwkRecordFieldSplittingMode> resolveAssignedRecordMode,
    AwkRuntimeCounters counters,
    AwkMemoryGovernor governor,
    int maxFields,
    Action<long> throwIfValueBytesExceeded,
    Action<int> recordByteWork,
    Action? checkpoint = null)
{
    private readonly AwkFieldFrame _frame = new(
        counters,
        governor,
        maxFields,
        throwIfValueBytesExceeded,
        formatStringByteText,
        recordByteWork,
        checkpoint);

    private Func<AwkText, List<AwkFieldText>>? _splitCurrentFields;

    // Reuse the callback on every field/NF read. Resolve the current plan when
    // invoked so lazy splitting still observes the plan captured for that record.
    private Func<AwkText, List<AwkFieldText>> SplitFields =>
        _splitCurrentFields ??= SplitCurrentFields;

    public AwkText CurrentRecordByteText => _frame.RecordAwkText;

    public int FieldCount =>
        _frame.GetFieldCount(SplitFields);

    public AwkValue FieldCountValue => _frame.GetFieldCountValue(SplitFields);

    public AwkValue CurrentRecordValue => _frame.GetRecordValue(createNumericByteText, createStringByteText);

    public void PrepareOutputFieldSeparatorAssignment() => _frame.PrepareOutputFieldSeparatorAssignment();

    public void ResetRecord() => _frame.ResetRecord();

    public AwkValue GetFieldValue(int index) =>
        GetFieldValue(new AwkFieldIndex(index));

    public AwkValue GetFieldValue(AwkFieldIndex index) =>
        _frame.GetFieldValue(index, SplitFields, createNumericByteText);

    public void SetCurrentRecord(AwkText line)
    {
        _frame.SetRecordText(line, fieldSplitter.CaptureRecordPlan());
    }

    public long EstimateRecordMemoryGrowth(AwkText text, AwkRecordFieldSplittingMode mode) =>
        _frame.EstimateRecordMemoryGrowth(text, fieldSplitter.CaptureRecordPlan(mode));

    public void SetCurrentRecordValue(AwkValue value)
    {
        _frame.SetRecordValue(value, formatStringByteText,
            fieldSplitter.CaptureRecordPlan(resolveAssignedRecordMode()));
    }

    public void AssignField(int index, string value)
    {
        AssignFieldValue(index, createStringValue(value));
    }

    public void AssignFieldValue(int index, AwkValue value)
    {
        if (index == 0)
        {
            SetCurrentRecordValue(value);
            return;
        }

        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        AssignFieldValue(new AwkFieldIndex(index), value);
    }

    public void AssignFieldValue(AwkFieldIndex index, AwkValue value)
    {
        if (index.IsCurrentRecord)
        {
            SetCurrentRecordValue(value);
            return;
        }

        _frame.AssignFieldValue(
            index,
            value,
            resolveOutputFieldSeparator(),
            SplitFields);
    }

    public void ExtendFieldCountForReference(AwkFieldIndex index) =>
        _frame.ExtendFieldCountForReference(index, SplitFields);

    public void AssignFieldCount(int count)
    {
        if (count < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        AssignFieldCount(new AwkFieldCount(count));
    }

    public void AssignFieldCount(AwkFieldCount count)
    {
        _frame.AssignFieldCount(
            count,
            resolveOutputFieldSeparator(),
            SplitFields);
    }

    public void AssignFieldCountValue(AwkValue value)
    {
        var count = Math.Truncate(value.AsNumber(checkpoint));
        if (count < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (!double.IsFinite(count) || count > maxFields)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        _frame.AssignFieldCount(new AwkFieldCount((int)count), value,
            resolveOutputFieldSeparator(), SplitFields);
    }

    private List<AwkFieldText> SplitCurrentFields(AwkText text) =>
        fieldSplitter.SplitRecordFields(text, _frame.SplitPlan);
}
