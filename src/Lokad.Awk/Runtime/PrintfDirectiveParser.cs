namespace Lokad.Awk;

internal static class PrintfDirectiveParser
{
    public static PrintfDirective Read<TArguments>(
        string format, int index, AwkCompatibilityMode mode, ref PrintfArgumentCursor<TArguments> arguments)
        where TArguments : IPrintfArguments
    {
        var cursor = index + 1;
        var flags = PrintfFlags.None;
        var width = new PrintfWidthValue(0);
        var precision = PrintfPrecisionValue.None;
        var position = default(PrintfArgumentPosition);
        var part = PrintfDirectivePart.Width;
        var negativePrecision = false;
        var modifiers = 0;
        var specifier = '\0';

        // GNU accepts flags and ignored modifiers interspersed with the field width.
        // Resolve controls as encountered: incomplete directives still consume dynamic
        // arguments and validate explicit positions.
        while (cursor < format.Length)
        {
            specifier = format[cursor++];
            switch (specifier)
            {
                case >= '0' and <= '9':
                    if (part == PrintfDirectivePart.Complete)
                        goto Finish;
                    if (specifier == '0' && part == PrintfDirectivePart.Width)
                    {
                        flags = flags.WithZeroPad();
                        if (flags.LeftJustify)
                            continue;
                    }
                    cursor--;
                    var value = ReadDigits(format, ref cursor);
                    if (part == PrintfDirectivePart.Width)
                        width = new(value);
                    else
                    {
                        precision = negativePrecision ? PrintfPrecisionValue.None : PrintfPrecisionValue.Specified(value);
                        part = PrintfDirectivePart.Complete;
                    }
                    continue;
                case '$':
                    if (mode != AwkCompatibilityMode.GnuDefault)
                        throw new AwkRuntimeException(AwkMessages.ErrorPrintfPositionalProfile);
                    var argumentNumber = part == PrintfDirectivePart.Width ? width.Value : precision.Value;
                    if (argumentNumber == 0)
                        throw new AwkRuntimeException(AwkMessages.ErrorPrintfArgumentPosition);
                    position = new(true, argumentNumber);
                    arguments.BeginDirective(position);
                    if (part == PrintfDirectivePart.Width)
                        width = new(0);
                    else
                        precision = PrintfPrecisionValue.Specified(0);
                    continue;
                case '*':
                    if (part == PrintfDirectivePart.Complete)
                        goto Finish;
                    var control = ReadControlPosition(format, ref cursor, mode);
                    var number = arguments.ReadDynamicNumber(control);
                    if (part == PrintfDirectivePart.Width)
                        width = ResolveWidth(number, ref flags);
                    else
                    {
                        precision = ResolvePrecision(number);
                        negativePrecision = number < 0;
                        part = PrintfDirectivePart.Complete;
                    }
                    continue;
                case '-':
                    if (negativePrecision)
                        goto Finish;
                    if (part == PrintfDirectivePart.Precision)
                    {
                        negativePrecision = true;
                        precision = PrintfPrecisionValue.None;
                        continue;
                    }
                    flags = flags.WithLeftJustify();
                    break;
                case '+': flags = flags.WithForceSign(); break;
                case ' ': flags = flags.WithLeadingSpace(); break;
                case '#': flags = flags.WithAlternateForm(); break;
                case '.':
                    if (part != PrintfDirectivePart.Width)
                        goto Finish;
                    part = PrintfDirectivePart.Precision;
                    precision = PrintfPrecisionValue.Specified(0);
                    continue;
                case 'h' or 'j' or 'l' or 'L' or 't' or 'z':
                    var bit = specifier switch
                    {
                        'h' => 1, 'j' => 2, 'l' => 4, 'L' => 8, 't' => 16, _ => 32
                    };
                    if ((modifiers & bit) != 0)
                        goto Finish;
                    if (mode == AwkCompatibilityMode.Posix)
                        throw new AwkRuntimeException(AwkMessages.FormatPrintfPosixModifier(specifier));
                    modifiers |= bit;
                    continue;
                default:
                    goto Finish;
            }
            if (part != PrintfDirectivePart.Width)
                goto Finish;
        }
        // Preserve the entire unfinished directive rather than parsing its tail again.
        specifier = '\0';
        Finish:
        return new(flags, width, precision, PrintfConversionFacts.Parse(specifier), position, cursor - 1);
    }

    private static PrintfArgumentPosition ReadControlPosition(string format, ref int cursor, AwkCompatibilityMode mode)
    {
        if (mode != AwkCompatibilityMode.GnuDefault || cursor >= format.Length || !char.IsAsciiDigit(format[cursor]))
            return default;
        var value = ReadDigits(format, ref cursor);
        if (cursor >= format.Length || format[cursor] != '$')
            throw new AwkRuntimeException(AwkMessages.ErrorPrintfPositionSuffix);
        cursor++;
        return new(true, value);
    }

    private static PrintfWidthValue ResolveWidth(double value, ref PrintfFlags flags)
    {
        if (double.IsNaN(value))
            value = 0;
        if (value is > int.MaxValue or <= int.MinValue)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        var resolved = (int)value;
        if (resolved < 0)
        {
            flags = flags.WithLeftJustify();
            resolved = -resolved;
        }
        return new(resolved);
    }

    private static PrintfPrecisionValue ResolvePrecision(double value)
    {
        if (double.IsNaN(value) || value < 0)
            return PrintfPrecisionValue.None;
        if (value > int.MaxValue)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        return PrintfPrecisionValue.Specified((int)value);
    }

    private static int ReadDigits(string format, ref int cursor)
    {
        var value = 0;
        while (cursor < format.Length && char.IsAsciiDigit(format[cursor]))
        {
            var digit = format[cursor++] - '0';
            if (value > (int.MaxValue - digit) / 10)
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            value = value * 10 + digit;
        }
        return value;
    }
}

internal readonly record struct PrintfArgumentPosition(bool IsSpecified, int OneBasedIndex);

internal enum PrintfDirectivePart
{
    Width,
    Precision,
    Complete
}
