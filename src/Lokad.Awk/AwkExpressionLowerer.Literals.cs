using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    public static bool TryReadNumericLiteralToken(
        string text,
        ref int index,
        [NotNullWhen(true)] out string? numberText,
        out double numberValue)
    {
        numberText = null;
        numberValue = 0;
        if (index >= text.Length ||
            !AwkCharacterFacts.IsDecimalDigit(text[index]) &&
            (text[index] != '.' || index + 1 >= text.Length || !AwkCharacterFacts.IsDecimalDigit(text[index + 1])))
            return false;

        var start = index;
        if (text[index] == '.')
        {
            index++;
            while (index < text.Length && AwkCharacterFacts.IsDecimalDigit(text[index]))
                index++;

            ParseExponentIfPresent(text, ref index);
            numberText = text.Substring(start, index - start);
            return TryParseNumericLiteral(numberText, out numberValue);
        }

        if (index + 2 < text.Length &&
            text[index] == '0' &&
            text[index + 1] is 'x' or 'X' &&
            AwkCharacterFacts.IsHexDigit(text[index + 2]))
        {
            index += 3;
            while (index < text.Length && AwkCharacterFacts.IsHexDigit(text[index]))
                index++;

            numberText = text.Substring(start, index - start);
            return AwkValue.TryParseNumber(
                numberText,
                AwkNumberParsingMode.NonDecimalData,
                AwkNumberTextExtent.EntireText,
                out numberValue);
        }

        while (index < text.Length && AwkCharacterFacts.IsDecimalDigit(text[index]))
            index++;

        if (index < text.Length && text[index] == '.')
        {
            index++;
            while (index < text.Length && AwkCharacterFacts.IsDecimalDigit(text[index]))
                index++;
        }

        ParseExponentIfPresent(text, ref index);

        numberText = text.Substring(start, index - start);
        return TryParseNumericLiteral(numberText, out numberValue);

        static void ParseExponentIfPresent(string text, ref int index)
        {
            if (index >= text.Length || text[index] is not ('e' or 'E'))
                return;

            var exponentStart = index;
            index++;
            if (index < text.Length && text[index] is '+' or '-')
                index++;

            var digitStart = index;
            while (index < text.Length && AwkCharacterFacts.IsDecimalDigit(text[index]))
                index++;

            if (digitStart == index)
                index = exponentStart;
        }

        static bool TryParseNumericLiteral(string numberText, out double numberValue)
        {
            static string? NormalizeTrailingDotExponentLiteral(string numberText)
            {
                for (var index = 1; index + 1 < numberText.Length; index++)
                {
                    if (numberText[index] == '.' && numberText[index + 1] is ('e' or 'E'))
                        return numberText[..(index + 1)] + "0" + numberText[(index + 1)..];
                }

                return null;
            }

            if (AwkValue.TryParseNumber(
                    numberText,
                    AwkNumberParsingMode.NonDecimalData,
                    AwkNumberTextExtent.EntireText,
                    out numberValue))
            {
                return true;
            }

            var normalized = NormalizeTrailingDotExponentLiteral(numberText);
            return normalized != null &&
                AwkValue.TryParseNumber(
                    normalized,
                    AwkNumberParsingMode.Decimal,
                    AwkNumberTextExtent.EntireText,
                    out numberValue);
        }
    }

    public static bool TryReadStringLiteralToken(string source, ref int position, out string literal,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
    {
        literal = string.Empty;
        if (position >= source.Length || source[position] != '"')
            return false;

        position++;
        var start = position;
        var escaped = false;
        while (position < source.Length)
        {
            var current = source[position];
            if (escaped)
            {
                escaped = false;
                position++;
                continue;
            }

            if (current == '\\')
            {
                escaped = true;
                position++;
                continue;
            }

            if (current == '"')
            {
                literal = AwkEscapes.DecodeSourceString(source.Substring(start, position - start), compatibilityMode);
                position++;
                return true;
            }

            position++;
        }

        return false;
    }
}
