using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkFieldFrame(
    AwkRuntimeCounters counters,
    AwkMemoryAccounting governor,
    Func<AwkValue, AwkText> formatStringByteText,
    AwkFieldSplitter fieldSplitter,
    Func<AwkText, AwkValue> createFieldValue,
    Func<AwkText, AwkValue> createTextValue,
    Func<AwkText> resolveOutputFieldSeparator,
    Action? checkpoint = null)
{
    private const int MutationCheckpointFields = 1024;
    // Bound reusable descriptor/cache storage; their headers are admitted separately.
    private const int MaxReusableBufferBytes = 4096;
    // Bound descriptor payload; its list/array headers are admitted separately.
    private const int MaxReusableFieldListCapacity =
        MaxReusableBufferBytes / AwkRuntimeMemory.RecordFieldBytes;
    private const int MaxReusableFieldCacheCapacity =
        (MaxReusableBufferBytes - AwkRuntimeMemory.ListBufferOverheadBytes) / AwkRuntimeMemory.CachedFieldValueBytes;
    // Private empty view while a reusable buffer is staged; never mutated.
    private static readonly AwkRecordFieldBuffer EmptyFieldTexts = new();

    private AwkRecordStorage? _inputStorage;
    private AwkRecordStorage? _bufferedStorage;
    private ReadOnlyMemory<byte> _bufferedRecord;
    private int _bufferedSlot;
    private bool HasBufferedRecord => _bufferedStorage is not null;

    internal AwkRecordStorage InputRecordStorage =>
        _inputStorage ??= new(governor, counters, checkpoint ?? (static () => { }), this);

    private ReadOnlyMemory<byte> BufferedRecord => _bufferedRecord;
    private int RecordByteLength => HasBufferedRecord ? BufferedRecord.Length : _recordText.ByteLength;
    private ReadOnlySpan<byte> RecordBytes()
    {
        // Borrowed-byte inspection is bounded and has no callback or effect.
        // Owners and long consumers poll; owned text keeps its materialization check.
        return HasBufferedRecord ? BufferedRecord.Span : _recordText.AsSpan(checkpoint);
    }

    private void ClearBufferedRecord()
    {
        _bufferedStorage = null;
        _bufferedRecord = default;
    }

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
            AwkRuntimeMemory.EstimateTextBytes(BufferedRecord.Length));
        using var admission = governor.AdmitMutation(Math.Max(0, retained - _retainedMemoryBytes));
        var text = AwkText.CopyTrustedBytes(BufferedRecord.Span, counters, governor, checkpoint);
        checkpoint?.Invoke();
        ReplaceRetainedMemory(retained);
        _recordText = text;
        ClearBufferedRecord();
        return text;
    }

    private AwkText MaterializeField(AwkRecordField field)
    {
        if (field.CacheSlot > 0 && !_fieldValueCache[field.CacheSlot - 1].IsScalar)
            return _fieldValueCache[field.CacheSlot - 1].Spelling;
        if (!HasBufferedRecord || field.Kind != AwkFieldTextKind.RecordSlice)
            return FieldText(field).Materialize(_recordText, counters, governor, checkpoint);
        return field.IsWholeRecord(RecordByteLength) ? OwnedRecordText() :
            AwkText.CopyTrustedBytes(field.BytesInRecord(RecordBytes()), counters, governor, checkpoint);
    }

    internal void SetBufferedRecord(AwkRecordStorage storage, int slot, AwkRecordPublication publication)
    {
        ReplaceRetainedMemory(publication.RetainedBytes);
        _splitPlan = publication.SplitPlan;
        _recordText = AwkText.Empty;
        _bufferedStorage = storage;
        _bufferedSlot = slot;
        _bufferedRecord = storage.ContentAt(slot);
        _recordWasRecomposed = false;
        _recordValue = default;
        _recordValueIsCurrent = false;
        _recordTextIsCurrent = true;
        ClearSplitFields();
    }

    // Only the owning storage can change this view before retiring a host borrow.
    internal void RebindBufferedRecord(AwkRecordStorage storage, int slot, ReadOnlyMemory<byte> bytes)
    {
        if (ReferenceEquals(_bufferedStorage, storage) && _bufferedSlot == slot)
            _bufferedRecord = bytes;
    }

    private AwkText _recordText = AwkText.Empty;
    private AwkValue _recordValue = AwkValue.FromText(string.Empty);
    private AwkValue _fieldCountValue = AwkValue.FromNumber(0);
    private AwkRecordFieldBuffer _fieldTexts = new();
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
    public bool SupportsIncrementalFields => _splitPlan.SupportsIncrementalFields;

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
        _splitPlan = default;
    }

    internal AwkByteTextOperand RecordByteOperand
    {
        get
        {
            EnsureRecordText();
            return HasBufferedRecord ? new(BufferedRecord.Span) : new(_recordText);
        }
    }

    internal bool TryGetBufferedRecordBytes(out ReadOnlySpan<byte> bytes)
    {
        EnsureRecordText();
        bytes = HasBufferedRecord ? BufferedRecord.Span : default;
        return HasBufferedRecord;
    }

    public AwkText RecordAwkText
    {
        get
        {
            return OwnedRecordText();
        }
    }

    public bool TryGetRecomposedRecordFields(
        out AwkRecordFields fields, out ReadOnlySpan<byte> record, out AwkText separator,
        out int byteLength)
    {
        // Only text has a stable spelling without reconstruction. Numeric fields must
        // still freeze their CONVFMT spelling when $0 is first observed.
        var available = !_recordTextIsCurrent && _numericFieldCount == 0;
        fields = available ? new(_fieldTexts.AsSpan(), CollectionsMarshal.AsSpan(_fieldValueCache)) : default;
        record = RecordBytes();
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
            _recordValue = (_recordWasRecomposed ? createTextValue : createFieldValue)(_recordText);
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
        EnsureField(index);
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return AwkValue.FromText(string.Empty);

        var fieldIndex = index.Value - 1;
        if (TryGetCachedFieldValue(fieldIndex, out var cached)) return cached;
        return MaterializeFieldValue(fieldIndex);
    }

    // Owned construction and cache publication stay at this frame. Keep their
    // allocation/admission machinery out of callers performing ordinary cached reads.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private AwkValue MaterializeFieldValue(int fieldIndex)
    {
        var value = createFieldValue(MaterializeField(_fieldTexts[fieldIndex]));
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
            if (!HasBufferedRecord) return false;
            input = AwkTextInput.FromMemory(BufferedRecord, 0);
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
        if (HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned) return false;
        if (HasBufferedRecord)
            input = AwkTextInput.FromMemory(BufferedRecord.Slice(field.RecordStart, field.ByteLength()), index.Value);
        else
        {
            // Capture the immutable backing before long-input or materialization polling can
            // reenter and replace this frame's current record.
            var record = _recordText;
            input = AwkTextInput.FromTextRange(record, field.RecordStart, field.ByteLength(), index.Value);
            if (record.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes) checkpoint?.Invoke();
            _ = record.AsSpan(checkpoint);
        }
        return true;
    }

    internal bool TryCaptureReaderText(AwkFieldIndex index,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkReadBuffer? borrow,
        out int offset, out int length)
    {
        borrow = null;
        offset = length = 0;
        if (_bufferedStorage is not { } storage) return false;
        ReadOnlySpan<byte> bytes;
        if (index.IsCurrentRecord)
        {
            if (!_recordTextIsCurrent || _recordValueIsCurrent && _recordValue.IsNumberValue) return false;
            bytes = BufferedRecord.Span;
        }
        else if (!TryGetUnmaterializedFieldBytes(index, out bytes)) return false;
        if (!storage.TryCaptureText(_bufferedSlot, bytes, out borrow, out offset)) return false;
        length = bytes.Length;
        return true;
    }

    // Keep immutable spelling with the record's existing cache owner. Repeated
    // captures and later scalar promotion reuse it without classifying early.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private AwkText CaptureFieldTextSnapshot(int index, AwkRecordField field)
    {
        var capacity = PlanFieldCacheCapacity(_fieldValueCache.Count + 1);
        var admitted = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateTextBytes(field.ByteLength()),
            capacity != _fieldValueCache.Capacity ? EstimateFieldCacheBufferBytes(capacity) : 0);
        using var admission = governor.AdmitMutation(admitted);
        var text = AwkText.CopyTrustedBytes(field.BytesInRecord(RecordBytes()), counters, governor, checkpoint);
        var classified = field.HasInputClassification;
        var facts = classified ? field.InputClassification :
            field.TryGetNumericValue(out var number) ? AwkValue.FromNumber(number) : default;
        CacheFieldEntry(index, new(index,
            classified ? AwkFieldCacheKind.ClassifiedText : AwkFieldCacheKind.TextOnly, facts, text));
        return text;
    }

    public double GetFieldNumber(
        AwkFieldIndex index,
        AwkNumberParsingMode mode) =>
        TryReadFieldNumber(index, mode, deferError: false, out var number, out var cachedValue)
            ? number : ReadFieldArithmeticValue(index, cachedValue).AsNumber(checkpoint);

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
        EnsureField(index);
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return new(AwkValue.FromText(string.Empty));
        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        // A later $0 read can retire dirty record slices while reconstructing it.
        // Clean input storage survives all prepared computational operands.
        if (!_recordTextIsCurrent || HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned)
            return new(GetFieldValue(index));
        field = ClassifyInputField(fieldIndex, field, mode);
        return new(field.InputClassification, field.BytesInRecord(RecordBytes()));
    }

    internal bool TryGetFieldComparisonNumber(AwkFieldIndex index, AwkNumberParsingMode mode, out double number)
    {
        EnsureField(index);
        number = 0;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count) return false;
        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (!_recordTextIsCurrent || HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned)
        {
            var value = GetFieldValue(index);
            return value.StoredKind is AwkValueKind.Number or AwkValueKind.NumericString &&
                value.TryGetStoredComparisonNumber(out number, checkpoint);
        }
        return ClassifyInputField(fieldIndex, field, mode).TryGetComparisonNumber(out number);
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
            cached.Value.TryGetStoredComparisonNumber(out number, checkpoint);
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
        counters.RecordNumericFieldByteParse();
        var value = AwkValue.ClassifyInput(field.BytesInRecord(RecordBytes()), mode, checkpoint);
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
        EnsureField(index);
        number = 0;
        cachedValue = -1;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count) return true;
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

        counters.RecordNumericFieldByteParse();
        AwkNumberParser.Parse(field.BytesInRecord(RecordBytes()), mode, out var parsed, checkpoint);
        // Capturing an argument must not diagnose an untaken numeric conversion.
        if (deferError && parsed.Status == AwkNumberParseStatus.MissingHexExponent) return false;
        parsed.ThrowIfError();
        number = parsed.Status == AwkNumberParseStatus.Number ? parsed.Number : 0;
        _fieldTexts[fieldIndex] = field.WithNumericValue(number);
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
            cachedValue = field.CacheSlot - 1;
            return false;
        }
        if (cached.HasInputClassification)
        {
            if (deferError && cached.Value.HasInputConversionError) return false;
            number = cached.Value.AsNumber(checkpoint);
            return true;
        }
        // An arithmetic prefix is not a strict classification or scalar value.
        if (cached.Value.IsNumberValue)
        {
            number = cached.Value.StoredNumber;
            return true;
        }
        counters.RecordNumericFieldByteParse();
        AwkNumberParser.Parse(cached.Spelling.AsSpan(checkpoint), mode, out var parsed, checkpoint);
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
        if (governor.IsEnabled)
        {
            if (capacity != _fieldValueCache.Capacity)
                retainedBytes = AdjustEstimate(retainedBytes,
                    EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity), EstimateFieldCacheBufferBytes(capacity));
            retainedBytes = AdjustEstimate(retainedBytes,
                field.CacheSlot > 0 ? _fieldValueCache[field.CacheSlot - 1].PayloadBytes : 0,
                cached.PayloadBytes);
        }
        if (capacity != _fieldValueCache.Capacity)
            GrowFieldValueCache(capacity, retainedBytes);
        else
        {
            checkpoint?.Invoke();
            ReplaceRetainedMemory(retainedBytes);
        }
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
            length = FieldSpelling(field).GetRuneLength(checkpoint);
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
        length = AwkText.CountRunes(bytes, checkpoint);
        checkpoint?.Invoke();
        counters.RecordFieldRuneBytesScanned(bytes.Length);
        _fieldTexts[fieldIndex] = field.WithRuneLength(length);
        return true;
    }

    public bool TryGetUnmaterializedFieldBytes(
        AwkFieldIndex index, out ReadOnlySpan<byte> bytes)
    {
        EnsureField(index);
        bytes = default;
        if (!index.IsNamedField || index.Value > _fieldTexts.Count)
            return true;

        var fieldIndex = index.Value - 1;
        var field = _fieldTexts[fieldIndex];
        if (HasCachedScalarValue(field) || field.Kind == AwkFieldTextKind.Owned)
            return false;
        bytes = field.BytesInRecord(RecordBytes());
        return true;
    }

    internal bool TryGetStableFieldBytes(AwkFieldIndex index, out ReadOnlySpan<byte> bytes)
    {
        // An immediate $0 read can retire dirty slices while rebuilding the record.
        if (_recordTextIsCurrent) return TryGetUnmaterializedFieldBytes(index, out bytes);
        bytes = default;
        return false;
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

    public bool TryGetUnmaterializedFieldText(
        AwkFieldIndex index,
        out AwkFieldText field, out ReadOnlySpan<byte> record)
    {
        // Reuse byte-view admission without transporting descriptors for existing byte consumers.
        var borrowed = TryGetUnmaterializedFieldBytes(index, out _);
        field = borrowed && index.IsNamedField && index.Value <= _fieldTexts.Count
            ? FieldText(_fieldTexts[index.Value - 1]) : AwkFieldText.Empty;
        record = RecordBytes();
        return borrowed;
    }

    public void SetRecordText(AwkText text) => SetRecordText(text, fieldSplitter.CaptureRecordPlan());

    public void SetRecordText(AwkText text, AwkRecordSplitPlan splitPlan) =>
        PublishRecordText(text, PrepareRecordPublication(text, splitPlan));

    internal void PublishRecordText(AwkText text, AwkRecordPublication publication)
    {
        ReplaceRetainedMemory(publication.RetainedBytes);
        _splitPlan = publication.SplitPlan;
        ClearBufferedRecord();
        _recordText = text;
        _recordWasRecomposed = false;
        _recordValue = default;
        _recordValueIsCurrent = false;
        _recordTextIsCurrent = true;
        ClearSplitFields();
    }

    internal AwkRecordPublication PrepareRecordPublication(AwkText text, AwkRecordSplitPlan splitPlan)
    {
        var retained = EstimateEmptyRecordBytes(text, splitPlan);
        return new(splitPlan, retained, Math.Max(0, retained - _retainedMemoryBytes));
    }

    public void SetRecordValue(
        AwkValue value,
        AwkRecordSplitPlan splitPlan)
    {
        var text = formatStringByteText(value);
        ReplaceRetainedMemory(EstimateEmptyRecordBytes(text, splitPlan));
        _splitPlan = splitPlan;
        ClearBufferedRecord();
        _recordText = text;
        _recordWasRecomposed = false;
        _recordValue = value;
        _recordValueIsCurrent = true;
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

        var separator = resolveOutputFieldSeparator();
        var payloadBytes = AwkRuntimeMemory.CheckedAdd(
            _fieldPayloadByteLength - _fieldTexts[to].ByteLength(), field.ByteLength());
        var recordBytes = EstimateComposedRecordByteLength(separator.ByteLength, _fieldTexts.Count, payloadBytes);
        var retainedBytes = AdjustEstimate(_retainedMemoryBytes,
            previous.CacheSlot > 0 ? _fieldValueCache[previous.CacheSlot - 1].PayloadBytes : 0, 0);
        retainedBytes = ReplaceSeparatorEstimate(retainedBytes, separator, _fieldTexts.Count);
        using var peak = PreflightRecomposition(retainedBytes, recordBytes);
        checkpoint?.Invoke();
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
        var stageCache = cacheCapacity != _fieldValueCache.Capacity;
        var oldIsNumeric = TryGetCachedFieldValue(fieldIndex, out var oldValue) && oldValue.IsNumberValue;
        // Pure numbers retain no formatted field text. Their conversion belongs to the
        // next actual record reconstruction, under its then-current CONVFMT.
        var spelling = value.IsNumberValue ? AwkText.Empty : formatStringByteText(value);
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
        using var recompositionPeak = PreflightRecomposition(newRetainedBytes, composedRecordByteLength);
        var addedFields = newFieldCount - oldFieldCount;
        var stageFields = addedFields >= MutationCheckpointFields || newCapacity != _fieldTexts.Capacity;
        if (stageFields || stageCache)
        {
            var stagedBytes = stageFields ? AwkRuntimeMemory.EstimateRecordFieldListBytes(newCapacity) : 0;
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
        while (_fieldTexts.Count < newFieldCount) _fieldTexts.Add(AwkRecordField.Empty);
        _fieldTexts[fieldIndex] = newField;
        SetCachedFieldValue(fieldIndex, value, spelling);
        _fieldPayloadByteLength = newPayloadByteLength;
        _numericFieldCount += (value.IsNumberValue ? 1 : 0) - (oldIsNumeric ? 1 : 0);
        if (newFieldCount > oldFieldCount)
            _fieldCountValue = AwkValue.FromNumber(newFieldCount);
        MarkRecordForRecomposition(outputFieldSeparator);
        counters.RecordFieldsMaterialized(addedFields);

        long EstimateRetainedBytesAfterFieldAssignment(
            int fieldIndex,
            AwkRecordField oldField,
            int newCapacity)
        {
            var bytes = AdjustEstimate(
                _retainedMemoryBytes,
                AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity),
                AwkRuntimeMemory.EstimateRecordFieldListBytes(newCapacity));
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
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity),
            AwkRuntimeMemory.EstimateRecordFieldListBytes(newCapacity));
        newRetainedBytes = AdjustEstimate(newRetainedBytes, _fieldCountValue.RetainedTextMemoryBytes, 0);
        if (addedFields >= MutationCheckpointFields || newCapacity != _fieldTexts.Capacity)
        {
            var stagedBytes = AwkRuntimeMemory.EstimateRecordFieldListBytes(newCapacity);
            using var staging = governor.ReserveTransient(stagedBytes);
            var fields = StageFieldTexts(index.Value, newCapacity);
            PublishStagedFields(fields, _fieldValueCache, newRetainedBytes, staging, stagedBytes);
        }
        else
        {
            checkpoint?.Invoke();
            ReplaceRetainedMemory(newRetainedBytes);
            while (_fieldTexts.Count < index.Value)
                _fieldTexts.Add(AwkRecordField.Empty);
        }

        _fieldCountValue = AwkValue.FromNumber(index.Value);
        counters.RecordFieldsMaterialized(addedFields);
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
        var stageFields = Math.Abs(newFieldCount - oldFieldCount) >= MutationCheckpointFields ||
            newCapacity != _fieldTexts.Capacity;
        var newCacheCount = _fieldValueCache.Count;
        var cacheCapacity = _fieldValueCache.Capacity;
        var newPayloadByteLength = _fieldPayloadByteLength;
        var newNumericFieldCount = _numericFieldCount;
        var newRetainedBytes = AdjustEstimate(
            _retainedMemoryBytes,
            AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity),
            AwkRuntimeMemory.EstimateRecordFieldListBytes(newCapacity));
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
                if (field.CacheSlot <= 0) continue;
                newCacheCount--;
                newRetainedBytes = AdjustEstimate(newRetainedBytes,
                    _fieldValueCache[field.CacheSlot - 1].PayloadBytes, addedBytes: 0);
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
            var stagedBytes = AwkRuntimeMemory.EstimateRecordFieldListBytes(newCapacity);
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
                _fieldTexts.Truncate(newFieldCount);
            }
            else if (newFieldCount > oldFieldCount)
            {
                while (_fieldTexts.Count < newFieldCount)
                    _fieldTexts.Add(AwkRecordField.Empty);
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

    private AwkRecordFieldBuffer StageFieldTexts(int count, int capacity)
    {
        checkpoint?.Invoke();
        var fields = new AwkRecordFieldBuffer(capacity);
        for (var index = 0; index < count; index++)
        {
            if ((index & (MutationCheckpointFields - 1)) == 0)
                checkpoint?.Invoke();
            fields.Add(index < _fieldTexts.Count ? _fieldTexts[index] : AwkRecordField.Empty);
        }
        checkpoint?.Invoke();
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

    private void GrowFieldValueCache(int capacity, long retainedBytes)
    {
        var bytes = EstimateFieldCacheBufferBytes(capacity);
        using var staging = governor.ReserveTransient(bytes);
        var cache = StageFieldValueCache(_fieldTexts, capacity);
        PublishStagedFields(_fieldTexts, cache, retainedBytes, staging, bytes);
    }

    // Cache slots belong to this frame's descriptors; scalar snapshots never carry them.
    // All capacity and ownership changes are admitted before these non-fallible mutations.
    private AwkFieldText FieldText(AwkRecordField field) => field.Text(CollectionsMarshal.AsSpan(_fieldValueCache));
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
        AwkRecordFieldBuffer fields,
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

    private AwkFieldText CompositionField(int index, ReadOnlySpan<AwkFieldText> formatted) =>
        formatted.IsEmpty ? FieldText(_fieldTexts[index]) : formatted[index];

    private void EnsureRecordText()
    {
        if (_recordTextIsCurrent) return;
        var bufferBytes = AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Count);
        if (_numericFieldCount > 0)
            bufferBytes = AwkRuntimeMemory.CheckedAdd(bufferBytes,
                AwkRuntimeMemory.EstimateFieldTextListBytes(_fieldTexts.Count));
        using var buffers = governor.ReserveTransient(bufferBytes);
        long formattedBytes = 0;
        try
        {
            ReadOnlySpan<AwkFieldText> formatted = default;
            var payloadByteLength = _fieldPayloadByteLength;
            if (_numericFieldCount > 0)
            {
                var values = new List<AwkFieldText>(_fieldTexts.Count);
                for (var index = 0; index < _fieldTexts.Count; index++)
                {
                    if ((index & 1023) == 0) checkpoint?.Invoke();
                    var field = FieldText(_fieldTexts[index]);
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
                        field = AwkFieldText.FromOwned(text);
                    }
                    values.Add(field);
                }
                formatted = CollectionsMarshal.AsSpan(values);
            }
            var byteLength = EstimateComposedRecordByteLength(_recompositionSeparator.ByteLength,
                _fieldTexts.Count, payloadByteLength);
            var fields = new AwkRecordFieldBuffer(_fieldTexts.Count);
            var offset = 0;
            for (var index = 0; index < _fieldTexts.Count; index++)
            {
                if ((index & 1023) == 0) checkpoint?.Invoke();
                if (index > 0) offset = AwkRuntimeMemory.CheckedAdd(offset, _recompositionSeparator.ByteLength);
                var length = CompositionField(index, formatted).ByteLength();
                fields.Add(AwkRecordField.FromRecordSlice(offset, length).WithCacheFrom(_fieldTexts[index]));
                offset = AwkRuntimeMemory.CheckedAdd(offset, length);
            }
            var retained = EstimateRetainedBytes(AwkRuntimeMemory.EstimateTextBytes(byteLength),
                fields, _fieldValueCache, _fieldCountValue);
            using var peak = PreflightRecomposition(retained, byteLength);
            var recordText = AwkText.Empty;
            if (byteLength > 0)
            {
                var bytes = AwkText.AllocateOwnedBytes(byteLength, governor);
                var separator = _recompositionSeparator.AsSpan(checkpoint);
                offset = 0;
                for (var index = 0; index < _fieldTexts.Count; index++)
                {
                    if ((index & 1023) == 0) checkpoint?.Invoke();
                    if (index > 0)
                    {
                        AwkText.CopyBytes(separator, bytes.AsSpan(offset), checkpoint);
                        offset += separator.Length;
                    }
                    var field = CompositionField(index, formatted).AsSpan(RecordBytes(), checkpoint);
                    AwkText.CopyBytes(field, bytes.AsSpan(offset), checkpoint);
                    offset += field.Length;
                }
                recordText = AwkText.FromTrustedOwnedBytes(bytes, counters);
            }
            checkpoint?.Invoke();
            ReplaceRetainedMemory(retained);
            ClearBufferedRecord();
            _recordText = recordText;
            _fieldTexts = fields;
            _recordTextIsCurrent = true;
            _recordWasRecomposed = true;
            _recompositionSeparator = AwkText.Empty;
        }
        finally { governor.ReleaseCommitted(formattedBytes); }
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
        counters.RecordFieldPrefixExtension();
        var previousCount = _fieldTexts.Count;
        if (previousCount == 0)
        {
            counters.RecordFieldSplit();
            checkpoint?.Invoke();
        }
        var grows = _fieldTexts.Capacity < targetCount;
        var (fields, staging) = grows ? StageFieldPrefix(targetCount) : (_fieldTexts, AwkMemoryLease.Disabled);
        try
        {
            var position = fieldSplitter.ExtendRecordFields(RecordBytes(), _splitPlan, _splitPosition,
                fields.UnpublishedTail(targetCount), out var addedCount, out var addedPayloadBytes);
            var complete = position < 0;
            if (requireComplete && !complete) return false;
            var payloadBytes = AwkRuntimeMemory.CheckedAdd(_fieldPayloadByteLength, addedPayloadBytes);
            // Incremental slices leave cached/owned payload unchanged. Only capacity
            // replacement or split-plan retirement changes the retained estimate.
            var retained = governor.IsEnabled && grows ? AdjustEstimate(_retainedMemoryBytes,
                AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity),
                AwkRuntimeMemory.EstimateRecordFieldListBytes(fields.Capacity)) : _retainedMemoryBytes;
            if (complete && governor.IsEnabled)
                retained = AdjustEstimate(retained, _splitPlan.RetainedMemoryBytes, 0);
            if (grows)
            {
                // This destination is still private during the publication preflight.
                fields.PublishCount(previousCount + addedCount);
                PublishStagedFields(fields, _fieldValueCache, retained, staging, staging.Bytes);
            }
            else
            {
                checkpoint?.Invoke();
                ReplaceRetainedMemory(retained);
                fields.PublishCount(previousCount + addedCount);
            }
            _splitPosition = position;
            _fieldPayloadByteLength = payloadBytes;
            counters.RecordFieldsMaterialized(addedCount);
            if (!grows && previousCount == 0) counters.RecordFieldListBufferReuse();
            if (complete)
            {
                _fieldsAreSplit = true;
                _fieldCountValue = AwkValue.FromNumber(fields.Count);
                _splitPlan = default;
            }
            return complete;
        }
        finally { staging.Dispose(); }
    }

    private (AwkRecordFieldBuffer Fields, AwkMemoryLease Staging) StageFieldPrefix(int targetCount)
    {
        var capacity = (int)Math.Max(targetCount, Math.Min(Array.MaxLength, (long)_fieldTexts.Capacity * 2));
        var staging = governor.ReserveTransient(governor.IsEnabled
            ? AwkRuntimeMemory.EstimateRecordFieldListBytes(capacity) : 0);
        var copied = 0;
        try
        {
            checkpoint?.Invoke();
            var fields = new AwkRecordFieldBuffer(capacity);
            counters.RecordFieldPrefixStage();
            for (var index = 0; index < _fieldTexts.Count; index++)
            {
                if ((index & 1023) == 0) checkpoint?.Invoke();
                fields.Add(_fieldTexts[index]);
                copied++;
            }
            // Frame cleanup keeps this lease through publication or interrupted scans.
            return (fields, staging);
        }
        catch { staging.Dispose(); throw; }
        finally { counters.RecordFieldDescriptorCopies(copied); }
    }

    private void EnsureFields()
    {
        if (_fieldsAreSplit)
            return;

        if (_fieldTexts.Count > 0 && _splitPlan.SupportsIncrementalFields &&
            TryExtendFields(_fieldTexts.Capacity, requireComplete: true))
            return;

        if (_fieldTexts.Count == 0)
            counters.RecordFieldSplit();
        var reusable = _fieldTexts.Count == 0 && _fieldTexts.Capacity > 0 &&
            ReuseFieldBuffer() ? _fieldTexts : null;
        var reusableCapacity = reusable?.Capacity ?? 0;
        var previousCount = _fieldTexts.Count;
        var initialValues = _fieldValueCache;
        var previousCacheCount = initialValues.Count;
        // Keep a private empty view until staging succeeds; later field mutation owns its cells.
        if (reusable is not null) _fieldTexts = EmptyFieldTexts;
        var fieldTexts = reusable ?? new();
        var writer = new AwkRecordFieldWriter(fieldTexts, _fieldValueCache, governor, counters,
            checkpoint ?? (static () => { }), AwkMemoryLease.Disabled);
        var published = false;
        try
        {
            if (_splitPlan.RequiresRegexSubject)
                fieldSplitter.FillRecordFields(OwnedRecordText(), _splitPlan, ref writer);
            else
                fieldSplitter.FillRecordFields(RecordBytes(), _splitPlan, ref writer);
            // Complete splitting without discarding numbers already read from a prefix.
            for (var index = 0; index < previousCount; index++)
            {
                if ((index & 1023) == 0) checkpoint?.Invoke();
                if (_fieldTexts[index].CacheSlot < 0)
                    fieldTexts[index] = fieldTexts[index].WithCacheFrom(_fieldTexts[index]);
                if (_fieldTexts[index].RuneLength >= 0)
                    fieldTexts[index] = fieldTexts[index].WithRuneLength(_fieldTexts[index].RuneLength);
            }
            // Writer-owned escaped text already has its final cell mapping.
            for (var index = 0; index < previousCacheCount; index++)
            {
                if ((index & 1023) == 0) checkpoint?.Invoke();
                var field = _fieldValueCache[index].FieldIndex;
                fieldTexts[field] = fieldTexts[field].WithCacheSlot(index + 1);
            }
            var retainedBytes = governor.IsEnabled ? EstimateRetainedBytes(
                AwkRuntimeMemory.EstimateTextBytes(_recordText), fieldTexts, writer.Values, default) : 0;
            PublishStagedFields(fieldTexts, writer.Values, retainedBytes, writer.Staging, writer.Staging.Bytes);
            published = true;
            _fieldCountValue = AwkValue.FromNumber(fieldTexts.Count);
            _fieldPayloadByteLength = writer.PayloadBytes;
            counters.RecordFieldsMaterialized(_fieldTexts.Count - previousCount);
            _fieldsAreSplit = true;
            _splitPlan = default;
            if (ReferenceEquals(fieldTexts, reusable)) counters.RecordFieldListBufferReuse();
        }
        catch
        {
            if (reusable is not null)
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
    }

    private void ClearSplitFields(bool releaseCache = false)
    {
        _fieldCountValue = AwkValue.FromNumber(0);
        // Producers only write unpublished frame-owned destinations.
        if (!releaseCache && _fieldTexts.Capacity > 0 && ReuseFieldBuffer())
            _fieldTexts.Clear();
        else if (_fieldTexts.Capacity != 0)
            _fieldTexts = new();
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
                ReuseFieldBuffer()
                    ? AwkRuntimeMemory.EstimateRecordFieldListBytes(_fieldTexts.Capacity) : 0,
                ReuseCacheBuffer() ? EstimateFieldCacheBufferBytes(_fieldValueCache.Capacity) : 0));

    private long EstimateRetainedBytes(
        AwkText recordText,
        AwkRecordFieldBuffer fieldTexts,
        List<CachedFieldValue> fieldValueCache,
        AwkValue fieldCountValue = default)
        => governor.IsEnabled
            ? EstimateRetainedBytes(AwkRuntimeMemory.EstimateTextBytes(recordText), fieldTexts, fieldValueCache, fieldCountValue)
            : 0;

    private long EstimateRetainedBytes(long recordTextBytes, AwkRecordFieldBuffer fieldTexts,
        List<CachedFieldValue> fieldValueCache, AwkValue fieldCountValue)
    {
        if (!governor.IsEnabled) return 0;
        var bytes = AwkRuntimeMemory.CheckedAdd(recordTextBytes, fieldCountValue.RetainedTextMemoryBytes);
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateRecordFieldListBytes(fieldTexts.Capacity));
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, EstimateFieldCacheBufferBytes(fieldValueCache.Capacity));
        for (var index = 0; index < fieldValueCache.Count; index++)
        {
            if ((index & 1023) == 0) checkpoint?.Invoke();
            if (fieldValueCache[index].FieldIndex < fieldTexts.Count)
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, fieldValueCache[index].PayloadBytes);
        }
        return bytes;
    }

    private long EstimateFieldCacheBufferBytes(int capacity) =>
        governor.IsEnabled ? AwkRuntimeMemory.EstimateFieldValueListBytes(capacity) : 0;

    private bool ReuseFieldBuffer() =>
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
