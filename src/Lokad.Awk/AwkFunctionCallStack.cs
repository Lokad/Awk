using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionCallStack(
    int maxFunctionCallDepth,
    AwkMemoryAccounting memoryAccounting,
    Func<AwkVariableSlot, AwkValue> createValue,
    Func<AwkArrayName, bool>? containsArray = null,
    Action? checkpoint = null,
    Func<AwkArrayName, AwkArrayIdentity?>? findArray = null,
    Func<AwkUntypedScalar, AwkArrayIdentity>? promoteArrayOrigin = null,
    AwkRuntimeCounters? counters = null)
{
    private readonly AwkRuntimeCounters _counters = counters ?? AwkRuntimeCounters.Disabled;
    private readonly Func<AwkArrayName, bool> _containsArray = containsArray ?? (static _ => false);
    private AwkFunctionFrame? _current;
    private int _depth;
    private readonly Stack<AwkFunctionFrame> _recycledFrames = new();
    // Most potential aliases are independent function arguments. Counting live
    // references lets those promote directly; only forwarded aliases need a scan.
    private readonly Dictionary<AwkArrayName, int> _potentialArrayAliasCounts = new();
    private long _aliasCapacityBytes;
    private long _nextFrameId;
    private int _localScopeFloor;

    // Scheduling and depth limits include callers suspended by global file actions.
    public bool IsInsideFunction => _current is not null;
    internal AwkFunctionFrame CurrentFrame => _current ?? throw new InvalidOperationException("No active function frame.");

    public GlobalBindingScope EnterGlobalBindings()
    {
        // Hide caller bindings without releasing frames, charges or array aliases.
        var previous = _localScopeFloor;
        _localScopeFloor = _depth;
        return new GlobalBindingScope(this, previous);
    }

    internal readonly struct GlobalBindingScope(AwkFunctionCallStack calls, int previousFloor) : IDisposable
    {
        public void Dispose() => calls._localScopeFloor = previousFloor;
    }

    private bool TryPeekLocalFrame([NotNullWhen(true)] out AwkFunctionFrame? frame)
    {
        if (_depth > _localScopeFloor && _current is { } current)
        {
            frame = current;
            return true;
        }
        frame = null;
        return false;
    }

    public bool HasLocalSlot(AwkFunctionLocalSlotId slotId) =>
        TryPeekLocalFrame(out var frame) && (uint)slotId.Index < (uint)frame.Layout.SlotCount;

    // Existing callers provide evaluated arguments. Bound execution stages directly
    // into the same owner before activation, without making callee locals visible.
    public bool PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionLocalSlot> arguments)
    {
        if (_depth >= maxFunctionCallDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorFunctionRecursionLimit);
        var frame = PrepareFrame(layout);
        try
        {
            for (var index = 0; index < Math.Min(arguments.Length, layout.SlotCount); index++)
            {
                if ((index & 63) == 63) checkpoint?.Invoke();
                StageArgument(frame, index, arguments[index]);
            }
            ActivateFrame(frame);
            return frame.UsesInlineSlots;
        }
        catch
        {
            if (frame.IsIncoming) DiscardIncomingFrame(frame);
            throw;
        }
    }

    private int _incomingFrames;

    internal AwkFunctionFrame PrepareFrame(AwkFunctionLocalLayout layout)
    {
        var newFrame = _recycledFrames.Count == 0;
        var cached = newFrame ? null : _recycledFrames.Peek();
        var bufferBytes = 0L;
        var capacity = layout.SlotCount > 2 ? layout.SlotCount : 0;
        if (cached is not null && cached.SlotCapacity < capacity)
        {
            var retiredBytes = AwkRuntimeMemory.EstimateFunctionSlotBufferBytes(cached.SlotCapacity);
            cached.DiscardSlotBuffer();
            memoryAccounting.ReleaseCommitted(retiredBytes);
        }
        if (capacity > 0 && (cached is null || cached.SlotCapacity == 0))
            bufferBytes = AwkRuntimeMemory.EstimateFunctionSlotBufferBytes(capacity);
        var frameBytes = newFrame ? AwkRuntimeMemory.EstimateFunctionFrameBytes() : 0;
        var unpublishedBytes = AwkRuntimeMemory.CheckedAdd(frameBytes, bufferBytes);
        memoryAccounting.ReserveAndCommit(unpublishedBytes);
        AwkFunctionFrame? frame = null;
        try
        {
            if (newFrame || bufferBytes > 0) checkpoint?.Invoke();
            if (newFrame)
            {
                // Covers active, incoming and recycled owners. Cleanup never grows
                // the recycle stack, including when nested argument calls fail.
                var frameCapacity = checked(_depth + _incomingFrames + _recycledFrames.Count + 1);
                _recycledFrames.EnsureCapacity(frameCapacity);
                frame = new AwkFunctionFrame();
                unpublishedBytes -= frameBytes;
            }
            else frame = _recycledFrames.Pop();
            frame.Initialize(++_nextFrameId, layout);
            unpublishedBytes -= bufferBytes;
            frame.IsIncoming = true;
            _incomingFrames++;
            _counters.RecordFunctionFramePrepared(bufferBytes > 0);
            return frame;
        }
        catch
        {
            if (frame is not null) RecycleFrame(frame);
            throw;
        }
        finally { memoryAccounting.ReleaseCommitted(unpublishedBytes); }
    }

    // Borrow immutable argument transport only during this synchronous staging.
    // Pure numbers publish into funded cells without an intermediate local slot.
    internal void StageValue(AwkFunctionFrame frame, int index, in AwkValue value)
    {
        if (value.IsNumberValue) StageNumber(frame, index, value.AsNumber());
        else StageArgument(frame, index, AwkFunctionLocalSlot.FromArgumentValue(value));
    }

    internal void StageArgument(AwkFunctionFrame frame, int index, AwkFunctionLocalSlot argument)
    {
        if ((uint)index >= (uint)frame.Layout.SlotCount) return; // Extra operands still evaluate in the caller.
        var bytes = memoryAccounting.IsEnabled ? EstimateLocalSlotBytes(argument) : 0;
        memoryAccounting.ReserveAndCommit(bytes);
        try
        {
            if (bytes > 0) checkpoint?.Invoke();
            frame.GetSlot(index) = argument;
            frame.RecordArgument(index, argument.Kind);
            frame.RetainedMemoryBytes = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, bytes);
        }
        catch { memoryAccounting.ReleaseCommitted(bytes); throw; }
    }

    internal void StageNumber(AwkFunctionFrame frame, int index, double number)
    {
        if ((uint)index < (uint)frame.Layout.SlotCount)
            frame.GetSlot(index) = AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromNumber(number));
    }

    internal void ActivateFrame(AwkFunctionFrame frame)
    {
        if (_depth >= maxFunctionCallDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorFunctionRecursionLimit);
        var registered = 0;
        try
        {
            for (var index = 0; index < frame.ReconcileEnd; index++)
            {
                if ((index & 63) == 63) checkpoint?.Invoke();
                ref var slot = ref frame.GetSlot(index);
                if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
                {
                    if (slot.Value.TryGetArrayBinding(out var array))
                    {
                        if (array.IsDetached)
                            slot = AwkFunctionLocalSlot.ArrayAlias(AwkArrayName.FunctionLocal(frame.Layout.Parameters[index].Identifier, frame.Id));
                    }
                    else slot = AwkFunctionLocalSlot.ArrayAlias(slot.ArrayName, findArray?.Invoke(slot.ArrayName));
                }
                else if (slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar &&
                    slot.Value.TryGetUntypedScalar(out var origin) && origin.IsDetached)
                    slot = AwkFunctionLocalSlot.MissingScalar();
            }
            for (; registered < frame.PotentialAliasEnd; registered++)
            {
                if ((registered & 63) == 63) checkpoint?.Invoke();
                var slot = frame.GetSlot(registered);
                if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                    RegisterPotentialArrayAlias(slot.ArrayName);
            }
            checkpoint?.Invoke();
            frame.Parent = _current;
            _current = frame;
            _depth++;
            _incomingFrames--;
            frame.IsIncoming = false;
            _counters.RecordFunctionActivationSlots(frame.ReconcileEnd + frame.PotentialAliasEnd);
        }
        catch
        {
            for (var index = 0; index < registered; index++)
                if (frame.GetSlot(index).Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                    UnregisterPotentialArrayAlias(frame.GetSlot(index).ArrayName);
            throw;
        }
    }

    internal void DiscardIncomingFrame(AwkFunctionFrame frame)
    {
        var payload = frame.RetainedMemoryBytes;
        _incomingFrames--;
        RecycleFrame(frame);
        memoryAccounting.ReleaseCommitted(payload);
    }

    public void PopFrame(Action<AwkArrayName> removeFunctionLocalArray)
    {
        var frame = CurrentFrame;
        _current = frame.Parent;
        _depth--;
        for (var index = 0; index < frame.AliasEnd; index++)
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

        var payloadBytes = frame.RetainedMemoryBytes;
        _counters.RecordFunctionRetirementSlots(frame.AliasEnd);
        RecycleFrame(frame);
        memoryAccounting.ReleaseCommitted(payloadBytes);
    }

    public bool TryResolveScalarLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value)
    {
        if (TryPeekLocalFrame(out var frame) && (uint)slotId.Index < (uint)frame.Layout.SlotCount)
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            if (slot.Kind == AwkFunctionLocalSlotKind.Scalar)
            {
                var scalar = slot.Value;
                value = scalar.TryGetNumber(out var number) ? AwkValue.FromNumber(number) : createValue(scalar);
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

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionLocalSlot argument)
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

        return TryResolveArrayArgument(frame, slotId, slot, out argument);
    }

    public bool TryResolveArrayArgument(AwkFunctionLocalSlotId slotId, out AwkFunctionLocalSlot argument)
    {
        if (!TryPeekLocalFrame(out var frame) || !TryGetLocalSlot(frame, slotId, out var slot))
        {
            argument = default;
            return false;
        }
        return TryResolveArrayArgument(frame, slotId, slot, out argument);
    }

    private bool TryResolveArrayArgument(AwkFunctionFrame frame, AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot slot, out AwkFunctionLocalSlot argument)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            argument = slot.ArrayName.Text.Length == 0 && slot.Value.TryGetArrayBinding(out var array)
                ? AwkFunctionLocalSlot.ArrayAlias(array) : AwkFunctionLocalSlot.ArrayAlias(slot.ArrayName);
            return true;
        }

        if (slot.Value.TryGetArrayBinding(out var child))
        {
            argument = AwkFunctionLocalSlot.ArrayAlias(child);
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntyped)
        {
            if (_containsArray(slot.ArrayName))
            {
                argument = AwkFunctionLocalSlot.ArrayAlias(slot.ArrayName);
                return true;
            }
            argument = AwkFunctionLocalSlot.PotentialArrayAlias(slot.ArrayName,
                createValue(slot.Value));
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntyped)
        {
            argument = AwkFunctionLocalSlot.FromArgumentValue(
                createValue(slot.Value));
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
        {
            var localArrayName = AwkArrayName.FunctionLocal(frame.Layout.Parameters[slotId.Index].Identifier, frame.Id);
            using var allocationPeak = memoryAccounting.ReserveAllocationPeak(AwkRuntimeMemory.EstimateUntypedScalarBytes());
            var value = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
            ReplaceLocalSlot(
                frame,
                slotId,
                slot,
                AwkFunctionLocalSlot.PotentialArrayAlias(localArrayName, value));
            argument = AwkFunctionLocalSlot.PotentialArrayAlias(localArrayName, value);
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
            for (var frame = _current; frame is not null; frame = frame.Parent)
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

    public bool TryGetBoundArray(AwkVariableName name, [NotNullWhen(true)] out AwkArrayIdentity? array,
        bool promote = false)
    {
        if (TryPeekLocalFrame(out var frame) && TryGetLocalSlot(frame, name, out var slotId, out var slot))
            return TryGetBoundArray(frame, slotId, slot, out array, promote);
        array = null;
        return false;
    }

    public bool TryGetBoundArray(AwkFunctionLocalSlotId slotId, [NotNullWhen(true)] out AwkArrayIdentity? array,
        bool promote = false)
    {
        if (TryPeekLocalFrame(out var frame) && TryGetLocalSlot(frame, slotId, out var slot))
            return TryGetBoundArray(frame, slotId, slot, out array, promote);
        array = null;
        return false;
    }

    private bool TryGetBoundArray(AwkFunctionFrame frame, AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot slot, [NotNullWhen(true)] out AwkArrayIdentity? array, bool promote)
    {
        if (slot.Value.TryGetArrayBinding(out array)) return true;
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            array = findArray?.Invoke(slot.ArrayName);
            if (array is not null)
            {
                // The existing array owner funds its storage; this is a non-owning alias.
                frame.GetSlot(slotId.Index) = AwkFunctionLocalSlot.ArrayAlias(slot.ArrayName, array);
                return true;
            }
        }
        if (promote && slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntyped &&
            slot.Value.TryGetUntypedScalar(out var origin) && origin.Parent is not null && promoteArrayOrigin is not null)
        {
            array = promoteArrayOrigin(origin);
            return true;
        }
        array = null;
        return false;
    }

    public void RepairSubarrayAliases(AwkArrayIdentity removed, bool includeRoot, bool cancellable)
    {
        var poll = cancellable ? checkpoint : null;
        for (var frame = _current; frame is not null; frame = frame.Parent)
        {
            for (var index = 0; index < frame.Layout.SlotCount; index++)
            {
                var slot = frame.GetSlot(index);
                if ((index & 63) == 0) poll?.Invoke();
                if (slot.Value.TryGetArrayBinding(out var array) && array.IsWithin(removed, poll) &&
                    (includeRoot || !ReferenceEquals(array, removed)))
                {
                    var name = AwkArrayName.FunctionLocal(frame.Layout.Parameters[index].Identifier, frame.Id);
                    ReplaceLocalSlot(frame, new(index), slot, AwkFunctionLocalSlot.ArrayAlias(name));
                }
                else if (slot.Value.TryGetUntypedScalar(out var origin) && !origin.IsScalar && origin.Array is null &&
                    origin.Parent is { } parent && parent.IsWithin(removed, poll))
                    ReplaceLocalSlot(frame, new(index), slot, AwkFunctionLocalSlot.MissingScalar());
            }
        }
    }

    public void RepairElementAliases(AwkUntypedScalar removed)
    {
        if (removed.IsScalar) return;
        for (var frame = _current; frame is not null; frame = frame.Parent)
            for (var index = 0; index < frame.Layout.SlotCount; index++)
            {
                if ((index & 63) == 0) checkpoint?.Invoke();
                var slot = frame.GetSlot(index);
                if (slot.Value.TryGetUntypedScalar(out var origin) && ReferenceEquals(origin, removed))
                    ReplaceLocalSlot(frame, new(index), slot, AwkFunctionLocalSlot.MissingScalar());
            }
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
            slot.Value.TryGetArrayBinding(out _) ||
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
            slot.Value.TryGetArrayBinding(out _) ||
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

        var growth = memoryAccounting.IsEnabled
            ? EstimateLocalSlotBytes(newSlot) - EstimateLocalSlotBytes(oldSlot)
            : 0;
        try
        {
            if (growth > 0)
                memoryAccounting.Reserve(growth);

            try
            {
                if (growth > 0) checkpoint?.Invoke();
                frame.GetSlot(slotId.Index) = newSlot;
                frame.RecordAlias(slotId.Index, newSlot.Kind);
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
        frame.Reset();
        _recycledFrames.Push(frame);
    }

    private AwkResolvedFunctionLocal ResolveLocal(
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (slot.Value.TryGetArrayBinding(out _))
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);
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
        if (projection == AwkFunctionLocalProjection.Value && slot.Value.TryGetArrayBinding(out _))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(frame.Layout.Parameters[slotId.Index].Text));
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
        AwkRuntimeMemory.EstimateFunctionLocalPayloadBytes(slot);

    private enum AwkFunctionLocalProjection
    {
        Kind,
        Value
    }

}
