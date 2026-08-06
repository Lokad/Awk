namespace Lokad.Awk;

internal static class AwkReplacement
{
    public static AwkText Expand(
        AwkRegexMatch match,
        string replacement,
        AwkReplacementMode mode,
        AwkRuntimeState state)
    {
        if (replacement.Length == 0)
            return AwkText.Empty;

        return mode == AwkReplacementMode.Substitution
            ? ExpandSubstitution(match, replacement, state)
            : ExpandGensub(match, replacement, state);
    }

    private static AwkText ExpandGensub(AwkRegexMatch match, string replacement, AwkRuntimeState state)
    {
        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, replacement.Length + match.Value.ByteLength);
        for (var index = 0; index < replacement.Length; index++)
        {
            var current = replacement[index];
            if (current == '&')
            {
                builder.Append(match.Value);
                continue;
            }

            if (current != '\\' || index + 1 >= replacement.Length)
            {
                AppendString(builder, current.ToString(), state);
                continue;
            }

            index++;
            var escaped = replacement[index];
            if (escaped == '&')
            {
                AppendString(builder, "&", state);
                continue;
            }

            if (escaped == '\\')
            {
                AppendString(builder, "\\", state);
                continue;
            }

            if (char.IsDigit(escaped))
            {
                var groupIndex = escaped - '0';
                if (groupIndex < match.Groups.Count && match.Groups[groupIndex].Success)
                    builder.Append(match.Groups[groupIndex].Value);

                continue;
            }

            AppendString(builder, escaped.ToString(), state);
        }

        return builder.CopyToAwkText(state.Counters);
    }

    private static AwkText ExpandSubstitution(AwkRegexMatch match, string replacement, AwkRuntimeState state)
    {
        using var builder = new AwkUtf8Builder(state.Limits, state.MemoryGovernor, replacement.Length + match.Value.ByteLength);
        for (var index = 0; index < replacement.Length; index++)
        {
            var current = replacement[index];
            if (current == '&')
            {
                builder.Append(match.Value);
                continue;
            }

            if (current != '\\')
            {
                AppendString(builder, current.ToString(), state);
                continue;
            }

            var slashStart = index;
            while (index < replacement.Length && replacement[index] == '\\')
                index++;

            var slashCount = index - slashStart;
            if (index < replacement.Length && replacement[index] == '&')
            {
                AppendString(builder, new string('\\', slashCount / 2), state);
                if (slashCount % 2 == 0)
                    builder.Append(match.Value);
                else
                    AppendString(builder, "&", state);

                continue;
            }

            AppendString(builder, new string('\\', slashCount), state);
            index--;
        }

        return builder.CopyToAwkText(state.Counters);
    }

    private static void AppendString(AwkUtf8Builder builder, string text, AwkRuntimeState state)
    {
        if (text.Length == 0)
            return;

        builder.Append(AwkText.FromString(text, state.Counters, state.MemoryGovernor));
    }
}

internal enum AwkReplacementMode
{
    Substitution,
    Gensub
}
