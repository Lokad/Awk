namespace Lokad.Awk;

/// <summary>Byte-native AWK translation helpers, with admitted storage and bounded cancellation.</summary>
internal static class AwkRegexConstruction
{
    // Source diagnostics still inspect UTF-16; byte scans may check more frequently.
    internal const int ChunkCharacters = AwkRuntimeLimits.CancellationCheckBytes / sizeof(char);

    public static void CheckProgress(int position, ref long next, Action? checkpoint)
    {
        if (checkpoint is null || position < next) return;
        checkpoint();
        next = (long)position + ChunkCharacters;
    }

    internal static int BracketEnd(AwkText pattern, int opening, Action? checkpoint = null)
    {
        var bytes = pattern.AsSpan(checkpoint);
        var index = opening + 1;
        long next = index;
        if (index < bytes.Length && bytes[index] == (byte)'^') index++;
        if (index < bytes.Length && bytes[index] == (byte)']') index++;
        for (; index < bytes.Length; index++)
        {
            CheckProgress(index, ref next, checkpoint);
            if (bytes[index] == (byte)'\\') index++;
            else if (bytes[index] == (byte)']') return index;
        }
        checkpoint?.Invoke();
        return -1;
    }

    internal static int TokenEnd(ReadOnlySpan<byte> pattern, int opening, Action? checkpoint)
    {
        Span<byte> delimiter = stackalloc byte[2] { pattern[opening + 1], (byte)']' };
        return IndexOf(pattern, delimiter, opening + 2, checkpoint);
    }

    internal static int IndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value, int start, Action? checkpoint)
    {
        for (var index = start; index < text.Length;)
        {
            checkpoint?.Invoke();
            var advance = Math.Min(ChunkCharacters, text.Length - index);
            var count = (int)Math.Min(text.Length - (long)index, advance + (long)value.Length - 1);
            var offset = text.Slice(index, count).IndexOf(value);
            if (offset >= 0) return index + offset;
            index += advance;
        }
        checkpoint?.Invoke();
        return -1;
    }

    internal static int IndexOf(ReadOnlySpan<byte> text, byte value, int start, Action? checkpoint)
    {
        for (var index = start; index < text.Length;)
        {
            checkpoint?.Invoke();
            var count = Math.Min(ChunkCharacters, text.Length - index);
            var offset = text.Slice(index, count).IndexOf(value);
            if (offset >= 0) return index + offset;
            index += count;
        }
        checkpoint?.Invoke();
        return -1;
    }

    internal static AwkText Finish(AwkUtf8Builder builder, AwkMemoryLease results, Action? checkpoint)
    {
        checkpoint?.Invoke();
        if (builder.Length == 0) return AwkText.Empty;
        results.ReserveAdditional(AwkRuntimeMemory.EstimateTextBytes(builder.Length));
        var result = AwkText.CopyTrustedBytes(builder.AsSpan(), AwkRuntimeCounters.Disabled,
            AwkMemoryAccounting.Disabled, checkpoint);
        checkpoint?.Invoke();
        return result;
    }

    internal static AwkText EscapeLiteral(AwkText text, Action? checkpoint,
        AwkMemoryAccounting governor, AwkMemoryLease results)
    {
        var bytes = text.AsSpan(checkpoint);
        long next = 0;
        var first = -1;
        for (var index = 0; index < bytes.Length; index++)
        {
            CheckProgress(index, ref next, checkpoint);
            if (NeedsEscape(bytes[index])) { first = index; break; }
        }
        checkpoint?.Invoke();
        if (first < 0) return text;
        using var builder = new AwkUtf8Builder(governor, bytes.Length);
        builder.Append(bytes[..first], checkpoint);
        for (var index = first; index < bytes.Length; index++)
        {
            CheckProgress(index, ref next, checkpoint);
            AppendLiteral(builder, bytes[index], inBracket: false);
        }
        return Finish(builder, results, checkpoint);
    }

    internal static void AppendLiteral(AwkUtf8Builder builder, byte value, bool inBracket)
    {
        if (inBracket && value is (byte)']' or (byte)'-' or (byte)'^' || NeedsEscape(value))
            builder.Append((byte)'\\');
        // Preserve the established escaped whitespace spelling without Regex.Escape or strings.
        builder.Append(value switch
        {
            (byte)'\t' => (byte)'t', (byte)'\n' => (byte)'n',
            (byte)'\f' => (byte)'f', (byte)'\r' => (byte)'r', _ => value
        });
    }

    private static bool NeedsEscape(byte value) => value is
        (byte)'\t' or (byte)'\n' or (byte)'\f' or (byte)'\r' or (byte)' ' or (byte)'#' or
        (byte)'$' or (byte)'(' or (byte)')' or (byte)'*' or (byte)'+' or (byte)'.' or
        (byte)'?' or (byte)'[' or (byte)'\\' or (byte)'^' or (byte)'{' or (byte)'|';
}
