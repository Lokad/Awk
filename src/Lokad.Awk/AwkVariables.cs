using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lokad.Awk;

internal sealed class AwkVariables
{
    public const string FunctionTableName = "FUNCTAB";
    public const string SymbolTableName = "SYMTAB";

    private readonly Func<AwkText, AwkValue> _createByteTextValue;
    private readonly Func<AwkText, AwkValue> _createNumericStringValue;
    private readonly Action<string, AwkValue> _applyScalarAssignment;
    private readonly int _maxArrayElements;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkMemoryGovernor _memoryGovernor;
    private readonly Dictionary<AwkVariableName, AwkGlobalSlotId> _scalarSlotsByName = new();
    private readonly List<AwkScalarSlot> _scalarSlots = [];
    private readonly AwkArrayStorage _arrays = new();
    private readonly AwkSymbolTableArray _symbolTable;
    private bool _governScalarMemory;

    private readonly struct AwkScalarSlot(AwkVariableName name, AwkScalarState state)
    {
        public AwkVariableName Name { get; } = name;

        public AwkScalarState State { get; } = state;

        public AwkScalarSlot WithState(AwkScalarState state) =>
            new(Name, state);
    }

    private readonly struct AwkScalarState(
        AwkVariableSlot slot,
        long baselineBytes,
        long chargedBytes,
        bool hasBaseline)
    {
        public AwkVariableSlot Slot { get; } = slot;
        public long BaselineBytes { get; } = baselineBytes;
        public long ChargedBytes { get; } = chargedBytes;
        public bool HasBaseline { get; } = hasBaseline;

        public static AwkScalarState Ungoverned(AwkVariableSlot slot) =>
            new(slot, baselineBytes: 0, chargedBytes: 0, hasBaseline: false);

        public AwkScalarState WithBaseline(long baselineBytes) =>
            new(Slot, baselineBytes, chargedBytes: 0, hasBaseline: true);

        public AwkScalarState WithSlot(AwkVariableSlot slot, long chargedBytes) =>
            new(slot, BaselineBytes, chargedBytes, hasBaseline: true);
    }

    public AwkVariables(
        int maxArrayElements,
        AwkMemoryGovernor memoryGovernor,
        AwkRuntimeCounters counters,
        Func<AwkText, AwkValue> createByteTextValue,
        Func<AwkText, AwkValue> createNumericStringValue,
        Action<string, AwkValue> applyScalarAssignment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxArrayElements);

