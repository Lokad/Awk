using System;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal static class PrintfEvaluation
{
    public static AwkText FormatByteText<TArguments>(
        string format,
        TArguments arguments,
        bool strictMissingArguments,
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
        where TArguments : IPrintfArguments
    {
        if (format.Length == 0)
            return AwkText.Empty;

        using var output = new PrintfByteOutput(limits, counters, governor);
        var argIndex = 0;

        for (var index = 0; index < format.Length; index++)
        {
            var current = format[index];
            if (current == '\\')
            {
                if (index + 1 >= format.Length)
                {
                    output.AppendAscii('\\');
                    continue;
                }

                var escaped = format[++index];
                var escapedByte = escaped switch
                {
                    'n' => (byte)'\n',
                    'r' => (byte)'\r',
                    't' => (byte)'\t',
                    'b' => (byte)'\b',
                    'f' => (byte)'\f',
                    'v' => (byte)'\v',
                    '\\' => (byte)'\\',
                    _ => byte.MaxValue
                };
                if (escapedByte != byte.MaxValue)
                    output.AppendAscii(escapedByte);
                else
                    AppendFormatCharacter(output, format, ref index);

                continue;
            }

            if (current != '%')
            {
                AppendFormatCharacter(output, format, ref index);
                continue;
            }

            if (index + 1 >= format.Length)
            {
                output.AppendAscii('%');
                continue;
            }

            if (!TryReadDirective(format, ref index, out var directive))
            {
                output.AppendAscii('%');
                continue;
            }

            var flags = directive.Flags;
            var width = ResolveWidth(
                directive.Width,
                arguments,
                strictMissingArguments,
                ref argIndex,
                ref flags);
            var precision = ResolvePrecision(
                directive.Precision,
                arguments,
                strictMissingArguments,
                ref argIndex);

            if (directive.Conversion == PrintfConversion.Percent)
            {
                output.AppendAscii('%');
                continue;
            }

            if (directive.Conversion == PrintfConversion.String)
            {
                if (TryReadArgumentIndex(arguments, strictMissingArguments, ref argIndex, out var argumentIndex))
                {
                    output.PreflightAdditionalBytes(width.Value);
                    var value = arguments.GetString(argumentIndex);
                    if (precision.HasValue)
                        value = TruncateToRuneCount(value, precision.Value);

                    AppendWithWidth(output, value, width.Value, flags.LeftJustify, zeroPad: false);
                }

                continue;
            }

            if (directive.Conversion.IsInteger())
            {
                if (TryReadArgumentIndex(arguments, strictMissingArguments, ref argIndex, out var argumentIndex))
                {
                    var formatted = FormatInteger(arguments.GetNumber(argumentIndex), directive.Conversion, flags);
                    output.PreflightAdditionalBytes(Math.Max(
                        width.Value,
                        AwkUtf8Codec.GetEncodedByteCount(formatted)));
                    AppendWithWidth(output, formatted, width.Value, flags.LeftJustify, flags.ZeroPad);
                }

                continue;
            }

            if (directive.Conversion.IsFloat())
            {
                if (TryReadArgumentIndex(arguments, strictMissingArguments, ref argIndex, out var argumentIndex))
                {
                    var number = arguments.GetNumber(argumentIndex);
                    output.PreflightAdditionalBytes(Math.Max(
                        width.Value,
                        EstimateFloatByteCount(number, directive.Conversion, precision)));
                    var formatted = FormatFloat(number, directive.Conversion, precision, flags);
                    AppendWithWidth(output, formatted, width.Value, flags.LeftJustify, flags.ZeroPad);
                }

                continue;
            }

            if (directive.Conversion == PrintfConversion.Character)
            {
                if (TryReadArgumentIndex(arguments, strictMissingArguments, ref argIndex, out var argumentIndex))
                {
                    var formatted = FormatCharacter(arguments, argumentIndex);
                    output.PreflightAdditionalBytes(Math.Max(
                        width.Value,
                        AwkUtf8Codec.GetEncodedByteCount(formatted)));
                    AppendWithWidth(
                        output,
                        formatted,
                        width.Value,
                        flags.LeftJustify,
                        zeroPad: false);
                }

                continue;
            }

            output.AppendAscii('%');
            AppendFormatCharacter(output, format, ref index);
        }

        return output.ToAwkText();
    }

    private static bool TryReadDirective(string format, ref int index, out PrintfDirective directive)
    {
        var cursor = index + 1;
        if (cursor >= format.Length)
        {
            directive = default;
            return false;
        }

        var flags = PrintfFlags.None;
        while (cursor < format.Length)
        {
            var flag = format[cursor];
            if (flag == '-')
                flags = flags.WithLeftJustify();
            else if (flag == '+')
                flags = flags.WithForceSign();
            else if (flag == ' ')
                flags = flags.WithLeadingSpace();
            else if (flag == '0')
                flags = flags.WithZeroPad();
            else if (flag == '#')
                flags = flags.WithAlternateForm();
            else
                break;

            cursor++;
        }

        var width = PrintfWidth.None;
        if (cursor < format.Length && format[cursor] == '*')
        {
            width = PrintfWidth.Dynamic;
            cursor++;
        }
        else
        {
            var literalWidth = 0;
            while (cursor < format.Length && char.IsDigit(format[cursor]))
            {
                literalWidth = AppendDecimalDigit(literalWidth, format[cursor]);
                cursor++;
            }

            if (literalWidth > 0)
                width = PrintfWidth.Literal(literalWidth);
        }

        var precision = PrintfPrecision.None;
        if (cursor < format.Length && format[cursor] == '.')
        {
            cursor++;
            if (cursor < format.Length && format[cursor] == '*')
            {
                precision = PrintfPrecision.Dynamic;
                cursor++;
            }
            else
            {
                var literalPrecision = 0;
                while (cursor < format.Length && char.IsDigit(format[cursor]))
                {
                    literalPrecision = AppendDecimalDigit(literalPrecision, format[cursor]);
                    cursor++;
                }

                precision = PrintfPrecision.Literal(literalPrecision);
            }
        }

        if (cursor >= format.Length)
        {
            directive = default;
            return false;
        }

        var specifier = format[cursor];
        directive = new PrintfDirective(
            flags,
            width,
            precision,
            PrintfConversionFacts.Parse(specifier),
            specifier);
        index = cursor;
        return true;

        static int AppendDecimalDigit(int value, char digit)
        {
            if (value > (int.MaxValue - (digit - '0')) / 10)
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

            return (value * 10) + (digit - '0');
        }
    }

    private static PrintfWidthValue ResolveWidth(
        PrintfWidth width,
        IPrintfArguments arguments,
        bool strictMissingArguments,
        ref int argIndex,
        ref PrintfFlags flags)
    {
        if (width.Kind == PrintfWidthKind.Dynamic)
        {
            var dynamicWidth = 0;
            if (argIndex >= arguments.Count)
            {
                if (strictMissingArguments)
                    throw new AwkRuntimeException(AwkMessages.ErrorNotEnoughPrintfArguments);
            }
            else
            {
                var value = arguments.GetNumber(argIndex++);
                if (double.IsNaN(value))
                    value = 0;

                if (value is > int.MaxValue or <= int.MinValue)
                    throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

                dynamicWidth = (int)value;
            }

            if (dynamicWidth < 0)
            {
                flags = flags.WithLeftJustify();
                dynamicWidth = -dynamicWidth;
            }

            return new PrintfWidthValue(dynamicWidth);
        }

        return new PrintfWidthValue(width.Value);
    }

    private static PrintfPrecisionValue ResolvePrecision(
        PrintfPrecision precision,
        IPrintfArguments arguments,
        bool strictMissingArguments,
        ref int argIndex)
    {
        if (precision.Kind == PrintfPrecisionKind.Dynamic)
        {
            var dynamicPrecision = 0;
            if (argIndex >= arguments.Count)
            {
                if (strictMissingArguments)
                    throw new AwkRuntimeException(AwkMessages.ErrorNotEnoughPrintfArguments);
            }
            else
            {
                var value = arguments.GetNumber(argIndex++);
                if (double.IsNaN(value) || value < 0)
                    return PrintfPrecisionValue.None;

                if (value > int.MaxValue)
                    throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

                dynamicPrecision = (int)value;
            }

            return dynamicPrecision < 0
                ? PrintfPrecisionValue.None
                : PrintfPrecisionValue.Specified(dynamicPrecision);
        }

        return precision.Kind == PrintfPrecisionKind.Literal
            ? PrintfPrecisionValue.Specified(precision.Value)
            : PrintfPrecisionValue.None;
    }

    private static bool TryReadArgumentIndex(
        IPrintfArguments arguments,
        bool strictMissingArguments,
        ref int argIndex,
        out int argumentIndex)
    {
        if (argIndex >= arguments.Count)
        {
            if (strictMissingArguments)
                throw new AwkRuntimeException(AwkMessages.ErrorNotEnoughPrintfArguments);

            argumentIndex = 0;
            return false;
        }

        argumentIndex = argIndex++;
        return true;
    }

    private static string FormatInteger(double number, PrintfConversion conversion, PrintfFlags flags)
    {
        if (TryFormatInfinity(number, conversion, out var infinity))
        {
            return infinity;
        }

        var value = (long)number;
        var formatted = conversion switch
        {
            PrintfConversion.UnsignedDecimal => unchecked((ulong)value).ToString(CultureInfo.InvariantCulture),
            PrintfConversion.Octal => Convert.ToString(value, 8),
            PrintfConversion.HexLower => value.ToString("x", CultureInfo.InvariantCulture),
            PrintfConversion.HexUpper => value.ToString("X", CultureInfo.InvariantCulture),
            _ => value.ToString(CultureInfo.InvariantCulture)
        };
        formatted = ApplyAlternateIntegerForm(formatted, conversion, value, flags.AlternateForm);
        return ApplyNumericSign(
            formatted,
            value < 0,
            flags.ForceSign,
            flags.LeadingSpace,
            conversion is not (
                PrintfConversion.UnsignedDecimal or
                PrintfConversion.Octal or
                PrintfConversion.HexLower or
                PrintfConversion.HexUpper));
    }

    private static string FormatFloat(
        double value,
        PrintfConversion conversion,
        PrintfPrecisionValue precision,
        PrintfFlags flags)
    {
        if (TryFormatInfinity(value, conversion, out var infinity))
        {
            return infinity;
        }

        var digits = precision.HasValue ? precision.Value : 6;
        var formatSpecifier = conversion switch
        {
            PrintfConversion.FixedLower or PrintfConversion.FixedUpper => "F" + digits.ToString(CultureInfo.InvariantCulture),
            PrintfConversion.ScientificLower => "e" + digits.ToString(CultureInfo.InvariantCulture),
            PrintfConversion.ScientificUpper => "E" + digits.ToString(CultureInfo.InvariantCulture),
            PrintfConversion.GeneralLower or PrintfConversion.GeneralUpper => "G" + digits.ToString(CultureInfo.InvariantCulture),
            _ => "G" + digits.ToString(CultureInfo.InvariantCulture)
        };
        var formatted = value.ToString(formatSpecifier, CultureInfo.InvariantCulture);
        formatted = NormalizeFloatExponent(formatted, conversion);
        formatted = ApplyAlternateFloatForm(formatted, flags.AlternateForm, conversion, digits);
        return ApplyNumericSign(formatted, value < 0, flags.ForceSign, flags.LeadingSpace, signedConversion: true);
    }

    private static void AppendWithWidth(PrintfByteOutput output, string text, int width, bool leftJustify, bool zeroPad)
    {
        var textWidth = CountRunes(text);
        if (width <= 0 || textWidth >= width)
        {
            output.Append(text);
            return;
        }

        if (leftJustify)
        {
            output.Append(text);
            output.AppendAscii(' ', width - textWidth);
            return;
        }

        if (zeroPad && (text.StartsWith('-') ||
            text.StartsWith('+') ||
            text.StartsWith(' ')))
        {
            output.AppendAscii(text[0]);
            output.AppendAscii('0', width - textWidth);
            output.Append(text.AsSpan(1));
            return;
        }

        output.AppendAscii(zeroPad ? '0' : ' ', width - textWidth);
        output.Append(text);
    }

    private static void AppendFormatCharacter(
        PrintfByteOutput output,
        string format,
        ref int index)
    {
        var charCount = char.IsHighSurrogate(format[index]) &&
            index + 1 < format.Length &&
            char.IsLowSurrogate(format[index + 1])
                ? 2
                : 1;
        output.Append(format.AsSpan(index, charCount));
        index += charCount - 1;
    }

    private static int EstimateFloatByteCount(
        double value,
        PrintfConversion conversion,
        PrintfPrecisionValue precision)
    {
        if (!double.IsFinite(value))
            return 4;

        var digits = precision.HasValue ? precision.Value : 6;
        var overhead = 16;
        if (conversion is PrintfConversion.FixedLower or PrintfConversion.FixedUpper)
        {
            var magnitude = Math.Abs(value);
            var integralDigits = magnitude < 1
                ? 1
                : Math.Min(309, (int)Math.Floor(Math.Log10(magnitude)) + 1);
            overhead = AwkRuntimeMemory.CheckedAdd(integralDigits, 3);
        }

        return AwkRuntimeMemory.CheckedAdd(digits, overhead);
    }

    private static string TruncateToRuneCount(string text, int maximumRuneCount)
    {
        if (maximumRuneCount <= 0 || text.Length == 0)
            return string.Empty;

        var runeCount = 0;
        var utf16Length = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (runeCount == maximumRuneCount)
                break;

            runeCount++;
            utf16Length += rune.Utf16SequenceLength;
        }

        return utf16Length == text.Length ? text : text[..utf16Length];
    }

    private static int CountRunes(string text)
    {
        var count = 0;
        foreach (var _ in text.EnumerateRunes())
            count++;

        return count;
    }

    private static bool TryFormatInfinity(double value, PrintfConversion conversion, out string formatted)
    {
        if (!double.IsInfinity(value))
        {
            formatted = string.Empty;
            return false;
        }

        formatted = value < 0 ? "-inf" : "+inf";
        if (conversion.IsUppercase())
        {
            formatted = formatted.ToUpperInvariant();
        }

        return true;
    }

    private static string ApplyAlternateIntegerForm(string formatted, PrintfConversion conversion, long value, bool alternateForm)
    {
        if (!alternateForm || value == 0)
        {
            return formatted;
        }

        return conversion switch
        {
            PrintfConversion.Octal => formatted.StartsWith('0') ? formatted : "0" + formatted,
            PrintfConversion.HexLower => "0x" + formatted,
            PrintfConversion.HexUpper => "0X" + formatted,
            _ => formatted
        };
    }

    private static string ApplyAlternateFloatForm(string formatted, bool alternateForm, PrintfConversion conversion, int precision)
    {
        if (!alternateForm)
        {
            return formatted;
        }

        var exponentIndex = formatted.IndexOfAny(['e', 'E']);
        var mantissa = exponentIndex < 0 ? formatted : formatted[..exponentIndex];
        var exponent = exponentIndex < 0 ? string.Empty : formatted[exponentIndex..];
        if (!mantissa.Contains('.', StringComparison.Ordinal))
        {
            mantissa += ".";
        }

        if (conversion is PrintfConversion.GeneralLower or PrintfConversion.GeneralUpper)
        {
            var significantPrecision = precision == 0 ? 1 : precision;
            var significantDigits = CountSignificantDigits(mantissa);
            if (significantDigits < significantPrecision)
            {
                mantissa += new string('0', significantPrecision - significantDigits);
            }
        }

        return mantissa + exponent;
    }

    private static int CountSignificantDigits(string mantissa)
    {
        var count = 0;
        var sawNonZero = false;
        var hasDigit = false;
        for (var index = 0; index < mantissa.Length; index++)
        {
            var current = mantissa[index];
            if (!char.IsDigit(current))
            {
                continue;
            }

            hasDigit = true;
            if (current != '0' || sawNonZero)
            {
                sawNonZero = true;
                count++;
            }
        }

        return sawNonZero || !hasDigit ? count : 1;
    }

    private static string NormalizeFloatExponent(string formatted, PrintfConversion conversion)
    {
        var exponentIndex = formatted.IndexOfAny(['e', 'E']);
        if (exponentIndex < 0)
        {
            return formatted;
        }

        var marker = conversion is PrintfConversion.ScientificLower or PrintfConversion.GeneralLower ? 'e' : 'E';
        var exponent = formatted[(exponentIndex + 1)..];
        var sign = string.Empty;
        if (exponent.Length > 0 && exponent[0] is '+' or '-')
        {
            sign = exponent[0].ToString();
            exponent = exponent[1..];
        }

        while (exponent.Length > 2 && exponent[0] == '0')
        {
            exponent = exponent[1..];
        }

        while (exponent.Length < 2)
        {
            exponent = "0" + exponent;
        }

        return formatted[..exponentIndex] + marker + sign + exponent;
    }

    private static string ApplyNumericSign(
        string formatted,
        bool negative,
        bool forceSign,
        bool leadingSpace,
        bool signedConversion)
    {
        if (!signedConversion || negative || formatted.StartsWith('-'))
        {
            return formatted;
        }

        if (forceSign)
        {
            return "+" + formatted;
        }

        return leadingSpace ? " " + formatted : formatted;
    }

    private static string FormatCharacter(IPrintfArguments arguments, int index)
    {
        if (arguments.IsNumeric(index))
        {
            var code = (long)arguments.GetNumber(index);
            return code >= 0 && code <= int.MaxValue && Rune.IsValid((int)code)
                ? new Rune((int)code).ToString()
                : string.Empty;
        }

        var value = arguments.GetString(index);
        return value.Length == 0 ? string.Empty : Rune.GetRuneAt(value, 0).ToString();
    }

    private sealed class PrintfByteOutput : IDisposable
    {
        private readonly AwkRuntimeLimits _limits;
        private readonly AwkRuntimeCounters _counters;
        private readonly AwkMemoryGovernor _governor;
        private readonly AwkUtf8Builder _builder;

        public PrintfByteOutput(
            AwkRuntimeLimits limits,
            AwkRuntimeCounters counters,
            AwkMemoryGovernor governor)
        {
            _limits = limits;
            _counters = counters;
            _governor = governor;
            _builder = new AwkUtf8Builder(limits, governor, capacity: 0);
        }

        public void Append(string text) => Append(text.AsSpan());

        public void Append(ReadOnlySpan<char> text)
        {
            var byteCount = AwkUtf8Codec.GetEncodedByteCount(text);
            PreflightAdditionalBytes(byteCount);
            _builder.AppendUtf8(text);
        }

        public void AppendAscii(char value)
        {
            if (value > 0x7F)
                throw new ArgumentOutOfRangeException(nameof(value));

            AppendAscii((byte)value);
        }

        public void AppendAscii(byte value)
        {
            PreflightAdditionalBytes(1);
            _builder.Append(value);
        }

        public void AppendAscii(char value, int count)
        {
            if (value > 0x7F)
                throw new ArgumentOutOfRangeException(nameof(value));

            PreflightAdditionalBytes(count);
            _builder.Append((byte)value, count);
        }

        public void PreflightAdditionalBytes(int byteCount)
        {
            if (byteCount <= 0)
                return;

            var nextLength = AwkRuntimeMemory.CheckedAdd(_builder.Length, byteCount);
            if (nextLength > _limits.MaxBuilderBytes ||
                _limits.MaxValueBytes != AwkRuntimeLimits.Unlimited && nextLength > _limits.MaxValueBytes)
            {
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            }

            _governor.PreflightAllocation(byteCount);
        }

        public AwkText ToAwkText()
        {
            if (_builder.Length > 0)
                _counters.RecordUtf8Encode();

            return _builder.CopyToAwkText(_counters);
        }

        public void Dispose() => _builder.Dispose();
    }
}

