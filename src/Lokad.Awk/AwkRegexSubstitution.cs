namespace Lokad.Awk;

internal static class AwkRegexSubstitution
{
    public static AwkText ReplaceAll(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        AwkText replacement,
        AwkReplacementMode mode,
        int maxReplacements,
        out int replacementCount) =>
        Replace(state, regex, input, replacement, mode, targetOccurrence: 0, maxReplacements, out replacementCount);

    public static AwkText ReplaceOccurrence(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        AwkText replacement,
        AwkReplacementMode mode,
        int occurrence,
        out int replacementCount) =>
        Replace(state, regex, input, replacement, mode, occurrence, maxReplacements: 1, out replacementCount);

    private static AwkText Replace(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        AwkText replacement,
        AwkReplacementMode mode,
        int targetOccurrence,
        int maxReplacements,
        out int replacementCount)
    {
        var spanReplacementKind = replacement.AsSpan().IndexOfAny((byte)'&', (byte)'\\') < 0
            ? SpanReplacementKind.Literal
            : SpanReplacementKind.Substitution;
        if (spanReplacementKind == SpanReplacementKind.Literal ||
            mode == AwkReplacementMode.Substitution)
        {
            if (targetOccurrence == 0 && maxReplacements == 1)
            {
                var matches = new FirstSpanMatchSource(state, regex, input);
                return ReplaceSpans(
                    state,
                    input,
                    replacement,
                    spanReplacementKind,
                    targetOccurrence,
                    maxReplacements,
                    matches,
                    out replacementCount);
            }

            var allMatches = new SpanMatchSource(state.EnumerateRegexMatchSpans(regex, input));
            return ReplaceSpans(
                state,
                input,
                replacement,
                spanReplacementKind,
                targetOccurrence,
                maxReplacements,
                allMatches,
                out replacementCount);
        }

        if (targetOccurrence == 0 && maxReplacements == 1)
        {
            var matches = new FirstValueMatchSource(state, regex, input);
            return ReplaceGensubMatches(
                state,
                input,
                replacement,
                targetOccurrence,
                maxReplacements,
                matches,
                out replacementCount);
        }

        var allValueMatches = new ValueMatchSource(state.EnumerateRegexMatches(regex, input));
        return ReplaceGensubMatches(
            state,
            input,
            replacement,
            targetOccurrence,
            maxReplacements,
            allValueMatches,
            out replacementCount);
    }

    private static AwkText ReplaceSpans<TMatchSource>(
        AwkRuntimeState state,
        AwkText input,
        AwkText replacement,
        SpanReplacementKind replacementKind,
        int targetOccurrence,
        int maxReplacements,
        TMatchSource matches,
        out int replacementCount)
        where TMatchSource : struct, IMatchSource<AwkRegexMatchSpan>
    {
        using var matchSource = matches;
        if (!matchSource.MoveNext(out var match))
        {
            replacementCount = 0;
            return input;
        }

        if (targetOccurrence > 0)
        {
            for (var seen = 1; seen < targetOccurrence; seen++)
            {
                state.ThrowIfCancellationRequested();
                if (!matchSource.MoveNext(out match))
                {
                    replacementCount = 0;
                    return input;
                }
            }

            state.ThrowIfCancellationRequested();
            using var occurrenceBuilder = new AwkUtf8Builder(
                state.Limits,
                state.MemoryGovernor,
                input.ByteLength);
            occurrenceBuilder.Append(input.AsSpan()[..match.ByteIndex]);
            AppendSpanReplacement(replacementKind, input, match, replacement, occurrenceBuilder);
            occurrenceBuilder.Append(input.AsSpan()[(match.ByteIndex + match.ByteLength)..]);
            replacementCount = 1;
            return occurrenceBuilder.CopyToAwkText(state.Counters);
        }

        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
        var position = 0;
        replacementCount = 0;
        do
        {
            state.ThrowIfCancellationRequested();
            builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
            AppendSpanReplacement(replacementKind, input, match, replacement, builder);
            position = match.ByteIndex + match.ByteLength;
            replacementCount++;

            if (maxReplacements > 0 && replacementCount >= maxReplacements)
            {
                break;
            }
        }
        while (matchSource.MoveNext(out match));

        builder.Append(input.AsSpan()[position..]);
        return builder.CopyToAwkText(state.Counters);
    }

