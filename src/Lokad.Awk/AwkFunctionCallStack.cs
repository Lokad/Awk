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
        frame.Initialize(layout, slots);
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

    public void PopFrame()
    {
        var frame = _frames.Pop();
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

    public bool TryResolveLocal(AwkVariableName name, out AwkResolvedFunctionLocal local)
    {
        if (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (TryGetLocalSlot(frame, name, out _, out var slot))
            {
                local = ResolveLocalKind(slot);
                return true;
            }
        }

        local = AwkResolvedFunctionLocal.Missing;
        return false;
    }

    public bool TryResolveLocal(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local)
    {
        if (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (TryGetLocalSlot(frame, slotId, out var slot))
            {
                local = ResolveLocalKind(slot);
                return true;
            }
        }

        local = AwkResolvedFunctionLocal.Missing;
        return false;
    }

    public bool TryResolveLocalText(AwkVariableName name, out AwkResolvedFunctionLocal local)
    {
        if (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (TryGetLocalSlot(frame, name, out _, out var slot))
            {
                local = ResolveLocalText(slot);
                return true;
            }
        }

        local = AwkResolvedFunctionLocal.Missing;
        return false;
    }

    public bool TryResolveLocalText(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local)
    {
        if (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (TryGetLocalSlot(frame, slotId, out var slot))
            {
                local = ResolveLocalText(slot);
                return true;
            }
        }

        local = AwkResolvedFunctionLocal.Missing;
        return false;
    }

    public bool TryResolveLocalValue(AwkVariableName name, out AwkResolvedFunctionLocal local)
    {
        if (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (TryGetLocalSlot(frame, name, out _, out var slot))
            {
                local = ResolveLocalValue(slot);
                return true;
            }
        }

        local = AwkResolvedFunctionLocal.Missing;
        return false;
    }

    public bool TryResolveLocalValue(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local)
    {
        if (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (TryGetLocalSlot(frame, slotId, out var slot))
            {
                local = ResolveLocalValue(slot);
                return true;
            }
        }

        local = AwkResolvedFunctionLocal.Missing;
        return false;
    }

    public bool TryPromotePotentialArrayAlias(AwkVariableName name, out AwkArrayName arrayName)
    {
        if (_frames.Count == 0)
        {
            arrayName = default;
            return false;
        }

        var frame = _frames.Peek();
        if (!TryGetLocalSlot(frame, name, out var slotId, out var slot) ||
            slot.Kind != AwkFunctionLocalSlotKind.PotentialArrayAlias)
        {
            arrayName = default;
            return false;
        }

        arrayName = slot.ArrayName;
        frame.Slots[slotId.Index] = AwkFunctionLocalSlot.ArrayAlias(slot.ArrayName);
        return true;
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

    private static AwkResolvedFunctionLocal ResolveLocalKind(AwkFunctionLocalSlot slot)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
            return AwkResolvedFunctionLocal.PotentialArrayAlias(slot.ArrayName);

        return AwkResolvedFunctionLocal.Scalar();
    }

    private AwkResolvedFunctionLocal ResolveLocalText(AwkFunctionLocalSlot slot)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
            return AwkResolvedFunctionLocal.PotentialArrayAlias(slot.ArrayName);

        return AwkResolvedFunctionLocal.ScalarText(slot.Value.StringValue);
    }

    private AwkResolvedFunctionLocal ResolveLocalValue(AwkFunctionLocalSlot slot)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
            return AwkResolvedFunctionLocal.PotentialArrayAlias(slot.ArrayName);

        return AwkResolvedFunctionLocal.ScalarValue(
            slot.Value.ToValue(createByteTextValue, createNumericStringValue));
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

}
