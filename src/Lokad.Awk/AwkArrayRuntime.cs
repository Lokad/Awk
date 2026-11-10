using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed partial class AwkArrayRuntime
{
    private readonly struct AwkArrayReservation(int count, AwkMemoryLease? lease, long keyTextBytes, bool materializeKeys) : IDisposable
    {
        public int Count { get; } = count;

        public long KeyTextBytes { get; } = keyTextBytes;

        public bool MaterializeKeys { get; } = materializeKeys;

        public AwkMemoryLease Lease => lease ??
            throw new InvalidOperationException("The default array reservation has no memory lease.");

        public void Dispose() => lease?.Dispose();
    }

    private static readonly AwkArrayName ProcInfoArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ProcInfo));

    private static readonly AwkArrayKey ProcInfoSortedInKey =
        new(AwkText.FromSourceLiteralString("sorted_in"));

    private static readonly AwkArraySortOrder DefaultIndexOrder =
        new AwkArraySortOrder.BuiltinOrder(AwkArrayBuiltinSortOrder.IndexStringAscending);

    private static readonly AwkText MatchStartCaptureKeyPart =
        AwkText.FromSourceLiteralString("start");

    private static readonly AwkText MatchLengthCaptureKeyPart =
        AwkText.FromSourceLiteralString("length");

    private static readonly AwkArraySortEntryComparer IndexStringAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexStringAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer IndexStringDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexStringDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer IndexNumberAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexNumberAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer IndexNumberDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexNumberDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueStringAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueStringAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueStringDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueStringDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueNumberAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueNumberAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueNumberDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueNumberDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueTypeAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueTypeAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueTypeDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueTypeDescending, AwkRuntimeCounters.Disabled);

    private readonly AwkRuntimeState _state;
    private readonly AwkVariables _variables;
    private readonly AwkVariableRuntime _variableRuntime;
    private readonly AwkFieldSplitter _fieldSplitter;

    public AwkArrayRuntime(
        AwkRuntimeState state,
        AwkVariables variables,
        AwkVariableRuntime variableRuntime,
        AwkFieldSplitter fieldSplitter)
    {
        _state = state;
        _variables = variables;
        _variableRuntime = variableRuntime;
        _fieldSplitter = fieldSplitter;
    }

    public AwkText GetByteText(AwkArrayName name, AwkArrayKey index) =>
        _state.FormatStringByteText(GetAwkValue(name, index));

    internal AwkArrayIdentity ResolveOrdinaryArray(AwkArrayName name, AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return array;
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        array = _variables.ResolveOrdinaryArray(resolved);
        _state.BindGlobalArray(binding, resolved);
        return array;
    }

    private AwkArrayTarget ResolveTarget(AwkArrayTarget target)
    {
        if (target.IsNamed)
        {
            if (_state.TryGetBoundArray(target.Name, default, out var bound))
                return new(AwkValue.FromArrayReference(bound));
            return new(_variableRuntime.ResolveOrPromoteArrayName(target.Name));
        }
        if (target.Reference.TryGetArrayName(out var name))
            return _variables.TryGetOrdinaryArray(name, out var global)
                ? new(AwkValue.FromArrayReference(global)) : new(name);
        if (target.TryGetArray(out var array))
        {
            if (array.IsDetached) throw new AwkRuntimeException(AwkMessages.ErrorDetachedArray);
            return target;
        }
        if (target.Reference.TryGetUntypedScalar(out var origin))
        {
            if (origin.IsDetached) throw new AwkRuntimeException(AwkMessages.ErrorDetachedArray);
            return new(AwkValue.FromArrayReference(_variables.PromoteArrayOrigin(origin)));
        }
        throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
    }

    internal AwkArrayIdentity GetSubarray(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return _variables.GetSubarray(array, key);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (!resolved.IsFunctionLocal && _variables.IsSymbolTable(resolved.Text))
        {
            var target = ResolveTarget(new AwkArrayTarget(_variables.GetArrayValue(resolved, key)));
            return target.TryGetArray(out array) ? array : _variables.ResolveOrdinaryArray(target.Name);
        }
        return _variables.GetSubarray(ResolveOrdinaryArray(name, binding), key);
    }

    internal AwkArrayIdentity? FindSubarray(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return _variables.FindSubarray(array, key);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (!resolved.IsFunctionLocal && _variables.IsSymbolTable(resolved.Text)) return GetSubarray(name, key, binding);
        return _variables.FindSubarray(ResolveOrdinaryArray(name, binding), key);
    }

    public AwkValue GetAwkValue(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return _variables.GetArrayValue(array, index);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (_variables.TryGetExistingArrayValue(resolved, index, out var value))
        {
            _state.BindGlobalArray(binding, resolved);
            return value;
        }
        using var admission = _variables.PreflightArrayElementRead(resolved, index);
        var destination = _variableRuntime.ResolveOrPromoteArrayName(name);
        var result = _variables.GetArrayValue(destination, index);
        _state.BindGlobalArray(binding, destination);
        return result;
    }

    public double GetNumber(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding)
    {
        return _state.TryGetBoundArray(name, binding, out var array)
            ? _variables.GetArrayNumber(array, index)
            : GetAwkValue(name, index, binding).AsNumber(_state.CancellationCheckpoint);
    }

    public bool TryGetBorrowedNumber(AwkArrayName name, ReadOnlySpan<byte> index, out double number,
        AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            if (_variables.TryGetArrayKeySlot(array, index, out _, out var slot))
            {
                number = _variables.ReadArraySlotNumber(slot);
                return true;
            }
            number = 0;
            return false;
        }
        var found = TryGetBorrowedAwkValue(name, index, out var value, binding);
        number = found ? value.AsNumber(_state.CancellationCheckpoint) : 0;
        return found;
    }

    public bool TryGetBorrowedAwkValue(AwkArrayName name, ReadOnlySpan<byte> index, out AwkValue value,
        AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return _variables.TryGetArrayValue(array, index, out value);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        var found = _variables.TryGetExistingArrayValue(resolved, index, out value);
        if (found) _state.BindGlobalArray(binding, resolved);
        return found;
    }

    public bool TryGetBorrowedKey(AwkArrayName name, ReadOnlySpan<byte> index, out AwkArrayKey key, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return array.ByteLookup.TryGetValue(index, out key, out _);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        var found = _variables.TryGetExistingArrayKey(resolved, index, out key);
        if (found) _state.BindGlobalArray(binding, resolved);
        return found;
    }

    public bool TryPrepareBorrowedUpdate(AwkArrayName name, ReadOnlySpan<byte> index,
        out AwkArrayUpdateTarget target, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            if (_variables.TryGetArrayKeySlot(array, index, out var key, out var slot))
            {
                target = new(key, _variables.ReadArraySlotNumber(slot), array, slot);
                return true;
            }
            target = default;
            return false;
        }
        return TryPrepareUnboundBorrowedUpdate(name, index, out target, binding);
    }

    private bool TryPrepareUnboundBorrowedUpdate(AwkArrayName name, ReadOnlySpan<byte> index,
        out AwkArrayUpdateTarget target, AwkArrayBinding binding)
    {
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (_variables.TryGetOrdinaryArray(resolved, out var array))
        {
            if (_variables.TryGetArrayKeySlot(array, index, out var key, out var slot))
            {
                _state.BindGlobalArray(binding, resolved);
                target = new(key, _variables.ReadArraySlotNumber(slot), array, slot);
                return true;
            }
        }
        else if (_variables.TryGetExistingArrayKeyValue(resolved, index, out var key, out var value))
        {
            _state.BindGlobalArray(binding, resolved);
            target = new(key, value.AsNumber(_state.CancellationCheckpoint), null);
            return true;
        }
        target = default;
        return false;
    }

    public AwkArrayKey ResolveKey(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        TryGetBorrowedKey(name, builder.AsSpan(), out var key, binding) ? key : CopyKey(builder);

    internal bool TrySetBorrowedAwkValue(AwkArrayName name, ReadOnlySpan<byte> index,
        AwkValue value, AwkArrayBinding binding)
    {
        AwkArrayCell? cell;
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            if (!_variables.TryGetArrayKeySlot(array, index, out _, out cell))
            {
                _variables.InsertArrayValue(array, index, value);
                return true;
            }
        }
        else
        {
            var resolved = _variableRuntime.ResolveArrayReadName(name);
            if (!_variables.TryGetOrdinaryArray(resolved, out array))
            {
                // Special arrays keep their mutation policy and existing key.
                // Materializing a field first could move admission before a
                // required read-only diagnostic.
                if (!_variables.TryGetExistingArrayKey(resolved, index, out var key)) return false;
                _state.ThrowIfCancellationRequested();
                SetAwkValue(name, key, value, binding);
                return true;
            }
            if (!_variables.TryGetArrayKeySlot(array, index, out _, out cell))
            {
                _variables.InsertArrayValue(array, index, value);
                _state.BindGlobalArray(binding, resolved);
                return true;
            }
            _state.BindGlobalArray(binding, resolved);
        }
        // Only the stable cell survives lookup; no borrowed bytes or dictionary
        // entry reference crosses mutation admission or its callbacks.
        _state.ThrowIfCancellationRequested();
        _variables.SetArrayCellValue(array, cell, value);
        return true;
    }

    internal void SetBuilderValue(AwkArrayName name, AwkUtf8Builder builder, AwkValue value, AwkArrayBinding binding)
    {
        if (TrySetBorrowedAwkValue(name, builder.AsSpan(), value, binding)) return;
        var key = CopyKey(builder);
        _state.ThrowIfCancellationRequested();
        SetAwkValue(name, key, value, binding);
    }

    public AwkValue GetAwkValue(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        TryGetBorrowedAwkValue(name, builder.AsSpan(), out var value, binding) ? value : GetAwkValue(name, CopyKey(builder), binding);

    public double GetNumber(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        TryGetBorrowedNumber(name, builder.AsSpan(), out var number, binding) ? number : GetNumber(name, CopyKey(builder), binding);

    public AwkArrayUpdateTarget PrepareUpdate(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        TryPrepareBorrowedUpdate(name, builder.AsSpan(), out var target, binding)
            ? target : PrepareUpdate(name, CopyKey(builder), binding);

    private AwkArrayKey CopyKey(AwkUtf8Builder builder) =>
        new(builder.CopyToAwkText(_state.Counters, _state.CancellationCheckpoint), _state.CancellationCheckpoint);

    public AwkArrayUpdateTarget PrepareUpdate(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return _variables.PrepareArrayUpdate(array, key);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (_variables.TryGetOrdinaryArray(resolved, out array))
        {
            _state.BindGlobalArray(binding, resolved);
            return _variables.PrepareArrayUpdate(array, key);
        }
        // Keep missing-read promotion/admission and special-array behavior in their
        // established order. The already-bound ordinary path above needs no second resolve.
        var previous = GetAwkValue(name, key, binding).AsNumber(_state.CancellationCheckpoint);
        if (!_state.TryGetBoundArray(name, binding, out array))
        {
            resolved = _variableRuntime.ResolveArrayReadName(name);
            _variables.TryGetOrdinaryArray(resolved, out array);
        }
        AwkArrayCell? cell = null;
        if (array is not null) array.Slots.TryGetValue(key, out cell);
        return new(key, previous, array, cell);
    }

    public AwkRuntimeValueKind GetElementRuntimeKind(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return _variables.GetArrayValue(array, index).RuntimeKind;
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        return !resolved.IsFunctionLocal && _variables.IsSymbolTable(resolved.Text)
            ? _variables.GetArrayElementRuntimeKind(resolved, index)
            : GetAwkValue(name, index, binding).RuntimeKind;
    }

    public bool TryPeekAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (_variableRuntime.IsLocalScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        return _variables.TryPeekArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index, out value);
    }

    public bool ContainsValue(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return array.Slots.ContainsKey(index);
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        var found = _variables.ContainsArrayValue(resolved, index);
        _state.BindGlobalArray(binding, resolved);
        return found;
    }

    public bool ContainsValue(AwkArrayName name, ReadOnlySpan<byte> index, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return array.ByteLookup.ContainsKey(index);
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        var found = _variables.ContainsArrayValue(resolved, index);
        _state.BindGlobalArray(binding, resolved);
        return found;
    }

    public void SetString(AwkArrayName name, AwkArrayKey index, string value)
    {
        _variables.SetArrayString(_variableRuntime.ResolveOrPromoteArrayName(name), index, value);
    }

    public void SetAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            _variables.SetArrayValue(array, index, value);
            return;
        }
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        _variables.SetArrayValue(resolved, index, value);
        _state.BindGlobalArray(binding, resolved);
    }

    public void DeleteValue(AwkArrayName name, AwkArrayKey index)
    {
        if (_state.TryGetBoundArray(name, default, out var array))
        {
            _variables.DeleteArrayValue(array, index);
            return;
        }
        _variables.DeleteArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index);
    }

    public int SplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkFieldSeparatorSpec separator,
        AwkOptionalIdentifier separatorArrayName,
        bool csv = false) => SplitIntoArray(input, new AwkArrayTarget(arrayName), separator,
            separatorArrayName.HasValue ? new AwkArrayTarget(new AwkArrayName(separatorArrayName.Value)) : null, csv);

    internal int SplitIntoArray(AwkText input, AwkArrayTarget arrayName, AwkFieldSeparatorSpec separator,
        AwkArrayTarget? separatorArrayName, bool csv = false)
    {
        if (separatorArrayName.HasValue && _state.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSeparatorArrayExtension);
        if (separatorArrayName is { IsNamed: true } other && arrayName.IsNamed && arrayName.Name == other.Name)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);
        var captureMode = separatorArrayName.HasValue
            ? AwkSeparatorCaptureMode.Capture
            : AwkSeparatorCaptureMode.Omit;
        using var splitLease = _state.MemoryAccounting.ReserveTransient(
            csv ? _state.MemoryAccounting.IsEnabled
                    ? AwkCsvFields.EstimateSplitTextMemory(input, _state.CancellationCheckpoint) : 0
                : EstimateSplitBuffers(input.ByteLength, captureMode));
        var result = csv ? AwkCsvFields.SplitText(input, _state.MemoryAccounting, _state.Counters, _state.CancellationCheckpoint)
            : _fieldSplitter.SplitFieldTextsBySeparator(
            input,
            separator,
            captureMode,
            _state.Counters);
        using var publication = _state.MemoryAccounting.AdmitMutation(
            _variables.EstimateSplitPublicationBytes(result.Fields, result.Separators, separatorArrayName.HasValue));
        arrayName = ResolveTarget(arrayName);
        if (separatorArrayName.HasValue)
        {
            _state.Counters.RecordSeparatorCaptureFallback();
            SetSplitArrayValues(
                arrayName,
                result.Fields,
                ResolveTarget(separatorArrayName.Value),
                result.Separators);
        }
        else
        {
            SetValuesFromExternalText(arrayName, result.Fields);
        }

        return result.Fields.Count;
    }

    public int PatsplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkRegexPattern pattern,
        AwkOptionalIdentifier separatorArrayName) => PatsplitIntoArray(input, new AwkArrayTarget(arrayName), pattern,
            separatorArrayName.HasValue ? new AwkArrayTarget(new AwkArrayName(separatorArrayName.Value)) : null);

    internal int PatsplitIntoArray(AwkText input, AwkArrayTarget arrayName, AwkRegexPattern pattern,
        AwkArrayTarget? separatorArrayName)
    {
        if (separatorArrayName is { IsNamed: true } other && arrayName.IsNamed && arrayName.Name == other.Name)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);
        var captureMode = separatorArrayName.HasValue
            ? AwkSeparatorCaptureMode.Capture
            : AwkSeparatorCaptureMode.Omit;
        using var splitLease = _state.MemoryAccounting.ReserveTransient(
            EstimateSplitBuffers(input.ByteLength, captureMode));
        var result = _fieldSplitter.Patsplit(input, pattern, captureMode, _state.Counters);
        using var publication = _state.MemoryAccounting.AdmitMutation(
            _variables.EstimateSplitPublicationBytes(result.Fields, result.Separators, separatorArrayName.HasValue));
        arrayName = ResolveTarget(arrayName);
        if (separatorArrayName.HasValue)
        {
            SetSplitArrayValues(
                arrayName,
                result.Fields,
                ResolveTarget(separatorArrayName.Value),
                result.Separators);
        }
        else
        {
            SetValuesFromExternalText(arrayName, result.Fields);
        }
        return result.Fields.Count;
    }

    private int PublishSortedValues(AwkArrayTarget source, AwkArrayTarget destinationArrayName,
        AwkArraySortEntry[] entries, AwkArrayReservation reservation)
    {
        var destination = ResolveTarget(destinationArrayName);
        _variables.ValidateArrayOverlap(source, destination, allowSame: true);
        long transferredValueTextBytes = 0;
        for (var index = 0; index < entries.Length; index++)
        {
            if ((index & 63) == 0)
                _state.CancellationCheckpoint();
            if (entries[index].Value.TryGetArrayBinding(out var child))
            {
                reservation.Lease.ReserveAdditional(_variables.EstimateSubarrayBytes(child));
                var copy = _variables.CloneSubarray(child);
                entries[index] = entries[index].WithClonedArray(copy, _variables.EstimateSubarrayBytes(copy));
            }
            transferredValueTextBytes = AwkRuntimeMemory.CheckedAdd(
                transferredValueTextBytes, entries[index].TransferredValueTextBytes);
        }
        _state.CancellationCheckpoint();
        _variables.SetArrayValues(
            destination,
            entries,
            static entry => entry.Value.TryGetArrayBinding(out var child)
                ? AwkVariableSlot.FromArrayBinding(child) : AwkVariableSlot.FromValue(entry.Value),
            reservation.Lease,
            transferredValueTextBytes);
        return entries.Length;
    }

    private int PublishSortedKeys(AwkArrayTarget source, AwkArrayTarget destinationArrayName,
        AwkArraySortEntry[] entries, AwkArrayReservation reservation)
    {
        var destination = ResolveTarget(destinationArrayName);
        _variables.ValidateArrayOverlap(source, destination, allowSame: true);
        _variables.SetArrayValues(
            destination,
            entries,
            static entry => AwkVariableSlot.FromText(entry.Key.StoredText),
            reservation.Lease,
            reservation.KeyTextBytes);
        return entries.Length;
    }

    public void Clear(AwkArrayName name)
    {
        if (_state.TryGetBoundArray(name, default, out var array))
        {
            _variables.ClearArray(array);
            return;
        }
        _variables.ClearArray(_variableRuntime.ResolveOrPromoteArrayName(name));
    }

    public void ReplaceMatchCaptures(AwkArrayTarget target, AwkLeasedRegexMatch leasedMatch)
    {
        var groups = leasedMatch.Match.Groups;
        var entryCount = 0;
        for (var index = 0; index < groups.Count; index++)
        {
            entryCount = AwkRuntimeMemory.CheckedAdd(entryCount, groups[index].Success ? 3 : 0);
        }
        var slots = _variables.CreateMutableArraySlots(entryCount);
        if (entryCount != 0)
        {
            var separator = _state.ResolveSubscriptSeparatorText();
            var capacity = AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.CheckedAdd(AwkArrayKey.FromOrdinal(groups.Count - 1).ByteLength, separator.ByteLength),
                MatchLengthCaptureKeyPart.ByteLength);
            using var builder = new AwkUtf8Builder(_state.MemoryAccounting, capacity);

            AwkArrayKey MetadataKey(AwkArrayKey ordinal, AwkText part)
            {
                builder.Clear();
                ordinal.AppendTo(builder, _state.CancellationCheckpoint);
                builder.Append(separator, _state.CancellationCheckpoint);
                builder.Append(part, _state.CancellationCheckpoint);
                return new AwkArrayKey(builder.CopyToAwkText(_state.Counters, _state.CancellationCheckpoint),
                    _state.CancellationCheckpoint);
            }

            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                if (!group.Success)
                    continue;

                var ordinalKey = AwkArrayKey.FromOrdinal(index);
                slots[ordinalKey] = new(AwkVariableSlot.FromValue(_state.CreateStringValue(group.Value)));
                slots[MetadataKey(ordinalKey, MatchStartCaptureKeyPart)] =
                    new(AwkVariableSlot.FromValue(AwkValue.FromNumber(group.Index + 1)));
                slots[MetadataKey(ordinalKey, MatchLengthCaptureKeyPart)] =
                    new(AwkVariableSlot.FromValue(AwkValue.FromNumber(group.Length)));
            }
        }

        // Admit the entire publication before typing a missing child as an array.
        // Existing targets retain the ordinary replacement credits and empty-match path.
        using var publication = !target.IsNamed && !target.TryGetArray(out _)
            ? _state.MemoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.ArrayIdentityBytes, AwkVariables.EstimateArrayBytes(slots, _state.CancellationCheckpoint)))
            : default;
        _variables.ReplaceMutableArraySlots(
            ResolveTarget(target),
            slots,
            leasedMatch.MemoryLease,
            leasedMatch.RetainedTextMemoryBytes);
    }

    private AwkLeasedReadOnlyList<AwkArrayKey> SnapshotUnsortedKeyValues(AwkArrayTarget arrayName)
    {
        var size = _variables.MeasureArraySnapshot(arrayName, materializeKeys: false);
        var count = size.Count;
        var lease = _state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count), size.KeyTextBytes));
        try
        {
            var result = new AwkArrayKey[count];
            _variables.CopyArrayKeys(arrayName, result);

            return new AwkLeasedReadOnlyList<AwkArrayKey>(result, lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private AwkArrayReservation ReserveSortOperation(
        AwkArrayTarget sourceArrayName,
        AwkArraySortOrder order,
        AwkArraySortOutput output,
        bool keys = false)
    {
        // Index orders and user comparators consume language key text repeatedly.
        // asorti publishes it. Value orders/traversal can retain owning keys directly.
        var materializeKeys = keys || order is AwkArraySortOrder.UserFunctionOrder or
            AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexStringAscending or
                AwkArrayBuiltinSortOrder.IndexStringDescending or AwkArrayBuiltinSortOrder.IndexNumberAscending or
                AwkArrayBuiltinSortOrder.IndexNumberDescending };
        var size = _variables.MeasureArraySnapshot(sourceArrayName, materializeKeys, retainValues: true);
        var lease = _state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            EstimateSortOperationBytes(size.Count, order, output), size.TextBytes));
        return new AwkArrayReservation(size.Count, lease, size.KeyTextBytes, materializeKeys);
    }

    private void SetValuesFromExternalText(AwkArrayTarget name, IReadOnlyList<AwkText> values)
    {
        _variables.SetArrayValuesFromExternalText(name, values);
    }

    private void SetSplitArrayValues(
        AwkArrayTarget fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayTarget separatorArrayName,
        IReadOnlyList<AwkCapturedSeparator> separators)
    {
        _variables.SetSplitArrayValues(
            fieldArrayName,
            fields,
            separatorArrayName,
            separators);
    }

    private bool TryResolveSortedInOrder(out AwkArraySortOrder order)
    {
        order = AwkArraySortOrder.Default;
        if (_state.CompatibilityMode != AwkCompatibilityMode.GnuDefault ||
            !_variables.TryPeekArrayValue(
            ProcInfoArrayName,
            ProcInfoSortedInKey,
            out var value))
        {
            return false;
        }

        var text = _state.FormatStringByteText(value);
        if (text.ByteLength == 0)
            return false;

        order = AwkArraySortOrder.Parse(text.AsString(_state.Counters, _state.CancellationCheckpoint));
        return order is not AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.Unsorted };
    }

    private AwkArraySortEntry[] SortEntries(
        AwkArrayTarget arrayName,
        AwkArraySortOrder order,
        AwkArrayReservation reservation)
    {
        static AwkArraySortPlan ResolveSortPlan(
            AwkArraySortOrder sortOrder,
            AwkRuntimeCounters counters)
        {
            var mode = sortOrder switch
            {
                AwkArraySortOrder.DefaultOrder =>
                    AwkArraySortMode.ValueTypeAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexStringAscending } =>
                    AwkArraySortMode.IndexStringAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexStringDescending } =>
                    AwkArraySortMode.IndexStringDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexNumberAscending } =>
                    AwkArraySortMode.IndexNumberAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexNumberDescending } =>
                    AwkArraySortMode.IndexNumberDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueStringAscending } =>
                    AwkArraySortMode.ValueStringAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueStringDescending } =>
                    AwkArraySortMode.ValueStringDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueNumberAscending } =>
                    AwkArraySortMode.ValueNumberAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueNumberDescending } =>
                    AwkArraySortMode.ValueNumberDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueTypeAscending } =>
                    AwkArraySortMode.ValueTypeAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueTypeDescending } =>
                    AwkArraySortMode.ValueTypeDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.Unsorted } =>
                    AwkArraySortMode.Unsorted,
                AwkArraySortOrder.UnknownBuiltinOrder unknown =>
                    throw new AwkRuntimeException(AwkMessages.FormatUndefinedSortComparison(unknown.Text)),
                _ => throw new InvalidOperationException("Unknown awk array sort order.")
            };

            if (counters.IsEnabled)
                return new AwkArraySortPlan(mode, new AwkArraySortEntryComparer(mode, counters));

            var comparer = mode switch
            {
                AwkArraySortMode.IndexStringAscending => IndexStringAscendingComparer,
                AwkArraySortMode.IndexStringDescending => IndexStringDescendingComparer,
                AwkArraySortMode.IndexNumberAscending => IndexNumberAscendingComparer,
                AwkArraySortMode.IndexNumberDescending => IndexNumberDescendingComparer,
                AwkArraySortMode.ValueStringDescending => ValueStringDescendingComparer,
                AwkArraySortMode.ValueNumberAscending => ValueNumberAscendingComparer,
                AwkArraySortMode.ValueNumberDescending => ValueNumberDescendingComparer,
                AwkArraySortMode.ValueTypeAscending => ValueTypeAscendingComparer,
                AwkArraySortMode.ValueTypeDescending => ValueTypeDescendingComparer,
                _ => ValueStringAscendingComparer
            };
            return new AwkArraySortPlan(mode, comparer);
        }

        if (order is AwkArraySortOrder.UserFunctionOrder)
        {
            throw new InvalidOperationException("User-defined awk sort comparators require asynchronous evaluation.");
        }

        var plan = ResolveSortPlan(order, _state.Counters);
        var entries = SnapshotSortEntries(arrayName, plan.Mode, reservation);
        if (plan.Mode == AwkArraySortMode.Unsorted || entries.Length < 2)
            return entries;
        var indices = CreateSortOrder(entries.Length, _state);
        try
        {
            Array.Sort(indices, new AwkArraySortIndexComparer(entries, plan.Comparer, _state));
        }
        catch (InvalidOperationException exception)
            when (exception.InnerException is AwkRuntimeException runtimeException)
        {
            throw runtimeException;
        }
        catch (InvalidOperationException exception)
            when (exception.InnerException is OperationCanceledException cancellationException)
        {
            throw cancellationException;
        }

        ApplySortOrder(entries, indices, _state);
        return entries;
    }

    private sealed class AwkArraySortIndexComparer(
        AwkArraySortEntry[] entries,
        AwkArraySortEntryComparer comparer,
        AwkRuntimeState state) : IComparer<int>
    {
        public int Compare(int left, int right)
        {
            state.Resources.Checkpoint();
            return comparer.Compare(in entries[left], in entries[right], state);
        }
    }

    private static int[] CreateSortOrder(int count, AwkRuntimeState state)
    {
        var indices = new int[count];
        for (var index = 0; index < count; index++)
        {
            if ((index & 63) == 0) state.CancellationCheckpoint();
            indices[index] = index;
        }
        state.CancellationCheckpoint();
        return indices;
    }

    // All comparisons have finished. Move each captured entry once around its
    // cycle; cancellation discards only private staging, before publication.
    private static void ApplySortOrder(AwkArraySortEntry[] entries, int[] indices, AwkRuntimeState state)
    {
        var moved = 0;
        for (var first = 0; first < entries.Length; first++)
        {
            if ((first & 63) == 0) state.CancellationCheckpoint();
            if (indices[first] == first) continue;
            var saved = entries[first];
            var current = first;
            while (true)
            {
                if ((moved++ & 63) == 0) state.CancellationCheckpoint();
                var next = indices[current];
                indices[current] = current;
                if (next == first)
                {
                    entries[current] = saved;
                    break;
                }
                entries[current] = entries[next];
                current = next;
            }
        }
        state.CancellationCheckpoint();
    }

    private readonly struct AwkArraySortPlan
    {
        private readonly AwkArraySortEntryComparer? _comparer;

        public AwkArraySortPlan(
            AwkArraySortMode mode,
            AwkArraySortEntryComparer comparer)
        {
            Mode = mode;
            _comparer = comparer;
        }

        public AwkArraySortMode Mode { get; }

        public AwkArraySortEntryComparer Comparer => _comparer ?? ValueTypeAscendingComparer;
    }

    private AwkArraySortEntry[] SnapshotSortEntries(
        AwkArrayTarget arrayName,
        AwkArraySortMode sortMode,
        AwkArrayReservation reservation)
    {
        static double ResolveNumericSortKey(
            AwkText key,
            AwkValue value,
            AwkArraySortMode sortMode,
            AwkRuntimeState state)
        {
            if (sortMode is AwkArraySortMode.ValueNumberAscending or AwkArraySortMode.ValueNumberDescending)
                return value.IsArrayValue ? 0 : value.AsNumber(state.CancellationCheckpoint);

            if (sortMode is not (AwkArraySortMode.IndexNumberAscending or AwkArraySortMode.IndexNumberDescending))
                return 0;

            return state.CreateStringValue(key).AsNumber(state.CancellationCheckpoint);
        }

        static AwkArraySortEntry Project(
            AwkArraySortEntryProjection projection,
            AwkArrayKey key,
            AwkValue value)
        {
            var runtime = projection.Runtime;
            if (value.TryGetArrayBinding(out var child)) value = AwkValue.FromArrayReference(child);
            else if (projection.Mode != AwkArraySortMode.UserFunction && value.TryGetUntypedScalar(out _))
                value = value.AsScalarCopy();
            // Snapshot ownership survives source mutation, including existing text keys/values.
            if (projection.MaterializeKeys) key = key.WithTextProjection(runtime._state.Counters);
            var stringValueOrder = projection.Mode is AwkArraySortMode.ValueStringAscending or AwkArraySortMode.ValueStringDescending;
            var typeValueOrder = projection.Mode is AwkArraySortMode.ValueTypeAscending or AwkArraySortMode.ValueTypeDescending;
            var valueText = !value.IsArrayValue && (stringValueOrder || typeValueOrder && !value.IsNumberValue)
                    ? runtime._state.FormatStringByteText(value, projection.MemoryLease)
                    : AwkText.Empty;
            // GNU's explicit string-value projection materializes nonfinite numbers as
            // strings in asort's destination. The source and other modes retain their type.
            var materializeNonfinite = stringValueOrder && value.IsNumberValue && !double.IsFinite(value.AsNumber());
            var projectedValue = materializeNonfinite ? runtime._state.CreateStringValue(valueText) : value;
            var valueKind = projectedValue.IsArrayValue ? AwkArraySortValueKind.Array : AwkArraySortValueKind.Scalar;
            var numericKey = ResolveNumericSortKey(key.StoredText, value, projection.Mode, runtime._state);
            if (typeValueOrder && valueKind != AwkArraySortValueKind.Array)
            {
                if (value.HasInputConversionError) valueKind = AwkArraySortValueKind.DeferredError;
                else if (value.TryGetNumber(out var number))
                {
                    valueKind = AwkArraySortValueKind.Number;
                    numericKey = number;
                }
            }
            return new AwkArraySortEntry(
                key,
                projectedValue,
                valueText,
                numericKey,
                projectedValue.RetainedTextMemoryBytes,
                valueKind);
        }

        return _variables.SnapshotArrayValues(
            arrayName,
            reservation.Count,
            new AwkArraySortEntryProjection(this, sortMode, reservation.Lease, reservation.MaterializeKeys),
            Project);
    }

    private AwkArrayKey[] CopyKeyValues(AwkArraySortEntry[] entries)
    {
        var result = new AwkArrayKey[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            if ((index & 63) == 0)
                _state.CancellationCheckpoint();
            result[index] = entries[index].Key;
        }

        _state.CancellationCheckpoint();
        return result;
    }

    private static long EstimateSplitBuffers(
        int inputByteLength,
        AwkSeparatorCaptureMode captureMode)
        => AwkRuntimeMemory.EstimateSplitBufferBytes(
            inputByteLength,
            fieldListCount: 1,
            captureMode);

    private static long EstimateSortOperationBytes(
        int count,
        AwkArraySortOrder order,
        AwkArraySortOutput output)
    {
        var bytes = AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(count);
        if (count >= 2 && order is not AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.Unsorted })
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateInt32ArrayBytes(count));
            if (order is AwkArraySortOrder.UserFunctionOrder)
            {
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.ArrayMergeProgressBytes);
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateInt32ArrayBytes(count));
            }
        }

        if (output is AwkArraySortOutput.KeySnapshot)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(
                bytes,
                AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count));
        }

        return bytes;
    }

    private readonly struct AwkArraySortEntry(
        AwkArrayKey key,
        AwkValue value,
        AwkText valueText,
        double numericKey,
        long transferredValueTextBytes,
        AwkArraySortValueKind valueKind)
    {
        public AwkArrayKey Key => key;

        public AwkValue Value { get; } = value;

        public AwkArraySortValueKind ValueKind { get; } = valueKind;

        public bool IsArray => ValueKind == AwkArraySortValueKind.Array;

        public AwkText ValueText => valueText;

        public double NumericKey { get; } = numericKey;

        public long TransferredValueTextBytes { get; } = transferredValueTextBytes;

        public AwkArraySortEntry WithClonedArray(AwkArrayIdentity array, long retainedBytes) =>
            new(Key, AwkValue.FromArrayReference(array), ValueText, NumericKey, retainedBytes, AwkArraySortValueKind.Array);
    }

    private enum AwkArraySortValueKind : byte
    {
        Scalar,
        Number,
        Array,
        DeferredError = 4
    }

    private readonly struct AwkArraySortEntryProjection(
        AwkArrayRuntime? runtime,
        AwkArraySortMode mode,
        AwkMemoryLease? memoryLease,
        bool materializeKeys)
    {
        public AwkArrayRuntime Runtime => runtime ??
            throw new InvalidOperationException("The default sort projection has no array runtime.");

        public AwkArraySortMode Mode { get; } = mode;

        public bool MaterializeKeys { get; } = materializeKeys;

        public AwkMemoryLease MemoryLease => memoryLease ??
            throw new InvalidOperationException("The default sort projection has no memory lease.");
    }

    private enum AwkArraySortMode
    {
        UserFunction,
        ValueStringAscending,
        ValueStringDescending,
        ValueNumberAscending,
        ValueNumberDescending,
        IndexStringAscending,
        IndexStringDescending,
        IndexNumberAscending,
        IndexNumberDescending,
        ValueTypeAscending,
        ValueTypeDescending,
        Unsorted
    }

    private enum AwkArraySortOutput
    {
        DestinationArray,
        KeySnapshot
    }

    private sealed class AwkArraySortEntryComparer(
        AwkArraySortMode mode,
        AwkRuntimeCounters counters)
    {
        public int Compare(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
        {
            if (mode is AwkArraySortMode.ValueStringAscending or AwkArraySortMode.ValueStringDescending or
                AwkArraySortMode.ValueNumberAscending or AwkArraySortMode.ValueNumberDescending or
                AwkArraySortMode.ValueTypeAscending or AwkArraySortMode.ValueTypeDescending)
            {
                // The independent array bit keeps the common scalar-pair check compact.
                if (((left.ValueKind | right.ValueKind) & AwkArraySortValueKind.Array) != 0)
                {
                    var comparison = left.IsArray.CompareTo(right.IsArray);
                    return mode is AwkArraySortMode.ValueStringDescending or AwkArraySortMode.ValueNumberDescending or
                        AwkArraySortMode.ValueTypeDescending
                        ? ThenCompareKeys(-comparison, right, left) : ThenCompareKeys(comparison, left, right);
                }
            }
            switch (mode)
            {
                case AwkArraySortMode.IndexStringAscending:
                    return CompareKeys(left, right, state);
                case AwkArraySortMode.IndexStringDescending:
                    return CompareKeys(right, left, state);
                case AwkArraySortMode.IndexNumberAscending:
                    return ThenCompareKeys(CompareNumbers(left, right), left, right);
                case AwkArraySortMode.IndexNumberDescending:
                    return ThenCompareKeys(CompareNumbers(right, left), right, left);
                case AwkArraySortMode.ValueStringDescending:
                    return ThenCompareKeys(CompareValues(right, left), right, left);
                case AwkArraySortMode.ValueNumberAscending:
                    return ThenCompareKeys(CompareNumericValues(left, right), left, right);
                case AwkArraySortMode.ValueNumberDescending:
                    return ThenCompareKeys(CompareNumericValues(right, left), right, left);
                case AwkArraySortMode.ValueTypeAscending:
                    return ThenCompareKeys(CompareTypes(left, right), left, right);
                case AwkArraySortMode.ValueTypeDescending:
                    return ThenCompareKeys(CompareTypes(right, left), right, left);
                default:
                    return ThenCompareKeys(CompareValues(left, right), left, right);
            }

            int ThenCompareKeys(
                int comparison,
                in AwkArraySortEntry left,
                in AwkArraySortEntry right) =>
                comparison != 0 ? comparison : CompareKeys(left, right, state);

            int CompareValues(in AwkArraySortEntry left, in AwkArraySortEntry right)
            {
                counters.RecordByteTextComparison();
                return state.CompareByteText(left.ValueText, right.ValueText, sorting: true);
            }

            int CompareNumericValues(in AwkArraySortEntry left, in AwkArraySortEntry right)
            {
                var comparison = CompareNumbers(left, right);
                if (comparison != 0)
                    return comparison;
                // Equal pure integers have the same canonical spelling, including
                // signed zero. Other origins and nonintegers still need their text.
                if (left.Value.IsNumberValue && right.Value.IsNumberValue &&
                    double.IsInteger(left.NumericKey))
                    return 0;
                counters.RecordByteTextComparison();
                using var textLease = state.MemoryAccounting.CreateTransientLease();
                return state.CompareByteText(state.FormatStringByteText(left.Value, textLease),
                    state.FormatStringByteText(right.Value, textLease), sorting: true);
            }

            int CompareTypes(in AwkArraySortEntry left, in AwkArraySortEntry right)
            {
                if (left.ValueKind == AwkArraySortValueKind.DeferredError) left.Value.TryGetNumber(out _);
                if (right.ValueKind == AwkArraySortValueKind.DeferredError) right.Value.TryGetNumber(out _);
                var leftNumeric = left.ValueKind == AwkArraySortValueKind.Number;
                var rightNumeric = right.ValueKind == AwkArraySortValueKind.Number;
                if (leftNumeric && rightNumeric)
                    return CompareNumbers(left, right);
                if (leftNumeric != rightNumeric)
                    return leftNumeric ? -1 : 1;
                return CompareValues(left, right);
            }

            static int CompareNumbers(in AwkArraySortEntry left, in AwkArraySortEntry right) =>
                CompareSortNumbers(left.NumericKey, right.NumericKey);
        }

        private static int CompareSortNumbers(double left, double right) =>
            double.IsNaN(left) ? (double.IsNaN(right) ? 0 : 1) :
            double.IsNaN(right) ? -1 : left.CompareTo(right);

        private int CompareKeys(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
        {
            counters.RecordByteTextComparison();
            if (!left.Key.RequiresTextMaterialization && !right.Key.RequiresTextMaterialization)
                return state.CompareByteText(left.Key.StoredText, right.Key.StoredText, sorting: true);
            Span<byte> leftBuffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
            Span<byte> rightBuffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
            return state.CompareByteText(new AwkByteTextOperand(left.Key.AsSpan(leftBuffer, state.CancellationCheckpoint)),
                new AwkByteTextOperand(right.Key.AsSpan(rightBuffer, state.CancellationCheckpoint)), sorting: true);
        }

    }
}
