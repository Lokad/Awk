namespace Lokad.Awk;

internal sealed partial class AwkVariables
{
    internal Action<AwkArrayIdentity, bool, bool>? RepairSubarrayAliases { private get; set; }
    internal Action<AwkUntypedScalar>? RepairElementAliases { private get; set; }

    internal void ValidateArrayOverlap(AwkArrayTarget first, AwkArrayTarget second, bool allowSame)
    {
        var sameName = first.IsNamed && second.IsNamed && first.Name == second.Name;
        TryGetTargetArray(first, out var firstArray);
        TryGetTargetArray(second, out var secondArray);
        if (sameName || firstArray is not null && ReferenceEquals(firstArray, secondArray))
        {
            if (!allowSame) throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);
            return;
        }
        if (firstArray is { } firstOwner && secondArray is { } secondOwner &&
            (firstOwner.IsWithin(secondOwner) || secondOwner.IsWithin(firstOwner)))
            throw new AwkRuntimeException(AwkMessages.ErrorOverlappingArrays);
    }

    internal long EstimateSubarrayBytes(AwkArrayIdentity array)
    {
        ThrowIfDetached(array);
        return AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes, EstimateArrayBytes(array.Slots, _checkpoint));
    }

    // The caller admits the whole copy under its staging lease. Child arrays and
    // untyped cells are independent; immutable key/text payloads retain their spelling.
    internal AwkArrayIdentity CloneSubarray(AwkArrayIdentity source)
    {
        ThrowIfDetached(source);
        var result = new AwkArrayIdentity(CreateArraySlots(source.Slots.Count));
        var destination = result;
        Stack<(AwkArrayIdentity Source, AwkArrayIdentity Destination)>? pending = null;
        var visited = 0;
        while (true)
        {
            foreach (var pair in source.Slots)
            {
                if ((visited++ & 63) == 0) _checkpoint?.Invoke();
                AwkVariableSlot copy;
                if (pair.Value.TryGetArrayBinding(out var child))
                {
                    var copiedChild = new AwkArrayIdentity(CreateArraySlots(child.Slots.Count), destination);
                    copy = AwkVariableSlot.FromArrayBinding(copiedChild);
                    (pending ??= new()).Push((child, copiedChild));
                }
                else if (pair.Value.TryGetUntypedScalar(out var origin))
                {
                    var copiedOrigin = new AwkUntypedScalar(destination);
                    if (origin.IsScalar) copiedOrigin.Promote();
                    copy = AwkVariableSlot.FromUntypedScalar(copiedOrigin);
                }
                else copy = pair.Value.Slot;
                destination.Slots.Add(pair.Key, new(copy));
            }
            if (pending is null || !pending.TryPop(out var next)) break;
            source = next.Source;
            destination = next.Destination;
        }
        return result;
    }

    internal bool TryGetTargetArray(AwkArrayTarget target,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        if (target.TryGetArray(out array)) return true;
        return _arrays.TryGetArray(target.Name, out array);
    }

    private void PublishArraySlots(AwkArrayTarget target, Dictionary<AwkArrayKey, AwkArrayCell> slots)
    {
        if (target.IsNamed) _arrays.SetSlots(target.Name, slots);
        else if (target.TryGetArray(out var array)) array.ReplaceSlots(slots);
        else throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
    }

    internal long EstimateSplitPublicationBytes(IReadOnlyList<AwkText> fields,
        IReadOnlyList<AwkCapturedSeparator> separators, bool includeSeparators)
    {
        var bytes = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes,
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(fields.Count)));
        for (var index = 0; index < fields.Count; index++)
        {
            if ((index & 63) == 0) _checkpoint?.Invoke();
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateArrayElementPayloadBytes(
                AwkArrayKey.FromOrdinal(index + 1), AwkVariableSlot.FromExternalText(fields[index])));
        }
        if (!includeSeparators) return bytes;
        bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes,
            AwkRuntimeMemory.EstimateArrayBackingBytes(BoundArrayCapacity(separators.Count))));
        foreach (var separator in separators)
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateArrayElementPayloadBytes(
                AwkArrayKey.FromOrdinal(separator.FieldIndex), AwkVariableSlot.FromExternalText(separator.Text)));
        return bytes;
    }

    internal AwkArraySnapshotSize MeasureArraySnapshot(AwkArrayTarget target, bool materializeOrdinalKeys,
        bool retainValues = false) => target.TryGetArray(out var array)
            ? MeasureArraySnapshot(array, materializeOrdinalKeys, retainValues)
            : MeasureArraySnapshot(target.Name, materializeOrdinalKeys, retainValues);

    internal TResult[] SnapshotArrayValues<TState, TResult>(AwkArrayTarget target, int count, TState state,
        Func<TState, AwkArrayKey, AwkValue, TResult> project)
    {
        if (!target.TryGetArray(out var array)) return SnapshotArrayValues(target.Name, count, state, project);
        ThrowIfDetached(array);
        var result = new TResult[count];
        var offset = 0;
        foreach (var pair in array.Slots)
        {
            if ((offset & 63) == 0) _checkpoint?.Invoke();
            result[offset++] = project(state, pair.Key, CreateValue(pair.Value));
        }
        return result;
    }

    internal void CopyArrayKeys(AwkArrayTarget target, AwkArrayKey[] destination)
    {
        if (target.TryGetArray(out var array)) CopyArrayKeys(array, destination);
        else CopyArrayKeys(target.Name, destination);
    }

    internal void SetArrayValues<TValue>(AwkArrayTarget target, IReadOnlyList<TValue> values,
        Func<TValue, AwkVariableSlot> createSlot, AwkMemoryLease sourceMemoryLease, long sourceRetainedTextBytes)
    {
        if (target.IsNamed)
        {
            SetArrayValues(target.Name, values, createSlot, sourceMemoryLease, sourceRetainedTextBytes);
            return;
        }
        using var construction = _memoryAccounting.AdmitMutation(
            AwkRuntimeMemory.EstimateArrayConstructionBytes(BoundArrayCapacity(values.Count), values.Count));
        ReplaceMutableArraySlots(target, CreateIndexedArraySlots(values, 1, createSlot), sourceMemoryLease, sourceRetainedTextBytes);
    }

    internal void SetArrayValuesFromExternalText(AwkArrayTarget target, IReadOnlyList<AwkText> values)
    {
        if (target.IsNamed)
        {
            SetArrayValuesFromExternalText(target.Name, values);
            return;
        }
        using var construction = _memoryAccounting.AdmitMutation(
            AwkRuntimeMemory.EstimateArrayConstructionBytes(BoundArrayCapacity(values.Count), values.Count));
        ReplaceMutableArraySlots(target, CreateIndexedArraySlots(values, 1, AwkVariableSlot.FromExternalText),
            AwkMemoryLease.Disabled, 0);
    }

    internal void ReplaceMutableArraySlots(AwkArrayTarget target, Dictionary<AwkArrayKey, AwkArrayCell> slots,
        AwkMemoryLease sourceMemoryLease, long transferredBytes)
    {
        if (target.IsNamed)
        {
            ReplaceMutableArraySlots(target.Name, slots, sourceMemoryLease, transferredBytes);
            return;
        }
        if (!target.TryGetArray(out var array))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
        ThrowIfDetached(array);
        var oldBytes = EstimateArrayBytes(array.Slots, _checkpoint);
        var newBytes = EstimateArrayBytes(slots, _checkpoint);
        using var mutation = ReserveMemoryMutation(newBytes, AwkRuntimeMemory.CheckedAdd(oldBytes, transferredBytes));
        RetireArrayContents(array, includeRoot: false);
        array.ReplaceSlots(slots);
        mutation.Commit();
        sourceMemoryLease.Transfer(transferredBytes);
    }

    private static void ThrowIfDetached(AwkArrayIdentity array)
    {
        if (array.IsDetached) throw new AwkRuntimeException(AwkMessages.ErrorDetachedArray);
    }

    internal AwkArrayIdentity PromoteArrayOrigin(AwkUntypedScalar origin)
    {
        if (origin.Array is { } existing) return existing;
        if (origin.Parent is not { } parent || origin.IsScalar)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
        ThrowIfDetached(parent);
        using var creation = ReserveMemoryMutation(AwkRuntimeMemory.ArrayIdentityBytes, 0);
        var child = new AwkArrayIdentity(CreateArraySlots(0), parent);
        origin.PromoteArray(child);
        creation.Commit();
        _counters.RecordSubarrayPromotion();
        return child;
    }

    internal AwkArrayIdentity ResolveOrdinaryArray(AwkArrayName name)
    {
        if (!name.IsFunctionLocal && IsSymbolTable(name.Text))
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidSymtabReference);
        if (!name.IsFunctionLocal)
            AwkSpecialArrayPolicy.ThrowIfElementMutationForbidden(name.Text, _mode);
        _ = GetOrCreateArraySlots(name);
        return _arrays.ResolveArray(name);
    }

    // The caller has already resolved the containing array. Inserting a child is one
    // admitted mutation; missing reads can instead leave an untyped cell until use.
    internal AwkArrayIdentity GetSubarray(AwkArrayIdentity parent, AwkArrayKey key)
    {
        ThrowIfDetached(parent);
        var slots = parent.Slots;
        var found = slots.TryGetValue(key, out var entry);
        var slot = entry?.Slot ?? default;
        if (slot.TryGetArrayBinding(out var existing)) return existing;
        if (found && !slot.IsUntyped)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));

        var growth = AwkRuntimeMemory.ArrayIdentityBytes;
        var estimatedGrowth = found ? growth : EstimateArrayInsertionGrowth(slots,
            AwkRuntimeMemory.CheckedAdd(growth, AwkRuntimeMemory.EstimateArrayElementPayloadBytes(key, default(AwkVariableSlot))));
        using var admission = _memoryAccounting.AdmitMutation(estimatedGrowth);
        using var creation = ReserveMemoryMutation(growth, 0);
        var child = new AwkArrayIdentity(CreateArraySlots(0), parent);
        if (found && slot.TryGetUntypedScalar(out var origin))
            origin.PromoteArray(child);
        else
            SetArraySlot(slots, key, AwkVariableSlot.FromArrayBinding(child));
        creation.Commit();
        _counters.RecordSubarrayPromotion();
        return child;
    }

    internal AwkArrayIdentity? FindSubarray(AwkArrayIdentity parent, AwkArrayKey key)
    {
        ThrowIfDetached(parent);
        if (!parent.Slots.TryGetValue(key, out var slot) || slot.IsUntyped) return null;
        if (slot.TryGetArrayBinding(out var child)) return child;
        throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
    }

    internal void ClearArray(AwkArrayIdentity array)
    {
        ThrowIfDetached(array);
        var oldBytes = EstimateArrayBytes(array.Slots, _checkpoint);
        var replacement = CreateArraySlots(0);
        RetireArrayContents(array, includeRoot: false);
        array.ReplaceSlots(replacement);
        _memoryAccounting.ReleaseCommitted(oldBytes);
    }

    internal void DeleteArrayValue(AwkArrayIdentity array, AwkArrayKey key)
    {
        ThrowIfDetached(array);
        var slots = array.Slots;
        if (!slots.GetAlternateLookup<AwkArrayKey>().TryGetValue(key, out var retainedKey, out var slot)) return;
        var oldBytes = AwkRuntimeMemory.EstimateArrayElementPayloadBytes(retainedKey, slot);
        if (slot.TryGetArrayBinding(out var child))
        {
            oldBytes = AwkRuntimeMemory.CheckedAdd(oldBytes,
                AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.ArrayIdentityBytes, EstimateArrayBytes(child.Slots, _checkpoint)));
            RetireArrayContents(child, includeRoot: true);
        }
        else if (slot.TryGetUntypedScalar(out var origin))
        {
            RepairElementAliases?.Invoke(origin);
            origin.Detach();
        }
        slots.Remove(retainedKey);
        slot.Detach();
        _memoryAccounting.ReleaseCommitted(oldBytes);
    }

    private void RetireArrayContents(AwkArrayIdentity array, bool includeRoot, bool cancellable = true)
    {
        // Repair every live frame before changing parent links. Removed subarray
        // parameters become independent existing function-local slots, not retained trees.
        RepairSubarrayAliases?.Invoke(array, includeRoot, cancellable);
        if (cancellable) _checkpoint?.Invoke();
        // Complete the backing replacement atomically after its cancellable scans.
        // Unwinding a function must finish retirement even when cancellation is set.
        Stack<AwkArrayIdentity>? pending = null;
        var slots = array.Slots;
        if (includeRoot) array.Detach();
        while (true)
        {
            foreach (var pair in slots)
            {
                var slot = pair.Value;
                if (slot.TryGetArrayBinding(out var child)) (pending ??= new()).Push(child);
                if (slot.TryGetUntypedScalar(out var origin)) origin.Detach();
                slot.Detach();
            }
            if (pending is null || !pending.TryPop(out var next)) break;
            slots = next.Slots;
            next.Detach();
        }
    }
}
