using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkFieldFrame(
    AwkRuntimeCounters counters,
    AwkMemoryAccounting governor,
    Func<AwkValue, AwkText> formatStringByteText,
        Action? checkpoint = null,
    AwkFieldSplitter? fieldSplitter = null)
{
    private const int MutationCheckpointFields = 1024;
    // Tiny records are cheaper to finish once than to stage successive prefixes.
    private const int IncrementalRecordBytes = 128;
    private const int MaxReusableBufferBytes = 4096;
    private const int MaxReusableFieldListCapacity =
        (MaxReusableBufferBytes - AwkRuntimeMemory.ListBufferOverheadBytes) / AwkRuntimeMemory.FieldTextBytes;
    private const int MaxReusableFieldCacheCapacity =
        (MaxReusableBufferBytes - AwkRuntimeMemory.ListBufferOverheadBytes) / AwkRuntimeMemory.CachedFieldValueBytes;
    // Private empty view while a reusable buffer is staged; never mutated.
    private static readonly List<AwkFieldText> EmptyFieldTexts = [];

    private AwkRecordStorage? _inputStorage;
    private ReadOnlyMemory<byte> _bufferedRecord;
    private bool _hasBufferedRecord;
    private bool? _bufferedAscii;

    internal AwkRecordStorage InputRecordStorage =>
        _inputStorage ??= new(governor, counters, checkpoint ?? (static () => { }));

    private int RecordByteLength => _hasBufferedRecord ? _bufferedRecord.Length : _recordText.ByteLength;
    private ReadOnlySpan<byte> RecordBytes()
    {
        checkpoint?.Invoke();
        return _hasBufferedRecord ? _bufferedRecord.Span : _recordText.AsSpan(checkpoint);
    }

    private void ClearBufferedRecord()
    {
        _bufferedRecord = default;
        _hasBufferedRecord = false;
        _bufferedAscii = null;
    }

    private AwkText OwnedRecordText()
    {
        EnsureRecordText();
        if (!_hasBufferedRecord) return _recordText;
        var retained = AdjustEstimate(_retainedMemoryBytes, 0,
            AwkRuntimeMemory.EstimateTextBytes(_bufferedRecord.Length));
        using var admission = governor.AdmitMutation(Math.Max(0, retained - _retainedMemoryBytes));
        var text = AwkText.CopyTrustedBytes(_bufferedRecord.Span, counters, governor, checkpoint);
        checkpoint?.Invoke();
        ReplaceRetainedMemory(retained);
        _recordText = text;
        ClearBufferedRecord();
        return text;
    }

    private AwkText MaterializeField(AwkFieldText field)
    {
        if (!_hasBufferedRecord || field.Kind != AwkFieldTextKind.RecordSlice)
            return field.Materialize(_recordText, counters, governor, checkpoint);
        return field.IsWholeRecord(RecordByteLength) ? OwnedRecordText() :
            AwkText.CopyTrustedBytes(field.AsSpan(RecordBytes(), checkpoint), counters, governor, checkpoint);
    }

    internal void SetBufferedRecord(ReadOnlyMemory<byte> bytes, AwkRecordSplitPlan plan)
    {
        ReplaceRetainedMemory(EstimateEmptyRecordBytes(AwkText.Empty, plan));
        SplitPlan = plan;
        _recordText = AwkText.Empty;
        _bufferedRecord = bytes;
        _hasBufferedRecord = true;
        _bufferedAscii = null;
        _recordWasRecomposed = false;
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = false;
        _recordTextIsCurrent = true;
        ClearSplitFields(reuseFieldBuffer: AwkFieldSplitter.SupportsIncrementalFields(plan));
    }

    private AwkText _recordText = AwkText.Empty;
    private AwkValue _recordValue = AwkValue.FromText(string.Empty);
    private AwkValue _fieldCountValue = AwkValue.FromNumber(0);
    private List<AwkFieldText> _fieldTexts = [];
    private readonly record struct CachedFieldValue(int FieldIndex, bool IsNumericOnly, AwkValue Value);
    private List<CachedFieldValue> _fieldValueCache = [];
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
        ClearBufferedRecord();
        _inputStorage?.Dispose();
        ReplaceRetainedMemory(0);
        _recordText = AwkText.Empty;
        _recordWasRecomposed = false;
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = true;
        _recordTextIsCurrent = true;
        ClearSplitFields(releaseCache: true);
        _fieldsAreSplit = true;
        SplitPlan = default;
    }

    internal AwkByteTextOperand RecordByteOperand
    {
        get
        {
            EnsureRecordText();
            return _hasBufferedRecord ? new(_bufferedRecord.Span) : new(_recordText);
        }
    }

    internal bool TryGetBufferedRecordBytes(out ReadOnlySpan<byte> bytes)
    {
        EnsureRecordText();
        bytes = _hasBufferedRecord ? _bufferedRecord.Span : default;
        return _hasBufferedRecord;
    }

    public AwkText RecordAwkText
    {
        get
        {
            return OwnedRecordText();
        }
    }

    public bool TryGetRecomposedRecordFields(
        out ReadOnlySpan<AwkFieldText> fields, out ReadOnlySpan<byte> record, out AwkText separator)
    {
        // Only text has a stable spelling without reconstruction. Numeric fields must
        // still freeze their CONVFMT spelling when $0 is first observed.
        var available = !_recordTextIsCurrent && _numericFieldCount == 0;
        fields = available ? CollectionsMarshal.AsSpan(_fieldTexts) : default;
        record = RecordBytes();
        separator = _recompositionSeparator;
        return available;
    }

    public AwkValue GetRecordValue(
        Func<AwkText, AwkValue> createRecordValue,
        Func<AwkText, AwkValue> createRecomposedRecordValue)
    {
        _ = OwnedRecordText();
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

        var fieldIndex = index.Value - 1;
        if (TryGetCachedFieldValue(fieldIndex, out var cached)) return cached;
        var value = createFieldValue(MaterializeField(_fieldTexts[fieldIndex]));
        CacheFieldValue(fieldIndex, value, numericOnly: false);
        return value;
    }

    public double GetFieldNumber(
        AwkFieldIndex index,
        Func<AwkText, List<AwkFieldText>> splitFields,
        Func<AwkText, AwkValue> createFieldValue,
        AwkNumberParsingMode mode)
    {
        EnsureField(index, splitFields);
        if (!index.IsNamedField || index.Value > _fieldTexts.Count) return 0;
        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (field.CacheSlot != 0)
            return _fieldValueCache[field.CacheSlot - 1].Value.AsNumber(checkpoint);
        if (field.Kind == AwkFieldTextKind.Owned)
            return GetFieldValue(index, splitFields, createFieldValue).AsNumber(checkpoint);

        counters.RecordNumericFieldByteParse();
        var parsed = AwkNumberParser.Parse(field.AsSpan(RecordBytes(), checkpoint), mode, checkpoint);
        parsed.ThrowIfError();
        var number = parsed.Status == AwkNumberParseStatus.Number ? parsed.Number : 0;
        // Spare slots and number payloads are already funded: this changes neither
        // capacity nor retained text. Only the cold initial buffer needs admission.
        if (_fieldValueCache.Count < _fieldValueCache.Capacity)
            SetCachedFieldValue(fieldIndex, AwkValue.FromNumber(number), numericOnly: true);
        else if (_fieldValueCache.Capacity == 0)
            CacheFieldValue(fieldIndex, AwkValue.FromNumber(number), numericOnly: true);
        return number;
    }

    private void CacheFieldValue(int index, AwkValue value, bool numericOnly)
    {
        var field = _fieldTexts[index];
        var capacity = PlanFieldCacheCapacity(_fieldValueCache.Count + (field.CacheSlot == 0 ? 1 : 0));
        var retainedBytes = _retainedMemoryBytes;
        if (governor.IsEnabled)
        {
            if (capacity != _fieldValueCache.Capacity)
                retainedBytes = AdjustEstimate(retainedBytes,
                    EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity), EstimateFieldCacheBufferBytes(capacity));
            retainedBytes = AwkRuntimeMemory.CheckedAdd(retainedBytes, EstimateCachedFieldPayloadBytes(field, value));
        }
        if (capacity != _fieldValueCache.Capacity)
            GrowFieldValueCache(capacity, retainedBytes);
        else
        {
            checkpoint?.Invoke();
            ReplaceRetainedMemory(retainedBytes);
        }
        SetCachedFieldValue(index, value, numericOnly);
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
        if (HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned)
            return false;

        if (!byteMode && field.Kind == AwkFieldTextKind.RecordSlice)
        {
            // The text caches this fact: repeated length reads cannot rescan the record.
            var ascii = _hasBufferedRecord
                ? (_bufferedAscii ??= Ascii.IsValid(RecordBytes()))
                : _recordText.IsAscii(out _, checkpoint);
            checkpoint?.Invoke();
            if (!ascii)
                return false;
        }

        length = field.ByteLength();
        return true;
    }

    public bool TryGetUnmaterializedFieldBytes(
        AwkFieldIndex index, Func<AwkText, List<AwkFieldText>> splitFields, out ReadOnlySpan<byte> bytes)
    {
        EnsureField(index, splitFields);
        bytes = default;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return true;

        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned)
            return false;
        bytes = field.AsSpan(RecordBytes(), checkpoint);
        return true;
    }

    public bool TryGetUnmaterializedFieldText(
        AwkFieldIndex index, Func<AwkText, List<AwkFieldText>> splitFields,
        out AwkFieldText field, out ReadOnlySpan<byte> record)
    {
        // Reuse byte-view admission without transporting descriptors for existing byte consumers.
        var borrowed = TryGetUnmaterializedFieldBytes(index, splitFields, out _);
        field = borrowed && index.IsNamedField && index.Value <= _fieldTexts.Count
            ? _fieldTexts[index.Value - 1] : AwkFieldText.Empty;
        record = RecordBytes();
        return borrowed;
    }

    public void SetRecordText(AwkText text, AwkRecordSplitPlan splitPlan = default)
    {
        ReplaceRetainedMemory(EstimateEmptyRecordBytes(text, splitPlan));
        SplitPlan = splitPlan;
        ClearBufferedRecord();
        _recordText = text;
        _recordWasRecomposed = false;
        _recordValue = AwkValue.FromText(string.Empty);
        _recordValueIsCurrent = false;
        _recordTextIsCurrent = true;
        ClearSplitFields(reuseFieldBuffer: AwkFieldSplitter.SupportsIncrementalFields(splitPlan));
    }

    public long EstimateRecordMemoryGrowth(AwkText text, AwkRecordSplitPlan splitPlan) =>
        Math.Max(0, EstimateEmptyRecordBytes(text, splitPlan) - _retainedMemoryBytes);

    public void SetRecordValue(
        AwkValue value,
        Func<AwkValue, AwkText> formatStringByteText,
        AwkRecordSplitPlan splitPlan = default)
    {
        var text = formatStringByteText(value);
        ReplaceRetainedMemory(EstimateEmptyRecordBytes(text, splitPlan));
        SplitPlan = splitPlan;
        ClearBufferedRecord();
        _recordText = text;
        _recordWasRecomposed = false;
        _recordValue = value;
        _recordValueIsCurrent = true;
        _recordTextIsCurrent = true;
        ClearSplitFields(reuseFieldBuffer: AwkFieldSplitter.SupportsIncrementalFields(splitPlan));
    }

    internal bool TryCopyInputField(AwkFieldIndex source, AwkFieldIndex destination,
        Func<AwkText> resolveOutputFieldSeparator, Func<AwkText, List<AwkFieldText>> splitFields)
    {
        if (fieldSplitter is null || !source.IsNamedField || !destination.IsNamedField) return false;
        EnsureField(source, splitFields);
        if (!_fieldsAreSplit || source.Value > _fieldTexts.Count || destination.Value > _fieldTexts.Count)
            return false;
        var from = source.Value - 1;
        var to = destination.Value - 1;
        var field = _fieldTexts[from];
        var previous = _fieldTexts[to];
        if (field.Kind != AwkFieldTextKind.RecordSlice || previous.Kind != AwkFieldTextKind.RecordSlice ||
            HasCachedScalarValue(field) || HasCachedScalarValue(previous)) return false;

        var separator = resolveOutputFieldSeparator();
        var payloadBytes = AwkRuntimeMemory.CheckedAdd(
            _fieldPayloadByteLength - _fieldTexts[to].ByteLength(), field.ByteLength());
        var recordBytes = EstimateComposedRecordByteLength(separator.ByteLength, _fieldTexts.Count, payloadBytes);
        var retainedBytes = ReplaceSeparatorEstimate(_retainedMemoryBytes, separator, _fieldTexts.Count);
        using var peak = PreflightRecomposition(retainedBytes, recordBytes);
        checkpoint?.Invoke();
        ReplaceRetainedMemory(retainedBytes);
        // Both descriptors still refer to the same funded record. Cache slots stay
        // local to each field, including when removing this slot remaps the source.
        RemoveCachedFieldValue(to);
        _fieldTexts[to] = field.WithCacheSlot(0);
        _fieldPayloadByteLength = payloadBytes;
        MarkRecordForRecomposition(separator);
        return true;
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
        var fieldIndex = index.Value - 1;
        var oldFieldCount = _fieldTexts.Count;
        var newFieldCount = Math.Max(oldFieldCount, index.Value);
        var newCapacity = PlanFieldCapacity(newFieldCount);
        var oldField = fieldIndex < oldFieldCount ? _fieldTexts[fieldIndex] : AwkFieldText.Empty;
        var cacheCapacity = PlanFieldCacheCapacity(_fieldValueCache.Count + (oldField.CacheSlot == 0 ? 1 : 0));
        var stageCache = cacheCapacity != _fieldValueCache.Capacity;
        var oldIsNumeric = TryGetCachedFieldValue(fieldIndex, out var oldValue) && oldValue.IsNumberValue;
        // Pure numbers retain no formatted field text. Their conversion belongs to the
        // next actual record reconstruction, under its then-current CONVFMT.
        var newField = (value.IsNumberValue ? AwkFieldText.Empty : AwkFieldText.FromOwned(formatStringByteText(value)))
            .WithCacheSlot(oldField.CacheSlot);
        var newPayloadByteLength = AwkRuntimeMemory.CheckedAdd(
            _fieldPayloadByteLength - (oldIsNumeric ? 0 : oldField.ByteLength()),
            newField.ByteLength());
        var newRetainedBytes = EstimateRetainedBytesAfterFieldAssignment(
            fieldIndex,
            oldField,
            newField,
            newCapacity);
        var composedRecordByteLength = EstimateComposedRecordByteLength(
            outputFieldSeparator.ByteLength,
            newFieldCount,
            newPayloadByteLength);
        if (newFieldCount > oldFieldCount)
            newRetainedBytes = AdjustEstimate(newRetainedBytes, _fieldCountValue.RetainedTextMemoryBytes, 0);
        newRetainedBytes = ReplaceSeparatorEstimate(newRetainedBytes, outputFieldSeparator, newFieldCount);
        using var recompositionPeak = PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        var addedFields = newFieldCount - oldFieldCount;
        var stageFields = addedFields >= MutationCheckpointFields || newCapacity != _fieldTexts.Capacity;
        if (stageFields || stageCache)
        {
            var stagedBytes = stageFields ? AwkRuntimeMemory.EstimateFieldTextListBytes(newCapacity) : 0;
            if (stageCache)
                stagedBytes = AwkRuntimeMemory.CheckedAdd(stagedBytes, EstimateFieldCacheBufferBytes(cacheCapacity));
            using var staging = governor.ReserveTransient(stagedBytes);
            var fields = stageFields ? StageFieldTexts(newFieldCount, newCapacity) : _fieldTexts;
            var cache = stageCache ? StageFieldValueCache(fields, cacheCapacity) : _fieldValueCache;
            PublishStagedFields(fields, cache, newRetainedBytes, staging, stagedBytes);
        }
        else
        {
            checkpoint?.Invoke();
            ReplaceRetainedMemory(newRetainedBytes);
        }
        while (_fieldTexts.Count < newFieldCount) _fieldTexts.Add(AwkFieldText.Empty);
        _fieldTexts[fieldIndex] = newField;
        SetCachedFieldValue(fieldIndex, value);
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
            int newCapacity)
        {
            var bytes = AdjustEstimate(
                _retainedMemoryBytes,
                AwkRuntimeMemory.EstimateFieldTextListBytes(_fieldTexts.Capacity),
                AwkRuntimeMemory.EstimateFieldTextListBytes(newCapacity));
            bytes = AdjustEstimate(bytes, oldField.RetainedTextByteLength, newField.RetainedTextByteLength);
            if (TryGetCachedFieldValue(fieldIndex, out var oldCachedValue))
                bytes = AdjustEstimate(bytes, EstimateCachedFieldPayloadBytes(oldField, oldCachedValue), 0);
            return cacheCapacity == _fieldValueCache.Capacity ? bytes : AdjustEstimate(bytes,
                EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity),
                EstimateFieldCacheBufferBytes(cacheCapacity));
        }
    }

    public void ExtendFieldCountForReference(
        AwkFieldIndex index,
        Func<AwkText, List<AwkFieldText>> splitFields)
    {
        EnsureFields(splitFields);
        if (index.Value <= _fieldTexts.Count)
            return;

        // Resolving an lvalue creates absent fields but does not assign them or rebuild $0.
        // If earlier writes left $0 dirty, freeze their result before extending the field list.
        EnsureRecordText();
        var addedFields = index.Value - _fieldTexts.Count;
        var newCapacity = PlanFieldCapacity(index.Value);
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateFieldTextListBytes(_fieldTexts.Capacity),
            AwkRuntimeMemory.EstimateFieldTextListBytes(newCapacity));
        newRetainedBytes = AdjustEstimate(newRetainedBytes, _fieldCountValue.RetainedTextMemoryBytes, 0);
        if (addedFields >= MutationCheckpointFields || newCapacity != _fieldTexts.Capacity)
        {
            var stagedBytes = AwkRuntimeMemory.EstimateFieldTextListBytes(newCapacity);
            using var staging = governor.ReserveTransient(stagedBytes);
            var fields = StageFieldTexts(index.Value, newCapacity);
            PublishStagedFields(fields, _fieldValueCache, newRetainedBytes, staging, stagedBytes);
        }
        else
        {
            checkpoint?.Invoke();
            ReplaceRetainedMemory(newRetainedBytes);
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
        var oldFieldCount = _fieldTexts.Count;
        var newFieldCount = count.Value;
        var newCapacity = PlanFieldCapacity(newFieldCount);
        var stageFields = Math.Abs(newFieldCount - oldFieldCount) >= MutationCheckpointFields ||
            newCapacity != _fieldTexts.Capacity;
        var newCacheCount = _fieldValueCache.Count;
        var cacheCapacity = _fieldValueCache.Capacity;
        var newPayloadByteLength = _fieldPayloadByteLength;
        var newNumericFieldCount = _numericFieldCount;
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateFieldTextListBytes(_fieldTexts.Capacity),
            AwkRuntimeMemory.EstimateFieldTextListBytes(newCapacity));
        newRetainedBytes = AdjustEstimate(newRetainedBytes,
            _fieldCountValue.RetainedTextMemoryBytes, value.RetainedTextMemoryBytes);
        if (newFieldCount < oldFieldCount)
        {
            for (var index = newFieldCount; index < oldFieldCount; index++)
            {
                if (oldFieldCount - newFieldCount >= MutationCheckpointFields &&
                    ((index - newFieldCount) & (MutationCheckpointFields - 1)) == 0)
                    checkpoint?.Invoke();
                var field = _fieldTexts[index];
                var numeric = TryGetCachedFieldValue(index, out var cached) && cached.IsNumberValue;
                newPayloadByteLength -= numeric ? 0 : field.ByteLength();
                newNumericFieldCount -= numeric ? 1 : 0;
                newRetainedBytes = AdjustEstimate(
                    newRetainedBytes,
                    field.RetainedTextByteLength,
                    addedBytes: 0);
                if (field.CacheSlot == 0) continue;
                cached = _fieldValueCache[field.CacheSlot - 1].Value;
                newCacheCount--;
                newRetainedBytes = AdjustEstimate(
                    newRetainedBytes,
                    EstimateCachedFieldPayloadBytes(field, cached),
                    addedBytes: 0);
            }

            if (stageFields) cacheCapacity = newCacheCount;
            if (cacheCapacity != _fieldValueCache.Capacity)
                newRetainedBytes = AdjustEstimate(newRetainedBytes,
                    EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity),
                    EstimateFieldCacheBufferBytes(cacheCapacity));
        }

        var composedRecordByteLength = EstimateComposedRecordByteLength(
            outputFieldSeparator.ByteLength,
            newFieldCount,
            newPayloadByteLength);
        newRetainedBytes = ReplaceSeparatorEstimate(newRetainedBytes, outputFieldSeparator, newFieldCount);
        using var recompositionPeak = PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        if (stageFields)
        {
            var stagedBytes = AwkRuntimeMemory.EstimateFieldTextListBytes(newCapacity);
            if (newFieldCount < oldFieldCount)
                stagedBytes = AwkRuntimeMemory.CheckedAdd(stagedBytes,
                    EstimateFieldCacheBufferBytes(cacheCapacity));
            using var staging = governor.ReserveTransient(stagedBytes);
            var fields = StageFieldTexts(newFieldCount, newCapacity);
            var cache = newFieldCount < oldFieldCount
                ? StageFieldValueCache(fields, cacheCapacity)
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
                    RemoveCachedFieldValue(index);
                _fieldTexts.RemoveRange(newFieldCount, oldFieldCount - newFieldCount);
            }
            else if (newFieldCount > oldFieldCount)
            {
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

    // Own the growth policy so admission and allocation use the same destination capacity.
    private int PlanFieldCapacity(int count)
    {
        if (count > Array.MaxLength)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        if (Math.Abs(count - _fieldTexts.Count) >= MutationCheckpointFields) return count;
        if (count <= _fieldTexts.Capacity) return _fieldTexts.Capacity;
        return (int)Math.Min(Array.MaxLength, Math.Max((long)count, Math.Max(4L, 2L * _fieldTexts.Capacity)));
    }

    private List<AwkFieldText> StageFieldTexts(int count, int capacity)
    {
        checkpoint?.Invoke();
        var fields = new List<AwkFieldText>(capacity);
        for (var index = 0; index < count; index++)
        {
            if ((index & (MutationCheckpointFields - 1)) == 0)
                checkpoint?.Invoke();
            fields.Add(index < _fieldTexts.Count ? _fieldTexts[index] : AwkFieldText.Empty);
        }
        checkpoint?.Invoke();
        return fields;
    }

    private bool HasCachedScalarValue(AwkFieldText field) =>
        field.CacheSlot != 0 && !_fieldValueCache[field.CacheSlot - 1].IsNumericOnly;

    private bool TryGetCachedFieldValue(int index, out AwkValue value)
    {
        if ((uint)index < (uint)_fieldTexts.Count && _fieldTexts[index].CacheSlot is > 0 and var slot &&
            !_fieldValueCache[slot - 1].IsNumericOnly)
        {
            value = _fieldValueCache[slot - 1].Value;
            return true;
        }
        value = default;
        return false;
    }

    private int PlanFieldCacheCapacity(int count)
    {
        if (count > Array.MaxLength)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        if (count <= _fieldValueCache.Capacity) return _fieldValueCache.Capacity;
        return (int)Math.Min(Array.MaxLength,
            Math.Max((long)count, Math.Max(4L, 2L * _fieldValueCache.Capacity)));
    }

    private void GrowFieldValueCache(int capacity, long retainedBytes)
    {
        var bytes = EstimateFieldCacheBufferBytes(capacity);
        using var staging = governor.ReserveTransient(bytes);
        var cache = StageFieldValueCache(_fieldTexts, capacity);
        PublishStagedFields(_fieldTexts, cache, retainedBytes, staging, bytes);
    }

    // Cache slots belong to this frame's descriptors; scalar snapshots never carry them.
    // All capacity and ownership changes are admitted before these non-fallible mutations.
    private void SetCachedFieldValue(int index, AwkValue value, bool numericOnly = false)
    {
        var field = _fieldTexts[index];
        var cached = new CachedFieldValue(index, numericOnly, value);
        if (field.CacheSlot == 0)
        {
            _fieldValueCache.Add(cached);
            _fieldTexts[index] = field.WithCacheSlot(_fieldValueCache.Count);
        }
        else _fieldValueCache[field.CacheSlot - 1] = cached;
    }

    private void RemoveCachedFieldValue(int index)
    {
        var field = _fieldTexts[index];
        if (field.CacheSlot == 0) return;
        var slot = field.CacheSlot - 1;
        var lastSlot = _fieldValueCache.Count - 1;
        if (slot != lastSlot)
        {
            var last = _fieldValueCache[lastSlot];
            _fieldValueCache[slot] = last;
            _fieldTexts[last.FieldIndex] = _fieldTexts[last.FieldIndex].WithCacheSlot(slot + 1);
        }
        _fieldValueCache.RemoveAt(lastSlot);
        _fieldTexts[index] = field.WithCacheSlot(0);
    }

    private List<CachedFieldValue> StageFieldValueCache(List<AwkFieldText> fields, int capacity)
    {
        checkpoint?.Invoke();
        var cache = new List<CachedFieldValue>(capacity);
        if (capacity > 0) counters.RecordFieldValueCacheBufferAllocation();
        var remap = !ReferenceEquals(fields, _fieldTexts);
        for (var index = 0; index < _fieldValueCache.Count; index++)
        {
            if ((index & (MutationCheckpointFields - 1)) == 0) checkpoint?.Invoke();
            var entry = _fieldValueCache[index];
            if (entry.FieldIndex >= fields.Count) continue;
            cache.Add(entry);
            // Remapping is allowed only in an unpublished descriptor buffer.
            if (remap) fields[entry.FieldIndex] = fields[entry.FieldIndex].WithCacheSlot(cache.Count);
        }
        checkpoint?.Invoke();
        return cache;
    }

    private void PublishStagedFields(
        List<AwkFieldText> fields,
        List<CachedFieldValue> cache,
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
                slices.Add(AwkFieldText.FromRecordSlice(offset, fieldByteLength).WithCacheSlot(source[index].CacheSlot));
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

                var field = fieldTexts[index].AsSpan(RecordBytes(), checkpoint);
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
                    if (TryGetCachedFieldValue(index, out var value) && value.IsNumberValue)
                    {
                        checkpoint?.Invoke();
                        var text = formatStringByteText(value);
                        checkpoint?.Invoke();
                        var bytes = text.EstimatedRetainedMemoryBytes;
                        var next = AwkRuntimeMemory.CheckedAdd(formattedBytes, bytes);
                        governor.ReserveAndCommit(bytes);
                        formattedBytes = next;
                        payloadByteLength = AwkRuntimeMemory.CheckedAdd(payloadByteLength, text.ByteLength);
                        field = AwkFieldText.FromOwned(text).WithCacheSlot(field.CacheSlot);
                    }
                    fields.Add(field);
                }
            }
            var byteLength = EstimateComposedRecordByteLength(_recompositionSeparator.ByteLength, fields.Count, payloadByteLength);
            var fieldTexts = CreateRecordSlices(_recompositionSeparator.ByteLength, fields);
            var newRetainedBytes = EstimateRetainedBytes(
                AwkRuntimeMemory.EstimateTextBytes(byteLength), fieldTexts, _fieldValueCache, _fieldCountValue);
            using var recompositionPeak = PreflightRecomposition(newRetainedBytes, byteLength);
            var recordText = ComposeRecordText(_recompositionSeparator, fields, payloadByteLength);
            checkpoint?.Invoke();
            ReplaceRetainedMemory(newRetainedBytes);
            ClearBufferedRecord();
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
        var upperBound = (long)RecordByteLength + 1;
        if (fieldSplitter is null || RecordByteLength < IncrementalRecordBytes ||
            !index.IsNamedField ||
            index.Value >= upperBound || !AwkFieldSplitter.SupportsIncrementalFields(SplitPlan))
        {
            EnsureFields(splitFields);
            return;
        }

        var previousCount = _fieldTexts.Count;
        if (previousCount == 0)
        {
            counters.RecordFieldSplit();
            checkpoint?.Invoke();
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
        var position = fieldSplitter.ExtendRecordFields(RecordBytes(), SplitPlan, _splitPosition, index.Value, fields);
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
            !governor.IsEnabled ? 0 : SplitPlan.Mode == AwkFieldSplittingMode.Csv
                ? AwkCsvFields.EstimateMemory(_recordText, materialize: false, checkpoint ?? (static () => { }))
                : AwkRuntimeMemory.EstimateSplitBufferBytes(
                RecordByteLength,
                fieldListCount: 2,
                AwkSeparatorCaptureMode.Omit));
        var reusable = _fieldTexts.Count == 0 && _fieldTexts.Capacity > 0 &&
            ReuseFieldBuffer(AwkFieldSplitter.SupportsIncrementalFields(SplitPlan)) ? _fieldTexts : null;
        var reusableCapacity = reusable?.Capacity ?? 0;
        var previousCount = _fieldTexts.Count;
        // Keep a private empty view until staging succeeds; later field mutation owns its list.
        if (reusable is not null) _fieldTexts = EmptyFieldTexts;
        try
        {
            var fieldTexts = _hasBufferedRecord && fieldSplitter is not null && AwkFieldSplitter.SupportsIncrementalFields(SplitPlan)
                ? fieldSplitter.SplitRecordByteFields(RecordBytes(), SplitPlan, reusable)
                : reusable is not null && fieldSplitter is not null
                    ? fieldSplitter.SplitRecordFields(OwnedRecordText(), SplitPlan, reusable)
                    : splitFields(OwnedRecordText());
            // A callback transfers its list to this frame. It may return that list again for
            // a later record; cache slots from its previous use have no meaning in this one.
            if (fieldSplitter is null)
            {
                for (var index = 0; index < fieldTexts.Count; index++)
                {
                    if ((index & 1023) == 0) checkpoint?.Invoke();
                    fieldTexts[index] = fieldTexts[index].WithCacheSlot(0);
                }
            }
            // Completing an incremental split creates new descriptors for cached prefix fields.
            for (var index = 0; index < _fieldValueCache.Count; index++)
            {
                if ((index & 1023) == 0) checkpoint?.Invoke();
                var field = _fieldValueCache[index].FieldIndex;
                fieldTexts[field] = fieldTexts[field].WithCacheSlot(index + 1);
            }
            var payloadByteLength = SumFieldPayloadByteLength(fieldTexts);
            var retainedBytes = EstimateRetainedBytes(_recordText, fieldTexts, _fieldValueCache);
            checkpoint?.Invoke();
            ReplaceRetainedMemory(retainedBytes);
            _fieldTexts = fieldTexts;
            _fieldCountValue = AwkValue.FromNumber(fieldTexts.Count);
            _fieldPayloadByteLength = payloadByteLength;
            counters.RecordFieldsMaterialized(_fieldTexts.Count - previousCount);
            _fieldsAreSplit = true;
            SplitPlan = default;
            if (ReferenceEquals(fieldTexts, reusable)) counters.RecordFieldListBufferReuse();
        }
        catch
        {
            if (reusable is not null)
            {
                reusable.Clear();
                _fieldTexts = reusable.Capacity == reusableCapacity ? reusable : [];
            }
            throw;
        }
    }

    private void ClearSplitFields(bool releaseCache = false, bool reuseFieldBuffer = true)
    {
        _fieldCountValue = AwkValue.FromNumber(0);
        // Only the bound splitter can refill reusable storage; callbacks transfer their returned lists.
        if (!releaseCache && _fieldTexts.Capacity > 0 && ReuseFieldBuffer(reuseFieldBuffer))
            _fieldTexts.Clear();
        else if (_fieldTexts.Capacity != 0)
            _fieldTexts = [];
        if (!releaseCache && ReuseCacheBuffer())
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
        _recompositionSeparator = _fieldTexts.Count > 1 ? outputFieldSeparator : AwkText.Empty;
    }

    // The frame can outlive the OFS variable's value. Its existing owner funds that
    // captured text independently, and zero/one-field records never need the separator.
    private long ReplaceSeparatorEstimate(long retainedBytes, AwkText separator, int fieldCount) =>
        AdjustEstimate(retainedBytes, _recompositionSeparator.EstimatedRetainedMemoryBytes,
            fieldCount > 1 ? separator.EstimatedRetainedMemoryBytes : 0);

    private void ReplaceRetainedMemory(long newBytes)
    {
        if (!governor.IsEnabled)
            return;

        if (newBytes > _retainedMemoryBytes)
        {
            governor.ReserveAndCommit(newBytes - _retainedMemoryBytes);
        }
        else if (_retainedMemoryBytes > newBytes)
        {
            governor.ReleaseCommitted(_retainedMemoryBytes - newBytes);
        }

        _retainedMemoryBytes = newBytes;
    }

    private long EstimateEmptyRecordBytes(AwkText text, AwkRecordSplitPlan plan) =>
        !governor.IsEnabled ? 0 : AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateTextBytes(text), plan.RetainedMemoryBytes),
            AwkRuntimeMemory.CheckedAdd(
                ReuseFieldBuffer(AwkFieldSplitter.SupportsIncrementalFields(plan))
                    ? AwkRuntimeMemory.EstimateFieldTextListBytes(_fieldTexts.Capacity) : 0,
                ReuseCacheBuffer() ? EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity) : 0));

    private long EstimateRetainedBytes(
        AwkText recordText,
        List<AwkFieldText> fieldTexts,
        List<CachedFieldValue> fieldValueCache,
        AwkValue fieldCountValue = default)
        => governor.IsEnabled
            ? EstimateRetainedBytes(AwkRuntimeMemory.EstimateTextBytes(recordText), fieldTexts, fieldValueCache, fieldCountValue)
            : 0;

    private long EstimateRetainedBytes(
        long recordTextBytes,
        List<AwkFieldText> fieldTexts,
        List<CachedFieldValue> fieldValueCache,
        AwkValue fieldCountValue)
    {
        if (!governor.IsEnabled)
            return 0;

        long EstimateFieldValueCacheBytes(
            List<AwkFieldText> fieldTexts,
            List<CachedFieldValue> fieldValueCache)
        {
            var bytes = EstimateFieldCacheBufferBytes(fieldValueCache.Capacity);
            for (var index = 0; index < fieldValueCache.Count; index++)
            {
                if ((index & 1023) == 0) checkpoint?.Invoke();
                var entry = fieldValueCache[index];
                if (entry.FieldIndex < fieldTexts.Count)
                    bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                        EstimateCachedFieldPayloadBytes(fieldTexts[entry.FieldIndex], entry.Value));
            }
            return bytes;
        }

        var bytes = AwkRuntimeMemory.CheckedAdd(
            recordTextBytes, fieldCountValue.RetainedTextMemoryBytes);
        bytes = AwkRuntimeMemory.CheckedAdd(
            bytes,
            AwkRuntimeMemory.EstimateFieldTextListBytes(fieldTexts.Capacity));
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

    private long EstimateFieldCacheBufferBytes(int capacity) =>
        governor.IsEnabled ? AwkRuntimeMemory.EstimateFieldValueListBytes(capacity) : 0;

    private bool ReuseFieldBuffer(bool allowed) => allowed && fieldSplitter is not null &&
        _fieldTexts.Capacity <= MaxReusableFieldListCapacity;

    private bool ReuseCacheBuffer() =>
        _fieldValueCache.Capacity <= MaxReusableFieldCacheCapacity;

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

}
