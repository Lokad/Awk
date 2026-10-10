using System.Buffers;
using System.Text;

namespace Lokad.Awk;

/// <summary>Invariant simple-case operations over valid UTF-8, without folded copies.</summary>
internal static class AwkCaseText
{
    public static int Compare(
        ReadOnlySpan<byte> left,
        ReadOnlySpan<byte> right,
        AwkStringUnitMode unitMode,
        Action<int>? recordByteWork)
    {
        var work = new ScanWork(recordByteWork);
        while (!left.IsEmpty && !right.IsEmpty)
        {
            work.Admit(8);
            var leftValue = ReadFolded(ref left, unitMode);
            var rightValue = ReadFolded(ref right, unitMode);
            if (leftValue != rightValue)
                return leftValue.CompareTo(rightValue);
        }
        return left.IsEmpty ? (right.IsEmpty ? 0 : -1) : 1;
    }

    public static int IndexOf(
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> value,
        AwkStringUnitMode unitMode,
        Action<int>? recordByteWork)
    {
        if (value.IsEmpty)
            return 0;

        var work = new ScanWork(recordByteWork);
        var position = 0;
        while (!source.IsEmpty)
        {
            var candidate = source;
            var remainingValue = value;
            while (!candidate.IsEmpty && !remainingValue.IsEmpty)
            {
                work.Admit(8);
                if (ReadFolded(ref candidate, unitMode) != ReadFolded(ref remainingValue, unitMode))
                    break;
                if (remainingValue.IsEmpty)
                    return position;
            }

            work.Admit(4);
            var before = source.Length;
            ReadFolded(ref source, unitMode);
            position += before - source.Length;
        }
        return -1;
    }

    private static int ReadFolded(ref ReadOnlySpan<byte> bytes, AwkStringUnitMode unitMode)
    {
        var first = bytes[0];
        if (first < 0x80 || unitMode is AwkStringUnitMode.Byte)
        {
            bytes = bytes[1..];
            return first is >= (byte)'A' and <= (byte)'Z' ? first + ('a' - 'A') : first;
        }

        if (Rune.DecodeFromUtf8(bytes, out var scalar, out var consumed) != OperationStatus.Done)
            throw new InvalidOperationException("AwkText contains invalid UTF-8.");
        bytes = bytes[consumed..];
        return Rune.ToLowerInvariant(scalar).Value;
    }

    private struct ScanWork(Action<int>? recordByteWork)
    {
        private int _remaining;

        public void Admit(int maximumBytes)
        {
            if (_remaining < maximumBytes)
            {
                recordByteWork?.Invoke(AwkExecutionGovernor.WorkChunkBytes);
                _remaining += AwkExecutionGovernor.WorkChunkBytes;
            }
            _remaining -= maximumBytes;
        }
    }
}
