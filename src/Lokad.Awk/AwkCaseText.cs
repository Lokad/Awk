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
        Action? checkpoint)
    {
        var work = new ScanCheckpoint(checkpoint);
        while (!left.IsEmpty && !right.IsEmpty)
        {
            work.Check();
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
        Action? checkpoint)
    {
        if (value.IsEmpty)
            return 0;

        var work = new ScanCheckpoint(checkpoint);
        var position = 0;
        while (!source.IsEmpty)
        {
            var candidate = source;
            var remainingValue = value;
            while (!candidate.IsEmpty && !remainingValue.IsEmpty)
            {
                work.Check();
                if (ReadFolded(ref candidate, unitMode) != ReadFolded(ref remainingValue, unitMode))
                    break;
                if (remainingValue.IsEmpty)
                    return position;
            }

            work.Check();
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

    private struct ScanCheckpoint(Action? checkpoint)
    {
        private int _characters;

        public void Check()
        {
            if ((_characters++ & 255) == 0)
                checkpoint?.Invoke();
        }
    }
}
