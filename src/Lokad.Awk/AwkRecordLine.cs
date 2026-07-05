using System;

namespace Lokad.Awk;

internal static class AwkRecordLine
{
    public static AwkRecordLineResult ReadUtf8DefaultNewline(ReadOnlyMemory<byte> rawLine) =>
        ReadUtf8DefaultNewline(rawLine, AwkRuntimeCounters.Disabled);

    public static AwkRecordLineResult ReadUtf8DefaultNewline(
        ReadOnlyMemory<byte> rawLine,
        AwkRuntimeCounters counters)
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

        return new AwkRecordLineResult(AwkText.FromHostBytes(rawLine[..end], counters), terminator);
    }
}

internal readonly struct AwkRecordLineResult(AwkText record, AwkRecordTerminator terminator)
{
    public AwkText Record { get; } = record;

    public AwkRecordTerminator Terminator { get; } = terminator;
}
