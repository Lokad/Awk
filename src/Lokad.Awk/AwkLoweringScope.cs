namespace Lokad.Awk;

internal sealed class AwkLoweringScope
{
    private readonly AwkFunctionLocalLayout _functionLocals;
    private readonly bool _hasFunctionLocals;

    private AwkLoweringScope(AwkFunctionLocalLayout functionLocals, bool hasFunctionLocals)
    {
        _functionLocals = functionLocals;
        _hasFunctionLocals = hasFunctionLocals;
    }

    public static AwkLoweringScope Global { get; } =
        new(AwkFunctionLocalLayout.Empty, hasFunctionLocals: false);

    public static AwkLoweringScope Function(AwkFunctionLocalLayout functionLocals) =>
        new(functionLocals, hasFunctionLocals: true);

    public bool TryGetFunctionLocalSlot(AwkVariableName name, out AwkFunctionLocalSlotId slot)
    {
        if (_hasFunctionLocals && _functionLocals.TryGetSlot(name, out slot))
            return true;

        slot = default;
        return false;
    }
}
