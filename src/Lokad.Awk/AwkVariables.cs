using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Lokad.Awk;

internal sealed partial class AwkVariables
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

    private struct AwkScalarSlot(AwkVariableName name, AwkScalarState state)
    {
        public AwkVariableName Name { get; } = name;

        public AwkScalarState State = state;
    }

    private struct AwkScalarState(
        AwkVariableSlot slot,
        long baselineBytes,
        long chargedBytes,
        bool hasBaseline)
    {
        public AwkVariableSlot Slot { readonly get; set; } = slot;
        public long BaselineBytes { readonly get; private set; } = baselineBytes;
        public long ChargedBytes { readonly get; private set; } = chargedBytes;
        public bool HasBaseline { readonly get; private set; } = hasBaseline;

        public static AwkScalarState Ungoverned(AwkVariableSlot slot) =>
            new(slot, baselineBytes: 0, chargedBytes: 0, hasBaseline: false);

        public static AwkScalarState Governed(AwkVariableSlot slot, long chargedBytes) =>
            new(slot, baselineBytes: 0, chargedBytes, hasBaseline: true);

        public void SetBaseline(long baselineBytes)
        {
            BaselineBytes = baselineBytes;
            ChargedBytes = 0;
            HasBaseline = true;
        }

        public void SetSlot(AwkVariableSlot slot, long baselineBytes, long chargedBytes)
        {
            Slot = slot;
            BaselineBytes = baselineBytes;
            ChargedBytes = chargedBytes;
            HasBaseline = true;
        }

        public void SetUngovernedSlot(AwkVariableSlot slot)
        {
            Slot = slot;
            BaselineBytes = 0;
            ChargedBytes = 0;
            HasBaseline = false;
        }
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
                var scalarName = GetScalarSlot(slotId).Name;
                slot = resolveFieldCountValue is not null &&
                    string.Equals(scalarName.Text, FieldCountName, StringComparison.Ordinal)
                    ? AwkVariableSlot.FromValue(resolveFieldCountValue())
                    : GetScalarSlot(slotId).State.Slot;
                return true;
            }

            slot = default;
            return false;
        }

        IEnumerable<AwkScalarInventoryItem> EnumerateScalarInventory()
        {
            for (var index = 0; index < _scalarSlots.Count; index++)
                yield return new AwkScalarInventoryItem(new AwkGlobalSlotId(index), CollectionsMarshal.AsSpan(_scalarSlots)[index].Name);
        }

        void SetScalarValueFromSymbolTable(string name, AwkValue value)
        {
            _applyScalarAssignment(name, value);
        }
    }

    public bool ContainsScalar(AwkVariableName name) =>
        (AwkSpecialVariables.TryGetScalar(name, out var special) &&
            AwkCompatibilityNames.EnablesSpecial(special, _mode)) ||
        (_scalarSlotsByName.TryGetValue(name, out var id) && !GetScalarSlot(id).State.Slot.IsUntyped);

    public bool IsSymbolTable(string name) =>
        AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.SymbolTable, _mode) &&
        AwkSymbolTableArray.IsName(name);

    public bool ContainsArray(AwkArrayName name) =>
        !name.IsFunctionLocal && IsSymbolTable(name.Text) ||
        _arrays.Contains(name);

    internal bool TryGetOrdinaryArray(AwkArrayName name,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        if (name.IsFunctionLocal || !AwkSpecialVariables.TryGet(name.Text, out _))
            return _arrays.TryGetArray(name, out array);
        array = null;
        return false;
    }

    internal AwkValue GetArrayValue(AwkArrayIdentity array, AwkArrayKey index)
    {
        ThrowIfDetached(array);
        var slots = array.Slots;
        if (slots.TryGetValue(index, out var slot)) return CreateValue(slot);
        return CreateMissingArrayValue(array, index);
    }

    internal double GetArrayNumber(AwkArrayIdentity array, AwkArrayKey index)
    {
        ThrowIfDetached(array);
        return array.Slots.TryGetValue(index, out var slot)
            ? ReadArraySlotNumber(slot)
            : CreateMissingArrayValue(array, index).AsNumber(_checkpoint);
    }

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, AwkArrayKey index)
    {
        ThrowIfDetached(array);
        if (array.Slots.TryGetValue(index, out var slot))
            return new(index, ReadArraySlotNumber(slot), array, slot);
        var created = CreateMissingArrayCell(array, index);
        return new(index, ReadArraySlotNumber(created), array, created);
    }

    internal double ReadArraySlotNumber(AwkArrayCell cell) => ReadArraySlotNumber(cell.Slot);

    internal double ReadArraySlotNumber(AwkVariableSlot slot)
    {
        if (!slot.TryGetNumber(out var number)) return CreateValue(slot).AsNumber(_checkpoint);
        _counters.RecordNumericArraySlotRead();
        return number;
    }

    private AwkValue CreateMissingArrayValue(AwkArrayIdentity array, AwkArrayKey index) =>
        CreateValue(CreateMissingArrayCell(array, index));

    private AwkArrayCell CreateMissingArrayCell(AwkArrayIdentity array, AwkArrayKey index)
    {
        var slots = array.Slots;
        using var admission = _memoryAccounting.AdmitMutation(EstimateArrayInsertionGrowth(slots,
            AwkRuntimeMemory.EstimateUntypedArrayElementBytes(index) - AwkRuntimeMemory.EstimateArrayBackingBytes(1)));
        var slot = AwkVariableSlot.FromUntypedScalar(new AwkUntypedScalar(array));
        return SetArraySlot(slots, index, slot);
    }

    internal bool TryGetArrayValue(AwkArrayIdentity array, ReadOnlySpan<byte> index, out AwkValue value)
    {
        if (array.Slots.GetAlternateLookup<ReadOnlySpan<byte>>().TryGetValue(index, out var slot))
        {
            value = CreateValue(slot);
            return true;
        }
        value = default;
        return false;
    }

    internal bool TryGetArrayKeySlot(AwkArrayIdentity array, ReadOnlySpan<byte> index,
        out AwkArrayKey key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayCell? slot) =>
        array.Slots.GetAlternateLookup<ReadOnlySpan<byte>>().TryGetValue(index, out key, out slot);

    internal void SetArrayValue(AwkArrayIdentity array, AwkArrayKey index, AwkValue value)
    {
        ThrowIfDetached(array);
        SetArraySlot(array.Slots, index, AwkVariableSlot.FromValue(value));
    }

    internal bool TrySetCapturedArrayNumber(AwkArrayCell? cell, double number)
    {
        if (cell is null || !cell.IsAttached) return false;
        SetArrayCell(cell, AwkVariableSlot.FromNumber(number));
        return true;
    }

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
            var oldState = GetScalarSlot(id).State;
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
            ref var scalar = ref CollectionsMarshal.AsSpan(_scalarSlots)[index];
            scalar.State.SetBaseline(EstimateScalarBytes(scalar.State.Slot));
        }

        _governScalarMemory = true;
    }

    public void InitializeGlobalNames(IReadOnlySet<AwkVariableName> names) => _globalNames = names;

    public AwkValue GetOrCreateUntypedScalarValue(AwkVariableName name)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var id))
            return CreateValue(GetScalarSlot(id).State.Slot);
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

        if (_scalarSlotsByName.TryGetValue(name, out var id))
            return CreateValue(GetScalarSlot(id).State.Slot).ForceScalar();

        SetScalarSlot(name, new AwkVariableSlot(string.Empty, AwkVariableSlotKind.Unassigned));
        return AwkValue.Unassigned();

    }

    public AwkValue GetScalarValue(AwkGlobalSlotId id)
    {
        if (_resolveFieldCountValue is not null &&
            string.Equals(GetScalarSlot(id).Name.Text, FieldCountName, StringComparison.Ordinal))
            return _resolveFieldCountValue();

        return CreateValue(GetScalarSlot(id).State.Slot).ForceScalar();
    }

    public bool TryGetScalarNumber(AwkGlobalSlotId id, out double number)
    {
        if (_resolveFieldCountValue is not null &&
            string.Equals(GetScalarSlot(id).Name.Text, FieldCountName, StringComparison.Ordinal))
        {
            number = 0;
            return false;
        }
        return GetScalarSlot(id).State.Slot.TryGetNumber(out number);
    }

    public void SetScalarValue(AwkGlobalSlotId id, AwkValue value)
    {
        var name = GetScalarSlot(id).Name;
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
            slots[pair.Key] = new(AwkVariableSlot.FromValue(pair.Value));
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
            slots[new AwkArrayKey(keyText, _checkpoint)] = new(AwkVariableSlot.FromExternalText(valueText));
        }

        ReplaceArraySlots(name, slots);
    }

    public void InitializeArrayStringNames(
        AwkArrayName name,
        int count,
        IEnumerable<string> names)
    {
        using var construction = _memoryAccounting.ReserveAllocationPeak(
            AwkRuntimeMemory.EstimateArrayConstructionBytes(BoundArrayCapacity(count), count));
        var slots = CreateArraySlots(count);
        foreach (var value in names)
        {
            var text = AwkText.FromString(value, AwkRuntimeCounters.Disabled, _checkpoint);
            slots[new AwkArrayKey(text, _checkpoint)] = new(AwkVariableSlot.FromText(text));
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
        return CreateMissingArrayValue(_arrays.ResolveArray(name), index);
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
        _ = GetOrCreateArraySlots(name);
        DeleteArrayValue(_arrays.ResolveArray(name), index);
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
        using var construction = _memoryAccounting.AdmitMutation(
            AwkRuntimeMemory.EstimateArrayConstructionBytes(BoundArrayCapacity(values.Count), values.Count));
        ReplaceArraySlots(name, CreateIndexedArraySlots(values, firstIndex: 1, createSlot),
            sourceMemoryLease, sourceRetainedTextBytes);
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
        out AwkArrayKey key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayCell? slot)
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
            AwkRuntimeMemory.EstimateArrayConstructionBytes(BoundArrayCapacity(values.Count), values.Count));
        ReplaceArraySlots(name, CreateIndexedArraySlots(values, firstIndex: 1, AwkVariableSlot.FromExternalText));
    }

    public void SetSplitArrayValues(
        AwkArrayName fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayName separatorArrayName,
        IReadOnlyList<AwkCapturedSeparator> separators) =>
        SetSplitArrayValues(new AwkArrayTarget(fieldArrayName), fields, new AwkArrayTarget(separatorArrayName), separators);

    internal void SetSplitArrayValues(
        AwkArrayTarget fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayTarget separatorArrayName,
        IReadOnlyList<AwkCapturedSeparator> separators)
    {
        ValidateArrayOverlap(fieldArrayName, separatorArrayName, allowSame: false);
        if (fieldArrayName.IsNamed && !fieldArrayName.Name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(fieldArrayName.Name.Text, _mode);
        if (separatorArrayName.IsNamed && !separatorArrayName.Name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(separatorArrayName.Name.Text, _mode);
        using var construction = _memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateArrayConstructionBytes(BoundArrayCapacity(fields.Count), fields.Count),
            AwkRuntimeMemory.EstimateArrayConstructionBytes(BoundArrayCapacity(separators.Count), separators.Count)));
        ReplaceArraySlots(
            fieldArrayName,
            CreateIndexedArraySlots(fields, firstIndex: 1, AwkVariableSlot.FromExternalText),
            separatorArrayName,
            CreateSeparatorArraySlots());

        Dictionary<AwkArrayKey, AwkArrayCell> CreateSeparatorArraySlots()
        {
            var slots = CreateArraySlots(separators.Count);
            foreach (var separator in separators)
                slots[AwkArrayKey.FromOrdinal(separator.FieldIndex)] = new(AwkVariableSlot.FromExternalText(separator.Text));
            return slots;
        }

        void ReplaceArraySlots(
            AwkArrayTarget firstName,
            Dictionary<AwkArrayKey, AwkArrayCell> firstSlots,
            AwkArrayTarget secondName,
            Dictionary<AwkArrayKey, AwkArrayCell> secondSlots)
        {
            long ExistingBytes(AwkArrayTarget name) =>
                TryGetTargetArray(name, out var array)
                    ? EstimateOwnedArrayBytes(array.Slots)
                    : 0;

            var oldBytes = AwkRuntimeMemory.CheckedAdd(ExistingBytes(firstName), ExistingBytes(secondName));
            var newBytes = AwkRuntimeMemory.CheckedAdd(
                EstimateOwnedArrayBytes(firstSlots),
                EstimateOwnedArrayBytes(secondSlots));
            using var mutation = ReserveMemoryMutation(newBytes, oldBytes);
            if (TryGetTargetArray(firstName, out var first)) RetireArrayContents(first, includeRoot: false);
            if (TryGetTargetArray(secondName, out var second)) RetireArrayContents(second, includeRoot: false);
            PublishArraySlots(firstName, firstSlots);
            PublishArraySlots(secondName, secondSlots);
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
        Dictionary<AwkArrayKey, AwkArrayCell> slots,
        AwkMemoryLease sourceMemoryLease,
        long sourceRetainedTextBytes)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _mode);
        ReplaceArraySlots(name, slots, sourceMemoryLease, sourceRetainedTextBytes);

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
        return MeasureArraySnapshot(GetOrCreateArraySlots(name), count, materializeOrdinalKeys, retainValues);
    }

    internal AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayIdentity array, bool materializeOrdinalKeys,
        bool retainValues = false) => MeasureArraySnapshot(array.Slots, array.Slots.Count, materializeOrdinalKeys, retainValues);

    private AwkArraySnapshotSize MeasureArraySnapshot(Dictionary<AwkArrayKey, AwkArrayCell> slots, int count,
        bool materializeOrdinalKeys, bool retainValues)
    {
        if (!_memoryAccounting.IsEnabled)
            return new AwkArraySnapshotSize(count, 0);

        var keyTextBytes = 0L;
        var valueTextBytes = 0L;
        var visited = 0;
        foreach (var pair in slots)
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

    private Dictionary<AwkArrayKey, AwkArrayCell> GetOrCreateArraySlots(AwkArrayName name)
    {
        if (_arrays.TryGetSlots(name, out var values))
            return values;

        using var creation = ReserveMemoryMutation(AwkRuntimeMemory.ArrayIdentityBytes, 0);
        values = _arrays.ResolveSlots(name);
        creation.Commit();
        return values;
    }

    private Dictionary<AwkArrayKey, AwkArrayCell> CreateIndexedArraySlots<TValue>(
        IReadOnlyList<TValue> values,
        int firstIndex,
        Func<TValue, AwkVariableSlot> createSlot)
    {
        var destination = CreateArraySlots(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var key = AwkArrayKey.FromOrdinal(index + firstIndex);
            destination[key] = new(createSlot(values[index]));
        }

        return destination;
    }

    public Dictionary<AwkArrayKey, AwkArrayCell> CreateMutableArraySlots(int capacity) =>
        CreateArraySlots(capacity);

    private Dictionary<AwkArrayKey, AwkArrayCell> CreateArraySlots(int capacity) =>
        new(capacity, _arrayKeyComparer);

    public void CopyArrayKeys(AwkArrayName name, AwkArrayKey[] destination)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            _symbolTable.CopyKeys(destination);
            return;
        }

        var slots = GetOrCreateArraySlots(name);
        CopyArrayKeys(slots, destination);
    }

    internal void CopyArrayKeys(AwkArrayIdentity array, AwkArrayKey[] destination) => CopyArrayKeys(array.Slots, destination);

    private void CopyArrayKeys(Dictionary<AwkArrayKey, AwkArrayCell> slots, AwkArrayKey[] destination)
    {
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
        if (!name.IsFunctionLocal || !_arrays.TryGetArray(name, out var array))
            return;

        var oldBytes = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes, EstimateArrayBytes(array.Slots));
        _arrays.Remove(name, out _);
        RetireArrayContents(array, includeRoot: true, cancellable: false);
        _memoryAccounting.ReleaseCommitted(oldBytes);
    }

    internal AwkValue CreateValue(AwkArrayCell cell) => CreateValue(cell.Slot);

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

    // Use only for immediate access. Admission and OFS preparation may grow the table;
    // publication must reacquire its reference after those callbacks finish.
    private ref AwkScalarSlot GetScalarSlot(AwkGlobalSlotId id) =>
        ref CollectionsMarshal.AsSpan(_scalarSlots)[id.Value];

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
                string.Equals(GetScalarSlot(id).Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal))
                _prepareOutputFieldSeparatorAssignment();
            GetScalarSlot(id).State.SetUngovernedSlot(slot);
            return;
        }

        if (slot.Kind == AwkVariableSlotKind.Number &&
            GetScalarSlot(id).State.Slot.Kind == AwkVariableSlotKind.Number)
        {
            if (string.Equals(GetScalarSlot(id).Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal))
                _prepareOutputFieldSeparatorAssignment();
            GetScalarSlot(id).State.Slot = slot;
            return;
        }

        var oldCharge = GetScalarSlot(id).State.ChargedBytes;
        var baselineBytes = GetScalarSlot(id).State.BaselineBytes;
        var baseline = GetScalarSlot(id).State.HasBaseline
            ? baselineBytes
            : 0;
        var newCharge = Math.Max(0, EstimateScalarBytes(slot) - baseline);
        using var mutation = ReserveMemoryMutation(newCharge, oldCharge);
        if (string.Equals(GetScalarSlot(id).Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal))
            _prepareOutputFieldSeparatorAssignment();
        GetScalarSlot(id).State.SetSlot(slot, baselineBytes, newCharge);
        mutation.Commit();
    }

    private AwkArrayCell SetArraySlot(Dictionary<AwkArrayKey, AwkArrayCell> values,
        AwkArrayKey index, AwkVariableSlot slot)
    {
        if (values.TryGetValue(index, out var existing))
        {
            SetArrayCell(existing, slot);
            return existing;
        }
        if (!_memoryAccounting.IsEnabled) return Insert();
        var oldCapacity = values.EnsureCapacity(0);
        var payload = AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot);
        var growth = values.Count == oldCapacity;
        var target = growth ? PlanArrayCapacity(oldCapacity) : oldCapacity;
        using var admission = growth ? _memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(target)), payload)) : default;
        if (growth) values.EnsureCapacity(target);
        using var mutation = ReserveMemoryMutation(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(values.EnsureCapacity(0)), payload),
            AwkRuntimeMemory.EstimateArrayBackingBytes(oldCapacity));
        _checkpoint?.Invoke();
        var cell = Insert();
        mutation.Commit();
        return cell;

        AwkArrayCell Insert()
        {
            var created = new AwkArrayCell(slot);
            values.Add(index, created);
            return created;
        }
    }

    private void SetArrayCell(AwkArrayCell cell, AwkVariableSlot slot)
    {
        var prior = cell.Slot;
        if (prior.TryGetArrayBinding(out _))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar("array element"));
        if (prior.Kind == AwkVariableSlotKind.Number && slot.Kind == AwkVariableSlotKind.Number)
        {
            cell.SetSlot(slot);
            return;
        }
        // The cell/key/backing are already owned: only the scalar payload changes.
        using var mutation = ReserveMemoryMutation(slot.RetainedTextMemoryBytes, prior.RetainedTextMemoryBytes);
        _checkpoint?.Invoke();
        if (prior.TryGetUntypedScalar(out var origin))
        {
            RepairElementAliases?.Invoke(origin);
            origin.Detach();
        }
        cell.SetSlot(slot);
        mutation.Commit();
    }

    private static long EstimateArrayInsertionGrowth(Dictionary<AwkArrayKey, AwkArrayCell>? slots, long payloadBytes)
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

    private void ReplaceArraySlots(AwkArrayName name, Dictionary<AwkArrayKey, AwkArrayCell> slots) =>
        ReplaceArraySlots(name, slots, AwkMemoryLease.Disabled, transferredBytes: 0);

    private void ReplaceArraySlots(
        AwkArrayName name,
        Dictionary<AwkArrayKey, AwkArrayCell> slots,
        AwkMemoryLease sourceMemoryLease,
        long transferredBytes)
    {
        var oldBytes = 0L;
        if (_arrays.TryGetSlots(name, out var existingSlots))
            oldBytes = EstimateOwnedArrayBytes(existingSlots);
        var newBytes = EstimateOwnedArrayBytes(slots);
        var creditedBytes = AwkRuntimeMemory.CheckedAdd(oldBytes, transferredBytes);
        using var mutation = ReserveMemoryMutation(newBytes, creditedBytes);
        if (_arrays.TryGetArray(name, out var existing)) RetireArrayContents(existing, includeRoot: false);
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

    internal static long EstimateArrayBytes(IReadOnlyDictionary<AwkArrayKey, AwkArrayCell> values,
        Action? checkpoint = null)
    {
        // Only nested arrays need a worklist; script-controlled depth must not recurse
        // through the CLR stack. Its reference metadata is minor beside admitted owners.
        Stack<IReadOnlyDictionary<AwkArrayKey, AwkArrayCell>>? pending = null;
        var bytes = 0L;
        var visited = 0;
        while (true)
        {
            // Synthetic enumeration-only inputs remain useful for checked-total qualification.
            bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                AwkRuntimeMemory.EstimateArrayBackingBytes(values is Dictionary<AwkArrayKey, AwkArrayCell> slots
                    ? slots.EnsureCapacity(0) : values.Count));
            foreach (var pair in values)
            {
                if ((visited++ & 63) == 0) checkpoint?.Invoke();
                bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                    AwkRuntimeMemory.EstimateArrayElementPayloadBytes(pair.Key, pair.Value));
                if (pair.Value.TryGetArrayBinding(out var child))
                {
                    bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.ArrayIdentityBytes);
                    (pending ??= new()).Push(child.Slots);
                }
            }
            if (pending is null || !pending.TryPop(out var next)) break;
            values = next;
        }
        return bytes;
    }

    private static long EstimateElementBytes(AwkArrayKey key, AwkVariableSlot slot) =>
        AwkRuntimeMemory.EstimateArrayElementBytes(key, slot);

    private static long EstimateScalarBytes(AwkVariableSlot slot) =>
        AwkRuntimeMemory.EstimateVariableSlotBytes(slot);

    private long EstimateOwnedArrayBytes(IReadOnlyDictionary<AwkArrayKey, AwkArrayCell> slots) =>
        _memoryAccounting.IsEnabled ? AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes,
            EstimateArrayBytes(slots, _checkpoint)) : 0;

}
