using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFieldFrame(
    AwkRuntimeCounters counters,
    AwkMemoryGovernor governor,
    int maxFields,
    Action<long> throwIfValueBytesExceeded,
    Func<AwkValue, AwkText> formatStringByteText,
    Action<int> recordByteWork)
{
    private static readonly Dictionary<int, AwkValue> EmptyFieldValueCache = [];

    private AwkText _recordText = AwkText.Empty;
    private AwkValue _recordValue = AwkValue.FromText(string.Empty);
    private AwkValue _fieldCountValue = AwkValue.FromNumber(0);
    private List<AwkFieldText> _fieldTexts = [];
    private Dictionary<int, AwkValue> _fieldValueCache = [];
    private bool _recordValueIsCurrent = true;
    private bool _recordTextIsCurrent = true;
    private bool _fieldsAreSplit = true;
    // Dirty fields may still slice the previous record. Keep that record alive and remember the
    // separator from the latest field mutation until a consumer actually observes $0.
    private AwkText _recompositionSeparator = AwkText.Empty;
    private int _fieldPayloadByteLength;
    private int _numericFieldCount;
    private bool _recordWasRecomposed;
    private long _retainedMemoryBytes;

    public AwkRecordSplitPlan SplitPlan { get; private set; }

    public void ResetRecord()
    {
        ReplaceRetainedMemory(0);
        _recordText = AwkText.Empty;
        _recordWasRecomposed = false;
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = true;
        _recordTextIsCurrent = true;
        ClearSplitFields();
        _fieldsAreSplit = true;
        SplitPlan = default;
    }

    public AwkText RecordAwkText
    {
        get
        {
            EnsureRecordText();
            return _recordText;
        }
    }

    public AwkValue GetRecordValue(
        Func<AwkText, AwkValue> createRecordValue,
        Func<AwkText, AwkValue> createRecomposedRecordValue)
    {
        EnsureRecordText();
        if (!_recordValueIsCurrent)
        {
            _recordValue = (_recordWasRecomposed ? createRecomposedRecordValue : createRecordValue)(_recordText);
            _recordValueIsCurrent = true;
        }

        return _recordValue;
    }

    public int GetFieldCount(
        Func<AwkText, List<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        return _fieldTexts.Count;
    }

    public AwkValue GetFieldCountValue(Func<AwkText, List<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        return _fieldCountValue;
    }

    public AwkValue GetFieldValue(
        AwkFieldIndex index,
        Func<AwkText, List<AwkFieldText>> splitFields,
        Func<AwkText, AwkValue> createFieldValue)
    {
        EnsureFields(splitFields);
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return AwkValue.FromText(string.Empty);

        return GetCachedFieldValue(index.Value - 1, createFieldValue);

        AwkValue GetCachedFieldValue(int index, Func<AwkText, AwkValue> createFieldValue)
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
    }

    public void SetRecordText(AwkText text, AwkRecordSplitPlan splitPlan = default)
    {
        ReplaceRetainedMemory(AwkRuntimeMemory.CheckedAdd(
            EstimateRetainedBytes(text, [], EmptyFieldValueCache),
            splitPlan.RetainedMemoryBytes));
        SplitPlan = splitPlan;
        _recordText = text;
        _recordWasRecomposed = false;
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = false;
        _recordTextIsCurrent = true;
        ClearSplitFields();
    }

    public long EstimateRecordMemoryGrowth(AwkText text, AwkRecordSplitPlan splitPlan) =>
        Math.Max(0, AwkRuntimeMemory.CheckedAdd(
            EstimateRetainedBytes(text, [], EmptyFieldValueCache),
            splitPlan.RetainedMemoryBytes) - _retainedMemoryBytes);

    public void SetRecordValue(
        AwkValue value,
        Func<AwkValue, AwkText> formatStringByteText,
        AwkRecordSplitPlan splitPlan = default)
    {
        var text = formatStringByteText(value);
        ReplaceRetainedMemory(AwkRuntimeMemory.CheckedAdd(
            EstimateRetainedBytes(text, [], EmptyFieldValueCache),
            splitPlan.RetainedMemoryBytes));
        SplitPlan = splitPlan;
        _recordText = text;
        _recordWasRecomposed = false;
        _recordValue = value;
        _recordValueIsCurrent = true;
        _recordTextIsCurrent = true;
        ClearSplitFields();
    }

    public void AssignFieldValue(
        AwkFieldIndex index,
        AwkValue value,
        AwkText outputFieldSeparator,
        Func<AwkText, List<AwkFieldText>> splitFields)
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
        var oldIsNumeric = _fieldValueCache.TryGetValue(fieldIndex, out var oldValue) && oldValue.IsNumberValue;
        // Pure numbers retain no formatted field text. Their conversion belongs to the
        // next actual record reconstruction, under its then-current CONVFMT.
        var newField = value.IsNumberValue ? AwkFieldText.Empty : AwkFieldText.FromOwned(formatStringByteText(value));
        var newPayloadByteLength = AwkRuntimeMemory.CheckedAdd(
            _fieldPayloadByteLength - (oldIsNumeric ? 0 : oldField.ByteLength()),
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
        if (newFieldCount > oldFieldCount)
            newRetainedBytes = AdjustEstimate(newRetainedBytes, _fieldCountValue.RetainedTextMemoryBytes, 0);
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
        _numericFieldCount += (value.IsNumberValue ? 1 : 0) - (oldIsNumeric ? 1 : 0);
        if (newFieldCount > oldFieldCount)
            _fieldCountValue = AwkValue.FromNumber(newFieldCount);
        MarkRecordForRecomposition(outputFieldSeparator);
        counters.RecordFieldsMaterialized(addedFields);

        long EstimateRetainedBytesAfterFieldAssignment(
            int fieldIndex,
            AwkFieldText oldField,
            AwkFieldText newField,
            int oldFieldCount,
            int newFieldCount)
        {
            var bytes = AdjustEstimate(
                _retainedMemoryBytes,
                AwkRuntimeMemory.EstimateFieldTextListBytes(oldFieldCount),
                AwkRuntimeMemory.EstimateFieldTextListBytes(newFieldCount));
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
    }

    public void ExtendFieldCountForReference(
        AwkFieldIndex index,
        Func<AwkText, List<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        ThrowIfFieldCountExceeded(index.Value);
        if (index.Value <= _fieldTexts.Count)
            return;

        // Resolving an lvalue creates absent fields but does not assign them or rebuild $0.
        // If earlier writes left $0 dirty, freeze their result before extending the field list.
        EnsureRecordText();
        var addedFields = index.Value - _fieldTexts.Count;
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateFieldTextListBytes(_fieldTexts.Count),
            AwkRuntimeMemory.EstimateFieldTextListBytes(index.Value));
        newRetainedBytes = AdjustEstimate(newRetainedBytes, _fieldCountValue.RetainedTextMemoryBytes, 0);
        ReplaceRetainedMemory(newRetainedBytes);
        _fieldTexts.EnsureCapacity(index.Value);
        while (_fieldTexts.Count < index.Value)
            _fieldTexts.Add(AwkFieldText.Empty);

        _fieldCountValue = AwkValue.FromNumber(index.Value);
        counters.RecordFieldsMaterialized(addedFields);
    }

    public void AssignFieldCount(
        AwkFieldCount count,
        AwkText outputFieldSeparator,
        Func<AwkText, List<AwkFieldText>> splitFields) =>
        AssignFieldCount(count, AwkValue.FromNumber(count.Value), outputFieldSeparator, splitFields);

    public void AssignFieldCount(
        AwkFieldCount count,
        AwkValue value,
        AwkText outputFieldSeparator,
        Func<AwkText, List<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        ThrowIfFieldCountExceeded(count.Value);
        var oldFieldCount = _fieldTexts.Count;
        var newFieldCount = count.Value;
        var newPayloadByteLength = _fieldPayloadByteLength;
        var newNumericFieldCount = _numericFieldCount;
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateFieldTextListBytes(oldFieldCount),
            AwkRuntimeMemory.EstimateFieldTextListBytes(newFieldCount));
        newRetainedBytes = AdjustEstimate(newRetainedBytes,
            _fieldCountValue.RetainedTextMemoryBytes, value.RetainedTextMemoryBytes);
        var newCacheCount = _fieldValueCache.Count;
        if (newFieldCount < oldFieldCount)
        {
            for (var index = newFieldCount; index < oldFieldCount; index++)
            {
                var field = _fieldTexts[index];
                var numeric = _fieldValueCache.TryGetValue(index, out var cached) && cached.IsNumberValue;
                newPayloadByteLength -= numeric ? 0 : field.ByteLength();
                newNumericFieldCount -= numeric ? 1 : 0;
                newRetainedBytes = AdjustEstimate(
                    newRetainedBytes,
                    field.RetainedTextByteLength,
                    addedBytes: 0);
                if (!_fieldValueCache.TryGetValue(index, out cached))
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
        _numericFieldCount = newNumericFieldCount;
        _fieldCountValue = value;
        MarkRecordForRecomposition(outputFieldSeparator);
    }

    private void EnsureRecordText()
    {
        static List<AwkFieldText> CreateRecordSlices(
            int separatorByteLength,
            List<AwkFieldText> source)
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

        AwkText ComposeRecordText(AwkText outputFieldSeparator, List<AwkFieldText> fieldTexts, int payloadByteLength)
        {
            if (fieldTexts.Count == 0)
                return AwkText.Empty;

            var separator = outputFieldSeparator.AsSpan();
            var byteLength = EstimateComposedRecordByteLength(
                separator.Length,
                fieldTexts.Count,
                payloadByteLength);

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

        if (_recordTextIsCurrent)
            return;

        using var buffers = governor.ReserveTransient(AwkRuntimeMemory.CheckedMultiply(
            AwkRuntimeMemory.EstimateFieldTextListBytes(_fieldTexts.Count), _numericFieldCount > 0 ? 2 : 1));
        long formattedBytes = 0;
        try
        {
            var fields = _fieldTexts;
            var payloadByteLength = _fieldPayloadByteLength;
            if (_numericFieldCount > 0)
            {
                fields = new List<AwkFieldText>(_fieldTexts.Count);
                for (var index = 0; index < _fieldTexts.Count; index++)
                {
                    var field = _fieldTexts[index];
                    if (_fieldValueCache.TryGetValue(index, out var value) && value.IsNumberValue)
                    {
                        recordByteWork(0);
                        var text = formatStringByteText(value);
                        recordByteWork(text.ByteLength);
                        var bytes = text.EstimatedRetainedMemoryBytes;
                        var next = AwkRuntimeMemory.CheckedAdd(formattedBytes, bytes);
                        governor.Reserve(bytes);
                        try { governor.CommitReserved(bytes); }
                        catch { governor.ReleaseReserved(bytes); throw; }
                        formattedBytes = next;
                        payloadByteLength = AwkRuntimeMemory.CheckedAdd(payloadByteLength, text.ByteLength);
                        field = AwkFieldText.FromOwned(text);
                    }
                    fields.Add(field);
                }
            }
            var byteLength = EstimateComposedRecordByteLength(_recompositionSeparator.ByteLength, fields.Count, payloadByteLength);
            throwIfValueBytesExceeded(byteLength);
            var fieldTexts = CreateRecordSlices(_recompositionSeparator.ByteLength, fields);
            var newRetainedBytes = EstimateRetainedBytes(
                AwkRuntimeMemory.EstimateTextBytes(byteLength), fieldTexts, _fieldValueCache, _fieldCountValue);
            PreflightRecomposition(newRetainedBytes, byteLength);
            var recordText = ComposeRecordText(_recompositionSeparator, fields, payloadByteLength);
            ReplaceRetainedMemory(newRetainedBytes);
            _recordText = recordText;
            _fieldTexts = fieldTexts;
            _recordTextIsCurrent = true;
            _recordWasRecomposed = true;
            _recompositionSeparator = AwkText.Empty;
        }
        finally
        {
            governor.ReleaseCommitted(formattedBytes);
        }
    }

    public void PrepareOutputFieldSeparatorAssignment()
    {
        // GNU's OFS setter freezes a dirty record before changing the separator. Only
        // numeric fields need that checkpoint: ordinary text already has fixed bytes.
        if (_numericFieldCount > 0)
            EnsureRecordText();
    }

    private void EnsureFields(
        Func<AwkText, List<AwkFieldText>> splitFields)
    {
        static int SumFieldPayloadByteLength(List<AwkFieldText> fieldTexts)
        {
            var byteLength = 0;
            for (var index = 0; index < fieldTexts.Count; index++)
                byteLength = AwkRuntimeMemory.CheckedAdd(byteLength, fieldTexts[index].ByteLength());

            return byteLength;
        }

        if (_fieldsAreSplit)
            return;

        counters.RecordFieldSplit();
        using var splitLease = governor.ReserveTransient(
            AwkRuntimeMemory.EstimateSplitBufferBytes(
                _recordText.ByteLength,
                fieldListCount: 2,
                AwkSeparatorCaptureMode.Omit));
        var fieldTexts = splitFields(_recordText);
        ThrowIfFieldCountExceeded(fieldTexts.Count);
        ReplaceRetainedMemory(EstimateRetainedBytes(_recordText, fieldTexts, EmptyFieldValueCache));
        _fieldTexts = fieldTexts;
        _fieldCountValue = AwkValue.FromNumber(fieldTexts.Count);
        _fieldPayloadByteLength = SumFieldPayloadByteLength(fieldTexts);
        counters.RecordFieldsMaterialized(_fieldTexts.Count);
        _fieldValueCache = [];
        _fieldsAreSplit = true;
        SplitPlan = default;
    }

    private void ClearSplitFields()
    {
        _fieldCountValue = AwkValue.FromNumber(0);
        _fieldTexts = [];
        _fieldValueCache = [];
        _fieldsAreSplit = false;
        _recompositionSeparator = AwkText.Empty;
        _fieldPayloadByteLength = 0;
        _numericFieldCount = 0;
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
        List<AwkFieldText> fieldTexts,
        Dictionary<int, AwkValue> fieldValueCache,
        AwkValue fieldCountValue = default)
        => EstimateRetainedBytes(AwkRuntimeMemory.EstimateTextBytes(recordText), fieldTexts, fieldValueCache, fieldCountValue);

    private static long EstimateRetainedBytes(
        long recordTextBytes,
        List<AwkFieldText> fieldTexts,
        Dictionary<int, AwkValue> fieldValueCache,
        AwkValue fieldCountValue)
    {
        static long EstimateFieldValueCacheBytes(
            List<AwkFieldText> fieldTexts,
            Dictionary<int, AwkValue> fieldValueCache)
        {
            var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(fieldValueCache.Count);
            foreach (var pair in fieldValueCache)
            {
                if ((uint)pair.Key < (uint)fieldTexts.Count &&
                    fieldTexts[pair.Key].Kind == AwkFieldTextKind.RecordSlice)
                {
                    bytes = AwkRuntimeMemory.CheckedAdd(
                        bytes,
                        pair.Value.RetainedTextMemoryBytes);
                }
            }

            return bytes;
        }

        var bytes = AwkRuntimeMemory.CheckedAdd(
            recordTextBytes, fieldCountValue.RetainedTextMemoryBytes);
        bytes = AwkRuntimeMemory.CheckedAdd(
            bytes,
            AwkRuntimeMemory.EstimateFieldTextListBytes(fieldTexts.Count));
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, EstimateFieldValueCacheBytes(fieldTexts, fieldValueCache));
        for (var index = 0; index < fieldTexts.Count; index++)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, fieldTexts[index].RetainedTextByteLength);
        }

        return bytes;
    }

    private static long EstimateCachedFieldPayloadBytes(AwkFieldText fieldText, AwkValue value) =>
        fieldText.Kind == AwkFieldTextKind.RecordSlice
            ? value.RetainedTextMemoryBytes
            : 0;

    private static long AdjustEstimate(long currentBytes, long removedBytes, long addedBytes)
    {
        if (removedBytes > currentBytes)
            throw new InvalidOperationException("The field memory estimate is inconsistent.");

        return AwkRuntimeMemory.CheckedAdd(currentBytes - removedBytes, addedBytes);
    }

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
