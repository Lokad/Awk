using System;

namespace Lokad.Awk;

internal sealed class AwkVariableRuntime(
    AwkVariables variables,
    AwkFunctionRuntime functions,
    AwkSpecialVariableDispatcher specialVariables)
{
    public bool HasScalar(AwkIdentifier identifier) =>
        HasScalar(new AwkVariableName(identifier));

    public bool HasScalar(AwkVariableName name) =>
        ResolveName(name).Kind is
            AwkVariableNameResolutionKind.LocalScalar or
            AwkVariableNameResolutionKind.GlobalScalar;

    public bool IsArray(AwkIdentifier identifier) =>
        IsArray(new AwkArrayName(identifier));

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
        var resolved = ResolveName(new AwkVariableName(name.Identifier));
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalArrayAlias)
            return variables.TryGetArrayCount(resolved.ArrayName, out count);

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

        var resolved = ResolveName(name);
        ThrowIfArrayAsScalar(name.Text, resolved);

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

    public void AssignVariable(AwkIdentifier identifier, string value) =>
        AssignVariable(new AwkVariableName(identifier), value);

    public void AssignVariable(AwkVariableName name, string value)
    {
        if (functions.TryAssignLocal(name, value))
            return;

        ThrowIfArrayAsScalar(name.Text, ResolveName(name));

        var text = AwkText.FromString(value);
        variables.SetScalarText(name, text);
        if (AwkSpecialVariables.TryGetScalar(name, out var id))
            specialVariables.ApplyAssignment(id, text);
    }

    public void AssignVariableValue(AwkIdentifier identifier, AwkValue value) =>
        AssignVariableValue(new AwkVariableName(identifier), value);

    public void AssignVariableValue(AwkVariableName name, AwkValue value)
    {
        if (functions.TryAssignLocal(name, value))
            return;

        AssignGlobalVariableValue(name, value);
    }

    public void AssignGlobalVariableValue(AwkVariableName name, AwkValue value)
    {
        ThrowIfArrayAsScalar(name.Text, ResolveGlobalName(name));

        variables.SetScalarValue(name, value);
        if (AwkSpecialVariables.TryGetScalar(name, out var id))
            specialVariables.ApplyAssignment(id, value);
    }

    public void AssignFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        string value)
    {
        if (!functions.TryResolveLocal(slotId, out var local))
        {
            AssignVariable(name, value);
            return;
        }

        if (local.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
            return;
        }

        if (functions.TryAssignLocal(slotId, value))
            return;

        AssignVariable(name, value);
    }

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value)
    {
        if (!functions.TryResolveLocal(slotId, out var local))
        {
            AssignVariableValue(name, value);
            return;
        }

        if (local.Kind == AwkFunctionLocalSlotKind.ArrayAlias)
        {
            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
            return;
        }

        if (functions.TryAssignLocal(slotId, value))
            return;

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
