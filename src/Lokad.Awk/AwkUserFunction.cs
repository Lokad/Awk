using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkUserFunction
{
    // Preparation finishes before the program is published to any execution.
    internal AwkRegion? Region { get; private set; }

    internal void PrepareRegion(AwkRegion region)
    {
        Region = region;
    }

    internal AwkUserFunction(AwkFunctionName name, AwkFunctionLocalLayout localLayout)
    {
        FunctionName = name;
        LocalLayout = localLayout;
    }

    public AwkFunctionName FunctionName { get; }

    public string Name => FunctionName.Text;

    public AwkFunctionLocalLayout LocalLayout { get; }

    public IReadOnlyList<AwkVariableName> Parameters => LocalLayout.Parameters;
}

internal readonly struct AwkFunctionArgument
{
    private AwkFunctionArgument(
        AwkFunctionArgumentKind kind,
        AwkValue value,
        AwkArrayName arrayName)
    {
        Kind = kind;
        Value = value;
        ArrayName = arrayName;
    }

    public AwkFunctionArgumentKind Kind { get; }

    public AwkValue Value { get; }

    public AwkArrayName ArrayName { get; }

    public static AwkFunctionArgument Scalar(AwkValue value) =>
        new(AwkFunctionArgumentKind.Scalar, value, default);

    public static AwkFunctionArgument FromValue(AwkValue value) =>
        value.TryGetArrayBinding(out var array)
            ? Array(array)
            : value.TryGetArrayName(out var name)
            ? Array(name)
            : value.IsUntypedValue
            ? new(AwkFunctionArgumentKind.UntypedScalar, value, default)
            : Scalar(value.AsScalarCopy());

    public static AwkFunctionArgument Array(AwkArrayName arrayName) =>
        new(AwkFunctionArgumentKind.Array, default, arrayName);

    public static AwkFunctionArgument Array(AwkArrayIdentity array) =>
        new(AwkFunctionArgumentKind.Array, AwkValue.FromArrayReference(array), default);

    public static AwkFunctionArgument PotentialArray(AwkArrayName arrayName) =>
        PotentialArray(arrayName, AwkValue.FromUntypedScalar(new AwkUntypedScalar()));

    public static AwkFunctionArgument PotentialArray(AwkArrayName arrayName, AwkValue value) =>
        new(AwkFunctionArgumentKind.PotentialArray, value, arrayName);
}

internal enum AwkFunctionArgumentKind
{
    Scalar,
    Array,
    PotentialArray,
    UntypedScalar
}
