namespace Lokad.Awk;

internal sealed class AwkLoweringScope
{
    private readonly AwkFunctionLocalLayout _functionLocals;
    private readonly bool _hasFunctionLocals;
    private readonly AwkGlobalReadLayout? _globalReads;

    private AwkLoweringScope(AwkFunctionLocalLayout functionLocals, bool hasFunctionLocals, AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null, AwkGlobalReadLayout? globalReads = null,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? synchronousFunctions = null,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction>? functions = null)
    {
        _functionLocals = functionLocals;
        _hasFunctionLocals = hasFunctionLocals;
        CompatibilityMode = compatibilityMode;
        Checkpoint = checkpoint;
        _globalReads = globalReads;
        SynchronousFunctions = synchronousFunctions;
        Functions = functions;
    }

    public static AwkLoweringScope Global { get; } =
        new(AwkFunctionLocalLayout.Empty, hasFunctionLocals: false);

    public static AwkLoweringScope GlobalFor(AwkCompatibilityMode mode, Action? checkpoint = null,
        AwkGlobalReadLayout? globalReads = null,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? synchronousFunctions = null,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction>? functions = null) =>
        mode == AwkCompatibilityMode.GnuDefault && checkpoint is null && globalReads is null && synchronousFunctions is null && functions is null
            ? Global
            : new(AwkFunctionLocalLayout.Empty, false, mode, checkpoint, globalReads, synchronousFunctions, functions);

    internal IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? SynchronousFunctions { get; }
    internal IReadOnlyDictionary<AwkFunctionName, AwkUserFunction>? Functions { get; }

    public static AwkLoweringScope Function(AwkFunctionLocalLayout functionLocals,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null, AwkGlobalReadLayout? globalReads = null,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction>? functions = null) =>
        new(functionLocals, true, mode, checkpoint, globalReads, functions: functions);

    public bool TryGetGlobalReadIndex(AwkVariableName name, out int index)
    {
        if (_globalReads is not null)
            return _globalReads.TryGetIndex(name, out index);
        index = default;
        return false;
    }

    internal AwkArrayBinding GetArrayBinding(AwkIdentifier identifier)
    {
        var name = new AwkVariableName(identifier);
        if (TryGetFunctionLocalSlot(name, out var slot)) return AwkArrayBinding.Local(slot);
        return !_hasFunctionLocals && TryGetGlobalReadIndex(name, out var index)
            ? AwkArrayBinding.Global(index) : default;
    }

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
