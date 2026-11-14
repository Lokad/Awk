namespace Lokad.Awk;

// The frame funds this array and its overlap during replacement. Raw cells contain
// no managed references; a failed scan can leave its unpublished tail untouched.
internal sealed class AwkRecordFieldBuffer(int capacity = 0)
{
    private AwkRecordField[] _cells = capacity == 0 ? [] : new AwkRecordField[capacity];

    internal int Count { get; private set; }
    internal int Capacity => _cells.Length;
    internal void CacheNumber(int index, double number) => _cells[index].CacheNumber(number);
    internal void CacheRuneLength(int index, int length) => _cells[index].CacheRuneLength(length);
    internal AwkRecordField this[int index]
    {
        get => _cells[index];
        set => _cells[index] = value;
    }

    internal ReadOnlySpan<AwkRecordField> AsSpan() => _cells.AsSpan(0, Count);
    internal Span<AwkRecordField> UnpublishedTail(int targetCount) =>
        _cells.AsSpan(Count, targetCount - Count);

    internal void PublishCount(int count) => Count = count;
    internal void Clear() => Count = 0;
    internal void Truncate(int count) => Count = count;

    // General producers grow only after their existing staging admission. The
    // incremental scanner consumes an admitted span and publishes its count once.
    internal void Add(AwkRecordField field)
    {
        _cells[Count] = field;
        Count++;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= _cells.Length) return;
        var cells = new AwkRecordField[capacity];
        _cells.AsSpan(0, Count).CopyTo(cells);
        _cells = cells;
    }
}
