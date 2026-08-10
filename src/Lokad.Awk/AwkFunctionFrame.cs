using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFunctionLocalLayout
{
    private readonly Dictionary<AwkVariableName, AwkFunctionLocalSlotId> _slotsByName;

    public static AwkFunctionLocalLayout Empty { get; } = new([]);

    public AwkFunctionLocalLayout(IReadOnlyList<AwkVariableName> parameters)
    {
        var copiedParameters = new AwkVariableName[parameters.Count];
        _slotsByName = new Dictionary<AwkVariableName, AwkFunctionLocalSlotId>(parameters.Count);
        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            copiedParameters[index] = parameter;
            _slotsByName[parameter] = new AwkFunctionLocalSlotId(index);
        }

        Parameters = copiedParameters;
    }

    public AwkVariableName[] Parameters { get; }

    public int SlotCount => Parameters.Length;

    public bool TryGetSlot(AwkVariableName name, out AwkFunctionLocalSlotId slot) =>
        _slotsByName.TryGetValue(name, out slot);
}

internal readonly record struct AwkFunctionLocalSlotId(int Index);

internal sealed class AwkFunctionFrame
{
    // Slot arrays are pooled and may be larger than the active layout. SlotCount
    // is therefore the authoritative bound while the frame is initialized.
    public AwkFunctionLocalLayout Layout { get; private set; } = AwkFunctionLocalLayout.Empty;

    public AwkFunctionLocalSlot[] Slots { get; private set; } = [];

    public bool HasReturnValue { get; set; }

    public AwkValue ReturnValue { get; set; } = AwkValue.FromText(string.Empty);

    public long RetainedMemoryBytes { get; set; }

    public long Id { get; private set; }

    public void Initialize(long id, AwkFunctionLocalLayout layout, AwkFunctionLocalSlot[] slots)
    {
        Id = id;
        Layout = layout;
        Slots = slots;
    }

    public AwkFunctionLocalSlot[] Reset()
    {
        var slots = Slots;
        Layout = AwkFunctionLocalLayout.Empty;
        Slots = [];
        HasReturnValue = false;
        ReturnValue = AwkValue.FromText(string.Empty);
        RetainedMemoryBytes = 0;
        Id = 0;
        return slots;
    }
}

internal readonly struct AwkFunctionLocalSlot
{
    private AwkFunctionLocalSlot(
        AwkFunctionLocalSlotKind kind,
        AwkVariableSlot value,
        AwkArrayName arrayName)
    {
        Kind = kind;
        Value = value;
        ArrayName = arrayName;
    }

    public AwkFunctionLocalSlotKind Kind { get; }

    public AwkVariableSlot Value { get; }

    public AwkArrayName ArrayName { get; }

    public static AwkFunctionLocalSlot Scalar(AwkVariableSlot value) =>
        new(AwkFunctionLocalSlotKind.Scalar, value, default);

    public static AwkFunctionLocalSlot MissingScalar() =>
        new(
            AwkFunctionLocalSlotKind.MissingScalar,
            new AwkVariableSlot(string.Empty, AwkVariableSlotKind.Unassigned),
            default);

    public static AwkFunctionLocalSlot ArrayAlias(AwkArrayName arrayName) =>
        new(AwkFunctionLocalSlotKind.ArrayAlias, default, arrayName);

    public static AwkFunctionLocalSlot PotentialArrayAlias(AwkArrayName arrayName) =>
        new(AwkFunctionLocalSlotKind.PotentialArrayAlias, default, arrayName);
}

internal enum AwkFunctionLocalSlotKind
{
    Scalar,
    MissingScalar,
    ArrayAlias,
    PotentialArrayAlias
}

internal readonly struct AwkResolvedFunctionLocal
{
    private AwkResolvedFunctionLocal(
        AwkFunctionLocalSlotKind kind,
        AwkValue value,
        AwkArrayName arrayName)
    {
        Kind = kind;
        Value = value;
        ArrayName = arrayName;
    }

    public AwkFunctionLocalSlotKind Kind { get; }

    public AwkValue Value { get; }

    public AwkArrayName ArrayName { get; }

    public static AwkResolvedFunctionLocal Scalar() =>
        new(AwkFunctionLocalSlotKind.Scalar, AwkValue.Unassigned(), default);

    public static AwkResolvedFunctionLocal ScalarValue(AwkValue value) =>
        new(AwkFunctionLocalSlotKind.Scalar, value, default);

    public static AwkResolvedFunctionLocal MissingScalar() =>
        new(AwkFunctionLocalSlotKind.MissingScalar, AwkValue.Unassigned(), default);

    public static AwkResolvedFunctionLocal ArrayAlias(AwkArrayName arrayName) =>
        new(AwkFunctionLocalSlotKind.ArrayAlias, AwkValue.Unassigned(), arrayName);

    public static AwkResolvedFunctionLocal PotentialArrayAlias(AwkArrayName arrayName) =>
        new(AwkFunctionLocalSlotKind.PotentialArrayAlias, AwkValue.Unassigned(), arrayName);

    public static AwkResolvedFunctionLocal NotFound { get; } =
        new(AwkFunctionLocalSlotKind.Scalar, AwkValue.Unassigned(), default);
}
