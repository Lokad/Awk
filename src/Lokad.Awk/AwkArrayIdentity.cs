using System.Buffers.Text;
using System.Collections;

namespace Lokad.Awk;

// Bind this owner, not its replaceable dictionary. RHS evaluation may clear/split it.
internal sealed class AwkArrayIdentity : IReadOnlyDictionary<AwkArrayKey, AwkValue>
{
    private static readonly Dictionary<AwkArrayKey, AwkArrayCell> DetachedSlots = new(AwkArrayKeyComparer.Instance);
    private Dictionary<AwkArrayKey, AwkArrayCell>.AlternateLookup<ReadOnlySpan<byte>> _lookup;
    private IEqualityComparer<AwkArrayKey> _comparer;
    private AwkValue?[]? _indexed;
    private int _indexedCount;
    private Action? _checkpoint;

    internal AwkArrayIdentity(int capacity, IEqualityComparer<AwkArrayKey> comparer, AwkArrayIdentity? parent = null)
    {
        _comparer = comparer;
        _lookup = (capacity == 0 ? DetachedSlots : new Dictionary<AwkArrayKey, AwkArrayCell>(capacity, comparer))
            .GetAlternateLookup<ReadOnlySpan<byte>>();
        Parent = parent;
    }

    private AwkArrayIdentity(AwkValue?[] indexed, IEqualityComparer<AwkArrayKey> comparer,
        int count, Action? checkpoint, AwkArrayIdentity? parent = null)
    {
        _lookup = DetachedSlots.GetAlternateLookup<ReadOnlySpan<byte>>();
        _comparer = comparer;
        _indexed = indexed;
        _indexedCount = count;
        _checkpoint = checkpoint;
        Parent = parent;
        if (count > 0) AdoptChildren();
    }

    internal static AwkArrayIdentity FromIndexed(AwkValue?[] cells, IEqualityComparer<AwkArrayKey> comparer,
        Action? checkpoint) => new(cells, comparer, cells.Length, checkpoint);

    internal AwkArrayIdentity CreateEmptyCopy(AwkArrayIdentity? parent = null) => _indexed is { } cells
        ? new(new AwkValue?[cells.Length], _comparer, 0, _checkpoint, parent)
        : new(Count, _comparer, parent);

    // The owner retains one prepared BCL handle. This synchronous view follows its current backing.
    public ByteView ByteLookup => new(this);

    public int Count => _indexed is null ? _lookup.Dictionary.Count : _indexedCount;
    internal bool IsIndexed => _indexed is not null;
    internal long BackingBytes => _indexed is { } cells
        ? AwkRuntimeMemory.EstimateIndexedArrayBackingBytes(cells.Length)
        : AwkRuntimeMemory.EstimateArrayBackingBytes(_lookup.Dictionary.EnsureCapacity(0));

    public IEnumerable<AwkArrayKey> Keys
    {
        get { foreach (var pair in this) yield return pair.Key; }
    }

    public IEnumerable<AwkValue> Values
    {
        get { foreach (var pair in this) yield return pair.Value; }
    }

    public AwkValue this[AwkArrayKey key]
    {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
        set
        {
            if (_indexed is not null) throw new InvalidOperationException("Indexed insertion requires admitted storage preparation.");
            WritableDictionary()[key] = new(value);
        }
    }

    public bool ContainsKey(AwkArrayKey key) => _indexed is null
        ? _lookup.Dictionary.ContainsKey(key) : TryGetIndexed(key, out _, out _);

    public bool TryGetValue(AwkArrayKey key, out AwkValue value)
    {
        if (_indexed is not null) return TryGetIndexed(key, out _, out value);
        if (_lookup.Dictionary.TryGetValue(key, out var cell))
        {
            value = cell.Slot;
            return true;
        }
        value = default;
        return false;
    }

    private bool TryGetIndexed(AwkArrayKey key, out int ordinal, out AwkValue value)
    {
        Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        if (key.ByteLength <= 10) return TryGetIndexed(key.AsSpan(buffer), out ordinal, out value);
        ordinal = 0;
        value = default;
        return false;
    }

