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
        ResolveName(name).Kind == AwkVariableNameResolutionKind.GlobalScalar;

    public bool IsArray(AwkIdentifier identifier) =>
        IsArray(new AwkArrayName(identifier));

    public bool IsArray(AwkArrayName name) =>
        ResolveName(new AwkVariableName(name.Identifier)).Kind is
            AwkVariableNameResolutionKind.LocalArrayAlias or
            AwkVariableNameResolutionKind.SymbolTableArray or
            AwkVariableNameResolutionKind.GlobalArray;

    public bool TryGetExistingArrayCount(AwkArrayName name, out int count)
    {
        var resolved = ResolveName(new AwkVariableName(name.Identifier));
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalArrayAlias)
            return TryGetExistingArrayCount(resolved.ArrayName, out count);

        return variables.TryGetArrayCount(name, out count);
    }

    public string ResolveVariable(AwkVariableName name)
    {
        if (functions.TryResolveLocalText(name, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Text;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return string.Empty;

            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        var resolved = ResolveName(name);
        ThrowIfArrayAsScalar(name.Text, resolved);

        return variables.GetOrCreateScalarString(name);
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

    public string ResolveFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name)
    {
        if (functions.TryResolveLocalText(slotId, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Text;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return string.Empty;

            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        return ResolveVariable(name);
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

    public string ResolveVariable(AwkVariableName name, string fallback)
    {
        if (functions.TryResolveLocalText(name, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Text;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return string.Empty;

            ThrowIfArrayAsScalar(name.Text, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        var resolved = ResolveName(name);
        ThrowIfArrayAsScalar(name.Text, resolved);

        return variables.GetOrCreateScalarString(name, fallback);
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

        ThrowIfArrayAsScalar(name.Text, ResolveName(name));

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
        local.Kind != AwkFunctionLocalSlotKind.ArrayAlias;

    public AwkArrayName ResolveOrPromoteArrayName(AwkArrayName name)
    {
        var resolved = ResolveName(new AwkVariableName(name.Identifier));
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalArrayAlias)
            return ResolveOrPromoteArrayName(resolved.ArrayName);

        if (functions.TryPromotePotentialArrayAlias(new AwkVariableName(name.Identifier), out var alias))
        {
            return ResolveOrPromoteArrayName(alias);
        }

        if (!string.Equals(name.Text, AwkVariables.SymbolTableName, StringComparison.Ordinal))
            EnsureArrayName(name);

        return name;
    }

    public void EnsureArrayName(AwkArrayName name)
    {
        var resolved = ResolveName(new AwkVariableName(name.Identifier));
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalScalar)
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
                _ => AwkVariableNameResolution.LocalScalar
            };
        }

        if (string.Equals(name.Text, AwkVariables.SymbolTableName, StringComparison.Ordinal))
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
        LocalArrayAlias,
        PotentialArrayAlias,
        SymbolTableArray,
        GlobalArray,
        GlobalScalar
    }
}
