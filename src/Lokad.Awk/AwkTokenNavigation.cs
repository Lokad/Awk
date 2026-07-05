using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkTokenNavigation
{
    public static bool IsSignificant(AwkTokenKind kind) =>
        kind is not AwkTokenKind.NewLine and not AwkTokenKind.Semicolon and not AwkTokenKind.Comment;

    public static int SkipNonSignificant(
        IReadOnlyList<AwkToken> tokens,
        int startIndex,
        int limit)
    {
        var index = startIndex;
        while (index < limit && !IsSignificant(tokens[index].Kind))
        {
            index++;
        }

        return index;
    }

    public static AwkToken? FindFirstSignificant(IReadOnlyList<AwkToken> tokens)
    {
        for (var index = 0; index < tokens.Count; index++)
        {
            if (IsSignificant(tokens[index].Kind))
                return tokens[index];
        }

        return null;
    }

    public static AwkToken? FindLastSignificant(IReadOnlyList<AwkToken> tokens)
    {
        for (var index = tokens.Count - 1; index >= 0; index--)
        {
            if (IsSignificant(tokens[index].Kind))
                return tokens[index];
        }

        return null;
    }

    public static AwkToken? FindNextSignificant(IReadOnlyList<AwkToken> tokens, int startIndex)
    {
        for (var index = startIndex; index < tokens.Count; index++)
        {
            if (IsSignificant(tokens[index].Kind))
                return tokens[index];
        }

        return null;
    }

    public static int FindTopLevel(
        IReadOnlyList<AwkToken> tokens,
        AwkTokenKind kind,
        int startIndex,
        int limit)
    {
        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        for (var index = startIndex; index < limit; index++)
        {
            if (tokens[index].Kind == kind &&
                parenDepth == 0 &&
                bracketDepth == 0 &&
                braceDepth == 0)
            {
                return index;
            }

            UpdateDepth(tokens[index].Kind, ref parenDepth, ref bracketDepth, ref braceDepth);
        }

        return -1;
    }

    public static IReadOnlyList<IReadOnlyList<AwkToken>> SplitTopLevel(
        IReadOnlyList<AwkToken> tokens,
        AwkTokenKind separatorKind)
    {
        var result = new List<IReadOnlyList<AwkToken>>();
        var start = 0;
        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Kind == separatorKind &&
                parenDepth == 0 &&
                bracketDepth == 0 &&
                braceDepth == 0)
            {
                result.Add(Slice(tokens, start, index - start));
                start = index + 1;
                continue;
            }

            UpdateDepth(tokens[index].Kind, ref parenDepth, ref bracketDepth, ref braceDepth);
        }

        result.Add(Slice(tokens, start, tokens.Count - start));
        return result;
    }

    public static IReadOnlyList<AwkToken> Slice(IReadOnlyList<AwkToken> tokens, int startIndex, int count)
    {
        if (count <= 0 || startIndex >= tokens.Count)
            return [];

        var safeStart = System.Math.Max(0, startIndex);
        var safeCount = System.Math.Min(count, tokens.Count - safeStart);
        if (safeCount <= 0)
            return [];

        var result = new AwkToken[safeCount];
        for (var index = 0; index < safeCount; index++)
            result[index] = tokens[safeStart + index];

        return result;
    }

    public static IReadOnlyList<AwkToken> Flatten(IEnumerable<IReadOnlyList<AwkToken>> groups)
    {
        var tokens = new List<AwkToken>();
        foreach (var group in groups)
            tokens.AddRange(group);

        return tokens;
    }

    public static int FindFirstTopLevel(
        IReadOnlyList<AwkToken> tokens,
        AwkTokenKind firstKind,
        AwkTokenKind secondKind,
        int startIndex,
        int limit) =>
        MinimumNonNegative(
            FindTopLevel(tokens, firstKind, startIndex, limit),
            FindTopLevel(tokens, secondKind, startIndex, limit),
            -1,
            -1);

    public static int MinimumNonNegative(int first, int second, int third, int fourth)
    {
        var minimum = -1;
        Apply(first);
        Apply(second);
        Apply(third);
        Apply(fourth);
        return minimum;

        void Apply(int value)
        {
            if (value < 0)
                return;

            if (minimum < 0 || value < minimum)
                minimum = value;
        }
    }

    public static int ResolveBalancedEnd(IReadOnlyList<AwkToken> tokens, int openIndex)
    {
        var openKind = tokens[openIndex].Kind;
        var closeKind = openKind switch
        {
            AwkTokenKind.LeftBracket => AwkTokenKind.RightBracket,
            AwkTokenKind.LeftParen => AwkTokenKind.RightParen,
            AwkTokenKind.LeftBrace => AwkTokenKind.RightBrace,
            _ => openKind
        };
        var depth = 0;
        for (var index = openIndex; index < tokens.Count; index++)
        {
            if (tokens[index].Kind == openKind)
                depth++;
            else if (tokens[index].Kind == closeKind)
            {
                depth--;
                if (depth == 0)
                    return index + 1;
            }
        }

        return tokens.Count;
    }

    public static void UpdateDepth(
        AwkTokenKind kind,
        ref int currentParenDepth,
        ref int currentBracketDepth,
        ref int currentBraceDepth)
    {
        switch (kind)
        {
            case AwkTokenKind.LeftParen:
                currentParenDepth++;
                break;
            case AwkTokenKind.RightParen:
                if (currentParenDepth > 0)
                    currentParenDepth--;
                break;
            case AwkTokenKind.LeftBracket:
                currentBracketDepth++;
                break;
            case AwkTokenKind.RightBracket:
                if (currentBracketDepth > 0)
                    currentBracketDepth--;
                break;
            case AwkTokenKind.LeftBrace:
                currentBraceDepth++;
                break;
            case AwkTokenKind.RightBrace:
                if (currentBraceDepth > 0)
                    currentBraceDepth--;
                break;
        }
    }

}
