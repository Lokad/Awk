using System.Text;
using System.Text.RegularExpressions;

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

    public static string Expand(Match match, string replacement, AwkReplacementMode mode)
    {
        if (replacement.Length == 0)
            return string.Empty;

        if (mode == AwkReplacementMode.Substitution)
            return ExpandSubstitution(match, replacement);

        return ExpandGensub(match, replacement);
    }

    private static string ExpandGensub(Match match, string replacement)
    {
        var builder = new StringBuilder(replacement.Length + match.Value.Length);
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
                builder.Append(current);
                continue;
            }

            index++;
            var escaped = replacement[index];
            if (escaped == '&')
            {
                builder.Append('&');
                continue;
            }

            if (escaped == '\\')
            {
                builder.Append('\\');
                continue;
            }

            if (char.IsDigit(escaped))
            {
                var groupIndex = escaped - '0';
                if (groupIndex < match.Groups.Count && match.Groups[groupIndex].Success)
                    builder.Append(match.Groups[groupIndex].Value);

                continue;
            }

            builder.Append(escaped);
        }

        return builder.ToString();
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

    private static string ExpandSubstitution(Match match, string replacement)
    {
        var builder = new StringBuilder(replacement.Length + match.Value.Length);
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
                builder.Append(current);
                continue;
            }

            var slashStart = index;
            while (index < replacement.Length && replacement[index] == '\\')
                index++;

            var slashCount = index - slashStart;
            if (index < replacement.Length && replacement[index] == '&')
            {
                builder.Append('\\', slashCount / 2);
                if (slashCount % 2 == 0)
                    builder.Append(match.Value);
                else
                    builder.Append('&');

                continue;
            }

            builder.Append('\\', slashCount);
            index--;
        }

        return builder.ToString();
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
