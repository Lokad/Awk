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
    NonDecimalConversionErrorInput,
    // Variable owners resolve external input in their reading mode before use.
    StoredInputText
}

internal readonly struct AwkValue
{
    // One reference holds text or an untouched scalar cell; numbers need neither.
    // This preserves the 24-byte layout and immutable argument snapshots.
    private readonly object? _text;
    private readonly double _number;
    private readonly AwkValueKind _kind;

    internal object? StoredReference => _text;
    internal double StoredNumber => _number;
    internal AwkValueKind StoredKind => _kind;

    internal static AwkValue FromStorage(object? reference, double number, AwkValueKind kind) =>
        new(reference, number, kind);

    // Globals and arrays retain this same payload. Ordinary text loses arithmetic
    // interpretation at storage, while input spelling is classified at its reader.
    internal static AwkValue ForStoredText(string text) => ForStoredText(AwkText.FromString(text));
    internal static AwkValue ForStoredText(AwkText text) => new(text, 0, AwkValueKind.Text);
    internal static AwkValue ForStoredInputText(string text) => ForStoredInputText(AwkText.FromString(text));
    internal static AwkValue ForStoredInputText(AwkText text) => new(text, 0, AwkValueKind.StoredInputText);

    internal static AwkValue ForVariableStorage(AwkValue value, AwkNumberParsingMode mode)
    {
        value = value.AsScalarCopy();
        if (value.IsNumberValue || value.IsUnassignedValue || value.TryGetPreparedNumericInput(out _)) return value;
        // The owner fixes ordinary text's reading mode now. Input spelling keeps
        // classification at its actual read; neither path projects the backing.
        return new(value._text, 0, value.RetainsInputClassification
            ? AwkValueKind.StoredInputText : DeferredTextKind(mode));
    }

    internal AwkValue ReadVariableStorage(AwkNumberParsingMode mode, AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        switch (_kind)
        {
            case AwkValueKind.NumericString:
                if (ByteText.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes) checkpoint?.Invoke();
                counters.RecordPreparedNumericSlotRead();
                return this;
            case AwkValueKind.StoredInputText:
                counters.RecordValueCreated();
                counters.RecordTextValueClassification();
                return FromNumericString(ByteText, mode, checkpoint);
            case AwkValueKind.Text:
                counters.RecordValueCreated();
                return new(_text, 0, DeferredTextKind(mode));
            default:
                return this;
        }
    }

    internal bool TryGetNumberValue(out double number)
    {
        number = _number;
        return IsNumberValue;
    }

    internal double ReadVariableNumber(AwkNumberParsingMode mode, AwkRuntimeCounters counters, Action? checkpoint)
    {
        if (_kind == AwkValueKind.StoredInputText)
        {
            // Arithmetic needs the prefix, not a string/strnum classification.
            // Capture the owning spelling before parsing can poll or materialize.
            var text = ByteText;
            var span = text.AsSpan(text.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ? checkpoint : null);
            AwkNumberParser.Parse(span, mode, out var parsed, checkpoint);
            parsed.ThrowIfError();
            return parsed.Number;
        }
        var captured = this;
        return captured.ReadVariableStorage(mode, counters, checkpoint).AsNumber(checkpoint);
    }

    internal bool HasSameTextStorage(AwkValue other) =>
        _kind is AwkValueKind.Text or AwkValueKind.DeferredDecimalText or
            AwkValueKind.DeferredHexadecimalText or AwkValueKind.DeferredNonDecimalText or AwkValueKind.Unassigned &&
        _kind == other._kind && ReferenceEquals(_text, other._text);

    // A frame fixes the reading mode once. Mode-independent input facts keep
    // their cached number; other input spellings are classified at this owner.
    internal AwkValue ForFunctionStorage(AwkNumberParsingMode mode, Action? checkpoint)
    {
        switch (_kind)
        {
            case AwkValueKind.Number:
            case AwkValueKind.Unassigned:
                return this;
            case AwkValueKind.ArrayReference:
            case AwkValueKind.Untyped:
                return AsScalarCopy();
            case AwkValueKind.NumericString:
            case AwkValueKind.NonDecimalConversionErrorInput:
                return TryGetPreparedNumericInput(out _) ? this : FromNumericString(ByteText, mode, checkpoint);
            default:
                // Other kinds already own scalar text. Keep its backing directly,
                // without projecting it through a general text/value conversion.
                return new(_text, 0, DeferredTextKind(mode));
        }
    }

    internal bool TryGetStoredScalarText(out AwkText text)
    {
        if (HasStoredScalarText(_kind))
        {
            // The kind proof already excludes numbers, arrays and untyped origins.
            text = AwkText.FromBacking(_text);
            return true;
        }
        text = default;
        return false;
    }

    internal static bool HasStoredScalarText(AwkValueKind kind) =>
        kind is not (AwkValueKind.Number or AwkValueKind.Untyped or AwkValueKind.ArrayReference);

    internal void PromoteUntypedScalar()
    {
        if (TryGetUntypedScalar(out var origin)) origin.Promote();
    }

    private AwkValue(
        object? text,
        double number,
        AwkValueKind kind)
    {
        _text = text;
        _number = number;
        _kind = kind;
    }

    private AwkValue(AwkText text, double number, AwkValueKind kind)
        : this(text.Backing, number, kind)
    {
    }

    private AwkValue(double number)
    {
        _text = null;
        _number = number;
        _kind = AwkValueKind.Number;
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
        var span = text.AsSpan(text.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ? checkpoint : null);
        return FromParsedText(text, span, mode, input, checkpoint);
    }

    // Classification only: the field frame keeps spelling and ownership separately.
    // This value must not escape as a text scalar.
    internal static AwkValue ClassifyInput(ReadOnlySpan<byte> bytes, AwkNumberParsingMode mode, Action? checkpoint) =>
        FromParsedText(AwkText.Empty, bytes, mode, input: true, checkpoint);

    private static AwkValue FromParsedText(AwkText text, ReadOnlySpan<byte> span,
        AwkNumberParsingMode mode, bool input, Action? checkpoint)
    {
        AwkNumberParser.Parse(span, mode, out var parsed, checkpoint);
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
        new(value);

    internal static AwkValue FromPreparedNumericInput(AwkText text, double number) =>
        new(text, number, AwkValueKind.NumericString);

    internal bool TryGetPreparedNumericInput(out double number)
    {
        number = _number;
        // Nonzero initial decimal digits have the same meaning in every input-number mode.
        if (_kind != AwkValueKind.NumericString)
            return false;
        var text = AwkText.FromBacking(_text);
        return text.ByteLength != 0 && text.AsSpan()[0] is >= (byte)'1' and <= (byte)'9';
    }

    public static AwkValue FromDeferredText(AwkText text, AwkNumberParsingMode numberParsingMode) =>
        new(text, 0, DeferredTextKind(numberParsingMode));

    private static AwkValueKind DeferredTextKind(AwkNumberParsingMode numberParsingMode) =>
        numberParsingMode switch
        {
            AwkNumberParsingMode.Decimal => AwkValueKind.DeferredDecimalText,
            AwkNumberParsingMode.HexadecimalInput => AwkValueKind.DeferredHexadecimalText,
            AwkNumberParsingMode.NonDecimalData => AwkValueKind.DeferredNonDecimalText,
            _ => throw new ArgumentOutOfRangeException(nameof(numberParsingMode))
        };

    public static AwkValue Unassigned() =>
        new(AwkText.Empty, 0, AwkValueKind.Unassigned);

    public static AwkValue FromUntypedScalar(AwkUntypedScalar scalar) =>
        new(scalar, 0, AwkValueKind.Untyped);

    // SYMTAB names refer to global arrays. The existing identifier text is enough
    // to retain this reference without allocating a wrapper or changing value size.
    public static AwkValue FromGlobalArrayReference(AwkIdentifier identifier) =>
        new(identifier.Text, 0, AwkValueKind.ArrayReference);

    public static AwkValue FromArrayReference(AwkArrayIdentity array) =>
        new(array, 0, AwkValueKind.ArrayReference);

    public bool IsArrayValue => _kind == AwkValueKind.ArrayReference ||
        _kind == AwkValueKind.Untyped && _text is AwkUntypedScalar { Array: not null };

    public bool TryGetArrayBinding([NotNullWhen(true)] out AwkArrayIdentity? array) =>
        TryGetArrayBinding(_text, _kind, out array);

    internal static bool TryGetArrayBinding(object? reference, AwkValueKind kind,
        [NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        array = kind == AwkValueKind.ArrayReference ? reference as AwkArrayIdentity :
            kind == AwkValueKind.Untyped ? (reference as AwkUntypedScalar)?.Array : null;
        return array is not null;
    }

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
        if (IsArrayValue)
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(
                _text is string name ? name : "array element"));
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
                : _kind == AwkValueKind.Untyped ? AwkText.Empty : AwkText.FromBacking(_text);
        }
    }

    // Text preparation changes storage, not the captured scalar's origin or number.
    internal AwkValue WithByteText(AwkText text) => new(text, _number, _kind);

    public long RetainedTextMemoryBytes =>
        _kind == AwkValueKind.Number
            ? 0
            : _text is AwkUntypedScalar
                ? AwkRuntimeMemory.EstimateUntypedScalarBytes()
                : _kind == AwkValueKind.ArrayReference ? 0 : AwkText.FromBacking(_text).EstimatedRetainedMemoryBytes;

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

    // Consume an existing fact without classifying text, promoting an origin or
    // forcing a deferred error. Failed probes leave ordinary evaluation in charge.
    internal bool TryGetStoredComparisonNumber(out double number, Action? checkpoint)
    {
        number = _number;
        if (!HasComparisonNumber || _kind == AwkValueKind.Untyped) return false;
        if (_kind == AwkValueKind.NumericString &&
            AwkText.FromBacking(_text).ByteLength >= AwkRuntimeLimits.CancellationCheckBytes)
            checkpoint?.Invoke();
        return true;
    }

    internal bool HasInputConversionError => _kind == AwkValueKind.NonDecimalConversionErrorInput;

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
        HasInputClassification(_kind);

    internal static bool HasInputClassification(AwkValueKind kind) =>
        kind is AwkValueKind.NumericString or AwkValueKind.NonDecimalConversionErrorInput;

    public bool IsUnassignedValue => _kind == AwkValueKind.Unassigned ||
        _kind == AwkValueKind.Untyped && _text is AwkUntypedScalar { IsScalar: true };

    public bool IsUntypedValue => _kind == AwkValueKind.Untyped &&
        _text is AwkUntypedScalar { IsScalar: false, Array: null };

    public bool TryGetArrayOrdinal(out int ordinal)
    {
        if (_kind == AwkValueKind.Number) return TryGetArrayOrdinal(_number, out ordinal);
        ordinal = 0;
        return false;
    }

    internal static bool TryGetArrayOrdinal(double number, out int ordinal)
    {
        if (number >= int.MinValue && number <= int.MaxValue && double.IsInteger(number))
        {
            ordinal = (int)number;
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
                AwkValueKind.Untyped => IsArrayValue ? AwkRuntimeValueKind.Array :
                    IsUntypedValue ? AwkRuntimeValueKind.Untyped : AwkRuntimeValueKind.Unassigned,
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
