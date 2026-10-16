using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionRuntime
{
    private readonly AwkFunctionCallStack _calls;
    private readonly Action<AwkArrayName> _removeFunctionLocalArray;
    private Dictionary<AwkFunctionName, IAwkUserFunction> _functions =
        new Dictionary<AwkFunctionName, IAwkUserFunction>();

    public AwkFunctionRuntime(
        int maxFunctionCallDepth,
        AwkMemoryAccounting memoryAccounting,
        Func<AwkText, AwkValue> createByteTextValue,
        Func<AwkText, AwkValue> createNumericStringValue,
        Func<AwkText, AwkValue> createDeferredTextValue,
        Func<AwkArrayName, bool> containsArray,
        Action<AwkArrayName> removeFunctionLocalArray)
    {
        _removeFunctionLocalArray = removeFunctionLocalArray;
        _calls = new AwkFunctionCallStack(
            maxFunctionCallDepth,
            memoryAccounting,
            createByteTextValue,
            createNumericStringValue,
            createDeferredTextValue,
            containsArray);
    }

    public bool IsInsideFunction => _calls.IsInsideFunction;

    public void InitializeFunctions<TFunction>(
        IReadOnlyDictionary<AwkFunctionName, TFunction> functions)
        where TFunction : IAwkUserFunction
    {
        var functionMap = new Dictionary<AwkFunctionName, IAwkUserFunction>(functions.Count);
        foreach (var function in functions)
        {
            functionMap[function.Key] = function.Value;
        }

        _functions = functionMap;
    }

    public bool TryGetUserFunction(AwkFunctionName name, [NotNullWhen(true)] out IAwkUserFunction? function) =>
        _functions.TryGetValue(name, out function);

    public void PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionArgument> arguments) =>
        _calls.PushFrame(layout, arguments);

    public void PopFrame() =>
        _calls.PopFrame(_removeFunctionLocalArray);

    public void SetReturnValue(AwkValue value) =>
        _calls.SetReturnValue(value);

    public bool TryConsumeReturnValue(out AwkValue value) =>
        _calls.TryConsumeReturnValue(out value);

    public bool TryResolveLocalNumber(AwkFunctionLocalSlotId slotId, out double number) =>
        _calls.TryResolveLocalNumber(slotId, out number);

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

    public bool TryAssignLocal(AwkVariableName name, string value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkVariableName name, AwkValue value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, string value) =>
        _calls.TryAssignLocal(slotId, value);

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value) =>
        _calls.TryAssignLocal(slotId, value);
}
