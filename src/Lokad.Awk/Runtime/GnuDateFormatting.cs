using System;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal static class GnuDateFormatting
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Format(DateTimeOffset value, string format)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < format.Length; index++)
        {
            var ch = format[index];
            if (ch != '%')
            {
                builder.Append(ch);
                continue;
            }

            if (index + 1 >= format.Length)
            {
                builder.Append('%');
                continue;
            }

            index++;
            var directiveStart = index;
            if (!TryReadDirective(format, directiveStart, ref index, out var directive))
            {
                builder.Append('%');
                builder.Append(format.AsSpan(directiveStart));
                break;
            }

            var text = ApplyCaseConversion(FormatDirective(value, directive), directive.CaseConversion);
            text = ApplyPadding(text, directive.Padding, directive.Width);
            builder.Append(text);
        }

        return builder.ToString();
    }

    private static bool TryReadDirective(
        string format,
        int directiveStart,
        ref int index,
        out GnuDateDirective directive)
    {
        var padding = GnuDatePadding.Default;
        var caseConversion = GnuDateCaseConversion.None;

        while (index < format.Length)
        {
            switch (format[index])
            {
                case '-':
                    padding = GnuDatePadding.None;
                    index++;
                    continue;
                case '_':
                    padding = GnuDatePadding.Space;
                    index++;
                    continue;
                case '0':
                    padding = GnuDatePadding.Zero;
                    index++;
                    continue;
                case '^':
                    caseConversion = GnuDateCaseConversion.Uppercase;
                    index++;
                    continue;
                case '#':
                    caseConversion = GnuDateCaseConversion.SwapCase;
                    index++;
                    continue;
            }

            break;
        }

        var colonCount = 0;
        while (index < format.Length && format[index] == ':')
        {
            colonCount++;
            index++;
        }

        var width = 0;
        while (index < format.Length && char.IsDigit(format[index]))
        {
            width = width * 10 + (format[index] - '0');
            index++;
        }

        if (index < format.Length && format[index] is 'E' or 'O')
            index++;

        if (index >= format.Length)
        {
            directive = default;
            return false;
        }

        directive = new GnuDateDirective(
            format[index],
            new GnuDateColonCount(colonCount),
            new GnuDateWidth(width),
            padding,
            caseConversion,
            format[directiveStart..(index + 1)]);
        return true;
    }

    private static string FormatDirective(DateTimeOffset value, GnuDateDirective directive)
    {
        if (directive.ColonCount.Value > 0 && directive.Specifier != 'z')
        {
            throw new GnuDateFormatException(directive.Text);
        }

        return directive.Specifier switch
        {
            '%' => "%",
            'n' => "\n",
            't' => "\t",
            'H' => value.ToString("HH", Culture),
            'I' => value.ToString("hh", Culture),
            'k' => value.Hour.ToString(Culture).PadLeft(2, ' '),
            'l' => (value.Hour % 12 == 0 ? 12 : value.Hour % 12).ToString(Culture).PadLeft(2, ' '),
            'M' => value.ToString("mm", Culture),
            'N' => FormatNanoseconds(value),
            'p' => value.ToString("tt", Culture),
            'P' => value.ToString("tt", Culture).ToLowerInvariant(),
            'r' => Format(value, "%I:%M:%S %p"),
            'R' => Format(value, "%H:%M"),
            's' => value.ToUnixTimeSeconds().ToString(Culture),
            'S' => value.ToString("ss", Culture),
            'T' => Format(value, "%H:%M:%S"),
            'X' => Format(value, "%H:%M:%S"),
            '+' => Format(value, "%a %b %e %H:%M:%S %Z %Y"),
            'z' => FormatOffset(value.Offset, directive.ColonCount),
            'Z' => value.Offset == TimeSpan.Zero ? "UTC" : FormatOffset(value.Offset, 1),
            'a' => value.ToString("ddd", Culture),
            'A' => value.ToString("dddd", Culture),
            'b' => value.ToString("MMM", Culture),
            'B' => value.ToString("MMMM", Culture),
            'c' => Format(value, "%a %b %e %H:%M:%S %Y"),
            'C' => (value.Year / 100).ToString(Culture).PadLeft(2, '0'),
            'd' => value.ToString("dd", Culture),
            'D' => Format(value, "%m/%d/%y"),
            'e' => value.Day.ToString(Culture).PadLeft(2, ' '),
            'F' => Format(value, "%Y-%m-%d"),
            'g' => (ISOWeek.GetYear(value.DateTime) % 100).ToString(Culture).PadLeft(2, '0'),
            'G' => ISOWeek.GetYear(value.DateTime).ToString(Culture),
            'h' => value.ToString("MMM", Culture),
            'j' => value.DayOfYear.ToString(Culture).PadLeft(3, '0'),
            'm' => value.ToString("MM", Culture),
            'q' => (((value.Month - 1) / 3) + 1).ToString(Culture),
            'u' => (value.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)value.DayOfWeek).ToString(Culture),
            'U' => GetWeekNumber(value, mondayFirst: false).ToString(Culture).PadLeft(2, '0'),
            'V' => ISOWeek.GetWeekOfYear(value.DateTime).ToString(Culture).PadLeft(2, '0'),
            'w' => ((int)value.DayOfWeek).ToString(Culture),
            'W' => GetWeekNumber(value, mondayFirst: true).ToString(Culture).PadLeft(2, '0'),
            'x' => Format(value, "%m/%d/%y"),
            'y' => value.ToString("yy", Culture),
            'Y' => value.ToString("yyyy", Culture),
            _ => throw new GnuDateFormatException(directive.Text)
        };
    }

    private static string FormatNanoseconds(DateTimeOffset value)
    {
        var ticksWithinSecond = value.Ticks % TimeSpan.TicksPerSecond;
        var nanoseconds = ticksWithinSecond * 100;
        return nanoseconds.ToString(Culture).PadLeft(9, '0');
    }

    private static string FormatOffset(TimeSpan offset, int colonCount) =>
        FormatOffset(offset, new GnuDateColonCount(colonCount));

    private static string FormatOffset(TimeSpan offset, GnuDateColonCount colonCount)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absoluteOffset = offset.Duration();
        return colonCount.Value switch
        {
            0 => string.Create(Culture, $"{sign}{absoluteOffset.Hours:00}{absoluteOffset.Minutes:00}"),
            1 => string.Create(Culture, $"{sign}{absoluteOffset.Hours:00}:{absoluteOffset.Minutes:00}"),
            2 => string.Create(Culture, $"{sign}{absoluteOffset.Hours:00}:{absoluteOffset.Minutes:00}:{absoluteOffset.Seconds:00}"),
            _ when absoluteOffset.Seconds != 0 => string.Create(Culture, $"{sign}{absoluteOffset.Hours:00}:{absoluteOffset.Minutes:00}:{absoluteOffset.Seconds:00}"),
            _ when absoluteOffset.Minutes != 0 => string.Create(Culture, $"{sign}{absoluteOffset.Hours:00}:{absoluteOffset.Minutes:00}"),
            _ => string.Create(Culture, $"{sign}{absoluteOffset.Hours:00}")
        };
    }

    private static string ApplyCaseConversion(string input, GnuDateCaseConversion caseConversion) =>
        caseConversion switch
        {
            GnuDateCaseConversion.Uppercase => input.ToUpperInvariant(),
            GnuDateCaseConversion.SwapCase => SwapCase(input),
            _ => input
        };

    private static string ApplyPadding(string input, GnuDatePadding padding, GnuDateWidth width)
    {
        if (padding == GnuDatePadding.None)
        {
            var unpadded = input.TrimStart('0', ' ');
            return unpadded.Length == 0 && input.Length > 0 ? "0" : unpadded;
        }

        if (padding == GnuDatePadding.Space)
        {
            input = ReplaceLeading(input, '0', ' ');
        }
        else if (padding == GnuDatePadding.Zero)
        {
            input = ReplaceLeading(input, ' ', '0');
        }

        if (width.Value > input.Length)
        {
            input = input.PadLeft(width.Value, padding.PaddingCharacter());
        }

        return input;
    }

    private static string ReplaceLeading(string input, char from, char to)
    {
        var chars = input.ToCharArray();
        for (var index = 0; index < chars.Length && chars[index] == from; index++)
        {
            chars[index] = to;
        }

        return new string(chars);
    }

    private static string SwapCase(string input)
    {
        var chars = input.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            var ch = chars[index];
            chars[index] = char.IsUpper(ch)
                ? char.ToLowerInvariant(ch)
                : char.IsLower(ch)
                    ? char.ToUpperInvariant(ch)
                    : ch;
        }

        return new string(chars);
    }

    private static int GetWeekNumber(DateTimeOffset value, bool mondayFirst)
    {
        var firstWeekDay = mondayFirst ? DayOfWeek.Monday : DayOfWeek.Sunday;
        var firstDayOfYear = new DateTime(value.Year, 1, 1).DayOfWeek;
        var firstWeekStart = ((int)firstWeekDay - (int)firstDayOfYear + 7) % 7;
        var dayOfYear = value.DayOfYear - 1;
        if (dayOfYear < firstWeekStart)
        {
            return 0;
        }

        return ((dayOfYear - firstWeekStart) / 7) + 1;
    }
}

internal readonly struct GnuDateDirective(
    char specifier,
    GnuDateColonCount colonCount,
    GnuDateWidth width,
    GnuDatePadding padding,
    GnuDateCaseConversion caseConversion,
    string? text)
{
    public char Specifier { get; } = specifier;

    public GnuDateColonCount ColonCount { get; } = colonCount;

    public GnuDateWidth Width { get; } = width;

    public GnuDatePadding Padding { get; } = padding;

    public GnuDateCaseConversion CaseConversion { get; } = caseConversion;

    public string Text => text ?? string.Empty;
}

internal readonly record struct GnuDateColonCount(int Value);

internal readonly record struct GnuDateWidth(int Value);

internal enum GnuDatePadding
{
    Default,
    None,
    Space,
    Zero
}

internal static class GnuDatePaddingExtensions
{
    public static char PaddingCharacter(this GnuDatePadding padding) =>
        padding switch
        {
            GnuDatePadding.Space => ' ',
            _ => '0'
        };
}

internal enum GnuDateCaseConversion
{
    None,
    Uppercase,
    SwapCase
}

internal sealed class GnuDateFormatException(string directive)
    : Exception($"'%{directive}' is not supported on this system.");
