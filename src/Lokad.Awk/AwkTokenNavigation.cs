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
        List<IReadOnlyList<AwkToken>>? nonEmptyGroups = null;
        foreach (var group in groups)
        {
            if (group.Count == 0)
                continue;

            nonEmptyGroups ??= [];
            nonEmptyGroups.Add(group);
        }

        return nonEmptyGroups?.Count switch
        {
            null or 0 => [],
            1 => nonEmptyGroups[0],
            _ => new CompositeTokenList(nonEmptyGroups)
        };
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

    private sealed class CompositeTokenList : IReadOnlyList<AwkToken>
    {
        private readonly IReadOnlyList<AwkToken>[] _groups;
        private AwkToken[]? _indexedTokens;

        public CompositeTokenList(List<IReadOnlyList<AwkToken>> groups)
        {
            _groups = groups.ToArray();
            for (var index = 0; index < _groups.Length; index++)
                Count = checked(Count + _groups[index].Count);
        }

        public int Count { get; }

        public AwkToken this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var indexedTokens = Volatile.Read(ref _indexedTokens);
                if (indexedTokens == null)
                {
                    var materialized = new AwkToken[Count];
                    var materializedIndex = 0;
                    foreach (var token in this)
                        materialized[materializedIndex++] = token;

                    indexedTokens = Interlocked.CompareExchange(
                        ref _indexedTokens,
                        materialized,
                        comparand: null) ?? materialized;
                }

                return indexedTokens[index];
            }
        }

        public IEnumerator<AwkToken> GetEnumerator()
        {
            var pending = new Stack<IReadOnlyList<AwkToken>>();
            for (var index = _groups.Length - 1; index >= 0; index--)
                pending.Push(_groups[index]);

            while (pending.Count > 0)
            {
                var group = pending.Pop();
                if (group is CompositeTokenList composite)
                {
                    for (var index = composite._groups.Length - 1; index >= 0; index--)
                        pending.Push(composite._groups[index]);
                    continue;
                }

                for (var index = 0; index < group.Count; index++)
                    yield return group[index];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
