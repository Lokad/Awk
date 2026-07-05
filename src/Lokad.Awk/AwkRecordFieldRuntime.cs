using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkRecordFieldRuntime(
    AwkFieldSplitter fieldSplitter,
    Func<string, AwkValue> createStringValue,
    Func<AwkText, AwkValue> createByteTextValue,
    Func<string, AwkValue> createNumericStringValue,
    Func<AwkText, AwkValue> createNumericByteText,
    Func<string> resolveOutputFieldSeparator,
    AwkRuntimeCounters counters,
    AwkMemoryGovernor governor,
    int maxFields)
{
    private readonly AwkFieldFrame _frame = new(counters, governor, maxFields);

    public string CurrentRecordString => _frame.RecordText;

    public AwkText CurrentRecordByteText => _frame.RecordAwkText;

    public IReadOnlyList<string> CurrentFieldStrings =>
        _frame.GetFields(fieldSplitter.SplitRecordFields);

    public int FieldCount =>
        _frame.GetFieldCount(fieldSplitter.SplitRecordFields);

    public AwkValue CurrentRecordValue => _frame.GetRecordValue(createNumericByteText);

    public AwkValue GetFieldValue(int index) =>
        GetFieldValue(new AwkFieldIndex(index));

    public AwkValue GetFieldValue(AwkFieldIndex index) =>
        _frame.GetFieldValue(index, fieldSplitter.SplitRecordFields, createNumericByteText);

    public void SetCurrentRecord(string line)
    {
        SetCurrentRecordValue(createNumericStringValue(line));
    }

    public void SetCurrentRecord(AwkText line)
    {
        _frame.SetRecordText(line);
    }

    public void SetCurrentRecordValue(AwkValue value)
    {
        _frame.SetRecordValue(value);
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
            fieldSplitter.SplitRecordFields,
            createByteTextValue);
    }

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
            fieldSplitter.SplitRecordFields,
            createByteTextValue);
    }
}
