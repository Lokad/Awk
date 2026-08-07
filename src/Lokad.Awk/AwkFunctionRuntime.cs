using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionRuntime
{
    private readonly AwkFunctionCallStack _calls;
    private IReadOnlyDictionary<AwkFunctionName, IAwkUserFunction> _functions =
        new Dictionary<AwkFunctionName, IAwkUserFunction>();

    public AwkFunctionRuntime(
        int maxFunctionCallDepth,
        AwkMemoryGovernor memoryGovernor,
        Func<AwkText, AwkValue> createByteTextValue,
        Func<AwkText, AwkValue> createNumericStringValue,
        Func<AwkText, AwkValue> createNumberFromText)
    {
        _calls = new AwkFunctionCallStack(
            maxFunctionCallDepth,
            memoryGovernor,
            createByteTextValue,
            createNumericStringValue,
            createNumberFromText);
    }

    public bool IsInsideFunction => _calls.IsInsideFunction;

    public IReadOnlyDictionary<AwkArrayKey, string> InitializeFunctions<TFunction>(
        IReadOnlyDictionary<AwkFunctionName, TFunction> functions)
        where TFunction : IAwkUserFunction
    {
        var functionMap = new Dictionary<AwkFunctionName, IAwkUserFunction>();
        foreach (var function in functions)
        {
            functionMap[function.Key] = function.Value;
        }

        _functions = functionMap;
        var functionTable = new Dictionary<AwkArrayKey, string>();
        foreach (var name in AwkBuiltins.Names)
        {
            functionTable[new AwkArrayKey(name)] = name;
        }

        foreach (var function in functions.Values)
        {
            functionTable[new AwkArrayKey(function.Name)] = function.Name;
        }

        return functionTable;
    }

    public bool TryGetUserFunction(AwkFunctionName name, [NotNullWhen(true)] out IAwkUserFunction? function) =>
        _functions.TryGetValue(name, out function);

    public void PushFrame(AwkFunctionLocalLayout layout, ReadOnlySpan<AwkFunctionArgument> arguments) =>
        _calls.PushFrame(layout, arguments);

    public void PopFrame() =>
        _calls.PopFrame();

    public void SetReturnValue(AwkValue value) =>
        _calls.SetReturnValue(value);

    public bool TryConsumeReturnValue(out AwkValue value) =>
        _calls.TryConsumeReturnValue(out value);

    public bool TryResolveLocal(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocal(name, out local);

    public bool TryResolveLocal(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocal(slotId, out local);

    public bool TryResolveLocalText(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalText(name, out local);

    public bool TryResolveLocalText(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalText(slotId, out local);

    public bool TryResolveLocalValue(AwkVariableName name, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalValue(name, out local);

    public bool TryResolveLocalValue(AwkFunctionLocalSlotId slotId, out AwkResolvedFunctionLocal local) =>
        _calls.TryResolveLocalValue(slotId, out local);

    public bool TryPromotePotentialArrayAlias(AwkVariableName name, out AwkArrayName arrayName) =>
        _calls.TryPromotePotentialArrayAlias(name, out arrayName);

    public bool TryAssignLocal(AwkVariableName name, string value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkVariableName name, AwkValue value) =>
        _calls.TryAssignLocal(name, value);

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, string value) =>
        _calls.TryAssignLocal(slotId, value);

    public bool TryAssignLocal(AwkFunctionLocalSlotId slotId, AwkValue value) =>
        _calls.TryAssignLocal(slotId, value);
}
