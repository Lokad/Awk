using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFieldFrame(AwkRuntimeCounters counters, AwkMemoryGovernor governor, int maxFields)
{
    private static readonly IReadOnlyDictionary<int, AwkValue> EmptyFieldValueCache =
        new Dictionary<int, AwkValue>();

    private AwkText _recordText = AwkText.Empty;
    private AwkValue _recordValue = AwkValue.FromText(string.Empty);
    private string _recordString = string.Empty;
    private IReadOnlyList<AwkFieldText> _fieldTexts = [];
    private List<string> _fields = [];
    private Dictionary<int, AwkValue> _fieldValueCache = [];
    private bool _recordStringIsCurrent = true;
    private bool _recordValueIsCurrent = true;
    private bool _fieldsAreSplit = true;
    private bool _fieldStringsAreCurrent = true;
    private long _retainedMemoryBytes;

    public string RecordText
    {
        get
        {
            EnsureRecordString();
            return _recordString;
        }
    }

    public AwkText RecordAwkText => _recordText;

    public AwkValue GetRecordValue(Func<AwkText, AwkValue> createRecordValue)
    {
        if (!_recordValueIsCurrent)
        {
            _recordValue = createRecordValue(_recordText);
            _recordValueIsCurrent = true;
        }

        return _recordValue;
    }

    public IReadOnlyList<string> GetFields(
        Func<AwkText, AwkRuntimeCounters, IReadOnlyList<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        EnsureFieldStrings();
        return _fields;
    }

    public int GetFieldCount(
        Func<AwkText, AwkRuntimeCounters, IReadOnlyList<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        return _fieldTexts.Count;
    }

    public AwkValue GetFieldValue(
        AwkFieldIndex index,
        Func<AwkText, AwkRuntimeCounters, IReadOnlyList<AwkFieldText>> splitFields,
        Func<AwkText, AwkValue> createFieldValue)
    {
        EnsureFields(splitFields);
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return AwkValue.FromText(string.Empty);

        return GetCachedFieldValue(index.Value - 1, createFieldValue);
    }

    public void SetRecordText(AwkText text)
    {
        ReplaceRetainedMemory(EstimateRetainedBytes(text, [], EmptyFieldValueCache));
        _recordText = text;
        _recordString = string.Empty;
        _recordStringIsCurrent = false;
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = false;
        ClearSplitFields();
    }

    public void SetRecordValue(AwkValue value)
    {
        ReplaceRetainedMemory(EstimateRetainedBytes(value.ByteText, [], EmptyFieldValueCache));
        _recordString = string.Empty;
        _recordStringIsCurrent = false;
        _recordText = value.ByteText;
        _recordValue = value;
        _recordValueIsCurrent = true;
        ClearSplitFields();
    }

    public void AssignFieldValue(
        AwkFieldIndex index,
        AwkValue value,
        string outputFieldSeparator,
        Func<AwkText, AwkRuntimeCounters, IReadOnlyList<AwkFieldText>> splitFields,
        Func<AwkText, AwkValue> createRecordValue)
    {
        if (index.IsCurrentRecord)
        {
            SetRecordValue(value);
            return;
        }

        EnsureFields(splitFields);
        ThrowIfFieldCountExceeded(index.Value);
        var addedFields = 0;
        var newFieldTexts = MaterializeFieldTexts();
        while (newFieldTexts.Count < index.Value)
        {
            newFieldTexts.Add(AwkFieldText.Empty);
            addedFields++;
        }

        newFieldTexts[index.Value - 1] = AwkFieldText.FromOwned(value.ByteText);
        var newRecordText = ComposeRecordText(outputFieldSeparator, newFieldTexts);
        var newFieldValueCache = new Dictionary<int, AwkValue> { [index.Value - 1] = value };
        ReplaceRetainedMemory(EstimateRetainedBytes(newRecordText, newFieldTexts, newFieldValueCache));

        counters.RecordFieldsMaterialized(addedFields);
        _fieldTexts = newFieldTexts;
        _fieldValueCache = newFieldValueCache;
        _fieldStringsAreCurrent = false;
        SetRecomposedRecord(newRecordText, createRecordValue);
    }

    public void AssignFieldCount(
        AwkFieldCount count,
        string outputFieldSeparator,
        Func<AwkText, AwkRuntimeCounters, IReadOnlyList<AwkFieldText>> splitFields,
        Func<AwkText, AwkValue> createRecordValue)
    {
        EnsureFields(splitFields);
        ThrowIfFieldCountExceeded(count.Value);
        var newFieldTexts = MaterializeFieldTexts();
        if (newFieldTexts.Count > count.Value)
        {
            newFieldTexts.RemoveRange(count.Value, newFieldTexts.Count - count.Value);
        }
        else
        {
            var addedFields = 0;
            while (newFieldTexts.Count < count.Value)
            {
                newFieldTexts.Add(AwkFieldText.Empty);
                addedFields++;
            }

            counters.RecordFieldsMaterialized(addedFields);
        }

        var newRecordText = ComposeRecordText(outputFieldSeparator, newFieldTexts);
        ReplaceRetainedMemory(EstimateRetainedBytes(newRecordText, newFieldTexts, EmptyFieldValueCache));
        _fieldTexts = newFieldTexts;
        _fieldValueCache = [];
        _fieldStringsAreCurrent = false;
        SetRecomposedRecord(newRecordText, createRecordValue);
    }

    private void SetRecomposedRecord(
        AwkText recordText,
        Func<AwkText, AwkValue> createRecordValue)
    {
        _recordString = string.Empty;
        _recordStringIsCurrent = false;
        _recordText = recordText;
        _recordValue = createRecordValue(_recordText);
        _recordValueIsCurrent = true;
        _fieldsAreSplit = true;
    }

    private AwkText ComposeRecordText(string outputFieldSeparator, IReadOnlyList<AwkFieldText> fieldTexts)
    {
        if (fieldTexts.Count == 0)
            return AwkText.Empty;

        var separator = AwkUtf8Codec.Encode(outputFieldSeparator, counters);
        var byteLength = EstimateComposedRecordByteLength(separator.Length, fieldTexts);

        if (byteLength == 0)
            return AwkText.Empty;

        var bytes = AwkText.AllocateOwnedBytes(byteLength, governor);
        var offset = 0;
        for (var index = 0; index < fieldTexts.Count; index++)
        {
            if (index > 0 && !separator.IsEmpty)
            {
                separator.Span.CopyTo(bytes.AsSpan(offset));
                offset += separator.Length;
            }

            var field = fieldTexts[index].AsSpan(_recordText);
            field.CopyTo(bytes.AsSpan(offset));
            offset += field.Length;
        }

        return AwkText.FromTrustedOwnedBytes(bytes, counters);
    }

    private void EnsureRecordString()
    {
        if (_recordStringIsCurrent)
            return;

        _recordString = _recordText.AsString(counters);
        _recordStringIsCurrent = true;
    }

    private void EnsureFields(
        Func<AwkText, AwkRuntimeCounters, IReadOnlyList<AwkFieldText>> splitFields)
    {
        if (_fieldsAreSplit)
            return;

        counters.RecordFieldSplit();
        using var splitLease = governor.ReserveTransient(EstimateFieldSplitBuffers(_recordText.ByteLength));
        var fieldTexts = splitFields(_recordText, counters);
        ThrowIfFieldCountExceeded(fieldTexts.Count);
        ReplaceRetainedMemory(EstimateRetainedBytes(_recordText, fieldTexts, EmptyFieldValueCache));
        _fieldTexts = fieldTexts;
        counters.RecordFieldsMaterialized(_fieldTexts.Count);
        _fieldValueCache = [];
        _fields = [];
        _fieldStringsAreCurrent = false;
        _fieldsAreSplit = true;
    }

    private List<AwkFieldText> MaterializeFieldTexts()
    {
        if (_fieldTexts.Count == 0)
            return [];

        var materialized = new List<AwkFieldText>(_fieldTexts.Count);
        for (var index = 0; index < _fieldTexts.Count; index++)
        {
            materialized.Add(AwkFieldText.FromOwned(
                _fieldTexts[index].Materialize(_recordText, counters, governor)));
        }

        return materialized;
    }

    private AwkValue GetCachedFieldValue(int index, Func<AwkText, AwkValue> createFieldValue)
    {
        if (_fieldValueCache.TryGetValue(index, out var cached))
            return cached;

        var fieldText = _fieldTexts[index];
        var value = createFieldValue(fieldText.Materialize(_recordText, counters, governor));
        _fieldValueCache[index] = value;
        ReplaceRetainedMemory(EstimateRetainedBytes(_recordText, _fieldTexts, _fieldValueCache));

        return value;
    }

    private void EnsureFieldStrings()
    {
        if (_fieldStringsAreCurrent)
            return;

        _fields = new List<string>(_fieldTexts.Count);
        for (var index = 0; index < _fieldTexts.Count; index++)
        {
            _fields.Add(_fieldTexts[index].AsString(_recordText, counters));
        }

        _fieldStringsAreCurrent = true;
    }

    private void ClearSplitFields()
    {
        _fieldTexts = [];
        _fields = [];
        _fieldValueCache = [];
        _fieldsAreSplit = false;
        _fieldStringsAreCurrent = false;
    }

    private void ReplaceRetainedMemory(long newBytes)
    {
        if (newBytes > _retainedMemoryBytes)
        {
            governor.Reserve(newBytes - _retainedMemoryBytes);
            try
            {
                governor.CommitReserved(newBytes - _retainedMemoryBytes);
            }
            catch
            {
                governor.ReleaseReserved(newBytes - _retainedMemoryBytes);
                throw;
            }
        }
        else if (_retainedMemoryBytes > newBytes)
        {
            governor.ReleaseCommitted(_retainedMemoryBytes - newBytes);
        }

        _retainedMemoryBytes = newBytes;
    }

    private static long EstimateRetainedBytes(
        AwkText recordText,
        IReadOnlyList<AwkFieldText> fieldTexts,
        IReadOnlyDictionary<int, AwkValue> fieldValueCache)
    {
        var bytes = AwkRuntimeMemory.EstimateTextBytes(recordText);
        bytes = AwkRuntimeMemory.CheckedAdd(
            bytes,
            AwkRuntimeMemory.EstimateReferenceListBytes(fieldTexts.Count));
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, EstimateFieldValueCacheBytes(fieldTexts, fieldValueCache));
        for (var index = 0; index < fieldTexts.Count; index++)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, fieldTexts[index].RetainedTextByteLength);
        }

        return bytes;
    }

    private static long EstimateFieldValueCacheBytes(
        IReadOnlyList<AwkFieldText> fieldTexts,
        IReadOnlyDictionary<int, AwkValue> fieldValueCache)
    {
        var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(fieldValueCache.Count);
        foreach (var pair in fieldValueCache)
        {
            if ((uint)pair.Key < (uint)fieldTexts.Count &&
                fieldTexts[pair.Key].Kind == AwkFieldTextKind.RecordSlice)
            {
                bytes = AwkRuntimeMemory.CheckedAdd(
                    bytes,
                    AwkRuntimeMemory.EstimateTextBytes(pair.Value.ByteText));
            }
        }

        return bytes;
    }

    private static long EstimateFieldSplitBuffers(int recordByteLength)
        => AwkRuntimeMemory.EstimateSplitBufferBytes(
            recordByteLength,
            fieldListCount: 2,
            includeSeparatorBoundaryList: false);

    private static int EstimateComposedRecordByteLength(int separatorLength, IReadOnlyList<AwkFieldText> fieldTexts)
    {
        var byteLength = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedMultiply(
            separatorLength,
            Math.Max(0, fieldTexts.Count - 1)));
        for (var index = 0; index < fieldTexts.Count; index++)
        {
            byteLength = AwkRuntimeMemory.CheckedAdd(
                byteLength,
                fieldTexts[index].ByteLength());
        }

        return byteLength;
    }

    private void ThrowIfFieldCountExceeded(int count)
    {
        if (count > maxFields)
            throw new AwkRuntimeException(AwkMessages.ErrorArrayElementLimit);
    }

}
