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

    private readonly AwkNumberParsingMode _numberParsingMode;
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

    internal AwkNumberParsingMode NumberParsingMode => _numberParsingMode;

    internal sealed class AwkScalarCell(AwkVariableName name, AwkScalarState state)
    {
        public AwkVariableName Name { get; } = name;

        public AwkScalarState State = state;
    }

    internal struct AwkScalarState(
        AwkValue slot,
        long baselineBytes,
        long chargedBytes,
        bool hasBaseline)
    {
        public AwkValue Slot { readonly get; set; } = slot;
        public long BaselineBytes { readonly get; private set; } = baselineBytes;
        public long ChargedBytes { readonly get; private set; } = chargedBytes;
        public bool HasBaseline { readonly get; private set; } = hasBaseline;

        public static AwkScalarState Ungoverned(AwkValue slot) =>
            new(slot, baselineBytes: 0, chargedBytes: 0, hasBaseline: false);

        public static AwkScalarState Governed(AwkValue slot, long chargedBytes) =>
            new(slot, baselineBytes: 0, chargedBytes, hasBaseline: true);

        public void SetBaseline(long baselineBytes)
        {
            BaselineBytes = baselineBytes;
            ChargedBytes = 0;
            HasBaseline = true;
        }

        public void SetSlot(AwkValue slot, long baselineBytes, long chargedBytes)
        {
            Slot = slot;
            BaselineBytes = baselineBytes;
            ChargedBytes = chargedBytes;
            HasBaseline = true;
        }

        public void SetUngovernedSlot(AwkValue slot)
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
        Action<string, AwkValue> applyScalarAssignment,
        Func<AwkValue>? resolveFieldCountValue = null,
        Action? prepareOutputFieldSeparatorAssignment = null,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null,
        AwkNumberParsingMode numberParsingMode = AwkNumberParsingMode.Decimal)
    {

        _mode = mode;
        _checkpoint = checkpoint;

        _memoryAccounting = memoryAccounting;
        _counters = counters;
        _numberParsingMode = numberParsingMode;
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

        bool TryGetScalarSlot(AwkGlobalSlotId slotId, out AwkValue slot)
        {
            if (slotId.Value >= 0 && slotId.Value < _scalarSlots.Count)
            {
                var scalarName = GetScalarCell(slotId).Name;
                slot = resolveFieldCountValue is not null &&
                    string.Equals(scalarName.Text, FieldCountName, StringComparison.Ordinal)
                    ? AwkValue.ForVariableStorage(resolveFieldCountValue(), _numberParsingMode)
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
        (_scalarSlotsByName.TryGetValue(name, out var id) && !GetScalarCell(id).State.Slot.IsUntypedValue);

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
        var slots = array;
        if (slots.TryGetValue(index, out var slot)) return CreateValue(slot);
        return CreateMissingArrayValue(array, index);
    }

    internal double GetArrayNumber(AwkArrayIdentity array, AwkArrayKey index)
    {
        ThrowIfDetached(array);
        return array.TryGetValue(index, out var slot)
            ? ReadArraySlotNumber(slot)
            : CreateMissingArrayValue(array, index).AsNumber(_checkpoint);
    }

    internal AwkValue GetArrayValue(AwkArrayIdentity array, int ordinal) =>
        CreateValue(GetArrayOrdinalSlot(array, ordinal));

    internal double GetArrayNumber(AwkArrayIdentity array, int ordinal) =>
        ReadArraySlotNumber(GetArrayOrdinalSlot(array, ordinal));

    private AwkValue GetArrayOrdinalSlot(AwkArrayIdentity array, int ordinal)
    {
        ThrowIfDetached(array);
        if (array.TryGetIndexed(ordinal, out var slot)) return slot;
        var key = AwkArrayKey.FromOrdinal(ordinal);
        return array.TryGetValue(key, out slot) ? slot : CreateMissingArraySlot(array, key, out _);
    }

    internal AwkValue GetArrayValue(AwkArrayIdentity array, ReadOnlySpan<byte> index) =>
        CreateValue(GetArraySlot(array, index));

    internal double GetArrayNumber(AwkArrayIdentity array, ReadOnlySpan<byte> index) =>
        ReadArraySlotNumber(GetArraySlot(array, index));

    private AwkValue GetArraySlot(AwkArrayIdentity array, ReadOnlySpan<byte> index)
    {
        ThrowIfDetached(array);
        return array.ByteLookup.TryGetValue(index, out var slot)
            ? slot : InsertByteSlot(array, index, default, missing: true, out _, out _);
    }

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, ReadOnlySpan<byte> index)
    {
        ThrowIfDetached(array);
        if (!array.ByteLookup.TryGetEntry(index, out var key, out var slot, out var cell))
            slot = InsertByteSlot(array, index, default, missing: true, out key, out cell);
        return new(key, ReadArraySlotNumber(slot), array, cell);
    }

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, ReadOnlySpan<byte> index,
        in AwkArrayUpdateTarget previous)
    {
        if (!array.IsDetached && ReferenceEquals(previous.Array, array) && previous.Cell is { IsAttached: true } priorCell &&
            previous.Key.RequiresTextMaterialization && previous.Key.Equals(index, checkpoint: null) &&
            priorCell.Slot.TryGetNumberValue(out var number))
        {
            // No callback occurs between validating the bounded owning key and
            // capturing the current number. Never reuse the earlier snapshot.
            _counters.RecordNumericArraySlotRead();
            return new(previous.Key, number, array, priorCell);
        }
        return PrepareArrayUpdate(array, index);
    }

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, AwkArrayKey index)
    {
        ThrowIfDetached(array);
        if (!array.TryGetEntry(index, out var slot, out var cell))
            slot = CreateMissingArraySlot(array, index, out cell);
        return new(index, ReadArraySlotNumber(slot), array, cell);
    }

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, AwkArrayKey index,
        in AwkArrayUpdateTarget previous)
    {
        if (!array.IsDetached && ReferenceEquals(previous.Array, array) && previous.Cell is { IsAttached: true } priorCell &&
            previous.Key.RequiresTextMaterialization && previous.Key.Equals(index, checkpoint: null) &&
            priorCell.Slot.TryGetNumberValue(out var number))
        {
            // Keep the newly evaluated owning key, including its origin. The old
            // location supplies only the attached cell and its current number.
            _counters.RecordNumericArraySlotRead();
            return new(index, number, array, priorCell);
        }
        // Fresh complete operations need no prior-target transport. Actual reuse
        // misses share their ordinary lookup, admission and conversion.
        return PrepareArrayUpdate(array, index);
    }

    internal double ReadArraySlotNumber(scoped in AwkValue slot)
    {
        if (!slot.TryGetNumberValue(out var number))
            return slot.ReadVariableNumber(_numberParsingMode, _counters, _checkpoint);
        _counters.RecordNumericArraySlotRead();
        return number;
    }

    private AwkValue CreateMissingArrayValue(AwkArrayIdentity array, AwkArrayKey index) =>
        CreateValue(CreateMissingArraySlot(array, index, out _));

    private AwkValue CreateMissingArraySlot(AwkArrayIdentity array, AwkArrayKey index, out AwkArrayCell? cell)
    {
        using var admission = _memoryAccounting.AdmitMutation(EstimateArrayInsertionGrowth(array, index,
            AwkRuntimeMemory.EstimateUntypedArrayElementBytes(index) - AwkRuntimeMemory.EstimateArrayBackingBytes(1)));
        var slot = AwkValue.FromUntypedScalar(new AwkUntypedScalar(array));
        cell = InsertAdmittedArraySlot(array, index, slot);
        return slot;
    }

    internal bool TryGetArrayKeySlot(AwkArrayIdentity array, ReadOnlySpan<byte> index,
        out AwkArrayKey key, out AwkValue value, out AwkArrayCell? cell) =>
        array.ByteLookup.TryGetEntry(index, out key, out value, out cell);

    internal void SetArrayValue(AwkArrayIdentity array, AwkArrayKey index, AwkValue value)
    {
        ThrowIfDetached(array);
        SetArraySlot(array, index, AwkValue.ForVariableStorage(value, _numberParsingMode));
    }

    internal void SetArrayOrdinalValue(AwkArrayIdentity array, int ordinal, AwkValue value)
    {
        ThrowIfDetached(array);
        var slot = AwkValue.ForVariableStorage(value, _numberParsingMode);
        if (array.TryGetIndexed(ordinal, out var prior))
            StoreArraySlot(array, default, prior, null, slot, ordinal);
        else if (array.TryPlanIndexedInsertion(ordinal, out var capacity))
        {
            var payload = _memoryAccounting.IsEnabled ? AwkRuntimeMemory.EstimateArrayElementPayloadBytes(default, slot) : 0;
            var peak = _memoryAccounting.IsEnabled
                ? AwkRuntimeMemory.CheckedAdd(payload - AwkRuntimeMemory.ArrayCellBytes,
                    array.EstimateIndexedInsertionBackingBytes(capacity)) : 0;
            using var admission = peak > payload ? _memoryAccounting.AdmitMutation(peak) : default;
            InsertIndexedArraySlot(array, ordinal, capacity, 0, slot);
        }
        else SetArraySlot(array, AwkArrayKey.FromOrdinal(ordinal), slot);
    }

    internal void InsertArrayValue(AwkArrayIdentity array, ReadOnlySpan<byte> index, AwkValue value) =>
        InsertByteSlot(array, index, AwkValue.ForVariableStorage(value, _numberParsingMode), missing: false, out _, out _);

    // Admission covers retained key construction and its actual storage form.
    private AwkValue InsertByteSlot(AwkArrayIdentity array, ReadOnlySpan<byte> index,
        AwkValue slot, bool missing, out AwkArrayKey key, out AwkArrayCell? cell)
    {
        ThrowIfDetached(array);
        using var admission = _memoryAccounting.AdmitMutation(_memoryAccounting.IsEnabled
            ? EstimateArrayInsertionGrowth(array, index, AwkRuntimeMemory.CheckedAdd(
                AwkArrayKey.EstimateRetainedTextBytes(index.Length),
                AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayCellBytes,
                    missing ? AwkRuntimeMemory.EstimateUntypedScalarBytes() : slot.RetainedTextMemoryBytes))) : 0);
        key = AwkArrayKey.CopyTrustedBytes(index, _counters, _memoryAccounting, _checkpoint);
        _checkpoint?.Invoke();
        if (missing) slot = AwkValue.FromUntypedScalar(new AwkUntypedScalar(array));
        cell = InsertAdmittedArraySlot(array, key, slot);
        return slot;
    }

    internal void SetLocatedArrayValue(AwkArrayIdentity array, AwkArrayKey key,
        AwkValue prior, AwkArrayCell? cell, AwkValue value)
    {
        ThrowIfDetached(array);
        StoreArraySlot(array, key, prior, cell, AwkValue.ForVariableStorage(value, _numberParsingMode));
    }

    internal bool TrySetCapturedArrayNumber(AwkArrayIdentity array, in AwkArrayUpdateTarget target, double number)
    {
        if (target.Cell is { } cell)
        {
            if (!cell.IsAttached) return false;
            if (cell.Slot.StoredKind == AwkValueKind.Number)
            {
                cell.SetSlot(AwkValue.FromNumber(number));
                return true;
            }
            StoreArraySlot(array, target.Key, cell.Slot, cell, AwkValue.FromNumber(number));
            return true;
        }
        if (!array.TryGetEntry(target.Key, out var prior, out var located)) return false;
        StoreArraySlot(array, target.Key, prior, located, AwkValue.FromNumber(number));
        return true;
    }

    public bool TryGetArrayCount(AwkArrayName name, out int count)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            count = _symbolTable.Count;
            return true;
        }

        if (_arrays.TryGetArray(name, out var values))
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
        SetScalarSlot(name, AwkValue.FromDeferredText(AwkText.FromString(value), _numberParsingMode));
    }

    public void SetScalarValue(AwkVariableName name, AwkValue value)
    {
        if (IsComputedFieldCount(name))
        {
            _applyScalarAssignment(name.Text, value);
            return;
        }
        SetScalarSlot(name, AwkValue.ForVariableStorage(value, _numberParsingMode));
    }

    private bool IsComputedFieldCount(AwkVariableName name) =>
        _governScalarMemory && _resolveFieldCountValue is not null &&
        string.Equals(name.Text, FieldCountName, StringComparison.Ordinal);

    public long EstimateScalarValueMemoryGrowth(AwkVariableName name, AwkValue value) =>
        EstimateScalarSlotMemoryGrowth(name, AwkValue.ForVariableStorage(value, _numberParsingMode));

    internal long EstimateScalarSlotMemoryGrowth(AwkVariableName name, AwkValue slot)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var id))
            return EstimateScalarSlotMemoryGrowth(GetScalarCell(id), slot);
        return _governScalarMemory
            ? AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateScalarSymbolMetadataBytes(name.Text.Length), EstimateScalarBytes(slot))
            : AwkRuntimeMemory.ScalarCellOverheadBytes;
    }

    internal long EstimateScalarSlotMemoryGrowth(AwkScalarCell cell, AwkValue slot)
    {
        if (!_governScalarMemory) return 0;
        var oldState = cell.State;
        if (oldState.HasBaseline && oldState.Slot.HasSameTextStorage(slot)) return 0;
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
        var slot = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
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

        SetScalarSlot(name, AwkValue.Unassigned());
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
        return cell.State.Slot.TryGetNumberValue(out number);
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

        SetScalarSlot(cell, AwkValue.ForVariableStorage(value, _numberParsingMode));
    }

    internal bool TrySetConsumedScalarValue(AwkScalarCell cell, AwkValue value,
        AwkRegionStorage source, in AwkRegionOperand sourceOperand)
    {
        // Computed NF and bootstrap storage retain their ordinary assignment owner.
        if (!_governScalarMemory || _resolveFieldCountValue is not null &&
            string.Equals(cell.Name.Text, FieldCountName, StringComparison.Ordinal)) return false;
        if (sourceOperand.Kind != AwkRegionOperandKind.TextTemporary)
            value = AwkValue.ForVariableStorage(value, _numberParsingMode);
        else System.Diagnostics.Debug.Assert(value.StoredKind ==
            AwkValue.FromDeferredText(AwkText.Empty, _numberParsingMode).StoredKind);
        StoreScalarSlot(cell, value,
            string.Equals(cell.Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal), source, in sourceOperand);
        return true;
    }

    public AwkGlobalSlotId GetOrCreateScalarSlot(AwkVariableName name) =>
        GetOrCreateScalarSlotCore(name);

    // Callers own language-level effects. Reuse this scalar's funded storage;
    // text/origin transitions still go through the ordinary mutation owner.
    internal void SetScalarCellNumber(AwkScalarCell cell, double number)
    {
        if (cell.State.Slot.StoredKind == AwkValueKind.Number)
        {
            cell.State.Slot = AwkValue.FromNumber(number);
            return;
        }
        StoreScalarSlot(cell, AwkValue.FromNumber(number), prepareOutputSeparator: false);
    }

    // Implicit RT publication has no assignment callback. Reuse its prepared
    // representation while keeping mutation admission with the actual owner.
    internal void SetRecordTerminatorSlot(AwkValue slot)
    {
        if (GetSpecialScalarCell(AwkSpecialVariableId.RecordTerminator) is { } cell)
        {
            if ((!_governScalarMemory || cell.State.HasBaseline) &&
                cell.State.Slot.HasSameTextStorage(slot)) return;
            StoreScalarSlot(cell, slot, prepareOutputSeparator: false);
        }
        else
            SetScalarSlot(AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordTerminator), slot);
    }

    // Input counters use existing scalar owners. A script may replace a counter
    // with text, so only the already-numeric case bypasses value conversion.
    internal void IncrementRecordCounter(AwkScalarCell cell)
    {
        var prior = cell.State.Slot;
        var number = prior.TryGetNumberValue(out var numeric) ? numeric : CreateValue(prior).AsNumber(_checkpoint);
        SetScalarCellNumber(cell, Math.Truncate(number) + 1);
    }

    public void InitializeArrayValues(AwkArrayName name, IReadOnlyList<KeyValuePair<AwkArrayKey, AwkValue>> values)
    {
        var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(values.Count);
        foreach (var pair in values)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                EstimateElementBytes(pair.Key, AwkValue.ForVariableStorage(pair.Value, _numberParsingMode)));
        }
        using var allocationPeak = _memoryAccounting.ReserveAllocationPeak(bytes);
        var slots = CreateArraySlots(values.Count);
        foreach (var pair in values)
            slots[pair.Key] = AwkValue.ForVariableStorage(pair.Value, _numberParsingMode);
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
            CreateIndexedArraySlots(values, firstIndex, static (text, _) => AwkValue.ForStoredInputText(text)));
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
            slots[new AwkArrayKey(keyText, _checkpoint)] = AwkValue.ForStoredInputText(valueText);
        }

        ReplaceArraySlots(name, slots);
    }

    public void InitializeArrayStringNames(
        AwkArrayName name,
        int count,
        IEnumerable<string> names)
    {
        using var construction = _memoryAccounting.ReserveAllocationPeak(
            AwkRuntimeMemory.EstimateArrayConstructionBytes(AwkRuntimeMemory.BoundArrayCapacity(count), count));
        var slots = CreateArraySlots(count);
        foreach (var value in names)
        {
            var text = AwkText.FromString(value, AwkRuntimeCounters.Disabled, _checkpoint);
            slots[new AwkArrayKey(text, _checkpoint)] = AwkValue.FromDeferredText(text, _numberParsingMode);
        }

        ReplaceArraySlots(name, slots);
    }

    public AwkValue GetArrayValue(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _symbolTable.GetValue(index.AsString(_counters, _checkpoint));

        var values = GetOrCreateArray(name);
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

        var values = GetOrCreateArray(name);
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
            : GetOrCreateArray(name).ContainsKey(index);

    public bool ContainsArrayValue(AwkArrayName name, ReadOnlySpan<byte> index) =>
        !name.IsFunctionLocal && IsSymbolTable(name.Text)
            ? _symbolTable.ContainsValue(AwkUtf8Codec.DecodeRuntimeText(index, _counters))
            : GetOrCreateArray(name).ByteLookup.ContainsKey(index);

    public void SetArrayString(AwkArrayName name, AwkArrayKey index, string value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            _symbolTable.AssignValue(
                index.AsString(_counters, _checkpoint),
                CreateValue(AwkValue.ForStoredText(AwkText.FromString(value, _counters, _memoryAccounting))));
            return;
        }

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        var values = GetOrCreateArray(name);
        SetArraySlot(values, index, AwkValue.FromDeferredText(AwkText.FromString(value), _numberParsingMode));
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
        var values = GetOrCreateArray(name);
        SetArraySlot(values, index, AwkValue.ForVariableStorage(value, _numberParsingMode));
    }

    public long EstimateArrayValueMemoryGrowth(AwkArrayName name, AwkArrayKey index, AwkValue value)
    {
        AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        var slot = AwkValue.ForVariableStorage(value, _numberParsingMode);
        if (_arrays.TryGetArray(name, out var slots) && slots.TryGetValue(index, out var oldSlot))
            return Math.Max(0, AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot) -
                AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, oldSlot));
        return EstimateArrayInsertionGrowth(slots, index, AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot));
    }

    public void DeleteArrayValue(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            throw new AwkRuntimeException(AwkMessages.ErrorCannotDeleteSymtab);

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        _ = GetOrCreateArray(name);
        DeleteArrayValue(_arrays.ResolveArray(name), index);
    }

    public void SetArrayValues<TValue>(
        AwkArrayName name,
        IReadOnlyList<TValue> values,
        Func<TValue, AwkNumberParsingMode, AwkValue> createSlot,
        AwkMemoryLease sourceMemoryLease,
        long sourceRetainedTextBytes)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _mode);
        using var construction = _memoryAccounting.AdmitMutation(
            AwkRuntimeMemory.EstimateIndexedArrayConstructionBytes(values.Count));
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
        if (_arrays.TryGetArray(name, out var slots) && slots.TryGetValue(index, out var slot))
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
            _arrays.TryGetArray(name, out var array) &&
            array.ByteLookup.TryGetValue(index, out var slot))
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
        out AwkArrayKey key, out AwkValue slot)
    {
        if ((name.IsFunctionLocal || !IsSymbolTable(name.Text)) &&
            _arrays.TryGetArray(name, out var array) &&
            array.ByteLookup.TryGetValue(index, out key, out slot))
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

        if (_arrays.TryGetArray(name, out var slots))
        {
            if (slots.ContainsKey(index))
                return default;
        }
        return _memoryAccounting.AdmitMutation(EstimateArrayInsertionGrowth(slots, index,
            AwkRuntimeMemory.EstimateUntypedArrayElementBytes(index) - AwkRuntimeMemory.EstimateArrayBackingBytes(1)));
    }

    public void SetArrayValuesFromExternalText(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _mode);
        using var construction = _memoryAccounting.AdmitMutation(
            AwkRuntimeMemory.EstimateIndexedArrayConstructionBytes(values.Count));
        ReplaceArraySlots(name, CreateIndexedArraySlots(values, firstIndex: 1, static (text, _) => AwkValue.ForStoredInputText(text)));
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
            AwkRuntimeMemory.EstimateIndexedArrayConstructionBytes(fields.Count),
            AwkRuntimeMemory.EstimateArrayConstructionBytes(AwkRuntimeMemory.BoundArrayCapacity(separators.Count), separators.Count)));
        ReplaceArraySlots(
            fieldArrayName,
            CreateIndexedArraySlots(fields, firstIndex: 1, static (text, _) => AwkValue.ForStoredInputText(text)),
            separatorArrayName,
            CreateSeparatorArraySlots());

        AwkArrayIdentity CreateSeparatorArraySlots()
        {
            var slots = CreateArraySlots(separators.Count);
            foreach (var separator in separators)
                slots[AwkArrayKey.FromOrdinal(separator.FieldIndex)] = AwkValue.ForStoredInputText(separator.Text);
            return slots;
        }

        void ReplaceArraySlots(
            AwkArrayTarget firstName,
            AwkArrayIdentity firstSlots,
            AwkArrayTarget secondName,
            AwkArrayIdentity secondSlots)
        {
            long ExistingBytes(AwkArrayTarget name) =>
                TryGetTargetArray(name, out var array)
                    ? EstimateOwnedArrayBytes(array)
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
        AwkArrayIdentity slots,
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

        var slots = GetOrCreateArray(name);
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

        return GetOrCreateArray(name).Count;
    }

    public AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayName name, bool materializeKeys, bool retainValues = false)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _memoryAccounting.IsEnabled
                ? _symbolTable.MeasureSnapshot(retainValues)
                : new AwkArraySnapshotSize(_symbolTable.Count, 0);

        var count = GetArraySnapshotCount(name);
        return MeasureArraySnapshot(GetOrCreateArray(name), count, materializeKeys, retainValues);
    }

    internal AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayIdentity array, bool materializeKeys,
        bool retainValues = false) => MeasureArraySnapshot(array, array.Count, materializeKeys, retainValues);

    private AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayIdentity slots, int count,
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

    private AwkArrayIdentity GetOrCreateArray(AwkArrayName name)
    {
        if (_arrays.TryGetArray(name, out var values))
            return values;

        using var creation = ReserveMemoryMutation(AwkRuntimeMemory.ArrayIdentityBytes, 0);
        values = _arrays.ResolveArray(name);
        creation.Commit();
        return values;
    }

    private AwkArrayIdentity CreateIndexedArraySlots<TValue>(
        IReadOnlyList<TValue> values,
        int firstIndex,
        Func<TValue, AwkNumberParsingMode, AwkValue> createSlot)
    {
        if (firstIndex == 1)
        {
            var cells = AwkArrayIdentity.AllocateIndexedSlots(values.Count,
                AwkArrayIdentity.IndexedPageCount(values.Count), _checkpoint);
            for (var index = 0; index < values.Count; index++)
            {
                if ((index & 63) == 0) _checkpoint?.Invoke();
                cells[index >> AwkArrayIdentity.IndexedPageShift][index & AwkArrayIdentity.IndexedPageMask] =
                    createSlot(values[index], _numberParsingMode);
            }
            _checkpoint?.Invoke();
            return AwkArrayIdentity.FromIndexed(cells, values.Count, _arrayKeyComparer, _checkpoint);
        }
        var destination = CreateArraySlots(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var key = AwkArrayKey.FromOrdinal(index + firstIndex);
            destination[key] = createSlot(values[index], _numberParsingMode);
        }

        return destination;
    }

    public AwkArrayIdentity CreateMutableArraySlots(int capacity) =>
        CreateArraySlots(capacity);

    private AwkArrayIdentity CreateArraySlots(int capacity, AwkArrayIdentity? parent = null) =>
        new(capacity, _arrayKeyComparer, parent);

    public void CopyArrayKeys(AwkArrayName name, AwkArrayKey[] destination)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            _symbolTable.CopyKeys(destination);
            return;
        }

        var slots = GetOrCreateArray(name);
        CopyArrayKeys(slots, destination);
    }

    internal void CopyArrayKeys(AwkArrayIdentity slots, AwkArrayKey[] destination)
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

        var oldBytes = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes, EstimateArrayBytes(array));
        _arrays.Remove(name, out _);
        RetireArrayContents(array, includeRoot: true, cancellable: false);
        _memoryAccounting.ReleaseCommitted(oldBytes);
    }

    internal AwkValue CreateValue(AwkValue slot) =>
        slot.ReadVariableStorage(_numberParsingMode, _counters, _checkpoint);

    // Cells stay valid when admission or OFS preparation grows the name/index tables.
    internal AwkScalarCell GetScalarCell(AwkGlobalSlotId id) => _scalarSlots[id.Value];

    internal AwkScalarCell? GetSpecialScalarCell(AwkSpecialVariableId id) => _specialScalarCells[(int)id];

    private AwkGlobalSlotId GetOrCreateScalarSlotCore(AwkVariableName name)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var existing))
            return existing;

        return AddScalarSlot(name, AwkValue.Unassigned());
    }

    private AwkGlobalSlotId AddScalarSlot(AwkVariableName name, AwkValue slot)
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

    private void SetScalarSlot(AwkVariableName name, AwkValue slot)
    {
        if (_scalarSlotsByName.TryGetValue(name, out var existing))
        {
            SetScalarSlot(existing, slot);
            return;
        }

        AddScalarSlot(name, slot);
    }

    private void SetScalarSlot(AwkGlobalSlotId id, AwkValue slot) => SetScalarSlot(GetScalarCell(id), slot);

    private void SetScalarSlot(AwkScalarCell cell, AwkValue slot) =>
        StoreScalarSlot(cell, slot, _governScalarMemory &&
            string.Equals(cell.Name.Text, OutputFieldSeparatorName, StringComparison.Ordinal));

    private void StoreScalarSlot(AwkScalarCell cell, AwkValue slot, bool prepareOutputSeparator,
        AwkRegionStorage? source = null, in AwkRegionOperand sourceOperand = default)
    {
        // A number overwrite retains its owner in either memory mode.
        if (slot.TryGetNumberValue(out var number) &&
            cell.State.Slot.StoredKind == AwkValueKind.Number)
        {
            if (prepareOutputSeparator)
                _prepareOutputFieldSeparatorAssignment();
            source?.TransferScalarFunding(in sourceOperand, 0);
            cell.State.Slot = AwkValue.FromNumber(number);
            return;
        }

        if (!_governScalarMemory || !_memoryAccounting.IsEnabled)
        {
            if (prepareOutputSeparator)
                _prepareOutputFieldSeparatorAssignment();
            source?.TransferScalarFunding(in sourceOperand, 0);
            cell.State.SetUngovernedSlot(slot);
            return;
        }

        var oldCharge = cell.State.ChargedBytes;
        var baselineBytes = cell.State.BaselineBytes;
        var baseline = cell.State.HasBaseline
            ? baselineBytes
            : 0;
        var newCharge = Math.Max(0, EstimateScalarBytes(slot) - baseline);
        var sourceBytes = source is null ? 0 : slot.RetainedTextMemoryBytes;
        var credited = AwkRuntimeMemory.CheckedAdd(oldCharge, sourceBytes);
        using var mutation = ReserveMemoryMutation(newCharge, credited);
        if (newCharge > credited) _checkpoint?.Invoke();
        if (prepareOutputSeparator)
            _prepareOutputFieldSeparatorAssignment();
        source?.TransferScalarFunding(in sourceOperand, sourceBytes);
        cell.State.SetSlot(slot, baselineBytes, newCharge);
        mutation.Commit();
    }

    private void SetArraySlot(AwkArrayIdentity values, AwkArrayKey index, AwkValue slot)
    {
        if (values.TryGetEntry(index, out var prior, out var cell))
            StoreArraySlot(values, index, prior, cell, slot);
        else InsertArraySlot(values, index, slot);
    }

    private void InsertArraySlot(AwkArrayIdentity values, AwkArrayKey index, AwkValue slot)
    {
        var payload = _memoryAccounting.IsEnabled ? AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot) : 0;
        var peak = _memoryAccounting.IsEnabled ? EstimateArrayInsertionGrowth(values, index, payload) : 0;
        using var admission = peak > payload ? _memoryAccounting.AdmitMutation(peak) : default;
        InsertAdmittedArraySlot(values, index, slot);
    }

    private AwkArrayCell? InsertAdmittedArraySlot(AwkArrayIdentity values, AwkArrayKey index, AwkValue slot)
    {
        if (values.TryPlanIndexedInsertion(index, out var ordinal, out var capacity))
        {
            InsertIndexedArraySlot(values, ordinal, capacity, index.RetainedTextMemoryBytes, slot);
            return null;
        }
        if (values.IsIndexed) return PromoteAndInsertArraySlot(values, index, slot);
        if (!_memoryAccounting.IsEnabled) return values.AddDictionarySlot(index, slot);
        var oldCapacity = values.EnsureCapacity(0);
        if (values.Count == oldCapacity) values.EnsureCapacity(AwkRuntimeMemory.PlanArrayCapacity(oldCapacity));
        using var mutation = ReserveMemoryMutation(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(values.EnsureCapacity(0)),
                AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot)),
            AwkRuntimeMemory.EstimateArrayBackingBytes(oldCapacity));
        _checkpoint?.Invoke();
        var cell = values.AddDictionarySlot(index, slot);
        mutation.Commit();
        return cell;
    }

    private void InsertIndexedArraySlot(AwkArrayIdentity values, int ordinal, int capacity,
        long keyBytes, AwkValue slot)
    {
        var oldBacking = values.BackingBytes;
        var newBacking = values.EstimateIndexedBackingBytes(capacity);
        using var mutation = ReserveMemoryMutation(AwkRuntimeMemory.CheckedAdd(
            newBacking, AwkRuntimeMemory.CheckedAdd(keyBytes, slot.RetainedTextMemoryBytes)), oldBacking);
        var slots = values.PrepareIndexedCapacity(capacity, _checkpoint);
        if (_memoryAccounting.IsEnabled || oldBacking != newBacking) _checkpoint?.Invoke();
        values.PublishIndexedInsertion(in slots, ordinal, slot, _checkpoint);
        mutation.Commit();
    }

    private AwkArrayCell PromoteAndInsertArraySlot(AwkArrayIdentity values, AwkArrayKey index, AwkValue slot)
    {
        var oldBacking = values.BackingBytes;
        var cellBytes = AwkRuntimeMemory.CheckedMultiply(values.Count, AwkRuntimeMemory.ArrayCellBytes);
        var dictionary = values.BuildDictionary(Math.Max(3, values.Count + 1), _checkpoint);
        using var mutation = ReserveMemoryMutation(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(dictionary.EnsureCapacity(0)), cellBytes),
            AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot)), oldBacking);
        _checkpoint?.Invoke();
        var cell = new AwkArrayCell(slot);
        dictionary.Add(index.PrepareByteLookup(), cell);
        values.PublishDictionary(dictionary);
        mutation.Commit();
        return cell;
    }

    private void StoreArraySlot(AwkArrayIdentity array, AwkArrayKey key, AwkValue prior,
        AwkArrayCell? cell, AwkValue slot, int ordinal = 0)
    {
        if (prior.TryGetArrayBinding(out _))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar("array element"));
        if (prior.StoredKind == AwkValueKind.Number && slot.TryGetNumberValue(out var number))
        {
            Publish(AwkValue.FromNumber(number));
            return;
        }
        using var mutation = ReserveMemoryMutation(slot.RetainedTextMemoryBytes, prior.RetainedTextMemoryBytes);
        _checkpoint?.Invoke();
        if (prior.TryGetUntypedScalar(out var origin))
        {
            RepairElementAliases?.Invoke(origin);
            origin.Detach();
        }
        Publish(slot);
        mutation.Commit();

        void Publish(AwkValue value)
        {
            if (cell is not null) cell.SetSlot(value);
            else if (ordinal > 0) array.SetIndexedValue(ordinal, value);
            else array.SetIndexedValue(key, value);
        }
    }

    private static long EstimateArrayInsertionGrowth(AwkArrayIdentity? slots, AwkArrayKey index, long payloadBytes) =>
        slots is not null && slots.TryPlanIndexedInsertion(index, out _, out var capacity)
            ? AwkRuntimeMemory.CheckedAdd(payloadBytes - AwkRuntimeMemory.ArrayCellBytes,
                slots.EstimateIndexedInsertionBackingBytes(capacity))
            : AwkRuntimeMemory.CheckedAdd(payloadBytes, EstimateDictionaryInsertionBackingBytes(slots));

    private static long EstimateArrayInsertionGrowth(AwkArrayIdentity slots, ReadOnlySpan<byte> index, long payloadBytes) =>
        slots.TryPlanIndexedInsertion(index, out _, out var capacity)
            ? AwkRuntimeMemory.CheckedAdd(payloadBytes - AwkRuntimeMemory.ArrayCellBytes,
                slots.EstimateIndexedInsertionBackingBytes(capacity))
            : AwkRuntimeMemory.CheckedAdd(payloadBytes, EstimateDictionaryInsertionBackingBytes(slots));

    private static long EstimateDictionaryInsertionBackingBytes(AwkArrayIdentity? slots)
    {
        var capacity = slots?.EnsureCapacity(0) ?? 0;
        return slots?.IsIndexed == true
            ? AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(AwkRuntimeMemory.BoundArrayCapacity(Math.Max(3, slots.Count + 1))),
                AwkRuntimeMemory.CheckedMultiply(slots.Count, AwkRuntimeMemory.ArrayCellBytes))
            : (slots?.Count ?? 0) == capacity
                ? AwkRuntimeMemory.EstimateArrayBackingBytes(AwkRuntimeMemory.BoundArrayCapacity(AwkRuntimeMemory.PlanArrayCapacity(capacity))) : 0;
    }

    private void ReplaceArraySlots(AwkArrayName name, AwkArrayIdentity slots) =>
        ReplaceArraySlots(name, slots, AwkMemoryLease.Disabled, transferredBytes: 0);

    private void ReplaceArraySlots(
        AwkArrayName name,
        AwkArrayIdentity slots,
        AwkMemoryLease sourceMemoryLease,
        long transferredBytes)
    {
        var oldBytes = 0L;
        if (_arrays.TryGetArray(name, out var existingSlots))
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

    internal static long EstimateArrayBytes(IReadOnlyDictionary<AwkArrayKey, AwkValue> values,
        Action? checkpoint = null)
    {
        // Only nested arrays need a worklist; script-controlled depth must not recurse
        // through the CLR stack. Its reference metadata is minor beside admitted owners.
        Stack<IReadOnlyDictionary<AwkArrayKey, AwkValue>>? pending = null;
        var bytes = 0L;
        var visited = 0;
        while (true)
        {
            // Synthetic enumeration-only inputs remain useful for checked-total qualification.
            if (values is AwkArrayIdentity slots)
            {
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, slots.BackingBytes);
                using var entries = slots.GetEnumerator(checkpoint);
                while (entries.MoveNext()) Observe(entries.Current);
            }
            else
            {
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateArrayBackingBytes(
                    values is Dictionary<AwkArrayKey, AwkValue> dictionary ? dictionary.EnsureCapacity(0) : values.Count));
                foreach (var pair in values) Observe(pair);
            }
            if (pending is null || !pending.TryPop(out var next)) break;
            values = next;
        }
        return bytes;

        void Observe(KeyValuePair<AwkArrayKey, AwkValue> pair)
        {
            if ((visited++ & 63) == 0) checkpoint?.Invoke();
            bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                values is AwkArrayIdentity { IsIndexed: true }
                    ? AwkRuntimeMemory.EstimateArrayPayloadBytes(pair.Key, pair.Value)
                    : AwkRuntimeMemory.EstimateArrayElementPayloadBytes(pair.Key, pair.Value));
            if (pair.Value.TryGetArrayBinding(out var child))
            {
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.ArrayIdentityBytes);
                (pending ??= new()).Push(child);
            }
        }
    }

    private static long EstimateElementBytes(AwkArrayKey key, AwkValue slot) =>
        AwkRuntimeMemory.EstimateArrayElementBytes(key, slot);

    private static long EstimateScalarBytes(AwkValue slot) =>
        AwkRuntimeMemory.EstimateStoredValueBytes(slot);

    private long EstimateOwnedArrayBytes(IReadOnlyDictionary<AwkArrayKey, AwkValue> slots) =>
        _memoryAccounting.IsEnabled ? AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes,
            EstimateArrayBytes(slots, _checkpoint)) : 0;

}
