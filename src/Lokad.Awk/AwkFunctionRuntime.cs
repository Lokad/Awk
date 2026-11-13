using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionRuntime
{
    private readonly AwkFunctionCallStack _calls;
    private readonly Action<AwkArrayName> _removeFunctionLocalArray;
    private IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> _functions = AwkProgram.Empty.Functions;

    public AwkFunctionRuntime(
        int maxFunctionCallDepth,
        AwkMemoryAccounting memoryAccounting,
        AwkNumberParsingMode numberParsingMode,
        Func<AwkArrayName, bool> containsArray,
        Action<AwkArrayName> removeFunctionLocalArray,
        Action? checkpoint = null,
        Func<AwkArrayName, AwkArrayIdentity?>? findArray = null,
        Func<AwkUntypedScalar, AwkArrayIdentity>? promoteArrayOrigin = null,
        AwkRuntimeCounters? counters = null)
    {
        _removeFunctionLocalArray = removeFunctionLocalArray;
        _calls = new AwkFunctionCallStack(
            maxFunctionCallDepth,
            memoryAccounting,
            numberParsingMode,
            containsArray,
            checkpoint,
            findArray,
            promoteArrayOrigin,
            counters);
    }

    public bool IsInsideFunction => _calls.IsInsideFunction;
    internal AwkFunctionFrame CurrentFrame => _calls.CurrentFrame;
    internal AwkValue ReadFrameValue(AwkFunctionFrame frame, AwkFunctionLocalSlotId slot) => _calls.ReadFrameValue(frame, slot);

    public AwkFunctionCallStack.GlobalBindingScope EnterGlobalBindings() => _calls.EnterGlobalBindings();

    public bool HasLocalSlot(AwkFunctionLocalSlotId slotId) => _calls.HasLocalSlot(slotId);

    // Borrow the program snapshot; callers keep the table immutable throughout execution.
    public void InitializeFunctions(IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions) =>
        _functions = functions;

    public bool TryGetUserFunction(AwkFunctionName name, [NotNullWhen(true)] out AwkUserFunction? function) =>
        _functions.TryGetValue(name, out function);

    public bool TryGetBoundArray(AwkFunctionLocalSlotId slot, [NotNullWhen(true)] out AwkArrayIdentity? array,
        bool promote = false) => _calls.TryGetBoundArray(slot, out array, promote);

    public void PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionLocalSlot> arguments) =>
        _calls.PushFrame(layout, arguments);

    internal AwkFunctionFrame PrepareFrame(AwkFunctionLocalLayout layout) => _calls.PrepareFrame(layout);
    internal void StageArgument(AwkFunctionFrame frame, int index, in AwkFunctionLocalSlot argument) => _calls.StageArgument(frame, index, in argument);
    internal void StageValue(AwkFunctionFrame frame, int index, in AwkValue value) => _calls.StageValue(frame, index, in value);
    internal void StageScalarArgument(AwkFunctionFrame frame, int index, in AwkValue value) =>
        _calls.StageScalarArgument(frame, index, in value);
    internal void StageReaderText(AwkFunctionFrame frame, int index, AwkReadBuffer borrow, int offset, int length) =>
        _calls.StageReaderText(frame, index, borrow, offset, length);
    internal void StageArrayAlias(AwkFunctionFrame frame, int index, AwkArrayName name, AwkArrayIdentity array) =>
        _calls.StageArrayAlias(frame, index, name, array);
    internal void StageKey(AwkFunctionFrame frame, int index, AwkArrayKey key) => _calls.StageKey(frame, index, key);
    internal bool TryStageLocalArgument(AwkFunctionLocalSlotId source, AwkFunctionFrame incoming, int index, bool allowArray, bool prepareKey) =>
        _calls.TryStageLocalArgument(source, incoming, index, allowArray, prepareKey);
    internal void ActivateFrame(AwkFunctionFrame frame) => _calls.ActivateFrame(frame);
    internal void DiscardIncomingFrame(AwkFunctionFrame frame) => _calls.DiscardIncomingFrame(frame);

    public void PopFrame() =>
        _calls.PopFrame(_removeFunctionLocalArray);

    internal bool TryReadBoundLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value, bool allowArray = false) =>
        _calls.TryReadBoundLocalValue(slotId, out value, allowArray);

    internal bool TryGetLocalFrame(AwkFunctionLocalSlotId slotId, [NotNullWhen(true)] out AwkFunctionFrame? frame) =>
        _calls.TryGetLocalFrame(slotId, out frame);

    public bool TryResolveLocal(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocal(name, out local);

    public bool TryResolveLocal(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocal(slotId, out local);

    public bool TryResolveLocalValue(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalValue(name, out local);

    public bool TryResolveLocalValue(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalValue(slotId, out local);

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionLocalSlot argument) =>
        _calls.TryResolveArrayArgument(name, out argument);

    public bool TryResolveOrPromoteArrayName(AwkVariableName name, out AwkArrayName arrayName) =>
        _calls.TryResolveOrPromoteArrayName(name, out arrayName);

    public bool TryGetArrayReadName(AwkVariableName name, out AwkArrayName arrayName) =>
        _calls.TryGetArrayReadName(name, out arrayName);

    public bool TryGetBoundArray(AwkVariableName name, [NotNullWhen(true)] out AwkArrayIdentity? array, bool promote = false) =>
        _calls.TryGetBoundArray(name, out array, promote);

    public void RepairSubarrayAliases(AwkArrayIdentity removed, bool includeRoot, bool cancellable) =>
        _calls.RepairSubarrayAliases(removed, includeRoot, cancellable);

    public void RepairElementAliases(AwkUntypedScalar removed) => _calls.RepairElementAliases(removed);

    public bool TryAssignLocal(AwkVariableName name, string value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkVariableName name, AwkValue value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value,
        AwkRegionStorage? source = null, in AwkRegionOperand sourceOperand = default) =>
        _calls.TryAssignLocal(slotId, value, source, in sourceOperand);

    internal bool TryRetireScalar(AwkFunctionLocalSlotId slotId) => _calls.TryRetireScalar(slotId);

}
