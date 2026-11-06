namespace Lokad.Awk;

// The record frame admits this destination before allocation or replacement.
// Cells contain no managed references; only the initialized prefix is published.
internal sealed class AwkRecordCellBuffer
{
    private AwkRecordField[] _cells;

    public AwkRecordCellBuffer(int capacity = 0)
    {
        _cells = capacity == 0 ? [] : new AwkRecordField[capacity];
        RetainedBytes = AwkRuntimeMemory.EstimateRecordCellBufferBytes(capacity);
    }

    public int Count { get; private set; }
    public int Capacity => _cells.Length;
    public long RetainedBytes { get; private set; }
    public Span<AwkRecordField> Span => _cells.AsSpan(0, Count);

    public ref AwkRecordField this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref _cells[index];
        }
    }

    public void Add(AwkRecordField field)
    {
        // Growth belongs to the admitted producer or mutation, never to append.
        _cells[Count] = field;
        Count++;
    }

    public void Clear() => Count = 0;

    public void Truncate(int count)
    {
        if ((uint)count > (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(count));
        Count = count;
    }

    public void EnsureCapacity(int capacity)
    {
        if (capacity <= Capacity) return;
        var cells = new AwkRecordField[capacity];
        Span.CopyTo(cells);
        _cells = cells;
        RetainedBytes = AwkRuntimeMemory.EstimateRecordCellBufferBytes(capacity);
    }
}
