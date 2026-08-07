using System;

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
}

internal readonly struct AwkRecordLineResult(
    ReadOnlyMemory<byte> recordBytes,
    AwkRecordTerminator terminator)
{
    public ReadOnlyMemory<byte> RecordBytes { get; } = recordBytes;

    public AwkRecordTerminator Terminator { get; } = terminator;
}
