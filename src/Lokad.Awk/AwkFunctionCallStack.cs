using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionCallStack(
    int maxFunctionCallDepth,
    AwkMemoryAccounting memoryAccounting,
    AwkNumberParsingMode numberParsingMode,
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
    public void PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionLocalSlot> arguments)
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
        if (_recycledFrames.TryPeek(out var cached) &&
            cached.SlotCapacity >= layout.SlotCount)
        {
            var frame = _recycledFrames.Pop();
            frame.Initialize(++_nextFrameId, layout);
            return PublishIncomingFrame(frame, grewBuffer: false);
        }
        return PrepareFrameStorage(layout);
    }

    private AwkFunctionFrame PublishIncomingFrame(AwkFunctionFrame frame, bool grewBuffer)
    {
        frame.IsIncoming = true;
        _incomingFrames++;
        _counters.RecordFunctionFramePrepared(grewBuffer);
        return frame;
    }

    // Only new owners or insufficient backing reach allocation preparation.
    // Sufficient recycled capacity remains funded without another admission.
    private AwkFunctionFrame PrepareFrameStorage(AwkFunctionLocalLayout layout)
    {
        var newFrame = _recycledFrames.Count == 0;
        var cached = newFrame ? null : _recycledFrames.Peek();
        var bufferBytes = 0L;
        var capacity = layout.SlotCount;
        if (cached is not null)
        {
            var retiredBytes = AwkRuntimeMemory.EstimateFunctionSlotBufferBytes(cached.SlotCapacity);
            cached.DiscardSlotBuffer();
            memoryAccounting.ReleaseCommitted(retiredBytes);
        }
        if (capacity > 0)
            bufferBytes = AwkRuntimeMemory.EstimateFunctionSlotBufferBytes(capacity);
        var frameBytes = newFrame ? AwkRuntimeMemory.EstimateFunctionFrameBytes() : 0;
        var unpublishedBytes = AwkRuntimeMemory.CheckedAdd(frameBytes, bufferBytes);
        memoryAccounting.ReserveAndCommit(unpublishedBytes);
        AwkFunctionFrame? frame = null;
        try
        {
            checkpoint?.Invoke();
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
            return PublishIncomingFrame(frame, bufferBytes > 0);
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
        if (value.IsNumberValue) frame.StageNumber(index, value.AsNumber());
        else if (value.IsArrayValue || value.IsUntypedValue)
            StageArgument(frame, index, AwkFunctionLocalSlot.FromArgumentValue(value));
        else
        {
            StageScalarArgument(frame, index, in value);
        }
    }

    internal void StageScalarArgument(AwkFunctionFrame frame, int index, in AwkValue value)
    {
        if ((uint)index >= (uint)frame.Layout.SlotCount) return;
        var captured = value.ForFunctionStorage(numberParsingMode, checkpoint);
        var bytes = memoryAccounting.IsEnabled ? captured.RetainedTextMemoryBytes : 0;
        memoryAccounting.ReserveAndCommit(bytes);
        try
        {
            if (bytes > 0) checkpoint?.Invoke();
            frame.StageScalar(index, in captured);
            frame.RetainedMemoryBytes = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, bytes);
        }
        catch { memoryAccounting.ReleaseCommitted(bytes); throw; }
    }

    internal void StageArgument(AwkFunctionFrame frame, int index, in AwkFunctionLocalSlot argument)
    {
        if (argument.Kind == AwkFunctionLocalSlotKind.Scalar)
        {
            var scalar = argument.Value;
            StageScalarArgument(frame, index, in scalar);
            return;
        }
        if ((uint)index >= (uint)frame.Layout.SlotCount) return; // Extra operands still evaluate in the caller.
        // Admission can invoke a callback. Capture before it, even if the caller
        // supplied a view of a live local slot.
        var captured = argument;
        var bytes = memoryAccounting.IsEnabled ? EstimateLocalSlotBytes(captured) : 0;
        memoryAccounting.ReserveAndCommit(bytes);
        try
        {
            if (bytes > 0) checkpoint?.Invoke();
            frame.GetSlot(index) = captured;
            frame.RecordArgument(index, captured.Kind);
            frame.RetainedMemoryBytes = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, bytes);
        }
        catch { memoryAccounting.ReleaseCommitted(bytes); throw; }
    }

    internal void StageArrayAlias(AwkFunctionFrame frame, int index, AwkArrayName name, AwkArrayIdentity array)
    {
        if ((uint)index >= (uint)frame.Layout.SlotCount) return;
        // The array already funds its storage. This non-owning alias retains its
        // language name and the ordinary activation/retirement bounds.
        frame.GetSlot(index) = AwkFunctionLocalSlot.ArrayAlias(name, array);
        frame.RecordArgument(index, AwkFunctionLocalSlotKind.ArrayAlias);
    }

    internal void StageKey(AwkFunctionFrame frame, int index, AwkArrayKey key)
    {
        if ((uint)index >= (uint)frame.Layout.SlotCount) return;
        var bytes = memoryAccounting.IsEnabled ? key.RetainedTextMemoryBytes : 0;
        if (bytes > 0)
        {
            StageRetainedKey(frame, index, key, bytes);
            return;
        }
        frame.GetSlot(index) = AwkFunctionLocalSlot.PreparedKey(key);
    }

    private void StageRetainedKey(AwkFunctionFrame frame, int index, AwkArrayKey key, long bytes)
    {
        // The owning key is captured by value before admission can change its source.
        memoryAccounting.ReserveAndCommit(bytes);
        try
        {
            checkpoint?.Invoke();
            var retained = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, bytes);
            frame.GetSlot(index) = AwkFunctionLocalSlot.PreparedKey(key);
            frame.RetainedMemoryBytes = retained;
        }
        catch { memoryAccounting.ReleaseCommitted(bytes); throw; }
    }

    internal bool TryStageLocalArgument(AwkFunctionLocalSlotId source, AwkFunctionFrame incoming, int index,
        bool allowArray, bool prepareKey)
    {
        if (!TryPeekLocalFrame(out var frame) || (uint)source.Index >= (uint)frame.Layout.SlotCount)
            return false;
        ref readonly var sourceSlot = ref frame.GetSlot(source.Index);
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.Key)
        {
            if (!prepareKey) throw new InvalidOperationException("A prepared key requires a key consumer.");
            StageKey(incoming, index, sourceSlot.Key);
            return true;
        }
        if (sourceSlot.TryReadNumber(out var number))
        {
            _counters.RecordNumericLocalRead();
            incoming.StageNumber(index, number);
            return true;
        }
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.ReaderText)
        {
            _counters.RecordScalarLocalValueRead();
            if (!prepareKey) StageArgument(incoming, index, in sourceSlot);
            else
            {
                var owned = MaterializeReaderText(frame, source, in sourceSlot).Value;
                StageKey(incoming, index, new AwkArrayKey(owned.ByteText, checkpoint));
            }
            return true;
        }
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.Scalar &&
            sourceSlot.Value.StoredKind is not (AwkValueKind.Untyped or AwkValueKind.ArrayReference))
        {
            var value = sourceSlot.Value;
            _counters.RecordScalarLocalValueRead();
            if (prepareKey && value.TryGetStoredScalarText(out var text))
                StageKey(incoming, index, new AwkArrayKey(text, checkpoint));
            else StageScalarArgument(incoming, index, in value);
            return true;
        }
        if (!allowArray) return false;
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.ArrayAlias && sourceSlot.Value.TryGetArrayBinding(out var array))
        {
            // Capture the actual name/owner into the incoming frame. Activation
            // still repairs a child detached by a later argument.
            _counters.RecordIndexedLocalArrayArgument();
            StageArrayAlias(incoming, index, sourceSlot.ArrayName, array);
            return true;
        }
        // Alias resolution can admit a potential origin. Capture before that
        // callback; ordinary scalars above need only their smaller value snapshot.
        var slot = sourceSlot;
        if (!TryResolveArrayArgument(frame, source, in slot, out var argument)) return false;
        _counters.RecordIndexedLocalArrayArgument();
        StageArgument(incoming, index, in argument);
        return true;
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

    internal bool TryReadBoundLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value, bool allowArray = false)
    {
        if (TryPeekLocalFrame(out var frame) && (uint)slotId.Index < (uint)frame.Layout.SlotCount)
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            if (slot.Kind == AwkFunctionLocalSlotKind.Scalar)
            {
                value = slot.ReadValue(checkpoint);
                return true;
            }
            if (slot.Kind == AwkFunctionLocalSlotKind.ReaderText)
            {
                value = ReadFrameValue(frame, slotId);
                return true;
            }
            if (allowArray && slot.Value.TryGetArrayBinding(out var array))
            {
                value = AwkValue.FromArrayReference(array);
                return true;
            }
        }
        value = default;
        return false;
    }

    internal bool TryGetLocalFrame(AwkFunctionLocalSlotId slotId, [NotNullWhen(true)] out AwkFunctionFrame? frame)
    {
        if (TryPeekLocalFrame(out frame) && (uint)slotId.Index < (uint)frame.Layout.SlotCount) return true;
        frame = null;
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

        return TryResolveArrayArgument(frame, slotId, in slot, out argument);
    }

    private bool TryResolveArrayArgument(AwkFunctionFrame frame, AwkFunctionLocalSlotId slotId,
        in AwkFunctionLocalSlot slot, out AwkFunctionLocalSlot argument)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            argument = slot;
            return true;
        }

        if (slot.Value.TryGetArrayBinding(out var child))
        {
            argument = AwkFunctionLocalSlot.ArrayAlias(child);
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntypedValue)
        {
            if (_containsArray(slot.ArrayName))
            {
                argument = AwkFunctionLocalSlot.ArrayAlias(slot.ArrayName);
                return true;
            }
            argument = AwkFunctionLocalSlot.PotentialArrayAlias(slot.ArrayName,
                slot.Value);
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntypedValue)
        {
            argument = AwkFunctionLocalSlot.FromArgumentValue(
                slot.Value);
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

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntypedValue)
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
                    slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntypedValue)
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
        if (promote && slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntypedValue &&
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
        TryAssignLocal(name, AwkValue.FromDeferredText(AwkText.FromString(value), numberParsingMode));

    public bool TryAssignLocal(AwkVariableName name, AwkValue value)
    {
        value = value.ForFunctionStorage(numberParsingMode, checkpoint);
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

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value)
    {
        value = value.ForFunctionStorage(numberParsingMode, checkpoint);
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
        if (slot.Kind == AwkFunctionLocalSlotKind.Key)
            return projection == AwkFunctionLocalProjection.Kind ? AwkResolvedFunctionLocal.Scalar() :
                throw new InvalidOperationException("A prepared key requires a key consumer.");
        if (slot.Value.TryGetArrayBinding(out _))
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntypedValue)
            return _containsArray(slot.ArrayName)
                ? AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName)
                : AwkResolvedFunctionLocal.PotentialArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntypedValue)
            return AwkResolvedFunctionLocal.UntypedScalar(
                slot.Value);

        if (slot.Kind == AwkFunctionLocalSlotKind.MissingScalar)
            return AwkResolvedFunctionLocal.MissingScalar();

        return projection switch
        {
            AwkFunctionLocalProjection.Kind => AwkResolvedFunctionLocal.Scalar(),
            AwkFunctionLocalProjection.Value => AwkResolvedFunctionLocal.ScalarValue(
                slot.ReadValue(checkpoint)),
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

    internal AwkValue ReadFrameValue(AwkFunctionFrame frame, AwkFunctionLocalSlotId slotId)
    {
        var slot = frame.GetSlot(slotId.Index);
        if (slot.Kind == AwkFunctionLocalSlotKind.ReaderText) slot = MaterializeReaderText(frame, slotId, in slot);
        return slot.ReadValue(checkpoint);
    }

    private AwkFunctionLocalSlot MaterializeReaderText(AwkFunctionFrame frame, AwkFunctionLocalSlotId slotId,
        in AwkFunctionLocalSlot slot)
    {
        if (!slot.TryGetReaderText(out var bytes)) throw new InvalidOperationException("A reader-text cell needs an active borrow.");
        var captured = slot;
        var id = frame.Id;
        // This frame admitted the complete text/cache capacity at argument capture.
        // Materialization consumes that capacity without another admission callback.
        var text = AwkText.CopyTrustedBytes(bytes.Span, _counters, AwkMemoryAccounting.Disabled, checkpoint);
        _counters.RecordValueCreated();
        var owned = AwkFunctionLocalSlot.Scalar(AwkValue.FromDeferredText(text, numberParsingMode));
        // Polls can retire/reuse a cell. Preserve the captured result regardless;
        // cache only into the original live payload, without changing its funding.
        if (frame.Id == id && (uint)slotId.Index < (uint)frame.Layout.SlotCount &&
            frame.GetSlot(slotId.Index).HasSameReaderText(in captured)) frame.GetSlot(slotId.Index) = owned;
        return owned;
    }

    private AwkFunctionLocalSlot PromoteScalarProjection(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (projection == AwkFunctionLocalProjection.Value && slot.Kind == AwkFunctionLocalSlotKind.ReaderText)
            return MaterializeReaderText(frame, slotId, in slot);
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
            : AwkValue.Unassigned();
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

    private static long EstimateLocalSlotBytes(in AwkFunctionLocalSlot slot) =>
        AwkRuntimeMemory.EstimateFunctionLocalPayloadBytes(in slot);

    private enum AwkFunctionLocalProjection
    {
        Kind,
        Value
    }

}
