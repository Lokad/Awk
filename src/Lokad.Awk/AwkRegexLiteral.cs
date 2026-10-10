namespace Lokad.Awk;

internal static class AwkRegexLiteral
{
    public static bool TryReadPattern(string text, out string pattern, Action? checkpoint = null)
    {
        pattern = string.Empty;
        if (text.Length < 2 || text[0] != '/')
            return false;

        var scan = Scan(text, patternStart: 1, checkpoint);
        if (scan.Termination != AwkRegexLiteralTermination.Delimiter ||
            scan.EndIndex != text.Length - 1)
        {
            return false;
        }

        checkpoint?.Invoke();
        pattern = text.Substring(1, scan.EndIndex - 1);
        return true;
    }

    public static AwkRegexLiteralScanResult Scan(string text, int patternStart, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        ArgumentOutOfRangeException.ThrowIfNegative(patternStart);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(patternStart, text.Length);

        var escaped = false;
        var inBracketExpression = false;
        long nextCheckpoint = patternStart;
        for (var index = patternStart; index < text.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)index + 1024;
            }
            var current = text[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                var escapedIndex = index + 1;
                if (AwkLineContinuations.TrySkipAfterBackslash(text, ref escapedIndex, text.Length))
                {
                    index = escapedIndex;
                    continue;
                }
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
                return new AwkRegexLiteralScanResult(AwkRegexLiteralTermination.Delimiter, index);

            if (current is '\r' or '\n')
                return new AwkRegexLiteralScanResult(AwkRegexLiteralTermination.NewLine, index);
        }

        return new AwkRegexLiteralScanResult(AwkRegexLiteralTermination.EndOfText, text.Length);
    }
}

internal readonly record struct AwkRegexLiteralScanResult(
    AwkRegexLiteralTermination Termination,
    int EndIndex);

internal enum AwkRegexLiteralTermination
{
    Delimiter,
    NewLine,
    EndOfText
}
