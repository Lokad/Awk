using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionCallStack(
    int maxFunctionCallDepth,
    AwkMemoryAccounting memoryAccounting,
    Func<AwkVariableSlot, AwkValue> createValue,
    Func<AwkArrayName, bool>? containsArray = null,
    Action? checkpoint = null)
{
    private readonly Func<AwkArrayName, bool> _containsArray = containsArray ?? (static _ => false);
    private readonly Stack<AwkFunctionFrame> _frames = new();
    private readonly Stack<AwkFunctionFrame> _recycledFrames = new();
    // Most potential aliases are independent function arguments. Counting live
    // references lets those promote directly; only forwarded aliases need a scan.
    private readonly Dictionary<AwkArrayName, int> _potentialArrayAliasCounts = new();
    private long _aliasCapacityBytes;
    private long _nextFrameId;
    private int _localScopeFloor;

    // Scheduling and depth limits include callers suspended by global file actions.
    public bool IsInsideFunction => _frames.Count > 0;

    public GlobalBindingScope EnterGlobalBindings()
    {
        // Hide caller bindings without releasing frames, charges or array aliases.
        var previous = _localScopeFloor;
        _localScopeFloor = _frames.Count;
        return new GlobalBindingScope(this, previous);
    }

    internal readonly struct GlobalBindingScope(AwkFunctionCallStack calls, int previousFloor) : IDisposable
    {
        public void Dispose() => calls._localScopeFloor = previousFloor;
    }

    private bool TryPeekLocalFrame([NotNullWhen(true)] out AwkFunctionFrame? frame)
    {
        if (_frames.Count > _localScopeFloor)
            return _frames.TryPeek(out frame);
        frame = null;
        return false;
    }

    public bool HasScalarLocal(AwkFunctionLocalSlotId slotId) =>
        TryPeekLocalFrame(out var frame) &&
        (uint)slotId.Index < (uint)frame.Layout.SlotCount &&
        frame.GetSlot(slotId.Index).Kind == AwkFunctionLocalSlotKind.Scalar;

    public bool PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionArgument> arguments)
    {
        if (_frames.Count >= maxFunctionCallDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorFunctionRecursionLimit);

        // Values are already evaluated in the caller. Estimate their slots without creating
        // the frame or its buffers; rejected admission must precede those allocations.
        var supplied = Math.Min(layout.SlotCount, arguments.Length);
        var localBytes = AwkRuntimeMemory.CheckedMultiply(layout.SlotCount - supplied,
            (int)AwkRuntimeMemory.EstimateFunctionLocalSlotBytes(AwkFunctionLocalSlot.MissingScalar()));
        for (var index = 0; index < supplied; index++)
        {
            if ((index & 63) == 63) checkpoint?.Invoke();
            localBytes = AwkRuntimeMemory.CheckedAdd(localBytes,
                AwkRuntimeMemory.EstimateFunctionArgumentSlotBytes(arguments[index]));
        }
        var newFrame = _recycledFrames.Count == 0;
        var retainedBytes = newFrame ? AwkRuntimeMemory.EstimateFunctionFrameBytes() : 0;
        // Keep synchronous staging local: no lease object on every ordinary call.
        var unpublishedBytes = AwkRuntimeMemory.CheckedAdd(retainedBytes, localBytes);
        memoryAccounting.ReserveAndCommit(unpublishedBytes);
        AwkFunctionFrame? frame = null;
        var potentialAliasesRegistered = false;
        try
        {
            if (newFrame)
            {
                if (memoryAccounting.IsEnabled)
                {
                    // The coarse frame charge covers both reference stacks. Prepare the
                    // recycle capacity now so failure/unwind needs no new admission/allocation.
                    _frames.EnsureCapacity(_frames.Count + 1);
                    _recycledFrames.EnsureCapacity(_frames.Count + 1);
                }
                frame = new AwkFunctionFrame();
                unpublishedBytes -= retainedBytes; // Retained with the execution's frame cache.
            }
            else frame = _recycledFrames.Pop();
            frame.Initialize(++_nextFrameId, layout, pooled: !memoryAccounting.IsEnabled);
            for (var index = 0; index < layout.SlotCount; index++)
            {
                if ((index & 63) == 63) checkpoint?.Invoke();
                frame.GetSlot(index) = CreateSlot(arguments, index);
            }

            RegisterPotentialArrayAliases(frame);
            potentialAliasesRegistered = true;
            checkpoint?.Invoke();
            _frames.Push(frame);
            frame.RetainedMemoryBytes = localBytes;
            unpublishedBytes -= localBytes;
            return frame.UsesInlineSlots;
        }
        catch
        {
            if (frame is not null)
            {
                if (potentialAliasesRegistered)
                    UnregisterPotentialArrayAliases(frame);
                RecycleFrame(frame);
            }
            throw;
        }
        finally
        {
            memoryAccounting.ReleaseCommitted(unpublishedBytes);
        }

        static AwkFunctionLocalSlot CreateSlot(ReadOnlySpan<AwkFunctionArgument> arguments, int index)
        {
            if (index >= arguments.Length) return AwkFunctionLocalSlot.MissingScalar();
            var argument = arguments[index];
            return argument.Kind switch
            {
                AwkFunctionArgumentKind.Array => AwkFunctionLocalSlot.ArrayAlias(argument.ArrayName),
                AwkFunctionArgumentKind.PotentialArray =>
                    AwkFunctionLocalSlot.PotentialArrayAlias(argument.ArrayName, argument.Value),
                AwkFunctionArgumentKind.UntypedScalar => AwkFunctionLocalSlot.UntypedScalar(argument.Value),
                _ => AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(argument.Value))
            };
        }

        void RegisterPotentialArrayAliases(AwkFunctionFrame frame)
        {
            var registeredSlotCount = 0;
            try
            {
                for (; registeredSlotCount < frame.Layout.SlotCount; registeredSlotCount++)
                {
                    if ((registeredSlotCount & 63) == 63) checkpoint?.Invoke();
                    var slot = frame.GetSlot(registeredSlotCount);
                    if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                        RegisterPotentialArrayAlias(slot.ArrayName);
                }
            }
            catch
            {
                for (var index = 0; index < registeredSlotCount; index++)
                {
                    var slot = frame.GetSlot(index);
                    if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                        UnregisterPotentialArrayAlias(slot.ArrayName);
                }

                throw;
            }
        }

        void UnregisterPotentialArrayAliases(AwkFunctionFrame frame)
        {
            for (var index = 0; index < frame.Layout.SlotCount; index++)
            {
                var slot = frame.GetSlot(index);
                if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                    UnregisterPotentialArrayAlias(slot.ArrayName);
            }
        }
    }

    public void PopFrame(Action<AwkArrayName> removeFunctionLocalArray)
    {
        var frame = _frames.Pop();
        for (var index = 0; index < frame.Layout.SlotCount; index++)
        {
            var slot = frame.GetSlot(index);
            if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                UnregisterPotentialArrayAlias(slot.ArrayName);
            if (slot.Kind is (AwkFunctionLocalSlotKind.ArrayAlias or AwkFunctionLocalSlotKind.PotentialArrayAlias) &&
                slot.ArrayName.IsOwnedByFunctionFrame(frame.Id))
            {
                removeFunctionLocalArray(slot.ArrayName);
            }
        }

        memoryAccounting.ReleaseCommitted(frame.RetainedMemoryBytes);
        RecycleFrame(frame);
    }

    public void SetReturnValue(AwkValue value)
    {
        if (!TryPeekLocalFrame(out var frame))
            throw new AwkRuntimeException(AwkMessages.ErrorReturnOutsideFunction);

        value = value.AsScalarCopy();
        frame.HasReturnValue = true;
        frame.ReturnValue = value;
    }

    public bool TryConsumeReturnValue(out AwkValue value)
    {
        if (TryPeekLocalFrame(out var frame) && frame.HasReturnValue)
        {
            value = frame.ReturnValue;
            return true;
        }

        value = AwkValue.FromText(string.Empty);
        return false;
    }

    public bool TryResolveLocalNumber(AwkFunctionLocalSlotId slotId, out double number)
    {
        if (TryPeekLocalFrame(out var frame) && (uint)slotId.Index < (uint)frame.Layout.SlotCount)
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            if (slot.Kind == AwkFunctionLocalSlotKind.Scalar)
                return slot.Value.TryGetNumber(out number);
        }

        number = 0;
        return false;
    }

    public bool TryResolveScalarLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value)
    {
        if (TryPeekLocalFrame(out var frame) && (uint)slotId.Index < (uint)frame.Layout.SlotCount)
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            if (slot.Kind == AwkFunctionLocalSlotKind.Scalar)
            {
                value = createValue(slot.Value);
                return true;
            }
        }
        value = default;
        return false;
    }

    public bool TryResolveLocal(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        TryResolveLocal(name, AwkFunctionLocalProjection.Kind, out local);

    public bool TryResolveLocal(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        TryResolveLocal(slotId, AwkFunctionLocalProjection.Kind, out local);

    public bool TryResolveLocalValue(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        TryResolveLocal(name, AwkFunctionLocalProjection.Value, out local);

    public bool TryResolveLocalValue(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        TryResolveLocal(slotId, AwkFunctionLocalProjection.Value, out local);

    private bool TryResolveLocal(
        AwkVariableName name,
        AwkFunctionLocalProjection projection,
        out AwkResolvedFunctionLocal local)
    {
        if (TryPeekLocalFrame(out var frame) &&
            TryGetLocalSlot(frame, name, out var slotId, out var slot))
        {
            slot = PromoteScalarProjection(frame, slotId, slot, projection);
            local = ResolveLocal(slot, projection);
            return true;
        }

        local = AwkResolvedFunctionLocal.NotFound;
        return false;
    }

    private bool TryResolveLocal(
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalProjection projection,
        out AwkResolvedFunctionLocal local)
    {
        if (TryPeekLocalFrame(out var frame) &&
            TryGetLocalSlot(frame, slotId, out var slot))
        {
            slot = PromoteScalarProjection(frame, slotId, slot, projection);
            local = ResolveLocal(slot, projection);
            return true;
        }

        local = AwkResolvedFunctionLocal.NotFound;
        return false;
    }

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionArgument argument)
    {
        if (!TryPeekLocalFrame(out var frame))
        {
            argument = default;
            return false;
        }

        if (!TryGetLocalSlot(frame, name, out var slotId, out var slot))
        {
            argument = default;
            return false;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            argument = AwkFunctionArgument.Array(slot.ArrayName);
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntyped)
        {
            if (_containsArray(slot.ArrayName))
            {
                argument = AwkFunctionArgument.Array(slot.ArrayName);
                return true;
            }
            argument = AwkFunctionArgument.PotentialArray(slot.ArrayName,
                createValue(slot.Value));
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntyped)
        {
            argument = AwkFunctionArgument.FromValue(
                createValue(slot.Value));
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
        {
            var localArrayName = AwkArrayName.FunctionLocal(name.Identifier, frame.Id);
            using var allocationPeak = memoryAccounting.ReserveAllocationPeak(AwkRuntimeMemory.EstimateUntypedScalarBytes());
            var value = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
            ReplaceLocalSlot(
                frame,
                slotId,
                slot,
                AwkFunctionLocalSlot.PotentialArrayAlias(localArrayName, value));
            argument = AwkFunctionArgument.PotentialArray(localArrayName, value);
            return true;
        }

        argument = default;
        return false;
    }

    public bool TryResolveOrPromoteArrayName(AwkVariableName name, out AwkArrayName arrayName)
    {
        if (!TryPeekLocalFrame(out var frame))
        {
            arrayName = default;
            return false;
        }

        if (!TryGetLocalSlot(frame, name, out var slotId, out var slot))
        {
            arrayName = default;
            return false;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            arrayName = slot.ArrayName;
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntyped)
        {
            arrayName = slot.ArrayName;
            PromotePotentialArrayAliases(frame, slotId, slot);
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
        {
            arrayName = AwkArrayName.FunctionLocal(name.Identifier, frame.Id);
            ReplaceLocalSlot(frame, slotId, slot, AwkFunctionLocalSlot.ArrayAlias(arrayName));
            return true;
        }

        arrayName = default;
        return false;

        void PromotePotentialArrayAliases(
            AwkFunctionFrame currentFrame,
            AwkFunctionLocalSlotId currentSlotId,
            AwkFunctionLocalSlot currentSlot)
        {
            var arrayName = currentSlot.ArrayName;
            if (!_potentialArrayAliasCounts.TryGetValue(arrayName, out var aliasCount))
                throw new InvalidOperationException("Potential array alias is not registered.");

            if (aliasCount == 1)
            {
                ReplaceLocalSlot(
                    currentFrame,
                    currentSlotId,
                    currentSlot,
                    AwkFunctionLocalSlot.ArrayAlias(arrayName));
                return;
            }

            var remainingAliases = aliasCount;
            foreach (var frame in _frames)
            {
                for (var index = 0; index < frame.Layout.SlotCount; index++)
                {
                    var slot = frame.GetSlot(index);
                    if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias &&
                        slot.ArrayName == arrayName)
                    {
                        ReplaceLocalSlot(
                            frame,
                            new AwkFunctionLocalSlotId(index),
                            slot,
                            AwkFunctionLocalSlot.ArrayAlias(arrayName));

                        remainingAliases--;
                        if (remainingAliases == 0)
                            return;
                    }
                }
            }

            throw new InvalidOperationException("Potential array alias count does not match the live function frames.");
        }
    }

    public bool TryGetArrayReadName(AwkVariableName name, out AwkArrayName arrayName)
    {
        if (TryPeekLocalFrame(out var frame))
        {
            if (TryGetLocalSlot(frame, name, out _, out var slot))
            {
                if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias ||
                    slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntyped)
                {
                    arrayName = slot.ArrayName;
                    return true;
                }
                if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
                {
                    arrayName = AwkArrayName.FunctionLocal(name.Identifier, frame.Id);
                    return true;
                }
            }
        }
        arrayName = default;
        return false;
    }

    public bool TryAssignLocal(AwkVariableName name, string value) =>
        TryAssignLocal(name, AwkVariableSlot.FromText(value));

    public bool TryAssignLocal(AwkVariableName name, AwkValue value) =>
        TryAssignLocal(name, AwkVariableSlot.FromValue(value));

    private bool TryAssignLocal(AwkVariableName name, AwkVariableSlot value)
    {
        if (!TryPeekLocalFrame(out var frame))
            return false;

        if (!TryGetLocalSlot(frame, name, out var slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias ||
            slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && _containsArray(slot.ArrayName))
        {
            return false;
        }

        ReplaceLocalSlot(frame, slotId, slot, AwkFunctionLocalSlot.Scalar(value));
        slot.Value.PromoteUntypedScalar();
        return true;
    }

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value) =>
        TryAssignLocal(slotId, AwkVariableSlot.FromValue(value));

    private bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkVariableSlot value)
    {
        if (!TryPeekLocalFrame(out var frame))
            return false;

        if (!TryGetLocalSlot(frame, slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias ||
            slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && _containsArray(slot.ArrayName))
        {
            return false;
        }

        ReplaceLocalSlot(frame, slotId, slot, AwkFunctionLocalSlot.Scalar(value));
        slot.Value.PromoteUntypedScalar();
        return true;
    }

    private void ReplaceLocalSlot(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot oldSlot,
        AwkFunctionLocalSlot newSlot)
    {
        var replacesPotentialAlias =
            oldSlot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias &&
            (newSlot.Kind != AwkFunctionLocalSlotKind.PotentialArrayAlias ||
             newSlot.ArrayName != oldSlot.ArrayName);
        var addsPotentialAlias =
            newSlot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias &&
            (oldSlot.Kind != AwkFunctionLocalSlotKind.PotentialArrayAlias ||
             oldSlot.ArrayName != newSlot.ArrayName);
        if (addsPotentialAlias)
            RegisterPotentialArrayAlias(newSlot.ArrayName);

        var oldBytes = EstimateLocalSlotBytes(oldSlot);
        var newBytes = EstimateLocalSlotBytes(newSlot);
        var growth = newBytes - oldBytes;
        try
        {
            if (growth > 0)
                memoryAccounting.Reserve(growth);

            try
            {
                frame.GetSlot(slotId.Index) = newSlot;
            }
            catch
            {
                if (growth > 0)
                    memoryAccounting.ReleaseReserved(growth);

                throw;
            }

            if (growth > 0)
                memoryAccounting.CommitReserved(growth);
            else if (growth < 0)
                memoryAccounting.ReleaseCommitted(-growth);

            frame.RetainedMemoryBytes = AwkRuntimeMemory.CheckedAdd(
                frame.RetainedMemoryBytes,
                growth);
        }
        catch
        {
            if (addsPotentialAlias)
                UnregisterPotentialArrayAlias(newSlot.ArrayName);

            throw;
        }

        if (replacesPotentialAlias)
            UnregisterPotentialArrayAlias(oldSlot.ArrayName);
    }

    private void RecycleFrame(AwkFunctionFrame frame)
    {
        var slots = frame.Reset();
        if (slots.Length > 0 && !memoryAccounting.IsEnabled)
            ArrayPool<AwkFunctionLocalSlot>.Shared.Return(slots, clearArray: true);
        _recycledFrames.Push(frame);
    }

    private AwkResolvedFunctionLocal ResolveLocal(
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntyped)
            return _containsArray(slot.ArrayName)
                ? AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName)
                : AwkResolvedFunctionLocal.PotentialArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntyped)
            return AwkResolvedFunctionLocal.UntypedScalar(
                createValue(slot.Value));

        if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
            return AwkResolvedFunctionLocal.MissingScalar();

        return projection switch
        {
            AwkFunctionLocalProjection.Kind => AwkResolvedFunctionLocal.Scalar(),
            AwkFunctionLocalProjection.Value => AwkResolvedFunctionLocal.ScalarValue(
                createValue(slot.Value)),
            _ => throw new InvalidOperationException("Unknown function-local projection.")
        };
    }

    private void RegisterPotentialArrayAlias(AwkArrayName arrayName)
    {
        _potentialArrayAliasCounts.TryGetValue(arrayName, out var count);
        if (memoryAccounting.IsEnabled && count == 0 &&
            _potentialArrayAliasCounts.Count == _potentialArrayAliasCounts.EnsureCapacity(0))
        {
            var target = (int)Math.Min(Array.MaxLength,
                Math.Max(3L, 2L * _potentialArrayAliasCounts.Count));
            // Dictionary prime rounding stays below this conservative bound. Admit the
            // new backing while the old backing is live; keep its actual capacity funded.
            var bound = (int)Math.Min(Array.MaxLength, 2L * target + 4);
            using var growth = memoryAccounting.AdmitMutation(AwkRuntimeMemory.EstimateDictionaryBufferBytes(bound));
            var capacity = _potentialArrayAliasCounts.EnsureCapacity(target);
            var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(capacity);
            memoryAccounting.ReserveAndCommit(bytes - _aliasCapacityBytes);
            _aliasCapacityBytes = bytes;
        }
        _potentialArrayAliasCounts[arrayName] = checked(count + 1);
    }

    private void UnregisterPotentialArrayAlias(AwkArrayName arrayName)
    {
        if (!_potentialArrayAliasCounts.TryGetValue(arrayName, out var count))
            throw new InvalidOperationException("Potential array alias is not registered.");

        if (count == 1)
            _potentialArrayAliasCounts.Remove(arrayName);
        else
            _potentialArrayAliasCounts[arrayName] = count - 1;
    }

    private AwkFunctionLocalSlot PromoteScalarProjection(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (projection != AwkFunctionLocalProjection.Value ||
            slot.Kind is not (AwkFunctionLocalSlotKind.MissingScalar or AwkFunctionLocalSlotKind.PotentialArrayAlias or AwkFunctionLocalSlotKind.UntypedScalar) ||
            slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && _containsArray(slot.ArrayName))
        {
            return slot;
        }

        var value = slot.Kind == AwkFunctionLocalSlotKind.MissingScalar
            ? slot.Value
            : AwkVariableSlot.FromValue(AwkValue.Unassigned());
        var promoted = AwkFunctionLocalSlot.Scalar(value);
        ReplaceLocalSlot(frame, slotId, slot, promoted);
        slot.Value.PromoteUntypedScalar();
        return promoted;
    }

    private static bool TryGetLocalSlot(
        AwkFunctionFrame frame,
        AwkVariableName name,
        out AwkFunctionLocalSlotId slotId,
        out AwkFunctionLocalSlot slot)
    {
        if (frame.Layout.TryGetSlot(name, out slotId))
        {
            slot = frame.GetSlot(slotId.Index);
            return true;
        }

        slot = default;
        return false;
    }

    private static bool TryGetLocalSlot(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        out AwkFunctionLocalSlot slot)
    {
        if ((uint)slotId.Index < (uint)frame.Layout.SlotCount)
        {
            slot = frame.GetSlot(slotId.Index);
            return true;
        }

        slot = default;
        return false;
    }

    private static long EstimateLocalSlotBytes(AwkFunctionLocalSlot slot) =>
        AwkRuntimeMemory.EstimateFunctionLocalSlotBytes(slot);

    private enum AwkFunctionLocalProjection
    {
        Kind,
        Value
    }

}
