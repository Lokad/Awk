using System.Text;

namespace Lokad.Awk;

internal static class AwkEscapes
{
    public static string DecodeSourceString(string text, AwkCompatibilityMode compatibilityMode,
        Action? checkpoint = null)
    {
        var slash = text.IndexOf('\\', StringComparison.Ordinal);
        if (slash < 0)
            return text;

        var builder = new StringBuilder(text.Length);
        builder.Append(text, 0, slash);
        Span<char> scalarBuffer = stackalloc char[2];
        var nextCheckpoint = slash;

        for (var index = slash; index < text.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = index + 1024;
            }
            var current = text[index];
            if (current != '\\' || index + 1 >= text.Length)
            {
                builder.Append(current);
                continue;
            }

            index++;
            var escaped = text[index];
            if (ReadUnicodeEscape(text, ref index, text.Length, compatibilityMode, out var scalar) != AwkUnicodeEscapeStatus.None)
            {
                var length = scalar.EncodeToUtf16(scalarBuffer);
                builder.Append(scalarBuffer[..length]);
                continue;
            }

            if (!(compatibilityMode == AwkCompatibilityMode.Posix && escaped == 'x') &&
                TryReadNumericEscape(text, ref index, text.Length, out var numeric))
            {
                builder.Append(numeric);
                continue;
            }

            if (AwkLineContinuations.TrySkipAfterBackslash(text, ref index, text.Length))
                continue;

            builder.Append(TryDecodeSimpleEscape(escaped, out var decoded) ? decoded : escaped);
        }

        return builder.ToString();
    }

    public static void ReportUnicodeWarnings(string text, AwkCompatibilityMode mode, Action<string> report,
        Action? checkpoint = null)
    {
        if (mode == AwkCompatibilityMode.Posix)
            return;
        var nextCheckpoint = 0;
        for (var index = 0; index + 1 < text.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = index + 1024;
            }
            if (text[index] != '\\')
                continue;
            index++;
            var status = ReadUnicodeEscape(text, ref index, text.Length, mode, out _);
            if (status == AwkUnicodeEscapeStatus.MissingDigits)
                report(AwkMessages.FormatStringUnicodeMissingDigitsWarning());
            else if (status == AwkUnicodeEscapeStatus.InvalidScalar)
                report(AwkMessages.FormatStringUnicodeInvalidScalarWarning());
        }
    }

    public static AwkUnicodeEscapeStatus ReadUnicodeEscape(
        ReadOnlySpan<char> text,
        ref int index,
        int endExclusive,
        AwkCompatibilityMode compatibilityMode,
        out Rune value)
    {
        value = default;
        if (index >= endExclusive || text[index] != 'u' || compatibilityMode == AwkCompatibilityMode.Posix)
            return AwkUnicodeEscapeStatus.None;

        if (index + 1 >= endExclusive || !AwkCharacterFacts.IsHexDigit(text[index + 1]))
        {
            value = new Rune('u');
            return AwkUnicodeEscapeStatus.MissingDigits;
        }

        uint numeric = 0;
        var digits = 0;
        while (digits < 8 && index + 1 < endExclusive && AwkCharacterFacts.IsHexDigit(text[index + 1]))
        {
            index++;
            digits++;
            numeric = (numeric << 4) | (uint)AwkCharacterFacts.HexValue(text[index]);
        }

        if (numeric <= 0x10FFFF && Rune.TryCreate((int)numeric, out value))
            return AwkUnicodeEscapeStatus.Valid;

        value = new Rune('?');
        return AwkUnicodeEscapeStatus.InvalidScalar;
    }

    public static bool TryReadNumericEscape(
        ReadOnlySpan<char> text,
        ref int index,
        int endExclusive,
        out char value)
    {
        value = default;
        if (index >= endExclusive)
            return false;

        var escaped = text[index];
        if (AwkCharacterFacts.IsOctalDigit(escaped))
        {
            var numeric = escaped - '0';
            var digits = 1;
            while (digits < 3 && index + 1 < endExclusive && AwkCharacterFacts.IsOctalDigit(text[index + 1]))
            {
                index++;
                digits++;
                numeric = (numeric * 8) + (text[index] - '0');
            }

            value = (char)numeric;
            return true;
        }

        if (escaped == 'x' && index + 1 < endExclusive && AwkCharacterFacts.IsHexDigit(text[index + 1]))
        {
            var numeric = 0;
            var digits = 0;
            while (digits < 2 && index + 1 < endExclusive && AwkCharacterFacts.IsHexDigit(text[index + 1]))
            {
                index++;
                digits++;
                numeric = (numeric * 16) + AwkCharacterFacts.HexValue(text[index]);
            }

            value = (char)numeric;
            return true;
        }

        return false;
    }

    public static bool TryDecodeSimpleEscape(char escaped, out char decoded)
    {
        decoded = escaped switch
        {
            '\\' => '\\',
            '"' => '"',
            'a' => '\a',
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'v' => '\v',
            _ => default
        };

        return decoded != default;
    }

}

internal enum AwkUnicodeEscapeStatus
{
    None,
    Valid,
    MissingDigits,
    InvalidScalar
}
