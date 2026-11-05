namespace Lokad.Awk;

// Persistent record-local coordinates and numeric state contain no managed references.
// Owned spelling and scalar values live in the frame cache, not in every raw field.
internal readonly struct AwkRecordField
{
    private readonly double _number;
    private readonly int _start;
    private readonly int _length;

    private AwkRecordField(AwkFieldTextKind kind, int start, int length, int cacheSlot = 0, double number = 0)
    {
        Kind = kind;
        _start = start;
        _length = length;
        CacheSlot = cacheSlot;
        _number = number;
    }

    internal static AwkRecordField Empty => default;
    internal AwkFieldTextKind Kind { get; }
    internal int CacheSlot { get; }
    internal int ByteLength() => _length;
    internal bool IsWholeRecord(int length) => Kind == AwkFieldTextKind.RecordSlice && _start == 0 && _length == length;

    internal static AwkRecordField FromRecordSlice(int start, int length) =>
        length == 0 ? Empty : new(AwkFieldTextKind.RecordSlice, start, length);

    internal static AwkRecordField FromOwned(int cacheSlot, int length) =>
        new(length == 0 ? AwkFieldTextKind.Empty : AwkFieldTextKind.Owned, 0, length, cacheSlot);

    internal AwkRecordField WithCacheSlot(int slot) => new(Kind, _start, _length, slot, _number);
    internal AwkRecordField WithNumericValue(double number) => new(Kind, _start, _length, -1, number);
    internal AwkRecordField WithCacheFrom(AwkRecordField source) =>
        new(Kind, _start, _length, source.CacheSlot, source._number);

    internal bool TryGetNumericValue(out double number)
    {
        number = _number;
        return CacheSlot == -1;
    }

    internal AwkFieldText Text(ReadOnlySpan<AwkFieldFrame.CachedFieldValue> cache) => Kind switch
    {
        AwkFieldTextKind.RecordSlice => AwkFieldText.FromRecordSlice(_start, _length),
        AwkFieldTextKind.Owned => AwkFieldText.FromOwned(cache[CacheSlot - 1].Spelling),
        _ => AwkFieldText.Empty
    };
}

internal enum AwkFieldCacheKind : byte { TextOnly, Classification, Scalar }

// A synchronous view into one record owner. It cannot outlive that owner or cross an await.
internal readonly ref struct AwkRecordFields
{
    private readonly ReadOnlySpan<AwkRecordField> _fields;
    private readonly ReadOnlySpan<AwkFieldText> _texts;
    private readonly ReadOnlySpan<AwkFieldFrame.CachedFieldValue> _cache;
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
    internal int Length => _fields.IsEmpty ? _texts.Length : _fields.Length;
    internal AwkFieldText this[int index] => _fields.IsEmpty ? _texts[index] : _fields[index].Text(_cache);
}