    // Only mutation/capture consumers ask for dictionary cell identity. Ordinal
    // storage keeps values directly and is located again after RHS evaluation.
    internal bool TryGetEntry(AwkArrayKey key, out AwkArrayKey retainedKey,
        out AwkValue value, out AwkArrayCell? cell)
    {
        if (_indexed is null)
        {
            if (_lookup.Dictionary.GetAlternateLookup<AwkArrayKey>().TryGetValue(key, out retainedKey, out cell))
            {
                value = cell.Slot;
                return true;
            }
            value = default;
            return false;
        }
        cell = null;
        if (TryGetIndexed(key, out var ordinal, out value))
        {
            retainedKey = AwkArrayKey.FromOrdinal(ordinal);
            return true;
        }
        retainedKey = default;
        return false;
    }

    private bool TryGetIndexed(ReadOnlySpan<byte> bytes, out int ordinal, out AwkValue value)
    {
        value = default;
        if (!TryParseOrdinal(bytes, out ordinal) || _indexed is not { } indexed || ordinal > indexed.Length ||
            indexed[ordinal - 1] is not { } stored) return false;
        value = stored;
        return true;
    }

    internal void SetIndexedValue(AwkArrayKey key, AwkValue value)
    {
        if (_indexed is not { } indexed || !TryGetIndexed(key, out var ordinal, out _))
            throw new InvalidOperationException("An indexed update requires an existing ordinal.");
        indexed[ordinal - 1] = value;
    }

    private static bool TryParseOrdinal(ReadOnlySpan<byte> bytes, out int ordinal)
    {
        ordinal = 0;
        return bytes.Length is > 0 and <= 10 && bytes[0] >= (byte)'1' && bytes[0] <= (byte)'9' &&
            Utf8Parser.TryParse(bytes, out ordinal, out var consumed) && consumed == bytes.Length && ordinal > 0;
    }

    // Preparation is bounded and owns no storage. Only a virgin owner can start
    // indexed storage; sparse or noncanonical insertions keep dictionary semantics.
    internal bool TryPlanIndexedInsertion(AwkArrayKey key, out int ordinal, out int capacity)
    {
        ordinal = capacity = 0;
        return (_indexed is not null || ReferenceEquals(_lookup.Dictionary, DetachedSlots)) &&
            PlanIndexedKeyInsertion(key, out ordinal, out capacity);
    }

    private bool PlanIndexedKeyInsertion(AwkArrayKey key, out int ordinal, out int capacity)
    {
        ordinal = capacity = 0;
        Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        return key.ByteLength <= 10 && PlanIndexedByteInsertion(key.AsSpan(buffer), out ordinal, out capacity);
    }

    internal bool TryPlanIndexedInsertion(ReadOnlySpan<byte> bytes, out int ordinal, out int capacity)
    {
        ordinal = capacity = 0;
        return (_indexed is not null || ReferenceEquals(_lookup.Dictionary, DetachedSlots)) &&
            PlanIndexedByteInsertion(bytes, out ordinal, out capacity);
    }

    private bool PlanIndexedByteInsertion(ReadOnlySpan<byte> bytes, out int ordinal, out int capacity)
    {
        capacity = 0;
        if (!TryParseOrdinal(bytes, out ordinal)) return false;
        if (_indexed is not { } indexed)
        {
            if (ordinal != 1) return false;
            capacity = AwkRuntimeMemory.PlanArrayCapacity(0);
            return true;
        }
        if (ordinal <= indexed.Length)
        {
            capacity = indexed.Length;
            return true;
        }
        if (ordinal != indexed.Length + 1 || indexed.Length >= Array.MaxLength) return false;
        capacity = AwkRuntimeMemory.PlanArrayCapacity(indexed.Length);
        return true;
    }

    internal long EstimateIndexedInsertionBackingBytes(int capacity) =>
        _indexed is { } indexed && indexed.Length == capacity ? 0 : AwkRuntimeMemory.EstimateIndexedArrayBackingBytes(capacity);

    // The caller admits the complete new backing while the current one is live.
    // Hole insertion reuses that backing; growth copies stay private until publication.
    internal AwkValue?[] PrepareIndexedCapacity(int capacity, Action? checkpoint)
    {
        if (_indexed is { } ready && ready.Length == capacity) return ready;
        var result = new AwkValue?[capacity];
        if (_indexed is { } source)
        {
            const int chunk = AwkRuntimeLimits.CancellationCheckBytes / 32;
            for (var offset = 0; offset < source.Length;)
            {
                checkpoint?.Invoke();
                var length = Math.Min(chunk, source.Length - offset);
                Array.Copy(source, offset, result, offset, length);
                offset += length;
            }
        }
        return result;
    }

