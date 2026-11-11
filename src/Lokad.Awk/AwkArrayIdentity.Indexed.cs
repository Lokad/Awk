namespace Lokad.Awk;

internal sealed partial class AwkArrayIdentity
{
    internal const int IndexedPageShift = 11;
    internal const int IndexedPageSize = 1 << IndexedPageShift;
    internal const int IndexedPageMask = IndexedPageSize - 1;

    internal static int IndexedPageCount(int capacity) => capacity <= 0 ? 0 : 1 + ((capacity - 1) >> IndexedPageShift);
    private static int LastIndexedPageLength(int capacity) => ((capacity - 1) & IndexedPageMask) + 1;

    // Bulk producers and clones allocate only the owner's actual slot/table capacity.
    // Unused table positions stay private; every page below value capacity is present.
    internal static AwkValue?[][] AllocateIndexedSlots(int capacity, int tableCapacity, Action? checkpoint)
    {
        if (capacity == 0) return Array.Empty<AwkValue?[]>();
        var pages = new AwkValue?[tableCapacity][];
        var count = IndexedPageCount(capacity);
        for (var index = 0; index < count; index++)
        {
            checkpoint?.Invoke();
            pages[index] = new AwkValue?[index == count - 1 ? LastIndexedPageLength(capacity) : IndexedPageSize];
        }
        return pages;
    }

    private bool PlanIndexedByteInsertion(ReadOnlySpan<byte> bytes, out int ordinal, out int capacity)
    {
        capacity = 0;
        if (!TryParseOrdinal(bytes, out ordinal)) return false;
        if (_indexed is null)
        {
            if (ordinal != 1) return false;
            capacity = AwkRuntimeMemory.PlanArrayCapacity(0);
            return true;
        }
        if (ordinal <= _indexedCapacity)
        {
            capacity = _indexedCapacity;
            return true;
        }
        if (ordinal != _indexedCapacity + 1 || _indexedCapacity >= Array.MaxLength) return false;
        if (_indexedCapacity == 0)
            capacity = AwkRuntimeMemory.PlanArrayCapacity(0);
        else
        {
            var last = LastIndexedPageLength(_indexedCapacity);
            var growth = last == IndexedPageSize ? IndexedPageSize : Math.Min(IndexedPageSize, Math.Max(3, last * 2)) - last;
            capacity = (int)Math.Min(Array.MaxLength, (long)_indexedCapacity + growth);
        }
        return true;
    }

    private int IndexedTableCapacity(int capacity)
    {
        var required = IndexedPageCount(capacity);
        var current = _indexed?.Length ?? 0;
        return current >= required ? current : Math.Max(required, AwkRuntimeMemory.PlanArrayCapacity(current));
    }

    internal long EstimateIndexedBackingBytes(int capacity) =>
        AwkRuntimeMemory.EstimateIndexedArrayBackingBytes(capacity, IndexedTableCapacity(capacity));

    // Admission funds newly allocated storage while replaced pages/table remain live.
    // Earlier full pages are retained by this same owner and need no second admission.
    internal long EstimateIndexedInsertionBackingBytes(int capacity)
    {
        if (_indexed is not null && capacity == _indexedCapacity) return 0;
        var table = IndexedTableCapacity(capacity);
        return AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateIndexedValuePageBytes(LastIndexedPageLength(capacity)),
            table == (_indexed?.Length ?? 0) ? 0 : AwkRuntimeMemory.EstimateIndexedTableBytes(table));
    }

    internal readonly struct IndexedInsertion(AwkValue?[][] pages, AwkValue?[]? replacement, int capacity)
    {
        private readonly AwkValue?[][]? _pages = pages;
        public AwkValue?[][] Pages => _pages ?? throw new InvalidOperationException("Indexed insertion has not been prepared.");
        public AwkValue?[]? Replacement { get; } = replacement;
        public int Capacity { get; } = capacity;
    }

    // Staging does not alter the live table. A callback/cancellation can discard it
    // before publication without retaining an unfunded page in inactive capacity.
    internal IndexedInsertion PrepareIndexedCapacity(int capacity, Action? checkpoint)
    {
        if (_indexed is { } ready && _indexedCapacity == capacity) return new(ready, null, capacity);
        var tableCapacity = IndexedTableCapacity(capacity);
        var source = _indexed;
        var pages = source is not null && source.Length == tableCapacity
            ? source : new AwkValue?[tableCapacity][];
        if (source is not null && !ReferenceEquals(source, pages))
            CopyIndexedStorage(source, pages, sizeof(long), checkpoint);
        var page = new AwkValue?[LastIndexedPageLength(capacity)];
        if (source is not null && IndexedPageCount(capacity) == IndexedPageCount(_indexedCapacity))
            CopyIndexedStorage(source[IndexedPageCount(_indexedCapacity) - 1], page, 32, checkpoint);
        return new(pages, page, capacity);
    }

    private static void CopyIndexedStorage<T>(T[] source, T[] destination, int elementBytes, Action? checkpoint)
    {
        var chunk = AwkRuntimeLimits.CancellationCheckBytes / elementBytes;
        for (var offset = 0; offset < source.Length;)
        {
            checkpoint?.Invoke();
            var length = Math.Min(chunk, source.Length - offset);
            Array.Copy(source, offset, destination, offset, length);
            offset += length;
        }
    }

    internal void PublishIndexedInsertion(in IndexedInsertion storage, int ordinal, AwkValue value, Action? checkpoint)
    {
        var pages = storage.Pages;
        var index = ordinal - 1;
        var pageIndex = index >> IndexedPageShift;
        var page = storage.Replacement ?? pages[pageIndex];
        if (page[index & IndexedPageMask] is not null) throw new InvalidOperationException("Duplicate indexed insertion.");
        if (storage.Replacement is not null) pages[pageIndex] = page;
        page[index & IndexedPageMask] = value;
        _indexed = pages;
        _indexedCapacity = storage.Capacity;
        _indexedCount++;
        _checkpoint = checkpoint;
    }
}
