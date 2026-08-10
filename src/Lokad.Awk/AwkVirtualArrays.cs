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

    public bool ContainsValue(string name)
    {
        if (!AwkIdentifier.TryParse(name, out var identifier))
            return false;

        return IsName(name) ||
            containsArray(new AwkArrayName(identifier)) ||
            tryResolveScalar(new AwkVariableName(identifier), out _);
    }

    public TResult[] SnapshotValues<TState, TResult>(
        int count,
        TState state,
        Func<TState, AwkArrayKey, AwkValue, TResult> project)
    {
        var result = new TResult[count];
        var index = 0;
        result[index++] = project(
            state,
            new AwkArrayKey(AwkVariables.SymbolTableName),
            AwkValue.FromText(AwkText.Empty));

        foreach (var scalar in getScalars())
        {
            if (IsName(scalar.Name.Text))
                continue;

            var value = tryGetScalar(scalar.SlotId, out var slot)
                ? createValue(slot)
                : AwkValue.Unassigned();
            result[index++] = project(state, new AwkArrayKey(scalar.Name.Text), value);
        }

        foreach (var arrayName in getArrayNames())
        {
            var name = arrayName.Text;
            if (IsName(name) || tryResolveScalar(new AwkVariableName(arrayName.Identifier), out _))
                continue;

            result[index++] = project(
                state,
                new AwkArrayKey(name),
                AwkValue.FromText(AwkText.Empty));
        }

        return result;
    }

    public void CopyKeys(AwkArrayKey[] destination)
    {
        var index = 0;
        destination[index++] = new AwkArrayKey(AwkVariables.SymbolTableName);

        foreach (var scalar in getScalars())
        {
            if (!IsName(scalar.Name.Text))
                destination[index++] = new AwkArrayKey(scalar.Name.Text);
        }

        foreach (var arrayName in getArrayNames())
        {
            var name = arrayName.Text;
            if (!IsName(name) && !tryResolveScalar(new AwkVariableName(arrayName.Identifier), out _))
                destination[index++] = new AwkArrayKey(name);
        }
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
