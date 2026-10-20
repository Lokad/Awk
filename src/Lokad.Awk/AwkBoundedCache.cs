using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkBoundedCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, CacheEntry> _entries = [];
    private readonly LinkedList<TKey> _recency = [];
    private readonly Action<TValue>? _release;

    public AwkBoundedCache(int capacity, Action<TValue>? release = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _release = release;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            value = default;
            return false;
        }
        _recency.Remove(entry.Node);
        _recency.AddLast(entry.Node);
        value = entry.Value;
        return true;
    }

    public void Set(TKey key, TValue value)
    {
        if (_entries.TryGetValue(key, out var existing))
        {
            _recency.Remove(existing.Node);
            _recency.AddLast(existing.Node);
            _entries[key] = new(value, existing.Node);
            if (!ReferenceEquals(existing.Value, value)) _release?.Invoke(existing.Value);
            return;
        }
        if (_entries.Count == _capacity && _recency.First is { } oldest)
        {
            var removed = _entries[oldest.Value];
            _entries.Remove(oldest.Value);
            _recency.RemoveFirst();
            _release?.Invoke(removed.Value);
        }
        _entries.Add(key, new(value, _recency.AddLast(key)));
    }

    public void Clear()
    {
        foreach (var entry in _entries.Values) _release?.Invoke(entry.Value);
        _entries.Clear();
        _recency.Clear();
    }

    private readonly record struct CacheEntry(TValue Value, LinkedListNode<TKey> Node);
}
