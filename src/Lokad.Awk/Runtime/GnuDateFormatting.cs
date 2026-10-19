using System;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal static class GnuDateFormatting
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Format(DateTimeOffset value, string format,
        AwkMemoryPeakAdmission admission = default, Action? checkpoint = null)
    {
        var builder = new StringBuilder();
        var admittedBytes = 0L;
        PreflightAppend(0);

        void PreflightAppend(int characters, int temporaryCharacters = 64)
        {
            checkpoint?.Invoke();
            var length = AwkRuntimeMemory.CheckedAdd(builder.Length, characters);
            var capacity = builder.Capacity;
            var grows = length > capacity;
            if (grows)
                capacity = (int)Math.Min(int.MaxValue / sizeof(char), Math.Max(length, (long)capacity * 2));
            // UTF-16 builder growth, directive/padding scratch, final string and directive text.
            // The caller retains this existing peak lease through UTF-8 result construction.
            if (Math.Max(Math.Max(length, capacity), temporaryCharacters) > Array.MaxLength / sizeof(char))
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            var ownedCharacters = 2L * capacity + 2L * temporaryCharacters + format.Length;
            if (grows) ownedCharacters += builder.Capacity;
            var bytes = AwkRuntimeMemory.CheckedAdd(192, AwkRuntimeMemory.CheckedMultiply(ownedCharacters, sizeof(char)));
            if (bytes > admittedBytes)
            {
                admission.Resize(bytes);
                admittedBytes = bytes;
            }
            if (grows) builder.EnsureCapacity(capacity);
        }

        for (var index = 0; index < format.Length; index++)
        {
            checkpoint?.Invoke();
            var ch = format[index];
            if (ch != '%')
            {
                PreflightAppend(1);
                builder.Append(ch);
                continue;
            }

            if (index + 1 >= format.Length)
            {
                PreflightAppend(1);
                builder.Append('%');
                continue;
            }

            index++;
            var directiveStart = index;
            if (!TryReadDirective(format, directiveStart, ref index, checkpoint, out var directive))
            {
                PreflightAppend(format.Length - directiveStart + 1);
                builder.Append('%');
                builder.Append(format.AsSpan(directiveStart));
                break;
            }

            PreflightAppend(0, Math.Max(64, directive.Padding == GnuDatePadding.None ? 0 : directive.Width.Value));
            var text = ApplyCaseConversion(
                FormatDirective(value, directive),
                directive.CaseConversion);
            text = ApplyPadding(text, directive.Padding, directive.Width);
            PreflightAppend(text.Length, text.Length);
            builder.Append(text);
        }

        checkpoint?.Invoke();
        return builder.ToString();

        static bool TryReadDirective(
            string format,
            int directiveStart,
            ref int index,
            Action? checkpoint,
            out GnuDateDirective directive)
        {
            var padding = GnuDatePadding.Default;
            var caseConversion = GnuDateCaseConversion.None;

            while (index < format.Length)
            {
                checkpoint?.Invoke();
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
                checkpoint?.Invoke();
                colonCount = AwkRuntimeMemory.CheckedAdd(colonCount, 1);
                index++;
            }

            var width = 0;
            while (index < format.Length && char.IsDigit(format[index]))
            {
                checkpoint?.Invoke();
                var digit = format[index] - '0';
                if (width > int.MaxValue / 10 ||
                    (width == int.MaxValue / 10 && digit > int.MaxValue % 10))
                    throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

                width = width * 10 + digit;
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

        static string FormatDirective(
            DateTimeOffset value,
            GnuDateDirective directive)
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
                'Z' => value.Offset == TimeSpan.Zero ? "UTC" : FormatOffsetWithColonCount(value.Offset, 1),
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
                'U' => GetWeekNumber(value, DayOfWeek.Sunday).ToString(Culture).PadLeft(2, '0'),
                'V' => ISOWeek.GetWeekOfYear(value.DateTime).ToString(Culture).PadLeft(2, '0'),
                'w' => ((int)value.DayOfWeek).ToString(Culture),
                'W' => GetWeekNumber(value, DayOfWeek.Monday).ToString(Culture).PadLeft(2, '0'),
                'x' => Format(value, "%m/%d/%y"),
                'y' => value.ToString("yy", Culture),
                'Y' => value.ToString("yyyy", Culture),
                _ => throw new GnuDateFormatException(directive.Text)
            };

            static string FormatNanoseconds(DateTimeOffset value)
            {
                var ticksWithinSecond = value.Ticks % TimeSpan.TicksPerSecond;
                var nanoseconds = ticksWithinSecond * 100;
                return nanoseconds.ToString(Culture).PadLeft(9, '0');
            }

            static string FormatOffsetWithColonCount(TimeSpan offset, int colonCount) =>
                FormatOffset(offset, new GnuDateColonCount(colonCount));

            static int GetWeekNumber(DateTimeOffset value, DayOfWeek firstWeekDay)
            {
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

        static string ApplyCaseConversion(string input, GnuDateCaseConversion caseConversion)
        {
            return caseConversion switch
            {
                GnuDateCaseConversion.Uppercase => input.ToUpperInvariant(),
                GnuDateCaseConversion.SwapCase => SwapCase(input),
                _ => input
            };
            static string SwapCase(string input)
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
        }
        ;

        static string ApplyPadding(string input, GnuDatePadding padding, GnuDateWidth width)
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

            static string ReplaceLeading(string input, char from, char to)
            {
                var chars = input.ToCharArray();
                for (var index = 0; index < chars.Length && chars[index] == from; index++)
                {
                    chars[index] = to;
                }

                return new string(chars);
            }
        }
    }

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
