namespace Lokad.Awk;

internal static class AwkReplacement
{
    public static void AppendSubstitution(
        ReadOnlySpan<byte> match,
        AwkText replacement,
        AwkUtf8Builder builder,
        AwkCompatibilityMode compatibilityMode,
        Action? checkpoint = null)
    {
        if (replacement.ByteLength == 0)
            return;

        AppendSubstitution(match, replacement.AsSpan(checkpoint), builder, compatibilityMode, checkpoint);

        static void AppendSubstitution(
            ReadOnlySpan<byte> match,
            ReadOnlySpan<byte> replacement,
            AwkUtf8Builder builder,
            AwkCompatibilityMode compatibilityMode,
            Action? checkpoint)
        {
            long nextCheckpoint = 0;
            for (var index = 0; index < replacement.Length; index++)
            {
                CheckProgress(index);
                var current = replacement[index];
                if (current == (byte)'&')
                {
                    builder.Append(match, checkpoint);
                    continue;
                }

                if (current != (byte)'\\')
                {
                    var literalStart = index;
                    while (index + 1 < replacement.Length &&
                        replacement[index + 1] is not (byte)'&' and not (byte)'\\')
                    {
                        index++;
                        CheckProgress(index);
                    }

                    builder.Append(replacement.Slice(literalStart, index - literalStart + 1), checkpoint);
                    continue;
                }

                var slashStart = index;
                while (index < replacement.Length && replacement[index] == (byte)'\\')
                {
                    CheckProgress(index);
                    index++;
                }

                var slashCount = index - slashStart;
                if (index < replacement.Length && replacement[index] == (byte)'&')
                {
                    builder.Append((byte)'\\', slashCount / 2, checkpoint);
                    if (slashCount % 2 == 0)
                        builder.Append(match, checkpoint);
                    else
                        builder.Append((byte)'&');

                    continue;
                }

                // POSIX reduces pairs; GNU/traditional reduce groups of four
                // before ordinary text or the end of the replacement.
                var literalSlashes = compatibilityMode == AwkCompatibilityMode.Posix
                    ? slashCount - slashCount / 2
                    : slashCount - 2 * (slashCount / 4);
                builder.Append((byte)'\\', literalSlashes, checkpoint);
                index--;
            }
            checkpoint?.Invoke();

            void CheckProgress(int offset)
            {
                if (offset < nextCheckpoint)
                    return;
                checkpoint?.Invoke();
                nextCheckpoint = (long)offset + AwkRuntimeLimits.CancellationCheckBytes;
            }
        }
    }
}

internal enum AwkReplacementMode
{
    Substitution,
    Gensub
}
