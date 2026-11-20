using System.Collections.Frozen;
using System.Text;

namespace Lokad.Awk;

internal sealed partial class AwkVariables
{
    public const string FunctionTableName = "FUNCTAB";
    public const string SymbolTableName = "SYMTAB";

    private static readonly AwkVariableName FieldCountName = AwkSpecialVariables.VariableName(AwkSpecialVariableId.FieldCount);

    private readonly AwkRuntimeState _state;
    private readonly Dictionary<AwkVariableName, AwkGlobalSlotId> _scalarSlotsByName = new();
    private readonly List<AwkScalarCell> _scalarSlots = [];
    private readonly AwkScalarCell?[] _specialScalarCells;
    private readonly AwkArrayKeyComparer _arrayKeyComparer;
    private readonly AwkArrayStorage _arrays;
    private readonly AwkSymbolTableArray _symbolTable;
    private IReadOnlySet<AwkVariableName> _globalNames = FrozenSet<AwkVariableName>.Empty;
    private bool _governScalarMemory;

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

    public AwkVariables(AwkRuntimeState state)
    {
        _state = state;
        var checkpoint = state.CancellationCheckpoint;
        // Scalar IDs precede arrays. These are references to real owners, not a
        // second value store; absent and disabled names keep ordinary resolution.
        using (var bindingMemory = ReserveMemoryMutation(AwkRuntimeMemory.EstimateSpecialScalarBindingsBytes(), 0))
        {
            if (_state.MemoryAccounting.IsEnabled) _state.ThrowIfCancellationRequested();
            _specialScalarCells = new AwkScalarCell?[(int)AwkSpecialVariableId.ArgumentVector];
            bindingMemory.Commit();
        }
        _arrayKeyComparer = AwkArrayKeyComparer.ForRuntime(checkpoint);
        _arrays = new AwkArrayStorage(_arrayKeyComparer);
        _symbolTable = new AwkSymbolTableArray(
            () => _globalNames,
            EnumerateScalarInventory,
            TryResolveScalarSlot,
            TryGetScalarSlot,
            _arrays.Contains,
            () => _arrays.Names,
            CreateValue,
            state.ApplySpecialVariableAssignment,
            checkpoint);

        bool TryResolveScalarSlot(AwkVariableName name, out AwkGlobalSlotId slotId) =>
            _scalarSlotsByName.TryGetValue(name, out slotId);

        bool TryGetScalarSlot(AwkGlobalSlotId slotId, out AwkValue slot)
        {
            if (slotId.Value >= 0 && slotId.Value < _scalarSlots.Count)
            {
                var cell = GetScalarCell(slotId);
                slot = IsFieldCountCell(cell)
                    ? AwkValue.ForVariableStorage(_state.RecordFields.GetFieldCountValue(), _state.NumberParsingMode)
                    : cell.State.Slot;
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
    }

    public bool ContainsScalar(AwkVariableName name) =>
        (AwkSpecialVariables.TryGetScalar(name, out var special) &&
            AwkCompatibilityNames.EnablesSpecial(special, _state.CompatibilityMode)) ||
        (_scalarSlotsByName.TryGetValue(name, out var id) && !GetScalarCell(id).State.Slot.IsUntypedValue);

    public bool IsSymbolTable(string name) =>
        AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.SymbolTable, _state.CompatibilityMode) &&
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
        if (array.IsIndexed)
        {
            if (array.TryGetIndexed(index, out _, out var slot)) return ReadArraySlotNumber(slot);
        }
        else if (array.TryGetDictionaryCell(index, out var cell)) return ReadArraySlotNumber(in cell.BorrowedSlot);
        return CreateMissingArrayValue(array, index).AsNumber(_state.CancellationCheckpoint);
    }

    internal AwkValue GetArrayValue(AwkArrayIdentity array, AwkArrayKey index, in AwkArrayUpdateTarget location) =>
        location.TryGetCurrentCell(array, index, out var cell) ? CreateValue(cell.Slot) : GetArrayValue(array, index);

    internal double GetArrayNumber(AwkArrayIdentity array, AwkArrayKey index, in AwkArrayUpdateTarget location) =>
        location.TryGetCurrentCell(array, index, out var cell)
            ? ReadArraySlotNumber(in cell.BorrowedSlot) : GetArrayNumber(array, index);

    internal AwkValue GetArrayValue(AwkArrayIdentity array, int ordinal, in AwkArrayUpdateTarget location) =>
        CreateValue(GetArrayOrdinalSlot(array, ordinal, in location));

    internal double GetArrayNumber(AwkArrayIdentity array, int ordinal, in AwkArrayUpdateTarget location) =>
        ReadArraySlotNumber(GetArrayOrdinalSlot(array, ordinal, in location));

