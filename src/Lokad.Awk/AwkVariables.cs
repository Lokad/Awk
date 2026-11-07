using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal sealed partial class AwkVariables
{
    public const string FunctionTableName = "FUNCTAB";
    public const string SymbolTableName = "SYMTAB";

    private static readonly string FieldCountName = AwkSpecialVariables.Name(AwkSpecialVariableId.FieldCount);
    private static readonly string OutputFieldSeparatorName = AwkSpecialVariables.Name(AwkSpecialVariableId.OutputFieldSeparator);

    private readonly Func<AwkText, AwkValue> _createNumericStringValue;
    private readonly Func<AwkText, AwkValue> _createStringValue;
    private readonly Action<string, AwkValue> _applyScalarAssignment;
    private readonly AwkCompatibilityMode _mode;
    private readonly Action? _checkpoint;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkMemoryAccounting _memoryAccounting;
    private readonly Dictionary<AwkVariableName, AwkGlobalSlotId> _scalarSlotsByName = new();
    private readonly List<AwkScalarCell> _scalarSlots = [];
    private readonly AwkScalarCell?[] _specialScalarCells;
    private readonly AwkArrayKeyComparer _arrayKeyComparer;
    private readonly AwkArrayStorage _arrays;
    private readonly AwkSymbolTableArray _symbolTable;
    private IReadOnlySet<AwkVariableName> _globalNames = FrozenSet<AwkVariableName>.Empty;
    private readonly Func<AwkValue>? _resolveFieldCountValue;
    private readonly Action _prepareOutputFieldSeparatorAssignment;
    private bool _governScalarMemory;

    internal sealed class AwkScalarCell(AwkVariableName name, AwkScalarState state)
    {
        public AwkVariableName Name { get; } = name;

        public AwkScalarState State = state;
    }

    internal struct AwkScalarState(
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
        Func<AwkText, AwkValue> createNumericStringValue,
        Func<AwkText, AwkValue> createStringValue,
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
        _createNumericStringValue = createNumericStringValue;
        _createStringValue = createStringValue;
        _applyScalarAssignment = applyScalarAssignment;
        _resolveFieldCountValue = resolveFieldCountValue;
        _prepareOutputFieldSeparatorAssignment = prepareOutputFieldSeparatorAssignment ?? (() => { });
        // Scalar IDs precede arrays. These are references to real owners, not a
        // second value store; absent and disabled names keep ordinary resolution.
        using (var bindingMemory = ReserveMemoryMutation(AwkRuntimeMemory.EstimateSpecialScalarBindingsBytes(), 0))
        {
            if (_memoryAccounting.IsEnabled) _checkpoint?.Invoke();
            _specialScalarCells = new AwkScalarCell?[(int)AwkSpecialVariableId.ArgumentVector];
            bindingMemory.Commit();
        }
        _arrayKeyComparer = AwkArrayKeyComparer.ForRuntime(checkpoint, counters);
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
                var scalarName = GetScalarCell(slotId).Name;
                slot = resolveFieldCountValue is not null &&
                    string.Equals(scalarName.Text, FieldCountName, StringComparison.Ordinal)
                    ? AwkVariableSlot.FromValue(resolveFieldCountValue())
                    : GetScalarCell(slotId).State.Slot;
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
        (_scalarSlotsByName.TryGetValue(name, out var id) && !GetScalarCell(id).State.Slot.IsUntyped);

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

    // The caller's synchronous borrowed lookup proved this key absent. Admit the
    // retained key and insertion together; no field scalar or cache entry is needed.
    internal void InsertArrayValue(AwkArrayIdentity array, ReadOnlySpan<byte> index, AwkValue value)
    {
        ThrowIfDetached(array);
        var slot = AwkVariableSlot.FromValue(value);
        var slots = array.Slots;
        var oldCapacity = _memoryAccounting.IsEnabled ? slots.EnsureCapacity(0) : 0;
        using var admission = _memoryAccounting.AdmitMutation(_memoryAccounting.IsEnabled
            ? EstimateArrayInsertionGrowth(slots, AwkRuntimeMemory.CheckedAdd(
                AwkArrayKey.EstimateRetainedTextBytes(index.Length),
                AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayCellBytes, slot.RetainedTextMemoryBytes))) : 0);
        var key = AwkArrayKey.CopyTrustedBytes(index, _counters, _memoryAccounting, _checkpoint);
        _checkpoint?.Invoke();
        if (_memoryAccounting.IsEnabled) InsertAdmittedArraySlot(slots, key, slot, oldCapacity);
        else AddArraySlot(slots, key, slot);
    }

    internal void SetArrayCellValue(AwkArrayIdentity array, AwkArrayCell cell, AwkValue value)
    {
        ThrowIfDetached(array);
        SetArrayCell(cell, AwkVariableSlot.FromValue(value));
    }

    internal bool TrySetCapturedArrayNumber(AwkArrayCell? cell, double number)
    {
        if (cell is null || !cell.IsAttached) return false;
        if (cell.Kind == AwkVariableSlotKind.Number)
        {
            cell.SetSlot(AwkVariableSlot.FromNumber(number));
            return true;
        }
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

    public long EstimateScalarValueMemoryGrowth(AwkVariableName name, AwkValue value) =>
        EstimateScalarSlotMemoryGrowth(name, AwkVariableSlot.FromValue(value));

    internal long EstimateScalarSlotMemoryGrowth(AwkVariableName name, AwkVariableSlot slot)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var id))
            return EstimateScalarSlotMemoryGrowth(GetScalarCell(id), slot);
        return _governScalarMemory
            ? AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateScalarSymbolMetadataBytes(name.Text.Length), EstimateScalarBytes(slot))
            : AwkRuntimeMemory.ScalarCellOverheadBytes;
    }

    internal long EstimateScalarSlotMemoryGrowth(AwkScalarCell cell, AwkVariableSlot slot)
    {
        if (!_governScalarMemory) return 0;
        var oldState = cell.State;
        var baseline = oldState.HasBaseline ? oldState.BaselineBytes : 0;
        var newCharge = Math.Max(0, EstimateScalarBytes(slot) - baseline);
        return Math.Max(0, newCharge - oldState.ChargedBytes);
    }

    public void CaptureScalarMemoryBaseline()
    {
        for (var index = 0; index < _scalarSlots.Count; index++)
        {
            var scalar = _scalarSlots[index];
            scalar.State.SetBaseline(EstimateScalarBytes(scalar.State.Slot));
        }

        _governScalarMemory = true;
    }

    public void InitializeGlobalNames(IReadOnlySet<AwkVariableName> names) => _globalNames = names;

    public AwkValue GetOrCreateUntypedScalarValue(AwkVariableName name)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var id))
            return CreateValue(GetScalarCell(id).State.Slot);
        using var admission = _governScalarMemory
            ? _memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateScalarSymbolMetadataBytes(name.Text.Length),
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
            return CreateValue(GetScalarCell(id).State.Slot).ForceScalar();

        SetScalarSlot(name, AwkVariableSlot.Unassigned());
        return AwkValue.Unassigned();

    }

    public AwkValue GetScalarValue(AwkGlobalSlotId id) => GetScalarValue(GetScalarCell(id));

    internal AwkValue GetScalarValue(AwkScalarCell cell)
    {
        if (_resolveFieldCountValue is not null &&
            string.Equals(cell.Name.Text, FieldCountName, StringComparison.Ordinal))
            return _resolveFieldCountValue();

        return CreateValue(cell.State.Slot).ForceScalar();
    }

    internal static AwkText PrepareScalarStringCache(AwkScalarCell cell, AwkText expected)
    {
        var slot = cell.State.Slot;
        if (!slot.TryGetStoredScalarText(out var stored) || !stored.HasSameStorage(expected))
            return expected;

        // The scalar's potential-cache estimate already funds this storage.
        // Preserve its origin, cached number, baseline and charge; older copies
        // continue to own their immutable bytes independently of this cache.
        var prepared = stored.WithStringCache();
        cell.State.Slot = slot.WithByteText(prepared);
        return prepared;
    }

    public bool TryGetScalarNumber(AwkGlobalSlotId id, out double number) =>
        TryGetScalarNumber(GetScalarCell(id), out number);

    internal bool TryGetScalarNumber(AwkScalarCell cell, out double number)
    {
        if (_resolveFieldCountValue is not null &&
            string.Equals(cell.Name.Text, FieldCountName, StringComparison.Ordinal))
        {
            number = 0;
            return false;
        }
        return cell.State.Slot.TryGetNumber(out number);
    }

    public void SetScalarValue(AwkGlobalSlotId id, AwkValue value) => SetScalarValue(GetScalarCell(id), value);

    internal void SetScalarValue(AwkScalarCell cell, AwkValue value)
    {
        var name = cell.Name;
        if (_resolveFieldCountValue is not null &&
            string.Equals(name.Text, FieldCountName, StringComparison.Ordinal))
        {
            _applyScalarAssignment(name.Text, value);
            return;
        }

        SetScalarSlot(cell, AwkVariableSlot.FromValue(value));
    }

    public AwkGlobalSlotId GetOrCreateScalarSlot(AwkVariableName name) =>
        GetOrCreateScalarSlotCore(name);

    // Callers own language-level effects. Reuse this scalar's funded storage;
    // text/origin transitions still go through the ordinary mutation owner.
    internal void SetScalarCellNumber(AwkScalarCell cell, double number)
    {
        if (cell.State.Slot.Kind == AwkVariableSlotKind.Number)
        {
            cell.State.Slot = AwkVariableSlot.FromNumber(number);
            return;
        }
        StoreScalarSlot(cell, AwkVariableSlot.FromNumber(number), prepareOutputSeparator: false);
    }

    // Implicit RT publication has no assignment callback. Reuse its prepared
    // representation while keeping mutation admission with the actual owner.
    internal void SetRecordTerminatorSlot(AwkVariableSlot slot)
    {
        if (GetSpecialScalarCell(AwkSpecialVariableId.RecordTerminator) is { } cell)
            StoreScalarSlot(cell, slot, prepareOutputSeparator: false);
        else
            SetScalarSlot(AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordTerminator), slot);
    }

    // Input counters use existing scalar owners. A script may replace a counter
    // with text, so only the already-numeric case bypasses value conversion.
    internal void IncrementRecordCounter(AwkScalarCell cell)
    {
        var prior = cell.State.Slot;
        var number = prior.TryGetNumber(out var numeric) ? numeric : CreateValue(prior).AsNumber(_checkpoint);
        SetScalarCellNumber(cell, Math.Truncate(number) + 1);
    }

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
                _createStringValue(AwkText.FromString(value, _counters, _memoryAccounting)));
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

    public AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayName name, bool materializeKeys, bool retainValues = false)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _memoryAccounting.IsEnabled
                ? _symbolTable.MeasureSnapshot(retainValues)
                : new AwkArraySnapshotSize(_symbolTable.Count, 0);

        var count = GetArraySnapshotCount(name);
        return MeasureArraySnapshot(GetOrCreateArraySlots(name), count, materializeKeys, retainValues);
    }

    internal AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayIdentity array, bool materializeKeys,
        bool retainValues = false) => MeasureArraySnapshot(array.Slots, array.Slots.Count, materializeKeys, retainValues);

    private AwkArraySnapshotSize MeasureArraySnapshot(Dictionary<AwkArrayKey, AwkArrayCell> slots, int count,
        bool materializeKeys, bool retainValues)
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
                key.RequiresTextMaterialization && materializeKeys
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
            : slot.ToValue(_createNumericStringValue, _createStringValue);

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

    // Cells stay valid when admission or OFS preparation grows the name/index tables.
    internal AwkScalarCell GetScalarCell(AwkGlobalSlotId id) => _scalarSlots[id.Value];

    internal AwkScalarCell? GetSpecialScalarCell(AwkSpecialVariableId id) => _specialScalarCells[(int)id];

    private AwkGlobalSlotId GetOrCreateScalarSlotCore(AwkVariableName name)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var existing))
            return existing;

        return AddScalarSlot(name, AwkVariableSlot.Unassigned());
    }

    private AwkGlobalSlotId AddScalarSlot(AwkVariableName name, AwkVariableSlot slot)
    {

        var id = new AwkGlobalSlotId(_scalarSlots.Count);
        var slotBytes = _governScalarMemory ? EstimateScalarBytes(slot) : 0;
        var metadataBytes = _governScalarMemory
            ? AwkRuntimeMemory.EstimateScalarSymbolMetadataBytes(name.Text.Length)
            : AwkRuntimeMemory.ScalarCellOverheadBytes;
        using var mutation = ReserveMemoryMutation(AwkRuntimeMemory.CheckedAdd(metadataBytes, slotBytes), 0);
        if (_memoryAccounting.IsEnabled) _checkpoint?.Invoke();
        _scalarSlotsByName.Add(name, id);
        var cell = new AwkScalarCell(
            name,
            _governScalarMemory
                ? AwkScalarState.Governed(slot, slotBytes)
                : AwkScalarState.Ungoverned(slot));
        _scalarSlots.Add(cell);
        if (AwkSpecialVariables.TryGetScalar(name, out var special) &&
            AwkCompatibilityNames.EnablesSpecial(special, _mode))
            _specialScalarCells[(int)special] = cell;
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

    private void SetScalarSlot(AwkGlobalSlotId id, AwkVariableSlot slot) => SetScalarSlot(GetScalarCell(id), slot);

    private void SetScalarSlot(AwkScalarCell cell, AwkVariableSlot slot) =>
        StoreScalarSlot(cell, slot, _governScalarMemory &&
            string.Equals(cell.Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal));

    private void StoreScalarSlot(AwkScalarCell cell, AwkVariableSlot slot, bool prepareOutputSeparator)
    {
        // A number overwrite retains its owner in either memory mode.
        if (slot.TryGetNumber(out var number) &&
            cell.State.Slot.Kind == AwkVariableSlotKind.Number)
        {
            if (prepareOutputSeparator)
                _prepareOutputFieldSeparatorAssignment();
            cell.State.Slot = AwkVariableSlot.FromNumber(number);
            return;
        }

        if (!_governScalarMemory || !_memoryAccounting.IsEnabled)
        {
            if (prepareOutputSeparator)
                _prepareOutputFieldSeparatorAssignment();
            cell.State.SetUngovernedSlot(slot);
            return;
        }

        var oldCharge = cell.State.ChargedBytes;
        var baselineBytes = cell.State.BaselineBytes;
        var baseline = cell.State.HasBaseline
            ? baselineBytes
            : 0;
        var newCharge = Math.Max(0, EstimateScalarBytes(slot) - baseline);
        using var mutation = ReserveMemoryMutation(newCharge, oldCharge);
        if (newCharge > oldCharge) _checkpoint?.Invoke();
        if (prepareOutputSeparator)
            _prepareOutputFieldSeparatorAssignment();
        cell.State.SetSlot(slot, baselineBytes, newCharge);
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
        return InsertArraySlot(values, index, slot);
    }

    private AwkArrayCell InsertArraySlot(Dictionary<AwkArrayKey, AwkArrayCell> values,
        AwkArrayKey index, AwkVariableSlot slot)
    {
        if (!_memoryAccounting.IsEnabled) return AddArraySlot(values, index, slot);
        var oldCapacity = values.EnsureCapacity(0);
        var payload = AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot);
        var growth = values.Count == oldCapacity;
        var target = growth ? PlanArrayCapacity(oldCapacity) : oldCapacity;
        using var admission = growth ? _memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(target)), payload)) : default;
        return InsertAdmittedArraySlot(values, index, slot, oldCapacity);
    }

    // The caller admits any capacity growth before key construction or allocation.
    // Complete the retained owner charge once, before publishing the new cell.
    private AwkArrayCell InsertAdmittedArraySlot(Dictionary<AwkArrayKey, AwkArrayCell> values,
        AwkArrayKey index, AwkVariableSlot slot, int oldCapacity)
    {
        if (values.Count == oldCapacity) values.EnsureCapacity(PlanArrayCapacity(oldCapacity));
        using var mutation = ReserveMemoryMutation(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(values.EnsureCapacity(0)),
                AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot)),
            AwkRuntimeMemory.EstimateArrayBackingBytes(oldCapacity));
        _checkpoint?.Invoke();
        var cell = AddArraySlot(values, index, slot);
        mutation.Commit();
        return cell;
    }

    private static AwkArrayCell AddArraySlot(Dictionary<AwkArrayKey, AwkArrayCell> values,
        AwkArrayKey index, AwkVariableSlot slot)
    {
        var created = new AwkArrayCell(slot);
        values.Add(index, created);
        return created;
    }

    private void SetArrayCell(AwkArrayCell cell, AwkVariableSlot slot)
    {
        var prior = cell.Slot;
        if (prior.TryGetArrayBinding(out _))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar("array element"));
        if (prior.Kind == AwkVariableSlotKind.Number && slot.TryGetNumber(out var number))
        {
            cell.SetSlot(AwkVariableSlot.FromNumber(number));
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
