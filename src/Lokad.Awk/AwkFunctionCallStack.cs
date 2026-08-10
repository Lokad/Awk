using System;
using System.Buffers;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFunctionCallStack(
    int maxFunctionCallDepth,
    AwkMemoryGovernor memoryGovernor,
    Func<AwkText, AwkValue> createByteTextValue,
    Func<AwkText, AwkValue> createNumericStringValue)
{
    private readonly Stack<AwkFunctionFrame> _frames = new();
    private readonly Stack<AwkFunctionFrame> _recycledFrames = new();
    private long _nextFrameId;

    public bool IsInsideFunction => _frames.Count > 0;

    public void PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionArgument> arguments)
    {
        if (_frames.Count >= maxFunctionCallDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorFunctionRecursionLimit);

        var slots = ArrayPool<AwkFunctionLocalSlot>.Shared.Rent(layout.SlotCount);
        for (var index = 0; index < layout.SlotCount; index++)
        {
            if (index >= arguments.Length)
            {
                slots[index] = AwkFunctionLocalSlot.MissingScalar();
                continue;
            }

            var argument = arguments[index];
            if (argument.Kind == AwkFunctionArgumentKind.Array)
            {
                slots[index] = AwkFunctionLocalSlot.ArrayAlias(argument.ArrayName);
                continue;
            }

            if (argument.Kind == AwkFunctionArgumentKind.PotentialArray)
            {
                slots[index] = AwkFunctionLocalSlot.PotentialArrayAlias(argument.ArrayName);
                continue;
            }

            slots[index] = AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(argument.Value));
        }

        var frame = _recycledFrames.Count == 0
            ? new AwkFunctionFrame()
            : _recycledFrames.Pop();
        frame.Initialize(++_nextFrameId, layout, slots);
        try
        {
            frame.RetainedMemoryBytes = EstimateFrameBytes(frame);
            memoryGovernor.Reserve(frame.RetainedMemoryBytes);
            try
            {
                _frames.Push(frame);
            }
            catch
            {
                memoryGovernor.ReleaseReserved(frame.RetainedMemoryBytes);
                throw;
            }

            try
            {
                memoryGovernor.CommitReserved(frame.RetainedMemoryBytes);
            }
            catch
            {
                _frames.Pop();
                memoryGovernor.ReleaseReserved(frame.RetainedMemoryBytes);
                throw;
            }
        }
        catch
        {
            RecycleFrame(frame);
            throw;
        }
    }

    public void PopFrame(Action<AwkArrayName> removeFunctionLocalArray)
    {
        var frame = _frames.Pop();
        for (var index = 0; index < frame.Layout.SlotCount; index++)
        {
            var slot = frame.Slots[index];
            if (slot.Kind is (AwkFunctionLocalSlotKind.ArrayAlias or AwkFunctionLocalSlotKind.PotentialArrayAlias) &&
                slot.ArrayName.IsOwnedByFunctionFrame(frame.Id))
            {
                removeFunctionLocalArray(slot.ArrayName);
            }
        }

        memoryGovernor.ReleaseCommitted(frame.RetainedMemoryBytes);
        RecycleFrame(frame);
    }

    public void SetReturnValue(AwkValue value)
    {
        if (_frames.Count == 0)
            throw new AwkRuntimeException(AwkMessages.ErrorReturnOutsideFunction);

        var frame = _frames.Peek();
        frame.HasReturnValue = true;
        frame.ReturnValue = value;
    }

    public bool TryConsumeReturnValue(out AwkValue value)
    {
        if (_frames.Count > 0 && _frames.Peek().HasReturnValue)
        {
            value = _frames.Peek().ReturnValue;
            return true;
        }

        value = AwkValue.FromText(string.Empty);
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
        if (_frames.Count > 0 &&
            TryGetLocalSlot(_frames.Peek(), name, out var slotId, out var slot))
        {
            slot = PromoteScalarProjection(_frames.Peek(), slotId, slot, projection);
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
        if (_frames.Count > 0 &&
            TryGetLocalSlot(_frames.Peek(), slotId, out var slot))
        {
            slot = PromoteScalarProjection(_frames.Peek(), slotId, slot, projection);
            local = ResolveLocal(slot, projection);
            return true;
        }

        local = AwkResolvedFunctionLocal.NotFound;
        return false;
    }

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionArgument argument)
    {
        if (_frames.Count == 0)
        {
            argument = default;
            return false;
        }

        var frame = _frames.Peek();
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

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
        {
            argument = AwkFunctionArgument.PotentialArray(slot.ArrayName);
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
        {
            var localArrayName = AwkArrayName.FunctionLocal(name.Identifier, frame.Id);
            ReplaceLocalSlot(
                frame,
                slotId,
                slot,
                AwkFunctionLocalSlot.PotentialArrayAlias(localArrayName));
            argument = AwkFunctionArgument.PotentialArray(localArrayName);
            return true;
        }

        argument = default;
        return false;
    }

    public bool TryResolveOrPromoteArrayName(AwkVariableName name, out AwkArrayName arrayName)
    {
        if (_frames.Count == 0)
        {
            arrayName = default;
            return false;
        }

        var frame = _frames.Peek();
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

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
        {
            arrayName = slot.ArrayName;
            PromotePotentialArrayAliases(arrayName);
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
    }

    public bool TryAssignLocal(AwkVariableName name, string value)
    {
        if (_frames.Count == 0)
            return false;

        var frame = _frames.Peek();
        if (!TryGetLocalSlot(frame, name, out var slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            return false;
        }

        ReplaceLocalSlot(frame, slotId, slot, AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromText(value)));
        return true;
    }

    public bool TryAssignLocal(AwkVariableName name, AwkValue value)
    {
        if (_frames.Count == 0)
            return false;

        var frame = _frames.Peek();
        if (!TryGetLocalSlot(frame, name, out var slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            return false;
        }

        ReplaceLocalSlot(frame, slotId, slot, AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(value)));
        return true;
    }

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, string value)
    {
        if (_frames.Count == 0)
            return false;

        var frame = _frames.Peek();
        if (!TryGetLocalSlot(frame, slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            return false;
        }

        ReplaceLocalSlot(frame, slotId, slot, AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromText(value)));
        return true;
    }

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value)
    {
        if (_frames.Count == 0)
            return false;

        var frame = _frames.Peek();
        if (!TryGetLocalSlot(frame, slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            return false;
        }

        ReplaceLocalSlot(frame, slotId, slot, AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(value)));
        return true;
    }

    private void ReplaceLocalSlot(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot oldSlot,
        AwkFunctionLocalSlot newSlot)
    {
        var oldBytes = EstimateLocalSlotBytes(oldSlot);
        var newBytes = EstimateLocalSlotBytes(newSlot);
        var growth = newBytes - oldBytes;
        if (growth > 0)
            memoryGovernor.Reserve(growth);

        try
        {
            frame.Slots[slotId.Index] = newSlot;
        }
        catch
        {
            if (growth > 0)
                memoryGovernor.ReleaseReserved(growth);

            throw;
        }

        if (growth > 0)
            memoryGovernor.CommitReserved(growth);
        else if (growth < 0)
            memoryGovernor.ReleaseCommitted(-growth);

        frame.RetainedMemoryBytes = AwkRuntimeMemory.CheckedAdd(
            frame.RetainedMemoryBytes,
            growth);
    }

    private static long EstimateFrameBytes(AwkFunctionFrame frame)
    {
        var bytes = AwkRuntimeMemory.EstimateFunctionFrameBytes();
        for (var index = 0; index < frame.Layout.SlotCount; index++)
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, EstimateLocalSlotBytes(frame.Slots[index]));

        return bytes;
    }

    private void RecycleFrame(AwkFunctionFrame frame)
    {
        var slots = frame.Reset();
        ArrayPool<AwkFunctionLocalSlot>.Shared.Return(slots, clearArray: true);
        _recycledFrames.Push(frame);
    }

    private AwkResolvedFunctionLocal ResolveLocal(
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
            return AwkResolvedFunctionLocal.PotentialArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
            return AwkResolvedFunctionLocal.MissingScalar();

        return projection switch
        {
            AwkFunctionLocalProjection.Kind => AwkResolvedFunctionLocal.Scalar(),
            AwkFunctionLocalProjection.Value => AwkResolvedFunctionLocal.ScalarValue(
                slot.Value.ToValue(createByteTextValue, createNumericStringValue)),
            _ => throw new InvalidOperationException("Unknown function-local projection.")
        };
    }

    private void PromotePotentialArrayAliases(AwkArrayName arrayName)
    {
        foreach (var frame in _frames)
        {
            for (var index = 0; index < frame.Layout.SlotCount; index++)
            {
                var slot = frame.Slots[index];
                if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias &&
                    slot.ArrayName == arrayName)
                {
                    ReplaceLocalSlot(
                        frame,
                        new AwkFunctionLocalSlotId(index),
                        slot,
                        AwkFunctionLocalSlot.ArrayAlias(arrayName));
                }
            }
        }
    }

    private AwkFunctionLocalSlot PromoteScalarProjection(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (projection != AwkFunctionLocalProjection.Value ||
            slot.Kind is not (AwkFunctionLocalSlotKind.MissingScalar or AwkFunctionLocalSlotKind.PotentialArrayAlias))
        {
            return slot;
        }

        var value = slot.Kind == AwkFunctionLocalSlotKind.MissingScalar
            ? slot.Value
            : AwkVariableSlot.FromValue(AwkValue.Unassigned());
        var promoted = AwkFunctionLocalSlot.Scalar(value);
        ReplaceLocalSlot(frame, slotId, slot, promoted);
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
            slot = frame.Slots[slotId.Index];
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
            slot = frame.Slots[slotId.Index];
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