    private static void AppendSpanReplacement(
        SpanReplacementKind replacementKind,
        AwkText input,
        AwkRegexMatchSpan match,
        AwkText replacement,
        AwkUtf8Builder builder)
    {
        if (replacementKind == SpanReplacementKind.Literal)
        {
            builder.Append(replacement);
            return;
        }

        AwkReplacement.AppendSubstitution(
            input.AsSpan().Slice(match.ByteIndex, match.ByteLength),
            replacement,
            builder);
    }

    private static AwkText ReplaceGensubMatches<TMatchSource>(
        AwkRuntimeState state,
        AwkText input,
        AwkText replacement,
        int targetOccurrence,
        int maxReplacements,
        TMatchSource matches,
        out int replacementCount)
        where TMatchSource : struct, IMatchSource<AwkRegexMatch>
    {
        using var matchSource = matches;
        if (!matchSource.MoveNext(out var match))
        {
            replacementCount = 0;
            return input;
        }

        if (targetOccurrence > 0)
        {
            for (var seen = 1; seen < targetOccurrence; seen++)
            {
                state.ThrowIfCancellationRequested();
                if (!matchSource.MoveNext(out match))
                {
                    replacementCount = 0;
                    return input;
                }
            }

            state.ThrowIfCancellationRequested();
            using var occurrenceBuilder = new AwkUtf8Builder(
                state.Limits,
                state.MemoryGovernor,
                input.ByteLength);
            occurrenceBuilder.Append(input.AsSpan()[..match.ByteIndex]);
            AwkReplacement.AppendGensub(match, replacement, occurrenceBuilder);
            occurrenceBuilder.Append(input.AsSpan()[(match.ByteIndex + match.ByteLength)..]);
            replacementCount = 1;
            return occurrenceBuilder.CopyToAwkText(state.Counters);
        }

        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
        var position = 0;
        replacementCount = 0;
        do
        {
            state.ThrowIfCancellationRequested();
            builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
            AwkReplacement.AppendGensub(match, replacement, builder);
            position = match.ByteIndex + match.ByteLength;
            replacementCount++;

            if (maxReplacements > 0 && replacementCount >= maxReplacements)
            {
                break;
            }
        }
        while (matchSource.MoveNext(out match));

        builder.Append(input.AsSpan()[position..]);
        return builder.CopyToAwkText(state.Counters);
    }

    private interface IMatchSource<TMatch> : IDisposable
    {
        bool MoveNext(out TMatch match);
    }

    private struct FirstSpanMatchSource(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input) : IMatchSource<AwkRegexMatchSpan>
    {
        private bool _read;

        public bool MoveNext(out AwkRegexMatchSpan match)
        {
            if (_read)
            {
                match = AwkRegexMatchSpan.NoMatch;
                return false;
            }

            _read = true;
            match = state.MatchRegexSpan(regex, input);
            return match.Success;
        }

        public readonly void Dispose()
        {
        }
    }

    private readonly struct SpanMatchSource(
        AwkRegexMatchSpanEnumerator matches) : IMatchSource<AwkRegexMatchSpan>
    {
        public bool MoveNext(out AwkRegexMatchSpan match) => matches.MoveNext(out match);

        public void Dispose() => matches.Dispose();
    }

    private struct FirstValueMatchSource(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input) : IMatchSource<AwkRegexMatch>
    {
        private bool _read;

        public bool MoveNext(out AwkRegexMatch match)
        {
            if (_read)
            {
                match = AwkRegexMatch.NoMatch;
                return false;
            }

            _read = true;
            match = state.MatchRegexValue(regex, input);
            return match.Success;
        }

        public readonly void Dispose()
        {
        }
    }

    private readonly struct ValueMatchSource(
        AwkRegexMatchEnumerator matches) : IMatchSource<AwkRegexMatch>
    {
        public bool MoveNext(out AwkRegexMatch match) => matches.MoveNext(out match);

        public void Dispose() => matches.Dispose();
    }

    private enum SpanReplacementKind
    {
        Literal,
        Substitution
    }
}