internal interface IPrintfArguments
{
    int Count { get; }

    string GetString(int index);

    double GetNumber(int index);

    bool IsNumeric(int index);
}

internal readonly struct AwkValueArrayPrintfArguments(
    AwkValue[] arguments,
    int count,
    AwkRuntimeState state) : IPrintfArguments
{
    public int Count => count;

    public string GetString(int index)
    {
        state.Counters.RecordFormattingStringInteropInput();
        return state.FormatStringByteText(arguments[index]).AsString(state.Counters);
    }

    public double GetNumber(int index) => arguments[index].AsNumber();

    public bool IsNumeric(int index) =>
        arguments[index].IsNumberValue || arguments[index].IsNumericStringValue;
}

internal readonly struct AwkBuiltinContextPrintfArguments(
    AwkBuiltinContext context,
    int startIndex) : IPrintfArguments
{
    public int Count => context.ArgumentCount - startIndex;

    public string GetString(int index)
    {
        context.State.Counters.RecordFormattingStringInteropInput();
        return context.GetString(index + startIndex);
    }

    public double GetNumber(int index) => context.GetNumber(index + startIndex);

    public bool IsNumeric(int index)
    {
        var value = context.GetValue(index + startIndex);
        return value.IsNumberValue || value.IsNumericStringValue;
    }
}

