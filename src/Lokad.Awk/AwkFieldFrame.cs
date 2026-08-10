using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFieldFrame(
    AwkRuntimeCounters counters,
    AwkMemoryGovernor governor,
    int maxFields,
    Action<long> throwIfValueBytesExceeded)
{
    private static readonly IReadOnlyDictionary<int, AwkValue> EmptyFieldValueCache =
        new Dictionary<int, AwkValue>();

    private AwkText _recordText = AwkText.Empty;
    private AwkValue _recordValue = AwkValue.FromText(string.Empty);
    private List<AwkFieldText> _fieldTexts = [];
    private Dictionary<int, AwkValue> _fieldValueCache = [];
    private bool _recordValueIsCurrent = true;
    private bool _recordTextIsCurrent = true;
    private bool _fieldsAreSplit = true;
    // Dirty fields may still slice the previous record. Keep that record alive and remember the
    // separator from the latest field mutation until a consumer actually observes $0.
    private AwkText _recompositionSeparator = AwkText.Empty;
    private int _fieldPayloadByteLength;
    private long _retainedMemoryBytes;

    public AwkText RecordAwkText
    {
        get
        {
            EnsureRecordText();
            return _recordText;
        }
    }

    public AwkValue GetRecordValue(Func<AwkText, AwkValue> createRecordValue)
    {
        EnsureRecordText();
        if (!_recordValueIsCurrent)
        {
            _recordValue = createRecordValue(_recordText);
            _recordValueIsCurrent = true;
        }

        return _recordValue;
    }

    public int GetFieldCount(
        Func<AwkText, IReadOnlyList<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        return _fieldTexts.Count;
    }

    public AwkValue GetFieldValue(
        AwkFieldIndex index,
        Func<AwkText, IReadOnlyList<AwkFieldText>> splitFields,
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
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = false;
        _recordTextIsCurrent = true;
        ClearSplitFields();
    }

    public void SetRecordValue(AwkValue value, Func<AwkValue, AwkText> formatStringByteText)
    {
        var text = formatStringByteText(value);
        ReplaceRetainedMemory(EstimateRetainedBytes(text, [], EmptyFieldValueCache));
        _recordText = text;
        _recordValue = value;
        _recordValueIsCurrent = true;
        _recordTextIsCurrent = true;
        ClearSplitFields();
    }

    public void AssignFieldValue(
        AwkFieldIndex index,
        AwkValue value,
        AwkText outputFieldSeparator,
        Func<AwkValue, AwkText> formatStringByteText,
        Func<AwkText, IReadOnlyList<AwkFieldText>> splitFields)
    {
        if (index.IsCurrentRecord)
        {
            SetRecordValue(value, formatStringByteText);
            return;
        }

        EnsureFields(splitFields);
        ThrowIfFieldCountExceeded(index.Value);
        var fieldIndex = index.Value - 1;
        var oldFieldCount = _fieldTexts.Count;
        var newFieldCount = Math.Max(oldFieldCount, index.Value);
        var oldField = fieldIndex < oldFieldCount ? _fieldTexts[fieldIndex] : AwkFieldText.Empty;
        var newField = AwkFieldText.FromOwned(formatStringByteText(value));
        var newPayloadByteLength = AwkRuntimeMemory.CheckedAdd(
            _fieldPayloadByteLength - oldField.ByteLength(),
            newField.ByteLength());
        var newRetainedBytes = EstimateRetainedBytesAfterFieldAssignment(
            fieldIndex,
            oldField,
            newField,
            oldFieldCount,
            newFieldCount);
        var composedRecordByteLength = EstimateComposedRecordByteLength(
            outputFieldSeparator.ByteLength,
            newFieldCount,
            newPayloadByteLength);
        PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        ReplaceRetainedMemory(newRetainedBytes);

        var addedFields = newFieldCount - oldFieldCount;
        if (addedFields > 0)
        {
            _fieldTexts.EnsureCapacity(newFieldCount);
            while (_fieldTexts.Count < newFieldCount)
                _fieldTexts.Add(AwkFieldText.Empty);
        }

        _fieldTexts[fieldIndex] = newField;
        _fieldValueCache[fieldIndex] = value;
        _fieldPayloadByteLength = newPayloadByteLength;
        MarkRecordForRecomposition(outputFieldSeparator);
        counters.RecordFieldsMaterialized(addedFields);
    }

    public void AssignFieldCount(
        AwkFieldCount count,
        AwkText outputFieldSeparator,
        Func<AwkText, IReadOnlyList<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        ThrowIfFieldCountExceeded(count.Value);
        var oldFieldCount = _fieldTexts.Count;
        var newFieldCount = count.Value;
        var newPayloadByteLength = _fieldPayloadByteLength;
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateReferenceListBytes(oldFieldCount),
            AwkRuntimeMemory.EstimateReferenceListBytes(newFieldCount));
        var newCacheCount = _fieldValueCache.Count;
        if (newFieldCount < oldFieldCount)
        {
            for (var index = newFieldCount; index < oldFieldCount; index++)
            {
                var field = _fieldTexts[index];
                newPayloadByteLength -= field.ByteLength();
                newRetainedBytes = AdjustEstimate(
                    newRetainedBytes,
                    field.RetainedTextByteLength,
                    addedBytes: 0);
                if (!_fieldValueCache.TryGetValue(index, out var cached))
                    continue;

                newCacheCount--;
                newRetainedBytes = AdjustEstimate(
                    newRetainedBytes,
                    EstimateCachedFieldPayloadBytes(field, cached),
                    addedBytes: 0);
            }

            newRetainedBytes = AdjustEstimate(
                newRetainedBytes,
                AwkRuntimeMemory.EstimateDictionaryBufferBytes(_fieldValueCache.Count),
                AwkRuntimeMemory.EstimateDictionaryBufferBytes(newCacheCount));
        }

        var composedRecordByteLength = EstimateComposedRecordByteLength(
            outputFieldSeparator.ByteLength,
            newFieldCount,
            newPayloadByteLength);
        PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        ReplaceRetainedMemory(newRetainedBytes);

        if (newFieldCount < oldFieldCount)
        {
            for (var index = newFieldCount; index < oldFieldCount; index++)
                _fieldValueCache.Remove(index);

            _fieldTexts.RemoveRange(newFieldCount, oldFieldCount - newFieldCount);
        }
        else if (newFieldCount > oldFieldCount)
        {
            _fieldTexts.EnsureCapacity(newFieldCount);
            while (_fieldTexts.Count < newFieldCount)
                _fieldTexts.Add(AwkFieldText.Empty);

            counters.RecordFieldsMaterialized(newFieldCount - oldFieldCount);
        }

        _fieldPayloadByteLength = newPayloadByteLength;
        MarkRecordForRecomposition(outputFieldSeparator);
    }

    private AwkText ComposeRecordText(AwkText outputFieldSeparator, List<AwkFieldText> fieldTexts)
    {
        if (fieldTexts.Count == 0)
            return AwkText.Empty;

        var separator = outputFieldSeparator.AsSpan();
        var byteLength = EstimateComposedRecordByteLength(
            separator.Length,
            fieldTexts.Count,
            _fieldPayloadByteLength);

        if (byteLength == 0)
            return AwkText.Empty;

        var bytes = AwkText.AllocateOwnedBytes(byteLength, governor);
        var offset = 0;
        for (var index = 0; index < fieldTexts.Count; index++)
        {
            if (index > 0 && !separator.IsEmpty)
            {
                separator.CopyTo(bytes.AsSpan(offset));
                offset += separator.Length;
            }

            var field = fieldTexts[index].AsSpan(_recordText);
            field.CopyTo(bytes.AsSpan(offset));
            offset += field.Length;
        }

        return AwkText.FromTrustedOwnedBytes(bytes, counters);
    }

    private void EnsureRecordText()
    {
        if (_recordTextIsCurrent)
            return;

        var recordText = ComposeRecordText(_recompositionSeparator, _fieldTexts);
        var fieldTexts = CreateRecordSlices(_recompositionSeparator.ByteLength, _fieldTexts);
        ReplaceRetainedMemory(EstimateRetainedBytes(recordText, fieldTexts, _fieldValueCache));
        _recordText = recordText;
        _fieldTexts = fieldTexts;
        _recordTextIsCurrent = true;
        _recompositionSeparator = AwkText.Empty;
    }

    private void EnsureFields(
        Func<AwkText, IReadOnlyList<AwkFieldText>> splitFields)
    {
        if (_fieldsAreSplit)
            return;

        counters.RecordFieldSplit();
        using var splitLease = governor.ReserveTransient(EstimateFieldSplitBuffers(_recordText.ByteLength));
        var splitFieldTexts = splitFields(_recordText);
        ThrowIfFieldCountExceeded(splitFieldTexts.Count);
        var fieldTexts = CopyFieldTexts(splitFieldTexts);
        ReplaceRetainedMemory(EstimateRetainedBytes(_recordText, fieldTexts, EmptyFieldValueCache));
        _fieldTexts = fieldTexts;
        _fieldPayloadByteLength = SumFieldPayloadByteLength(fieldTexts);
        counters.RecordFieldsMaterialized(_fieldTexts.Count);
        _fieldValueCache = [];
        _fieldsAreSplit = true;
    }

    private static List<AwkFieldText> CopyFieldTexts(IReadOnlyList<AwkFieldText> source)
    {
        if (source.Count == 0)
            return [];

        var copy = new List<AwkFieldText>(source.Count);
        for (var index = 0; index < source.Count; index++)
            copy.Add(source[index]);

        return copy;
    }

    private static List<AwkFieldText> CreateRecordSlices(
        int separatorByteLength,
        IReadOnlyList<AwkFieldText> source)
    {
        if (source.Count == 0)
            return [];

        var slices = new List<AwkFieldText>(source.Count);
        var offset = 0;
        for (var index = 0; index < source.Count; index++)
        {
            if (index > 0)
                offset = AwkRuntimeMemory.CheckedAdd(offset, separatorByteLength);

            var fieldByteLength = source[index].ByteLength();
            slices.Add(AwkFieldText.FromRecordSlice(offset, fieldByteLength));
            offset = AwkRuntimeMemory.CheckedAdd(offset, fieldByteLength);
        }

        return slices;
    }

    private AwkValue GetCachedFieldValue(int index, Func<AwkText, AwkValue> createFieldValue)
    {
        if (_fieldValueCache.TryGetValue(index, out var cached))
            return cached;

        var fieldText = _fieldTexts[index];
        var value = createFieldValue(fieldText.Materialize(_recordText, counters, governor));
        var oldCacheCount = _fieldValueCache.Count;
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateDictionaryBufferBytes(oldCacheCount),
            AwkRuntimeMemory.EstimateDictionaryBufferBytes(oldCacheCount + 1));
        newRetainedBytes = AwkRuntimeMemory.CheckedAdd(
            newRetainedBytes,
            EstimateCachedFieldPayloadBytes(fieldText, value));
        ReplaceRetainedMemory(newRetainedBytes);
        _fieldValueCache[index] = value;

        return value;
    }

    private void ClearSplitFields()
    {
        _fieldTexts = [];
        _fieldValueCache = [];
        _fieldsAreSplit = false;
        _recompositionSeparator = AwkText.Empty;
        _fieldPayloadByteLength = 0;
    }

    private long EstimateRetainedBytesAfterFieldAssignment(
        int fieldIndex,
        AwkFieldText oldField,
        AwkFieldText newField,
        int oldFieldCount,
        int newFieldCount)
    {
        var bytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateReferenceListBytes(oldFieldCount),
            AwkRuntimeMemory.EstimateReferenceListBytes(newFieldCount));
        bytes = AdjustEstimate(bytes, oldField.RetainedTextByteLength, newField.RetainedTextByteLength);
        if (_fieldValueCache.TryGetValue(fieldIndex, out var oldCachedValue))
        {
            return AdjustEstimate(
                bytes,
                EstimateCachedFieldPayloadBytes(oldField, oldCachedValue),
                EstimateCachedFieldPayloadBytes(newField, oldCachedValue));
        }

        return AdjustEstimate(
            bytes,
            AwkRuntimeMemory.EstimateDictionaryBufferBytes(_fieldValueCache.Count),
            AwkRuntimeMemory.EstimateDictionaryBufferBytes(_fieldValueCache.Count + 1));
    }

    private void PreflightRecomposition(long newRetainedBytes, int recordByteLength)
    {
        throwIfValueBytesExceeded(recordByteLength);
        var retainedIncrease = Math.Max(0, newRetainedBytes - _retainedMemoryBytes);
        // Recomposition allocates its destination while the previous record and dirty fields are live.
        governor.PreflightAllocation(AwkRuntimeMemory.CheckedAdd(
            retainedIncrease,
            AwkRuntimeMemory.EstimateTextBytes(recordByteLength)));
    }

    private void MarkRecordForRecomposition(AwkText outputFieldSeparator)
    {
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = false;
        _recordTextIsCurrent = false;
        _recompositionSeparator = outputFieldSeparator;
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
                    AwkRuntimeMemory.EstimateTextBytes(pair.Value.RetainedTextByteLength));
            }
        }

        return bytes;
    }

    private static long EstimateCachedFieldPayloadBytes(AwkFieldText fieldText, AwkValue value) =>
        fieldText.Kind == AwkFieldTextKind.RecordSlice
            ? AwkRuntimeMemory.EstimateTextBytes(value.RetainedTextByteLength)
            : 0;

    private static long AdjustEstimate(long currentBytes, long removedBytes, long addedBytes)
    {
        if (removedBytes > currentBytes)
            throw new InvalidOperationException("The field memory estimate is inconsistent.");

        return AwkRuntimeMemory.CheckedAdd(currentBytes - removedBytes, addedBytes);
    }

    private static int SumFieldPayloadByteLength(IReadOnlyList<AwkFieldText> fieldTexts)
    {
        var byteLength = 0;
        for (var index = 0; index < fieldTexts.Count; index++)
            byteLength = AwkRuntimeMemory.CheckedAdd(byteLength, fieldTexts[index].ByteLength());

        return byteLength;
    }

    private static long EstimateFieldSplitBuffers(int recordByteLength)
        => AwkRuntimeMemory.EstimateSplitBufferBytes(
            recordByteLength,
            fieldListCount: 2,
            includeSeparatorBoundaryList: false);

    private static int EstimateComposedRecordByteLength(
        int separatorLength,
        int fieldCount,
        int fieldPayloadByteLength) =>
        AwkRuntimeMemory.CheckedAdd(
            fieldPayloadByteLength,
            AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedMultiply(
                separatorLength,
                Math.Max(0, fieldCount - 1))));

    private void ThrowIfFieldCountExceeded(int count)
    {
        if (count > maxFields)
            throw new AwkRuntimeException(AwkMessages.ErrorArrayElementLimit);
    }

}