    internal void PublishIndexedInsertion(AwkValue?[] cells, int ordinal, AwkValue value, Action? checkpoint)
    {
        if (cells[ordinal - 1] is not null) throw new InvalidOperationException("Duplicate indexed insertion.");
        cells[ordinal - 1] = value;
        _indexed = cells;
        _indexedCount++;
        _checkpoint = checkpoint;
    }

    internal int EnsureCapacity(int capacity)
    {
        if (_indexed is not { } cells) return capacity == 0 ? _lookup.Dictionary.EnsureCapacity(0) : WritableDictionary().EnsureCapacity(capacity);
        if (capacity != 0) throw new InvalidOperationException("Indexed growth requires admitted storage preparation.");
        return cells.Length;
    }

    internal void Add(AwkArrayKey key, AwkValue value)
    {
        if (_indexed is { } indexed)
        {
            // Unpublished clones fill admitted slots. Language insertion prepares storage separately.
            Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
            var bytes = key.AsSpan(buffer);
            if (!TryParseOrdinal(bytes, out var ordinal) || ordinal > indexed.Length || indexed[ordinal - 1] is not null)
                throw new InvalidOperationException("Invalid indexed clone position.");
            indexed[ordinal - 1] = value;
            _indexedCount++;
            return;
        }
        WritableDictionary().Add(key, new(value));
    }

    internal AwkArrayCell AddDictionarySlot(AwkArrayKey key, AwkValue value)
    {
        if (_indexed is not null) throw new InvalidOperationException("A dictionary insertion requires dictionary storage.");
        var cell = new AwkArrayCell(value);
        WritableDictionary().Add(key, cell);
        return cell;
    }

    private Dictionary<AwkArrayKey, AwkArrayCell> WritableDictionary()
    {
        if (IsDetached) throw new InvalidOperationException("Detached array storage cannot mutate.");
        if (ReferenceEquals(_lookup.Dictionary, DetachedSlots))
            _lookup = new Dictionary<AwkArrayKey, AwkArrayCell>(_comparer).GetAlternateLookup<ReadOnlySpan<byte>>();
        return _lookup.Dictionary;
    }

    internal bool Remove(AwkArrayKey key)
    {
        if (_indexed is not { } indexed) return _lookup.Dictionary.Remove(key);
        Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        if (key.ByteLength > 10 || !TryGetIndexed(key.AsSpan(buffer), out var ordinal, out _)) return false;
        indexed[ordinal - 1] = null;
        _indexedCount--;
        return true;
    }

    // Private staging retains the old indexed backing until its caller commits publication.
    internal Dictionary<AwkArrayKey, AwkArrayCell> BuildDictionary(int capacity, Action? checkpoint)
    {
        if (_indexed is not { } indexed) throw new InvalidOperationException("Only indexed storage requires promotion.");
        var result = new Dictionary<AwkArrayKey, AwkArrayCell>(capacity, _comparer);
        for (var index = 0; index < indexed.Length; index++)
        {
            if ((index & 63) == 0) checkpoint?.Invoke();
            if (indexed[index] is { } value) result.Add(AwkArrayKey.FromOrdinal(index + 1), new(value));
        }
        return result;
    }

    internal void PublishDictionary(Dictionary<AwkArrayKey, AwkArrayCell> dictionary)
    {
        _lookup = dictionary.GetAlternateLookup<ReadOnlySpan<byte>>();
        _indexed = null;
        _indexedCount = 0;
    }

    public AwkArrayIdentity? Parent { get; private set; }

    public bool IsDetached { get; private set; }

    public void Detach()
    {
        // Removed owners can still be in argument/target staging. They retain no old
        // backing or payload; mutation must resolve a live target before using storage.
        _lookup = DetachedSlots.GetAlternateLookup<ReadOnlySpan<byte>>();
        _indexed = null;
        _indexedCount = 0;
        _comparer = AwkArrayKeyComparer.Instance;
        _checkpoint = null;
        Parent = null;
        IsDetached = true;
    }

    public bool IsWithin(AwkArrayIdentity ancestor, Action? checkpoint = null)
    {
        var visited = 0;
        for (AwkArrayIdentity? current = this; current is not null; current = current.Parent)
        {
            if ((visited++ & 63) == 0) checkpoint?.Invoke();
            if (ReferenceEquals(current, ancestor)) return true;
        }
        return false;
    }

