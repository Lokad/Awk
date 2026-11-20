using System.Runtime.InteropServices;

namespace Lokad.Awk;

internal sealed class AwkFieldFrame(AwkRuntimeState state)
{
    private const int MutationCheckpointFields = 1024;
    // Keep small buffers through empty/narrow records. Larger funded capacity is
    // useful only while it fits the incoming record's conservative field bound.
    private const int SmallReusableBufferBytes = 4096;
    private const int SmallFieldBufferCapacity =
        SmallReusableBufferBytes / AwkRuntimeMemory.RecordFieldBytes;
    private const int SmallFieldCacheCapacity =
        (SmallReusableBufferBytes - AwkRuntimeMemory.ListBufferOverheadBytes) / AwkRuntimeMemory.CachedFieldValueBytes;
    // Private empty view while a reusable buffer is staged; never mutated.
    private static readonly AwkRecordFieldBuffer EmptyFieldTexts = new();

    private AwkRecordStorage? _inputStorage;
    private AwkRecordStorage? _bufferedStorage;
    private int _bufferedSlot;
    private bool HasBufferedRecord => _bufferedStorage is not null;

    internal AwkRecordStorage InputRecordStorage =>
        _inputStorage ??= new(state);

    private int RecordByteLength => _bufferedStorage is { } storage
        ? storage.LengthAt(_bufferedSlot) : _recordText.ByteLength;
    private ReadOnlySpan<byte> RecordBytes()
    {
        // A custom accessor can promote its borrow while the span is obtained.
        return _bufferedStorage is { } storage
            ? storage.BytesAt(_bufferedSlot) : _recordText.AsSpan(state.CancellationCheckpoint);
    }

    private void ClearBufferedRecord() => _bufferedStorage = null;

    private AwkText OwnedRecordText()
    {
        EnsureRecordText();
        return HasBufferedRecord ? MaterializeBufferedRecordText() : _recordText;
    }

    // Keep allocation/admission state at the actual owner, away from repeated
    // cached record and field-text reads.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private AwkText MaterializeBufferedRecordText()
    {
        var retained = AdjustEstimate(_retainedMemoryBytes, 0,
            AwkRuntimeMemory.EstimateTextBytes(RecordByteLength));
        using var admission = state.MemoryAccounting.AdmitMutation(Math.Max(0, retained - _retainedMemoryBytes));
        var text = state.CopyText(CaptureCopySource());
        state.CancellationCheckpoint?.Invoke();
        ReplaceRetainedMemory(retained);
        _recordText = text;
        ClearBufferedRecord();
        return text;
    }

    internal void SetBufferedRecord(AwkRecordStorage storage, int slot, AwkRecordSplitPlan splitPlan, long retainedBytes)
    {
        ReplaceRetainedMemory(retainedBytes);
        _splitPlan = splitPlan;
        _recordText = AwkText.Empty;
        _bufferedStorage = storage;
        _bufferedSlot = slot;
        FinishRecordPublication();
    }

    private AwkText _recordText = AwkText.Empty;
    private AwkValue _recordValue = AwkValue.UnassignedText();
    private AwkValue _fieldCountValue = AwkValue.FromNumber(0);
    private AwkRecordFieldBuffer _fieldTexts = new();
    private AwkRecordFieldBuffer? _rebuildFields;
    private AwkUtf8Builder? _rebuildNumberText;
    internal readonly struct CachedFieldValue
    {
        private readonly AwkText _spelling;

        internal CachedFieldValue(int fieldIndex, AwkFieldCacheKind kind, AwkValue value, AwkText spelling)
        {
            FieldIndex = fieldIndex;
            Kind = kind;
            Value = value;
            _spelling = spelling;
        }

        internal int FieldIndex { get; }
        internal AwkFieldCacheKind Kind { get; }
        internal AwkValue Value { get; }
        internal AwkText Spelling => _spelling;
        internal bool HasInputClassification => Kind == AwkFieldCacheKind.ClassifiedText;
        internal bool IsScalar => Kind == AwkFieldCacheKind.Scalar;
        internal long PayloadBytes
        {
            get
            {
                var spellingBytes = Spelling.EstimatedRetainedMemoryBytes;
                var valueBytes = Value.RetainedTextMemoryBytes;
                return valueBytes == 0 || Spelling.HasSameStorage(Value.ByteText)
                    ? spellingBytes : AwkRuntimeMemory.CheckedAdd(spellingBytes, valueBytes);
            }
        }
    }
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

    private AwkRecordSplitPlan _splitPlan;

    // Callers that can publish or invoke callbacks keep an independent value snapshot.
    public AwkRecordSplitPlan SplitPlan => _splitPlan;

    public void ResetRecord()
    {
        ClearBufferedRecord();
        _inputStorage?.Dispose();
        ReplaceRetainedMemory(0);
        _recordText = AwkText.Empty;
        _recordWasRecomposed = false;
        _recordValue = AwkValue.UnassignedText();
        _recordValueIsCurrent = true;
        _recordTextIsCurrent = true;
        ClearSplitFields(releaseCache: true);
        RetireRebuildNumberText();
        _fieldsAreSplit = true;
        _splitPlan = default;
    }

    internal bool TryGetPrintableBufferedRecord(out AwkCopySource bytes)
    {
        bytes = default;
        if (!_recordTextIsCurrent || _bufferedStorage is not { } storage) return false;
        bytes = new(storage, _bufferedSlot);
        return true;
    }

    internal AwkCopySource CaptureCopySource() => _bufferedStorage is { } storage
        ? new(storage, _bufferedSlot) : new(RecordBytes());

    internal bool TryGetFieldCopySource(AwkFieldIndex index, out AwkCopySource bytes)
    {
        bytes = default;
        if (!TryGetUnmaterializedField(index, out var descriptor)) return false;
        var length = descriptor.ByteLength();
        if (length != 0)
            bytes = _bufferedStorage is { } storage ? new(storage, _bufferedSlot, descriptor.RecordStart, length) :
                new(descriptor.BytesInRecord(RecordBytes()));
        return true;
    }

    public AwkText RecordAwkText
    {
        get
        {
            return OwnedRecordText();
        }
    }

    public bool TryGetRecomposedRecordFields(
        out AwkRecordFields fields, out AwkCopySource record, out AwkText separator,
        out int byteLength)
    {
        // Only text has a stable spelling without reconstruction. Numeric fields must
        // still freeze their CONVFMT spelling when $0 is first observed.
        var available = !_recordTextIsCurrent && _numericFieldCount == 0;
        fields = available ? new(_fieldTexts.AsSpan(), CollectionsMarshal.AsSpan(_fieldValueCache)) : default;
        record = available ? CaptureCopySource() : default;
        separator = _recompositionSeparator;
        byteLength = available ? EstimateComposedRecordByteLength(
            separator.ByteLength, _fieldTexts.Count, _fieldPayloadByteLength) : 0;
        return available;
    }

    public AwkValue GetRecordValue()
    {
        _ = OwnedRecordText();
        if (!_recordValueIsCurrent)
        {
            _recordValue = _recordWasRecomposed ? state.CreateStringValue(_recordText) : state.CreateNumericStringValue(_recordText, null);
            _recordValueIsCurrent = true;
        }

        return _recordValue;
    }

    public int GetFieldCount()
    {
        EnsureFields();
        return _fieldTexts.Count;
    }

    public AwkValue GetFieldCountValue()
    {
        EnsureFields();
        return _fieldCountValue;
    }

    // A speculative comparison probe cannot complete splitting or invoke admission.
    // Assigned NF keeps its scalar value, which can differ from the descriptor count.
    internal bool TryGetFieldCountNumber(out double number)
    {
        if (_fieldsAreSplit && _fieldCountValue.IsNumberValue)
        {
            number = _fieldCountValue.StoredNumber;
            return true;
        }
        number = 0;
        return false;
    }

    public AwkValue GetFieldValue(
        AwkFieldIndex index)
    {
        if (index.IsCurrentRecord) return GetRecordValue();
        EnsureField(index);
        if (index.Value > _fieldTexts.Count)
            return AwkValue.UnassignedText();

        return GetPreparedFieldValue(index.Value - 1);
    }

