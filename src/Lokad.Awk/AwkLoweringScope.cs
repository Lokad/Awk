namespace Lokad.Awk;

internal sealed class AwkLoweringScope
{
    private readonly AwkFunctionLocalLayout _functionLocals;
    private readonly bool _hasFunctionLocals;

    private AwkLoweringScope(AwkFunctionLocalLayout functionLocals, bool hasFunctionLocals, AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        _functionLocals = functionLocals;
        _hasFunctionLocals = hasFunctionLocals;
        CompatibilityMode = compatibilityMode;
        Checkpoint = checkpoint;
    }

    public static AwkLoweringScope Global { get; } =
        new(AwkFunctionLocalLayout.Empty, hasFunctionLocals: false);

    public static AwkLoweringScope GlobalFor(AwkCompatibilityMode mode, Action? checkpoint = null) =>
        mode == AwkCompatibilityMode.GnuDefault && checkpoint is null
            ? Global
            : new(AwkFunctionLocalLayout.Empty, false, mode, checkpoint);

    public static AwkLoweringScope Function(AwkFunctionLocalLayout functionLocals,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null) => new(functionLocals, true, mode, checkpoint);

    public AwkCompatibilityMode CompatibilityMode { get; }

    public Action? Checkpoint { get; }

    public void CheckCancellation() => Checkpoint?.Invoke();

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
