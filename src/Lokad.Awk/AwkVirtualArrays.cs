using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly record struct AwkScalarInventoryItem(AwkGlobalSlotId SlotId, AwkVariableName Name);

internal readonly record struct AwkArraySnapshotSize(int Count, long KeyTextBytes, long ValueTextBytes = 0)
{
    public long TextBytes => AwkRuntimeMemory.CheckedAdd(KeyTextBytes, ValueTextBytes);
}

internal delegate bool AwkTryResolveScalarSlot(AwkVariableName name, out AwkGlobalSlotId slotId);

internal delegate bool AwkTryGetScalarSlot(AwkGlobalSlotId slotId, out AwkVariableSlot slot);

internal sealed class AwkSymbolTableArray(
    Func<IReadOnlySet<AwkVariableName>> getGlobalNames,
    Func<IEnumerable<AwkScalarInventoryItem>> getScalars,
    AwkTryResolveScalarSlot tryResolveScalar,
    AwkTryGetScalarSlot tryGetScalar,
    Func<AwkArrayName, bool> containsArray,
    Func<IEnumerable<AwkArrayName>> getArrayNames,
    Func<AwkVariableSlot, AwkValue> createValue,
    Action<string, AwkValue> assignScalarValue,
    Action? checkpoint = null)
{
    public static bool IsName(string name) =>
        string.Equals(name, AwkVariables.SymbolTableName, StringComparison.Ordinal);

    public int Count
    {
        get
        {
            var count = 0;
            foreach (var name in EnumerateNames())
                count++;
            return count;
        }
    }

    public AwkArraySnapshotSize MeasureSnapshot(bool retainValues = false)
    {
        var count = 0;
        var keyTextBytes = 0L;
        var valueTextBytes = 0L;
        foreach (var name in EnumerateNames())
        {
            count = AwkRuntimeMemory.CheckedAdd(count, 1);
            // AwkIdentifier admits ASCII names, so byte length equals character length.
            keyTextBytes = AwkRuntimeMemory.CheckedAdd(keyTextBytes,
                AwkRuntimeMemory.EstimateTextBytes(name.Text.Length));
            if (retainValues && !containsArray(new AwkArrayName(name.Identifier)) &&
                tryResolveScalar(name, out var slotId) && tryGetScalar(slotId, out var slot))
                valueTextBytes = AwkRuntimeMemory.CheckedAdd(valueTextBytes, slot.RetainedTextMemoryBytes);
        }
        return new AwkArraySnapshotSize(count, keyTextBytes, valueTextBytes);
    }

    public AwkValue GetValue(string name)
    {
        if (!AwkIdentifier.TryParse(name, out var identifier) || IsName(name))
            throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(AwkVariables.SymbolTableName, name));

        if (containsArray(new AwkArrayName(identifier)))
            return AwkValue.FromGlobalArrayReference(identifier);

        if (!tryResolveScalar(new AwkVariableName(identifier), out var slotId) ||
            !tryGetScalar(slotId, out var slot))
        {
            if (getGlobalNames().Contains(new AwkVariableName(identifier)))
                return AwkValue.Unassigned();
            throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(AwkVariables.SymbolTableName, name));
        }

        return createValue(slot).AsScalarCopy();
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

        return !IsName(name) && (
            getGlobalNames().Contains(new AwkVariableName(identifier)) ||
            containsArray(new AwkArrayName(identifier)) ||
            tryResolveScalar(new AwkVariableName(identifier), out _));
    }

    public AwkRuntimeValueKind GetElementRuntimeKind(string name)
    {
        if (!IsName(name) && AwkIdentifier.TryParse(name, out var identifier) &&
            containsArray(new AwkArrayName(identifier)))
            return AwkRuntimeValueKind.Array;

        return GetValue(name).RuntimeKind;
    }

    public TResult[] SnapshotValues<TState, TResult>(
        int count,
        TState state,
        Func<TState, AwkArrayKey, AwkValue, TResult> project)
    {
        var result = new TResult[count];
        var index = 0;
        foreach (var name in EnumerateNames())
        {
            var value = containsArray(new AwkArrayName(name.Identifier))
                ? AwkValue.FromText(AwkText.Empty)
                : tryResolveScalar(name, out var slotId) && tryGetScalar(slotId, out var slot)
                    ? createValue(slot).AsScalarCopy()
                    : AwkValue.Unassigned();
            result[index++] = project(state, new AwkArrayKey(name.Text, checkpoint), value);
        }

        return result;
    }

    public void CopyKeys(AwkArrayKey[] destination)
    {
        var index = 0;
        foreach (var name in EnumerateNames())
            destination[index++] = new AwkArrayKey(name.Text, checkpoint);
    }

    private IEnumerable<AwkVariableName> EnumerateNames()
    {
        checkpoint?.Invoke();
        var visited = 0;
        var globals = getGlobalNames();
        foreach (var name in globals)
        {
            if ((visited++ & 63) == 0)
                checkpoint?.Invoke();
            if (!IsName(name.Text))
                yield return name;
        }

        foreach (var scalar in getScalars())
        {
            if ((visited++ & 63) == 0)
                checkpoint?.Invoke();
            if (!IsName(scalar.Name.Text) && !globals.Contains(scalar.Name))
                yield return scalar.Name;
        }

        foreach (var arrayName in getArrayNames())
        {
            if ((visited++ & 63) == 0)
                checkpoint?.Invoke();
            var name = arrayName.Text;
            var variableName = new AwkVariableName(arrayName.Identifier);
            if (!IsName(name) && !globals.Contains(variableName) && !tryResolveScalar(variableName, out _))
                yield return variableName;
        }
        checkpoint?.Invoke();
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
    public static bool IsElementReadOnly(string name,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault) =>
        AwkSpecialVariables.TryGetArray(name, out var definition) &&
        AwkCompatibilityNames.EnablesSpecial(definition.Id, mode) &&
        !definition.CanModifyArrayElements;

    public static void ThrowIfElementMutationForbidden(string name,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault)
    {
        if (IsElementReadOnly(name, mode))
            throw new AwkRuntimeException(AwkMessages.FormatCannotModifySpecialArray(name));
    }

    public static void ThrowIfReplacementForbidden(string name,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault)
    {
        if (!AwkSpecialVariables.TryGetArray(name, out var definition) ||
            !AwkCompatibilityNames.EnablesSpecial(definition.Id, mode) ||
            definition.CanReplaceArray)
        {
            return;
        }

        throw new AwkRuntimeException(definition.MutationPolicy == AwkSpecialArrayMutationPolicy.NonReplaceable
            ? AwkMessages.ErrorCannotDeleteSymtab
            : AwkMessages.FormatCannotModifySpecialArray(name));
    }
}
