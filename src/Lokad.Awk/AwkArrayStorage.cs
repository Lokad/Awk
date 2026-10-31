using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkArrayStorage
{
    private readonly Dictionary<AwkArrayName, AwkArrayIdentity> _arrays = new();

    private readonly IEqualityComparer<AwkArrayKey> _keyComparer;
    private readonly AwkRuntimeCounters _counters;
    private AwkArrayName _cachedName;
    private AwkArrayIdentity? _cachedArray;

    public AwkArrayStorage()
        : this(AwkArrayKeyComparer.Instance)
    {
    }

    public AwkArrayStorage(IEqualityComparer<AwkArrayKey> keyComparer)
        : this(keyComparer, AwkRuntimeCounters.Disabled)
    {
    }

    public AwkArrayStorage(IEqualityComparer<AwkArrayKey> keyComparer, AwkRuntimeCounters counters)
    {
        _keyComparer = keyComparer;
        _counters = counters;
    }

    public IEnumerable<AwkArrayName> Names
    {
        get
        {
            foreach (var name in _arrays.Keys)
            {
                if (!name.IsFunctionLocal)
                    yield return name;
            }
        }
    }

    public bool Contains(AwkArrayName name) =>
        TryGetCachedArray(name, out _) || _arrays.ContainsKey(name);

    public bool TryGetSlots(
        AwkArrayName name,
        [NotNullWhen(true)] out Dictionary<AwkArrayKey, AwkArrayCell>? slots)
    {
        var found = TryGetArray(name, out var array);
        slots = array?.Slots;
        return found;
    }

    public bool TryGetArray(AwkArrayName name, [NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        if (TryGetCachedArray(name, out array)) return true;
        if (!_arrays.TryGetValue(name, out array)) return false;
        _cachedName = name;
        _cachedArray = array;
        return true;
    }

    public Dictionary<AwkArrayKey, AwkArrayCell> ResolveSlots(AwkArrayName name) => ResolveArray(name).Slots;

    public AwkArrayIdentity ResolveArray(AwkArrayName name)
    {
        if (_arrays.TryGetValue(name, out var array)) return array;
        array = new AwkArrayIdentity(new Dictionary<AwkArrayKey, AwkArrayCell>(_keyComparer));
        _arrays[name] = array;
        return array;
    }

    public void SetSlots(AwkArrayName name, Dictionary<AwkArrayKey, AwkArrayCell> slots)
    {
        if (_arrays.TryGetValue(name, out var array)) array.ReplaceSlots(slots);
        else _arrays[name] = new AwkArrayIdentity(slots);
    }

    public void Clear(AwkArrayName name)
    {
        SetSlots(name, new Dictionary<AwkArrayKey, AwkArrayCell>(_keyComparer));
    }

    public bool Remove(
        AwkArrayName name,
        [NotNullWhen(true)] out Dictionary<AwkArrayKey, AwkArrayCell>? slots)
    {
        var removed = _arrays.Remove(name, out var array);
        slots = array?.Slots;
        if (removed && _cachedName == name)
        {
            _cachedName = default;
            _cachedArray = null;
        }
        return removed;
    }

    private bool TryGetCachedArray(AwkArrayName name,
        [NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        if (_cachedArray is { } cached && _cachedName == name)
        {
            _counters.RecordArrayContainerCacheHit();
            array = cached;
            return true;
        }
        array = null;
        return false;
    }
}
