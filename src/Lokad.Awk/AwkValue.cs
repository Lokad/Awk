using System;
namespace Lokad.Awk;

internal enum AwkNumberParsingMode
{
    Decimal,
    HexadecimalInput,
    NonDecimalData
}

internal enum AwkNumberTextExtent
{
    Prefix,
    EntireText
}

internal enum AwkValueKind
{
    Text,
    NumericText,
    Number,
    NumericString,
    Unassigned,
    DeferredDecimalText,
    DeferredHexadecimalText,
    DeferredNonDecimalText,
    NonDecimalConversionErrorText,
    NonDecimalConversionErrorInput
}

internal readonly struct AwkValue
{
    private readonly AwkText? _text;
    private readonly double _number;
    private readonly AwkValueKind _kind;

    private AwkValue(
        AwkText text,
        double number,
        AwkValueKind kind)
    {
        _text = text;
        _number = number;
        _kind = kind;
    }

    public static AwkValue FromText(string text) =>
        FromText(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromText(string text, AwkNumberParsingMode numberParsingMode)
    {
        return FromText(AwkText.FromString(text), numberParsingMode);
    }

    public static AwkValue FromText(AwkText text) =>
        FromText(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromText(AwkText text, AwkNumberParsingMode numberParsingMode)
    {
        return FromParsedText(text, numberParsingMode, input: false);
    }

    public static AwkValue FromNumericString(string text) =>
        FromNumericString(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromNumericString(string text, AwkNumberParsingMode numberParsingMode)
    {
        return FromNumericString(AwkText.FromString(text), numberParsingMode);
    }

    public static AwkValue FromNumericString(AwkText text) =>
        FromNumericString(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromNumericString(AwkText text, AwkNumberParsingMode numberParsingMode)
    {
        return FromParsedText(text, numberParsingMode, input: true);
    }

    private static AwkValue FromParsedText(AwkText text, AwkNumberParsingMode mode, bool input)
    {
        var span = text.AsSpan();
        var parsed = AwkNumberParser.Parse(span, mode);
        // Text may still be printed or sliced. GNU diagnoses this only when numeric conversion/type is requested.
        if (parsed.Status == AwkNumberParseStatus.MissingHexExponent)
            return new(text, 0, input ? AwkValueKind.NonDecimalConversionErrorInput : AwkValueKind.NonDecimalConversionErrorText);
        if (parsed.Status == AwkNumberParseStatus.Number)
        {
            var kind = input && AwkNumberParser.SkipWhitespace(span, parsed.End) == span.Length
                ? AwkValueKind.NumericString
                : AwkValueKind.NumericText;
            return new(text, parsed.Number, kind);
        }
        return new(text, 0, AwkValueKind.Text);
    }

    public static AwkValue FromNumber(double value) =>
        new(AwkText.Empty, value, AwkValueKind.Number);

    public static AwkValue FromDeferredText(AwkText text, AwkNumberParsingMode numberParsingMode) =>
        new(
            text,
            0,
            numberParsingMode switch
            {
                AwkNumberParsingMode.Decimal => AwkValueKind.DeferredDecimalText,
                AwkNumberParsingMode.HexadecimalInput => AwkValueKind.DeferredHexadecimalText,
                AwkNumberParsingMode.NonDecimalData => AwkValueKind.DeferredNonDecimalText,
                _ => throw new ArgumentOutOfRangeException(nameof(numberParsingMode))
            });

    public static AwkValue Unassigned() =>
        new(AwkText.Empty, 0, AwkValueKind.Unassigned);

    public static bool TryParseNumber(
        string text,
        AwkNumberParsingMode numberParsingMode,
        AwkNumberTextExtent textExtent,
        out double number)
    {
        var valueText = AwkText.FromString(text);
        if (TryParseAwkNumber(valueText, numberParsingMode, out number, out var consumedAll) &&
            (textExtent is AwkNumberTextExtent.Prefix || consumedAll))
        {
            return true;
        }

        number = 0;
        return false;
    }

    public string AsString() =>
        _kind == AwkValueKind.Number ? AwkNumberFormatter.ToString(_number) : ByteText.AsString();

    public string AsString(AwkRuntimeCounters counters) =>
        _kind == AwkValueKind.Number ? AwkNumberFormatter.ToString(_number) : ByteText.AsString(counters);

    public AwkText ByteText =>
        _kind == AwkValueKind.Number
            ? AwkNumberFormatter.ToAwkText(_number)
            : _text ?? AwkText.Empty;

    public long RetainedTextMemoryBytes =>
        _kind == AwkValueKind.Number
            ? 0
            : (_text ?? AwkText.Empty).EstimatedRetainedMemoryBytes;

    public double AsNumber()
    {
        if (_kind is AwkValueKind.NonDecimalConversionErrorText or AwkValueKind.NonDecimalConversionErrorInput)
            throw new AwkRuntimeException(AwkMessages.ErrorHexadecimalFloatExponent);
        var kind = _kind;
        if (TryGetDeferredNumberParsingMode(out var numberParsingMode))
        {
            return TryParseAwkNumber(ByteText, numberParsingMode, out var number, out _)
                ? number
                : 0;
        }

        return HasArithmeticNumber ? _number : 0;

        bool TryGetDeferredNumberParsingMode(out AwkNumberParsingMode numberParsingMode)
        {
            switch (kind)
            {
                case AwkValueKind.DeferredDecimalText:
                    numberParsingMode = AwkNumberParsingMode.Decimal;
                    return true;
                case AwkValueKind.DeferredHexadecimalText:
                    numberParsingMode = AwkNumberParsingMode.HexadecimalInput;
                    return true;
                case AwkValueKind.DeferredNonDecimalText:
                    numberParsingMode = AwkNumberParsingMode.NonDecimalData;
                    return true;
                default:
                    numberParsingMode = default;
                    return false;
            }
        }
    }

    public bool TryGetNumber(out double number)
    {
        ThrowIfInputConversionError();
        number = _number;
        return HasComparisonNumber;
    }

    public bool IsNumberValue => _kind == AwkValueKind.Number;

    public bool IsNumericStringValue
    {
        get
        {
            ThrowIfInputConversionError();
            return _kind == AwkValueKind.NumericString;
        }
    }

    internal bool RetainsInputClassification =>
        _kind is AwkValueKind.NumericString or AwkValueKind.NonDecimalConversionErrorInput;

    public bool IsUnassignedValue => _kind == AwkValueKind.Unassigned;

    public bool IsDeferredTextValue => _kind is
        AwkValueKind.DeferredDecimalText or
        AwkValueKind.DeferredHexadecimalText or
        AwkValueKind.DeferredNonDecimalText;

    public bool TryGetArrayOrdinal(out int ordinal)
    {
        if (_kind == AwkValueKind.Number &&
            _number >= int.MinValue &&
            _number <= int.MaxValue &&
            AwkNumberFormatter.IsInteger(_number))
        {
            ordinal = (int)_number;
            return true;
        }

        ordinal = 0;
        return false;
    }

    public AwkRuntimeValueKind RuntimeKind
    {
        get
        {
            ThrowIfInputConversionError();
            return _kind switch
            {
                AwkValueKind.Unassigned => AwkRuntimeValueKind.Unassigned,
                AwkValueKind.Number => AwkRuntimeValueKind.Number,
                AwkValueKind.NumericString => AwkRuntimeValueKind.NumericString,
                _ => AwkRuntimeValueKind.String
            };
        }
    }

    public static int Compare(AwkValue left, AwkValue right)
    {
        return Compare(left, right, AwkRuntimeCounters.Disabled);
    }

    public static int Compare(AwkValue left, AwkValue right, AwkRuntimeCounters counters)
    {
        if (left.TryGetNumber(out var leftNumber) && right.TryGetNumber(out var rightNumber))
            return leftNumber.CompareTo(rightNumber);

        counters.RecordByteTextComparison();
        return left.ByteText.CompareBytes(right.ByteText);
    }

    public bool IsTruthy()
    {
        ThrowIfInputConversionError();
        if (_kind is AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned)
            return _number != 0;

        return ByteText.ByteLength != 0;
    }

    private bool HasArithmeticNumber =>
        _kind is AwkValueKind.NumericText or AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned;

    private bool HasComparisonNumber =>
        _kind is AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned;

    private void ThrowIfInputConversionError()
    {
        if (_kind == AwkValueKind.NonDecimalConversionErrorInput)
            throw new AwkRuntimeException(AwkMessages.ErrorHexadecimalFloatExponent);
    }

    private static bool TryParseAwkNumber(
        AwkText text,
        AwkNumberParsingMode numberParsingMode,
        out double number,
        out bool consumedAll) =>
        text.TryParseAwkNumber(numberParsingMode, out number, out consumedAll);
}
