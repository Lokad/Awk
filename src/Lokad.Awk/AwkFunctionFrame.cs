using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkFunctionLocalLayout
{
    private readonly Dictionary<AwkVariableName, AwkFunctionLocalSlotId> _slotsByName;
    private AwkParameterInput[] _inputs = [];

    internal void PrepareInputs(AwkParameterInput[] inputs)
    {
        _inputs = inputs;
        HasTextInput = inputs.Contains(AwkParameterInput.Text);
    }
    internal bool HasTextInput { get; private set; }

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
internal enum AwkParameterInput : byte { Value, Arithmetic, Key, NumericComparison, Text }

internal sealed class AwkFunctionFrame
{
    private AwkFunctionLocalSlot[] _slots = [];

    // The call stack funds retained capacity, including across smaller activations.
    public AwkFunctionLocalLayout Layout { get; private set; } = AwkFunctionLocalLayout.Empty;

    public int SlotCapacity => _slots.Length;

    // Only the call stack retires backing from an inactive, cleared frame.
    public void DiscardSlotBuffer() => _slots = [];

    public ref AwkFunctionLocalSlot GetSlot(int index) => ref _slots[index];

    internal bool TryReadNumber(int index, out double number)
    {
        ref readonly var slot = ref GetSlot(index);
        return slot.TryReadNumber(out number);
    }

    internal bool TryReadNumericScalar(int index, out double number, Action? checkpoint)
    {
        ref readonly var slot = ref GetSlot(index);
        if (slot.Kind == AwkFunctionLocalSlotKind.Scalar)
            return slot.Value.TryGetStoredComparisonNumber(out number, checkpoint);
        number = 0;
        return false;
    }

    // Argument segments initialize each incoming cell once, while the caller's
    // bindings remain active. Cleared retained capacity is already funded.
    internal void StageNumber(int index, double number)
    {
        System.Diagnostics.Debug.Assert(IsIncoming);
        if ((uint)index < (uint)Layout.SlotCount)
            GetSlot(index).InitializeNumber(number);
    }

    internal void StageScalar(int index, in AwkValue value)
    {
        System.Diagnostics.Debug.Assert(IsIncoming);
        GetSlot(index).InitializeScalar(in value);
    }

    internal void StageReaderText(int index, AwkReadBuffer borrow, int offset, int length)
    {
        System.Diagnostics.Debug.Assert(IsIncoming);
        GetSlot(index).InitializeReaderText(borrow, offset, length);
    }

    internal bool TryWriteNumber(int index, double number)
    {
        ref var slot = ref GetSlot(index);
        // Numeric and omitted-local cells own no text, alias or pending user. The
        // caller has already checked cancellation before the observable store.
        return slot.TryWriteNumber(number);
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
        if (layout.SlotCount > 0 && _slots.Length == 0)
            _slots = new AwkFunctionLocalSlot[layout.SlotCount];
        Id = id;
        Layout = layout;
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
        // Inactive storage was cleared when its last activation retired. Clear
        // only the cells that this activation could have published into.
        var count = Layout.SlotCount;
        Array.Clear(_slots, 0, count);
        Layout = AwkFunctionLocalLayout.Empty;
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

internal struct AwkFunctionLocalSlot
{
    // Scalar and key payloads are mutually exclusive. The role selects the meaning
    // of these owning words; array names retain their full independent identity.
    private object? _reference;
    private long _word;
    private int _payloadTag;

    private AwkFunctionLocalSlot(
        AwkFunctionLocalSlotKind kind,
        AwkValue value,
        AwkArrayName arrayName)
    {
        Kind = kind;
        _reference = value.StoredReference;
        _word = BitConverter.DoubleToInt64Bits(value.StoredNumber);
        _payloadTag = (int)value.StoredKind;
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

    internal readonly bool TryGetReaderText(out ReadOnlyMemory<byte> bytes)
    {
        if (Kind == AwkFunctionLocalSlotKind.ReaderText && _reference is AwkReadBuffer borrow)
        {
            bytes = borrow.Content.Slice((int)(_word >> 32), (int)_word);
            return true;
        }
        bytes = default;
        return false;
    }

    internal readonly bool TryGetText(out AwkByteTextOperand text, Action? checkpoint)
    {
        if (TryGetReaderText(out var bytes))
        {
            text = new(bytes.Span);
            return true;
        }
        var kind = (AwkValueKind)_payloadTag;
        if (Kind != AwkFunctionLocalSlotKind.Scalar || !AwkValue.HasStoredScalarText(kind))
        {
            text = default;
            return false;
        }
        text = new(AwkText.FromBacking(_reference));
        if (AwkValue.HasInputClassification(kind) && text.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes)
            checkpoint?.Invoke();
        return true;
    }

    internal readonly bool HasSameReaderText(in AwkFunctionLocalSlot other) =>
        Kind == AwkFunctionLocalSlotKind.ReaderText && other.Kind == Kind &&
        ReferenceEquals(_reference, other._reference) && _word == other._word;

    public AwkFunctionLocalSlotKind Kind { readonly get; private set; }

    // Cleared cells are missing locals. Their language value is unassigned,
    // independently of the scalar value's text-valued default.
    public readonly AwkValue Value => Kind is AwkFunctionLocalSlotKind.MissingScalar or AwkFunctionLocalSlotKind.Key or AwkFunctionLocalSlotKind.ReaderText
        ? AwkValue.Unassigned()
        : AwkValue.FromStorage(_reference, BitConverter.Int64BitsToDouble(_word), (AwkValueKind)_payloadTag);

    internal readonly AwkValue ReadValue(Action? checkpoint)
    {
        if (Kind == AwkFunctionLocalSlotKind.ReaderText)
            throw new InvalidOperationException("Reader text requires its funded frame consumer.");
        // Capture before the callback; the cell can retire while it runs.
        if (Kind is AwkFunctionLocalSlotKind.MissingScalar or AwkFunctionLocalSlotKind.Key)
            return AwkValue.Unassigned();
        var reference = _reference;
        var word = _word;
        var kind = (AwkValueKind)_payloadTag;
        if (AwkValue.HasInputClassification(kind) &&
            AwkText.FromBacking(reference).ByteLength >= AwkRuntimeLimits.CancellationCheckBytes)
            checkpoint?.Invoke();
        return AwkValue.FromStorage(reference, BitConverter.Int64BitsToDouble(word), kind);
    }

    public AwkArrayName ArrayName { get; }

    // Only prepared key consumers read this owning value. It never borrows a record.
    public readonly AwkArrayKey Key => Kind == AwkFunctionLocalSlotKind.Key
        ? AwkArrayKey.FromStorage(AwkText.FromBacking(_reference), unchecked((int)_word), _payloadTag) : default;

    internal readonly bool TryReadNumber(out double number)
    {
        if (Kind == AwkFunctionLocalSlotKind.Scalar && _payloadTag == (int)AwkValueKind.Number)
        {
            number = BitConverter.Int64BitsToDouble(_word);
            return true;
        }
        number = 0;
        return false;
    }

    // The frame owns this storage. Only cleared/numeric cells can reuse their
    // payload words; all other roles retain ordinary admission and retirement.
    // Value/key projections and copies remain independent snapshots.
    internal bool TryWriteNumber(double number)
    {
        if (Kind == AwkFunctionLocalSlotKind.MissingScalar)
        {
            InitializeNumber(number);
            return true;
        }
        if (Kind != AwkFunctionLocalSlotKind.Scalar || _payloadTag != (int)AwkValueKind.Number)
            return false;
        _word = BitConverter.DoubleToInt64Bits(number);
        return true;
    }

    internal void InitializeNumber(double number)
    {
        System.Diagnostics.Debug.Assert(Kind == AwkFunctionLocalSlotKind.MissingScalar);
        _word = BitConverter.DoubleToInt64Bits(number);
        _payloadTag = (int)AwkValueKind.Number;
        Kind = AwkFunctionLocalSlotKind.Scalar;
    }

    // Incoming scalar storage is already cleared and funded. Capture/admission
    // belongs to the call stack; initialize the actual cell without a cell temporary.
    internal void InitializeScalar(in AwkValue value)
    {
        System.Diagnostics.Debug.Assert(Kind == AwkFunctionLocalSlotKind.MissingScalar);
        _reference = value.StoredReference;
        _word = BitConverter.DoubleToInt64Bits(value.StoredNumber);
        _payloadTag = (int)value.StoredKind;
        Kind = AwkFunctionLocalSlotKind.Scalar;
    }

    internal void InitializeReaderText(AwkReadBuffer borrow, int offset, int length)
    {
        System.Diagnostics.Debug.Assert(Kind == AwkFunctionLocalSlotKind.MissingScalar);
        _reference = borrow;
        _word = ((long)offset << 32) | (uint)length;
        Kind = AwkFunctionLocalSlotKind.ReaderText;
    }

    public static AwkFunctionLocalSlot PreparedKey(AwkArrayKey key) =>
        new(key);

    public static AwkFunctionLocalSlot Scalar(AwkValue value) =>
        new(AwkFunctionLocalSlotKind.Scalar, value.AsScalarCopy(), default);

    // Produce the actual incoming cell, preserving mutable untyped origins until
    // activation. Immutable scalars keep their canonical value representation.
    public static AwkFunctionLocalSlot FromArgumentValue(AwkValue value) =>
        value.TryGetArrayBinding(out var array) ? ArrayAlias(array) :
        value.TryGetArrayName(out var name) ? ArrayAlias(name) :
        value.IsUntypedValue ? UntypedScalar(value) : Scalar(value);

    public static AwkFunctionLocalSlot MissingScalar() => default;

    public static AwkFunctionLocalSlot ArrayAlias(AwkArrayName arrayName, AwkArrayIdentity? array = null) =>
        new(AwkFunctionLocalSlotKind.ArrayAlias,
            array is null ? AwkValue.Unassigned() : AwkValue.FromArrayReference(array), arrayName);

    public static AwkFunctionLocalSlot ArrayAlias(AwkArrayIdentity array) => ArrayAlias(default, array);

    public static AwkFunctionLocalSlot PotentialArrayAlias(AwkArrayName arrayName) =>
        PotentialArrayAlias(arrayName, AwkValue.FromUntypedScalar(new AwkUntypedScalar()));

    public static AwkFunctionLocalSlot PotentialArrayAlias(AwkArrayName arrayName, AwkValue value) =>
        new(AwkFunctionLocalSlotKind.PotentialArrayAlias, value, arrayName);

    public static AwkFunctionLocalSlot UntypedScalar(AwkValue value) =>
        new(AwkFunctionLocalSlotKind.UntypedScalar, value, default);
}

internal enum AwkFunctionLocalSlotKind
{
    MissingScalar,
    Scalar,
    ArrayAlias,
    PotentialArrayAlias,
    UntypedScalar,
    Key,
    ReaderText
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