    private AwkValue GetPreparedFieldValue(int fieldIndex)
    {
        if (TryGetCachedFieldValue(fieldIndex, out var cached)) return cached;
        return MaterializeFieldValue(fieldIndex);
    }

    // Owned construction and cache publication stay at this frame. Keep their
    // allocation/admission machinery out of callers performing ordinary cached reads.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private AwkValue MaterializeFieldValue(int fieldIndex)
    {
        var field = _fieldTexts[fieldIndex];
        if (field.IsUnassigned) return AwkValue.UnassignedText();
        AwkValue? classification = field.HasInputClassification ? field.InputClassification :
            field.CacheSlot > 0 && _fieldValueCache[field.CacheSlot - 1].HasInputClassification
                ? _fieldValueCache[field.CacheSlot - 1].Value : null;
        if (field.CacheSlot > 0)
            return CacheMaterializedField(fieldIndex, _fieldValueCache[field.CacheSlot - 1].Spelling, classification);
        if (field.Kind != AwkFieldTextKind.RecordSlice)
            return CacheMaterializedField(fieldIndex, AwkText.Empty, classification);
        if (_bufferedStorage is not { } storage)
            return CacheMaterializedField(fieldIndex,
                state.CopyByteSlice(_recordText, field.RecordStart, field.ByteLength()), classification);
        if (_recordTextIsCurrent && field.IsWholeRecord(RecordByteLength))
            return CacheMaterializedField(fieldIndex, OwnedRecordText(), classification);

        var source = new AwkCopySource(storage, _bufferedSlot, field.RecordStart, field.ByteLength());
        using var admission = AdmitFieldCacheCopy(source.Length);
        return CacheMaterializedField(fieldIndex, state.CopyText(source), classification);
    }

    private AwkValue CacheMaterializedField(int fieldIndex, AwkText text, AwkValue? classification)
    {
        // Capture facts before admission can replace owner storage. Reusing them
        // avoids a scan, but long owning reads retain their cancellation boundary.
        if (classification.HasValue && text.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes)
            state.CancellationCheckpoint?.Invoke();
        var value = state.CreateNumericStringValue(text, classification);
        CacheFieldValue(fieldIndex, value);
        return value;
    }

