using System;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal enum PrintfMissingArgumentPolicy
{
    OmitDirective,
    Throw
}

internal static class PrintfEvaluation
{
    public static AwkText FormatByteText<TArguments>(
        string format,
        TArguments arguments,
        PrintfMissingArgumentPolicy missingArgumentPolicy,
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null,
        AwkMemoryLease? retainedTextLease = null)
        where TArguments : IPrintfArguments
    {
        if (format.Length == 0)
        {
            checkpoint?.Invoke();
            return AwkText.Empty;
        }

        using var builder = new AwkUtf8Builder(limits, governor, capacity: 0);
        FormatToBuilder(builder, format, arguments, missingArgumentPolicy, limits, counters, governor,
            compatibilityMode, checkpoint);
        var output = new PrintfByteOutput(builder, limits, counters, checkpoint, retainedTextLease);
        var result = output.ToAwkText();
        checkpoint?.Invoke();
        return result;
    }

    public static void FormatToBuilder<TArguments>(
        AwkUtf8Builder builder,
        string format,
        TArguments arguments,
        PrintfMissingArgumentPolicy missingArgumentPolicy,
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
        where TArguments : IPrintfArguments
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (format.Length == 0)
        {
            checkpoint?.Invoke();
            return;
        }
        var output = new PrintfByteOutput(builder, limits, counters, checkpoint, null);
        var argumentCursor = new PrintfArgumentCursor<TArguments>(arguments, missingArgumentPolicy, format, counters, governor, checkpoint);

        long nextCheckpoint = 0;
        for (var index = 0; index < format.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)index + AwkRuntimeLimits.CancellationCheckBytes;
            }
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

            var directiveStart = index;
            var directive = PrintfDirectiveParser.Read(format, index, compatibilityMode, ref argumentCursor, checkpoint);
            index = directive.EndIndex;
            var flags = directive.Flags;
            var width = directive.Width;
            var precision = directive.Precision;

            if (directive.Conversion == PrintfConversion.Percent)
            {
                output.AppendAscii('%');
                continue;
            }

            if (directive.Conversion == PrintfConversion.String)
            {
                if (argumentCursor.TryReadIndex(directive.ArgumentPosition, out var argumentIndex))
                {
                    output.PreflightAdditionalBytes(width.Value);
                    if (width.Value == 0 && !precision.HasValue && arguments.TryGetBorrowedBytes(argumentIndex, out var borrowed))
                    {
                        checkpoint?.Invoke();
                        output.Append(borrowed);
                        continue;
                    }
                    var text = arguments.GetByteText(argumentIndex);
                    checkpoint?.Invoke();
                    if (width.Value == 0 && !precision.HasValue)
                    {
                        output.Append(text);
                        continue;
                    }

                    counters.RecordFormattingStringInteropInput();
                    var value = text.AsString(counters, checkpoint);
                    if (precision.HasValue)
                        value = TruncateToRuneCount(value, precision.Value, checkpoint);

                    AppendWithWidth(output, value, width.Value, flags.LeftJustify, zeroPad: false, checkpoint);
                }

                continue;
            }

            if (directive.Conversion.IsInteger() || directive.Conversion.IsFloat())
            {
                if (!argumentCursor.TryReadIndex(directive.ArgumentPosition, out var argumentIndex))
                    continue;
                var number = arguments.GetNumber(argumentIndex);
                checkpoint?.Invoke();
                if (!double.IsFinite(number))
                {
                    var special = FormatNonfinite(number, directive.Conversion, flags, compatibilityMode);
                    output.PreflightAdditionalBytes(Math.Max(width.Value, special.Length));
                    AppendWithWidth(output, special, width.Value, flags.LeftJustify, zeroPad: false, checkpoint);
                }
                else if (directive.Conversion.IsInteger())
                    AppendInteger(output, number, directive.Conversion, flags, width.Value, precision, counters);
                else if (directive.Conversion is PrintfConversion.HexFloatLower or PrintfConversion.HexFloatUpper)
                {
                    var hexadecimal = AwkHexFloatFormat.CreateFinite(number, precision, flags, directive.Conversion.IsUppercase());
                    output.PreflightAdditionalBytes(Math.Max(width.Value, hexadecimal.ByteCount));
                    checkpoint?.Invoke();
                    var formatted = hexadecimal.Format();
                    checkpoint?.Invoke();
                    AppendWithWidth(output, formatted, width.Value, flags.LeftJustify, flags.ZeroPad, checkpoint);
                }
                else
                {
                    output.PreflightAdditionalBytes(Math.Max(
                        width.Value, EstimateFloatByteCount(number, directive.Conversion, precision)));
                    var formatted = FormatFloat(number, directive.Conversion, precision, flags, checkpoint);
                    AppendWithWidth(output, formatted, width.Value, flags.LeftJustify, flags.ZeroPad, checkpoint);
                }
                continue;
            }

