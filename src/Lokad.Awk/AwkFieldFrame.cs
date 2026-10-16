using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFieldFrame(
    AwkRuntimeCounters counters,
    AwkMemoryAccounting governor,
    int maxFields,
    Action<long> throwIfValueBytesExceeded,
    Func<AwkValue, AwkText> formatStringByteText,
    Action<int> recordByteWork,
    Action? checkpoint = null,
    AwkFieldSplitter? fieldSplitter = null)
{
    private const int MutationCheckpointFields = 1024;
    // Tiny records are cheaper to finish once than to stage successive prefixes.
    private const int IncrementalRecordBytes = 128;
    private const int MaxReusableFieldCacheCapacity = 128;
    // A rough backing-capacity charge covers this dictionary and its two small arrays.
    private const int MaxGovernedReusableFieldCacheCapacity = 3;
    private const int SmallFieldCacheBufferBytes = 320;
    private static readonly Dictionary<int, AwkValue> EmptyFieldValueCache = [];
    // Used only by estimates; never published as a mutable field list.
    private static readonly List<AwkFieldText> EmptyFieldTexts = [];

    private AwkText _recordText = AwkText.Empty;
    private AwkValue _recordValue = AwkValue.FromText(string.Empty);
    private AwkValue _fieldCountValue = AwkValue.FromNumber(0);
    private List<AwkFieldText> _fieldTexts = [];
    private Dictionary<int, AwkValue> _fieldValueCache = [];
    private bool _recordValueIsCurrent = true;
    private bool _recordTextIsCurrent = true;
    private bool _fieldsAreSplit = true;
    private int _splitPosition;
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
        ClearSplitFields(releaseCache: governor.IsEnabled);
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
        EnsureField(index, splitFields);
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return AwkValue.FromText(string.Empty);

        return GetCachedFieldValue(index.Value - 1, createFieldValue);

        AwkValue GetCachedFieldValue(int index, Func<AwkText, AwkValue> createFieldValue)
        {
            if (_fieldValueCache.TryGetValue(index, out var cached))
                return cached;

            var fieldText = _fieldTexts[index];
            var value = createFieldValue(fieldText.Materialize(_recordText, counters, governor, checkpoint));
            var newRetainedBytes = 0L;
            if (governor.IsEnabled)
            {
                var oldCacheCount = _fieldValueCache.Count;
                newRetainedBytes = AdjustEstimate(
                    _retainedMemoryBytes,
                    EstimateFieldCacheBufferBytes(oldCacheCount),
                    EstimateFieldCacheBufferBytes(oldCacheCount + 1));
                newRetainedBytes = AwkRuntimeMemory.CheckedAdd(
                    newRetainedBytes,
                    EstimateCachedFieldPayloadBytes(fieldText, value));
            }
            checkpoint?.Invoke();
            ReplaceRetainedMemory(newRetainedBytes);
            _fieldValueCache[index] = value;

            return value;
        }
    }

    public bool TryGetUnmaterializedFieldByteLength(
        AwkFieldIndex index, Func<AwkText, List<AwkFieldText>> splitFields, bool byteMode, out int length)
    {
        EnsureField(index, splitFields);
        length = 0;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return true;

        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (_fieldValueCache.ContainsKey(fieldIndex) || field.Kind == AwkFieldTextKind.Owned)
            return false;

        if (!byteMode && field.Kind == AwkFieldTextKind.RecordSlice)
        {
            // The text caches this fact: repeated length reads cannot rescan the record.
            var ascii = _recordText.IsAscii(out var inspectedBytes, checkpoint);
            recordByteWork(inspectedBytes);
            if (!ascii)
                return false;
        }

        length = field.ByteLength();
        throwIfValueBytesExceeded(length);
        return true;
    }

    public bool TryGetUnmaterializedFieldBytes(
        AwkFieldIndex index, Func<AwkText, List<AwkFieldText>> splitFields, out ReadOnlySpan<byte> bytes)
    {
        var borrowed = TryGetUnmaterializedFieldText(index, splitFields, out var field, out var record);
        bytes = borrowed ? field.AsSpan(record, checkpoint) : default;
        return borrowed;
    }

    public bool TryGetUnmaterializedFieldText(
        AwkFieldIndex index, Func<AwkText, List<AwkFieldText>> splitFields,
        out AwkFieldText field, out AwkText record)
    {
        EnsureField(index, splitFields);
        field = AwkFieldText.Empty;
        record = _recordText;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return true;

        var fieldIndex = index.Value - 1;
        var text = _fieldTexts[fieldIndex];
        if (_fieldValueCache.ContainsKey(fieldIndex) || text.Kind == AwkFieldTextKind.Owned)
            return false;

        throwIfValueBytesExceeded(text.ByteLength());
        field = text;
        return true;
    }

    public void SetRecordText(AwkText text, AwkRecordSplitPlan splitPlan = default)
    {
        ReplaceRetainedMemory(AwkRuntimeMemory.CheckedAdd(
            EstimateRetainedBytes(text, EmptyFieldTexts, EmptyFieldValueCache),
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
            EstimateRetainedBytes(text, EmptyFieldTexts, EmptyFieldValueCache),
            splitPlan.RetainedMemoryBytes) - _retainedMemoryBytes);

    public void SetRecordValue(
        AwkValue value,
        Func<AwkValue, AwkText> formatStringByteText,
        AwkRecordSplitPlan splitPlan = default)
    {
        var text = formatStringByteText(value);
        ReplaceRetainedMemory(AwkRuntimeMemory.CheckedAdd(
            EstimateRetainedBytes(text, EmptyFieldTexts, EmptyFieldValueCache),
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
        using var recompositionPeak = PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        var addedFields = newFieldCount - oldFieldCount;
        if (addedFields >= MutationCheckpointFields)
        {
            var stagedBytes = AwkRuntimeMemory.EstimateFieldTextListBytes(newFieldCount);
            using var staging = governor.ReserveTransient(stagedBytes);
            var fields = StageFieldTexts(newFieldCount);
            fields[fieldIndex] = newField;
            PublishStagedFields(fields, _fieldValueCache, newRetainedBytes, staging, stagedBytes);
        }
        else
        {
            checkpoint?.Invoke();
            ReplaceRetainedMemory(newRetainedBytes);
            if (addedFields > 0)
            {
                _fieldTexts.EnsureCapacity(newFieldCount);
                while (_fieldTexts.Count < newFieldCount)
                    _fieldTexts.Add(AwkFieldText.Empty);
            }
            _fieldTexts[fieldIndex] = newField;
        }
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
                EstimateFieldCacheBufferBytes(_fieldValueCache.Count),
                EstimateFieldCacheBufferBytes(_fieldValueCache.Count + 1));
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
        if (addedFields >= MutationCheckpointFields)
        {
            var stagedBytes = AwkRuntimeMemory.EstimateFieldTextListBytes(index.Value);
            using var staging = governor.ReserveTransient(stagedBytes);
            var fields = StageFieldTexts(index.Value);
            PublishStagedFields(fields, _fieldValueCache, newRetainedBytes, staging, stagedBytes);
        }
        else
        {
            checkpoint?.Invoke();
            ReplaceRetainedMemory(newRetainedBytes);
            _fieldTexts.EnsureCapacity(index.Value);
            while (_fieldTexts.Count < index.Value)
                _fieldTexts.Add(AwkFieldText.Empty);
        }

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
                if (oldFieldCount - newFieldCount >= MutationCheckpointFields &&
                    ((index - newFieldCount) & (MutationCheckpointFields - 1)) == 0)
                    checkpoint?.Invoke();
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
                EstimateFieldCacheBufferBytes(_fieldValueCache.Count),
                EstimateFieldCacheBufferBytes(newCacheCount));
        }

        var composedRecordByteLength = EstimateComposedRecordByteLength(
            outputFieldSeparator.ByteLength,
            newFieldCount,
            newPayloadByteLength);
        using var recompositionPeak = PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        if (Math.Abs(newFieldCount - oldFieldCount) >= MutationCheckpointFields)
        {
            var stagedBytes = AwkRuntimeMemory.EstimateFieldTextListBytes(newFieldCount);
            if (newFieldCount < oldFieldCount)
                stagedBytes = AwkRuntimeMemory.CheckedAdd(stagedBytes,
                    AwkRuntimeMemory.EstimateDictionaryBufferBytes(newCacheCount));
            using var staging = governor.ReserveTransient(stagedBytes);
            var fields = StageFieldTexts(newFieldCount);
            var cache = newFieldCount < oldFieldCount
                ? StageFieldValueCache(newFieldCount, newCacheCount)
                : _fieldValueCache;
            PublishStagedFields(fields, cache, newRetainedBytes, staging, stagedBytes);
        }
        else
        {
            checkpoint?.Invoke();
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
            }
        }
        if (newFieldCount > oldFieldCount)
            counters.RecordFieldsMaterialized(newFieldCount - oldFieldCount);

        _fieldPayloadByteLength = newPayloadByteLength;
        _numericFieldCount = newNumericFieldCount;
        _fieldCountValue = value;
        MarkRecordForRecomposition(outputFieldSeparator);
    }

    private List<AwkFieldText> StageFieldTexts(int count)
    {
        checkpoint?.Invoke();
        var fields = new List<AwkFieldText>(count);
        for (var index = 0; index < count; index++)
        {
            if ((index & (MutationCheckpointFields - 1)) == 0)
                checkpoint?.Invoke();
            fields.Add(index < _fieldTexts.Count ? _fieldTexts[index] : AwkFieldText.Empty);
        }
        checkpoint?.Invoke();
        return fields;
    }

    private Dictionary<int, AwkValue> StageFieldValueCache(int fieldCount, int cacheCount)
    {
        checkpoint?.Invoke();
        var cache = new Dictionary<int, AwkValue>(cacheCount);
        var visited = 0;
        foreach (var pair in _fieldValueCache)
        {
            if ((visited++ & (MutationCheckpointFields - 1)) == 0)
                checkpoint?.Invoke();
            if (pair.Key < fieldCount)
                cache.Add(pair.Key, pair.Value);
        }
        checkpoint?.Invoke();
        return cache;
    }

    private void PublishStagedFields(
        List<AwkFieldText> fields,
        Dictionary<int, AwkValue> cache,
        long retainedBytes,
        AwkMemoryLease staging,
        long stagedBytes)
    {
        checkpoint?.Invoke();
        // The temporary lease already admits the new buffers while the previous ones are live.
        // Transfer only the retained increase; disposal releases the remaining overlap charge.
        var transferredBytes = Math.Min(stagedBytes, Math.Max(0, retainedBytes - _retainedMemoryBytes));
        ReplaceRetainedMemory(retainedBytes - transferredBytes);
        staging.Transfer(transferredBytes);
        _retainedMemoryBytes = retainedBytes;
        _fieldTexts = fields;
        _fieldValueCache = cache;
    }

    private void EnsureRecordText()
    {
        List<AwkFieldText> CreateRecordSlices(
            int separatorByteLength,
            List<AwkFieldText> source)
        {
            if (source.Count == 0)
                return [];

            var slices = new List<AwkFieldText>(source.Count);
            var offset = 0;
            for (var index = 0; index < source.Count; index++)
            {
                if ((index & 1023) == 0)
                    checkpoint?.Invoke();
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

            var separator = outputFieldSeparator.AsSpan(checkpoint);
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
                if ((index & 1023) == 0)
                    checkpoint?.Invoke();
                if (index > 0 && !separator.IsEmpty)
                {
                    AwkText.CopyBytes(separator, bytes.AsSpan(offset), checkpoint);
                    offset += separator.Length;
                }

                var field = fieldTexts[index].AsSpan(_recordText, checkpoint);
                AwkText.CopyBytes(field, bytes.AsSpan(offset), checkpoint);
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
                    if ((index & 1023) == 0)
                        checkpoint?.Invoke();
                    var field = _fieldTexts[index];
                    if (_fieldValueCache.TryGetValue(index, out var value) && value.IsNumberValue)
                    {
                        recordByteWork(0);
                        var text = formatStringByteText(value);
                        recordByteWork(text.ByteLength);
                        var bytes = text.EstimatedRetainedMemoryBytes;
                        var next = AwkRuntimeMemory.CheckedAdd(formattedBytes, bytes);
                        governor.ReserveAndCommit(bytes);
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
            using var recompositionPeak = PreflightRecomposition(newRetainedBytes, byteLength);
            var recordText = ComposeRecordText(_recompositionSeparator, fields, payloadByteLength);
            checkpoint?.Invoke();
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

    private void EnsureField(AwkFieldIndex index, Func<AwkText, List<AwkFieldText>> splitFields)
    {
        if (_fieldsAreSplit || (index.IsNamedField && index.Value <= _fieldTexts.Count))
            return;
        // Prove the complete field count fits before publishing a prefix. Tighter
        // quotas retain complete validation and its established rejection timing.
        var upperBound = (long)_recordText.ByteLength + 1;
        if (fieldSplitter is null || _recordText.ByteLength < IncrementalRecordBytes ||
            !index.IsNamedField || upperBound > maxFields ||
            index.Value >= upperBound || !AwkFieldSplitter.SupportsIncrementalFields(SplitPlan))
        {
            EnsureFields(splitFields);
            return;
        }

        var previousCount = _fieldTexts.Count;
        if (previousCount == 0)
        {
            counters.RecordFieldSplit();
            // Preserve the conservative logical work charge; descriptor counters
            // independently report the work avoided by partial splitting.
            recordByteWork(_recordText.ByteLength);
        }
        var stagingBytes = governor.IsEnabled
            ? AwkRuntimeMemory.CheckedMultiply(AwkRuntimeMemory.EstimateFieldTextListBytes(index.Value), 2)
            : 0;
        using var staging = governor.ReserveTransient(stagingBytes);
        var fields = new List<AwkFieldText>(index.Value);
        for (var i = 0; i < previousCount; i++)
        {
            if ((i & 1023) == 0)
                checkpoint?.Invoke();
            fields.Add(_fieldTexts[i]);
        }
        var position = fieldSplitter.ExtendRecordFields(_recordText, SplitPlan, _splitPosition, index.Value, fields);
        var complete = position < 0;
        if (complete)
            fields.TrimExcess();
        var payloadBytes = _fieldPayloadByteLength;
        for (var i = previousCount; i < fields.Count; i++)
        {
            if ((i & 1023) == 0)
                checkpoint?.Invoke();
            payloadBytes = AwkRuntimeMemory.CheckedAdd(payloadBytes, fields[i].ByteLength());
        }
        var retained = EstimateRetainedBytes(_recordText, fields, _fieldValueCache);
        if (!complete && governor.IsEnabled)
            retained = AwkRuntimeMemory.CheckedAdd(retained, SplitPlan.RetainedMemoryBytes);
        checkpoint?.Invoke();
        PublishStagedFields(fields, _fieldValueCache, retained, staging, stagingBytes);
        _splitPosition = position;
        _fieldPayloadByteLength = payloadBytes;
        counters.RecordFieldsMaterialized(fields.Count - previousCount);
        if (complete)
        {
            _fieldsAreSplit = true;
            _fieldCountValue = AwkValue.FromNumber(fields.Count);
            SplitPlan = default;
        }
    }

    private void EnsureFields(
        Func<AwkText, List<AwkFieldText>> splitFields)
    {
        int SumFieldPayloadByteLength(List<AwkFieldText> fieldTexts)
        {
            var byteLength = 0;
            for (var index = 0; index < fieldTexts.Count; index++)
            {
                if ((index & 1023) == 0)
                    checkpoint?.Invoke();
                byteLength = AwkRuntimeMemory.CheckedAdd(byteLength, fieldTexts[index].ByteLength());
            }

            return byteLength;
        }

        if (_fieldsAreSplit)
            return;

        if (_fieldTexts.Count == 0)
            counters.RecordFieldSplit();
        using var splitLease = governor.ReserveTransient(
            governor.IsEnabled ? AwkRuntimeMemory.EstimateSplitBufferBytes(
                _recordText.ByteLength,
                fieldListCount: 2,
                AwkSeparatorCaptureMode.Omit) : 0);
        var fieldTexts = splitFields(_recordText);
        ThrowIfFieldCountExceeded(fieldTexts.Count);
        var payloadByteLength = SumFieldPayloadByteLength(fieldTexts);
        var retainedBytes = EstimateRetainedBytes(_recordText, fieldTexts, _fieldValueCache);
        checkpoint?.Invoke();
        ReplaceRetainedMemory(retainedBytes);
        var previousCount = _fieldTexts.Count;
        _fieldTexts = fieldTexts;
        _fieldCountValue = AwkValue.FromNumber(fieldTexts.Count);
        _fieldPayloadByteLength = payloadByteLength;
        counters.RecordFieldsMaterialized(_fieldTexts.Count - previousCount);
        _fieldsAreSplit = true;
        SplitPlan = default;
    }

    private void ClearSplitFields(bool releaseCache = false)
    {
        _fieldCountValue = AwkValue.FromNumber(0);
        if (_fieldTexts.Capacity != 0)
            _fieldTexts = [];
        if (!releaseCache && _fieldValueCache.EnsureCapacity(0) <=
            (governor.IsEnabled ? MaxGovernedReusableFieldCacheCapacity : MaxReusableFieldCacheCapacity))
            _fieldValueCache.Clear();
        else
            _fieldValueCache = [];
        _fieldsAreSplit = false;
        _splitPosition = 0;
        _recompositionSeparator = AwkText.Empty;
        _fieldPayloadByteLength = 0;
        _numericFieldCount = 0;
    }

    private AwkMemoryPeakAdmission PreflightRecomposition(long newRetainedBytes, int recordByteLength)
    {
        throwIfValueBytesExceeded(recordByteLength);
        if (!governor.IsEnabled)
            return default;
        var retainedIncrease = Math.Max(0, newRetainedBytes - _retainedMemoryBytes);
        // Recomposition allocates its destination while the previous record and dirty fields are live.
        return governor.ReserveAllocationPeak(AwkRuntimeMemory.CheckedAdd(
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
        if (!governor.IsEnabled)
            return;

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

    private long EstimateRetainedBytes(
        AwkText recordText,
        List<AwkFieldText> fieldTexts,
        Dictionary<int, AwkValue> fieldValueCache,
        AwkValue fieldCountValue = default)
        => governor.IsEnabled
            ? EstimateRetainedBytes(AwkRuntimeMemory.EstimateTextBytes(recordText), fieldTexts, fieldValueCache, fieldCountValue)
            : 0;

    private long EstimateRetainedBytes(
        long recordTextBytes,
        List<AwkFieldText> fieldTexts,
        Dictionary<int, AwkValue> fieldValueCache,
        AwkValue fieldCountValue)
    {
        if (!governor.IsEnabled)
            return 0;

        long EstimateFieldValueCacheBytes(
            List<AwkFieldText> fieldTexts,
            Dictionary<int, AwkValue> fieldValueCache)
        {
            var bytes = EstimateFieldCacheBufferBytes(fieldValueCache.Count);
            var visited = 0;
            foreach (var pair in fieldValueCache)
            {
                if ((visited++ & 1023) == 0)
                    checkpoint?.Invoke();
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
            if ((index & 1023) == 0)
                checkpoint?.Invoke();
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, fieldTexts[index].RetainedTextByteLength);
        }

        return bytes;
    }

    private long EstimateCachedFieldPayloadBytes(AwkFieldText fieldText, AwkValue value) =>
        governor.IsEnabled && fieldText.Kind == AwkFieldTextKind.RecordSlice
            ? value.RetainedTextMemoryBytes
            : 0;

    private long EstimateFieldCacheBufferBytes(int count)
    {
        if (!governor.IsEnabled) return 0;
        if (count > 0) return Math.Max(SmallFieldCacheBufferBytes, AwkRuntimeMemory.EstimateDictionaryBufferBytes(count));
        return _fieldValueCache.EnsureCapacity(0) is > 0 and <= MaxGovernedReusableFieldCacheCapacity
            ? SmallFieldCacheBufferBytes : 0;
    }

    private long AdjustEstimate(long currentBytes, long removedBytes, long addedBytes)
    {
        if (!governor.IsEnabled)
            return 0;

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