    internal bool TryGetFieldTextSnapshot(AwkFieldIndex index, out AwkText text)
    {
        if (index.IsCurrentRecord)
        {
            // Assigned numbers retain formatting at the actual consumer, after
            // later arguments. Input/recomposed text already has this owner.
            text = AwkText.Empty;
            if (_recordValueIsCurrent && _recordValue.IsNumberValue) return false;
            text = OwnedRecordText();
            return true;
        }
        EnsureField(index);
        text = AwkText.Empty;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count) return true;
        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (field.CacheSlot > 0)
        {
            var cached = _fieldValueCache[field.CacheSlot - 1];
            if (cached.IsScalar) return false;
            text = cached.Spelling;
        }
        else if (field.ByteLength() != 0)
            text = CaptureFieldTextSnapshot(fieldIndex, field);
        return true;
    }

    internal bool TryCaptureTextInput(AwkFieldIndex index, out AwkTextInput input)
    {
        input = default;
        if (index.IsCurrentRecord)
        {
            EnsureRecordText();
            if (_bufferedStorage is not { } current) return false;
            input = AwkTextInput.FromRecord(current, _bufferedSlot, 0, current.LengthAt(_bufferedSlot), 0);
            return true;
        }
        if (!_recordTextIsCurrent) return false;
        EnsureField(index);
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
        {
            input = new(AwkText.Empty);
            return true;
        }
        var field = _fieldTexts[index.Value - 1];
        if (field.IsUnassigned)
        {
            input = AwkTextInput.FromTextRange(AwkText.Empty, 0, 0, index.Value);
            return true;
        }
        if (HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned) return false;
        if (_bufferedStorage is { } storage)
            input = AwkTextInput.FromRecord(storage, _bufferedSlot, field.RecordStart, field.ByteLength(), index.Value);
        else
        {
            // Capture the immutable backing before long-input or materialization polling can
            // reenter and replace this frame's current record.
            var record = _recordText;
            input = AwkTextInput.FromTextRange(record, field.RecordStart, field.ByteLength(), index.Value);
            if (record.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes) state.CancellationCheckpoint?.Invoke();
            _ = record.AsSpan(state.CancellationCheckpoint);
        }
        return true;
    }

    internal bool TryCaptureRecordText(AwkFieldIndex index,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkRecordStorage? record,
        out int slot, out int offset, out int length)
    {
        record = null;
        slot = offset = length = 0;
        if (_bufferedStorage is null) return false;
        if (index.IsCurrentRecord)
        {
            if (!_recordTextIsCurrent || _recordValueIsCurrent && _recordValue.IsNumberValue) return false;
            length = RecordByteLength;
        }
        else
        {
            if (!TryGetUnmaterializedField(index, out var field)) return false;
            length = field.ByteLength();
            offset = field.RecordStart;
        }
        if (length == 0) return false;
        record = _bufferedStorage;
        slot = _bufferedSlot;
        if (record is null) return false;
        record.ObserveMemoryAt(slot);
        return true;
    }

    // Keep immutable spelling with the record's existing cache owner. Repeated
    // captures and later scalar promotion reuse it without classifying early.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private AwkText CaptureFieldTextSnapshot(int index, AwkRecordField field)
    {
        var source = CaptureCopySource().Slice(field.RecordStart, field.ByteLength());
        using var admission = AdmitFieldCacheCopy(source.Length);
        var text = state.CopyText(source);
        var classified = field.HasInputClassification;
        var facts = classified ? field.InputClassification :
            field.TryGetNumericValue(out var number) ? AwkValue.FromNumber(number) : default;
        CacheFieldEntry(index, new(index,
            classified ? AwkFieldCacheKind.ClassifiedText : AwkFieldCacheKind.TextOnly, facts, text));
        return text;
    }

    private AwkMemoryMutationAdmission AdmitFieldCacheCopy(int length)
    {
        if (!state.MemoryAccounting.IsEnabled) return default;

        state.CancellationCheckpoint?.Invoke();
        var capacity = PlanFieldCacheCapacity(_fieldValueCache.Count + 1);
        var growth = capacity != _fieldValueCache.Capacity ? EstimateFieldCacheBufferBytes(capacity) : 0;
        return state.MemoryAccounting.AdmitMutation(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateTextBytes(length), growth));
    }

    public double GetFieldNumber(
        AwkFieldIndex index,
        AwkNumberParsingMode mode) =>
        TryReadFieldNumber(index, mode, deferError: false, out var number, out var cachedValue)
            ? number : ReadFieldArithmeticValue(index, cachedValue).AsNumber(state.CancellationCheckpoint);

    public AwkValue GetFieldArithmeticArgument(
        AwkFieldIndex index,
        AwkNumberParsingMode mode) =>
        TryReadFieldNumber(index, mode, deferError: true, out var number, out var cachedValue)
            ? AwkValue.FromNumber(number) : ReadFieldArithmeticValue(index, cachedValue);

    internal bool TryGetFieldArithmeticNumber(AwkFieldIndex index, AwkNumberParsingMode mode, out double number) =>
        TryReadFieldNumber(index, mode, deferError: true, out number, out _);

    private AwkValue ReadFieldArithmeticValue(AwkFieldIndex index, int cachedValue) =>
        cachedValue >= 0 ? _fieldValueCache[cachedValue].Value : GetFieldValue(index);

    internal AwkScalarOperand GetFieldComparison(AwkFieldIndex index,
        AwkNumberParsingMode mode)
    {
        if (index.IsCurrentRecord) return new(GetRecordValue());
        EnsureField(index);
        if (index.Value > _fieldTexts.Count)
            return new(AwkValue.UnassignedText());
        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (field.IsUnassigned) return new(AwkValue.UnassignedText());
        // A later $0 read can retire dirty record slices while reconstructing it.
        // Captured clean record ranges keep their spelling through promotion.
        if (!_recordTextIsCurrent || HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned)
            return new(GetFieldValue(index));
        field = ClassifyInputField(fieldIndex, field, mode);
        return new(field.InputClassification, ReadFieldByteOperand(field));
    }

    internal AwkValue GetFieldComparisonArgument(AwkFieldIndex index, AwkNumberParsingMode mode)
    {
        if (index.IsCurrentRecord) return GetRecordValue();
        EnsureField(index);
        if (index.Value > _fieldTexts.Count) return AwkValue.UnassignedText();
        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (field.IsUnassigned) return AwkValue.UnassignedText();
        if (!_recordTextIsCurrent || HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned)
        {
            var value = GetPreparedFieldValue(fieldIndex);
            return value.StoredKind is AwkValueKind.Number or AwkValueKind.NumericString &&
                value.TryGetStoredComparisonNumber(out var normalized, state.CancellationCheckpoint)
                ? AwkValue.FromNumber(normalized) : value;
        }
        return ClassifyInputField(fieldIndex, field, mode).TryGetComparisonNumber(out var number)
            ? AwkValue.FromNumber(number) : GetPreparedFieldValue(fieldIndex);
    }

    // Speculative computation can consume an established fact, but cannot split,
    // classify, materialize or admit. Ordinary evaluation retains those effects.
    internal bool TryGetStoredFieldComparisonNumber(AwkFieldIndex index, out double number)
    {
        number = 0;
        if (!_recordTextIsCurrent || !index.IsNamedField || index.Value > _fieldTexts.Count) return false;
        var field = _fieldTexts[index.Value - 1];
        if (field.TryGetComparisonNumber(out number)) return true;
        if (field.CacheSlot <= 0) return false;
        var cached = _fieldValueCache[field.CacheSlot - 1];
        return cached.Kind != AwkFieldCacheKind.TextOnly &&
            cached.Value.TryGetStoredComparisonNumber(out number, state.CancellationCheckpoint);
    }

    private AwkRecordField ClassifyInputField(int fieldIndex, AwkRecordField field, AwkNumberParsingMode mode)
    {
        if (field.HasInputClassification) return field;
        return ClassifyInputFieldBytes(fieldIndex, field, mode);
    }

    // Cached facts stay on the short owner read. Parsing and owned-spelling
    // publication need not expand every ordinary comparison consumer.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private AwkRecordField ClassifyInputFieldBytes(int fieldIndex, AwkRecordField field, AwkNumberParsingMode mode)
    {
        if (field.CacheSlot > 0 && _fieldValueCache[field.CacheSlot - 1].HasInputClassification)
            return field.WithInputClassification(_fieldValueCache[field.CacheSlot - 1].Value);

        var value = AwkValue.ClassifyInput(field.BytesInRecord(RecordBytes()), mode, state.CancellationCheckpoint);
        if (field.CacheSlot > 0)
        {
            // Recomposition can rebase an already-owned CSV spelling onto raw
            // record bytes. Its actual cache slot still owns that spelling.
            var cached = _fieldValueCache[field.CacheSlot - 1];
            _fieldValueCache[field.CacheSlot - 1] = new(fieldIndex,
                AwkFieldCacheKind.ClassifiedText, value, cached.Spelling);
            return field.WithInputClassification(value);
        }
        field = field.WithInputClassification(value);
        _fieldTexts[fieldIndex] = field;
        return field;
    }

    // Keep byte parsing/cache publication with the numeric consumer, without an
    // AwkValue round trip or an extra cold parser-result transport boundary.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private bool TryReadFieldNumber(
        AwkFieldIndex index,
        AwkNumberParsingMode mode, bool deferError, out double number, out int cachedValue)
    {
        number = 0;
        cachedValue = -1;
        if (index.IsCurrentRecord) return false;
        EnsureField(index);
        if (index.Value > _fieldTexts.Count) return true;
        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (field.CacheSlot < 0)
        {
            if (field.TryGetNumericValue(out number)) return true;
            if (deferError) return false;
            throw new AwkRuntimeException(AwkMessages.ErrorHexadecimalFloatExponent);
        }
        if (field.CacheSlot > 0)
            return TryReadCachedFieldNumber(fieldIndex, field, mode, deferError, out number, out cachedValue);

        AwkNumberParser.Parse(field.BytesInRecord(RecordBytes()), mode, out var parsed, state.CancellationCheckpoint);
        // Capturing an argument must not diagnose an untaken numeric conversion.
        if (deferError && parsed.Status == AwkNumberParseStatus.MissingHexExponent) return false;
        parsed.ThrowIfError();
        number = parsed.Status == AwkNumberParseStatus.Number ? parsed.Number : 0;
        // A complete successful parse already proves strict input classification.
        // Partial arithmetic prefixes retain their ordinary comparison fallback.
        _fieldTexts.PublishNumericValue(fieldIndex, in field, number,
            parsed.Status == AwkNumberParseStatus.Number && parsed.End == field.ByteLength());
        return true;
    }

    // Cache-backed scalars and owned spelling share their owner read without
    // expanding ordinary raw-field arithmetic with owning-value temporaries.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private bool TryReadCachedFieldNumber(int fieldIndex, AwkRecordField field,
        AwkNumberParsingMode mode, bool deferError, out double number, out int cachedValue)
    {
        number = 0;
        cachedValue = -1;
        var cached = _fieldValueCache[field.CacheSlot - 1];
        if (cached.IsScalar)
        {
            if (cached.Value.IsNumberValue)
            {
                number = cached.Value.StoredNumber;
                return true;
            }
            cachedValue = field.CacheSlot - 1;
            return false;
        }
        if (cached.HasInputClassification)
        {
            if (deferError && cached.Value.HasInputConversionError) return false;
            number = cached.Value.AsNumber(state.CancellationCheckpoint);
            return true;
        }
        // An arithmetic prefix is not a strict classification or scalar value.
        if (cached.Value.IsNumberValue)
        {
            number = cached.Value.StoredNumber;
            return true;
        }

        AwkNumberParser.Parse(cached.Spelling.AsSpan(state.CancellationCheckpoint), mode, out var parsed, state.CancellationCheckpoint);
        if (deferError && parsed.Status == AwkNumberParseStatus.MissingHexExponent) return false;
        parsed.ThrowIfError();
        number = parsed.Status == AwkNumberParseStatus.Number ? parsed.Number : 0;
        _fieldValueCache[field.CacheSlot - 1] = new(fieldIndex, AwkFieldCacheKind.TextOnly,
            AwkValue.FromNumber(number), cached.Spelling);
        return true;
    }

    private void CacheFieldValue(int index, AwkValue value) =>
        CacheFieldEntry(index, new(index, AwkFieldCacheKind.Scalar, value, FieldSpelling(_fieldTexts[index])));

    private void CacheFieldEntry(int index, CachedFieldValue cached)
    {
        var field = _fieldTexts[index];
        var capacity = PlanFieldCacheCapacity(_fieldValueCache.Count + (field.CacheSlot <= 0 ? 1 : 0));
        var retainedBytes = _retainedMemoryBytes;
        if (state.MemoryAccounting.IsEnabled)
        {
            if (capacity != _fieldValueCache.Capacity)
                retainedBytes = AdjustEstimate(retainedBytes,
                    EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity), EstimateFieldCacheBufferBytes(capacity));
            retainedBytes = AdjustEstimate(retainedBytes,
                field.CacheSlot > 0 ? _fieldValueCache[field.CacheSlot - 1].PayloadBytes : 0,
                cached.PayloadBytes);
        }
        using var retirement = ResizeFieldStorage(_fieldTexts.Count, _fieldTexts.Capacity, capacity, retainedBytes);
        SetCachedFieldEntry(index, cached);
    }

    public bool TryGetFieldStringLength(
        AwkFieldIndex index, bool byteMode, out int length)
    {
        EnsureField(index);
        length = 0;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return true;

        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (HasCachedScalarValue(field))
            return false;

        if (byteMode)
        {
            length = field.ByteLength();
            return true;
        }
        if (field.Kind == AwkFieldTextKind.Owned)
        {
            length = FieldSpelling(field).GetRuneLength(state.CancellationCheckpoint);
            return true;
        }
        if (field.RuneLength >= 0)
        {
            length = field.RuneLength;
            return true;
        }
        if (field.Kind == AwkFieldTextKind.RecordSlice && _bufferedStorage is { } storage &&
            storage.IsKnownAsciiAt(_bufferedSlot))
        {
            length = field.ByteLength();
            return true;
        }
        var bytes = field.BytesInRecord(RecordBytes());
        length = AwkText.CountRunes(bytes, state.CancellationCheckpoint);
        state.CancellationCheckpoint?.Invoke();

        _fieldTexts[fieldIndex] = field.WithRuneLength(length);
        return true;
    }

    public bool TryGetUnmaterializedFieldBytes(
        AwkFieldIndex index, out ReadOnlySpan<byte> bytes)
    {
        bytes = default;
        if (!TryGetUnmaterializedField(index, out var field)) return false;
        if (field.ByteLength() != 0) bytes = field.BytesInRecord(RecordBytes());
        return true;
    }

    private bool TryGetUnmaterializedField(AwkFieldIndex index, out AwkRecordField field)
    {
        EnsureField(index);
        field = default;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return true;

        field = _fieldTexts[index.Value - 1];
        return !HasCachedScalarValue(field) && field.Kind != AwkFieldTextKind.Owned;
    }

    internal bool TryGetFieldByteOperand(AwkFieldIndex index, out AwkByteTextOperand operand)
    {
        operand = default;
        if (index.IsCurrentRecord)
        {
            EnsureRecordText();
            if (_bufferedStorage is not { } record) return false;
            var slot = _bufferedSlot;
            operand = new(record, slot, 0, record.LengthAt(slot));
            record.ObserveMemoryAt(slot);
            return true;
        }
        if (!_recordTextIsCurrent || !TryGetUnmaterializedField(index, out var field)) return false;
        operand = ReadFieldByteOperand(field);
        return true;
    }

    private AwkByteTextOperand ReadFieldByteOperand(scoped in AwkRecordField field)
    {
        var length = field.ByteLength();
        if (length == 0) return new(ReadOnlySpan<byte>.Empty);
        if (_bufferedStorage is not { } storage)
            return new(field.BytesInRecord(RecordBytes()));
        var slot = _bufferedSlot;
        var operand = new AwkByteTextOperand(storage, slot, field.RecordStart, length);
        storage.ObserveMemoryAt(slot);
        return operand;
    }

    internal bool TryGetReadyShortFieldBytes(AwkFieldIndex index, out ReadOnlySpan<byte> bytes)
    {
        bytes = default;
        if (!_recordTextIsCurrent || !index.IsNamedField || index.Value > _fieldTexts.Count) return false;
        var field = _fieldTexts[index.Value - 1];
        if (HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned || field.ByteLength() is < 1 or > 7)
            return false;
        // Do not split, classify or admit speculatively. All eligibility checks
        // precede custom-memory access; the returned bytes are consumed once.
        bytes = field.BytesInRecord(RecordBytes());
        return true;
    }

    internal bool TryGetUnmaterializedFieldText(AwkFieldIndex index, out AwkFieldText field)
    {
        var borrowed = TryGetUnmaterializedField(index, out var descriptor);
        field = borrowed ? descriptor.Text(default) : AwkFieldText.Empty;
        return borrowed;
    }

    public void SetRecordText(AwkText text) => SetRecordText(text, state.FieldSplitter.CaptureRecordPlan());

    public void SetRecordText(AwkText text, AwkRecordSplitPlan splitPlan) =>
        PublishRecordText(text, splitPlan, EstimateUnsplitRecordBytes(text, splitPlan, text.ByteLength));

    internal void PublishRecordText(AwkText text, AwkRecordSplitPlan splitPlan, long retainedBytes,
        AwkValue? assignedValue = null)
    {
        ReplaceRetainedMemory(retainedBytes);
        _splitPlan = splitPlan;
        ClearBufferedRecord();
        _recordText = text;
        FinishRecordPublication(assignedValue);
    }

    internal long GetRecordReplacementGrowth(long retainedBytes) =>
        Math.Max(0, retainedBytes - _retainedMemoryBytes);

    public void SetRecordValue(
        AwkValue value,
        AwkRecordSplitPlan splitPlan)
    {
        var text = state.FormatStringByteText(value);
        PublishRecordText(text, splitPlan, EstimateUnsplitRecordBytes(text, splitPlan, text.ByteLength), value);
    }

    private void FinishRecordPublication(AwkValue? assignedValue = null)
    {
        // The new payload is live. Use its length to select reusable field storage.
        _recordWasRecomposed = false;
        _recordValue = assignedValue.GetValueOrDefault();
        _recordValueIsCurrent = assignedValue.HasValue;
        _recordTextIsCurrent = true;
        ClearSplitFields();
    }

    internal bool TryCopyInputField(AwkFieldIndex source, AwkFieldIndex destination)
    {
        if (!source.IsNamedField || !destination.IsNamedField) return false;
        // Field mutation needs the complete record. Finish its owner before taking
        // views, independently of which prefixes the RHS has already read.
        EnsureFields();
        if (source.Value > _fieldTexts.Count || destination.Value > _fieldTexts.Count)
            return false;
        var from = source.Value - 1;
        var to = destination.Value - 1;
        var field = _fieldTexts[from];
        var previous = _fieldTexts[to];
        if (field.Kind != AwkFieldTextKind.RecordSlice || previous.Kind != AwkFieldTextKind.RecordSlice ||
            HasCachedScalarValue(field) || HasCachedScalarValue(previous)) return false;

        var separator = state.ResolveOutputFieldSeparator();
        var payloadBytes = AwkRuntimeMemory.CheckedAdd(
            _fieldPayloadByteLength - _fieldTexts[to].ByteLength(), field.ByteLength());
        var recordBytes = EstimateComposedRecordByteLength(separator.ByteLength, _fieldTexts.Count, payloadBytes);
        var retainedBytes = AdjustEstimate(_retainedMemoryBytes,
            previous.CacheSlot > 0 ? _fieldValueCache[previous.CacheSlot - 1].PayloadBytes : 0, 0);
        retainedBytes = ReplaceSeparatorEstimate(retainedBytes, separator, _fieldTexts.Count);
        using var recompositionAdmission = PreflightRecomposition(retainedBytes, recordBytes);
        state.CancellationCheckpoint?.Invoke();
        ReplaceRetainedMemory(retainedBytes);
        // Both descriptors still refer to the same funded record. Cache slots stay
        // local to each field, including when removing this slot remaps the source.
        RemoveCachedFieldValue(to);
        _fieldTexts[to] = field.CacheSlot <= 0 ? field : field.WithCacheSlot(0);
        _fieldPayloadByteLength = payloadBytes;
        MarkRecordForRecomposition(separator);
        return true;
    }

    public void AssignFieldValue(
        AwkFieldIndex index,
        AwkValue value,
        AwkText outputFieldSeparator)
    {
        // The language boundary handles $0 before evaluating the separator.
        EnsureFields();
        var fieldIndex = index.Value - 1;
        var oldFieldCount = _fieldTexts.Count;
        var newFieldCount = Math.Max(oldFieldCount, index.Value);
        var newCapacity = PlanFieldCapacity(newFieldCount);
        var oldField = fieldIndex < oldFieldCount ? _fieldTexts[fieldIndex] : AwkRecordField.Empty;
        var cacheCapacity = PlanFieldCacheCapacity(_fieldValueCache.Count + (oldField.CacheSlot <= 0 ? 1 : 0));
        var oldIsNumeric = TryGetCachedFieldValue(fieldIndex, out var oldValue) && oldValue.IsNumberValue;
        // Pure numbers retain no formatted field text. Their conversion belongs to the
        // next actual record reconstruction, under its then-current CONVFMT.
        var spelling = value.IsNumberValue ? AwkText.Empty : state.FormatStringByteText(value);
        var cacheSlot = oldField.CacheSlot > 0 ? oldField.CacheSlot : _fieldValueCache.Count + 1;
        var newField = AwkRecordField.FromOwned(cacheSlot, spelling.ByteLength);
        var newPayloadByteLength = AwkRuntimeMemory.CheckedAdd(
            _fieldPayloadByteLength - (oldIsNumeric ? 0 : oldField.ByteLength()),
            newField.ByteLength());
        var newRetainedBytes = EstimateRetainedBytesAfterFieldAssignment(
            fieldIndex, oldField, newCapacity);
        var composedRecordByteLength = EstimateComposedRecordByteLength(
            outputFieldSeparator.ByteLength,
            newFieldCount,
            newPayloadByteLength);
        if (newFieldCount > oldFieldCount)
            newRetainedBytes = AdjustEstimate(newRetainedBytes, _fieldCountValue.RetainedTextMemoryBytes, 0);
        newRetainedBytes = ReplaceSeparatorEstimate(newRetainedBytes, outputFieldSeparator, newFieldCount);
        using var recompositionAdmission = PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        var addedFields = newFieldCount - oldFieldCount;
        using var retirement = ResizeFieldStorage(newFieldCount, newCapacity, cacheCapacity, newRetainedBytes);
        _fieldTexts[fieldIndex] = newField;
        SetCachedFieldValue(fieldIndex, value, spelling);
        _fieldPayloadByteLength = newPayloadByteLength;
        _numericFieldCount += (value.IsNumberValue ? 1 : 0) - (oldIsNumeric ? 1 : 0);
        if (newFieldCount > oldFieldCount)
            _fieldCountValue = AwkValue.FromNumber(newFieldCount);
        MarkRecordForRecomposition(outputFieldSeparator);
        state.Counters.Add(AwkCounter.FieldsPrepared, addedFields);

        long EstimateRetainedBytesAfterFieldAssignment(
            int fieldIndex,
            AwkRecordField oldField,
            int newCapacity)
        {
            var bytes = EstimateFieldBufferResize(newCapacity);
            bytes = AdjustEstimate(bytes,
                oldField.CacheSlot > 0 ? _fieldValueCache[oldField.CacheSlot - 1].PayloadBytes : 0,
                new CachedFieldValue(fieldIndex, AwkFieldCacheKind.Scalar, value, spelling).PayloadBytes);
            return cacheCapacity == _fieldValueCache.Capacity ? bytes : AdjustEstimate(bytes,
                EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity),
                EstimateFieldCacheBufferBytes(cacheCapacity));
        }
    }

    public void ExtendFieldCountForReference(
        AwkFieldIndex index)
    {
        EnsureFields();
        if (index.Value <= _fieldTexts.Count)
            return;

        // Resolving an lvalue creates absent fields but does not assign them or rebuild $0.
        // If earlier writes left $0 dirty, freeze their result before extending the field list.
        EnsureRecordText();
        var addedFields = index.Value - _fieldTexts.Count;
        var newCapacity = PlanFieldCapacity(index.Value);
        var newRetainedBytes = EstimateFieldBufferResize(newCapacity);
        newRetainedBytes = AdjustEstimate(newRetainedBytes, _fieldCountValue.RetainedTextMemoryBytes, 0);
        using var retirement = ResizeFieldStorage(index.Value, newCapacity, _fieldValueCache.Capacity, newRetainedBytes);

        _fieldCountValue = AwkValue.FromNumber(index.Value);
        state.Counters.Add(AwkCounter.FieldsPrepared, addedFields);
    }

    public void AssignFieldCount(
        AwkFieldCount count,
        AwkText outputFieldSeparator) =>
        AssignFieldCount(count, AwkValue.FromNumber(count.Value), outputFieldSeparator);

    public void AssignFieldCount(
        AwkFieldCount count,
        AwkValue value,
        AwkText outputFieldSeparator)
    {
        EnsureFields();
        var oldFieldCount = _fieldTexts.Count;
        var newFieldCount = count.Value;
        var newCapacity = PlanFieldCapacity(newFieldCount);
        var newCacheCount = _fieldValueCache.Count;
        var cacheCapacity = _fieldValueCache.Capacity;
        var newPayloadByteLength = _fieldPayloadByteLength;
        var newNumericFieldCount = _numericFieldCount;
        var newRetainedBytes = EstimateFieldBufferResize(newCapacity);
        newRetainedBytes = AdjustEstimate(newRetainedBytes,
            _fieldCountValue.RetainedTextMemoryBytes, value.RetainedTextMemoryBytes);
        if (newFieldCount < oldFieldCount)
        {
            for (var index = newFieldCount; index < oldFieldCount; index++)
            {
                if (oldFieldCount - newFieldCount >= MutationCheckpointFields &&
                    ((index - newFieldCount) & (MutationCheckpointFields - 1)) == 0)
                    state.CancellationCheckpoint?.Invoke();
                var field = _fieldTexts[index];
                var numeric = TryGetCachedFieldValue(index, out var cached) && cached.IsNumberValue;
                newPayloadByteLength -= numeric ? 0 : field.ByteLength();
                newNumericFieldCount -= numeric ? 1 : 0;
                if (field.CacheSlot <= 0) continue;
                newCacheCount--;
                newRetainedBytes = AdjustEstimate(newRetainedBytes,
                    _fieldValueCache[field.CacheSlot - 1].PayloadBytes, addedBytes: 0);
            }

            if (ShouldStageFields(newFieldCount, newCapacity)) cacheCapacity = newCacheCount;
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
        using var recompositionAdmission = PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        using var retirement = ResizeFieldStorage(newFieldCount, newCapacity, cacheCapacity, newRetainedBytes);
        if (newFieldCount > oldFieldCount)
            state.Counters.Add(AwkCounter.FieldsPrepared, newFieldCount - oldFieldCount);

        _fieldPayloadByteLength = newPayloadByteLength;
        _numericFieldCount = newNumericFieldCount;
        _fieldCountValue = value;
        MarkRecordForRecomposition(outputFieldSeparator);
    }

    private bool ShouldStageFields(int count, int capacity) =>
        Math.Abs(count - _fieldTexts.Count) >= MutationCheckpointFields || capacity != _fieldTexts.Capacity;

    private long EstimateFieldBufferResize(int capacity) => capacity == _fieldTexts.Capacity
        ? _retainedMemoryBytes
        : AdjustEstimate(_retainedMemoryBytes, AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity),
            AwkRuntimeMemory.EstimateRecordFieldListBytes(capacity));

    // Keep replaced storage funded until the caller has also published its field values and NF.
    private AwkMemoryLease ResizeFieldStorage(int count, int capacity, int cacheCapacity, long retainedBytes)
    {
        var stageFields = ShouldStageFields(count, capacity);
        // A staged truncation must rebuild cache slots even if their capacity stays the same.
        var stageCache = cacheCapacity != _fieldValueCache.Capacity || (stageFields && count < _fieldTexts.Count);
        var staging = AwkMemoryLease.Disabled;
        try
        {
            if (stageFields || stageCache)
            {
                var stagedBytes = stageFields ? AwkRuntimeMemory.EstimateRecordFieldListBytes(capacity) : 0;
                if (stageCache)
                    stagedBytes = AwkRuntimeMemory.CheckedAdd(stagedBytes,
                        EstimateFieldCacheBufferBytes(cacheCapacity));
                staging = state.MemoryAccounting.ReserveTransient(stagedBytes);
                var fields = stageFields ? StageFieldTexts(count, capacity) : _fieldTexts;
                var cache = stageCache ? StageFieldValueCache(fields, cacheCapacity) : _fieldValueCache;
                state.CancellationCheckpoint?.Invoke();
                staging = TransferFieldMutationFunding(retainedBytes, staging);
                _fieldTexts = fields;
                _fieldValueCache = cache;
            }
            else
            {
                state.CancellationCheckpoint?.Invoke();
                staging = TransferFieldMutationFunding(retainedBytes, staging);
                if (count < _fieldTexts.Count)
                {
                    for (var index = count; index < _fieldTexts.Count; index++)
                        RemoveCachedFieldValue(index);
                    _fieldTexts.Truncate(count);
                }
            }
            while (_fieldTexts.Count < count) _fieldTexts.Add(AwkRecordField.Unassigned);
            return staging;
        }
        catch
        {
            staging.Dispose();
            throw;
        }
    }

    private AwkMemoryLease TransferFieldMutationFunding(long retainedBytes, AwkMemoryLease staging)
    {
        var change = retainedBytes - _retainedMemoryBytes;
        if (change < 0)
        {
            if (ReferenceEquals(staging, AwkMemoryLease.Disabled))
                staging = state.MemoryAccounting.CreateTransientLease();
            staging.ReceiveTransferred(-change);
        }
        else if (change > 0)
        {
            var transferred = Math.Min(staging.Bytes, change);
            ReplaceRetainedMemory(retainedBytes - transferred);
            staging.Transfer(transferred);
        }
        _retainedMemoryBytes = retainedBytes;
        return staging;
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

    private AwkRecordFieldBuffer StageFieldTexts(int count, int capacity)
    {
        state.CancellationCheckpoint?.Invoke();
        var fields = new AwkRecordFieldBuffer(capacity);
        var destination = fields.UnpublishedTail(count);
        var copiedCount = Math.Min(_fieldTexts.Count, count);
        var source = _fieldTexts.AsSpan()[..copiedCount];
        for (var offset = 0; offset < copiedCount;)
        {
            state.CancellationCheckpoint?.Invoke();
            var length = Math.Min(copiedCount - offset, MutationCheckpointFields);
            source.Slice(offset, length).CopyTo(destination.Slice(offset, length));
            offset += length;
        }
        for (var offset = copiedCount; offset < count;)
        {
            state.CancellationCheckpoint?.Invoke();
            var length = Math.Min(count - offset, MutationCheckpointFields);
            destination.Slice(offset, length).Fill(AwkRecordField.Unassigned);
            offset += length;
        }
        fields.PublishCount(count);
        state.CancellationCheckpoint?.Invoke();
        return fields;
    }

    private bool HasCachedScalarValue(AwkRecordField field) =>
        field.CacheSlot > 0 && _fieldValueCache[field.CacheSlot - 1].IsScalar;

    private bool TryGetCachedFieldValue(int index, out AwkValue value)
    {
        if ((uint)index < (uint)_fieldTexts.Count && _fieldTexts[index].CacheSlot is > 0 and var slot &&
            _fieldValueCache[slot - 1].IsScalar)
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

    // Cache slots belong to this frame's descriptors; scalar snapshots never carry them.
    // All capacity and ownership changes are admitted before these non-fallible mutations.
    private AwkText FieldSpelling(AwkRecordField field) => field.CacheSlot > 0
        ? _fieldValueCache[field.CacheSlot - 1].Spelling : AwkText.Empty;

    private void SetCachedFieldValue(int index, AwkValue value, AwkText spelling)
        => SetCachedFieldEntry(index, new(index, AwkFieldCacheKind.Scalar, value, spelling));

    private void SetCachedFieldEntry(int index, CachedFieldValue cached)
    {
        var field = _fieldTexts[index];
        if (field.CacheSlot <= 0 || field.CacheSlot > _fieldValueCache.Count)
        {
            _fieldValueCache.Add(cached);
            _fieldTexts[index] = field.WithCacheSlot(_fieldValueCache.Count);
        }
        else _fieldValueCache[field.CacheSlot - 1] = cached;
    }

    private void RemoveCachedFieldValue(int index)
    {
        var field = _fieldTexts[index];
        if (field.CacheSlot <= 0)
        {
            _fieldTexts[index] = field.WithCacheSlot(0);
            return;
        }
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

    private List<CachedFieldValue> StageFieldValueCache(AwkRecordFieldBuffer fields, int capacity)
    {
        state.CancellationCheckpoint?.Invoke();
        var cache = new List<CachedFieldValue>(capacity);

        var remap = !ReferenceEquals(fields, _fieldTexts);
        for (var index = 0; index < _fieldValueCache.Count; index++)
        {
            if ((index & (MutationCheckpointFields - 1)) == 0) state.CancellationCheckpoint?.Invoke();
            var entry = _fieldValueCache[index];
            if (entry.FieldIndex >= fields.Count) continue;
            cache.Add(entry);
            // Remapping is allowed only in an unpublished descriptor buffer.
            if (remap) fields[entry.FieldIndex] = fields[entry.FieldIndex].WithCacheSlot(cache.Count);
        }
        state.CancellationCheckpoint?.Invoke();
        return cache;
    }

    private void EnsureRecordText()
    {
        if (!_recordTextIsCurrent && RebuildRecordText())
            state.CancellationCheckpoint?.Invoke();
    }

    private const int LocalRebuildSpellingCapacity = 8;

    private bool RebuildRecordText()
    {
        var fieldCount = _fieldTexts.Count;
        var numericCount = _numericFieldCount;
        var reusable = _rebuildFields is { } spare && FitsRebuildWidth(spare.Capacity, fieldCount) ? spare : null;
        var fieldCapacity = reusable?.Capacity ?? fieldCount;
        var recycled = FitsRebuildWidth(_fieldTexts.Capacity, fieldCount) ? _fieldTexts : null;
        var bufferBytes = state.MemoryAccounting.IsEnabled
            ? AwkRuntimeMemory.CheckedAdd(reusable is null ? AwkRuntimeMemory.EstimateRecordFieldListBytes(fieldCapacity) : 0,
                numericCount > LocalRebuildSpellingCapacity ? AwkRuntimeMemory.EstimateNumericFieldSpellingArrayBytes(numericCount) : 0)
            : 0;
        // A reused array is already funded. The lease still has to admit the new record text.
        using var buffers = bufferBytes == 0 && reusable is not null
            ? state.MemoryAccounting.CreateTransientLease()
            : state.MemoryAccounting.ReserveTransient(bufferBytes);
        if (bufferBytes > 0) state.CancellationCheckpoint?.Invoke();
        return numericCount == 0
            ? PublishRebuiltRecord(fieldCount, _fieldPayloadByteLength, reusable, recycled, buffers)
            : RebuildRecordWithNumbers(fieldCount, numericCount, reusable, recycled, buffers);
    }

    private bool RebuildRecordWithNumbers(int fieldCount, int numericCount,
        AwkRecordFieldBuffer? reusable, AwkRecordFieldBuffer? recycled, AwkMemoryLease buffers)
    {
        Span<(int FieldIndex, int Start, int Length)> formatted = numericCount <= LocalRebuildSpellingCapacity
            ? stackalloc (int, int, int)[LocalRebuildSpellingCapacity] : new (int, int, int)[numericCount];
        var numberText = _rebuildNumberText ??= new(state.MemoryAccounting, capacity: 0);
        numberText.Clear();
        var payloadByteLength = _fieldPayloadByteLength;
        var formattedCount = 0;
        for (var index = 0; index < fieldCount && formattedCount < numericCount; index++)
        {
            if ((index & 1023) == 0) state.CancellationCheckpoint?.Invoke();
            if (!TryGetCachedFieldValue(index, out var value) || !value.IsNumberValue) continue;

            state.CancellationCheckpoint?.Invoke();
            var start = numberText.Length;
            state.AppendConversionNumber(value.StoredNumber, numberText);
            state.CancellationCheckpoint?.Invoke();
            var length = numberText.Length - start;
            payloadByteLength = AwkRuntimeMemory.CheckedAdd(payloadByteLength, length);
            formatted[formattedCount++] = (index, start, length);
        }

        var retiredFunding = PublishRebuiltRecord(fieldCount, payloadByteLength, reusable, recycled, buffers,
            numberText, formatted[..formattedCount]);
        if (numberText.Capacity > AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity)
        {
            RetireRebuildNumberText();
            retiredFunding |= state.MemoryAccounting.IsEnabled;
        }
        return retiredFunding;
    }

    private bool PublishRebuiltRecord(int fieldCount, int payloadByteLength, AwkRecordFieldBuffer? reusable,
        AwkRecordFieldBuffer? recycled, AwkMemoryLease buffers, AwkUtf8Builder? numberText = null,
        scoped ReadOnlySpan<(int FieldIndex, int Start, int Length)> formatted = default)
    {
        var fieldCapacity = reusable?.Capacity ?? fieldCount;
        var byteLength = EstimateComposedRecordByteLength(_recompositionSeparator.ByteLength,
            fieldCount, payloadByteLength);
        var retained = EstimateRetainedBytes(AwkRuntimeMemory.EstimateTextBytes(byteLength),
            fieldCount, fieldCapacity, recycled?.Capacity ?? 0, _fieldValueCache, _fieldCountValue);
        var fields = reusable ?? new AwkRecordFieldBuffer(fieldCapacity);
        var stagedFields = fields.UnpublishedTail(fieldCount);
        var checkpoint = state.CancellationCheckpoint;
        var destination = AwkText.AllocateOwnedBytes(byteLength, state.MemoryAccounting, buffers, checkpoint);
        var separator = byteLength == 0 ? default : _recompositionSeparator.AsSpan(checkpoint);
        // Resolve the source after admission. The frame keeps it live through the copy.
        var record = byteLength == 0 ? default : RecordBytes();
        var sourceFields = _fieldTexts.AsSpan();
        var cache = CollectionsMarshal.AsSpan(_fieldValueCache);
        var numericBytes = numberText is not null ? numberText.AsSpan() : default;
        var offset = 0;
        var bytesUntilCheck = AwkRuntimeLimits.CancellationCheckBytes;
        var nextFormatted = 0;
        for (var index = 0; index < fieldCount; index++)
        {
            if ((index & 1023) == 0)
            {
                checkpoint?.Invoke();
                bytesUntilCheck = AwkRuntimeLimits.CancellationCheckBytes;
            }
            if (index > 0 && !separator.IsEmpty)
                AwkText.CopyFragment(separator, destination, ref offset, ref bytesUntilCheck, checkpoint);
            ref readonly var descriptor = ref sourceFields[index];
            ReadOnlySpan<byte> bytes;
            if (nextFormatted < formatted.Length && formatted[nextFormatted].FieldIndex == index)
            {
                var range = formatted[nextFormatted++];
                bytes = numericBytes.Slice(range.Start, range.Length);
            }
            else bytes = descriptor.Bytes(record, cache, checkpoint);
            descriptor.WriteRebased(out stagedFields[index], offset, bytes.Length);
            if (!bytes.IsEmpty)
                AwkText.CopyFragment(bytes, destination, ref offset, ref bytesUntilCheck, checkpoint);
        }
        var recordText = byteLength == 0 ? AwkText.Empty : state.AdoptText(destination);
        checkpoint?.Invoke();
        fields.PublishCount(fieldCount);
        ClearBufferedRecord();
        _recordText = recordText;
        _fieldTexts = fields;
        recycled?.Clear();
        _rebuildFields = recycled;
        _recordTextIsCurrent = true;
        _recordWasRecomposed = true;
        _recompositionSeparator = AwkText.Empty;
        return TransferFieldProductionFunding(retained, buffers);
    }

    private void RetireRebuildNumberText()
    {
        var spelling = _rebuildNumberText;
        _rebuildNumberText = null;
        spelling?.Dispose();
    }

    public void PrepareOutputFieldSeparatorAssignment()
    {
        // GNU's OFS setter freezes a dirty record before changing the separator. Only
        // numeric fields need that checkpoint: ordinary text already has fixed bytes.
        if (_numericFieldCount > 0)
            EnsureRecordText();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal void PrepareAdmittedFields(int maximum)
    {
        // Do not move allocation/admission ahead of its language operation. Warm
        // record-local storage can publish a shared prefix without another owner.
        if (_fieldsAreSplit || maximum <= _fieldTexts.Count || maximum > _fieldTexts.Capacity) return;
        var kind = _splitPlan.IncrementalKind;
        if (kind == AwkIncrementalFieldKind.None ||
            (!HasBufferedRecord && !_recordText.HasAvailableBytes) ||
            (kind == AwkIncrementalFieldKind.SingleByte && !_splitPlan.Separator.ByteText.HasAvailableBytes)) return;
        TryExtendFields(maximum, requireComplete: false);
    }

    private void EnsureField(AwkFieldIndex index)
    {
        if (_fieldsAreSplit || (index.IsNamedField && index.Value <= _fieldTexts.Count))
            return;
        EnsureMissingField(index);
    }

    // Ready readers keep their short guard; cold production owns scanning,
    // allocation and publication without expanding each field consumer's stack.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void EnsureMissingField(AwkFieldIndex index)
    {
        var upperBound = (long)RecordByteLength + 1;
        if (!index.IsNamedField ||
            index.Value >= upperBound || !_splitPlan.SupportsIncrementalFields)
        {
            EnsureFields();
            return;
        }

        TryExtendFields(index.Value, requireComplete: false);
    }

    // A scanner writes only the admitted tail. Publish its logical count after
    // completion and cancellation checks, so failure needs no descriptor rollback.
    private bool TryExtendFields(int targetCount, bool requireComplete)
    {

        var previousCount = _fieldTexts.Count;
        if (previousCount == 0)
        { state.Counters.Add(AwkCounter.FieldSplits, 1); }
        var grows = _fieldTexts.Capacity < targetCount;
        var (fields, staging) = grows ? StageFieldPrefix(targetCount) : (_fieldTexts, AwkMemoryLease.Disabled);
        bool complete;
        bool retiredFunding;
        try
        {
            var position = state.FieldSplitter.ExtendRecordFields(RecordBytes(), _splitPlan, _splitPosition,
                fields.UnpublishedTail(targetCount), out var addedCount, out var addedPayloadBytes);
            complete = position < 0;
            if (requireComplete && !complete) return false;
            // An incremental tail adds disjoint slices of the current record.
            var payloadBytes = _fieldPayloadByteLength + addedPayloadBytes;
            // Incremental slices leave cached/owned payload unchanged. Only capacity
            // replacement or split-plan retirement changes the retained estimate.
            var retained = state.MemoryAccounting.IsEnabled && grows ? AdjustEstimate(_retainedMemoryBytes,
                AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity),
                AwkRuntimeMemory.EstimateRecordFieldListBytes(fields.Capacity)) : _retainedMemoryBytes;
            if (complete && state.MemoryAccounting.IsEnabled)
                retained = AdjustEstimate(retained, _splitPlan.RetainedMemoryBytes, 0);
            state.CancellationCheckpoint?.Invoke();
            fields.PublishCount(previousCount + addedCount);
            if (grows) _fieldTexts = fields;
            _splitPosition = position;
            _fieldPayloadByteLength = payloadBytes;
            state.Counters.Add(AwkCounter.FieldsPrepared, addedCount);

            if (complete)
            {
                _fieldsAreSplit = true;
                _fieldCountValue = AwkValue.FromNumber(fields.Count);
                _splitPlan = default;
            }
            retiredFunding = TransferFieldProductionFunding(retained, staging);
        }
        finally { staging.Dispose(); }
        // Actual funding retirement can cancel the execution. Observe it before
        // the caller consumes the completed fields or publishes a numeric result.
        if (retiredFunding) state.CancellationCheckpoint?.Invoke();
        return complete;
    }

    // Producers have admitted the full new backing before publication. Keep
    // retired old capacity/plan funding in that same staging lease until cleanup;
    // no budget callback can observe an unfinished or underfunded handoff.
    private bool TransferFieldProductionFunding(long retainedBytes, AwkMemoryLease staging)
    {
        if (!state.MemoryAccounting.IsEnabled) return false;
        if (ReferenceEquals(staging, AwkMemoryLease.Disabled))
        {
            if (retainedBytes == _retainedMemoryBytes) return false;
            var retired = retainedBytes < _retainedMemoryBytes;
            ReplaceRetainedMemory(retainedBytes);
            return retired;
        }
        if (retainedBytes >= _retainedMemoryBytes)
            staging.Transfer(retainedBytes - _retainedMemoryBytes);
        else staging.ReceiveTransferred(_retainedMemoryBytes - retainedBytes);
        _retainedMemoryBytes = retainedBytes;
        return staging.Bytes > 0;
    }

    private (AwkRecordFieldBuffer Fields, AwkMemoryLease Staging) StageFieldPrefix(int targetCount)
    {
        // Reused storage needs only bounded scan and publication checks. Actual
        // growth checks cancellation before admission and again after its callback.
        state.CancellationCheckpoint?.Invoke();
        var capacity = (int)Math.Max(targetCount, Math.Min(Array.MaxLength, (long)_fieldTexts.Capacity * 2));
        var staging = state.MemoryAccounting.ReserveTransient(state.MemoryAccounting.IsEnabled
            ? AwkRuntimeMemory.EstimateRecordFieldListBytes(capacity) : 0);
        try
        {
            // Frame cleanup keeps this lease through publication or interrupted scans.
            return (StageFieldTexts(_fieldTexts.Count, capacity), staging);
        }
        catch { staging.Dispose(); throw; }
    }

    private void EnsureFields()
    {
        if (_fieldsAreSplit)
            return;

        if (_fieldTexts.Count > 0 && _splitPlan.SupportsIncrementalFields &&
            TryExtendFields(_fieldTexts.Capacity, requireComplete: true))
            return;

        if (_fieldTexts.Count == 0)
        { state.Counters.Add(AwkCounter.FieldSplits, 1); }
        var reusable = _fieldTexts.Count == 0 && _fieldTexts.Capacity > 0 ? _fieldTexts : null;
        var reusableCapacity = reusable?.Capacity ?? 0;
        var previousCount = _fieldTexts.Count;
        var initialValues = _fieldValueCache;
        var previousCacheCount = initialValues.Count;
        // Keep a private empty view until staging succeeds; later field mutation owns its cells.
        if (reusable is not null) _fieldTexts = EmptyFieldTexts;
        var fieldTexts = reusable ?? new();
        var writer = new AwkRecordFieldWriter(fieldTexts, _fieldValueCache, state.MemoryAccounting, state.CancellationCheckpoint, AwkMemoryLease.Disabled);
        var published = false;
        var retiredFunding = false;
        try
        {
            // Copy the published cells with their cached numeric and text observations.
            if (previousCount > 0)
                writer.CopyPrefix(_fieldTexts.AsSpan(), _fieldPayloadByteLength);
            if (_splitPlan.RequiresRegexSubject)
                state.FieldSplitter.FillRecordFields(OwnedRecordText(), _splitPlan, ref writer);
            else
                state.FieldSplitter.FillRecordFields(RecordBytes(), _splitPlan, ref writer, _splitPosition);
            var retainedBytes = state.MemoryAccounting.IsEnabled ? EstimateRetainedBytes(
                AwkRuntimeMemory.EstimateTextBytes(_recordText), fieldTexts.Count, fieldTexts.Capacity,
                _rebuildFields?.Capacity ?? 0, writer.Values, default) : 0;
            state.CancellationCheckpoint?.Invoke();
            _fieldTexts = fieldTexts;
            _fieldValueCache = writer.Values;
            published = true;
            _fieldCountValue = AwkValue.FromNumber(fieldTexts.Count);
            _fieldPayloadByteLength = writer.PayloadBytes;
            state.Counters.Add(AwkCounter.FieldsPrepared, _fieldTexts.Count - previousCount);
            _fieldsAreSplit = true;
            _splitPlan = default;
            retiredFunding = TransferFieldProductionFunding(retainedBytes, writer.Staging);
        }
        catch
        {
            if (!published && reusable is not null)
            {
                reusable.Clear();
                _fieldTexts = reusable.Capacity == reusableCapacity ? reusable : new();
            }
            throw;
        }
        finally
        {
            if ((!published || !ReferenceEquals(writer.Values, initialValues)) && initialValues.Count > previousCacheCount)
                initialValues.RemoveRange(previousCacheCount, initialValues.Count - previousCacheCount);
            writer.Dispose();
        }
        if (retiredFunding) state.CancellationCheckpoint?.Invoke();
    }

    private void ClearSplitFields(bool releaseCache = false)
    {
        _fieldCountValue = AwkValue.FromNumber(0);
        var recordByteLength = RecordByteLength;
        // Producers only write unpublished frame-owned destinations.
        if (!releaseCache && _fieldTexts.Capacity > 0 && ReuseFieldBuffer(_fieldTexts.Capacity, recordByteLength))
            _fieldTexts.Clear();
        else if (_fieldTexts.Capacity != 0)
            _fieldTexts = new();
        if (releaseCache || !ReuseFieldBuffer(_rebuildFields?.Capacity ?? 0, recordByteLength))
            _rebuildFields = null;
        if (!releaseCache && ReuseCacheBuffer(recordByteLength))
            _fieldValueCache.Clear();
        else
            _fieldValueCache = [];
        _fieldsAreSplit = false;
        _splitPosition = 0;
        _recompositionSeparator = AwkText.Empty;
        _fieldPayloadByteLength = 0;
        _numericFieldCount = 0;
    }

    private AwkMemoryMutationAdmission PreflightRecomposition(long newRetainedBytes, int recordByteLength)
    {
        if (!state.MemoryAccounting.IsEnabled)
            return default;
        var retainedIncrease = Math.Max(0, newRetainedBytes - _retainedMemoryBytes);
        // The synchronous mutation admits its destination while the old record
        // and dirty fields are live. Actual retained charges consume that floor.
        return state.MemoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
            retainedIncrease,
            AwkRuntimeMemory.EstimateTextBytes(recordByteLength)));
    }

    private void MarkRecordForRecomposition(AwkText outputFieldSeparator)
    {
        _recordValue = default;
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
        if (!state.MemoryAccounting.IsEnabled)
            return;

        if (newBytes > _retainedMemoryBytes)
        {
            state.MemoryAccounting.Charge(newBytes - _retainedMemoryBytes);
        }
        else if (_retainedMemoryBytes > newBytes)
        {
            state.MemoryAccounting.Refund(_retainedMemoryBytes - newBytes);
        }

        _retainedMemoryBytes = newBytes;
    }

    internal long EstimateUnsplitRecordBytes(AwkText text, AwkRecordSplitPlan plan, int recordByteLength)
    {
        if (!state.MemoryAccounting.IsEnabled) return 0;
        // Retain already-funded capacity, never allocate speculatively. The same
        // incoming length selects storage here and when the owner clears its cells.
        var spareCapacity = _rebuildFields?.Capacity ?? 0;
        var buffers = (ReuseFieldBuffer(_fieldTexts.Capacity, recordByteLength)
            ? AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity) : 0) +
            (ReuseFieldBuffer(spareCapacity, recordByteLength) ? AwkRuntimeMemory.EstimateRecordFieldListBytes(spareCapacity) : 0) +
            (ReuseCacheBuffer(recordByteLength) ? EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity) : 0);
        var bytes = plan.RetainedMemoryBytes;
        var textBytes = text.EstimatedRetainedMemoryBytes;
        if (textBytes != 0) bytes = AwkRuntimeMemory.CheckedAdd(bytes, textBytes);
        return buffers == 0 ? bytes : AwkRuntimeMemory.CheckedAdd(bytes, buffers);
    }

    private long EstimateRetainedBytes(long recordTextBytes, int fieldCount, int fieldCapacity, int spareCapacity,
        List<CachedFieldValue> fieldValueCache, AwkValue fieldCountValue)
    {
        if (!state.MemoryAccounting.IsEnabled) return 0;
        var bytes = AwkRuntimeMemory.CheckedAdd(recordTextBytes, fieldCountValue.RetainedTextMemoryBytes);
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateRecordFieldListBytes(fieldCapacity));
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateRecordFieldListBytes(spareCapacity));
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, EstimateFieldCacheBufferBytes(fieldValueCache.Capacity));
        for (var index = 0; index < fieldValueCache.Count; index++)
        {
            if ((index & 1023) == 0) state.CancellationCheckpoint?.Invoke();
            if (fieldValueCache[index].FieldIndex < fieldCount)
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, fieldValueCache[index].PayloadBytes);
        }
        return bytes;
    }

    private long EstimateFieldCacheBufferBytes(int capacity) =>
        state.MemoryAccounting.IsEnabled ? AwkRuntimeMemory.EstimateFieldValueListBytes(capacity) : 0;

    // At most one field per byte plus an empty tail; allow ordinary doubling.
    // Long calculations avoid overflow for the largest representable records.
    private static bool ReuseFieldBuffer(int capacity, int recordByteLength) =>
        capacity <= SmallFieldBufferCapacity || capacity <= 2L * (recordByteLength + 1L);

    private static bool FitsRebuildWidth(int capacity, int fieldCount) =>
        fieldCount > 0 && capacity >= fieldCount && capacity <= 2L * fieldCount;

    private bool ReuseCacheBuffer(int recordByteLength) =>
        _fieldValueCache.Capacity <= SmallFieldCacheCapacity ||
        _fieldValueCache.Capacity <= 2L * (recordByteLength + 1L);

    private long AdjustEstimate(long currentBytes, long removedBytes, long addedBytes)
    {
        if (!state.MemoryAccounting.IsEnabled)
            return 0;

        if (removedBytes > currentBytes)
            throw new InvalidOperationException("The field memory estimate is inconsistent.");

        var remainingBytes = currentBytes - removedBytes;
        return addedBytes == 0 ? remainingBytes : AwkRuntimeMemory.CheckedAdd(remainingBytes, addedBytes);
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
