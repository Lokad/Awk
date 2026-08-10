using System;
namespace Lokad.Awk;

internal enum AwkNumberParsingMode
{
    Decimal,
    HexadecimalInput,
    NonDecimalData
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
    DeferredNonDecimalText
}

internal readonly struct AwkValue
{
    private readonly AwkText _text;
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
        var valueText = AwkText.FromString(text);
        if (TryParseAwkNumber(valueText, numberParsingMode, out var number, out _))
            return new AwkValue(valueText, number, AwkValueKind.NumericText);

        return new AwkValue(valueText, 0, AwkValueKind.Text);
    }

    public static AwkValue FromText(AwkText text) =>
        FromText(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromText(AwkText text, AwkNumberParsingMode numberParsingMode)
    {
        if (TryParseAwkNumber(text, numberParsingMode, out var number, out _))
            return new AwkValue(text, number, AwkValueKind.NumericText);

        return new AwkValue(text, 0, AwkValueKind.Text);
    }

    public static AwkValue FromNumericString(string text) =>
        FromNumericString(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromNumericString(string text, AwkNumberParsingMode numberParsingMode)
    {
        var valueText = AwkText.FromString(text);
        if (TryParseAwkNumber(valueText, numberParsingMode, out var number, out var consumedAll) && consumedAll)
            return new AwkValue(valueText, number, AwkValueKind.NumericString);

        return FromText(text, numberParsingMode);
    }

    public static AwkValue FromNumericString(AwkText text) =>
        FromNumericString(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromNumericString(AwkText text, AwkNumberParsingMode numberParsingMode)
    {
        if (TryParseAwkNumber(text, numberParsingMode, out var number, out var consumedAll) && consumedAll)
            return new AwkValue(text, number, AwkValueKind.NumericString);

        return FromText(text, numberParsingMode);
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
        bool requireFullText,
        out double number)
    {
        var valueText = AwkText.FromString(text);
        if (TryParseAwkNumber(valueText, numberParsingMode, out number, out var consumedAll) &&
            (!requireFullText || consumedAll))
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

    public int RetainedTextByteLength =>
        _kind == AwkValueKind.Number ? 0 : (_text ?? AwkText.Empty).ByteLength;

    public double AsNumber()
    {
        if (TryGetDeferredNumberParsingMode(out var numberParsingMode))
        {
            return TryParseAwkNumber(ByteText, numberParsingMode, out var number, out _)
                ? number
                : 0;
        }

        return HasArithmeticNumber ? _number : 0;
    }

    public bool TryGetNumber(out double number)
    {
        number = _number;
        return HasComparisonNumber;
    }

    public bool IsNumberValue => _kind == AwkValueKind.Number;

    public bool IsNumericStringValue => _kind == AwkValueKind.NumericString;

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
        if (_kind is AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned)
            return Math.Abs(_number) >= double.Epsilon;

        return ByteText.ByteLength != 0;
    }

    private bool HasArithmeticNumber =>
        _kind is AwkValueKind.NumericText or AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned;

    private bool HasComparisonNumber =>
        _kind is AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned;

    private bool TryGetDeferredNumberParsingMode(out AwkNumberParsingMode numberParsingMode)
    {
        switch (_kind)
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

    private static bool TryParseAwkNumber(
        AwkText text,
        AwkNumberParsingMode numberParsingMode,
        out double number,
        out bool consumedAll)
    {
        if (text.TryParseAwkNumber(numberParsingMode, out number, out consumedAll))
        {
            return true;
        }

        if (TryParseSignedInfinity(text, out number, out consumedAll))
            return true;

        number = 0;
        consumedAll = false;
        return false;
    }

    private static bool TryParseSignedInfinity(AwkText text, out double number, out bool consumedAll)
    {
        var span = text.AsSpan();
        var index = SkipAsciiWhitespace(span, 0);
        number = 0;
        consumedAll = false;

        if (index >= span.Length || span[index] is not ((byte)'+' or (byte)'-'))
            return false;

        var sign = span[index] == (byte)'-' ? -1 : 1;
        index++;
        if (index + 3 > span.Length ||
            FoldAscii(span[index]) != (byte)'i' ||
            FoldAscii(span[index + 1]) != (byte)'n' ||
            FoldAscii(span[index + 2]) != (byte)'f')
        {
            return false;
        }

        index += 3;
        consumedAll = SkipAsciiWhitespace(span, index) == span.Length;
        if (!consumedAll)
            return false;

        number = sign > 0 ? double.PositiveInfinity : double.NegativeInfinity;
        return true;
    }

    private static int SkipAsciiWhitespace(ReadOnlySpan<byte> span, int index)
    {
        while (index < span.Length && span[index] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r')
            index++;

        return index;
    }

    private static byte FoldAscii(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z'
            ? (byte)(value + 32)
            : value;
}
