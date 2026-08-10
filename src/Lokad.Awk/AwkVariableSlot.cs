using System;

namespace Lokad.Awk;

internal readonly struct AwkVariableSlot
{
    private readonly AwkText _text;
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
        if (value.IsNumberValue)
            return new AwkVariableSlot(value.AsNumber());

        AwkVariableSlotKind kind;
        if (value.IsUnassignedValue)
            kind = AwkVariableSlotKind.Unassigned;
        else if (value.IsDeferredTextValue)
            kind = AwkVariableSlotKind.DeferredText;
        else if (value.IsNumericStringValue)
            kind = AwkVariableSlotKind.NumericString;
        else
            kind = AwkVariableSlotKind.Text;

        return new AwkVariableSlot(value.ByteText, kind);
    }

    public AwkText ByteText =>
        Kind == AwkVariableSlotKind.Number
            ? AwkNumberFormatter.ToAwkText(_number)
            : _text ?? AwkText.Empty;

    public long RetainedTextMemoryBytes =>
        Kind == AwkVariableSlotKind.Number
            ? 0
            : (_text ?? AwkText.Empty).EstimatedRetainedMemoryBytes;

    public AwkVariableSlotKind Kind { get; }

    public AwkValue ToValue(
        Func<AwkText, AwkValue> createByteTextValue,
        Func<AwkText, AwkValue> createNumericStringValue,
        Func<AwkText, AwkValue> createDeferredTextValue) =>
        Kind switch
        {
            AwkVariableSlotKind.Unassigned => AwkValue.Unassigned(),
            AwkVariableSlotKind.Number => AwkValue.FromNumber(_number),
            AwkVariableSlotKind.NumericString => createNumericStringValue(ByteText),
            AwkVariableSlotKind.DeferredText => createDeferredTextValue(ByteText),
            _ => createByteTextValue(ByteText)
        };
}

internal enum AwkVariableSlotKind
{
    Text,
    Unassigned,
    Number,
    NumericString,
    DeferredText
}
