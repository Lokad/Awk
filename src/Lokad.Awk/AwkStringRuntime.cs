using System;
using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkStringRuntime
{
    private readonly Func<bool> _charactersAsBytes;

    public AwkStringRuntime(Func<bool> charactersAsBytes)
    {
        _charactersAsBytes = charactersAsBytes;
    }

    public int CountUnits(AwkText value)
    {
        return _charactersAsBytes() ? value.ByteLength : value.RuneLength;
    }

    public int IndexOfUnits(AwkText source, AwkText value)
    {
        var position = source.IndexOf(value);
        if (position < 0)
            return 0;

        return _charactersAsBytes()
            ? position + 1
            : CountRunes(source.AsSpan()[..position]) + 1;
    }

    public AwkText SubstringByUnits(
        AwkText source,
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        return _charactersAsBytes()
            ? source.CopyByteSlice(start, length, counters, governor)
            : source.CopyRuneSlice(start, length, counters, governor);
    }

    private static int CountRunes(ReadOnlySpan<byte> value)
    {
        var count = 0;
        while (!value.IsEmpty)
        {
            var status = Rune.DecodeFromUtf8(value, out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            count++;
            value = value[consumed..];
        }

        return count;
    }
}