    public void ReplaceSlots(AwkArrayIdentity replacement)
    {
        if (ReferenceEquals(this, replacement)) return;
        _lookup = replacement._lookup;
        _comparer = replacement._comparer;
        _indexed = replacement._indexed;
        _indexedCount = replacement._indexedCount;
        _checkpoint = replacement._checkpoint;
        replacement.Detach();
        AdoptChildren();
    }

    private void AdoptChildren()
    {
        if (_indexed is { } indexed)
        {
            foreach (var slot in indexed)
                if (slot is { } value && value.TryGetArrayBinding(out var child)) child.Parent = this;
            return;
        }
        foreach (var pair in this)
            if (pair.Value.TryGetArrayBinding(out var child)) child.Parent = this;
    }

    public Enumerator GetEnumerator() => GetEnumerator(_checkpoint);
    internal Enumerator GetEnumerator(Action? checkpoint) => _indexed is { } cells ? new(cells, checkpoint) : new(_lookup.Dictionary.GetEnumerator());
    IEnumerator<KeyValuePair<AwkArrayKey, AwkValue>> IEnumerable<KeyValuePair<AwkArrayKey, AwkValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<KeyValuePair<AwkArrayKey, AwkValue>>
    {
        private Dictionary<AwkArrayKey, AwkArrayCell>.Enumerator _dictionary;
        private readonly AwkValue?[]? _indexed;
        private readonly Action? _checkpoint;
        private int _index;
        public KeyValuePair<AwkArrayKey, AwkValue> Current { get; private set; }
        object IEnumerator.Current => Current;
        internal Enumerator(AwkValue?[] cells, Action? checkpoint) { _indexed = cells; _checkpoint = checkpoint; }
        internal Enumerator(Dictionary<AwkArrayKey, AwkArrayCell>.Enumerator dictionary) { _dictionary = dictionary; }
        public bool MoveNext()
        {
            if (_indexed is not { } cells)
            {
                if (!_dictionary.MoveNext()) return false;
                var entry = _dictionary.Current;
                Current = new(entry.Key, entry.Value.Slot);
                return true;
            }
            while (_index < cells.Length)
            {
                var ordinal = ++_index;
                if (cells[ordinal - 1] is not { } cell)
                {
                    if ((ordinal & 63) == 0) _checkpoint?.Invoke();
                    continue;
                }
                Current = new(AwkArrayKey.FromOrdinal(ordinal), cell);
                return true;
            }
            return false;
        }
        // Retirement consumes the captured backing, even after its owner detaches.
        internal void RetireCurrent()
        {
            if (_indexed is { } values) values[_index - 1] = null;
            else _dictionary.Current.Value.Detach();
            Current = default;
        }
        public void Dispose()
        {
            Current = default;
            _dictionary.Dispose();
            _dictionary = default;
        }
        public void Reset() => throw new NotSupportedException();
    }

    public readonly struct ByteView(AwkArrayIdentity owner)
    {
        public bool ContainsKey(ReadOnlySpan<byte> key) => owner._indexed is null
            ? owner._lookup.ContainsKey(key) : owner.TryGetIndexed(key, out _, out _);
        public bool TryGetValue(ReadOnlySpan<byte> key, out AwkValue value)
        {
            if (owner._indexed is not null) return owner.TryGetIndexed(key, out _, out value);
            if (owner._lookup.TryGetValue(key, out var cell))
            {
                value = cell.Slot;
                return true;
            }
            value = default;
            return false;
        }
        public bool TryGetValue(ReadOnlySpan<byte> key, out AwkArrayKey retainedKey, out AwkValue value) =>
            TryGetEntry(key, out retainedKey, out value, out _);

        internal bool TryGetEntry(ReadOnlySpan<byte> key, out AwkArrayKey retainedKey,
            out AwkValue value, out AwkArrayCell? cell)
        {
            if (owner._indexed is null)
            {
                if (owner._lookup.TryGetValue(key, out retainedKey, out cell))
                {
                    value = cell.Slot;
                    return true;
                }
                value = default;
                return false;
            }
            cell = null;
            if (owner.TryGetIndexed(key, out var ordinal, out value))
            {
                retainedKey = AwkArrayKey.FromOrdinal(ordinal);
                return true;
            }
            retainedKey = default;
            return false;
        }
    }
}
