using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkArrayStorage
{
    private readonly Dictionary<AwkArrayName, Dictionary<AwkArrayKey, AwkVariableSlot>> _ordinaryArrays = new();

    private readonly Dictionary<AwkArrayName, Dictionary<AwkArrayKey, AwkVariableSlot>> _specialArrays = new();

    private readonly HashSet<AwkArrayName> _storedSpecialArrayNames = [];

    private readonly IEqualityComparer<AwkArrayKey> _keyComparer;

    public AwkArrayStorage()
        : this(AwkArrayKeyComparer.Instance)
    {
    }

    public AwkArrayStorage(IEqualityComparer<AwkArrayKey> keyComparer)
    {
        _keyComparer = keyComparer;
        foreach (var definition in AwkSpecialVariables.Arrays)
        {
            if (definition.Id != AwkSpecialVariableId.SymbolTable)
                _storedSpecialArrayNames.Add(definition.ArrayName);
        }
    }

    public IEnumerable<AwkArrayName> Names
    {
        get
        {
            foreach (var name in _ordinaryArrays.Keys)
            {
                if (!name.IsFunctionLocal)
                    yield return name;
            }

            foreach (var name in _specialArrays.Keys)
                yield return name;
        }
    }

    public bool Contains(AwkArrayName name) =>
        SelectTable(name).ContainsKey(name);

    public bool TryGetSlots(
        AwkArrayName name,
        [NotNullWhen(true)] out Dictionary<AwkArrayKey, AwkVariableSlot>? slots)
    {
        return SelectTable(name).TryGetValue(name, out slots);
    }

    public Dictionary<AwkArrayKey, AwkVariableSlot> ResolveSlots(AwkArrayName name)
    {
        var table = SelectTable(name);
        if (table.TryGetValue(name, out var slots))
            return slots;

        slots = new Dictionary<AwkArrayKey, AwkVariableSlot>(_keyComparer);
        table[name] = slots;
        return slots;
    }

    public void SetSlots(AwkArrayName name, Dictionary<AwkArrayKey, AwkVariableSlot> slots)
    {
        SelectTable(name)[name] = slots;
    }

    public void Clear(AwkArrayName name)
    {
        SetSlots(name, new Dictionary<AwkArrayKey, AwkVariableSlot>(_keyComparer));
    }

    public bool Remove(
        AwkArrayName name,
        [NotNullWhen(true)] out Dictionary<AwkArrayKey, AwkVariableSlot>? slots) =>
        SelectTable(name).Remove(name, out slots);

    private Dictionary<AwkArrayName, Dictionary<AwkArrayKey, AwkVariableSlot>> SelectTable(AwkArrayName name) =>
        _storedSpecialArrayNames.Contains(name) ? _specialArrays : _ordinaryArrays;
}
