using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionLocalLayout
{
    private readonly Dictionary<AwkVariableName, AwkFunctionLocalSlotId> _slotsByName;
    private AwkParameterInput[] _inputs = [];

    internal void PrepareInputs(AwkParameterInput[] inputs) => _inputs = inputs;

    internal AwkParameterInput InputConsumption(int index) =>
        (uint)index < (uint)_inputs.Length ? _inputs[index] : AwkParameterInput.Value;

    public static AwkFunctionLocalLayout Empty { get; } = new([]);

    public AwkFunctionLocalLayout(IReadOnlyList<AwkVariableName> parameters, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var copiedParameters = new AwkVariableName[parameters.Count];
        _slotsByName = new Dictionary<AwkVariableName, AwkFunctionLocalSlotId>(parameters.Count);
        for (var index = 0; index < parameters.Count; index++)
        {
            checkpoint?.Invoke();
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
internal enum AwkParameterInput : byte { Value, Arithmetic, Key }

internal sealed class AwkFunctionFrame
{
    private AwkFunctionLocalSlot _firstSlot;
    private AwkFunctionLocalSlot _secondSlot;
    private AwkFunctionLocalSlot[] _slots = [];

    // The call stack funds retained capacity, including across smaller activations.
    public AwkFunctionLocalLayout Layout { get; private set; } = AwkFunctionLocalLayout.Empty;

    public bool UsesInlineSlots => Layout.SlotCount is 1 or 2;

    public int SlotCapacity => _slots.Length;

    // Only the call stack retires backing from an inactive, cleared frame.
    public void DiscardSlotBuffer() => _slots = [];

    public ref AwkFunctionLocalSlot GetSlot(int index) =>
        ref (UsesInlineSlots
            ? ref (index == 0 ? ref _firstSlot : ref _secondSlot)
            : ref _slots[index]);

    internal bool TryReadNumber(int index, out double number)
    {
        ref readonly var slot = ref GetSlot(index);
        return slot.TryReadNumber(out number);
    }

    internal bool TryWriteNumber(int index, double number)
    {
        ref var slot = ref GetSlot(index);
        // Numeric and omitted-local cells own no text, alias or pending user. The
        // caller has already checked cancellation before the observable store.
        if (slot.Kind != AwkFunctionLocalSlotKind.MissingScalar &&
            !slot.TryReadNumber(out _))
            return false;
        slot = AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromNumber(number));
        return true;
    }

    public long RetainedMemoryBytes { get; set; }

    internal bool IsIncoming { get; set; }
    internal int ReconcileEnd { get; private set; }
    internal int PotentialAliasEnd { get; private set; }
    internal int AliasEnd { get; private set; }
    // Binding/ownership ancestry also includes callers hidden by a global phase;
    // it need not equal the execution continuation's caller frame.
    internal AwkFunctionFrame? Parent { get; set; }
    private AwkRegion? _callerRegion;
    private AwkRegionStorage? _callerStorage;
    private AwkFunctionFrame? _callerFrame;
    private AwkRegionResumePoint _resumePoint;
    internal bool HasAssignmentReturnConsumer => _resumePoint.IsAssignment;

    internal void SetCaller(AwkRegion region, AwkRegionStorage storage, int position,
        AwkFunctionFrame? frame, int destination, AwkRegionReturnConsumer consumer)
    {
        _callerRegion = region;
        _callerStorage = storage;
        _callerFrame = frame;
        _resumePoint = new(position, destination, consumer);
    }

    internal bool TryGetCaller([NotNullWhen(true)] out AwkRegion? region,
        [NotNullWhen(true)] out AwkRegionStorage? storage, out AwkFunctionFrame? frame,
        out AwkRegionResumePoint resumePoint)
    {
        region = _callerRegion;
        storage = _callerStorage;
        frame = _callerFrame;
        resumePoint = _resumePoint;
        return region is not null && storage is not null;
    }

    private AwkRegionStorage? _regionStorage;
    internal AwkRegionStorage GetRegionStorage(AwkRuntimeState state) => _regionStorage ??= new(state);

    public long Id { get; private set; }

    public void Initialize(long id, AwkFunctionLocalLayout layout)
    {
        var slots = _slots;
        if (layout.SlotCount > 2 && slots.Length == 0)
            slots = new AwkFunctionLocalSlot[layout.SlotCount];
        Id = id;
        Layout = layout;
        _slots = slots;
    }

    internal void RecordArgument(int index, AwkFunctionLocalSlotKind kind)
    {
        if (kind is AwkFunctionLocalSlotKind.ArrayAlias or AwkFunctionLocalSlotKind.UntypedScalar)
            ReconcileEnd = Math.Max(ReconcileEnd, index + 1);
        if (kind == AwkFunctionLocalSlotKind.PotentialArrayAlias)
            PotentialAliasEnd = Math.Max(PotentialAliasEnd, index + 1);
        RecordAlias(index, kind);
    }

    internal void RecordAlias(int index, AwkFunctionLocalSlotKind kind)
    {
        if (kind is AwkFunctionLocalSlotKind.ArrayAlias or AwkFunctionLocalSlotKind.PotentialArrayAlias)
            AliasEnd = Math.Max(AliasEnd, index + 1);
    }

    public void Reset()
    {
        if (!UsesInlineSlots && Layout.SlotCount > 0) Array.Clear(_slots, 0, Layout.SlotCount);
        Layout = AwkFunctionLocalLayout.Empty;
        _firstSlot = default;
        _secondSlot = default;
        RetainedMemoryBytes = 0;
        IsIncoming = false;
        ReconcileEnd = 0;
        PotentialAliasEnd = 0;
        AliasEnd = 0;
        Parent = null;
        _callerRegion = null;
        _callerStorage = null;
        _callerFrame = null;
        _resumePoint = default;
        _regionStorage?.RetireActivation();
        Id = 0;
    }
}

internal readonly struct AwkFunctionLocalSlot
{
    // Scalar and key payloads are mutually exclusive. The role selects the meaning
    // of these owning words; array names retain their full independent identity.
    private readonly object? _reference;
    private readonly long _word;
    private readonly int _payloadTag;

    private AwkFunctionLocalSlot(
        AwkFunctionLocalSlotKind kind,
        AwkVariableSlot value,
        AwkArrayName arrayName)
    {
        Kind = kind;
        _reference = value.StoredReference;
        _word = BitConverter.DoubleToInt64Bits(value.StoredNumber);
        _payloadTag = (int)value.Kind;
        ArrayName = arrayName;
    }

    private AwkFunctionLocalSlot(AwkArrayKey key)
    {
        Kind = AwkFunctionLocalSlotKind.Key;
        _reference = key.StoredText.Backing;
        _word = key.StoredOrdinal;
        _payloadTag = key.StorageTag;
        ArrayName = default;
    }

    public AwkFunctionLocalSlotKind Kind { get; }

    // Cleared cells are missing locals. Their language value is unassigned,
    // independently of the ordinary scalar slot's text-valued default.
    public AwkVariableSlot Value => Kind is AwkFunctionLocalSlotKind.MissingScalar or AwkFunctionLocalSlotKind.Key
        ? AwkVariableSlot.Unassigned()
        : AwkVariableSlot.FromStorage(_reference, BitConverter.Int64BitsToDouble(_word), (AwkVariableSlotKind)_payloadTag);

    public AwkArrayName ArrayName { get; }

    // Only prepared key consumers read this owning value. It never borrows a record.
    public AwkArrayKey Key => Kind == AwkFunctionLocalSlotKind.Key
        ? AwkArrayKey.FromStorage(AwkText.FromBacking(_reference), unchecked((int)_word), _payloadTag) : default;

    internal bool TryReadNumber(out double number)
    {
        if (Kind == AwkFunctionLocalSlotKind.Scalar && _payloadTag == (int)AwkVariableSlotKind.Number)
        {
            number = BitConverter.Int64BitsToDouble(_word);
            return true;
        }
        number = 0;
        return false;
    }

    public static AwkFunctionLocalSlot PreparedKey(AwkArrayKey key) =>
        new(key);

    public static AwkFunctionLocalSlot Scalar(AwkVariableSlot value) =>
        new(AwkFunctionLocalSlotKind.Scalar, value, default);

    public static AwkFunctionLocalSlot Scalar(AwkValue value) => Scalar(AwkVariableSlot.FromValue(value));

    // Produce the actual incoming cell, preserving mutable untyped origins until
    // activation. Immutable scalar values need only one representation conversion.
    public static AwkFunctionLocalSlot FromArgumentValue(AwkValue value) =>
        value.TryGetArrayBinding(out var array) ? ArrayAlias(array) :
        value.TryGetArrayName(out var name) ? ArrayAlias(name) :
        value.IsUntypedValue ? UntypedScalar(value) : Scalar(value);

    public static AwkFunctionLocalSlot MissingScalar() => default;

    public static AwkFunctionLocalSlot ArrayAlias(AwkArrayName arrayName, AwkArrayIdentity? array = null) =>
        new(AwkFunctionLocalSlotKind.ArrayAlias,
            array is null ? default : AwkVariableSlot.FromArrayBinding(array), arrayName);

    public static AwkFunctionLocalSlot ArrayAlias(AwkArrayIdentity array) => ArrayAlias(default, array);

    public static AwkFunctionLocalSlot PotentialArrayAlias(AwkArrayName arrayName) =>
        PotentialArrayAlias(arrayName, AwkValue.FromUntypedScalar(new AwkUntypedScalar()));

    public static AwkFunctionLocalSlot PotentialArrayAlias(AwkArrayName arrayName, AwkValue value) =>
        new(AwkFunctionLocalSlotKind.PotentialArrayAlias, AwkVariableSlot.FromArgumentValue(value), arrayName);

    public static AwkFunctionLocalSlot UntypedScalar(AwkValue value) =>
        new(AwkFunctionLocalSlotKind.UntypedScalar, AwkVariableSlot.FromArgumentValue(value), default);
}

internal enum AwkFunctionLocalSlotKind
{
    MissingScalar,
    Scalar,
    ArrayAlias,
    PotentialArrayAlias,
    UntypedScalar,
    Key
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

    public static AwkResolvedFunctionLocal UntypedScalar(AwkValue value) =>
        new(AwkFunctionLocalSlotKind.UntypedScalar, value, default);

    public static AwkResolvedFunctionLocal NotFound { get; } =
        new(AwkFunctionLocalSlotKind.Scalar, AwkValue.Unassigned(), default);
}
