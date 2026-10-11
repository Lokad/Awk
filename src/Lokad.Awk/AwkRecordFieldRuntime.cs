using System;

namespace Lokad.Awk;

internal sealed class AwkRecordFieldRuntime
{
    private readonly AwkFieldSplitter _fieldSplitter;
    private readonly Func<string, AwkValue> _createStringValue;
    private readonly Func<AwkText, AwkValue> _createStringByteText;
    private readonly Func<AwkText, AwkValue> _createNumericByteText;
    private readonly Func<AwkValue, AwkText> _formatStringByteText;
    private readonly Func<AwkText> _resolveOutputFieldSeparator;
    private readonly Func<AwkRecordFieldSplittingMode> _resolveAssignedRecordMode;
    private readonly int _maxFields;
    private readonly Action? _checkpoint;
    private readonly AwkFieldFrame _frame;
    private readonly Func<AwkText, List<AwkFieldText>> _splitCurrentFields;

    public AwkRecordFieldRuntime(
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
        _fieldSplitter = fieldSplitter;
        _createStringValue = createStringValue;
        _createStringByteText = createStringByteText;
        _createNumericByteText = createNumericByteText;
        _formatStringByteText = formatStringByteText;
        _resolveOutputFieldSeparator = resolveOutputFieldSeparator;
        _resolveAssignedRecordMode = resolveAssignedRecordMode;
        _maxFields = maxFields;
        _checkpoint = checkpoint;
        _frame = new(counters, governor, maxFields, throwIfValueBytesExceeded,
            formatStringByteText, recordByteWork, checkpoint);
        // Bind once; the callback reads the current record's immutable split plan.
        _splitCurrentFields = SplitCurrentFields;
    }

    public AwkText CurrentRecordByteText => _frame.RecordAwkText;

    public int FieldCount =>
        _frame.GetFieldCount(_splitCurrentFields);

    public AwkValue FieldCountValue => _frame.GetFieldCountValue(_splitCurrentFields);

    public AwkValue CurrentRecordValue => _frame.GetRecordValue(_createNumericByteText, _createStringByteText);

    public void PrepareOutputFieldSeparatorAssignment() => _frame.PrepareOutputFieldSeparatorAssignment();

    public void ResetRecord() => _frame.ResetRecord();

    public AwkValue GetFieldValue(int index) =>
        GetFieldValue(new AwkFieldIndex(index));

    public AwkValue GetFieldValue(AwkFieldIndex index) =>
        _frame.GetFieldValue(index, _splitCurrentFields, _createNumericByteText);

    public void SetCurrentRecord(AwkText line)
    {
        _frame.SetRecordText(line, _fieldSplitter.CaptureRecordPlan());
    }

    public long EstimateRecordMemoryGrowth(AwkText text, AwkRecordFieldSplittingMode mode) =>
        _frame.EstimateRecordMemoryGrowth(text, _fieldSplitter.CaptureRecordPlan(mode));

    public void SetCurrentRecordValue(AwkValue value)
    {
        _frame.SetRecordValue(value, _formatStringByteText,
            _fieldSplitter.CaptureRecordPlan(_resolveAssignedRecordMode()));
    }

    public void AssignField(int index, string value)
    {
        AssignFieldValue(index, _createStringValue(value));
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
            _resolveOutputFieldSeparator(),
            _splitCurrentFields);
    }

    public void ExtendFieldCountForReference(AwkFieldIndex index) =>
        _frame.ExtendFieldCountForReference(index, _splitCurrentFields);

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
            _resolveOutputFieldSeparator(),
            _splitCurrentFields);
    }

    public void AssignFieldCountValue(AwkValue value)
    {
        var count = Math.Truncate(value.AsNumber(_checkpoint));
        if (count < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (!double.IsFinite(count) || count > _maxFields)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        _frame.AssignFieldCount(new AwkFieldCount((int)count), value,
            _resolveOutputFieldSeparator(), _splitCurrentFields);
    }

    private List<AwkFieldText> SplitCurrentFields(AwkText text) =>
        _fieldSplitter.SplitRecordFields(text, _frame.SplitPlan);
}
