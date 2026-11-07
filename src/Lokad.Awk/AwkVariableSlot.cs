using System;

namespace Lokad.Awk;

internal readonly struct AwkVariableSlot
{
    private readonly object? _text;
    private readonly double _number;

    private AwkVariableSlot(AwkText text, AwkVariableSlotKind kind)
        : this(text, 0, kind)
    {
    }

    private AwkVariableSlot(AwkText text, double number, AwkVariableSlotKind kind)
    {
        _text = text.Backing;
        _number = number;
        Kind = kind;
    }

    private AwkVariableSlot(double number)
    {
        _text = null;
        _number = number;
        Kind = AwkVariableSlotKind.Number;
    }

    private AwkVariableSlot(AwkUntypedScalar scalar)
    {
        _text = scalar;
        _number = 0;
        Kind = AwkVariableSlotKind.Untyped;
    }

    private AwkVariableSlot(AwkArrayIdentity array)
    {
        _text = array;
        _number = 0;
        Kind = AwkVariableSlotKind.ArrayBinding;
    }

    internal static AwkVariableSlot FromArrayBinding(AwkArrayIdentity array) => new(array);

    internal bool TryGetArrayBinding([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        array = Kind switch
        {
            AwkVariableSlotKind.ArrayBinding => _text as AwkArrayIdentity,
            AwkVariableSlotKind.Untyped => (_text as AwkUntypedScalar)?.Array,
            _ => null
        };
        return array is not null;
    }

    internal bool TryGetUntypedScalar([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkUntypedScalar? scalar)
    {
        scalar = Kind == AwkVariableSlotKind.Untyped ? _text as AwkUntypedScalar : null;
        return scalar is not null;
    }

    private AwkVariableSlot(AwkText text, double number)
        : this(text, number, AwkVariableSlotKind.PreparedNumericString)
    {
    }

    internal AwkVariableSlot WithByteText(AwkText text) => new(text, _number, Kind);

    public static AwkVariableSlot FromUntypedScalar(AwkUntypedScalar scalar) => new(scalar);

    public static AwkVariableSlot FromArgumentValue(AwkValue value) =>
        value.TryGetUntypedScalar(out var scalar) ? FromUntypedScalar(scalar) : FromValue(value);

    public bool IsUntyped => Kind == AwkVariableSlotKind.Untyped &&
        _text is AwkUntypedScalar { IsScalar: false, Array: null };

    public void PromoteUntypedScalar()
    {
        if (_text is AwkUntypedScalar scalar)
            scalar.Promote();
    }

    public static AwkVariableSlot FromText(string text) =>
        FromText(AwkText.FromString(text));

    public static AwkVariableSlot FromText(AwkText text) =>
        new(text, AwkVariableSlotKind.Text);

    public static AwkVariableSlot FromExternalText(string text) =>
        FromExternalText(AwkText.FromString(text));

    public static AwkVariableSlot FromExternalText(AwkText text) =>
        new(text, AwkVariableSlotKind.NumericString);

    internal static AwkVariableSlot FromNumber(double value) => new(value);

    internal static AwkVariableSlot Unassigned() => new(AwkText.Empty, AwkVariableSlotKind.Unassigned);

    public static AwkVariableSlot FromValue(AwkValue value)
    {
        value = value.AsScalarCopy();
        if (value.IsNumberValue)
            return new AwkVariableSlot(value.AsNumber());
        if (value.TryGetPreparedNumericInput(out var number))
            return new AwkVariableSlot(value.ByteText, number);

        AwkVariableSlotKind kind;
        if (value.IsUnassignedValue)
            kind = AwkVariableSlotKind.Unassigned;
        else if (value.RetainsInputClassification)
            kind = AwkVariableSlotKind.NumericString;
        else
            kind = AwkVariableSlotKind.Text;

        return new AwkVariableSlot(value.ByteText, kind);
    }

    public AwkText ByteText =>
        Kind == AwkVariableSlotKind.Number
            ? AwkNumberFormatter.ToAwkText(_number)
            : AwkText.FromBacking(_text);

    // Text-only consumers need neither numeric classification nor an AwkValue.
    // Numbers still require the current formatting policy; untyped origins must
    // retain scalar promotion and array rejection through ordinary value reads.
    internal bool TryGetStoredScalarText(out AwkText text)
    {
        if (Kind is AwkVariableSlotKind.Text or AwkVariableSlotKind.NumericString or
            AwkVariableSlotKind.PreparedNumericString or
            AwkVariableSlotKind.Unassigned)
        {
            text = ByteText;
            return true;
        }
        text = AwkText.Empty;
        return false;
    }

    public long RetainedTextMemoryBytes =>
        Kind == AwkVariableSlotKind.Number
            ? 0
            : _text is AwkUntypedScalar
                ? AwkRuntimeMemory.EstimateUntypedScalarBytes()
                : AwkText.FromBacking(_text).EstimatedRetainedMemoryBytes;

    public AwkVariableSlotKind Kind { get; }

    public bool TryGetNumber(out double number)
    {
        number = _number;
        return Kind == AwkVariableSlotKind.Number;
    }

    // Selected only after the caller checks PreparedNumericString.
    public AwkValue GetPreparedNumericInput() =>
        AwkValue.FromPreparedNumericInput(ByteText, _number);

    // String slots already have their origin; only input slots need classification.
    // Numeric consumption of ordinary text uses the reading execution's mode.
    public AwkValue ToValue(
        Func<AwkText, AwkValue> createNumericStringValue,
        Func<AwkText, AwkValue> createStringValue) =>
        Kind switch
        {
            AwkVariableSlotKind.ArrayBinding when _text is AwkArrayIdentity array => AwkValue.FromArrayReference(array),
            AwkVariableSlotKind.Unassigned => AwkValue.Unassigned(),
            AwkVariableSlotKind.Untyped when _text is AwkUntypedScalar scalar => AwkValue.FromUntypedScalar(scalar),
            AwkVariableSlotKind.Number => AwkValue.FromNumber(_number),
            AwkVariableSlotKind.NumericString or AwkVariableSlotKind.PreparedNumericString => createNumericStringValue(ByteText),
            _ => createStringValue(ByteText)
        };
}

internal enum AwkVariableSlotKind
{
    Text,
    Unassigned,
    Untyped,
    Number,
    NumericString,
    PreparedNumericString,
    ArrayBinding
}
