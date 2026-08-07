namespace Lokad.Awk;

internal static class AwkRegexSubstitution
{
    public static AwkText ReplaceAll(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
        AwkReplacementMode mode,
        int maxReplacements,
        out int replacementCount) =>
        Replace(state, regex, input, replacement, mode, targetOccurrence: 0, maxReplacements, out replacementCount);

    public static AwkText ReplaceOccurrence(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
        AwkReplacementMode mode,
        int occurrence,
        out int replacementCount) =>
        Replace(state, regex, input, replacement, mode, occurrence, maxReplacements: 1, out replacementCount);

    private static AwkText Replace(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
        AwkReplacementMode mode,
        int targetOccurrence,
        int maxReplacements,
        out int replacementCount)
    {
        var spanReplacementKind = IsLiteralReplacement(replacement)
            ? SpanReplacementKind.Literal
            : SpanReplacementKind.Substitution;
        if (spanReplacementKind == SpanReplacementKind.Literal ||
            mode == AwkReplacementMode.Substitution)
        {
            return targetOccurrence == 0 && maxReplacements == 1
                ? ReplaceFirstSpan(
                    state,
                    regex,
                    input,
                    replacement,
                    spanReplacementKind,
                    out replacementCount)
                : ReplaceSpans(
                    state,
                    regex,
                    input,
                    replacement,
                    spanReplacementKind,
                    targetOccurrence,
                    maxReplacements,
                    out replacementCount);
        }

        return targetOccurrence == 0 && maxReplacements == 1
            ? ReplaceFirstGensub(state, regex, input, replacement, out replacementCount)
            : ReplaceGensubMatches(
                state,
                regex,
                input,
                replacement,
                targetOccurrence,
                maxReplacements,
                out replacementCount);
    }

    private static AwkText ReplaceSpans(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
        SpanReplacementKind replacementKind,
        int targetOccurrence,
        int maxReplacements,
        out int replacementCount)
    {
        using var matches = state.EnumerateRegexMatchSpans(regex, input);
        if (!matches.MoveNext(out var match))
        {
            replacementCount = 0;
            return input;
        }

        var replacementText = AwkText.FromString(replacement, state.Counters, state.MemoryGovernor);
        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
        var position = 0;
        var seen = 0;
        replacementCount = 0;
        do
        {
            state.ThrowIfCancellationRequested();
            seen++;
            if (targetOccurrence != 0 && seen != targetOccurrence)
                continue;

            builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
            AppendSpanReplacement(replacementKind, input, match, replacementText, builder);
            position = match.ByteIndex + match.ByteLength;
            replacementCount++;

            if (targetOccurrence > 0 ||
                (maxReplacements > 0 && replacementCount >= maxReplacements))
            {
                break;
            }
        }
        while (matches.MoveNext(out match));

        builder.Append(input.AsSpan()[position..]);
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceFirstSpan(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
        SpanReplacementKind replacementKind,
        out int replacementCount)
    {
        var match = state.MatchRegexSpan(regex, input);
        if (!match.Success)
        {
            replacementCount = 0;
            return input;
        }

        var replacementText = AwkText.FromString(replacement, state.Counters, state.MemoryGovernor);
        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
        builder.Append(input.AsSpan()[..match.ByteIndex]);
        AppendSpanReplacement(replacementKind, input, match, replacementText, builder);
        builder.Append(input.AsSpan()[(match.ByteIndex + match.ByteLength)..]);
        replacementCount = 1;
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

    private static AwkText ReplaceGensubMatches(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
        int targetOccurrence,
        int maxReplacements,
        out int replacementCount)
    {
        using var matches = state.EnumerateRegexMatches(regex, input);
        if (!matches.MoveNext(out var match))
        {
            replacementCount = 0;
            return input;
        }

        var replacementText = AwkText.FromString(replacement, state.Counters, state.MemoryGovernor);
        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
        var position = 0;
        var seen = 0;
        replacementCount = 0;
        do
        {
            state.ThrowIfCancellationRequested();
            seen++;
            if (targetOccurrence != 0 && seen != targetOccurrence)
                continue;

            builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
            AwkReplacement.AppendGensub(match, replacementText, builder);
            position = match.ByteIndex + match.ByteLength;
            replacementCount++;

            if (targetOccurrence > 0 ||
                (maxReplacements > 0 && replacementCount >= maxReplacements))
            {
                break;
            }
        }
        while (matches.MoveNext(out match));

        builder.Append(input.AsSpan()[position..]);
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceFirstGensub(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
        out int replacementCount)
    {
        var match = state.MatchRegexValue(regex, input);
        if (!match.Success)
        {
            replacementCount = 0;
            return input;
        }

        var replacementText = AwkText.FromString(replacement, state.Counters, state.MemoryGovernor);
        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
        builder.Append(input.AsSpan()[..match.ByteIndex]);
        AwkReplacement.AppendGensub(match, replacementText, builder);
        builder.Append(input.AsSpan()[(match.ByteIndex + match.ByteLength)..]);
        replacementCount = 1;
        return builder.CopyToAwkText(state.Counters);
    }

    private static bool IsLiteralReplacement(string replacement) =>
        replacement.IndexOfAny(['&', '\\']) < 0;

    private enum SpanReplacementKind
    {
        Literal,
        Substitution
    }
}