internal readonly struct AwkSingleValuePrintfArguments(
    AwkValue value,
    AwkRuntimeCounters counters) : IPrintfArguments
{
    public int Count => 1;

    public string GetString(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

        counters.RecordFormattingStringInteropInput();
        return value.AsString(counters);
    }

    public double GetNumber(int index) =>
        index == 0 ? value.AsNumber() : throw new ArgumentOutOfRangeException(nameof(index));

    public bool IsNumeric(int index) =>
        index == 0
            ? value.IsNumberValue || value.IsNumericStringValue
            : throw new ArgumentOutOfRangeException(nameof(index));
}

internal readonly record struct PrintfDirective(
    PrintfFlags Flags,
    PrintfWidth Width,
    PrintfPrecision Precision,
    PrintfConversion Conversion,
    char Specifier);

internal readonly record struct PrintfFlags(
    bool LeftJustify,
    bool ForceSign,
    bool LeadingSpace,
    bool ZeroPad,
    bool AlternateForm)
{
    public static PrintfFlags None { get; } = new(false, false, false, false, false);

    public PrintfFlags WithLeftJustify() => this with { LeftJustify = true };

    public PrintfFlags WithForceSign() => this with { ForceSign = true };

    public PrintfFlags WithLeadingSpace() => this with { LeadingSpace = true };

    public PrintfFlags WithZeroPad() => this with { ZeroPad = true };

    public PrintfFlags WithAlternateForm() => this with { AlternateForm = true };
}

