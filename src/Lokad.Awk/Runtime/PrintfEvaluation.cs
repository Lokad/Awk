using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal static class PrintfEvaluation
{
    public static ReadOnlyMemory<byte> Format(
        string format,
        IReadOnlyList<string> arguments) =>
        Format(format, arguments, strictMissingArguments: false);

    public static ReadOnlyMemory<byte> Format(
        string format,
        IReadOnlyList<string> arguments,
        bool strictMissingArguments) =>
        Format(format, new PrintfStringListArguments(arguments, AwkRuntimeCounters.Disabled), strictMissingArguments);

    public static ReadOnlyMemory<byte> Format<TArguments>(
        string format,
        TArguments arguments,
        bool strictMissingArguments)
        where TArguments : IPrintfArguments
    {
        if (string.IsNullOrEmpty(format))
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var output = new StringBuilder(format.Length);
        var argIndex = 0;

        for (var index = 0; index < format.Length; index++)
        {
            var current = format[index];
            if (current == '\\')
            {
                if (index + 1 >= format.Length)
                {
                    output.Append('\\');
                    continue;
                }

                var escaped = format[++index];
                output.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'b' => '\b',
                    'f' => '\f',
                    'v' => '\v',
                    '\\' => '\\',
                    _ => escaped
                });
                continue;
            }

            if (current != '%')
            {
                output.Append(current);
                continue;
            }

            if (index + 1 >= format.Length)
            {
                output.Append(current);
                continue;
            }

            if (!TryReadDirective(format, ref index, out var directive))
            {
                output.Append(current);
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
                output.Append('%');
                continue;
            }

            if (directive.Conversion == PrintfConversion.String)
            {
                if (TryReadArgument(arguments, strictMissingArguments, ref argIndex, out var value))
                {
                    if (precision.HasValue && value.Length > precision.Value)
                    {
                        value = value[..precision.Value];
                    }

                    AppendWithWidth(output, value, width.Value, flags.LeftJustify, zeroPad: false);
                }

                continue;
            }

            if (directive.Conversion.IsInteger())
            {
                if (TryReadArgument(arguments, strictMissingArguments, ref argIndex, out var raw))
                {
                    var formatted = FormatInteger(raw, directive.Conversion, flags);
                    AppendWithWidth(output, formatted, width.Value, flags.LeftJustify, flags.ZeroPad);
                }

                continue;
            }

            if (directive.Conversion.IsFloat())
            {
                if (TryReadArgument(arguments, strictMissingArguments, ref argIndex, out var raw))
                {
                    var formatted = FormatFloat(raw, directive.Conversion, precision, flags);
                    AppendWithWidth(output, formatted, width.Value, flags.LeftJustify, flags.ZeroPad);
                }

                continue;
            }

            if (directive.Conversion == PrintfConversion.Character)
            {
                if (TryReadArgument(arguments, strictMissingArguments, ref argIndex, out var raw))
                {
                    AppendWithWidth(output, FormatCharacter(raw), width.Value, flags.LeftJustify, zeroPad: false);
                }

                continue;
            }

            output.Append('%');
            output.Append(directive.Specifier);
        }

        return AwkUtf8Codec.Encode(output.ToString());
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
                literalWidth = (literalWidth * 10) + (format[cursor] - '0');
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
                    literalPrecision = (literalPrecision * 10) + (format[cursor] - '0');
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
                    throw new FormatException("Not enough printf arguments.");
            }
            else
            {
                dynamicWidth = (int)ParseLong(arguments.GetString(argIndex++));
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
                    throw new FormatException("Not enough printf arguments.");
            }
            else
            {
                dynamicPrecision = (int)ParseLong(arguments.GetString(argIndex++));
            }

            return dynamicPrecision < 0
                ? PrintfPrecisionValue.None
                : PrintfPrecisionValue.Specified(dynamicPrecision);
        }

        return precision.Kind == PrintfPrecisionKind.Literal
            ? PrintfPrecisionValue.Specified(precision.Value)
            : PrintfPrecisionValue.None;
    }

    private static bool TryReadArgument(
        IPrintfArguments arguments,
        bool strictMissingArguments,
        ref int argIndex,
        out string value)
    {
        if (argIndex >= arguments.Count)
        {
            if (strictMissingArguments)
            {
                throw new FormatException("Not enough printf arguments.");
            }

            value = string.Empty;
            return false;
        }

        value = arguments.GetString(argIndex++);
        return true;
    }

    private static string FormatInteger(string raw, PrintfConversion conversion, PrintfFlags flags)
    {
        if (TryFormatInfinity(raw, conversion, out var infinity))
        {
            return infinity;
        }

        var value = ParseLong(raw);
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
        string raw,
        PrintfConversion conversion,
        PrintfPrecisionValue precision,
        PrintfFlags flags)
    {
        if (TryFormatInfinity(raw, conversion, out var infinity))
        {
            return infinity;
        }

        var value = ParseDouble(raw);
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

    private static void AppendWithWidth(StringBuilder output, string text, int width, bool leftJustify, bool zeroPad)
    {
        if (width <= 0 || text.Length >= width)
        {
            output.Append(text);
            return;
        }

        if (leftJustify)
        {
            output.Append(text);
            output.Append(' ', width - text.Length);
            return;
        }

        if (zeroPad && (text.StartsWith("-", StringComparison.Ordinal) ||
            text.StartsWith("+", StringComparison.Ordinal) ||
            text.StartsWith(" ", StringComparison.Ordinal)))
        {
            output.Append(text[0]);
            output.Append('0', width - text.Length);
            output.Append(text[1..]);
            return;
        }

        output.Append(zeroPad ? '0' : ' ', width - text.Length);
        output.Append(text);
    }

    private static long ParseLong(string value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return integer;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return (long)number;
        }

        return 0;
    }

    private static double ParseDouble(string value) =>
        TryParseSignedInfinity(value, out var infinity)
            ? infinity
            : double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 0;

    private static bool TryFormatInfinity(string value, PrintfConversion conversion, out string formatted)
    {
        if (!TryParseSignedInfinity(value, out var number))
        {
            formatted = string.Empty;
            return false;
        }

        formatted = number < 0 ? "-inf" : "+inf";
        if (conversion.IsUppercase())
        {
            formatted = formatted.ToUpperInvariant();
        }

        return true;
    }

    private static bool TryParseSignedInfinity(string value, out double number)
    {
        var trimmed = value.Trim();
        number = 0;
        if (trimmed.Length != 4 || trimmed[0] is not ('+' or '-'))
        {
            return false;
        }

        if (!string.Equals(trimmed[1..], "inf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        number = trimmed[0] == '+' ? double.PositiveInfinity : double.NegativeInfinity;
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
            PrintfConversion.Octal => formatted.StartsWith("0", StringComparison.Ordinal) ? formatted : "0" + formatted,
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
        if (!signedConversion || negative || formatted.StartsWith("-", StringComparison.Ordinal))
        {
            return formatted;
        }

        if (forceSign)
        {
            return "+" + formatted;
        }

        return leadingSpace ? " " + formatted : formatted;
    }

    private static string FormatCharacter(string value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) &&
            code >= char.MinValue &&
            code <= char.MaxValue)
        {
            return ((char)code).ToString();
        }

        return value.Length == 0 ? string.Empty : value[0].ToString();
    }
}

internal interface IPrintfArguments
{
    int Count { get; }

    string GetString(int index);
}

internal readonly struct PrintfStringListArguments(
    IReadOnlyList<string> arguments,
    AwkRuntimeCounters counters) : IPrintfArguments
{
    public int Count => arguments.Count;

    public string GetString(int index)
    {
        counters.RecordFormattingStringInteropInput();
        return arguments[index];
    }
}

internal readonly struct AwkValueArrayPrintfArguments(
    AwkValue[] arguments,
    int count,
    AwkRuntimeCounters counters) : IPrintfArguments
{
    public int Count => count;

    public string GetString(int index)
    {
        counters.RecordFormattingStringInteropInput();
        return arguments[index].AsString(counters);
    }
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
