namespace Lokad.Awk;

internal static class AwkRegexLiteral
{
    public static bool TryReadPattern(string text, out string pattern)
    {
        pattern = string.Empty;
        if (text.Length < 2 || text[0] != '/')
            return false;

        var start = 1;
        var escaped = false;
        var inBracketExpression = false;
        for (var index = 1; index < text.Length; index++)
        {
            var current = text[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                escaped = true;
                continue;
            }

            if (current == '[')
            {
                inBracketExpression = true;
                continue;
            }

            if (current == ']' && inBracketExpression)
            {
                inBracketExpression = false;
                continue;
            }

            if (current == '/' && !inBracketExpression)
            {
                if (index != text.Length - 1)
                    return false;

                pattern = text.Substring(start, index - start);
                return true;
            }
        }

        return false;
    }
}