internal readonly record struct PrintfWidth(PrintfWidthKind Kind, int Value)
{
    public static PrintfWidth None { get; } = new(PrintfWidthKind.None, 0);

    public static PrintfWidth Dynamic { get; } = new(PrintfWidthKind.Dynamic, 0);

    public static PrintfWidth Literal(int value) => new(PrintfWidthKind.Literal, value);
}

internal enum PrintfWidthKind
{
    None,
    Literal,
    Dynamic
}

internal readonly record struct PrintfPrecision(PrintfPrecisionKind Kind, int Value)
{
    public static PrintfPrecision None { get; } = new(PrintfPrecisionKind.None, 0);

    public static PrintfPrecision Dynamic { get; } = new(PrintfPrecisionKind.Dynamic, 0);

    public static PrintfPrecision Literal(int value) => new(PrintfPrecisionKind.Literal, value);
}

internal enum PrintfPrecisionKind
{
    None,
    Literal,
    Dynamic
}

internal readonly record struct PrintfWidthValue(int Value);

internal readonly record struct PrintfPrecisionValue
{
    private PrintfPrecisionValue(bool hasValue, int value)
    {
        HasValue = hasValue;
        Value = value;
    }

    public static PrintfPrecisionValue None { get; } = new(false, 0);

    public static PrintfPrecisionValue Specified(int value) => new(true, value);

    public bool HasValue { get; }

    public int Value { get; }
}

