using System;
using System.Diagnostics.CodeAnalysis;
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
    Untyped,
    ArrayReference,
    DeferredDecimalText,
    DeferredHexadecimalText,
    DeferredNonDecimalText,
    NonDecimalConversionErrorText,
    NonDecimalConversionErrorInput
}

internal readonly struct AwkValue
{
    // One reference holds either ordinary text or an untouched scalar cell. This
    // preserves the value's 24-byte layout while retaining argument origins.
    private readonly object? _text;
    private readonly double _number;
    private readonly AwkValueKind _kind;

    private AwkValue(
        object text,
        double number,
        AwkValueKind kind)
    {
        _text = text;
        _number = number;
        _kind = kind;
    }

    public static AwkValue FromText(string text) =>
        FromText(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromText(string text, AwkNumberParsingMode numberParsingMode, Action? checkpoint = null)
    {
        return FromText(AwkText.FromString(text, AwkRuntimeCounters.Disabled, checkpoint), numberParsingMode, checkpoint);
    }

    public static AwkValue FromText(AwkText text) =>
        FromText(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromText(AwkText text, AwkNumberParsingMode numberParsingMode, Action? checkpoint = null)
    {
        return FromParsedText(text, numberParsingMode, input: false, checkpoint);
    }

    public static AwkValue FromNumericString(string text) =>
        FromNumericString(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromNumericString(string text, AwkNumberParsingMode numberParsingMode)
    {
        return FromNumericString(AwkText.FromString(text), numberParsingMode);
    }

    public static AwkValue FromNumericString(AwkText text) =>
        FromNumericString(text, AwkNumberParsingMode.Decimal);

    public static AwkValue FromNumericString(AwkText text, AwkNumberParsingMode numberParsingMode, Action? checkpoint = null)
    {
        return FromParsedText(text, numberParsingMode, input: true, checkpoint);
    }

    private static AwkValue FromParsedText(AwkText text, AwkNumberParsingMode mode, bool input, Action? checkpoint)
    {
        // Preserve bounded initialization and post-cancellation inspection reads.
        // Long text must check before lazy materialization, then within parsing.
        var span = text.AsSpan(text.ByteLength >= AwkExecutionGovernor.WorkChunkBytes ? checkpoint : null);
        var parsed = AwkNumberParser.Parse(span, mode, checkpoint);
        // Text may still be printed or sliced. GNU diagnoses this only when numeric conversion/type is requested.
        if (parsed.Status == AwkNumberParseStatus.MissingHexExponent)
            return new(text, 0, input ? AwkValueKind.NonDecimalConversionErrorInput : AwkValueKind.NonDecimalConversionErrorText);
        if (parsed.Status == AwkNumberParseStatus.Number)
        {
            var kind = input && AwkNumberParser.SkipWhitespace(span, parsed.End, checkpoint) == span.Length
                ? AwkValueKind.NumericString
                : AwkValueKind.NumericText;
            return new(text, parsed.Number, kind);
        }
        return new(text, 0, AwkValueKind.Text);
    }

    public static AwkValue FromNumber(double value) =>
        new(AwkText.Empty, value, AwkValueKind.Number);

    internal static AwkValue FromPreparedNumericInput(AwkText text, double number) =>
        new(text, number, AwkValueKind.NumericString);

    internal bool TryGetPreparedNumericInput(out double number)
    {
        number = _number;
        // Nonzero initial decimal digits have the same meaning in every input-number mode.
        return _kind == AwkValueKind.NumericString && _text is AwkText text &&
            text.ByteLength != 0 && text.AsSpan()[0] is >= (byte)'1' and <= (byte)'9';
    }

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

    public static AwkValue FromUntypedScalar(AwkUntypedScalar scalar) =>
        new(scalar, 0, AwkValueKind.Untyped);

    // SYMTAB names refer to global arrays. The existing identifier text is enough
    // to retain this reference without allocating a wrapper or changing value size.
    public static AwkValue FromGlobalArrayReference(AwkIdentifier identifier) =>
        new(identifier.Text, 0, AwkValueKind.ArrayReference);

    public bool IsArrayValue => _kind == AwkValueKind.ArrayReference;

    public bool TryGetArrayName(out AwkArrayName name)
    {
        if (_kind == AwkValueKind.ArrayReference && _text is string text &&
            AwkIdentifier.TryParse(text, out var identifier))
        {
            name = new AwkArrayName(identifier);
            return true;
        }
        name = default;
        return false;
    }

    private void ThrowIfArrayValue()
    {
        if (_kind == AwkValueKind.ArrayReference && _text is string name)
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(name));
    }

    public bool TryGetUntypedScalar([NotNullWhen(true)] out AwkUntypedScalar? scalar)
    {
        scalar = _kind == AwkValueKind.Untyped ? _text as AwkUntypedScalar : null;
        return scalar is not null;
    }

    public AwkValue AsScalarCopy()
    {
        ThrowIfArrayValue();
        return _kind == AwkValueKind.Untyped ? Unassigned() : this;
    }

    public AwkValue ForceScalar()
    {
        if (TryGetUntypedScalar(out var scalar))
            scalar.Promote();
        return AsScalarCopy();
    }

    public static bool TryParseNumber(
        string text,
        AwkNumberParsingMode numberParsingMode,
        AwkNumberTextExtent textExtent,
        out double number,
        Action? checkpoint = null)
    {
        var valueText = AwkText.FromString(text);
        if (TryParseAwkNumber(valueText, numberParsingMode, out number, out var consumedAll, checkpoint) &&
            (textExtent is AwkNumberTextExtent.Prefix || consumedAll))
        {
            return true;
        }

        number = 0;
        return false;
    }

    public string AsString() =>
        _kind == AwkValueKind.Number ? AwkNumberFormatter.ToString(_number) : ByteText.AsString();

    public string AsString(AwkRuntimeCounters counters, Action? checkpoint = null) =>
        _kind == AwkValueKind.Number ? AwkNumberFormatter.ToString(_number) : ByteText.AsString(counters, checkpoint);

    public AwkText ByteText
    {
        get
        {
            ThrowIfArrayValue();
            return _kind == AwkValueKind.Number
                ? AwkNumberFormatter.ToAwkText(_number)
                : _text as AwkText ?? AwkText.Empty;
        }
    }

    public long RetainedTextMemoryBytes =>
        _kind == AwkValueKind.Number
            ? 0
            : _text is AwkUntypedScalar
                ? AwkRuntimeMemory.EstimateUntypedScalarBytes()
                : (_text as AwkText ?? AwkText.Empty).EstimatedRetainedMemoryBytes;

    public double AsNumber(Action? checkpoint = null)
    {
        ThrowIfArrayValue();
        if (_kind is AwkValueKind.NonDecimalConversionErrorText or AwkValueKind.NonDecimalConversionErrorInput)
            throw new AwkRuntimeException(AwkMessages.ErrorHexadecimalFloatExponent);
        var kind = _kind;
        if (TryGetDeferredNumberParsingMode(out var numberParsingMode))
        {
            return TryParseAwkNumber(ByteText, numberParsingMode, out var number, out _, checkpoint)
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
        ThrowIfArrayValue();
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

    public bool IsUnassignedValue => _kind == AwkValueKind.Unassigned ||
        _kind == AwkValueKind.Untyped && _text is AwkUntypedScalar { IsScalar: true };

    public bool IsUntypedValue => _kind == AwkValueKind.Untyped &&
        _text is AwkUntypedScalar { IsScalar: false };

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
                AwkValueKind.Untyped => IsUntypedValue ? AwkRuntimeValueKind.Untyped : AwkRuntimeValueKind.Unassigned,
                AwkValueKind.ArrayReference => AwkRuntimeValueKind.Array,
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
        ThrowIfArrayValue();
        ThrowIfInputConversionError();
        if (_kind is AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned or AwkValueKind.Untyped)
            return _number != 0;

        return ByteText.ByteLength != 0;
    }

    private bool HasArithmeticNumber =>
        _kind is AwkValueKind.NumericText or AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned or AwkValueKind.Untyped;

    private bool HasComparisonNumber =>
        _kind is AwkValueKind.Number or AwkValueKind.NumericString or AwkValueKind.Unassigned or AwkValueKind.Untyped;

    private void ThrowIfInputConversionError()
    {
        if (_kind == AwkValueKind.NonDecimalConversionErrorInput)
            throw new AwkRuntimeException(AwkMessages.ErrorHexadecimalFloatExponent);
    }

    private static bool TryParseAwkNumber(
        AwkText text,
        AwkNumberParsingMode numberParsingMode,
        out double number,
        out bool consumedAll,
        Action? checkpoint = null) =>
        text.TryParseAwkNumber(numberParsingMode, out number, out consumedAll, checkpoint);
}