    private AwkValue GetArrayOrdinalSlot(AwkArrayIdentity array, int ordinal, in AwkArrayUpdateTarget location)
    {
        ThrowIfDetached(array);
        if (array.TryGetIndexed(ordinal, out var slot)) return slot;
        var key = AwkArrayKey.FromOrdinal(ordinal);
        if (!array.IsIndexed && (location.TryGetCurrentCell(array, key, out var cell) ||
            array.TryGetDictionaryCell(key, out cell))) return cell.Slot;
        return CreateMissingArraySlot(array, key, out _);
    }

    internal AwkValue GetArrayValue(AwkArrayIdentity array, in AwkCopySource index, bool input = false) =>
        CreateValue(GetArraySlot(array, in index, input));

    internal double GetArrayNumber(AwkArrayIdentity array, in AwkCopySource index, bool input = false)
    {
        ThrowIfDetached(array);
        var bytes = index.AsSpan();
        if (array.IsIndexed)
        {
            if (array.TryGetIndexed(bytes, out _, out var slot)) return ReadArraySlotNumber(slot);
        }
        else if (array.ByteLookup.TryGetDictionaryCell(bytes, out var cell)) return ReadArraySlotNumber(in cell.BorrowedSlot);
        return ReadArraySlotNumber(InsertByteSlot(array, in index, default, missing: true, out _, out _, input));
    }