internal enum PrintfConversion
{
    Unknown,
    Percent,
    String,
    SignedDecimal,
    SignedInteger,
    UnsignedDecimal,
    Octal,
    HexLower,
    HexUpper,
    FixedLower,
    FixedUpper,
    ScientificLower,
    ScientificUpper,
    GeneralLower,
    GeneralUpper,
    Character
}

internal static class PrintfConversionFacts
{
    public static PrintfConversion Parse(char specifier) =>
        specifier switch
        {
            '%' => PrintfConversion.Percent,
            's' => PrintfConversion.String,
            'd' => PrintfConversion.SignedDecimal,
            'i' => PrintfConversion.SignedInteger,
            'u' => PrintfConversion.UnsignedDecimal,
            'o' => PrintfConversion.Octal,
            'x' => PrintfConversion.HexLower,
            'X' => PrintfConversion.HexUpper,
            'f' => PrintfConversion.FixedLower,
            'F' => PrintfConversion.FixedUpper,
            'e' => PrintfConversion.ScientificLower,
            'E' => PrintfConversion.ScientificUpper,
            'g' => PrintfConversion.GeneralLower,
            'G' => PrintfConversion.GeneralUpper,
            'c' => PrintfConversion.Character,
            _ => PrintfConversion.Unknown
        };

    public static bool IsInteger(this PrintfConversion conversion) =>
        conversion is
            PrintfConversion.SignedDecimal or
            PrintfConversion.SignedInteger or
            PrintfConversion.UnsignedDecimal or
            PrintfConversion.Octal or
            PrintfConversion.HexLower or
            PrintfConversion.HexUpper;

    public static bool IsFloat(this PrintfConversion conversion) =>
        conversion is
            PrintfConversion.FixedLower or
            PrintfConversion.FixedUpper or
            PrintfConversion.ScientificLower or
            PrintfConversion.ScientificUpper or
            PrintfConversion.GeneralLower or
            PrintfConversion.GeneralUpper;

    public static bool IsUppercase(this PrintfConversion conversion) =>
        conversion is
            PrintfConversion.HexUpper or
            PrintfConversion.FixedUpper or
            PrintfConversion.ScientificUpper or
            PrintfConversion.GeneralUpper;
}
