using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly record struct AwkScalarInventoryItem(AwkGlobalSlotId SlotId, AwkVariableName Name);

internal delegate bool AwkTryResolveScalarSlot(AwkVariableName name, out AwkGlobalSlotId slotId);

internal delegate bool AwkTryGetScalarSlot(AwkGlobalSlotId slotId, out AwkVariableSlot slot);

internal sealed class AwkSymbolTableArray(
    Func<IEnumerable<AwkScalarInventoryItem>> getScalars,
    AwkTryResolveScalarSlot tryResolveScalar,
    AwkTryGetScalarSlot tryGetScalar,
    Func<AwkArrayName, bool> containsArray,
    Func<IEnumerable<AwkArrayName>> getArrayNames,
    Func<AwkVariableSlot, AwkValue> createValue,
    Action<string, AwkValue> assignScalarValue)
{
    public static bool IsName(string name) =>
        string.Equals(name, AwkVariables.SymbolTableName, StringComparison.Ordinal);

    public int Count
    {
        get
        {
            var count = 1;
            foreach (var scalar in getScalars())
            {
                if (!IsName(scalar.Name.Text))
                    count++;
            }

            foreach (var arrayName in getArrayNames())
            {
                var name = arrayName.Text;
                if (!IsName(name) && !tryResolveScalar(new AwkVariableName(arrayName.Identifier), out _))
                    count++;
            }

            return count;
        }
    }

    public AwkValue GetValue(string name)
    {
        var identifier = ParseName(name);

        if (containsArray(new AwkArrayName(identifier)) || IsName(name))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name));

        if (!tryResolveScalar(new AwkVariableName(identifier), out var slotId) ||
            !tryGetScalar(slotId, out var slot))
        {
            return AwkValue.Unassigned();
        }

        return createValue(slot);
    }

    public void AssignValue(string name, AwkValue value)
    {
        var identifier = ParseName(name);

        if (containsArray(new AwkArrayName(identifier)) || IsName(name))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name));

        assignScalarValue(name, value);
    }

    public static bool ContainsValue(string name) =>
        AwkIdentifiers.IsValidName(name);

    public IReadOnlyDictionary<AwkArrayKey, AwkValue> SnapshotValues()
    {
        var values = new Dictionary<AwkArrayKey, AwkValue>();
        foreach (var name in SnapshotNames())
        {
            values[new AwkArrayKey(name)] = IsName(name) || containsArray(AwkArrayName.FromText(name))
                ? AwkValue.FromText(AwkText.Empty)
                : GetValue(name);
        }

        return values;
    }

    public IReadOnlyList<AwkArrayKey> SnapshotKeys()
    {
        var names = SnapshotNames();
        var keys = new AwkArrayKey[names.Count];
        var index = 0;
        foreach (var name in names)
            keys[index++] = new AwkArrayKey(name);

        return keys;
    }

    private HashSet<string> SnapshotNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            AwkVariables.SymbolTableName
        };

        foreach (var scalar in getScalars())
            names.Add(scalar.Name.Text);

        foreach (var name in getArrayNames())
            names.Add(name.Text);

        return names;
    }

    private static AwkIdentifier ParseName(string name)
    {
        if (!AwkIdentifier.TryParse(name, out var identifier))
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidSymtabReference);

        return identifier;
    }
}

internal static class AwkSpecialArrayPolicy
{
    public static bool IsElementReadOnly(string name) =>
        AwkSpecialVariables.TryGetArray(name, out var definition) &&
        !definition.CanModifyArrayElements;

    public static void ThrowIfElementMutationForbidden(string name)
    {
        if (IsElementReadOnly(name))
            throw new AwkRuntimeException(AwkMessages.FormatCannotModifySpecialArray(name));
    }

    public static void ThrowIfReplacementForbidden(string name)
    {
        if (!AwkSpecialVariables.TryGetArray(name, out var definition) ||
            definition.CanReplaceArray)
        {
            return;
        }

        throw new AwkRuntimeException(definition.MutationPolicy == AwkSpecialArrayMutationPolicy.NonReplaceable
            ? AwkMessages.ErrorCannotDeleteSymtab
            : AwkMessages.FormatCannotModifySpecialArray(name));
    }
}
