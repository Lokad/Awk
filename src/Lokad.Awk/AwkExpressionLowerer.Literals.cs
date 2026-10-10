using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    public static bool TryReadNumericLiteralToken(
        string text,
        ref int index,
        [NotNullWhen(true)] out string? numberText,
        out double numberValue,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
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
            {
                CheckLiteralProgress(index, checkpoint);
                index++;
            }

            ParseExponentIfPresent(text, ref index, checkpoint);
            checkpoint?.Invoke();
            numberText = text.Substring(start, index - start);
            return TryParseNumericLiteral(numberText, out numberValue, checkpoint);
        }

        if (index + 2 < text.Length &&
            text[index] == '0' &&
            text[index + 1] is 'x' or 'X' &&
            AwkCharacterFacts.IsHexDigit(text[index + 2]))
        {
            index += 3;
            while (index < text.Length && AwkCharacterFacts.IsHexDigit(text[index]))
            {
                CheckLiteralProgress(index, checkpoint);
                index++;
            }

            checkpoint?.Invoke();
            numberText = text.Substring(start, index - start);
            return AwkValue.TryParseNumber(
                numberText,
                AwkNumberParsingMode.NonDecimalData,
                AwkNumberTextExtent.EntireText,
                out numberValue, checkpoint);
        }

        while (index < text.Length && AwkCharacterFacts.IsDecimalDigit(text[index]))
        {
            CheckLiteralProgress(index, checkpoint);
            index++;
        }

        if (index < text.Length && text[index] == '.')
        {
            index++;
            while (index < text.Length && AwkCharacterFacts.IsDecimalDigit(text[index]))
            {
                CheckLiteralProgress(index, checkpoint);
                index++;
            }
        }

        ParseExponentIfPresent(text, ref index, checkpoint);

        checkpoint?.Invoke();
        numberText = text.Substring(start, index - start);
        return TryParseNumericLiteral(numberText, out numberValue, checkpoint);

        static void ParseExponentIfPresent(string text, ref int index, Action? checkpoint)
        {
            if (index >= text.Length || text[index] is not ('e' or 'E'))
                return;

            var exponentStart = index;
            index++;
            if (index < text.Length && text[index] is '+' or '-')
                index++;

            var digitStart = index;
            while (index < text.Length && AwkCharacterFacts.IsDecimalDigit(text[index]))
            {
                CheckLiteralProgress(index, checkpoint);
                index++;
            }

            if (digitStart == index)
                index = exponentStart;
        }

        static bool TryParseNumericLiteral(string numberText, out double numberValue, Action? checkpoint)
        {
            static string? NormalizeTrailingDotExponentLiteral(string numberText, Action? checkpoint)
            {
                for (var index = 1; index + 1 < numberText.Length; index++)
                {
                    CheckLiteralProgress(index, checkpoint);
                    if (numberText[index] == '.' && numberText[index + 1] is ('e' or 'E'))
                        return numberText[..(index + 1)] + "0" + numberText[(index + 1)..];
                }

                return null;
            }

            if (AwkValue.TryParseNumber(
                    numberText,
                    AwkNumberParsingMode.NonDecimalData,
                    AwkNumberTextExtent.EntireText,
                    out numberValue, checkpoint))
            {
                return true;
            }

            var normalized = NormalizeTrailingDotExponentLiteral(numberText, checkpoint);
            return normalized != null &&
                AwkValue.TryParseNumber(
                    normalized,
                    AwkNumberParsingMode.Decimal,
                    AwkNumberTextExtent.EntireText,
                    out numberValue, checkpoint);
        }
    }

    public static bool TryReadStringLiteralToken(string source, ref int position, out string literal,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        literal = string.Empty;
        if (position >= source.Length || source[position] != '"')
            return false;

        position++;
        var start = position;
        var escaped = false;
        long nextCheckpoint = position;
        while (position < source.Length)
        {
            if (position >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)position + 1024;
            }
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
                checkpoint?.Invoke();
                literal = AwkEscapes.DecodeSourceString(source.Substring(start, position - start), compatibilityMode, checkpoint);
                checkpoint?.Invoke();
                position++;
                return true;
            }

            position++;
        }

        return false;
    }

    private static void CheckLiteralProgress(int position, Action? checkpoint)
    {
        if ((position & 1023) == 0)
            checkpoint?.Invoke();
    }
}
