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
        var spanReplacementKind = IsLiteralReplacement(replacement, state.CancellationCheckpoint)
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

        using var replacementPlan = AwkGensubReplacementPlan.Rent(replacement, state.CancellationCheckpoint, state.MemoryGovernor);
        var capturePlan = replacementPlan.CapturePlan;
        if (targetOccurrence == 0 && maxReplacements == 1)
        {
            var matches = new FirstValueMatchSource(state, regex, input, capturePlan);
            return ReplaceGensubMatches(
                state,
                input,
                replacement,
                targetOccurrence,
                maxReplacements,
                matches,
                replacementPlan,
                out replacementCount);
        }

        var allValueMatches = new ValueMatchSource(
            state.EnumerateRegexMatches(regex, input, capturePlan));
        return ReplaceGensubMatches(
            state,
            input,
            replacement,
            targetOccurrence,
            maxReplacements,
            allValueMatches,
            replacementPlan,
            out replacementCount);

        static AwkText ReplaceSpans<TMatchSource>(
            AwkRuntimeState state,
            AwkText input,
            AwkText replacement,
            SpanReplacementKind replacementKind,
            int targetOccurrence,
            int maxReplacements,
            TMatchSource matches,
            out int replacementCount)
            where TMatchSource : struct, IMatchSource<AwkRegexMatchSpan>
            => ReplaceMatches<AwkRegexMatchSpan, TMatchSource, SpanMatchReplacement>(
                state,
                input,
                replacement,
                targetOccurrence,
                maxReplacements,
                matches,
                new SpanMatchReplacement(replacementKind),
                out replacementCount);

        static AwkText ReplaceGensubMatches<TMatchSource>(
            AwkRuntimeState state,
            AwkText input,
            AwkText replacement,
            int targetOccurrence,
            int maxReplacements,
            TMatchSource matches,
            AwkGensubReplacementPlan replacementPlan,
            out int replacementCount)
            where TMatchSource : struct, IMatchSource<AwkRegexMatch>
            => ReplaceMatches<AwkRegexMatch, TMatchSource, GensubMatchReplacement>(
                state,
                input,
                replacement,
                targetOccurrence,
                maxReplacements,
                matches,
                new GensubMatchReplacement(replacementPlan),
                out replacementCount);
    }

    private static AwkText ReplaceMatches<TMatch, TMatchSource, TReplacement>(
        AwkRuntimeState state,
        AwkText input,
        AwkText replacement,
        int targetOccurrence,
        int maxReplacements,
        TMatchSource matches,
        TReplacement matchReplacement,
        out int replacementCount)
        where TMatch : struct
        where TMatchSource : struct, IMatchSource<TMatch>
        where TReplacement : struct, IMatchReplacement<TMatch>
    {
        using var matchSource = matches;
        var previousNonEmptyEnd = -1;
        if (!MoveNext(out var match))
        {
            replacementCount = 0;
            return input;
        }

        var initialCapacity = Math.Min(
            input.ByteLength,
            AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity);
        if (targetOccurrence > 0)
        {
            for (var seen = 1; seen < targetOccurrence; seen++)
            {
                state.ThrowIfCancellationRequested();
                if (!MoveNext(out match))
                {
                    replacementCount = 0;
                    return input;
                }
            }

            state.ThrowIfCancellationRequested();
            using var occurrenceBuilder = new AwkUtf8Builder(
                state.Limits,
                state.MemoryGovernor,
                initialCapacity);
            var matchIndex = matchReplacement.GetByteIndex(in match);
            var matchLength = matchReplacement.GetByteLength(in match);
            occurrenceBuilder.Append(input.AsSpan(state.CancellationCheckpoint)[..matchIndex], state.CancellationCheckpoint);
            matchReplacement.Append(input, in match, replacement, occurrenceBuilder, state.CancellationCheckpoint);
            occurrenceBuilder.Append(input.AsSpan(state.CancellationCheckpoint)[(matchIndex + matchLength)..], state.CancellationCheckpoint);
            replacementCount = 1;
            return occurrenceBuilder.CopyToAwkText(state.Counters, state.CancellationCheckpoint);
        }

        using var builder = new AwkUtf8Builder(
            state.Limits,
            state.MemoryGovernor,
            initialCapacity);
        var position = 0;
        replacementCount = 0;
        do
        {
            state.ThrowIfCancellationRequested();
            var matchIndex = matchReplacement.GetByteIndex(in match);
            var matchLength = matchReplacement.GetByteLength(in match);
            builder.Append(input.AsSpan(state.CancellationCheckpoint).Slice(position, matchIndex - position), state.CancellationCheckpoint);
            matchReplacement.Append(input, in match, replacement, builder, state.CancellationCheckpoint);
            position = matchIndex + matchLength;
            replacementCount++;

            if (maxReplacements > 0 && replacementCount >= maxReplacements)
            {
                break;
            }
        }
        while (MoveNext(out match));

        builder.Append(input.AsSpan(state.CancellationCheckpoint)[position..], state.CancellationCheckpoint);
        return builder.CopyToAwkText(state.Counters, state.CancellationCheckpoint);

        bool MoveNext(out TMatch next)
        {
            while (matchSource.MoveNext(out next))
            {
                var index = matchReplacement.GetByteIndex(in next);
                var length = matchReplacement.GetByteLength(in next);
                if (targetOccurrence <= 0 && length == 0 && index == previousNonEmptyEnd)
                    continue;
                if (length > 0)
                    previousNonEmptyEnd = index + length;
                return true;
            }

            return false;
        }
    }

    private static bool IsLiteralReplacement(AwkText replacement, Action checkpoint)
    {
        var bytes = replacement.AsSpan(checkpoint);
        for (var offset = 0; offset < bytes.Length;)
        {
            checkpoint();
            var length = Math.Min(AwkExecutionGovernor.WorkChunkBytes, bytes.Length - offset);
            if (bytes.Slice(offset, length).IndexOfAny((byte)'&', (byte)'\\') >= 0)
                return false;
            offset += length;
        }

        checkpoint();
        return true;
    }

    private interface IMatchReplacement<TMatch>
        where TMatch : struct
    {
        int GetByteIndex(in TMatch match);

        int GetByteLength(in TMatch match);

        void Append(AwkText input, in TMatch match, AwkText replacement, AwkUtf8Builder builder, Action checkpoint);
    }

    private readonly record struct SpanMatchReplacement(SpanReplacementKind Kind)
        : IMatchReplacement<AwkRegexMatchSpan>
    {
        public int GetByteIndex(in AwkRegexMatchSpan match) => match.ByteIndex;

        public int GetByteLength(in AwkRegexMatchSpan match) => match.ByteLength;

        public void Append(
            AwkText input,
            in AwkRegexMatchSpan match,
            AwkText replacement,
            AwkUtf8Builder builder,
            Action checkpoint)
        {
            if (Kind == SpanReplacementKind.Literal)
            {
                builder.Append(replacement, checkpoint);
                return;
            }

            AwkReplacement.AppendSubstitution(
                input.AsSpan(checkpoint).Slice(match.ByteIndex, match.ByteLength),
                replacement,
                builder,
                checkpoint);
        }
    }

    private readonly struct GensubMatchReplacement(
        AwkGensubReplacementPlan replacementPlan) : IMatchReplacement<AwkRegexMatch>
    {
        public int GetByteIndex(in AwkRegexMatch match) => match.ByteIndex;

        public int GetByteLength(in AwkRegexMatch match) => match.ByteLength;

        public void Append(
            AwkText input,
            in AwkRegexMatch match,
            AwkText replacement,
            AwkUtf8Builder builder,
            Action checkpoint) => replacementPlan.Append(match, builder, checkpoint);
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
        AwkText input,
        AwkRegexCapturePlan capturePlan) : IMatchSource<AwkRegexMatch>
    {
        private AwkLeasedRegexMatch _currentMatch = AwkLeasedRegexMatch.NoMatch;
        private bool _read;

        public bool MoveNext(out AwkRegexMatch match)
        {
            _currentMatch.Dispose();
            _currentMatch = AwkLeasedRegexMatch.NoMatch;
            if (_read)
            {
                match = AwkRegexMatch.NoMatch;
                return false;
            }

            _read = true;
            _currentMatch = state.MatchRegexValue(regex, input, capturePlan);
            match = _currentMatch.Match;
            return match.Success;
        }

        public void Dispose()
        {
            _currentMatch.Dispose();
            _currentMatch = AwkLeasedRegexMatch.NoMatch;
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
