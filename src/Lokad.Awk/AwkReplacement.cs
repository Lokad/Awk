namespace Lokad.Awk;

internal static class AwkReplacement
{
    public static void AppendSubstitution(
        ReadOnlySpan<byte> match,
        AwkText replacement,
        AwkUtf8Builder builder)
    {
        if (replacement.ByteLength == 0)
            return;

        AppendSubstitution(match, replacement.AsSpan(), builder);
    }

    public static void AppendGensub(
        AwkRegexMatch match,
        AwkText replacement,
        AwkUtf8Builder builder)
    {
        if (replacement.ByteLength == 0)
            return;

        AppendGensub(match, replacement.AsSpan(), builder);
    }

    private static void AppendGensub(
        AwkRegexMatch match,
        ReadOnlySpan<byte> replacement,
        AwkUtf8Builder builder)
    {
        for (var index = 0; index < replacement.Length; index++)
        {
            var current = replacement[index];
            if (current == (byte)'&')
            {
                builder.Append(match.Value);
                continue;
            }

            if (current != (byte)'\\')
            {
                var literalStart = index;
                while (index + 1 < replacement.Length &&
                    replacement[index + 1] is not (byte)'&' and not (byte)'\\')
                {
                    index++;
                }

                builder.Append(replacement.Slice(literalStart, index - literalStart + 1));
                continue;
            }

            if (index + 1 >= replacement.Length)
            {
                builder.Append((byte)'\\');
                continue;
            }

            index++;
            var escaped = replacement[index];
            if (escaped == (byte)'&')
            {
                builder.Append((byte)'&');
                continue;
            }

            if (escaped == (byte)'\\')
            {
                builder.Append((byte)'\\');
                continue;
            }

            if (escaped is >= (byte)'0' and <= (byte)'9')
            {
                var groupIndex = escaped - '0';
                if (groupIndex < match.Groups.Count && match.Groups[groupIndex].Success)
                    builder.Append(match.Groups[groupIndex].Value);

                continue;
            }

            builder.Append(escaped);
        }
    }

    private static void AppendSubstitution(
        ReadOnlySpan<byte> match,
        ReadOnlySpan<byte> replacement,
        AwkUtf8Builder builder)
    {
        for (var index = 0; index < replacement.Length; index++)
        {
            var current = replacement[index];
            if (current == (byte)'&')
            {
                builder.Append(match);
                continue;
            }

            if (current != (byte)'\\')
            {
                var literalStart = index;
                while (index + 1 < replacement.Length &&
                    replacement[index + 1] is not (byte)'&' and not (byte)'\\')
                {
                    index++;
                }

                builder.Append(replacement.Slice(literalStart, index - literalStart + 1));
                continue;
            }

            var slashStart = index;
            while (index < replacement.Length && replacement[index] == (byte)'\\')
                index++;

            var slashCount = index - slashStart;
            if (index < replacement.Length && replacement[index] == (byte)'&')
            {
                builder.Append((byte)'\\', slashCount / 2);
                if (slashCount % 2 == 0)
                    builder.Append(match);
                else
                    builder.Append((byte)'&');

                continue;
            }

            builder.Append((byte)'\\', slashCount);
            index--;
        }
    }
}

internal enum AwkReplacementMode
{
    Substitution,
    Gensub
}
