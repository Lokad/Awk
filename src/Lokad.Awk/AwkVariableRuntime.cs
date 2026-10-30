using System;

namespace Lokad.Awk;

internal sealed class AwkVariableRuntime(
    AwkVariables variables,
    AwkFunctionRuntime functions,
    AwkSpecialVariableDispatcher specialVariables,
    Action? checkpoint = null,
    AwkRuntimeCounters? counters = null)
{
    public bool HasScalar(AwkVariableName name) =>
        ResolveName(name).Kind is
            AwkVariableNameResolutionKind.LocalScalar or
            AwkVariableNameResolutionKind.GlobalScalar;

    public bool IsArray(AwkArrayName name) =>
        ResolveName(new AwkVariableName(name.Identifier)).Kind is
            AwkVariableNameResolutionKind.LocalArrayAlias or
            AwkVariableNameResolutionKind.SymbolTableArray or
            AwkVariableNameResolutionKind.GlobalArray;

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionArgument argument)
    {
        if (functions.TryResolveArrayArgument(name, out argument))
            return true;

        if (functions.TryResolveLocal(name, out _))
        {
            argument = default;
            return false;
        }

        var arrayName = new AwkArrayName(name.Identifier);
        if (variables.ContainsArray(arrayName))
        {
            argument = AwkFunctionArgument.Array(arrayName);
            return true;
        }

        if (variables.ContainsScalar(name))
        {
            argument = default;
            return false;
        }

        argument = AwkFunctionArgument.PotentialArray(arrayName, variables.GetOrCreateUntypedScalarValue(name));
        return true;
    }

    public bool TryGetExistingArrayCount(AwkArrayName name, out int count)
    {
        if (functions.TryGetBoundArray(new AwkVariableName(name.Identifier), out var array))
        {
            count = array.Slots.Count;
            return true;
        }
        var resolved = ResolveName(new AwkVariableName(name.Identifier));
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalArrayAlias)
        {
            variables.TryGetArrayCount(resolved.ArrayName, out count);
            return true;
        }
        if (resolved.Kind == AwkVariableNameResolutionKind.PotentialArrayAlias)
            return variables.TryGetArrayCount(resolved.ArrayName, out count);

        if (resolved.Kind is AwkVariableNameResolutionKind.LocalScalar or AwkVariableNameResolutionKind.MissingLocalScalar)
        {
            count = 0;
            return false;
        }

        return variables.TryGetArrayCount(name, out count);
    }

    public AwkValue ResolveVariableValue(AwkVariableName name)
    {
        if (functions.TryResolveLocalValue(name, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Value;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return AwkValue.Unassigned();

            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        // Local values were resolved above. A global scalar read only needs to
        // reject arrays; scalar existence and creation belong to AwkVariables.
        if (variables.ContainsArray(new AwkArrayName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name.Text));

        return variables.GetOrCreateScalarValue(name);
    }

    public AwkValue ResolveFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name)
    {
        if (functions.TryResolveLocalValue(slotId, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Value;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return AwkValue.Unassigned();

            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        return ResolveVariableValue(name);
    }

    public void AssignVariable(AwkVariableName name, string value)
    {
        if (functions.TryAssignLocal(name, value))
            return;

        ThrowIfArrayAsScalar(name.Text, ResolveName(name));

        var text = AwkText.FromString(value);
        specialVariables.Assign(name, AwkValue.FromText(text, AwkNumberParsingMode.Decimal, checkpoint));
    }

    public void AssignVariableValue(AwkVariableName name, AwkValue value)
    {
        if (functions.TryAssignLocal(name, value))
            return;

        if (functions.TryResolveLocal(name, out var local) && local.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name.Text));
        AssignGlobalVariableValue(name, value);
    }

    public void AssignGlobalVariableValue(AwkVariableName name, AwkValue value)
    {
        // Storage owns scalar creation and update. This boundary only rejects arrays.
        if (variables.ContainsArray(new AwkArrayName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name.Text));

        specialVariables.Assign(name, value);
    }

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value)
    {
        // The mutation already resolves and validates the visible local slot.
        if (functions.TryAssignLocal(slotId, value))
            return;

        if (functions.TryResolveLocal(slotId, out var local) &&
            local.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
            return;
        }

        AssignVariableValue(name, value);
    }

    public AwkGlobalSlotId GetOrCreateGlobalScalarSlot(AwkVariableName name)
    {
        ThrowIfArrayAsScalar(name.Text, ResolveName(name));
        return variables.GetOrCreateScalarSlot(name);
    }

    public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) =>
        variables.GetScalarValue(id);

    public void AssignGlobalScalarValue(AwkGlobalSlotId id, AwkValue value)
    {
        variables.SetScalarValue(id, value);
    }

    public bool IsLocalScalar(AwkVariableName name) =>
        functions.TryResolveLocal(name, out var local) &&
        local.Kind is
            AwkFunctionLocalSlotKind.Scalar or
            AwkFunctionLocalSlotKind.MissingScalar or AwkFunctionLocalSlotKind.UntypedScalar;

    public AwkArrayName ResolveOrPromoteArrayName(AwkArrayName name)
    {
        counters?.RecordArrayNameResolution();
        if (name.IsFunctionLocal)
            return name;

        if (functions.TryResolveOrPromoteArrayName(new AwkVariableName(name.Identifier), out var localArrayName))
            return localArrayName;

        if (!variables.IsSymbolTable(name.Text))
            EnsureArrayName(name);

        return name;
    }

    public void EnsureArrayName(AwkArrayName name)
    {
        var resolved = ResolveName(new AwkVariableName(name.Identifier));
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalScalar)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        if (resolved.Kind == AwkVariableNameResolutionKind.MissingLocalScalar)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        if (resolved.Kind == AwkVariableNameResolutionKind.GlobalScalar)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));
    }

    private AwkVariableNameResolution ResolveName(AwkVariableName name)
    {
        if (functions.TryResolveLocal(name, out var local))
        {
            return local.Kind switch
            {
                AwkFunctionLocalSlotKind.ArrayAlias => AwkVariableNameResolution.LocalArrayAlias(local.ArrayName),
                AwkFunctionLocalSlotKind.PotentialArrayAlias => AwkVariableNameResolution.PotentialArrayAlias(local.ArrayName),
                AwkFunctionLocalSlotKind.MissingScalar => AwkVariableNameResolution.MissingLocalScalar,
                AwkFunctionLocalSlotKind.UntypedScalar => AwkVariableNameResolution.MissingLocalScalar,
                _ => AwkVariableNameResolution.LocalScalar
            };
        }

        return ResolveGlobalName(name);
    }

    public AwkArrayName ResolveArrayReadName(AwkArrayName name)
    {
        counters?.RecordArrayNameResolution();
        if (name.IsFunctionLocal)
            return name;
        if (functions.TryGetArrayReadName(new AwkVariableName(name.Identifier), out var local))
            return local;
        if (!variables.IsSymbolTable(name.Text))
            EnsureArrayName(name);
        return name;
    }

    private AwkVariableNameResolution ResolveGlobalName(AwkVariableName name)
    {
        if (variables.IsSymbolTable(name.Text))
            return AwkVariableNameResolution.SymbolTableArray;

        if (variables.ContainsArray(new AwkArrayName(name.Identifier)))
            return AwkVariableNameResolution.GlobalArray;

        if (variables.ContainsScalar(name))
            return AwkVariableNameResolution.GlobalScalar;

        return AwkVariableNameResolution.Missing;
    }

    private static void ThrowIfArrayAsScalar(string name, AwkVariableNameResolution resolved)
    {
        if (resolved.Kind is
            AwkVariableNameResolutionKind.LocalArrayAlias or
            AwkVariableNameResolutionKind.SymbolTableArray or
            AwkVariableNameResolutionKind.GlobalArray)
        {
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name));
        }
    }

    private readonly record struct AwkVariableNameResolution(
        AwkVariableNameResolutionKind Kind,
        AwkArrayName ArrayName)
    {
        public static AwkVariableNameResolution LocalScalar { get; } =
            new(AwkVariableNameResolutionKind.LocalScalar, default);

        public static AwkVariableNameResolution MissingLocalScalar { get; } =
            new(AwkVariableNameResolutionKind.MissingLocalScalar, default);

        public static AwkVariableNameResolution LocalArrayAlias(AwkArrayName arrayName) =>
            new(AwkVariableNameResolutionKind.LocalArrayAlias, arrayName);

        public static AwkVariableNameResolution PotentialArrayAlias(AwkArrayName arrayName) =>
            new(AwkVariableNameResolutionKind.PotentialArrayAlias, arrayName);

        public static AwkVariableNameResolution SymbolTableArray { get; } =
            new(AwkVariableNameResolutionKind.SymbolTableArray, default);

        public static AwkVariableNameResolution GlobalArray { get; } =
            new(AwkVariableNameResolutionKind.GlobalArray, default);

        public static AwkVariableNameResolution GlobalScalar { get; } =
            new(AwkVariableNameResolutionKind.GlobalScalar, default);

        public static AwkVariableNameResolution Missing { get; } =
            new(AwkVariableNameResolutionKind.Missing, default);
    }

    private enum AwkVariableNameResolutionKind
    {
        Missing,
        LocalScalar,
        MissingLocalScalar,
        LocalArrayAlias,
        PotentialArrayAlias,
        SymbolTableArray,
        GlobalArray,
        GlobalScalar
    }
}
