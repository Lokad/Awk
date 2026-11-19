namespace Lokad.Awk;

internal static class AwkRecordLine
{
    public static AwkRecordLineResult SplitDefaultNewline(ReadOnlyMemory<byte> rawLine)
    {
        var span = rawLine.Span;
        var end = span.Length;
        var terminator = AwkRecordTerminator.Empty;
        if (end > 0 && span[end - 1] == (byte)'\n')
        {
            terminator = AwkRecordTerminator.NewLine;
            end--;
            if (end > 0 && span[end - 1] == (byte)'\r')
                end--;
        }

        return new AwkRecordLineResult(rawLine[..end], terminator);
    }

    internal static void AppendNormalizedCsv(AwkUtf8Builder destination, ReadOnlySpan<byte> content,
        int firstCrLf, Action checkpoint)
    {
        var start = 0;
        var carriageReturn = firstCrLf;
        while (true)
        {
            destination.Append(content[start..carriageReturn], checkpoint);
            start = carriageReturn + 1;
            var next = IndexOfLiteral(content[start..], "\r\n"u8, checkpoint);
            if (next < 0) break;
            carriageReturn = start + next;
        }
        destination.Append(content[start..], checkpoint);
    }

    internal static int IndexOfByte(ReadOnlySpan<byte> content, byte value, Action checkpoint)
    {
        var offset = 0;
        while (offset < content.Length)
        {
            checkpoint();
            var length = Math.Min(AwkRuntimeLimits.CancellationCheckBytes, content.Length - offset);
            var found = content.Slice(offset, length).IndexOf(value);
            if (found >= 0)
                return offset + found;
            offset += length;
        }
        return -1;
    }

    internal static int IndexOfLiteral(ReadOnlySpan<byte> content, ReadOnlySpan<byte> value, Action checkpoint)
    {
        if (value.Length > AwkRuntimeLimits.CancellationCheckBytes)
        {
            // Keep the framework's long-needle algorithm rather than repeatedly
            // searching a large overlap. This remains an indivisible boundary.
            checkpoint();
            var found = content.IndexOf(value);
            checkpoint();
            return found;
        }
        var offset = 0;
        var candidates = content.Length - value.Length + 1;
        while (offset < candidates)
        {
            checkpoint();
            var candidateCount = Math.Min(AwkRuntimeLimits.CancellationCheckBytes, candidates - offset);
            // Include the overlap so a separator may start at the last candidate.
            // Large needles remain a framework boundary, not a new local matcher.
            var found = content.Slice(offset, candidateCount + value.Length - 1).IndexOf(value);
            if (found >= 0)
                return offset + found;
            offset += candidateCount;
        }
        return -1;
    }
}

internal readonly struct AwkRecordLineResult(
    ReadOnlyMemory<byte> recordBytes,
    AwkRecordTerminator terminator)
{
    public ReadOnlyMemory<byte> RecordBytes { get; } = recordBytes;

    public AwkRecordTerminator Terminator { get; } = terminator;
}
