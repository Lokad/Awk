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
        var bracket = BracketPosition.Outside;
        var bracketTerminator = '\0';
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
                if (bracket != BracketPosition.Outside) bracket = BracketPosition.Contents;
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

            if (current is '\r' or '\n')
                return new AwkRegexLiteralScanResult(AwkRegexLiteralTermination.NewLine, index);

            if (bracket != BracketPosition.Outside)
            {
                if (bracketTerminator != '\0')
                {
                    if (current == bracketTerminator && index + 1 < text.Length && text[index + 1] == ']')
                    {
                        bracketTerminator = '\0';
                        index++;
                    }
                    continue;
                }
                if (current == '^' && bracket == BracketPosition.First)
                {
                    bracket = BracketPosition.AfterNegation;
                    continue;
                }
                if (current == ']' && bracket == BracketPosition.Contents)
                {
                    bracket = BracketPosition.Outside;
                    continue;
                }
                // A leading ] and an ordinary [ are class members. Only the
                // POSIX [: :], [. .] and [= =] terms have an inner closing ].
                bracket = BracketPosition.Contents;
                if (current == '[' && index + 1 < text.Length && text[index + 1] is ':' or '.' or '=')
                    bracketTerminator = text[++index];
                continue;
            }

            if (current == '[')
            {
                bracket = BracketPosition.First;
                continue;
            }

            if (current == '/')
                return new AwkRegexLiteralScanResult(AwkRegexLiteralTermination.Delimiter, index);
        }

        return new AwkRegexLiteralScanResult(AwkRegexLiteralTermination.EndOfText, text.Length);
    }

    private enum BracketPosition { Outside, First, AfterNegation, Contents }
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
