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
        if (IsLiteralReplacement(replacement))
        {
            return targetOccurrence == 0 && maxReplacements == 1
                ? ReplaceFirstLiteral(state, regex, input, replacement, out replacementCount)
                : ReplaceLiteral(
                    state,
                    regex,
                    input,
                    replacement,
                    targetOccurrence,
                    maxReplacements,
                    out replacementCount);
        }

        if (targetOccurrence == 0 && maxReplacements == 1)
        {
            return mode == AwkReplacementMode.Substitution
                ? ReplaceFirstSubstitution(state, regex, input, replacement, out replacementCount)
                : ReplaceFirstGensub(state, regex, input, replacement, out replacementCount);
        }

        if (mode == AwkReplacementMode.Substitution)
        {
            return ReplaceSubstitution(
                state,
                regex,
                input,
                replacement,
                targetOccurrence,
                maxReplacements,
                out replacementCount);
        }

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
            var replace = targetOccurrence == 0 || seen == targetOccurrence;
            if (!replace)
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

        builder.Append(input.AsSpan().Slice(position));
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceSubstitution(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
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
            var replace = targetOccurrence == 0 || seen == targetOccurrence;
            if (!replace)
                continue;

            builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
            AwkReplacement.AppendSubstitution(
                input.AsSpan().Slice(match.ByteIndex, match.ByteLength),
                replacementText,
                builder);
            position = match.ByteIndex + match.ByteLength;
            replacementCount++;

            if (targetOccurrence > 0 ||
                (maxReplacements > 0 && replacementCount >= maxReplacements))
            {
                break;
            }
        }
        while (matches.MoveNext(out match));

        builder.Append(input.AsSpan().Slice(position));
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
        builder.Append(input.AsSpan().Slice(0, match.ByteIndex));
        AwkReplacement.AppendGensub(match, replacementText, builder);
        builder.Append(input.AsSpan().Slice(match.ByteIndex + match.ByteLength));
        replacementCount = 1;
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceFirstSubstitution(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
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
        AwkReplacement.AppendSubstitution(
            input.AsSpan().Slice(match.ByteIndex, match.ByteLength),
            replacementText,
            builder);
        builder.Append(input.AsSpan()[(match.ByteIndex + match.ByteLength)..]);
        replacementCount = 1;
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceLiteral(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
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
            var replace = targetOccurrence == 0 || seen == targetOccurrence;
            if (replace)
            {
                builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
                builder.Append(replacementText);
                position = match.ByteIndex + match.ByteLength;
                replacementCount++;

                if (targetOccurrence > 0 ||
                    (maxReplacements > 0 && replacementCount >= maxReplacements))
                    break;
            }
        }
        while (matches.MoveNext(out match));

        builder.Append(input.AsSpan().Slice(position));
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceFirstLiteral(
        AwkRuntimeState state,
        AwkRegex regex,
        AwkText input,
        string replacement,
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
        builder.Append(input.AsSpan().Slice(0, match.ByteIndex));
        builder.Append(replacementText);
        builder.Append(input.AsSpan().Slice(match.ByteIndex + match.ByteLength));
        replacementCount = 1;
        return builder.CopyToAwkText(state.Counters);
    }

    private static bool IsLiteralReplacement(string replacement) =>
        replacement.IndexOfAny(['&', '\\']) < 0;
}
