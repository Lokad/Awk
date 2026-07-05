using System;

namespace Lokad.Awk;

internal sealed class AwkVariableRuntime(
    AwkVariables variables,
    AwkFunctionRuntime functions,
    AwkSpecialVariableDispatcher specialVariables)
{
    public bool HasScalar(string name) => ResolveName(name).Kind == AwkVariableNameResolutionKind.GlobalScalar;

    public bool HasScalar(AwkIdentifier identifier) => HasScalar(identifier.Text);

    public bool HasScalar(AwkVariableName name) => HasScalar(name.Text);

    public bool IsArray(string name) =>
        ResolveName(name).Kind is
            AwkVariableNameResolutionKind.LocalArrayAlias or
            AwkVariableNameResolutionKind.SymbolTableArray or
            AwkVariableNameResolutionKind.GlobalArray;

    public bool IsArray(AwkIdentifier identifier) => IsArray(identifier.Text);

    public bool IsArray(AwkArrayName name) => IsArray(name.Text);

    public bool TryGetExistingArrayCount(string name, out int count)
    {
        var resolved = ResolveName(name);
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalArrayAlias)
            return TryGetExistingArrayCount(resolved.ArrayName, out count);

        if (AwkIdentifier.TryParse(name, out var identifier))
            return variables.TryGetArrayCount(new AwkArrayName(identifier), out count);

        count = 0;
        return false;
    }

    public bool TryGetExistingArrayCount(AwkArrayName name, out int count)
    {
        var resolved = ResolveName(name.Text);
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalArrayAlias)
            return TryGetExistingArrayCount(AwkArrayName.FromText(resolved.ArrayName), out count);

        return variables.TryGetArrayCount(name, out count);
    }

    public string ResolveVariable(string name)
    {
        if (functions.TryResolveLocalText(name, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Text;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return string.Empty;

            ThrowIfArrayAsScalar(name, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        var resolved = ResolveName(name);
        ThrowIfArrayAsScalar(name, resolved);

        return variables.GetOrCreateScalarString(name);
    }

    public AwkValue ResolveVariableValue(string name)
    {
        if (functions.TryResolveLocalValue(name, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Value;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return AwkValue.Unassigned();

            ThrowIfArrayAsScalar(name, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        var resolved = ResolveName(name);
        ThrowIfArrayAsScalar(name, resolved);

        return variables.GetOrCreateScalarValue(name);
    }

    public AwkValue ResolveVariableValue(AwkVariableName name) =>
        ResolveVariableValue(name.Text);

    public string ResolveVariable(AwkVariableName name) =>
        ResolveVariable(name.Text);

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

    public string ResolveVariable(string name, string fallback)
    {
        if (functions.TryResolveLocalText(name, out var local))
        {
            if (local.Kind == AwkFunctionLocalSlotKind.Scalar)
                return local.Text;

            if (local.Kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
                return string.Empty;

            ThrowIfArrayAsScalar(name, AwkVariableNameResolution.LocalArrayAlias(local.ArrayName));
        }

        var resolved = ResolveName(name);
        ThrowIfArrayAsScalar(name, resolved);

        return variables.GetOrCreateScalarString(name, fallback);
    }

    public string ResolveVariable(AwkVariableName name, string fallback) =>
        ResolveVariable(name.Text, fallback);

    public void AssignVariable(string name, string value)
    {
        if (functions.TryAssignLocal(name, value))
            return;

        ThrowIfArrayAsScalar(name, ResolveName(name));

        variables.SetScalarText(name, value);
        specialVariables.ApplyAssignment(name, value);
    }

    public void AssignVariable(AwkIdentifier identifier, string value) =>
        AssignVariable(identifier.Text, value);

    public void AssignVariable(AwkVariableName name, string value)
    {
        if (functions.TryAssignLocal(name.Text, value))
            return;

        ThrowIfArrayAsScalar(name.Text, ResolveName(name.Text));

        variables.SetScalarText(name.Text, value);
        if (AwkSpecialVariables.TryGetScalar(name, out var id))
            specialVariables.ApplyAssignment(id, value);
    }

    public void AssignVariableValue(string name, AwkValue value)
    {
        if (functions.TryAssignLocal(name, value))
            return;

        ThrowIfArrayAsScalar(name, ResolveName(name));

        variables.SetScalarValue(name, value);
        specialVariables.ApplyAssignment(name, value);
    }

    public void AssignVariableValue(AwkIdentifier identifier, AwkValue value) =>
        AssignVariableValue(identifier.Text, value);

    public void AssignVariableValue(AwkVariableName name, AwkValue value)
    {
        if (functions.TryAssignLocal(name.Text, value))
            return;

        ThrowIfArrayAsScalar(name.Text, ResolveName(name.Text));

        variables.SetScalarValue(name.Text, value);
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
        ThrowIfArrayAsScalar(name.Text, ResolveName(name.Text));
        return variables.GetOrCreateScalarSlot(name);
    }

    public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) =>
        variables.GetScalarValue(id);

    public void AssignGlobalScalarValue(AwkGlobalSlotId id, AwkValue value)
    {
        variables.SetScalarValue(id, value);
    }

    public bool IsLocalScalar(string name) =>
        functions.TryResolveLocal(name, out var local) &&
        local.Kind != AwkFunctionLocalSlotKind.ArrayAlias;

    public AwkArrayName ResolveOrPromoteArrayName(AwkArrayName name)
    {
        var resolved = ResolveName(name.Text);
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalArrayAlias)
            return ResolveOrPromoteArrayName(AwkArrayName.FromText(resolved.ArrayName));

        if (functions.TryPromotePotentialArrayAlias(name.Text, out var alias))
        {
            return ResolveOrPromoteArrayName(AwkArrayName.FromText(alias));
        }

        if (!string.Equals(name.Text, AwkVariables.SymbolTableName, StringComparison.Ordinal))
            EnsureArrayName(name);

        return name;
    }

    public string ResolveOrPromoteArrayName(string name) =>
        ResolveOrPromoteArrayName(AwkArrayName.FromText(name)).Text;

    public void EnsureArrayName(string name)
    {
        EnsureArrayName(AwkArrayName.FromText(name));
    }

    public void EnsureArrayName(AwkArrayName name)
    {
        var resolved = ResolveName(name.Text);
        if (resolved.Kind == AwkVariableNameResolutionKind.LocalScalar)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        if (resolved.Kind == AwkVariableNameResolutionKind.GlobalScalar)
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));
    }

    private AwkVariableNameResolution ResolveName(string name)
    {
        if (functions.TryResolveLocal(name, out var local))
        {
            return local.Kind switch
            {
                AwkFunctionLocalSlotKind.ArrayAlias => AwkVariableNameResolution.LocalArrayAlias(local.ArrayName),
                AwkFunctionLocalSlotKind.PotentialArrayAlias => AwkVariableNameResolution.PotentialArrayAlias(local.ArrayName),
                _ => AwkVariableNameResolution.LocalScalar()
            };
        }

        if (string.Equals(name, AwkVariables.SymbolTableName, StringComparison.Ordinal))
            return AwkVariableNameResolution.SymbolTableArray;

        if (variables.ContainsArray(name))
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

    private readonly struct AwkVariableNameResolution
    {
        private AwkVariableNameResolution(
            AwkVariableNameResolutionKind kind,
            string text,
            AwkValue value,
            string arrayName)
        {
            Kind = kind;
            Text = text;
            Value = value;
            ArrayName = arrayName;
        }

        public AwkVariableNameResolutionKind Kind { get; }

        public string Text { get; }

        public AwkValue Value { get; }

        public string ArrayName { get; }

        public static AwkVariableNameResolution LocalScalar() =>
            new(AwkVariableNameResolutionKind.LocalScalar, string.Empty, AwkValue.Unassigned(), string.Empty);

        public static AwkVariableNameResolution LocalArrayAlias(string arrayName) =>
            new(AwkVariableNameResolutionKind.LocalArrayAlias, string.Empty, AwkValue.Unassigned(), arrayName);

        public static AwkVariableNameResolution PotentialArrayAlias(string arrayName) =>
            new(AwkVariableNameResolutionKind.PotentialArrayAlias, string.Empty, AwkValue.Unassigned(), arrayName);

        public static AwkVariableNameResolution SymbolTableArray { get; } =
            new(AwkVariableNameResolutionKind.SymbolTableArray, string.Empty, AwkValue.Unassigned(), string.Empty);

        public static AwkVariableNameResolution GlobalArray { get; } =
            new(AwkVariableNameResolutionKind.GlobalArray, string.Empty, AwkValue.Unassigned(), string.Empty);

        public static AwkVariableNameResolution GlobalScalar { get; } =
            new(AwkVariableNameResolutionKind.GlobalScalar, string.Empty, AwkValue.Unassigned(), string.Empty);

        public static AwkVariableNameResolution Missing { get; } =
            new(AwkVariableNameResolutionKind.Missing, string.Empty, AwkValue.Unassigned(), string.Empty);
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
