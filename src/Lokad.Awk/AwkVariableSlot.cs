using System;

namespace Lokad.Awk;

internal readonly struct AwkVariableSlot
{
    private readonly object? _text;
    private readonly double _number;

    public AwkVariableSlot(string text, AwkVariableSlotKind kind)
        : this(AwkText.FromString(text), kind)
    {
    }

    public AwkVariableSlot(AwkText text, AwkVariableSlotKind kind)
    {
        _text = text;
        _number = kind == AwkVariableSlotKind.Number
            ? AwkValue.FromText(text).AsNumber()
            : 0;
        Kind = kind;
    }

    private AwkVariableSlot(double number)
    {
        _text = AwkText.Empty;
        _number = number;
        Kind = AwkVariableSlotKind.Number;
    }

    private AwkVariableSlot(AwkUntypedScalar scalar)
    {
        _text = scalar;
        _number = 0;
        Kind = AwkVariableSlotKind.Untyped;
    }

    private AwkVariableSlot(AwkText text, double number)
    {
        _text = text;
        _number = number;
        Kind = AwkVariableSlotKind.PreparedNumericString;
    }

    public static AwkVariableSlot FromUntypedScalar(AwkUntypedScalar scalar) => new(scalar);

    public static AwkVariableSlot FromArgumentValue(AwkValue value) =>
        value.TryGetUntypedScalar(out var scalar) ? FromUntypedScalar(scalar) : FromValue(value);

    public bool IsUntyped => Kind == AwkVariableSlotKind.Untyped &&
        _text is AwkUntypedScalar { IsScalar: false };

    public void PromoteUntypedScalar()
    {
        if (_text is AwkUntypedScalar scalar)
            scalar.Promote();
    }

    public static AwkVariableSlot FromText(string text) =>
        new(text, AwkVariableSlotKind.Text);

    public static AwkVariableSlot FromText(AwkText text) =>
        new(text, AwkVariableSlotKind.Text);

    public static AwkVariableSlot FromExternalText(string text) =>
        new(text, AwkVariableSlotKind.NumericString);

    public static AwkVariableSlot FromExternalText(AwkText text) =>
        new(text, AwkVariableSlotKind.NumericString);

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
        else if (value.IsDeferredTextValue)
            kind = AwkVariableSlotKind.DeferredText;
        else if (value.RetainsInputClassification)
            kind = AwkVariableSlotKind.NumericString;
        else
            kind = AwkVariableSlotKind.Text;

        return new AwkVariableSlot(value.ByteText, kind);
    }

    public AwkText ByteText =>
        Kind == AwkVariableSlotKind.Number
            ? AwkNumberFormatter.ToAwkText(_number)
            : _text as AwkText ?? AwkText.Empty;

    public long RetainedTextMemoryBytes =>
        Kind == AwkVariableSlotKind.Number
            ? 0
            : _text is AwkUntypedScalar
                ? AwkRuntimeMemory.EstimateUntypedScalarBytes()
                : (_text as AwkText ?? AwkText.Empty).EstimatedRetainedMemoryBytes;

    public AwkVariableSlotKind Kind { get; }

    public bool TryGetNumber(out double number)
    {
        number = _number;
        return Kind == AwkVariableSlotKind.Number;
    }

    // Selected only after the caller checks PreparedNumericString.
    public AwkValue GetPreparedNumericInput() =>
        AwkValue.FromPreparedNumericInput(ByteText, _number);

    public AwkValue ToValue(
        Func<AwkText, AwkValue> createByteTextValue,
        Func<AwkText, AwkValue> createNumericStringValue,
        Func<AwkText, AwkValue> createDeferredTextValue) =>
        Kind switch
        {
            AwkVariableSlotKind.Unassigned => AwkValue.Unassigned(),
            AwkVariableSlotKind.Untyped when _text is AwkUntypedScalar scalar => AwkValue.FromUntypedScalar(scalar),
            AwkVariableSlotKind.Number => AwkValue.FromNumber(_number),
            AwkVariableSlotKind.NumericString or AwkVariableSlotKind.PreparedNumericString => createNumericStringValue(ByteText),
            AwkVariableSlotKind.DeferredText => createDeferredTextValue(ByteText),
            _ => createByteTextValue(ByteText)
        };
}

internal enum AwkVariableSlotKind
{
    Text,
    Unassigned,
    Untyped,
    Number,
    NumericString,
    DeferredText,
    PreparedNumericString
}
