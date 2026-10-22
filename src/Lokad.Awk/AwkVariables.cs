using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkVariables
{
    public const string FunctionTableName = "FUNCTAB";
    public const string SymbolTableName = "SYMTAB";

    private static readonly string FieldCountName = AwkSpecialVariables.Name(AwkSpecialVariableId.FieldCount);
    private static readonly string OutputFieldSeparatorName = AwkSpecialVariables.Name(AwkSpecialVariableId.OutputFieldSeparator);

    private readonly Func<AwkText, AwkValue> _createByteTextValue;
    private readonly Func<AwkText, AwkValue> _createNumericStringValue;
    private readonly Func<AwkText, AwkValue> _createDeferredTextValue;
    private readonly Action<string, AwkValue> _applyScalarAssignment;
    private readonly AwkCompatibilityMode _mode;
    private readonly Action? _checkpoint;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkMemoryAccounting _memoryAccounting;
    private readonly Dictionary<AwkVariableName, AwkGlobalSlotId> _scalarSlotsByName = new();
    private readonly List<AwkScalarSlot> _scalarSlots = [];
    private readonly AwkArrayKeyComparer _arrayKeyComparer;
    private readonly AwkArrayStorage _arrays;
    private readonly AwkSymbolTableArray _symbolTable;
    private IReadOnlySet<AwkVariableName> _globalNames = FrozenSet<AwkVariableName>.Empty;
    private readonly Func<AwkValue>? _resolveFieldCountValue;
    private readonly Action _prepareOutputFieldSeparatorAssignment;
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

        public static AwkScalarState Governed(AwkVariableSlot slot, long chargedBytes) =>
            new(slot, baselineBytes: 0, chargedBytes, hasBaseline: true);

        public AwkScalarState WithBaseline(long baselineBytes) =>
            new(Slot, baselineBytes, chargedBytes: 0, hasBaseline: true);

        public AwkScalarState WithSlot(AwkVariableSlot slot, long chargedBytes) =>
            new(slot, BaselineBytes, chargedBytes, hasBaseline: true);
    }

    public AwkVariables(
        AwkMemoryAccounting memoryAccounting,
        AwkRuntimeCounters counters,
        Func<AwkText, AwkValue> createByteTextValue,
        Func<AwkText, AwkValue> createNumericStringValue,
        Func<AwkText, AwkValue> createDeferredTextValue,
        Action<string, AwkValue> applyScalarAssignment,
        Func<AwkValue>? resolveFieldCountValue = null,
        Action? prepareOutputFieldSeparatorAssignment = null,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {

        _mode = mode;
        _checkpoint = checkpoint;

        _memoryAccounting = memoryAccounting;
        _counters = counters;
        _createByteTextValue = createByteTextValue;
        _createNumericStringValue = createNumericStringValue;
        _createDeferredTextValue = createDeferredTextValue;
        _applyScalarAssignment = applyScalarAssignment;
        _resolveFieldCountValue = resolveFieldCountValue;
        _prepareOutputFieldSeparatorAssignment = prepareOutputFieldSeparatorAssignment ?? (() => { });
        _arrayKeyComparer = AwkArrayKeyComparer.ForRuntime(checkpoint, counters, memoryAccounting);
        _arrays = new AwkArrayStorage(_arrayKeyComparer, counters);
        _symbolTable = new AwkSymbolTableArray(
            () => _globalNames,
            EnumerateScalarInventory,
            TryResolveScalarSlot,
            TryGetScalarSlot,
            _arrays.Contains,
            () => _arrays.Names,
            CreateValue,
            SetScalarValueFromSymbolTable,
            checkpoint);

        bool TryResolveScalarSlot(AwkVariableName name, out AwkGlobalSlotId slotId) =>
            _scalarSlotsByName.TryGetValue(name, out slotId);

        bool TryGetScalarSlot(AwkGlobalSlotId slotId, out AwkVariableSlot slot)
        {
            if (slotId.Value >= 0 && slotId.Value < _scalarSlots.Count)
            {
                var scalar = _scalarSlots[slotId.Value];
                slot = resolveFieldCountValue is not null &&
                    string.Equals(scalar.Name.Text, FieldCountName, StringComparison.Ordinal)
                    ? AwkVariableSlot.FromValue(resolveFieldCountValue())
                    : GetScalarState(slotId).Slot;
                return true;
            }

            slot = default;
            return false;
        }

        IEnumerable<AwkScalarInventoryItem> EnumerateScalarInventory()
        {
            for (var index = 0; index < _scalarSlots.Count; index++)
                yield return new AwkScalarInventoryItem(new AwkGlobalSlotId(index), _scalarSlots[index].Name);
        }

        void SetScalarValueFromSymbolTable(string name, AwkValue value)
        {
            _applyScalarAssignment(name, value);
        }
    }

    public bool ContainsScalar(AwkVariableName name) =>
        (AwkSpecialVariables.TryGetScalar(name, out var special) &&
            AwkCompatibilityNames.EnablesSpecial(special, _mode)) ||
        (_scalarSlotsByName.TryGetValue(name, out var id) && !GetScalarState(id).Slot.IsUntyped);

    public bool IsSymbolTable(string name) =>
        AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.SymbolTable, _mode) &&
        AwkSymbolTableArray.IsName(name);

    public bool ContainsArray(AwkArrayName name) =>
        !name.IsFunctionLocal && IsSymbolTable(name.Text) ||
        _arrays.Contains(name);

    public bool TryGetArrayCount(AwkArrayName name, out int count)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
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
        if (IsComputedFieldCount(name))
        {
            _applyScalarAssignment(name.Text, AwkValue.FromText(value, AwkNumberParsingMode.Decimal, _checkpoint));
            return;
        }
        SetScalarSlot(name, AwkVariableSlot.FromText(value));
    }

    public void SetScalarValue(AwkVariableName name, AwkValue value)
    {
        if (IsComputedFieldCount(name))
        {
            _applyScalarAssignment(name.Text, value);
            return;
        }
        SetScalarSlot(name, AwkVariableSlot.FromValue(value));
    }

    private bool IsComputedFieldCount(AwkVariableName name) =>
        _governScalarMemory && _resolveFieldCountValue is not null &&
        string.Equals(name.Text, FieldCountName, StringComparison.Ordinal);

    public long EstimateScalarValueMemoryGrowth(AwkVariableName name, AwkValue value)
    {
        var slot = AwkVariableSlot.FromValue(value);
        if (_scalarSlotsByName.TryGetValue(name, out var id))
        {
            if (!_governScalarMemory)
                return 0;
            var oldState = GetScalarState(id);
            var baseline = oldState.HasBaseline ? oldState.BaselineBytes : 0;
            var newCharge = Math.Max(0, EstimateScalarBytes(slot) - baseline);
            return Math.Max(0, newCharge - oldState.ChargedBytes);
        }
        return _governScalarMemory
            ? AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateSymbolMetadataBytes(name.Text.Length), EstimateScalarBytes(slot))
            : 0;
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

    public void InitializeGlobalNames(IReadOnlySet<AwkVariableName> names) => _globalNames = names;

    public AwkValue GetOrCreateUntypedScalarValue(AwkVariableName name)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var id))
            return CreateValue(GetScalarState(id).Slot);
        using var admission = _governScalarMemory
            ? _memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateSymbolMetadataBytes(name.Text.Length),
                AwkRuntimeMemory.CheckedAdd(24, AwkRuntimeMemory.EstimateUntypedScalarBytes())))
            : default;
        var slot = AwkVariableSlot.FromUntypedScalar(new AwkUntypedScalar());
        SetScalarSlot(name, slot);
        return CreateValue(slot);
    }

    public AwkValue GetOrCreateScalarValue(AwkVariableName name)
    {
        if (_resolveFieldCountValue is not null &&
            string.Equals(name.Text, FieldCountName, StringComparison.Ordinal))
            return _resolveFieldCountValue();

        if (TryGetScalarState(name, out var state))
            return CreateValue(state.Slot).ForceScalar();

        SetScalarSlot(name, new AwkVariableSlot(string.Empty, AwkVariableSlotKind.Unassigned));
        return AwkValue.Unassigned();

        bool TryGetScalarState(AwkVariableName name, out AwkScalarState state)
        {
            if (_scalarSlotsByName.TryGetValue(name, out var id))
            {
                state = GetScalarState(id);
                return true;
            }

            state = default;
            return false;
        }
    }

    public AwkValue GetScalarValue(AwkGlobalSlotId id)
    {
        if (_resolveFieldCountValue is not null &&
            string.Equals(_scalarSlots[id.Value].Name.Text, FieldCountName, StringComparison.Ordinal))
            return _resolveFieldCountValue();

        return CreateValue(GetScalarState(id).Slot).ForceScalar();
    }

    public bool TryGetScalarNumber(AwkGlobalSlotId id, out double number)
    {
        if (_resolveFieldCountValue is not null &&
            string.Equals(_scalarSlots[id.Value].Name.Text, FieldCountName, StringComparison.Ordinal))
        {
            number = 0;
            return false;
        }
        return GetScalarState(id).Slot.TryGetNumber(out number);
    }

    public void SetScalarValue(AwkGlobalSlotId id, AwkValue value)
    {
        var name = _scalarSlots[id.Value].Name;
        if (_resolveFieldCountValue is not null &&
            string.Equals(name.Text, FieldCountName, StringComparison.Ordinal))
        {
            _applyScalarAssignment(name.Text, value);
            return;
        }

        SetScalarSlot(id, AwkVariableSlot.FromValue(value));
    }

    public AwkGlobalSlotId GetOrCreateScalarSlot(AwkVariableName name) =>
        GetOrCreateScalarSlotCore(name);

    public void InitializeArrayValues(AwkArrayName name, IReadOnlyList<KeyValuePair<AwkArrayKey, AwkValue>> values)
    {
        var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(values.Count);
        foreach (var pair in values)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                EstimateElementBytes(pair.Key, AwkVariableSlot.FromValue(pair.Value)));
        }
        using var allocationPeak = _memoryAccounting.ReserveAllocationPeak(bytes);
        var slots = CreateArraySlots(values.Count);
        foreach (var pair in values)
            slots[pair.Key] = AwkVariableSlot.FromValue(pair.Value);
        ReplaceArraySlots(name, slots);
    }

    public void InitializeIndexedArrayExternalStringValues(
        AwkArrayName name,
        IReadOnlyList<string> values,
        int firstIndex)
    {
        var prospectiveBytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var valueBytes = Encoding.UTF8.GetByteCount(values[index]);
            prospectiveBytes = AwkRuntimeMemory.CheckedAdd(
                prospectiveBytes,
                AwkRuntimeMemory.EstimateExternalArrayElementBytes(0, valueBytes));
        }

        using var allocationPeak = _memoryAccounting.ReserveAllocationPeak(prospectiveBytes);
        ReplaceArraySlots(
            name,
            CreateIndexedArraySlots(values, firstIndex, AwkVariableSlot.FromExternalText));
    }

    public void InitializeArrayExternalStringValues<TValue>(
        AwkArrayName name,
        IReadOnlyList<TValue> values,
        Func<TValue, string> selectKey,
        Func<TValue, string> selectValue)
    {
        var prospectiveBytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(values.Count);
        using var allocationPeak = _memoryAccounting.ReserveAllocationPeak(prospectiveBytes);
        var slots = CreateArraySlots(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            var keyText = selectKey(value);
            var valueText = selectValue(value);
            var keyBytes = Encoding.UTF8.GetByteCount(keyText);
            var valueBytes = Encoding.UTF8.GetByteCount(valueText);
            prospectiveBytes = AwkRuntimeMemory.CheckedAdd(
                prospectiveBytes,
                AwkRuntimeMemory.EstimateExternalArrayElementBytes(keyBytes, valueBytes));
            allocationPeak.Resize(prospectiveBytes);
            slots[new AwkArrayKey(keyText, _checkpoint)] = AwkVariableSlot.FromExternalText(valueText);
        }

        ReplaceArraySlots(name, slots);
    }

    public void InitializeArrayStringNames(
        AwkArrayName name,
        int count,
        IEnumerable<string> names)
    {
        using var construction = _memoryAccounting.ReserveAllocationPeak(
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(count)));
        var slots = CreateArraySlots(count);
        foreach (var value in names)
        {
            var text = AwkText.FromString(value, AwkRuntimeCounters.Disabled, _checkpoint);
            slots[new AwkArrayKey(text, _checkpoint)] = AwkVariableSlot.FromText(text);
        }

        ReplaceArraySlots(name, slots);
    }

    public AwkValue GetArrayValue(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _symbolTable.GetValue(index.AsString(_counters, _checkpoint));

        var values = GetOrCreateArraySlots(name);
        if (values.TryGetValue(index, out var slot))
            return CreateValue(slot);

        if (!name.IsFunctionLocal && AwkSpecialArrayPolicy.IsElementReadOnly(name.Text, _mode))
            throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(name.Text, index.AsString(_counters, _checkpoint)));
        using var admission = _memoryAccounting.AdmitMutation(EstimateArrayInsertionGrowth(values,
            AwkRuntimeMemory.EstimateUntypedArrayElementBytes(index) - AwkRuntimeMemory.EstimateArrayBackingBytes(1)));
        var untypedSlot = AwkVariableSlot.FromUntypedScalar(new AwkUntypedScalar());
        SetArraySlot(values, index, untypedSlot);
        return CreateValue(untypedSlot);
    }

    public bool TryPeekArrayValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            value = _symbolTable.GetValue(index.AsString(_counters, _checkpoint));
            return true;
        }

        if (!name.IsFunctionLocal && ContainsScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        var values = GetOrCreateArraySlots(name);
        if (!values.TryGetValue(index, out var slot))
        {
            if (!name.IsFunctionLocal && AwkSpecialArrayPolicy.IsElementReadOnly(name.Text, _mode))
                throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(name.Text, index.AsString(_counters, _checkpoint)));
            value = AwkValue.FromText(string.Empty);
            return false;
        }

        value = CreateValue(slot);
        return true;
    }

    public bool ContainsArrayValue(AwkArrayName name, AwkArrayKey index) =>
        !name.IsFunctionLocal && IsSymbolTable(name.Text)
            ? _symbolTable.ContainsValue(index.AsString(_counters, _checkpoint))
            : GetOrCreateArraySlots(name).ContainsKey(index);

    public bool ContainsArrayValue(AwkArrayName name, ReadOnlySpan<byte> index) =>
        !name.IsFunctionLocal && IsSymbolTable(name.Text)
            ? _symbolTable.ContainsValue(AwkUtf8Codec.DecodeRuntimeText(index, _counters))
            : GetOrCreateArraySlots(name).GetAlternateLookup<ReadOnlySpan<byte>>().ContainsKey(index);

    public void SetArrayString(AwkArrayName name, AwkArrayKey index, string value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            _symbolTable.AssignValue(
                index.AsString(_counters, _checkpoint),
                _createByteTextValue(AwkText.FromString(value, _counters, _memoryAccounting)));
            return;
        }

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        var values = GetOrCreateArraySlots(name);
        SetArraySlot(values, index, AwkVariableSlot.FromText(value));
    }

    public void SetArrayValue(AwkArrayName name, AwkArrayKey index, AwkValue value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            _symbolTable.AssignValue(index.AsString(_counters, _checkpoint), value);
            return;
        }

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        var values = GetOrCreateArraySlots(name);
        SetArraySlot(values, index, AwkVariableSlot.FromValue(value));
    }

    public long EstimateArrayValueMemoryGrowth(AwkArrayName name, AwkArrayKey index, AwkValue value)
    {
        AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        var slot = AwkVariableSlot.FromValue(value);
        if (_arrays.TryGetSlots(name, out var slots) && slots.TryGetValue(index, out var oldSlot))
            return Math.Max(0, AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot) -
                AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, oldSlot));
        return EstimateArrayInsertionGrowth(slots, AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot));
    }

    public void DeleteArrayValue(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            throw new AwkRuntimeException(AwkMessages.ErrorCannotDeleteSymtab);

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        var values = GetOrCreateArraySlots(name);
        // Equal ordinal/text keys can retain different storage. Refund the key actually stored.
        if (values.GetAlternateLookup<AwkArrayKey>().TryGetValue(index, out var storedKey, out var slot) && values.Remove(index))
            _memoryAccounting.ReleaseCommitted(AwkRuntimeMemory.EstimateArrayElementPayloadBytes(storedKey, slot));
    }

    public void SetArrayValues<TValue>(
        AwkArrayName name,
        IReadOnlyList<TValue> values,
        Func<TValue, AwkVariableSlot> createSlot,
        AwkMemoryLease sourceMemoryLease,
        long sourceRetainedTextBytes)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _mode);
        var transferredBytes = ShouldGovernArray(name) ? sourceRetainedTextBytes : 0;
        using var construction = _memoryAccounting.AdmitMutation(
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(values.Count)));
        ReplaceArraySlots(name, CreateIndexedArraySlots(values, firstIndex: 1, createSlot),
            sourceMemoryLease, transferredBytes);
    }

    public bool TryGetExistingArrayValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            value = _symbolTable.GetValue(index.AsString(_counters, _checkpoint));
            return true;
        }
        if (_arrays.TryGetSlots(name, out var slots) && slots.TryGetValue(index, out var slot))
        {
            value = CreateValue(slot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetExistingArrayValue(AwkArrayName name, ReadOnlySpan<byte> index, out AwkValue value)
    {
        // A miss must leave array creation/promotion and admission to the owned-key path.
        if ((name.IsFunctionLocal || !IsSymbolTable(name.Text)) &&
            _arrays.TryGetSlots(name, out var slots) &&
            slots.GetAlternateLookup<ReadOnlySpan<byte>>().TryGetValue(index, out var slot))
        {
            value = CreateValue(slot);
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetExistingArrayKey(AwkArrayName name, ReadOnlySpan<byte> index, out AwkArrayKey key) =>
        TryGetExistingArraySlot(name, index, out key, out _);

    public bool TryGetExistingArrayKeyValue(AwkArrayName name, ReadOnlySpan<byte> index,
        out AwkArrayKey key, out AwkValue value)
    {
        if (TryGetExistingArraySlot(name, index, out key, out var slot))
        {
            value = CreateValue(slot);
            return true;
        }
        value = default;
        return false;
    }

    private bool TryGetExistingArraySlot(AwkArrayName name, ReadOnlySpan<byte> index,
        out AwkArrayKey key, out AwkVariableSlot slot)
    {
        if ((name.IsFunctionLocal || !IsSymbolTable(name.Text)) &&
            _arrays.TryGetSlots(name, out var slots) &&
            slots.GetAlternateLookup<ReadOnlySpan<byte>>().TryGetValue(index, out key, out slot))
            return true;
        key = default;
        slot = default;
        return false;
    }

    public AwkRuntimeValueKind GetArrayElementRuntimeKind(AwkArrayName name, AwkArrayKey index) =>
        !name.IsFunctionLocal && IsSymbolTable(name.Text)
            ? _symbolTable.GetElementRuntimeKind(index.AsString(_counters, _checkpoint))
            : GetArrayValue(name, index).RuntimeKind;

    public AwkMemoryMutationAdmission PreflightArrayElementRead(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal &&
            (IsSymbolTable(name.Text) || AwkSpecialArrayPolicy.IsElementReadOnly(name.Text, _mode)))
            return default;

        if (_arrays.TryGetSlots(name, out var slots))
        {
            if (slots.ContainsKey(index))
                return default;
        }
        return _memoryAccounting.AdmitMutation(EstimateArrayInsertionGrowth(slots,
            AwkRuntimeMemory.EstimateUntypedArrayElementBytes(index) - AwkRuntimeMemory.EstimateArrayBackingBytes(1)));
    }

    public void SetArrayValuesFromExternalText(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _mode);
        using var construction = _memoryAccounting.AdmitMutation(
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(values.Count)));
        ReplaceArraySlots(name, CreateIndexedArraySlots(values, firstIndex: 1, AwkVariableSlot.FromExternalText));
    }

    public void SetSplitArrayValues(
        AwkArrayName fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayName separatorArrayName,
        IReadOnlyList<AwkCapturedSeparator> separators)
    {
        if (!fieldArrayName.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(fieldArrayName.Text, _mode);
        if (!separatorArrayName.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(separatorArrayName.Text, _mode);
        using var construction = _memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(fields.Count)),
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(separators.Count))));
        ReplaceArraySlots(
            fieldArrayName,
            CreateIndexedArraySlots(fields, firstIndex: 1, AwkVariableSlot.FromExternalText),
            separatorArrayName,
            CreateSeparatorArraySlots());

        Dictionary<AwkArrayKey, AwkVariableSlot> CreateSeparatorArraySlots()
        {
            var slots = CreateArraySlots(separators.Count);
            foreach (var separator in separators)
                slots[AwkArrayKey.FromOrdinal(separator.FieldIndex)] = AwkVariableSlot.FromExternalText(separator.Text);
            return slots;
        }

        void ReplaceArraySlots(
            AwkArrayName firstName,
            Dictionary<AwkArrayKey, AwkVariableSlot> firstSlots,
            AwkArrayName secondName,
            Dictionary<AwkArrayKey, AwkVariableSlot> secondSlots)
        {
            long ExistingBytes(AwkArrayName name) =>
                ShouldGovernArray(name) && _arrays.TryGetSlots(name, out var slots)
                    ? EstimateArrayBytes(slots)
                    : 0;

            long ReplacementBytes(
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
    }

    public void ClearArray(AwkArrayName name)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _mode);
        ReplaceArraySlots(name, CreateArraySlots(0));
    }

    public void ReplaceMutableArraySlots(
        AwkArrayName name,
        Dictionary<AwkArrayKey, AwkVariableSlot> slots,
        AwkMemoryLease sourceMemoryLease,
        long sourceRetainedTextBytes)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _mode);
        var transferredBytes = ShouldGovernArray(name) ? sourceRetainedTextBytes : 0;
        ReplaceArraySlots(name, slots, sourceMemoryLease, transferredBytes);

    }

    public TResult[] SnapshotArrayValues<TState, TResult>(
        AwkArrayName name,
        int count,
        TState state,
        Func<TState, AwkArrayKey, AwkValue, TResult> project)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _symbolTable.SnapshotValues(count, state, project);

        if (!name.IsFunctionLocal && ContainsScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        var slots = GetOrCreateArraySlots(name);
        var snapshot = new TResult[count];
        var writeIndex = 0;
        foreach (var pair in slots)
        {
            if ((writeIndex & 63) == 0)
                _checkpoint?.Invoke();
            snapshot[writeIndex++] = project(state, pair.Key, CreateValue(pair.Value));
        }
        _checkpoint?.Invoke();

        return snapshot;
    }

    public int GetArraySnapshotCount(AwkArrayName name)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _symbolTable.Count;

        if (!name.IsFunctionLocal && ContainsScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        return GetOrCreateArraySlots(name).Count;
    }

    public AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayName name, bool materializeOrdinalKeys, bool retainValues = false)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _memoryAccounting.IsEnabled
                ? _symbolTable.MeasureSnapshot(retainValues)
                : new AwkArraySnapshotSize(_symbolTable.Count, 0);

        var count = GetArraySnapshotCount(name);
        if (!_memoryAccounting.IsEnabled)
            return new AwkArraySnapshotSize(count, 0);

        var keyTextBytes = 0L;
        var valueTextBytes = 0L;
        var visited = 0;
        foreach (var pair in GetOrCreateArraySlots(name))
        {
            if ((visited++ & 63) == 0)
                _checkpoint?.Invoke();
            var key = pair.Key;
            keyTextBytes = AwkRuntimeMemory.CheckedAdd(keyTextBytes,
                key.IsOrdinal && materializeOrdinalKeys
                    ? AwkRuntimeMemory.EstimateTextBytes(key.ByteLength) : key.RetainedTextMemoryBytes);
            if (retainValues)
                valueTextBytes = AwkRuntimeMemory.CheckedAdd(valueTextBytes, pair.Value.RetainedTextMemoryBytes);
        }
        _checkpoint?.Invoke();
        return new AwkArraySnapshotSize(count, keyTextBytes, valueTextBytes);
    }

    private Dictionary<AwkArrayKey, AwkVariableSlot> GetOrCreateArraySlots(AwkArrayName name)
    {
        if (_arrays.TryGetSlots(name, out var values))
            return values;

        return _arrays.ResolveSlots(name);
    }

    private Dictionary<AwkArrayKey, AwkVariableSlot> CreateIndexedArraySlots<TValue>(
        IReadOnlyList<TValue> values,
        int firstIndex,
        Func<TValue, AwkVariableSlot> createSlot)
    {
        var destination = CreateArraySlots(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var key = AwkArrayKey.FromOrdinal(index + firstIndex);
            destination[key] = createSlot(values[index]);
        }

        return destination;
    }

    public Dictionary<AwkArrayKey, AwkVariableSlot> CreateMutableArraySlots(int capacity) =>
        CreateArraySlots(capacity);

    private Dictionary<AwkArrayKey, AwkVariableSlot> CreateArraySlots(int capacity) =>
        new(capacity, _arrayKeyComparer);

    public void CopyArrayKeys(AwkArrayName name, AwkArrayKey[] destination)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            _symbolTable.CopyKeys(destination);
            return;
        }

        var slots = GetOrCreateArraySlots(name);
        var writeIndex = 0;
        foreach (var key in slots.Keys)
        {
            if ((writeIndex & 63) == 0)
                _checkpoint?.Invoke();
            destination[writeIndex++] = key;
        }
        _checkpoint?.Invoke();
    }

    public void RemoveFunctionLocalArray(AwkArrayName name)
    {
        if (!name.IsFunctionLocal || !_arrays.Remove(name, out var slots))
            return;

        _memoryAccounting.ReleaseCommitted(EstimateArrayBytes(slots));
    }

    internal AwkValue CreateValue(AwkVariableSlot slot) =>
        slot.Kind == AwkVariableSlotKind.PreparedNumericString
            ? CreatePreparedNumericInput(slot)
            : slot.ToValue(_createByteTextValue, _createNumericStringValue, _createDeferredTextValue);

    private AwkValue CreatePreparedNumericInput(AwkVariableSlot slot)
    {
        var value = slot.GetPreparedNumericInput();
        var byteLength = value.ByteText.ByteLength;
        _counters.RecordValueCreated();
        if (byteLength >= AwkRuntimeLimits.CancellationCheckBytes)
            _checkpoint?.Invoke();
        _counters.RecordPreparedNumericSlotRead();
        return value;
    }

    private AwkScalarState GetScalarState(AwkGlobalSlotId id) =>
        _scalarSlots[id.Value].State;

    private AwkGlobalSlotId GetOrCreateScalarSlotCore(AwkVariableName name)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var existing))
            return existing;

        return AddScalarSlot(name, new AwkVariableSlot(string.Empty, AwkVariableSlotKind.Unassigned));
    }

    private AwkGlobalSlotId AddScalarSlot(AwkVariableName name, AwkVariableSlot slot)
    {

        var id = new AwkGlobalSlotId(_scalarSlots.Count);
        var slotBytes = _governScalarMemory ? EstimateScalarBytes(slot) : 0;
        var metadataBytes = _governScalarMemory
            ? AwkRuntimeMemory.EstimateSymbolMetadataBytes(name.Text.Length)
            : 0;
        using var mutation = ReserveMemoryMutation(AwkRuntimeMemory.CheckedAdd(metadataBytes, slotBytes), 0);
        _scalarSlotsByName.Add(name, id);
        _scalarSlots.Add(new AwkScalarSlot(
            name,
            _governScalarMemory
                ? AwkScalarState.Governed(slot, slotBytes)
                : AwkScalarState.Ungoverned(slot)));
        mutation.Commit();
        return id;
    }

    private void SetScalarSlot(AwkVariableName name, AwkVariableSlot slot)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var existing))
        {
            SetScalarSlot(existing, slot);
            return;
        }

        AddScalarSlot(name, slot);
    }

    private void SetScalarSlot(AwkGlobalSlotId id, AwkVariableSlot slot)
    {
        if (!_governScalarMemory || !_memoryAccounting.IsEnabled)
        {
            if (_governScalarMemory &&
                string.Equals(_scalarSlots[id.Value].Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal))
                _prepareOutputFieldSeparatorAssignment();
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
        if (string.Equals(_scalarSlots[id.Value].Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal))
            _prepareOutputFieldSeparatorAssignment();
        _scalarSlots[id.Value] = _scalarSlots[id.Value].WithState(oldState.WithSlot(slot, newCharge));
        mutation.Commit();
    }

    private void SetArraySlot(
        Dictionary<AwkArrayKey, AwkVariableSlot> values,
        AwkArrayKey index,
        AwkVariableSlot slot)
    {
        if (!_memoryAccounting.IsEnabled)
        {
            values[index] = slot;
            return;
        }

        var existed = values.GetAlternateLookup<AwkArrayKey>().TryGetValue(index, out var retainedKey, out var existingSlot);
        if (existed) index = retainedKey;
        var oldCapacity = values.EnsureCapacity(0);
        var oldPayload = existed ? AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, existingSlot) : 0;
        var newPayload = AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot);
        var growth = !existed && values.Count == oldCapacity;
        // Admit old/new table overlap before EnsureCapacity allocates. A generous prime-rounding
        // bound keeps sizing local; actual retained capacity comes from the dictionary itself.
        var target = growth ? PlanArrayCapacity(oldCapacity) : oldCapacity;
        using var admission = growth ? _memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(target)), Math.Max(0, newPayload - oldPayload))) : default;
        if (growth) values.EnsureCapacity(target);
        var newCapacity = values.EnsureCapacity(0);
        using var mutation = ReserveMemoryMutation(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(newCapacity), newPayload),
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(oldCapacity), oldPayload));
        values[index] = slot;
        mutation.Commit();
    }

    private static long EstimateArrayInsertionGrowth(Dictionary<AwkArrayKey, AwkVariableSlot>? slots, long payloadBytes)
    {
        var capacity = slots?.EnsureCapacity(0) ?? 0;
        return (slots?.Count ?? 0) == capacity ? AwkRuntimeMemory.CheckedAdd(payloadBytes,
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(PlanArrayCapacity(capacity)))) : payloadBytes;
    }

    private static int PlanArrayCapacity(int capacity)
    {
        if (capacity >= Array.MaxLength) throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        return (int)Math.Min(Array.MaxLength, Math.Max(3, (long)capacity * 2));
    }

    private static int BoundArrayCapacity(int requested) =>
        requested == 0 ? 0 : (int)Math.Min(Array.MaxLength, Math.Max(3, (long)requested * 2));

    private void ReplaceArraySlots(AwkArrayName name, Dictionary<AwkArrayKey, AwkVariableSlot> slots) =>
        ReplaceArraySlots(name, slots, AwkMemoryLease.Disabled, transferredBytes: 0);

    private void ReplaceArraySlots(
        AwkArrayName name,
        Dictionary<AwkArrayKey, AwkVariableSlot> slots,
        AwkMemoryLease sourceMemoryLease,
        long transferredBytes)
    {
        var oldBytes = ShouldGovernArray(name) && _arrays.TryGetSlots(name, out var existingSlots)
            ? EstimateArrayBytes(existingSlots)
            : 0;
        var newBytes = ShouldGovernArray(name)
            ? EstimateArrayBytes(slots)
            : 0;
        var creditedBytes = AwkRuntimeMemory.CheckedAdd(oldBytes, transferredBytes);
        using var mutation = ReserveMemoryMutation(newBytes, creditedBytes);
        _arrays.SetSlots(name, slots);
        mutation.Commit();
        sourceMemoryLease.Transfer(transferredBytes);
    }

    private AwkMemoryMutationLease ReserveMemoryMutation(long newBytes, long oldBytes) =>
        new(_memoryAccounting, newBytes, oldBytes);

    private struct AwkMemoryMutationLease : IDisposable
    {
        private readonly AwkMemoryAccounting _governor;
        private readonly long _newBytes;
        private readonly long _oldBytes;
        private bool _committed;

        public AwkMemoryMutationLease(AwkMemoryAccounting governor, long newBytes, long oldBytes)
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

    internal static long EstimateArrayBytes(IReadOnlyDictionary<AwkArrayKey, AwkVariableSlot> values)
    {
        // Synthetic enumeration-only inputs remain useful for checked-total qualification.
        var bytes = AwkRuntimeMemory.EstimateArrayBackingBytes(values is Dictionary<AwkArrayKey, AwkVariableSlot> slots
            ? slots.EnsureCapacity(0) : values.Count);
        foreach (var pair in values)
            bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                AwkRuntimeMemory.EstimateArrayElementPayloadBytes(pair.Key, pair.Value));
        return bytes;
    }

    private static long EstimateElementBytes(AwkArrayKey key, AwkVariableSlot slot) =>
        AwkRuntimeMemory.EstimateArrayElementBytes(key, slot);

    private static long EstimateScalarBytes(AwkVariableSlot slot) =>
        AwkRuntimeMemory.EstimateVariableSlotBytes(slot);

    private bool ShouldGovernArray(AwkArrayName name) =>
        name.IsFunctionLocal || !AwkSpecialArrayPolicy.IsElementReadOnly(name.Text, _mode);

}
