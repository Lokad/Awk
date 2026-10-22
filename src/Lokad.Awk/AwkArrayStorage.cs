using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkArrayStorage
{
    private readonly Dictionary<AwkArrayName, Dictionary<AwkArrayKey, AwkVariableSlot>> _arrays = new();

    private readonly IEqualityComparer<AwkArrayKey> _keyComparer;
    private readonly AwkRuntimeCounters _counters;
    private AwkArrayName _cachedName;
    private Dictionary<AwkArrayKey, AwkVariableSlot>? _cachedSlots;

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
        TryGetCachedSlots(name, out _) || _arrays.ContainsKey(name);

    public bool TryGetSlots(
        AwkArrayName name,
        [NotNullWhen(true)] out Dictionary<AwkArrayKey, AwkVariableSlot>? slots)
    {
        if (TryGetCachedSlots(name, out slots))
            return true;
        if (!_arrays.TryGetValue(name, out slots))
            return false;
        _cachedName = name;
        _cachedSlots = slots;
        return true;
    }

    public Dictionary<AwkArrayKey, AwkVariableSlot> ResolveSlots(AwkArrayName name)
    {
        if (_arrays.TryGetValue(name, out var slots))
            return slots;

        slots = new Dictionary<AwkArrayKey, AwkVariableSlot>(_keyComparer);
        _arrays[name] = slots;
        return slots;
    }

    public void SetSlots(AwkArrayName name, Dictionary<AwkArrayKey, AwkVariableSlot> slots)
    {
        _arrays[name] = slots;
        if (_cachedSlots != null && _cachedName == name)
            _cachedSlots = slots;
    }

    public void Clear(AwkArrayName name)
    {
        SetSlots(name, new Dictionary<AwkArrayKey, AwkVariableSlot>(_keyComparer));
    }

    public bool Remove(
        AwkArrayName name,
        [NotNullWhen(true)] out Dictionary<AwkArrayKey, AwkVariableSlot>? slots)
    {
        var removed = _arrays.Remove(name, out slots);
        if (removed && _cachedName == name)
        {
            _cachedName = default;
            _cachedSlots = null;
        }
        return removed;
    }

    private bool TryGetCachedSlots(AwkArrayName name,
        [NotNullWhen(true)] out Dictionary<AwkArrayKey, AwkVariableSlot>? slots)
    {
        if (_cachedSlots is { } cached && _cachedName == name)
        {
            _counters.RecordArrayContainerCacheHit();
            slots = cached;
            return true;
        }
        slots = null;
        return false;
    }
}
