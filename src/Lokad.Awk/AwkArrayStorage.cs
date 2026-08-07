using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkArrayStorage
{
    private readonly Dictionary<AwkArrayName, Dictionary<AwkArrayKey, AwkVariableSlot>> _ordinaryArrays = new();

    private readonly Dictionary<AwkArrayName, Dictionary<AwkArrayKey, AwkVariableSlot>> _specialArrays = new();

    private readonly HashSet<AwkArrayName> _storedSpecialArrayNames = [];

    public AwkArrayStorage()
    {
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
                yield return name;

            foreach (var name in _specialArrays.Keys)
                yield return name;
        }
    }

    public bool Contains(AwkArrayName name) =>
        SelectTable(name).ContainsKey(name);

    public bool TryGetSlots(AwkArrayName name, out Dictionary<AwkArrayKey, AwkVariableSlot> slots)
    {
        if (SelectTable(name).TryGetValue(name, out var existingSlots))
        {
            slots = existingSlots;
            return true;
        }

        slots = new Dictionary<AwkArrayKey, AwkVariableSlot>();
        return false;
    }

    public Dictionary<AwkArrayKey, AwkVariableSlot> ResolveSlots(AwkArrayName name)
    {
        var table = SelectTable(name);
        if (table.TryGetValue(name, out var slots))
            return slots;

        slots = new Dictionary<AwkArrayKey, AwkVariableSlot>();
        table[name] = slots;
        return slots;
    }

    public void SetSlots(AwkArrayName name, Dictionary<AwkArrayKey, AwkVariableSlot> slots)
    {
        SelectTable(name)[name] = slots;
    }

    public void Clear(AwkArrayName name)
    {
        SetSlots(name, new Dictionary<AwkArrayKey, AwkVariableSlot>());
    }

    private Dictionary<AwkArrayName, Dictionary<AwkArrayKey, AwkVariableSlot>> SelectTable(AwkArrayName name) =>
        _storedSpecialArrayNames.Contains(name) ? _specialArrays : _ordinaryArrays;
}
