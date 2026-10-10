using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkTokenNavigation
{
    public static bool IsSignificant(AwkTokenKind kind) =>
        kind is not AwkTokenKind.NewLine and not AwkTokenKind.Semicolon and not AwkTokenKind.Comment;

    public static int SkipNonSignificant(
        IReadOnlyList<AwkToken> tokens,
        int startIndex,
        int limit,
        Action? checkpoint = null)
    {
        tokens = ForIndexedAccess(tokens, checkpoint);
        var index = startIndex;
        while (index < limit && !IsSignificant(tokens[index].Kind))
        {
            checkpoint?.Invoke();
            index++;
        }

        return index;
    }

    public static AwkToken? FindFirstSignificant(IReadOnlyList<AwkToken> tokens, Action? checkpoint = null)
    {
        if (tokens is CompositeTokenList composite)
            return FindInComposite(composite, checkpoint);

        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
            var token = tokens[index];
            if (IsSignificant(token.Kind))
                return token;
        }

        return null;

        static AwkToken? FindInComposite(CompositeTokenList composite, Action? checkpoint)
        {
            for (var index = 0; index < composite.Groups.Count; index++)
            {
                checkpoint?.Invoke();
                var token = FindFirstSignificant(composite.Groups[index], checkpoint);
                if (token != null)
                    return token;
            }

            return null;
        }
    }

    public static AwkToken? FindLastSignificant(IReadOnlyList<AwkToken> tokens, Action? checkpoint = null)
    {
        if (tokens is CompositeTokenList composite)
            return FindInComposite(composite, checkpoint);

        for (var index = tokens.Count - 1; index >= 0; index--)
        {
            checkpoint?.Invoke();
            var token = tokens[index];
            if (IsSignificant(token.Kind))
                return token;
        }

        return null;

        static AwkToken? FindInComposite(CompositeTokenList composite, Action? checkpoint)
        {
            for (var index = composite.Groups.Count - 1; index >= 0; index--)
            {
                checkpoint?.Invoke();
                var token = FindLastSignificant(composite.Groups[index], checkpoint);
                if (token != null)
                    return token;
            }

            return null;
        }
    }

    public static AwkToken? FindNextSignificant(IReadOnlyList<AwkToken> tokens, int startIndex, Action? checkpoint = null)
    {
        var index = 0;
        foreach (var token in Enumerate(tokens, checkpoint))
        {
            checkpoint?.Invoke();
            if (index >= startIndex && IsSignificant(token.Kind))
                return token;

            index++;
        }

        return null;
    }

    public static int FindTopLevel(
        IReadOnlyList<AwkToken> tokens,
        AwkTokenKind kind,
        int startIndex,
        int limit,
        Action? checkpoint = null)
    {
        tokens = ForIndexedAccess(tokens, checkpoint);
        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        for (var index = startIndex; index < limit; index++)
        {
            checkpoint?.Invoke();
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
        AwkTokenKind separatorKind,
        Action? checkpoint = null)
    {
        var result = new List<IReadOnlyList<AwkToken>>();
        var start = 0;
        tokens = ForIndexedAccess(tokens, checkpoint);
        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
            if (tokens[index].Kind == separatorKind &&
                parenDepth == 0 &&
                bracketDepth == 0 &&
                braceDepth == 0)
            {
                result.Add(Slice(tokens, start, index - start, checkpoint));
                start = index + 1;
                continue;
            }

            UpdateDepth(tokens[index].Kind, ref parenDepth, ref bracketDepth, ref braceDepth);
        }

        result.Add(Slice(tokens, start, tokens.Count - start, checkpoint));
        return result;
    }

    public static IReadOnlyList<AwkToken> Slice(IReadOnlyList<AwkToken> tokens, int startIndex, int count, Action? checkpoint = null)
    {
        if (count <= 0 || startIndex >= tokens.Count)
            return [];

        tokens = ForIndexedAccess(tokens, checkpoint);
        var safeStart = System.Math.Max(0, startIndex);
        var safeCount = System.Math.Min(count, tokens.Count - safeStart);
        if (safeCount <= 0)
            return [];

        var result = new AwkToken[safeCount];
        for (var index = 0; index < safeCount; index++)
        {
            checkpoint?.Invoke();
            result[index] = tokens[safeStart + index];
        }

        return result;
    }

    public static IReadOnlyList<AwkToken> Flatten(IEnumerable<IReadOnlyList<AwkToken>> groups, Action? checkpoint = null)
    {
        List<IReadOnlyList<AwkToken>>? nonEmptyGroups = null;
        foreach (var group in groups)
        {
            checkpoint?.Invoke();
            if (group.Count == 0)
                continue;

            nonEmptyGroups ??= [];
            nonEmptyGroups.Add(group);
        }

        return nonEmptyGroups?.Count switch
        {
            null or 0 => [],
            1 => nonEmptyGroups[0],
            _ => new CompositeTokenList(nonEmptyGroups, checkpoint)
        };
    }

    public static IReadOnlyList<AwkToken> ForIndexedAccess(
        IReadOnlyList<AwkToken> tokens, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        return tokens is CompositeTokenList composite ? composite.Materialize(checkpoint) : tokens;
    }

    public static IEnumerable<AwkToken> Enumerate(
        IReadOnlyList<AwkToken> tokens, Action? checkpoint = null)
    {
        return tokens is CompositeTokenList composite
            ? composite.Enumerate(checkpoint)
            : EnumerateFlat(tokens, checkpoint);

        static IEnumerable<AwkToken> EnumerateFlat(IReadOnlyList<AwkToken> tokens, Action? checkpoint)
        {
            for (var index = 0; index < tokens.Count; index++)
            {
                checkpoint?.Invoke();
                yield return tokens[index];
            }
        }
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

        public CompositeTokenList(List<IReadOnlyList<AwkToken>> groups, Action? checkpoint)
        {
            _groups = groups.ToArray();
            for (var index = 0; index < _groups.Length; index++)
            {
                checkpoint?.Invoke();
                Count = checked(Count + _groups[index].Count);
            }
        }

        public int Count { get; }

        public IReadOnlyList<IReadOnlyList<AwkToken>> Groups => _groups;

        public AwkToken this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return Materialize(null)[index];
            }
        }

        public AwkToken[] Materialize(Action? checkpoint)
        {
            checkpoint?.Invoke();
            var indexedTokens = Volatile.Read(ref _indexedTokens);
            if (indexedTokens != null)
                return indexedTokens;

            // Interrupted construction never publishes a partial snapshot.
            // The callback belongs to this operation, not to the shared list.
            var materialized = new AwkToken[Count];
            var materializedIndex = 0;
            foreach (var token in Enumerate(checkpoint))
                materialized[materializedIndex++] = token;

            checkpoint?.Invoke();
            return Interlocked.CompareExchange(ref _indexedTokens, materialized, null) ?? materialized;
        }

        public IEnumerator<AwkToken> GetEnumerator() => Enumerate(null).GetEnumerator();

        public IEnumerable<AwkToken> Enumerate(Action? checkpoint)
        {
            checkpoint?.Invoke();
            var pending = new Stack<IReadOnlyList<AwkToken>>();
            for (var index = _groups.Length - 1; index >= 0; index--)
            {
                checkpoint?.Invoke();
                pending.Push(_groups[index]);
            }

            while (pending.Count > 0)
            {
                checkpoint?.Invoke();
                var group = pending.Pop();
                if (group is CompositeTokenList composite)
                {
                    for (var index = composite._groups.Length - 1; index >= 0; index--)
                    {
                        checkpoint?.Invoke();
                        pending.Push(composite._groups[index]);
                    }
                    continue;
                }

                for (var index = 0; index < group.Count; index++)
                {
                    checkpoint?.Invoke();
                    yield return group[index];
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
