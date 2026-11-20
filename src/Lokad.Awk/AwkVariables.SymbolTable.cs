namespace Lokad.Awk;

internal sealed partial class AwkVariables
{
    internal static bool IsSymbolTableName(string name) =>
        string.Equals(name, SymbolTableName, StringComparison.Ordinal);

    private int SymbolCount
    {
        get
        {
            var count = 0;
            foreach (var name in EnumerateSymbolNames())
                count++;
            return count;
        }
    }

    private AwkArraySnapshotSize MeasureSymbolSnapshot(bool retainValues = false)
    {
        var count = 0;
        var keyTextBytes = 0L;
        var valueTextBytes = 0L;
        foreach (var name in EnumerateSymbolNames())
        {
            count = AwkRuntimeMemory.CheckedAdd(count, 1);
            // AwkIdentifier admits ASCII names, so byte length equals character length.
            keyTextBytes = AwkRuntimeMemory.CheckedAdd(keyTextBytes,
                AwkRuntimeMemory.EstimateTextBytes(name.Text.Length));
            if (retainValues && !_arrays.Contains(new AwkArrayName(name.Identifier)) &&
                TryReadSymbolSlot(name, out var slot))
                valueTextBytes = AwkRuntimeMemory.CheckedAdd(valueTextBytes, slot.RetainedTextMemoryBytes);
        }
        return new AwkArraySnapshotSize(count, keyTextBytes, valueTextBytes);
    }

    private AwkValue GetSymbolValue(string name)
    {
        if (!AwkIdentifier.TryParse(name, out var identifier) || IsSymbolTableName(name))
            throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(SymbolTableName, name));

        if (_arrays.Contains(new AwkArrayName(identifier)))
            return AwkValue.FromGlobalArrayReference(identifier);

        var variable = new AwkVariableName(identifier);
        if (!TryReadSymbolSlot(variable, out var slot))
        {
            if (_globalNames.Contains(variable))
                return AwkValue.Unassigned();
            throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(SymbolTableName, name));
        }

        return CreateValue(slot).AsScalarCopy();
    }

    private void AssignSymbolValue(string name, AwkValue value)
    {
        if (!AwkIdentifier.TryParse(name, out var identifier))
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidSymtabReference);

        if (_arrays.Contains(new AwkArrayName(identifier)) || IsSymbolTableName(name))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name));

        _state.ApplySpecialVariableAssignment(new AwkVariableName(identifier), value);
    }

    private bool ContainsSymbolValue(string name)
    {
        if (!AwkIdentifier.TryParse(name, out var identifier))
            return false;

        return !IsSymbolTableName(name) && (
            _globalNames.Contains(new AwkVariableName(identifier)) ||
            _arrays.Contains(new AwkArrayName(identifier)) ||
            _scalarSlotsByName.ContainsKey(new AwkVariableName(identifier)));
    }

    private TResult[] SnapshotSymbolValues<TState, TResult>(
        int count,
        TState state,
        Func<TState, AwkArrayKey, AwkValue, TResult> project)
    {
        var result = new TResult[count];
        var index = 0;
        foreach (var name in EnumerateSymbolNames())
        {
            var value = _arrays.Contains(new AwkArrayName(name.Identifier))
                ? AwkValue.FromText(AwkText.Empty)
                : TryReadSymbolSlot(name, out var slot)
                    ? CreateValue(slot).AsScalarCopy()
                    : AwkValue.Unassigned();
            result[index++] = project(state, new AwkArrayKey(name.Text, _state.CancellationCheckpoint), value);
        }

        return result;
    }

    private void CopySymbolKeys(AwkArrayKey[] destination)
    {
        var index = 0;
        foreach (var name in EnumerateSymbolNames())
            destination[index++] = new AwkArrayKey(name.Text, _state.CancellationCheckpoint);
    }

    private IEnumerable<AwkVariableName> EnumerateSymbolNames()
    {
        _state.ThrowIfCancellationRequested();
        var visited = 0;
        var globals = _globalNames;
        foreach (var name in globals)
        {
            if ((visited++ & 63) == 0)
                _state.ThrowIfCancellationRequested();
            if (!IsSymbolTableName(name.Text))
                yield return name;
        }

        for (var index = 0; index < _scalarSlots.Count; index++)
        {
            if ((visited++ & 63) == 0)
                _state.ThrowIfCancellationRequested();
            var name = _scalarSlots[index].Name;
            if (!IsSymbolTableName(name.Text) && !globals.Contains(name))
                yield return name;
        }

        foreach (var arrayName in _arrays.Names)
        {
            if ((visited++ & 63) == 0)
                _state.ThrowIfCancellationRequested();
            var name = arrayName.Text;
            var variableName = new AwkVariableName(arrayName.Identifier);
            if (!IsSymbolTableName(name) && !globals.Contains(variableName) && !_scalarSlotsByName.ContainsKey(variableName))
                yield return variableName;
        }
        _state.ThrowIfCancellationRequested();
    }

    private bool TryReadSymbolSlot(AwkVariableName name, out AwkValue slot)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var id))
        {
            var cell = GetScalarCell(id);
            slot = IsFieldCountCell(cell)
                ? AwkValue.ForVariableStorage(_state.RecordFields.GetFieldCountValue(), _state.NumberParsingMode)
                : cell.State.Slot;
            return true;
        }

        slot = default;
        return false;
    }
}
