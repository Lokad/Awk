namespace Lokad.Awk;

internal sealed class AwkLoweringScope
{
    private readonly AwkFunctionLocalLayout _functionLocals;
    private readonly bool _hasFunctionLocals;

    private AwkLoweringScope(AwkFunctionLocalLayout functionLocals, bool hasFunctionLocals, AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
    {
        _functionLocals = functionLocals;
        _hasFunctionLocals = hasFunctionLocals;
        CompatibilityMode = compatibilityMode;
    }

    public static AwkLoweringScope Global { get; } =
        new(AwkFunctionLocalLayout.Empty, hasFunctionLocals: false);

    public static AwkLoweringScope GlobalFor(AwkCompatibilityMode mode) =>
        mode == AwkCompatibilityMode.GnuDefault ? Global : new(AwkFunctionLocalLayout.Empty, false, mode);

    public static AwkLoweringScope Function(AwkFunctionLocalLayout functionLocals,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault) => new(functionLocals, true, mode);

    public AwkCompatibilityMode CompatibilityMode { get; }

    public bool IsReservedVariable(string name) =>
        !TryGetFunctionLocalSlot(AwkVariableName.FromText(name), out _) &&
        AwkCompatibilityNames.IsReservedName(name, CompatibilityMode);

    public bool TryGetFunctionLocalSlot(AwkVariableName name, out AwkFunctionLocalSlotId slot)
    {
        if (_hasFunctionLocals && _functionLocals.TryGetSlot(name, out slot))
            return true;

        slot = default;
        return false;
    }
}
