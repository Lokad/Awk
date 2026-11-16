using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkBoundedCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    // Recency nodes own the key/value; the dictionary retains their location.
    private readonly Dictionary<TKey, LinkedListNode<CacheEntry>> _entries;
    private readonly LinkedList<CacheEntry> _recency = [];
    private readonly Action<TValue>? _release;

    public AwkBoundedCache(int capacity, Action<TValue>? release = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _entries = new(capacity);
        _release = release;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_recency.Last is { Value: var recent } && _entries.Comparer.Equals(recent.Key, key))
        {
            value = recent.Value;
            return true;
        }
        if (!_entries.TryGetValue(key, out var entry))
        {
            value = default;
            return false;
        }
        MakeRecent(entry);
        value = entry.Value.Value;
        return true;
    }

    public void Set(TKey key, TValue value)
    {
        if (_entries.TryGetValue(key, out var existing))
        {
            var previous = existing.Value;
            MakeRecent(existing);
            existing.Value = new(previous.Key, value);
            if (!ReferenceEquals(previous.Value, value)) _release?.Invoke(previous.Value);
            return;
        }
        if (_entries.Count == _capacity && _recency.First is { } oldest)
        {
            _entries.Remove(oldest.Value.Key);
            _recency.RemoveFirst();
            _release?.Invoke(oldest.Value.Value);
        }
        _entries.Add(key, _recency.AddLast(new CacheEntry(key, value)));
    }

    public void Clear()
    {
        foreach (var entry in _entries.Values) _release?.Invoke(entry.Value.Value);
        _entries.Clear();
        _recency.Clear();
    }

    private void MakeRecent(LinkedListNode<CacheEntry> entry)
    {
        if (ReferenceEquals(entry, _recency.Last)) return;
        _recency.Remove(entry);
        _recency.AddLast(entry);
    }

    private readonly record struct CacheEntry(TKey Key, TValue Value);
}