    private AwkValue GetArraySlot(AwkArrayIdentity array, in AwkCopySource index, bool input)
    {
        ThrowIfDetached(array);
        return array.ByteLookup.TryGetValue(index.AsSpan(), out var slot)
            ? slot : InsertByteSlot(array, in index, default, missing: true, out _, out _, input);
    }

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, in AwkCopySource index,
        in AwkArrayUpdateTarget previous = default, bool input = false)
    {
        // Reuse only the location. The current cell supplies the observed number.
        var bytes = index.AsSpan();
        if (previous.TryGetCurrentCell(array, bytes, out var prior) && prior.Slot.TryGetNumberValue(out var number))
            return new(previous.Key.WithInputOrigin(input), number, array, prior);

        ThrowIfDetached(array);
        AwkArrayKey key;
        AwkValue slot = default;
        ref readonly var numberSource = ref slot;
        AwkArrayCell? cell;
        if (array.IsIndexed)
        {
            cell = null;
            if (array.TryGetIndexed(bytes, out var ordinal, out slot)) key = AwkArrayKey.FromOrdinal(ordinal);
            else slot = InsertByteSlot(array, in index, default, missing: true, out key, out cell, input);
        }
        else
        {
            if (array.ByteLookup.TryGetDictionaryCell(bytes, out key, out cell)) numberSource = ref cell.BorrowedSlot;
            else slot = InsertByteSlot(array, in index, default, missing: true, out key, out cell, input);
        }
        return new(key.WithInputOrigin(input), ReadArraySlotNumber(in numberSource), array, cell);
    }

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, AwkArrayKey index,
        in AwkArrayUpdateTarget previous = default)
    {
        // Reuse only the location. The current cell supplies the observed number.
        if (previous.TryGetCurrentCell(array, index, out var prior) && prior.Slot.TryGetNumberValue(out var number))
            return new(index, number, array, prior);

        ThrowIfDetached(array);
        AwkValue slot = default;
        ref readonly var numberSource = ref slot;
        AwkArrayCell? cell;
        if (array.IsIndexed)
        {
            cell = null;
            // A successful indexed read proves canonical spelling. Keep the ordinal through RHS evaluation.
            if (array.TryGetIndexed(index, out var ordinal, out slot))
                index = AwkArrayKey.FromOrdinal(ordinal).WithInputOrigin(index.HasInputOrigin);
            else slot = CreateMissingArraySlot(array, index, out cell);
        }
        else
        {
            if (array.TryGetDictionaryCell(index, out cell)) numberSource = ref cell.BorrowedSlot;
            else slot = CreateMissingArraySlot(array, index, out cell);
        }
        return new(index, ReadArraySlotNumber(in numberSource), array, cell);
    }

    internal double ReadArraySlotNumber(scoped in AwkValue slot)
    {
        if (!slot.TryGetNumberValue(out var number))
            return slot.ReadVariableNumber(_state.NumberParsingMode, _state.CancellationCheckpoint);

        return number;
    }

    private AwkValue CreateMissingArrayValue(AwkArrayIdentity array, AwkArrayKey index) =>
        CreateValue(CreateMissingArraySlot(array, index, out _));

    private AwkValue CreateMissingArraySlot(AwkArrayIdentity array, AwkArrayKey index, out AwkArrayCell? cell)
    {
        using var admission = _state.MemoryAccounting.AdmitMutation(EstimateArrayInsertionGrowth(array, index,
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
        SetArraySlot(array, index, AwkValue.ForVariableStorage(value, _state.NumberParsingMode));
    }

    internal void SetArrayOrdinalValue(AwkArrayIdentity array, int ordinal, AwkValue value)
    {
        ThrowIfDetached(array);
        var slot = AwkValue.ForVariableStorage(value, _state.NumberParsingMode);
        if (array.TryGetIndexed(ordinal, out var prior))
            StoreArraySlot(array, prior, null, ordinal, slot);
        else if (array.TryPlanIndexedInsertion(ordinal, out var capacity))
        {
            var allocation = _state.MemoryAccounting.IsEnabled ? array.EstimateIndexedInsertionBackingBytes(capacity) : 0;
            using var admission = allocation == 0 ? default : _state.MemoryAccounting.AdmitMutation(
                AwkRuntimeMemory.CheckedAdd(allocation, slot.RetainedTextMemoryBytes));
            InsertIndexedArraySlot(array, ordinal, capacity, 0, slot);
        }
        else SetArraySlot(array, AwkArrayKey.FromOrdinal(ordinal), slot);
    }

    internal void InsertArrayValue(AwkArrayIdentity array, in AwkCopySource index, AwkValue value, bool input = false) =>
        InsertByteSlot(array, in index, AwkValue.ForVariableStorage(value, _state.NumberParsingMode), missing: false, out _, out _, input);

    // Admission covers retained key construction and its actual storage form.
    private AwkValue InsertByteSlot(AwkArrayIdentity array, in AwkCopySource index,
        AwkValue slot, bool missing, out AwkArrayKey key, out AwkArrayCell? cell, bool input)
    {
        ThrowIfDetached(array);
        using var admission = _state.MemoryAccounting.AdmitMutation(_state.MemoryAccounting.IsEnabled
            ? EstimateArrayInsertionGrowth(array, index.AsSpan(), AwkRuntimeMemory.CheckedAdd(
                AwkArrayKey.EstimateRetainedTextBytes(index.Length),
                AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayCellBytes,
                    missing ? AwkRuntimeMemory.EstimateUntypedScalarBytes() : slot.RetainedTextMemoryBytes))) : 0);
        key = _state.CopyArrayKey(in index)
            .WithInputOrigin(input);
        _state.ThrowIfCancellationRequested();
        if (missing) slot = AwkValue.FromUntypedScalar(new AwkUntypedScalar(array));
        cell = InsertAdmittedArraySlot(array, key, slot);
        return slot;
    }

    internal void SetLocatedArrayValue(AwkArrayIdentity array, AwkArrayKey key,
        AwkValue prior, AwkArrayCell? cell, AwkValue value)
    {
        ThrowIfDetached(array);
        var ordinal = cell is null && key.TryGetOrdinalValue(out var index) ? index : 0;
        StoreArraySlot(array, prior, cell, ordinal, AwkValue.ForVariableStorage(value, _state.NumberParsingMode));
    }

    internal void SetCapturedArrayNumber(AwkArrayIdentity array, in AwkArrayUpdateTarget target, double number)
    {
        // The commit caller has matched the captured target to its live owner.
        System.Diagnostics.Debug.Assert(ReferenceEquals(array, target.Array) && !array.IsDetached);
        var value = AwkValue.FromNumber(number);
        if (target.Cell is { IsAttached: true } cell)
        {
            if (cell.Slot.StoredKind == AwkValueKind.Number)
                cell.SetSlot(value);
            else StoreArraySlot(array, cell.Slot, cell, 0, value);
        }
        else SetArraySlot(array, target.Key, value);
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
            _state.AssignFieldCountValue(AwkValue.FromText(value, AwkNumberParsingMode.Decimal, _state.CancellationCheckpoint));
            return;
        }
        SetScalarSlot(name, AwkValue.FromDeferredText(AwkText.FromString(value), _state.NumberParsingMode));
    }

    public void SetScalarValue(AwkVariableName name, AwkValue value)
    {
        if (IsComputedFieldCount(name))
        {
            _state.AssignFieldCountValue(value);
            return;
        }
        SetScalarSlot(name, AwkValue.ForVariableStorage(value, _state.NumberParsingMode));
    }

    private bool IsComputedFieldCount(AwkVariableName name) =>
        _governScalarMemory && name == FieldCountName;

    public long EstimateScalarValueMemoryGrowth(AwkVariableName name, AwkValue value) =>
        EstimateScalarSlotMemoryGrowth(name, AwkValue.ForVariableStorage(value, _state.NumberParsingMode));

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
            ? _state.MemoryAccounting.AdmitMutation(AwkRuntimeMemory.EstimateUntypedScalarInsertionBytes(name.Text.Length))
            : default;
        var slot = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
        SetScalarSlot(name, slot);
        return CreateValue(slot);
    }

    public AwkValue GetOrCreateScalarValue(AwkVariableName name)
    {
        if (name == FieldCountName)
            return _state.RecordFields.GetFieldCountValue();

        if (_scalarSlotsByName.TryGetValue(name, out var id))
            return CreateValue(GetScalarCell(id).State.Slot).ForceScalar();

        SetScalarSlot(name, AwkValue.Unassigned());
        return AwkValue.Unassigned();

    }

    internal AwkValue GetScalarValue(AwkScalarCell cell)
    {
        if (IsFieldCountCell(cell))
            return _state.RecordFields.GetFieldCountValue();

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

    public void SetScalarValue(AwkGlobalSlotId id, AwkValue value) => SetScalarValue(GetScalarCell(id), value);

    internal void SetScalarValue(AwkScalarCell cell, AwkValue value)
    {
        if (IsFieldCountCell(cell))
        {
            _state.AssignFieldCountValue(value);
            return;
        }

        SetScalarSlot(cell, AwkValue.ForVariableStorage(value, _state.NumberParsingMode));
    }

    internal bool TrySetConsumedScalarValue(AwkScalarCell cell, AwkValue value,
        in AwkRegionStorage.ScalarSource source)
    {
        // Computed NF and bootstrap storage retain their ordinary assignment owner.
        if (!_governScalarMemory || IsFieldCountCell(cell)) return false;
        if (!source.IsText)
            value = AwkValue.ForVariableStorage(value, _state.NumberParsingMode);
        else System.Diagnostics.Debug.Assert(value.StoredKind ==
            AwkValue.FromDeferredText(AwkText.Empty, _state.NumberParsingMode).StoredKind);
        StoreScalarSlot(cell, value, IsOutputFieldSeparatorCell(cell), in source);
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
        var number = prior.TryGetNumberValue(out var numeric) ? numeric : CreateValue(prior).AsNumber(_state.CancellationCheckpoint);
        SetScalarCellNumber(cell, Math.Truncate(number) + 1);
    }

    public void InitializeArrayValues(AwkArrayName name, IReadOnlyList<KeyValuePair<AwkArrayKey, AwkValue>> values)
    {
        var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(values.Count);
        foreach (var pair in values)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                EstimateElementBytes(pair.Key, AwkValue.ForVariableStorage(pair.Value, _state.NumberParsingMode)));
        }
        using var allocationPeak = _state.MemoryAccounting.ReserveTransient(bytes);
        var slots = CreateArraySlots(values.Count);
        foreach (var pair in values)
            slots[pair.Key] = AwkValue.ForVariableStorage(pair.Value, _state.NumberParsingMode);
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

        using var allocationPeak = _state.MemoryAccounting.ReserveTransient(prospectiveBytes);
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
        using var allocationPeak = _state.MemoryAccounting.ReserveTransient(prospectiveBytes);
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
            slots[new AwkArrayKey(keyText, _state.CancellationCheckpoint)] = AwkValue.ForStoredInputText(valueText);
        }

        ReplaceArraySlots(name, slots);
    }

    public void InitializeArrayStringNames(
        AwkArrayName name,
        int count,
        IEnumerable<string> names)
    {
        using var construction = _state.MemoryAccounting.ReserveTransient(
            AwkRuntimeMemory.EstimateArrayConstructionBytes(AwkRuntimeMemory.BoundArrayCapacity(count), count));
        var slots = CreateArraySlots(count);
        foreach (var value in names)
        {
            var text = AwkText.FromString(value, _state.CancellationCheckpoint);
            slots[new AwkArrayKey(text, _state.CancellationCheckpoint)] = AwkValue.FromDeferredText(text, _state.NumberParsingMode);
        }

        ReplaceArraySlots(name, slots);
    }

    public AwkValue GetArrayValue(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            return _symbolTable.GetValue(_state.DecodeArrayKey(index));

        var values = GetOrCreateArray(name);
        if (values.TryGetValue(index, out var slot))
            return CreateValue(slot);

        if (!name.IsFunctionLocal && AwkSpecialArrayPolicy.IsElementReadOnly(name.Text, _state.CompatibilityMode))
            throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(name.Text, _state.DecodeArrayKey(index)));
        return CreateMissingArrayValue(_arrays.ResolveArray(name), index);
    }

    public bool TryPeekArrayValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            value = _symbolTable.GetValue(_state.DecodeArrayKey(index));
            return true;
        }

        if (!name.IsFunctionLocal && ContainsScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        var values = GetOrCreateArray(name);
        if (!values.TryGetValue(index, out var slot))
        {
            if (!name.IsFunctionLocal && AwkSpecialArrayPolicy.IsElementReadOnly(name.Text, _state.CompatibilityMode))
                throw new AwkRuntimeException(AwkMessages.FormatUninitializedSpecialArrayElement(name.Text, _state.DecodeArrayKey(index)));
            value = AwkValue.FromText(string.Empty);
            return false;
        }

        value = CreateValue(slot);
        return true;
    }

    public bool ContainsArrayValue(AwkArrayName name, AwkArrayKey index) =>
        !name.IsFunctionLocal && IsSymbolTable(name.Text)
            ? _symbolTable.ContainsValue(_state.DecodeArrayKey(index))
            : GetOrCreateArray(name).ContainsKey(index);

    public bool ContainsArrayValue(AwkArrayName name, ReadOnlySpan<byte> index)
    {
        if (name.IsFunctionLocal || !IsSymbolTable(name.Text))
            return GetOrCreateArray(name).ByteLookup.ContainsKey(index);

        var decoded = AwkUtf8Codec.DecodeRuntimeText(index);
        _state.Counters.Add(AwkCounter.StringDecodes);
        return _symbolTable.ContainsValue(decoded);
    }

    public void SetArrayValue(AwkArrayName name, AwkArrayKey index, AwkValue value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            _symbolTable.AssignValue(_state.DecodeArrayKey(index), value);
            return;
        }

        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _state.CompatibilityMode);
        var values = GetOrCreateArray(name);
        SetArraySlot(values, index, AwkValue.ForVariableStorage(value, _state.NumberParsingMode));
    }

    public long EstimateArrayValueMemoryGrowth(AwkArrayName name, AwkArrayKey index, AwkValue value)
    {
        AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _state.CompatibilityMode);
        var slot = AwkValue.ForVariableStorage(value, _state.NumberParsingMode);
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
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _state.CompatibilityMode);
        _ = GetOrCreateArray(name);
        DeleteArrayValue(_arrays.ResolveArray(name), index);
    }

    public bool TryGetExistingArrayValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
        {
            value = _symbolTable.GetValue(_state.DecodeArrayKey(index));
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
            ? _symbolTable.GetElementRuntimeKind(_state.DecodeArrayKey(index))
            : GetArrayValue(name, index).RuntimeKind;

    public AwkMemoryMutationAdmission PreflightArrayElementRead(AwkArrayName name, AwkArrayKey index)
    {
        if (!name.IsFunctionLocal &&
            (IsSymbolTable(name.Text) || AwkSpecialArrayPolicy.IsElementReadOnly(name.Text, _state.CompatibilityMode)))
            return default;

        if (_arrays.TryGetArray(name, out var slots))
        {
            if (slots.ContainsKey(index))
                return default;
        }
        return _state.MemoryAccounting.AdmitMutation(EstimateArrayInsertionGrowth(slots, index,
            AwkRuntimeMemory.EstimateUntypedArrayElementBytes(index) - AwkRuntimeMemory.EstimateArrayBackingBytes(1)));
    }

    internal void SetSplitArrayValues(
        AwkArrayTarget fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayTarget separatorArrayName,
        IReadOnlyList<AwkCapturedSeparator> separators,
        long fieldBytes)
    {
        ValidateArrayOverlap(fieldArrayName, separatorArrayName, allowSame: false);
        if (fieldArrayName.IsNamed && !fieldArrayName.Name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(fieldArrayName.Name.Text, _state.CompatibilityMode);
        if (separatorArrayName.IsNamed && !separatorArrayName.Name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(separatorArrayName.Name.Text, _state.CompatibilityMode);
        using var construction = _state.MemoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
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
            for (var index = 0; index < separators.Count; index++)
            {
                if ((index & 63) == 0) _state.ThrowIfCancellationRequested();
                var separator = separators[index];
                slots[AwkArrayKey.FromOrdinal(separator.FieldIndex)] = AwkValue.ForStoredInputText(separator.Text);
            }
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
            var newBytes = _state.MemoryAccounting.IsEnabled
                ? AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes, fieldBytes),
                    EstimateOwnedArrayBytes(secondSlots))
                : 0;
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
            AwkSpecialArrayPolicy.ThrowIfReplacementForbidden(name.Text, _state.CompatibilityMode);
        ReplaceArraySlots(name, CreateArraySlots(0));
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
                _state.ThrowIfCancellationRequested();
            snapshot[writeIndex++] = project(state, pair.Key, CreateValue(pair.Value));
        }
        _state.ThrowIfCancellationRequested();

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
            return _state.MemoryAccounting.IsEnabled
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
        if (!_state.MemoryAccounting.IsEnabled)
            return new AwkArraySnapshotSize(count, 0);

        // Indexed owners enumerate ordinals, which retain no text in an unsorted key snapshot.
        if (slots.IsIndexed && !materializeKeys && !retainValues)
        {
            _state.ThrowIfCancellationRequested();
            return new AwkArraySnapshotSize(count, 0);
        }

        var keyTextBytes = 0L;
        var valueTextBytes = 0L;
        var visited = 0;
        foreach (var pair in slots)
        {
            if ((visited++ & 63) == 0)
                _state.ThrowIfCancellationRequested();
            var key = pair.Key;
            keyTextBytes = AwkRuntimeMemory.CheckedAdd(keyTextBytes,
                key.RequiresTextMaterialization && materializeKeys
                    ? AwkRuntimeMemory.EstimateTextBytes(key.ByteLength) : key.RetainedTextMemoryBytes);
            if (retainValues)
                valueTextBytes = AwkRuntimeMemory.CheckedAdd(valueTextBytes, pair.Value.RetainedTextMemoryBytes);
        }
        _state.ThrowIfCancellationRequested();
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
                AwkArrayIdentity.IndexedPageCount(values.Count), _state.CancellationCheckpoint);
            for (var index = 0; index < values.Count; index++)
            {
                if ((index & 63) == 0) _state.ThrowIfCancellationRequested();
                cells[index >> AwkArrayIdentity.IndexedPageShift][index & AwkArrayIdentity.IndexedPageMask] =
                    createSlot(values[index], _state.NumberParsingMode);
            }
            _state.ThrowIfCancellationRequested();
            return AwkArrayIdentity.FromIndexed(cells, values.Count, _arrayKeyComparer, _state.CancellationCheckpoint);
        }
        var destination = CreateArraySlots(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            var key = AwkArrayKey.FromOrdinal(index + firstIndex);
            destination[key] = createSlot(values[index], _state.NumberParsingMode);
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
        foreach (var pair in slots)
        {
            if ((writeIndex & 63) == 0)
                _state.ThrowIfCancellationRequested();
            destination[writeIndex++] = pair.Key;
        }
        _state.ThrowIfCancellationRequested();
    }

    public void RemoveFunctionLocalArray(AwkArrayName name)
    {
        if (!name.IsFunctionLocal || !_arrays.TryGetArray(name, out var array))
            return;

        var oldBytes = _state.MemoryAccounting.IsEnabled
            ? AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes, EstimateArrayBytes(array)) : 0;
        _arrays.Remove(name, out _);
        RetireArrayContents(array, includeRoot: true, cancellable: false);
        _state.MemoryAccounting.Refund(oldBytes);
    }

    internal AwkValue CreateValue(AwkValue slot) =>
        slot.ReadVariableStorage(_state.NumberParsingMode, _state.CancellationCheckpoint);

    // Cells stay valid when admission or OFS preparation grows the name/index tables.
    internal AwkScalarCell GetScalarCell(AwkGlobalSlotId id) => _scalarSlots[id.Value];

    internal AwkScalarCell? GetSpecialScalarCell(AwkSpecialVariableId id) => _specialScalarCells[(int)id];

    private bool IsFieldCountCell(AwkScalarCell cell) =>
        ReferenceEquals(cell, GetSpecialScalarCell(AwkSpecialVariableId.FieldCount));

    private bool IsOutputFieldSeparatorCell(AwkScalarCell cell) =>
        ReferenceEquals(cell, GetSpecialScalarCell(AwkSpecialVariableId.OutputFieldSeparator));

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
        if (_state.MemoryAccounting.IsEnabled) _state.ThrowIfCancellationRequested();
        _scalarSlotsByName.Add(name, id);
        var cell = new AwkScalarCell(
            name,
            _governScalarMemory
                ? AwkScalarState.Governed(slot, slotBytes)
                : AwkScalarState.Ungoverned(slot));
        _scalarSlots.Add(cell);
        if (AwkSpecialVariables.TryGetScalar(name, out var special) &&
            AwkCompatibilityNames.EnablesSpecial(special, _state.CompatibilityMode))
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
        StoreScalarSlot(cell, slot, _governScalarMemory && IsOutputFieldSeparatorCell(cell));

    private void StoreScalarSlot(AwkScalarCell cell, AwkValue slot, bool prepareOutputSeparator,
        in AwkRegionStorage.ScalarSource source = default)
    {
        // A number overwrite retains its owner in either memory mode.
        if (slot.TryGetNumberValue(out var number) &&
            cell.State.Slot.StoredKind == AwkValueKind.Number)
        {
            if (prepareOutputSeparator)
                _state.RecordFields.PrepareOutputFieldSeparatorAssignment();
            source.Transfer(0);
            cell.State.Slot = AwkValue.FromNumber(number);
            return;
        }

        if (!_governScalarMemory || !_state.MemoryAccounting.IsEnabled)
        {
            if (prepareOutputSeparator)
                _state.RecordFields.PrepareOutputFieldSeparatorAssignment();
            source.Transfer(0);
            cell.State.SetUngovernedSlot(slot);
            return;
        }

        var oldCharge = cell.State.ChargedBytes;
        var baselineBytes = cell.State.BaselineBytes;
        var baseline = cell.State.HasBaseline
            ? baselineBytes
            : 0;
        var newCharge = Math.Max(0, EstimateScalarBytes(slot) - baseline);
        var sourceBytes = source.Credit(in slot);
        var credited = AwkRuntimeMemory.CheckedAdd(oldCharge, sourceBytes);
        using var mutation = ReserveMemoryMutation(newCharge, credited);
        if (newCharge > credited) _state.ThrowIfCancellationRequested();
        if (prepareOutputSeparator)
            _state.RecordFields.PrepareOutputFieldSeparatorAssignment();
        source.Transfer(sourceBytes);
        cell.State.SetSlot(slot, baselineBytes, newCharge);
        mutation.Commit();
    }

    private void SetArraySlot(AwkArrayIdentity values, AwkArrayKey index, AwkValue slot)
    {
        if (values.TryGetEntry(index, out var prior, out var cell, out var ordinal))
            StoreArraySlot(values, prior, cell, ordinal, slot);
        else InsertArraySlot(values, index, slot);
    }

    private void InsertArraySlot(AwkArrayIdentity values, AwkArrayKey index, AwkValue slot)
    {
        var payload = _state.MemoryAccounting.IsEnabled ? AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot) : 0;
        var peak = _state.MemoryAccounting.IsEnabled ? EstimateArrayInsertionGrowth(values, index, payload) : 0;
        using var admission = peak > payload ? _state.MemoryAccounting.AdmitMutation(peak) : default;
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
        if (!_state.MemoryAccounting.IsEnabled) return values.AddDictionarySlot(index, slot);
        var oldCapacity = values.EnsureCapacity(0);
        if (values.Count == oldCapacity) values.EnsureCapacity(AwkRuntimeMemory.PlanArrayCapacity(oldCapacity));
        using var mutation = ReserveMemoryMutation(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(values.EnsureCapacity(0)),
                AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot)),
            AwkRuntimeMemory.EstimateArrayBackingBytes(oldCapacity));
        _state.ThrowIfCancellationRequested();
        var cell = values.AddDictionarySlot(index, slot);
        mutation.Commit();
        return cell;
    }

    private void InsertIndexedArraySlot(AwkArrayIdentity values, int ordinal, int capacity,
        long keyBytes, AwkValue slot)
    {
        var governed = _state.MemoryAccounting.IsEnabled;
        long oldBacking = 0, newBytes = 0;
        if (governed)
        {
            newBytes = slot.RetainedTextMemoryBytes;
            if (keyBytes != 0) newBytes = AwkRuntimeMemory.CheckedAdd(keyBytes, newBytes);
            if (values.NeedsIndexedCapacity(capacity))
            {
                oldBacking = values.BackingBytes;
                newBytes = AwkRuntimeMemory.CheckedAdd(values.EstimateIndexedBackingBytes(capacity), newBytes);
            }
        }
        using var mutation = ReserveMemoryMutation(newBytes, oldBacking);
        var slots = values.PrepareIndexedCapacity(capacity, _state.CancellationCheckpoint);
        if (governed || slots.Replacement is not null) _state.ThrowIfCancellationRequested();
        values.PublishIndexedInsertion(in slots, ordinal, slot, _state.CancellationCheckpoint);
        mutation.Commit();
    }

    private AwkArrayCell PromoteAndInsertArraySlot(AwkArrayIdentity values, AwkArrayKey index, AwkValue slot)
    {
        var oldBacking = values.BackingBytes;
        var cellBytes = AwkRuntimeMemory.CheckedMultiply(values.Count, AwkRuntimeMemory.ArrayCellBytes);
        var dictionary = values.BuildDictionary(Math.Max(3, values.Count + 1), _state.CancellationCheckpoint);
        using var mutation = ReserveMemoryMutation(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateArrayBackingBytes(dictionary.EnsureCapacity(0)), cellBytes),
            AwkRuntimeMemory.EstimateArrayElementPayloadBytes(index, slot)), oldBacking);
        _state.ThrowIfCancellationRequested();
        var cell = new AwkArrayCell(slot);
        dictionary.Add(index.PrepareByteLookup(), cell);
        values.PublishDictionary(dictionary);
        mutation.Commit();
        return cell;
    }

    private void StoreArraySlot(AwkArrayIdentity array, AwkValue prior, AwkArrayCell? cell, int ordinal, AwkValue slot)
    {
        if (prior.TryGetArrayBinding(out _))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar("array element"));
        if (prior.StoredKind == AwkValueKind.Number && slot.TryGetNumberValue(out var number))
        {
            Publish(AwkValue.FromNumber(number));
            return;
        }
        using var mutation = ReserveMemoryMutation(slot.RetainedTextMemoryBytes, prior.RetainedTextMemoryBytes);
        _state.ThrowIfCancellationRequested();
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
            else array.SetIndexedValue(ordinal, value);
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
        ReplaceArraySlots(new AwkArrayTarget(name), slots, AwkMemoryLease.Disabled, transferredBytes: 0);

    private void ReplaceArraySlots(
        AwkArrayTarget target,
        AwkArrayIdentity slots,
        AwkMemoryLease sourceMemoryLease,
        long transferredBytes,
        long? measuredContentsBytes = null)
    {
        AwkArrayIdentity? existing;
        if (target.IsNamed)
            _arrays.TryGetArray(target.Name, out existing);
        else
        {
            if (!target.TryGetArray(out existing))
                throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
            ThrowIfDetached(existing);
        }
        var rootBytes = target.IsNamed ? AwkRuntimeMemory.ArrayIdentityBytes : 0;
        long oldBytes = 0, newBytes = 0;
        if (_state.MemoryAccounting.IsEnabled)
        {
            if (existing is not null)
                oldBytes = AwkRuntimeMemory.CheckedAdd(rootBytes, EstimateArrayBytes(existing, _state.CancellationCheckpoint));
            newBytes = AwkRuntimeMemory.CheckedAdd(rootBytes,
                measuredContentsBytes ?? EstimateArrayBytes(slots, _state.CancellationCheckpoint));
        }
        var creditedBytes = AwkRuntimeMemory.CheckedAdd(oldBytes, transferredBytes);
        using var mutation = ReserveMemoryMutation(newBytes, creditedBytes);
        if (existing is not null) RetireArrayContents(existing, includeRoot: false);
        else _state.ThrowIfCancellationRequested();
        PublishArraySlots(target, slots);
        mutation.Commit();
        sourceMemoryLease.Transfer(transferredBytes);
    }

    private AwkMemoryMutationLease ReserveMemoryMutation(long newBytes, long oldBytes) =>
        new(_state.MemoryAccounting, newBytes, oldBytes);

    private struct AwkMemoryMutationLease : IDisposable
    {
        private readonly AwkMemoryAccounting _accounting;
        private readonly long _growth;
        private bool _published;

        public AwkMemoryMutationLease(AwkMemoryAccounting accounting, long newBytes, long oldBytes)
        {
            _accounting = accounting;
            _growth = newBytes - oldBytes;
            _published = false;
            accounting.Charge(_growth);
        }

        public void Commit()
        {
            if (_published) return;
            _accounting.Refund(-_growth);
            _published = true;
        }

        public void Dispose()
        {
            if (!_published) _accounting.Refund(_growth);
        }
    }

    internal static long EstimateArrayBytes(AwkArrayIdentity array,
        Action? checkpoint = null)
    {
        // Only nested arrays need a worklist; script-controlled depth must not recurse
        // through the CLR stack. Its reference metadata is minor beside admitted owners.
        Stack<AwkArrayIdentity>? pending = null;
        var bytes = 0L;
        var visited = 0;
        while (true)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, array.BackingBytes);
            if (!array.IsIndexed)
                bytes = AwkRuntimeMemory.CheckedAdd(bytes,
                    AwkRuntimeMemory.CheckedMultiply(array.Count, AwkRuntimeMemory.ArrayCellBytes));
            using var entries = array.GetEnumerator(checkpoint);
            while (entries.MoveNext())
            {
                if ((visited++ & 63) == 0) checkpoint?.Invoke();
                var pair = entries.Current;
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateArrayPayloadBytes(pair.Key, pair.Value));
                if (pair.Value.TryGetArrayBinding(out var child))
                {
                    bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.ArrayIdentityBytes);
                    (pending ??= new()).Push(child);
                }
            }
            if (pending is null || !pending.TryPop(out var next)) break;
            array = next;
        }
        return bytes;
    }

    private static long EstimateElementBytes(AwkArrayKey key, AwkValue slot) =>
        AwkRuntimeMemory.EstimateArrayElementBytes(key, slot);

    private static long EstimateScalarBytes(AwkValue slot) =>
        AwkRuntimeMemory.EstimateStoredValueBytes(slot);

    private long EstimateOwnedArrayBytes(AwkArrayIdentity slots) =>
        _state.MemoryAccounting.IsEnabled ? AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes,
            EstimateArrayBytes(slots, _state.CancellationCheckpoint)) : 0;

}
