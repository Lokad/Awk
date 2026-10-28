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
        Func<AwkVariableSlot, AwkValue> createValue,
        Func<AwkArrayName, bool> containsArray,
        Action<AwkArrayName> removeFunctionLocalArray,
        Action? checkpoint = null,
        Func<AwkArrayName, AwkArrayIdentity?>? findArray = null)
    {
        _removeFunctionLocalArray = removeFunctionLocalArray;
        _calls = new AwkFunctionCallStack(
            maxFunctionCallDepth,
            memoryAccounting,
            createValue,
            containsArray,
            checkpoint,
            findArray);
    }

    public bool IsInsideFunction => _calls.IsInsideFunction;

    public AwkFunctionCallStack.GlobalBindingScope EnterGlobalBindings() => _calls.EnterGlobalBindings();

    public bool HasScalarLocal(AwkFunctionLocalSlotId slotId) => _calls.HasScalarLocal(slotId);

    // Borrow the program snapshot; callers keep the table immutable throughout execution.
    public void InitializeFunctions(IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions) =>
        _functions = functions;

    public bool TryGetUserFunction(AwkFunctionName name, [NotNullWhen(true)] out AwkUserFunction? function) =>
        _functions.TryGetValue(name, out function);

    public bool PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionArgument> arguments) =>
        _calls.PushFrame(layout, arguments);

    public void PopFrame() =>
        _calls.PopFrame(_removeFunctionLocalArray);

    public void SetReturnValue(AwkValue value) =>
        _calls.SetReturnValue(value);

    public bool TryConsumeReturnValue(out AwkValue value) =>
        _calls.TryConsumeReturnValue(out value);

    public bool TryResolveLocalNumber(AwkFunctionLocalSlotId slotId, out double number) =>
        _calls.TryResolveLocalNumber(slotId, out number);

    public bool TryResolveScalarLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value) =>
        _calls.TryResolveScalarLocalValue(slotId, out value);

    public bool TryResolveLocal(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocal(name, out local);

    public bool TryResolveLocal(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocal(slotId, out local);

    public bool TryResolveLocalValue(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalValue(name, out local);

    public bool TryResolveLocalValue(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalValue(slotId, out local);

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionArgument argument) =>
        _calls.TryResolveArrayArgument(name, out argument);

    public bool TryResolveOrPromoteArrayName(AwkVariableName name, out AwkArrayName arrayName) =>
        _calls.TryResolveOrPromoteArrayName(name, out arrayName);

    public bool TryGetArrayReadName(AwkVariableName name, out AwkArrayName arrayName) =>
        _calls.TryGetArrayReadName(name, out arrayName);

    public bool TryGetBoundArray(AwkVariableName name, [NotNullWhen(true)] out AwkArrayIdentity? array) =>
        _calls.TryGetBoundArray(name, out array);

    public bool TryAssignLocal(AwkVariableName name, string value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkVariableName name, AwkValue value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value) =>
        _calls.TryAssignLocal(slotId, value);
}
