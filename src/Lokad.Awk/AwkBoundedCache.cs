using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkBoundedCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly long _maxWeight;
    private readonly Dictionary<TKey, CacheEntry> _entries = [];
    private readonly LinkedList<TKey> _recency = [];
    private readonly Action<TValue>? _release;
    private long _weight;

    public AwkBoundedCache(int capacity, long maxWeight = long.MaxValue, Action<TValue>? release = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWeight);

        _capacity = capacity;
        _maxWeight = maxWeight;
        _release = release;
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

    public bool Set(TKey key, TValue value, long weight = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
        if (weight > _maxWeight)
            return false;

        if (_entries.TryGetValue(key, out var existing))
        {
            var nextWeight = AwkRuntimeMemory.CheckedAdd(_weight - existing.Weight, weight);
            while (nextWeight > _maxWeight)
            {
                if (!TryEvictLeastRecent(hasRetainedKey: true, key))
                    return false;

                nextWeight = AwkRuntimeMemory.CheckedAdd(_weight - existing.Weight, weight);
            }

            _recency.Remove(existing.RecencyNode);
            _recency.AddLast(existing.RecencyNode);
            _entries[key] = new CacheEntry(value, existing.RecencyNode, weight);
            _weight = nextWeight;
            if (_release is not null && !ReferenceEquals(existing.Value, value)) _release(existing.Value);
            return true;
        }

        while (_entries.Count == _capacity || weight > _maxWeight - _weight)
        {
            if (!TryEvictLeastRecent(hasRetainedKey: false, key))
                return false;
        }

        var recencyNode = _recency.AddLast(key);
        _entries.Add(key, new CacheEntry(value, recencyNode, weight));
        _weight = AwkRuntimeMemory.CheckedAdd(_weight, weight);
        return true;

        bool TryEvictLeastRecent(bool hasRetainedKey, TKey retainedKey)
        {
            var leastRecent = _recency.First;
            while (leastRecent != null &&
                   hasRetainedKey &&
                   EqualityComparer<TKey>.Default.Equals(leastRecent.Value, retainedKey))
            {
                leastRecent = leastRecent.Next;
            }

            if (leastRecent == null)
                return false;

            if (!_entries.Remove(leastRecent.Value, out var removed))
                throw new InvalidOperationException("A cache recency entry must have a value.");

            _recency.Remove(leastRecent);
            _weight -= removed.Weight;
            _release?.Invoke(removed.Value);
            return true;
        }
    }

    public void Clear()
    {
        if (_release is not null)
            foreach (var entry in _entries.Values) _release(entry.Value);
        _entries.Clear();
        _recency.Clear();
        _weight = 0;
    }

    private readonly record struct CacheEntry(
        TValue Value,
        LinkedListNode<TKey> RecencyNode,
        long Weight);
}
