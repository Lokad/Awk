using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkBoundedCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, CacheEntry> _entries = [];
    private readonly LinkedList<TKey> _recency = [];

    public AwkBoundedCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _capacity = capacity;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            value = default;
            return false;
        }

        _recency.Remove(entry.RecencyNode);
        _recency.AddLast(entry.RecencyNode);
        value = entry.Value;
        return true;
    }

    public void Set(TKey key, TValue value)
    {
        if (_entries.TryGetValue(key, out var existing))
        {
            _recency.Remove(existing.RecencyNode);
            _recency.AddLast(existing.RecencyNode);
            _entries[key] = new CacheEntry(value, existing.RecencyNode);
            return;
        }

        if (_entries.Count == _capacity)
        {
            var leastRecent = _recency.First ??
                throw new InvalidOperationException("A full cache must have a least-recent entry.");
            _recency.RemoveFirst();
            _entries.Remove(leastRecent.Value);
        }

        var recencyNode = _recency.AddLast(key);
        _entries.Add(key, new CacheEntry(value, recencyNode));
    }

    private readonly record struct CacheEntry(
        TValue Value,
        LinkedListNode<TKey> RecencyNode);
}
