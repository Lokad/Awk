namespace Lokad.Awk;

// Persistent record-local coordinates and numeric state contain no managed references.
// Owned spelling and scalar values live in the frame cache, not in every raw field.
internal struct AwkRecordField
{
    private double _number;
    private readonly int _start;
    private readonly int _length;
    private int _runeLength;
    private int _cacheSlot;

    private AwkRecordField(int start, int length, int cacheSlot = 0,
        double number = 0, int runeLength = -1)
    {
        _start = start;
        _length = length;
        _cacheSlot = cacheSlot;
        _number = number;
        _runeLength = runeLength;
    }

    internal static AwkRecordField Empty => default;
    // Raw slices have record coordinates; owned text needs no record start.
    // Derive the kind instead of storing a second discriminator in every cell.
    internal readonly AwkFieldTextKind Kind => _length == 0 ? AwkFieldTextKind.Empty :
        _start < 0 ? AwkFieldTextKind.Owned : AwkFieldTextKind.RecordSlice;
    internal readonly int CacheSlot => _cacheSlot;
    internal readonly int RecordStart => _start;
    internal readonly int ByteLength() => _length;
    internal readonly int RuneLength => _runeLength;
    internal readonly bool IsWholeRecord(int length) => _length > 0 && _start == 0 && _length == length;

    // Numeric/borrowed consumers have already excluded owned spelling. Read the
    // admitted record directly instead of constructing a detached owning-text view.
    internal readonly ReadOnlySpan<byte> BytesInRecord(ReadOnlySpan<byte> record) =>
        _start >= 0 ? record.Slice(_start, _length) : ReadOnlySpan<byte>.Empty;


    internal static AwkRecordField FromRecordSlice(int start, int length) =>
        length == 0 ? Empty : new(start, length);

    internal static AwkRecordField FromOwned(int cacheSlot, int length) =>
        new(-1, length, cacheSlot);

    internal readonly AwkRecordField WithCacheSlot(int slot) => new(_start, _length, slot, _number, _runeLength);
    // The owning buffer publishes cache words only after parsing/counting completes.
    // Captured cells remain independent values; no reference crosses those operations.
    internal void CacheNumber(double number)
    {
        _number = number;
        _cacheSlot = -1;
    }
    internal void CacheRuneLength(int length) => _runeLength = length;
    // Negative slots describe facts in these existing words; positive slots own
    // spelling/scalars in the frame cache. Arithmetic prefixes are not classifications.
    internal readonly bool HasInputClassification => CacheSlot < -1;
    private readonly AwkValueKind InputClassificationKind => (AwkValueKind)(-CacheSlot - 2);
    internal readonly AwkValue InputClassification => AwkValue.FromStorage(null, _number, InputClassificationKind);
    internal readonly AwkRecordField WithInputClassification(AwkValue value) =>
        new(_start, _length, -(int)value.StoredKind - 2, value.StoredNumber, _runeLength);
    internal readonly bool TryGetComparisonNumber(out double number)
    {
        number = _number;
        return CacheSlot == -(int)AwkValueKind.NumericString - 2;
    }
    internal readonly AwkRecordField WithCacheFrom(AwkRecordField source) =>
        new(_start, _length, source.CacheSlot, source._number, source._runeLength);

    internal readonly bool TryGetNumericValue(out double number)
    {
        number = _number;
        // The parser stores text, numeric text, numeric strings or a deferred
        // input error. Every established fact except that error has a number.
        return CacheSlot < 0 && CacheSlot != -(int)AwkValueKind.NonDecimalConversionErrorInput - 2;
    }

    internal readonly AwkFieldText Text(ReadOnlySpan<AwkFieldFrame.CachedFieldValue> cache) => Kind switch
    {
        AwkFieldTextKind.RecordSlice => AwkFieldText.FromRecordSlice(_start, _length),
        AwkFieldTextKind.Owned => AwkFieldText.FromOwned(cache[CacheSlot - 1].Spelling),
        _ => AwkFieldText.Empty
    };
}

internal enum AwkFieldCacheKind : byte { TextOnly, ClassifiedText, Scalar }

// A synchronous view into one record owner. It cannot outlive that owner or cross an await.
internal readonly ref struct AwkRecordFields
{
    private readonly ReadOnlySpan<AwkRecordField> _fields;
    private readonly ReadOnlySpan<AwkFieldText> _texts;
    private readonly ReadOnlySpan<AwkFieldFrame.CachedFieldValue> _cache;
    private readonly ReadOnlySpan<byte> _finalText;
    private readonly bool _hasFinalText;
    internal AwkRecordFields(ReadOnlySpan<AwkRecordField> fields, ReadOnlySpan<AwkFieldFrame.CachedFieldValue> cache)
    {
        _fields = fields;
        _cache = cache;
        _texts = default;
    }
    internal AwkRecordFields(ReadOnlySpan<AwkFieldText> texts)
    {
        _texts = texts;
        _fields = default;
        _cache = default;
    }
    internal AwkRecordFields(ReadOnlySpan<AwkFieldText> texts, ReadOnlySpan<byte> finalText)
    {
        _texts = texts;
        _finalText = finalText;
        _hasFinalText = true;
    }
    internal int Length => _fields.IsEmpty ? _texts.Length : _fields.Length;

    internal ReadOnlySpan<byte> BytesAt(int index, ReadOnlySpan<byte> record, Action? checkpoint)
    {
        if (_fields.IsEmpty)
            return _hasFinalText && index == _texts.Length - 1 ? _finalText :
                _texts[index].AsSpan(record, checkpoint);
        ref readonly var field = ref _fields[index];
        return field.Kind == AwkFieldTextKind.Owned
            ? _cache[field.CacheSlot - 1].Spelling.AsSpan(checkpoint)
            : field.BytesInRecord(record);
    }
}
