using System.Text.RegularExpressions;

namespace Lokad.Awk;

internal static class AwkRegexSubstitution
{
    public static AwkText ReplaceAll(
        AwkRuntimeState state,
        Regex regex,
        AwkText input,
        string replacement,
        AwkReplacementMode mode,
        int maxReplacements,
        out int replacementCount) =>
        Replace(state, regex, input, replacement, mode, targetOccurrence: 0, maxReplacements, out replacementCount);

    public static AwkText ReplaceOccurrence(
        AwkRuntimeState state,
        Regex regex,
        AwkText input,
        string replacement,
        AwkReplacementMode mode,
        int occurrence,
        out int replacementCount) =>
        Replace(state, regex, input, replacement, mode, occurrence, maxReplacements: 1, out replacementCount);

    private static AwkText Replace(
        AwkRuntimeState state,
        Regex regex,
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
            return ReplaceFirst(state, regex, input, replacement, mode, out replacementCount);

        using var matches = state.EnumerateRegexMatches(regex, input);
        if (matches.Count == 0)
        {
            replacementCount = 0;
            return input;
        }

        var replacementText = AwkText.FromString(replacement, state.Counters, state.MemoryGovernor);
        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
        var position = 0;
        var seen = 0;
        replacementCount = 0;
        foreach (var match in matches)
        {
            state.ThrowIfCancellationRequested();
            seen++;
            var replace = targetOccurrence == 0 || seen == targetOccurrence;
            if (!replace)
                continue;

            builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
            AwkReplacement.AppendExpanded(match, replacementText, mode, builder);
            position = match.ByteIndex + match.ByteLength;
            replacementCount++;

            if (targetOccurrence > 0 ||
                (maxReplacements > 0 && replacementCount >= maxReplacements))
            {
                break;
            }
        }

        builder.Append(input.AsSpan().Slice(position));
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceFirst(
        AwkRuntimeState state,
        Regex regex,
        AwkText input,
        string replacement,
        AwkReplacementMode mode,
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
        AwkReplacement.AppendExpanded(match, replacementText, mode, builder);
        builder.Append(input.AsSpan().Slice(match.ByteIndex + match.ByteLength));
        replacementCount = 1;
        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ReplaceLiteral(
        AwkRuntimeState state,
        Regex regex,
        AwkText input,
        string replacement,
        int targetOccurrence,
        int maxReplacements,
        out int replacementCount)
    {
        var matches = state.EnumerateRegexMatchSpans(regex, input);
        try
        {
            if (matches.Count == 0)
            {
                replacementCount = 0;
                return input;
            }

            var replacementText = AwkText.FromString(replacement, state.Counters, state.MemoryGovernor);
            using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, input.ByteLength);
            var position = 0;
            var seen = 0;
            replacementCount = 0;
            foreach (var match in matches)
            {
                state.ThrowIfCancellationRequested();
                seen++;
                var replace = targetOccurrence == 0 || seen == targetOccurrence;
                if (!replace)
                    continue;

                builder.Append(input.AsSpan().Slice(position, match.ByteIndex - position));
                builder.Append(replacementText);
                position = match.ByteIndex + match.ByteLength;
                replacementCount++;

                if (targetOccurrence > 0 ||
                    (maxReplacements > 0 && replacementCount >= maxReplacements))
                {
                    break;
                }
            }

            builder.Append(input.AsSpan().Slice(position));
            return builder.CopyToAwkText(state.Counters);
        }
        finally
        {
            matches.Dispose();
        }
    }

    private static AwkText ReplaceFirstLiteral(
        AwkRuntimeState state,
        Regex regex,
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
