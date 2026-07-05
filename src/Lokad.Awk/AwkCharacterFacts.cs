namespace Lokad.Awk;

internal static class AwkCharacterFacts
{
    public static bool IsDecimalDigit(char value) => value is >= '0' and <= '9';

    public static bool IsOctalDigit(char value) => value is >= '0' and <= '7';

    public static bool IsHexDigit(char value) =>
        value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

    public static int HexValue(char value)
    {
        if (value is >= '0' and <= '9')
            return value - '0';

        if (value is >= 'a' and <= 'f')
            return value - 'a' + 10;

        return value - 'A' + 10;
    }

    public static bool AreOctalDigits(string text, int start, int end)
    {
        for (var index = start; index < end; index++)
        {
            if (!IsOctalDigit(text[index]))
                return false;
        }

        return true;
    }
}