        _maxArrayElements = maxArrayElements;
        _memoryGovernor = memoryGovernor;
        _counters = counters;
        _createByteTextValue = createByteTextValue;
        _createNumericStringValue = createNumericStringValue;
        _applyScalarAssignment = applyScalarAssignment;
        _symbolTable = new AwkSymbolTableArray(
            EnumerateScalarInventory,
            TryResolveScalarSlot,
            TryGetScalarSlot,
            _arrays.Contains,
            () => _arrays.Names,
            CreateValue,
            SetScalarValueFromSymbolTable);
    }

    public bool ContainsScalar(AwkVariableName name) =>
        _scalarSlotsByName.ContainsKey(name);

    public bool ContainsArray(AwkArrayName name) =>
        !name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text) ||
        _arrays.Contains(name);

    public bool TryGetArrayCount(AwkArrayName name, out int count)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
        {
            count = _symbolTable.Count;
            return true;
        }

        if (_arrays.TryGetSlots(name, out var values))
        {
            count = values.Count;
            return true;
        }

        count = 0;
        return false;
    }

    public void SetScalarText(AwkVariableName name, string value)
    {
        SetScalarSlot(name, AwkVariableSlot.FromText(value));
    }

    public void SetScalarText(AwkVariableName name, AwkText value)
    {
        SetScalarSlot(name, AwkVariableSlot.FromText(value));
    }

    public void SetScalarValue(AwkVariableName name, AwkValue value)
    {
        SetScalarSlot(name, AwkVariableSlot.FromValue(value));
    }

    public void CaptureScalarMemoryBaseline()
    {
        for (var index = 0; index < _scalarSlots.Count; index++)
        {
            var scalar = _scalarSlots[index];
            _scalarSlots[index] = scalar.WithState(scalar.State.WithBaseline(EstimateScalarBytes(scalar.State.Slot)));
        }

        _governScalarMemory = true;
    }

    public AwkValue GetOrCreateScalarValue(AwkVariableName name)
    {
        if (TryGetScalarState(name, out var state))
            return CreateValue(state.Slot);

        SetScalarSlot(name, new AwkVariableSlot(string.Empty, AwkVariableSlotKind.Unassigned));
        return AwkValue.Unassigned();
    }

    public AwkValue GetScalarValue(AwkGlobalSlotId id) =>
        CreateValue(GetScalarState(id).Slot);

    public void SetScalarValue(AwkGlobalSlotId id, AwkValue value)
    {
        SetScalarSlot(id, AwkVariableSlot.FromValue(value));
    }

    public AwkGlobalSlotId GetOrCreateScalarSlot(AwkVariableName name) =>
        GetOrCreateScalarSlotCore(name);

    public void InitializeArrayStringValue(AwkArrayName name, AwkArrayKey key, string value)
    {
        ThrowIfArrayReplacementWouldExceedLimit(1);
        ReplaceArraySlots(
            name,
            new Dictionary<AwkArrayKey, AwkVariableSlot>
            {
                [key] = AwkVariableSlot.FromText(value)
            });
    }

    public void InitializeIndexedArrayExternalStringValues(
        AwkArrayName name,
        IReadOnlyList<string> values,
        int firstIndex)
    {
        ThrowIfArrayReplacementWouldExceedLimit(values.Count);
        ReplaceArraySlots(
            name,
            CreateIndexedArraySlots(values, firstIndex, AwkVariableSlot.FromExternalText));
    }

    public void InitializeArrayExternalStringValues<TValue>(
        AwkArrayName name,
        IReadOnlyList<TValue> values,
        Func<TValue, AwkArrayKey> selectKey,
        Func<TValue, string> selectValue)
    {
        var slots = new Dictionary<AwkArrayKey, AwkVariableSlot>(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            slots[selectKey(value)] = AwkVariableSlot.FromExternalText(selectValue(value));
        }

        ThrowIfArrayReplacementWouldExceedLimit(slots.Count);
        ReplaceArraySlots(name, slots);
    }

    public void InitializeArrayStringNames(
        AwkArrayName name,
        int count,
        IEnumerable<string> names)
    {
        var slots = new Dictionary<AwkArrayKey, AwkVariableSlot>(count);
        foreach (var value in names)
            slots[new AwkArrayKey(value)] = AwkVariableSlot.FromText(value);

        ReplaceArraySlots(name, slots);
    }

    public AwkValue GetArrayValue(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
            return _symbolTable.GetValue(index.AsString(_counters));

        var values = GetOrCreateArraySlots(name);
        if (values.TryGetValue(index, out var slot))
            return CreateValue(slot);

        if (!name.IsFunctionLocal && AwkSpecialArrayPolicy.IsElementReadOnly(name.Text))
            return AwkValue.FromText(string.Empty);

        ThrowIfArrayWouldGrowPastLimit(values, index);
        SetArraySlot(values, index, new AwkVariableSlot(string.Empty, AwkVariableSlotKind.Unassigned));
        return AwkValue.Unassigned();
    }

    public bool TryPeekArrayValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
        {
            value = _symbolTable.GetValue(index.AsString(_counters));
            return true;
        }

        if (!name.IsFunctionLocal && ContainsScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        var values = GetOrCreateArraySlots(name);
        if (!values.TryGetValue(index, out var slot))
        {
            value = AwkValue.FromText(string.Empty);
            return false;
        }

        value = CreateValue(slot);
        return true;
    }

    public bool ContainsArrayValue(AwkArrayName name, AwkArrayKey index) =>
        !name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text)
            ? _symbolTable.ContainsValue(index.AsString(_counters))
            : GetOrCreateArraySlots(name).ContainsKey(index);

    public void SetArrayString(AwkArrayName name, AwkArrayKey index, string value)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
        {
            _symbolTable.AssignValue(
                index.AsString(_counters),
                _createByteTextValue(AwkText.FromString(value, _counters, _memoryGovernor)));
            return;
        }

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text);
        var values = GetOrCreateArraySlots(name);
        ThrowIfArrayWouldGrowPastLimit(values, index);
        SetArraySlot(values, index, AwkVariableSlot.FromText(value));
    }

    public void SetArrayValue(AwkArrayName name, AwkArrayKey index, AwkValue value)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
        {
            _symbolTable.AssignValue(index.AsString(_counters), value);
            return;
        }

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text);
        var values = GetOrCreateArraySlots(name);
        ThrowIfArrayWouldGrowPastLimit(values, index);
        SetArraySlot(values, index, AwkVariableSlot.FromValue(value));
    }

    public void DeleteArrayValue(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
            throw new AwkRuntimeException(AwkMessages.ErrorCannotDeleteSymtab);

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text);
        var values = GetOrCreateArraySlots(name);
        if (values.TryGetValue(index, out var slot) && values.Remove(index))
            _memoryGovernor.ReleaseCommitted(EstimateElementBytes(index, slot));
    }

    public void SetArrayValues<TValue>(
        AwkArrayName name,
        IReadOnlyList<TValue> values,
        Func<TValue, AwkVariableSlot> createSlot)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text);
        ThrowIfArrayReplacementWouldExceedLimit(values.Count);
        ReplaceArraySlots(name, CreateIndexedArraySlots(values, firstIndex: 1, createSlot));
    }

    public void SetArrayValuesFromExternalText(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text);
        ThrowIfArrayReplacementWouldExceedLimit(values.Count);
        ReplaceArraySlots(name, CreateIndexedArraySlots(values, firstIndex: 1, AwkVariableSlot.FromExternalText));
    }

    public void SetSplitArrayValues(
        AwkArrayName fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayName separatorArrayName,
        IReadOnlyList<AwkText> separators)
    {
        if (!fieldArrayName.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(fieldArrayName.Text);
        if (!separatorArrayName.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(separatorArrayName.Text);
        ThrowIfArrayReplacementWouldExceedLimit(fields.Count);
        ThrowIfArrayReplacementWouldExceedLimit(separators.Count);
        ReplaceArraySlots(
            fieldArrayName,
            CreateIndexedArraySlots(fields, firstIndex: 1, AwkVariableSlot.FromExternalText),
            separatorArrayName,
            CreateIndexedArraySlots(separators, firstIndex: 0, AwkVariableSlot.FromText));
    }

    public void ClearArray(AwkArrayName name)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text);
        ReplaceArraySlots(name, new Dictionary<AwkArrayKey, AwkVariableSlot>());
    }

    public TResult[] SnapshotArrayValues<TState, TResult>(
        AwkArrayName name,
        TState state,
        Func<TState, AwkArrayKey, AwkValue, TResult> project)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
            return _symbolTable.SnapshotValues(state, project);

        if (!name.IsFunctionLocal && ContainsScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        var slots = GetOrCreateArraySlots(name);
        var snapshot = new TResult[slots.Count];
        var writeIndex = 0;
        foreach (var pair in slots)
            snapshot[writeIndex++] = project(state, pair.Key, CreateValue(pair.Value));

        return snapshot;
    }

    public int GetArraySnapshotCount(AwkArrayName name)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
            return _symbolTable.Count;

        if (!name.IsFunctionLocal && ContainsScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        return GetOrCreateArraySlots(name).Count;
    }

    private Dictionary<AwkArrayKey, AwkVariableSlot> GetOrCreateArraySlots(AwkArrayName name)
    {
        if (_arrays.TryGetSlots(name, out var values))
            return values;

        return _arrays.ResolveSlots(name);
    }

    private static Dictionary<AwkArrayKey, AwkVariableSlot> CreateIndexedArraySlots<TValue>(
        IReadOnlyList<TValue> values,
        int firstIndex,
        Func<TValue, AwkVariableSlot> createSlot)
    {
        var destination = new Dictionary<AwkArrayKey, AwkVariableSlot>(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var key = AwkArrayKey.FromOrdinal(index + firstIndex);
            destination[key] = createSlot(values[index]);
        }

        return destination;
    }

    public void CopyArrayKeys(AwkArrayName name, AwkArrayKey[] destination)
    {
        if (!name.IsFunctionLocal && AwkSymbolTableArray.IsName(name.Text))
        {
            var keys = _symbolTable.SnapshotKeys();
            for (var index = 0; index < keys.Count; index++)
                destination[index] = keys[index];

            return;
        }

        var slots = GetOrCreateArraySlots(name);
        var writeIndex = 0;
        foreach (var key in slots.Keys)
            destination[writeIndex++] = key;
    }

    public void RemoveFunctionLocalArray(AwkArrayName name)
    {
        if (!name.IsFunctionLocal || !_arrays.Remove(name, out var slots))
            return;

        _memoryGovernor.ReleaseCommitted(EstimateArrayBytes(slots));
    }

    private AwkValue CreateValue(AwkVariableSlot slot) =>
        slot.ToValue(_createByteTextValue, _createNumericStringValue);

    private bool TryResolveScalarSlot(AwkVariableName name, out AwkGlobalSlotId slotId) =>
        _scalarSlotsByName.TryGetValue(name, out slotId);

    private bool TryGetScalarSlot(AwkGlobalSlotId slotId, out AwkVariableSlot slot)
    {
        if (slotId.Value >= 0 && slotId.Value < _scalarSlots.Count)
        {
            slot = GetScalarState(slotId).Slot;
            return true;
        }

        slot = default;
        return false;
    }

    private bool TryGetScalarState(AwkVariableName name, out AwkScalarState state)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var id))
        {
            state = GetScalarState(id);
            return true;
        }

        state = default;
        return false;
    }

    private AwkScalarState GetScalarState(AwkGlobalSlotId id) =>
        _scalarSlots[id.Value].State;

    private IEnumerable<AwkScalarInventoryItem> EnumerateScalarInventory()
    {
        for (var index = 0; index < _scalarSlots.Count; index++)
            yield return new AwkScalarInventoryItem(new AwkGlobalSlotId(index), _scalarSlots[index].Name);
    }

    private AwkGlobalSlotId GetOrCreateScalarSlotCore(AwkVariableName name)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var existing))
            return existing;

        var id = new AwkGlobalSlotId(_scalarSlots.Count);
        _scalarSlotsByName[name] = id;
        _scalarSlots.Add(new AwkScalarSlot(
            name,
            AwkScalarState.Ungoverned(new AwkVariableSlot(string.Empty, AwkVariableSlotKind.Unassigned))));
        return id;
    }

    private void SetScalarValueFromSymbolTable(string name, AwkValue value)
    {
        SetScalarValue(AwkVariableName.FromText(name), value);
        _applyScalarAssignment(name, value);
    }

    private void SetScalarSlot(AwkVariableName name, AwkVariableSlot slot)
    {
        SetScalarSlot(GetOrCreateScalarSlotCore(name), slot);
    }

    private void SetScalarSlot(AwkGlobalSlotId id, AwkVariableSlot slot)
    {
        if (!_governScalarMemory)
        {
            _scalarSlots[id.Value] = _scalarSlots[id.Value].WithState(AwkScalarState.Ungoverned(slot));
            return;
        }

        var oldState = GetScalarState(id);
        var oldCharge = oldState.ChargedBytes;
        var baseline = oldState.HasBaseline
            ? oldState.BaselineBytes
            : 0;
        var newCharge = Math.Max(0, EstimateScalarBytes(slot) - baseline);
        using var mutation = ReserveMemoryMutation(newCharge, oldCharge);
        _scalarSlots[id.Value] = _scalarSlots[id.Value].WithState(oldState.WithSlot(slot, newCharge));
        mutation.Commit();
    }

    private void SetArraySlot(
        Dictionary<AwkArrayKey, AwkVariableSlot> values,
        AwkArrayKey index,
        AwkVariableSlot slot)
    {
        var oldBytes = values.TryGetValue(index, out var existingSlot)
            ? EstimateElementBytes(index, existingSlot)
            : 0;
        var newBytes = EstimateElementBytes(index, slot);
        using var mutation = ReserveMemoryMutation(newBytes, oldBytes);
        values[index] = slot;
        mutation.Commit();
    }

    private void ReplaceArraySlots(AwkArrayName name, Dictionary<AwkArrayKey, AwkVariableSlot> slots)
    {
        var oldBytes = ShouldGovernArray(name) && _arrays.TryGetSlots(name, out var existingSlots)
            ? EstimateArrayBytes(existingSlots)
            : 0;
        var newBytes = ShouldGovernArray(name)
            ? EstimateArrayBytes(slots)
            : 0;
        using var mutation = ReserveMemoryMutation(newBytes, oldBytes);
        _arrays.SetSlots(name, slots);
        mutation.Commit();
    }

    private void ReplaceArraySlots(
        AwkArrayName firstName,
        Dictionary<AwkArrayKey, AwkVariableSlot> firstSlots,
        AwkArrayName secondName,
        Dictionary<AwkArrayKey, AwkVariableSlot> secondSlots)
    {
        long ExistingBytes(AwkArrayName name) =>
            ShouldGovernArray(name) && _arrays.TryGetSlots(name, out var slots)
                ? EstimateArrayBytes(slots)
                : 0;

        static long ReplacementBytes(
            AwkArrayName name,
            IReadOnlyDictionary<AwkArrayKey, AwkVariableSlot> slots) =>
            ShouldGovernArray(name) ? EstimateArrayBytes(slots) : 0;

        var oldBytes = AwkRuntimeMemory.CheckedAdd(ExistingBytes(firstName), ExistingBytes(secondName));
        var newBytes = AwkRuntimeMemory.CheckedAdd(
            ReplacementBytes(firstName, firstSlots),
            ReplacementBytes(secondName, secondSlots));
        using var mutation = ReserveMemoryMutation(newBytes, oldBytes);
        _arrays.SetSlots(firstName, firstSlots);
        _arrays.SetSlots(secondName, secondSlots);
        mutation.Commit();
    }

    private AwkMemoryMutationLease ReserveMemoryMutation(long newBytes, long oldBytes) =>
        new(_memoryGovernor, newBytes, oldBytes);

    private struct AwkMemoryMutationLease : IDisposable
    {
        private readonly AwkMemoryGovernor _governor;
        private readonly long _newBytes;
        private readonly long _oldBytes;
        private bool _committed;

        public AwkMemoryMutationLease(AwkMemoryGovernor governor, long newBytes, long oldBytes)
        {
            _governor = governor;
            _newBytes = newBytes;
            _oldBytes = oldBytes;
            _committed = false;

            if (newBytes > oldBytes)
                governor.Reserve(newBytes - oldBytes);
        }

        public void Commit()
        {
            if (_committed)
                return;

            if (_newBytes > _oldBytes)
            {
                _governor.CommitReserved(_newBytes - _oldBytes);
            }
            else if (_oldBytes > _newBytes)
            {
                _governor.ReleaseCommitted(_oldBytes - _newBytes);
            }

            _committed = true;
        }

        public void Dispose()
        {
            if (!_committed && _newBytes > _oldBytes)
                _governor.ReleaseReserved(_newBytes - _oldBytes);
        }
    }

    private static long EstimateArrayBytes(IReadOnlyDictionary<AwkArrayKey, AwkVariableSlot> values)
    {
        var bytes = 0L;
        foreach (var pair in values)
            bytes += EstimateElementBytes(pair.Key, pair.Value);

        return bytes;
    }

    private static long EstimateElementBytes(AwkArrayKey key, AwkVariableSlot slot) =>
        AwkRuntimeMemory.EstimateArrayElementBytes(key, slot);

    private static long EstimateScalarBytes(AwkVariableSlot slot) =>
        AwkRuntimeMemory.EstimateVariableSlotBytes(slot);

    private static bool ShouldGovernArray(AwkArrayName name) =>
        name.IsFunctionLocal || !AwkSpecialArrayPolicy.IsElementReadOnly(name.Text);

    private void ThrowIfArrayWouldGrowPastLimit(Dictionary<AwkArrayKey, AwkVariableSlot> values, AwkArrayKey index)
    {
        if (values.ContainsKey(index))
            return;

        if (values.Count >= _maxArrayElements)
            throw new AwkRuntimeException(AwkMessages.ErrorArrayElementLimit);
    }

    private void ThrowIfArrayReplacementWouldExceedLimit(int count)
    {
        if (count > _maxArrayElements)
            throw new AwkRuntimeException(AwkMessages.ErrorArrayElementLimit);
    }

}
