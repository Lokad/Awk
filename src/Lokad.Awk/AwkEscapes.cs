using System.Text;

namespace Lokad.Awk;

internal static class AwkEscapes
{
    public static string Decode(string text)
    {
        var slash = text.IndexOf('\\');
        if (slash < 0)
            return text;

        var builder = new StringBuilder(text.Length);
        builder.Append(text, 0, slash);

        for (var index = slash; index < text.Length; index++)
        {
            var current = text[index];
            if (current != '\\' || index + 1 >= text.Length)
            {
                builder.Append(current);
                continue;
            }

            index++;
            var escaped = text[index];
            if (TryReadNumericEscape(text, ref index, text.Length, out var numeric))
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