            if (directive.Conversion == PrintfConversion.Character)
            {
                if (argumentCursor.TryReadIndex(directive.ArgumentPosition, out var argumentIndex))
                {
                    var formatted = FormatCharacter(arguments, argumentIndex, counters, checkpoint);
                    output.PreflightAdditionalBytes(Math.Max(
                        width.Value,
                        AwkUtf8Codec.GetEncodedByteCount(formatted)));
                    AppendWithWidth(
                        output,
                        formatted,
                        width.Value,
                        flags.LeftJustify,
                        zeroPad: false, checkpoint);
                }

                continue;
            }

            output.Append(format.AsSpan(directiveStart, index - directiveStart + 1));
        }

        checkpoint?.Invoke();
        if (builder.Length > 0)
            counters.RecordUtf8Encode();

        static void AppendInteger(
            PrintfByteOutput output, double number, PrintfConversion conversion, PrintfFlags flags,
            int width, PrintfPrecisionValue precision, AwkRuntimeCounters counters)
        {
            var signed = conversion is PrintfConversion.SignedDecimal or PrintfConversion.SignedInteger;
            var integral = Math.Truncate(number);
            Span<char> storage = stackalloc char[310]; // The largest finite double has 309 decimal digits.
            scoped ReadOnlySpan<char> digits;
            var sign = signed ? integral < 0 ? '-' : flags.ForceSign ? '+' : flags.LeadingSpace ? ' ' : '\0' : '\0';
            if (conversion is PrintfConversion.SignedDecimal or PrintfConversion.SignedInteger or PrintfConversion.UnsignedDecimal)
                flags = flags with { AlternateForm = false };
            if (precision.HasValue)
                flags = flags with { ZeroPad = false };

            if (signed)
            {
                var magnitude = Math.Abs(integral);
                int written;
                if (magnitude < 18446744073709551616.0)
                {
                    ((ulong)magnitude).TryFormat(storage, out written, provider: CultureInfo.InvariantCulture);
                    counters.RecordPrintfSignedIntegerFastPath();
                }
                else
                    magnitude.TryFormat(storage, out written, "F0", CultureInfo.InvariantCulture);
                digits = storage[..written];
            }
            else
            {
                // GNU uses a 64-bit unsigned value, with negative inputs first converted
                // through signed 64-bit. Unrepresentable values use the original number as %g.
                if (integral is >= 18446744073709551616.0 or < -9223372036854775808.0)
                {
                    AppendIntegerFallback(output, number, flags, width, precision);
                    return;
                }
                var magnitude = integral < 0 ? unchecked((ulong)(long)integral) : (ulong)integral;
                if (conversion == PrintfConversion.Octal)
                {
                    var start = storage.Length;
                    do
                    {
                        storage[--start] = (char)('0' + (magnitude & 7));
                        magnitude >>= 3;
                    } while (magnitude > 0);
                    digits = storage[start..];
                }
                else
                {
                    var format = conversion == PrintfConversion.HexLower ? "x" : conversion == PrintfConversion.HexUpper ? "X" : "D";
                    magnitude.TryFormat(storage, out var written, format, CultureInfo.InvariantCulture);
                    digits = storage[..written];
                }
            }

            if (precision.HasValue && precision.Value == 0 && number == 0 &&
                !(flags.AlternateForm && conversion == PrintfConversion.Octal))
                digits = [];
            var zeros = precision.HasValue ? Math.Max(0, precision.Value - digits.Length) : 0;
            var prefix = flags.AlternateForm && number != 0
                ? conversion == PrintfConversion.HexLower ? "0x"
                    : conversion == PrintfConversion.HexUpper ? "0X"
                    : conversion == PrintfConversion.Octal && zeros == 0 && !digits.IsEmpty && digits[0] != '0' ? "0" : ""
                : "";
            var length = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedAdd(digits.Length, zeros),
                prefix.Length + (sign == '\0' ? 0 : 1));
            output.PreflightAdditionalBytes(Math.Max(width, length));
            var padding = Math.Max(0, width - length);
            if (!flags.LeftJustify && !flags.ZeroPad)
                output.AppendAscii(' ', padding);
            if (sign != '\0')
                output.AppendAscii(sign);
            output.Append(prefix);
            if (!flags.LeftJustify && flags.ZeroPad)
                output.AppendAscii('0', padding);
            output.AppendAscii('0', zeros);
            output.Append(digits);
            if (flags.LeftJustify)
                output.AppendAscii(' ', padding);
        }

        static void AppendIntegerFallback(
            PrintfByteOutput output, double number, PrintfFlags flags, int width, PrintfPrecisionValue precision)
        {
            var significantPrecision = precision.HasValue ? Math.Max(1, precision.Value) : 6;
            Span<char> format = stackalloc char[4];
            format[0] = 'G';
            // Every value in this fallback is an integral double with at most 309 digits.
            // Additional precision only adds alternate-form zeros, which the builder fills.
            Math.Min(309, significantPrecision).TryFormat(format[1..], out var formatLength, provider: CultureInfo.InvariantCulture);
            Span<char> storage = stackalloc char[315]; // Up to 308 digits, a point, and e+308, or 309 fixed digits.
            if (!Math.Abs(number).TryFormat(storage, out var written, format[..(formatLength + 1)], CultureInfo.InvariantCulture))
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            var exponentIndex = storage[..written].IndexOf('E');
            var mantissaLength = exponentIndex < 0 ? written : exponentIndex;
            var hasPoint = storage[..mantissaLength].Contains('.');
            var significantDigits = mantissaLength - (hasPoint ? 1 : 0);
            var zeros = flags.AlternateForm ? Math.Max(0, significantPrecision - significantDigits) : 0;
            var addPoint = flags.AlternateForm && !hasPoint;
            var sign = number < 0 ? '-' : flags.ForceSign ? '+' : flags.LeadingSpace ? ' ' : '\0';
            var length = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedAdd(written, zeros),
                (addPoint ? 1 : 0) + (sign == '\0' ? 0 : 1));
            output.PreflightAdditionalBytes(Math.Max(width, length));
            var padding = Math.Max(0, width - length);
            if (!flags.LeftJustify && !flags.ZeroPad)
                output.AppendAscii(' ', padding);
            if (sign != '\0')
                output.AppendAscii(sign);
            if (!flags.LeftJustify && flags.ZeroPad)
                output.AppendAscii('0', padding);
            output.Append(storage[..mantissaLength]);
            if (addPoint)
                output.AppendAscii('.');
            output.AppendAscii('0', zeros);
            if (exponentIndex >= 0)
            {
                output.AppendAscii('e');
                output.Append(storage[(exponentIndex + 1)..written]);
            }
            if (flags.LeftJustify)
                output.AppendAscii(' ', padding);
        }
        static string FormatFloat(
            double value,
            PrintfConversion conversion,
            PrintfPrecisionValue precision,
            PrintfFlags flags, Action? checkpoint)
        {
            checkpoint?.Invoke();
            var digits = precision.HasValue ? precision.Value : 6;
            if (conversion is PrintfConversion.GeneralLower or PrintfConversion.GeneralUpper && digits == 0)
                digits = 1;
            var formatSpecifier = conversion switch
            {
                PrintfConversion.FixedLower or PrintfConversion.FixedUpper => "F" + digits.ToString(CultureInfo.InvariantCulture),
                PrintfConversion.ScientificLower => "e" + digits.ToString(CultureInfo.InvariantCulture),
                PrintfConversion.ScientificUpper => "E" + digits.ToString(CultureInfo.InvariantCulture),
                PrintfConversion.GeneralLower or PrintfConversion.GeneralUpper => "G" + digits.ToString(CultureInfo.InvariantCulture),
                _ => "G" + digits.ToString(CultureInfo.InvariantCulture)
            };
            var formatted = value.ToString(formatSpecifier, CultureInfo.InvariantCulture);
            checkpoint?.Invoke();
            formatted = NormalizeFloatExponent(formatted, conversion, checkpoint);
            formatted = ApplyAlternateFloatForm(formatted, flags.AlternateForm, conversion, digits, checkpoint);
            return ApplyNumericSign(formatted, value < 0, flags.ForceSign, flags.LeadingSpace, signedConversion: true);

            static string ApplyAlternateFloatForm(string formatted, bool alternateForm, PrintfConversion conversion, int precision, Action? checkpoint)
            {
                checkpoint?.Invoke();
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
                    var significantDigits = CountSignificantDigits(mantissa, checkpoint);
                    if (significantDigits < significantPrecision)
                    {
                        mantissa += new string('0', significantPrecision - significantDigits);
                    }
                }

                checkpoint?.Invoke();
                var result = mantissa + exponent;
                checkpoint?.Invoke();
                return result;

                static int CountSignificantDigits(string mantissa, Action? checkpoint)
                {
                    var count = 0;
                    long nextCheckpoint = 0;
                    var sawNonZero = false;
                    var hasDigit = false;
                    for (var index = 0; index < mantissa.Length; index++)
                    {
                        if (index >= nextCheckpoint)
                        {
                            checkpoint?.Invoke();
                            nextCheckpoint = (long)index + AwkRuntimeLimits.CancellationCheckBytes;
                        }
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
            }

            static string NormalizeFloatExponent(string formatted, PrintfConversion conversion, Action? checkpoint)
            {
                checkpoint?.Invoke();
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
                    checkpoint?.Invoke();
                    exponent = exponent[1..];
                }

                while (exponent.Length < 2)
                {
                    checkpoint?.Invoke();
                    exponent = "0" + exponent;
                }

                return formatted[..exponentIndex] + marker + sign + exponent;
            }
        }

        static void AppendWithWidth(PrintfByteOutput output, string text, int width, bool leftJustify, bool zeroPad, Action? checkpoint)
        {
            var textWidth = CountRunes(text, checkpoint);
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

            var prefixLength = text.StartsWith('-') || text.StartsWith('+') || text.StartsWith(' ') ? 1 : 0;
            if (text.AsSpan(prefixLength).StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                prefixLength += 2;
            if (zeroPad && prefixLength > 0)
            {
                output.Append(text.AsSpan(0, prefixLength));
                output.AppendAscii('0', width - textWidth);
                output.Append(text.AsSpan(prefixLength));
                return;
            }

            output.AppendAscii(zeroPad ? '0' : ' ', width - textWidth);
            output.Append(text);

            static int CountRunes(string text, Action? checkpoint)
            {
                checkpoint?.Invoke();
                var count = 0;
                var utf16Length = 0;
                long nextCheckpoint = 0;
                foreach (var rune in text.EnumerateRunes())
                {
                    if (utf16Length >= nextCheckpoint)
                    {
                        checkpoint?.Invoke();
                        nextCheckpoint = (long)utf16Length + AwkRuntimeLimits.CancellationCheckBytes;
                    }
                    count++;
                    utf16Length += rune.Utf16SequenceLength;
                }
                checkpoint?.Invoke();
                return count;
            }
        }

        static void AppendFormatCharacter(
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

        static int EstimateFloatByteCount(
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

        static string TruncateToRuneCount(string text, int maximumRuneCount, Action? checkpoint)
        {
            checkpoint?.Invoke();
            if (maximumRuneCount <= 0 || text.Length == 0)
                return string.Empty;

            var runeCount = 0;
            var utf16Length = 0;
            long nextCheckpoint = 0;
            foreach (var rune in text.EnumerateRunes())
            {
                if (utf16Length >= nextCheckpoint)
                {
                    checkpoint?.Invoke();
                    nextCheckpoint = (long)utf16Length + AwkRuntimeLimits.CancellationCheckBytes;
                }
                if (runeCount == maximumRuneCount)
                    break;

                runeCount++;
                utf16Length += rune.Utf16SequenceLength;
            }

            checkpoint?.Invoke();
            var truncated = utf16Length == text.Length ? text : text[..utf16Length];
            checkpoint?.Invoke();
            return truncated;
        }

        static string FormatCharacter(IPrintfArguments arguments, int index, AwkRuntimeCounters counters, Action? checkpoint)
        {
            checkpoint?.Invoke();
            if (arguments.IsNumeric(index))
            {
                var code = (long)arguments.GetNumber(index);
                checkpoint?.Invoke();
                return code >= 0 && code <= int.MaxValue && Rune.IsValid((int)code)
                    ? new Rune((int)code).ToString()
                    : string.Empty;
            }

            counters.RecordFormattingStringInteropInput();
            var value = arguments.GetByteText(index).AsString(counters, checkpoint);
            checkpoint?.Invoke();
            return value.Length == 0 ? string.Empty : Rune.GetRuneAt(value, 0).ToString();
        }
    }

    private static string FormatNonfinite(
        double value, PrintfConversion conversion, PrintfFlags flags, AwkCompatibilityMode mode)
    {
        var negative = BitConverter.DoubleToUInt64Bits(value) >> 63 != 0;
        var sign = negative ? '-' : mode != AwkCompatibilityMode.Posix || flags.ForceSign ? '+' : flags.LeadingSpace ? ' ' : '\0';
        var uppercase = conversion.IsUppercase() && (mode != AwkCompatibilityMode.Posix || conversion.IsFloat());
        return (double.IsNaN(value), uppercase, sign) switch
        {
            (true, false, '-') => "-nan",
            (true, false, '+') => "+nan",
            (true, false, ' ') => " nan",
            (true, false, _) => "nan",
            (true, true, '-') => "-NAN",
            (true, true, '+') => "+NAN",
            (true, true, ' ') => " NAN",
            (true, true, _) => "NAN",
            (false, false, '-') => "-inf",
            (false, false, '+') => "+inf",
            (false, false, ' ') => " inf",
            (false, false, _) => "inf",
            (false, true, '-') => "-INF",
            (false, true, '+') => "+INF",
            (false, true, ' ') => " INF",
            (false, true, _) => "INF"
        };
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

    private readonly struct PrintfByteOutput
    {
        private readonly AwkRuntimeLimits _limits;
        private readonly AwkRuntimeCounters _counters;
        private readonly AwkUtf8Builder _builder;
        private readonly Action? _checkpoint;
        private readonly AwkMemoryLease? _retainedTextLease;

        public PrintfByteOutput(
            AwkUtf8Builder builder,
            AwkRuntimeLimits limits,
            AwkRuntimeCounters counters,
            Action? checkpoint, AwkMemoryLease? retainedTextLease)
        {
            _checkpoint = checkpoint;
            _retainedTextLease = retainedTextLease;
            _limits = limits;
            _counters = counters;
            _builder = builder;
        }

        public void Append(AwkText text)
        {
            var bytes = text.AsSpan(_checkpoint);
            PreflightAdditionalBytes(bytes.Length);
            _builder.Append(bytes, _checkpoint);
        }

        public void Append(ReadOnlySpan<byte> bytes)
        {
            PreflightAdditionalBytes(bytes.Length);
            _builder.Append(bytes, _checkpoint);
        }

        public void Append(string text) => Append(text.AsSpan());

        public void Append(ReadOnlySpan<char> text)
        {
            var longText = text.Length >= AwkRuntimeLimits.CancellationCheckBytes;
            if (longText)
                _checkpoint?.Invoke();
            var byteCount = AwkUtf8Codec.GetEncodedByteCount(text);
            if (longText)
                _checkpoint?.Invoke();
            PreflightAdditionalBytes(byteCount);
            _builder.AppendUtf8(text);
            if (longText)
                _checkpoint?.Invoke();
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

            var longPadding = count >= AwkRuntimeLimits.CancellationCheckBytes;
            if (longPadding)
                _checkpoint?.Invoke();
            PreflightAdditionalBytes(count);
            _builder.Append((byte)value, count);
            if (longPadding)
                _checkpoint?.Invoke();
        }

        public void PreflightAdditionalBytes(int byteCount)
        {
            if (byteCount <= 0)
                return;

            var nextLength = AwkRuntimeMemory.CheckedAdd(_builder.Length, byteCount);
            if (nextLength > _limits.MaxBuilderBytes)
            {
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            }
        }

        public AwkText ToAwkText()
        {
            if (_retainedTextLease is null)
                return _builder.CopyToAwkText(_counters, _checkpoint);

            _retainedTextLease.ReserveAdditional(AwkRuntimeMemory.EstimateTextBytes(_builder.Length));
            return AwkText.CopyTrustedBytes(_builder.AsSpan(), _counters,
                AwkMemoryAccounting.Disabled, _checkpoint);
        }

    }
}

internal interface IPrintfArguments
{
    int Count { get; }

    AwkText GetByteText(int index);

    double GetNumber(int index);

    bool IsNumeric(int index);

    bool TryGetBorrowedBytes(int index, out ReadOnlySpan<byte> bytes) { bytes = default; return false; }
}

internal readonly struct AwkValueArrayPrintfArguments(
    AwkValue[] arguments,
    int count,
    AwkRuntimeState state) : IPrintfArguments
{
    public int Count => count;

    public bool TryGetBorrowedBytes(int index, out ReadOnlySpan<byte> bytes) { bytes = default; return false; }

    public AwkText GetByteText(int index) =>
        state.FormatStringByteText(arguments[index].ForceScalar());

    public double GetNumber(int index) => arguments[index].ForceScalar().AsNumber(state.CancellationCheckpoint);

    public bool IsNumeric(int index) =>
        arguments[index].IsNumberValue || arguments[index].IsNumericStringValue;
}

internal readonly struct AwkBuiltinContextPrintfArguments(
    AwkBuiltinContext context,
    int startIndex) : IPrintfArguments
{
    public int Count => context.ArgumentCount - startIndex;

    public bool TryGetBorrowedBytes(int index, out ReadOnlySpan<byte> bytes) { bytes = default; return false; }

    public AwkText GetByteText(int index) =>
        context.State.FormatStringByteText(context.GetValue(index + startIndex).ForceScalar());

    public double GetNumber(int index) => context.GetValue(index + startIndex).ForceScalar().AsNumber(context.State.CancellationCheckpoint);

    public bool IsNumeric(int index)
    {
        var value = context.GetValue(index + startIndex);
        return value.IsNumberValue || value.IsNumericStringValue;
    }
}

internal readonly struct AwkSingleValuePrintfArguments(
    AwkValue value,
    AwkRuntimeCounters counters,
    Action? checkpoint = null) : IPrintfArguments
{
    public int Count => 1;

    public bool TryGetBorrowedBytes(int index, out ReadOnlySpan<byte> bytes) { bytes = default; return false; }

    public AwkText GetByteText(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

        // Custom scalar formats retain their existing full-precision string conversion.
        return value.IsNumberValue
            ? AwkText.FromString(value.AsString(counters, checkpoint), counters, checkpoint)
            : value.ByteText;
    }

    public double GetNumber(int index) =>
        index == 0 ? value.AsNumber(checkpoint) : throw new ArgumentOutOfRangeException(nameof(index));

    public bool IsNumeric(int index) =>
        index == 0
            ? value.IsNumberValue || value.IsNumericStringValue
            : throw new ArgumentOutOfRangeException(nameof(index));
}

internal readonly record struct PrintfDirective(
    PrintfFlags Flags,
    PrintfWidthValue Width,
    PrintfPrecisionValue Precision,
    PrintfConversion Conversion,
    PrintfArgumentPosition ArgumentPosition,
    int EndIndex);

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
    Character,
    HexFloatLower,
    HexFloatUpper
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
            'a' => PrintfConversion.HexFloatLower,
            'A' => PrintfConversion.HexFloatUpper,
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
            PrintfConversion.GeneralUpper or
            PrintfConversion.HexFloatLower or
            PrintfConversion.HexFloatUpper;

    public static bool IsUppercase(this PrintfConversion conversion) =>
        conversion is
            PrintfConversion.HexUpper or
            PrintfConversion.FixedUpper or
            PrintfConversion.ScientificUpper or
            PrintfConversion.GeneralUpper or
            PrintfConversion.HexFloatUpper;
}
