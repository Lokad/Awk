using System;
using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkStringRuntime
{
    private readonly AwkStringUnitMode _unitMode;
    private readonly Action<int>? _recordByteWork;

    public AwkStringRuntime(AwkStringUnitMode unitMode, Action<int>? recordByteWork = null)
    {
        _unitMode = unitMode;
        _recordByteWork = recordByteWork;
    }

    public int CountUnits(AwkText value)
    {
        if (_unitMode is not AwkStringUnitMode.Byte)
            _recordByteWork?.Invoke(value.ByteLength);
        return _unitMode is AwkStringUnitMode.Byte ? value.ByteLength : value.RuneLength;
    }

    public int IndexOfUnits(AwkText source, AwkText value)
    {
        static int CountRunes(ReadOnlySpan<byte> bytes)
        {
            var count = 0;
            while (!bytes.IsEmpty)
            {
                var status = Rune.DecodeFromUtf8(bytes, out _, out var consumed);
                if (status != OperationStatus.Done)
                    throw new InvalidOperationException("AwkText contains invalid UTF-8.");

                count++;
                bytes = bytes[consumed..];
            }

            return count;
        }

        _recordByteWork?.Invoke(source.ByteLength);
        var position = source.IndexOf(value);
        if (position < 0)
            return 0;

        return _unitMode is AwkStringUnitMode.Byte
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
        _recordByteWork?.Invoke(source.ByteLength);
        return _unitMode is AwkStringUnitMode.Byte
            ? source.CopyByteSlice(start, length, counters, governor)
            : source.CopyRuneSlice(start, length, counters, governor);
    }

}
