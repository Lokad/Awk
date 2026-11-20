using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionCallStack(AwkRuntimeState state)
{
    private AwkFunctionFrame? _current;
    private int _depth;
    private AwkFunctionFrame? _recycledFrame;
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

    internal AwkFunctionFrame PrepareFrame(AwkFunctionLocalLayout layout)
    {
        var cached = _recycledFrame;
        if (cached is null || cached.SlotCapacity < layout.SlotCount)
            return PrepareFrameStorage(layout, cached);

        TakeRecycledFrame(cached);
        cached.InitializeIncoming(++_nextFrameId, layout);
        return cached;
    }

    // Only new owners or insufficient backing reach allocation preparation.
    // Sufficient recycled capacity remains funded without another admission.
    private AwkFunctionFrame PrepareFrameStorage(AwkFunctionLocalLayout layout, AwkFunctionFrame? cached)
    {
        var bufferBytes = 0L;
        var capacity = layout.SlotCount;
        if (cached is not null)
        {
            var retiredBytes = AwkRuntimeMemory.EstimateFunctionSlotBufferBytes(cached.SlotCapacity);
            cached.DiscardSlotBuffer();
            state.MemoryAccounting.Refund(retiredBytes);
        }
        if (capacity > 0)
            bufferBytes = AwkRuntimeMemory.EstimateFunctionSlotBufferBytes(capacity);
        var frameBytes = cached is null ? AwkRuntimeMemory.EstimateFunctionFrameBytes() : 0;
        var unpublishedBytes = AwkRuntimeMemory.CheckedAdd(frameBytes, bufferBytes);
        state.MemoryAccounting.Charge(unpublishedBytes);
        AwkFunctionFrame? frame = null;
        try
        {
            state.CancellationCheckpoint();
            if (cached is null)
            {
                frame = new AwkFunctionFrame();
                unpublishedBytes -= frameBytes;
            }
            else
            {
                TakeRecycledFrame(cached);
                frame = cached;
            }
            frame.InitializeIncoming(++_nextFrameId, layout);
            unpublishedBytes -= bufferBytes;
            return frame;
        }
        catch
        {
            if (frame is not null) RecycleFrame(frame);
            throw;
        }
        finally { state.MemoryAccounting.Refund(unpublishedBytes); }
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
        var captured = value.ForFunctionStorage(state.NumberParsingMode, state.CancellationCheckpoint);
        PublishScalarArgument(frame, index, in captured);
    }

    private void PublishScalarArgument(AwkFunctionFrame frame, int index, in AwkValue captured)
    {
        // Arithmetic-only consumers need the normalized numeric fact, rather than
        // another owner of its spelling. Other contracts retain the full scalar.
        if (captured.StoredKind == AwkValueKind.NumericString &&
            frame.Layout.InputConsumption(index) == AwkParameterInput.Arithmetic)
        {
            frame.StageNumber(index, captured.StoredNumber);
            return;
        }
        var bytes = state.MemoryAccounting.IsEnabled ? captured.RetainedTextMemoryBytes : 0;
        state.MemoryAccounting.Charge(bytes);
        try
        {
            if (bytes > 0) state.CancellationCheckpoint();
            frame.StageScalar(index, in captured);
            frame.RetainedMemoryBytes = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, bytes);
        }
        catch { state.MemoryAccounting.Refund(bytes); throw; }
    }

    internal void StageRecordText(AwkFunctionFrame frame, int index, AwkRecordStorage record, int slot, int offset, int length)
    {
        if ((uint)index >= (uint)frame.Layout.SlotCount) return;
        // The record owns the bytes through the host-free call and any promotion.
        frame.StageRecordText(index, record, slot, offset, length);
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
        var bytes = state.MemoryAccounting.IsEnabled ? EstimateLocalSlotBytes(argument) : 0;
        if (bytes > 0)
        {
            // Capture before admission can replace a live source cell.
            var captured = argument;
            StageRetainedArgument(frame, index, in captured, bytes);
            return;
        }
        frame.StageCell(index, in argument);
    }

    private void StageRetainedArgument(AwkFunctionFrame frame, int index,
        in AwkFunctionLocalSlot captured, long bytes)
    {
        // The caller captured the actual owning cell before this admission.
        state.MemoryAccounting.Charge(bytes);
        try
        {
            state.CancellationCheckpoint();
            var retained = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, bytes);
            frame.StageCell(index, in captured);
            frame.RetainedMemoryBytes = retained;
        }
        catch { state.MemoryAccounting.Refund(bytes); throw; }
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
        var bytes = state.MemoryAccounting.IsEnabled ? key.RetainedTextMemoryBytes : 0;
        if (bytes > 0)
        {
            var captured = AwkFunctionLocalSlot.PreparedKey(key);
            StageRetainedArgument(frame, index, in captured, bytes);
            return;
        }
        frame.GetSlot(index).InitializeKey(key);
    }

    internal bool TryStageLocalArgument(AwkFunctionFrame? frame,
        AwkFunctionLocalSlotId source, AwkFunctionFrame incoming, int index,
        bool allowArray, bool prepareKey)
    {
        // Prepared execution has its actual owner; root/file and standalone helpers
        // retain visible-stack resolution. No source-cell reference crosses staging.
        if (frame is null && !TryPeekLocalFrame(out frame)) return false;
        if ((uint)source.Index >= (uint)frame.Layout.SlotCount) return false;
        ref readonly var sourceSlot = ref frame.GetSlot(source.Index);
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.Key)
        {
            if (!prepareKey) throw new InvalidOperationException("A prepared key requires a key consumer.");
            StageKey(incoming, index, sourceSlot.Key);
            return true;
        }
        if (sourceSlot.TryReadNumber(out var number))
        {

            incoming.StageNumber(index, number);
            return true;
        }
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.RecordText)
        {

            if (!prepareKey) StageArgument(incoming, index, in sourceSlot);
            else
            {
                var owned = MaterializeRecordText(frame, source, in sourceSlot).Value;
                StageKey(incoming, index,
                    new AwkArrayKey(owned.ByteText, state.CancellationCheckpoint).WithInputOrigin(owned.HasInputOrigin));
            }
            return true;
        }
        // Scalar publication already normalizes the payload at its actual owner.
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.Scalar)
        {
            var value = sourceSlot.Value;

            if (prepareKey && value.TryGetStoredScalarText(out var text))
                StageKey(incoming, index,
                    new AwkArrayKey(text, state.CancellationCheckpoint).WithInputOrigin(value.HasInputOrigin));
            else if ((uint)index < (uint)incoming.Layout.SlotCount)
                PublishScalarArgument(incoming, index, in value);
            return true;
        }
        if (!allowArray) return false;
        if (sourceSlot.Kind == AwkFunctionLocalSlotKind.ArrayAlias && sourceSlot.TryGetArrayBinding(out _))
        {
            // Capture the actual name/owner into the incoming frame. Activation
            // still repairs a child detached by a later argument.

            if ((uint)index < (uint)incoming.Layout.SlotCount) incoming.StageCell(index, in sourceSlot);
            return true;
        }
        // Alias resolution can admit a potential origin. Capture before that
        // callback; ordinary scalars above need only their smaller value snapshot.
        var slot = sourceSlot;
        if (!TryResolveArrayArgument(frame, source, in slot, out var argument)) return false;

        StageArgument(incoming, index, in argument);
        return true;
    }

    internal void EnsureDepthAvailable()
    {
        if (_depth >= state.Limits.MaxFunctionCallDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorFunctionRecursionLimit);
    }

    internal void ActivateFrame(AwkFunctionFrame frame)
    {
        EnsureDepthAvailable();
        if (frame.ReconcileEnd != 0 || frame.PotentialAliasEnd != 0)
            PrepareIncomingAliases(frame);
        else state.CancellationCheckpoint();

        frame.StackLink = _current;
        _current = frame;
        _depth++;
        frame.IsIncoming = false;
    }

    private void PrepareIncomingAliases(AwkFunctionFrame frame)
    {
        var registered = 0;
        try
        {
            for (var index = 0; index < frame.ReconcileEnd; index++)
            {
                if ((index & 63) == 63) state.CancellationCheckpoint();
                ref var slot = ref frame.GetSlot(index);
                if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
                {
                    if (slot.TryGetArrayBinding(out var array))
                    {
                        if (array.IsDetached)
                            slot = AwkFunctionLocalSlot.ArrayAlias(AwkArrayName.FunctionLocal(frame.Layout.Parameters[index].Identifier, frame.Id));
                    }
                    else
                    {
                        var name = slot.ArrayName;
                        state.Variables.TryGetOrdinaryArray(name, out var binding);
                        slot = AwkFunctionLocalSlot.ArrayAlias(name, binding);
                    }
                }
                else if (slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar &&
                    slot.Value.TryGetUntypedScalar(out var origin) && origin.IsDetached)
                    slot = AwkFunctionLocalSlot.MissingScalar();
            }
            for (; registered < frame.PotentialAliasEnd; registered++)
            {
                if ((registered & 63) == 63) state.CancellationCheckpoint();
                var slot = frame.GetSlot(registered);
                if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                    RegisterPotentialArrayAlias(slot.ArrayName);
            }
            state.CancellationCheckpoint();
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
        // An incoming frame has not executed its body, and recycled scratch was
        // cleared at prior retirement. Its reset cannot own another incoming tree.
        var payload = frame.RetainedMemoryBytes;
        RecycleFrame(frame);
        state.MemoryAccounting.Refund(payload);
    }

    public void PopFrame(Action<AwkArrayName> removeFunctionLocalArray)
    {
        var frame = CurrentFrame;
        _current = frame.StackLink;
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

        RecycleFrame(frame);
        state.MemoryAccounting.Refund(payloadBytes);
    }

    internal bool TryReadBoundLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value, bool allowArray = false)
    {
        if (TryPeekLocalFrame(out var frame) && (uint)slotId.Index < (uint)frame.Layout.SlotCount)
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            if (slot.Kind == AwkFunctionLocalSlotKind.Scalar)
            {
                value = slot.ReadValue(state.CancellationCheckpoint);
                return true;
            }
            if (slot.Kind == AwkFunctionLocalSlotKind.RecordText)
            {
                value = ReadFrameValue(frame, slotId);
                return true;
            }
            if (allowArray && slot.TryGetArrayBinding(out var array))
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

        if (slot.TryGetArrayBinding(out var child))
        {
            argument = AwkFunctionLocalSlot.ArrayAlias(child);
            return true;
        }

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntypedValue)
        {
            if (state.Variables.ContainsArray(slot.ArrayName))
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
            using var allocationPeak = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.EstimateUntypedScalarBytes());
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
            for (var frame = _current; frame is not null; frame = frame.StackLink)
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
        if (TryPeekLocalFrame(out var frame) && frame.Layout.TryGetSlot(name, out var slotId))
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            return TryGetBoundArray(frame, slotId, in slot, out array, promote);
        }
        array = null;
        return false;
    }

    public bool TryGetBoundArray(AwkFunctionLocalSlotId slotId, [NotNullWhen(true)] out AwkArrayIdentity? array,
        bool promote = false)
    {
        if (TryGetLocalFrame(slotId, out var frame))
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            return TryGetBoundArray(frame, slotId, in slot, out array, promote);
        }
        array = null;
        return false;
    }

    private bool TryGetBoundArray(AwkFunctionFrame frame, AwkFunctionLocalSlotId slotId,
        scoped in AwkFunctionLocalSlot slot, [NotNullWhen(true)] out AwkArrayIdentity? array, bool promote)
    {
        if (slot.TryGetArrayBinding(out array)) return true;
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            var name = slot.ArrayName;
            if (state.Variables.TryGetOrdinaryArray(name, out array))
            {
                // The existing array owner funds its storage; this is a non-owning alias.
                frame.GetSlot(slotId.Index) = AwkFunctionLocalSlot.ArrayAlias(name, array);
                return true;
            }
            return false;
        }
        if (promote && slot.Kind == AwkFunctionLocalSlotKind.UntypedScalar && slot.Value.IsUntypedValue &&
            slot.Value.TryGetUntypedScalar(out var origin) && origin.Parent is not null)
        {
            array = state.Variables.PromoteArrayOrigin(origin);
            return true;
        }
        array = null;
        return false;
    }

    public void RepairSubarrayAliases(AwkArrayIdentity removed, bool includeRoot, bool cancellable)
    {
        var poll = cancellable ? state.CancellationCheckpoint : null;
        for (var frame = _current; frame is not null; frame = frame.StackLink)
        {
            var end = frame.AliasRepairEnd;
            if (end == 0) poll?.Invoke();
            for (var index = 0; index < end; index++)
            {
                var slot = frame.GetSlot(index);
                if ((index & 63) == 0) poll?.Invoke();
                if (slot.TryGetArrayBinding(out var array) && array.IsWithin(removed, poll) &&
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
        for (var frame = _current; frame is not null; frame = frame.StackLink)
        {
            var end = frame.AliasRepairEnd;
            if (end == 0) state.CancellationCheckpoint();
            for (var index = 0; index < end; index++)
            {
                if ((index & 63) == 0) state.CancellationCheckpoint();
                var slot = frame.GetSlot(index);
                if (slot.Value.TryGetUntypedScalar(out var origin) && ReferenceEquals(origin, removed))
                    ReplaceLocalSlot(frame, new(index), slot, AwkFunctionLocalSlot.MissingScalar());
            }
        }
    }

    public bool TryAssignLocal(AwkVariableName name, string value) =>
        TryAssignLocal(name, AwkValue.FromDeferredText(AwkText.FromString(value), state.NumberParsingMode));

    public bool TryAssignLocal(AwkVariableName name, AwkValue value)
    {
        value = value.ForFunctionStorage(state.NumberParsingMode, state.CancellationCheckpoint);
        if (!TryPeekLocalFrame(out var frame))
            return false;

        if (!TryGetLocalSlot(frame, name, out var slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias ||
            slot.TryGetArrayBinding(out _) ||
            slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && state.Variables.ContainsArray(slot.ArrayName))
        {
            return false;
        }

        var assigned = default(AwkFunctionLocalSlot);
        assigned.InitializeScalar(in value);
        ReplaceLocalSlot(frame, slotId, slot, assigned);
        slot.Value.PromoteUntypedScalar();
        return true;
    }

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value,
        in AwkRegionStorage.ScalarSource source = default)
    {
        // Computed text was constructed in this execution's immutable mode.
        // Other captures keep their ordinary scalar/origin normalization.
        if (!source.IsText)
            value = value.ForFunctionStorage(state.NumberParsingMode, state.CancellationCheckpoint);
        else System.Diagnostics.Debug.Assert(value.StoredKind ==
            AwkValue.FromDeferredText(AwkText.Empty, state.NumberParsingMode).StoredKind);
        if (!TryPeekLocalFrame(out var frame))
            return false;

        if (!TryGetLocalSlot(frame, slotId, out var slot) ||
            slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias ||
            slot.TryGetArrayBinding(out _) ||
            slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && state.Variables.ContainsArray(slot.ArrayName))
        {
            return false;
        }

        var assigned = default(AwkFunctionLocalSlot);
        assigned.InitializeScalar(in value);
        ReplaceLocalSlot(frame, slotId, slot, assigned, in source);
        slot.Value.PromoteUntypedScalar();
        return true;
    }

    private void ReplaceLocalSlot(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot oldSlot,
        AwkFunctionLocalSlot newSlot,
        in AwkRegionStorage.ScalarSource source = default)
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

        var oldBytes = state.MemoryAccounting.IsEnabled ? EstimateLocalSlotBytes(oldSlot) : 0;
        var nextBytes = state.MemoryAccounting.IsEnabled ? EstimateLocalSlotBytes(newSlot) : 0;
        var growth = nextBytes - oldBytes;
        var credit = source.Credit(nextBytes);
        var unfunded = growth - credit;
        var admission = Math.Max(0, unfunded);
        try
        {
            var retained = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, growth);
            if (admission > 0)
                state.MemoryAccounting.Charge(admission);

            try
            {
                if (admission > 0) state.CancellationCheckpoint();
                source.Transfer(credit);
                frame.GetSlot(slotId.Index) = newSlot;
                frame.RecordAlias(slotId.Index, newSlot.Kind);
            }
            catch
            {
                if (admission > 0)
                    state.MemoryAccounting.Refund(admission);

                throw;
            }

            // Publish the funded local and clear a consumed source before refunds
            // can call the provider. Actual new admission retains its state.CancellationCheckpoint.
            frame.RetainedMemoryBytes = retained;
            if (unfunded < 0) state.MemoryAccounting.Refund(-unfunded);
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
        frame.StackLink = _recycledFrame;
        _recycledFrame = frame;
    }

    private void TakeRecycledFrame(AwkFunctionFrame frame)
    {
        System.Diagnostics.Debug.Assert(ReferenceEquals(_recycledFrame, frame));
        _recycledFrame = frame.StackLink;
        frame.StackLink = null;
    }

    private AwkResolvedFunctionLocal ResolveLocal(
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (slot.Kind == AwkFunctionLocalSlotKind.Key)
            return projection == AwkFunctionLocalProjection.Kind ? AwkResolvedFunctionLocal.Scalar() :
                throw new InvalidOperationException("A prepared key requires a key consumer.");
        if (slot.TryGetArrayBinding(out _))
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);
        if (slot.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            return AwkResolvedFunctionLocal.ArrayAlias(slot.ArrayName);

        if (slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && slot.Value.IsUntypedValue)
            return state.Variables.ContainsArray(slot.ArrayName)
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
                slot.ReadValue(state.CancellationCheckpoint)),
            _ => throw new InvalidOperationException("Unknown function-local projection.")
        };
    }

    private void RegisterPotentialArrayAlias(AwkArrayName arrayName)
    {
        _potentialArrayAliasCounts.TryGetValue(arrayName, out var count);
        if (state.MemoryAccounting.IsEnabled && count == 0 &&
            _potentialArrayAliasCounts.Count == _potentialArrayAliasCounts.EnsureCapacity(0))
        {
            var target = (int)Math.Min(Array.MaxLength,
                Math.Max(3L, 2L * _potentialArrayAliasCounts.Count));
            // Dictionary prime rounding stays below this conservative bound. Admit the
            // new backing while the old backing is live; keep its actual capacity funded.
            var bound = (int)Math.Min(Array.MaxLength, 2L * target + 4);
            using var growth = state.MemoryAccounting.AdmitMutation(AwkRuntimeMemory.EstimateDictionaryBufferBytes(bound));
            var capacity = _potentialArrayAliasCounts.EnsureCapacity(target);
            var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(capacity);
            state.MemoryAccounting.Charge(bytes - _aliasCapacityBytes);
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
        if (slot.Kind == AwkFunctionLocalSlotKind.RecordText) slot = MaterializeRecordText(frame, slotId, in slot);
        return slot.ReadValue(state.CancellationCheckpoint);
    }

    private AwkFunctionLocalSlot MaterializeRecordText(AwkFunctionFrame frame, AwkFunctionLocalSlotId slotId,
        in AwkFunctionLocalSlot slot)
    {
        if (!slot.TryGetRecordSource(out var source)) throw new InvalidOperationException("A record-text cell needs its source slot.");
        var captured = slot;
        var id = frame.Id;
        var retained = state.MemoryAccounting.IsEnabled ? AwkRuntimeMemory.EstimateTextBytes(source.Length) : 0;
        state.MemoryAccounting.Charge(retained);
        try
        {
            if (retained > 0) state.CancellationCheckpoint();
            var text = state.CopyFundedText(source.AsSpan());

            var owned = AwkFunctionLocalSlot.Scalar(AwkValue.FromDeferredText(text, state.NumberParsingMode));
            // Admission and polls can retire/reuse a cell. Keep the copy funded
            // through construction, caching only into its original live owner.
            if (frame.Id == id && (uint)slotId.Index < (uint)frame.Layout.SlotCount &&
                frame.GetSlot(slotId.Index).HasSameRecordText(in captured))
            {
                var total = AwkRuntimeMemory.CheckedAdd(frame.RetainedMemoryBytes, retained);
                frame.GetSlot(slotId.Index) = owned;
                frame.RetainedMemoryBytes = total;
                retained = 0;
            }
            return owned;
        }
        finally { state.MemoryAccounting.Refund(retained); }
    }

    private AwkFunctionLocalSlot PromoteScalarProjection(
        AwkFunctionFrame frame,
        AwkFunctionLocalSlotId slotId,
        AwkFunctionLocalSlot slot,
        AwkFunctionLocalProjection projection)
    {
        if (projection == AwkFunctionLocalProjection.Value && slot.Kind == AwkFunctionLocalSlotKind.RecordText)
            return MaterializeRecordText(frame, slotId, in slot);
        if (projection == AwkFunctionLocalProjection.Value && slot.TryGetArrayBinding(out _))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(frame.Layout.Parameters[slotId.Index].Text));
        if (projection != AwkFunctionLocalProjection.Value ||
            slot.Kind is not (AwkFunctionLocalSlotKind.MissingScalar or AwkFunctionLocalSlotKind.PotentialArrayAlias or AwkFunctionLocalSlotKind.UntypedScalar) ||
            slot.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias && state.Variables.ContainsArray(slot.ArrayName))
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
