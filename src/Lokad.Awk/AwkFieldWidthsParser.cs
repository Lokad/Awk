namespace Lokad.Awk;

internal static class AwkFieldWidthsParser
{
    // Count/validate without allocating, then fill the exact-size buffer after preflight.
    public static int Parse(ReadOnlySpan<byte> text, List<AwkFieldWidth>? destination, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var nul = text.IndexOf((byte)0);
        checkpoint?.Invoke();
        if (nul >= 0)
            text = text[..nul];
        var position = 0;
        var count = 0;
        long nextCheckpoint = 0;
        while (position < text.Length)
        {
            CheckProgress(position, checkpoint, ref nextCheckpoint);
            while (position < text.Length && IsBlank(text[position]))
            {
                CheckProgress(position, checkpoint, ref nextCheckpoint);
                position++;
            }
            if (position == text.Length)
                break;

            var skip = 0;
            var width = int.MaxValue;
            var remainder = text[position] == (byte)'*';
            if (!remainder)
            {
                width = ReadPositiveInteger(text, ref position, checkpoint, ref nextCheckpoint);
                if (position < text.Length && text[position] == (byte)':')
                {
                    skip = width;
                    position++;
                    if (position == text.Length || IsBlank(text[position]))
                        throw Invalid();
                    if (text[position] != (byte)'*')
                        width = ReadPositiveInteger(text, ref position, checkpoint, ref nextCheckpoint);
                    else
                    {
                        width = int.MaxValue;
                        remainder = true;
                    }
                }
            }

            if (remainder)
            {
                position++;
                while (position < text.Length && IsBlank(text[position]))
                {
                    CheckProgress(position, checkpoint, ref nextCheckpoint);
                    position++;
                }
                if (position != text.Length)
                    throw new AwkRuntimeException(AwkMessages.ErrorFieldWidthsStarNotLast);
            }
            else if (position < text.Length && !IsBlank(text[position]))
                throw Invalid();

            destination?.Add(new(new AwkFieldSkip(skip), new AwkFieldWidthLength(width)));
            count++;
        }
        checkpoint?.Invoke();
        return count;
    }

    private static int ReadPositiveInteger(ReadOnlySpan<byte> text, ref int position, Action? checkpoint, ref long nextCheckpoint)
    {
        // strtoul accepts leading ASCII whitespace; only space/tab delimit fields.
        while (position < text.Length && text[position] is (byte)' ' or (byte)'\t' or
            (byte)'\r' or (byte)'\n' or (byte)'\v' or (byte)'\f')
        {
            CheckProgress(position, checkpoint, ref nextCheckpoint);
            position++;
        }
        if (position < text.Length && text[position] == (byte)'+')
            position++;
        var start = position;
        var number = 0UL;
        while (position < text.Length && text[position] is >= (byte)'0' and <= (byte)'9')
        {
            CheckProgress(position, checkpoint, ref nextCheckpoint);
            number = number * 10 + (uint)(text[position++] - (byte)'0');
            if (number > uint.MaxValue)
                throw Invalid();
        }
        if (position == start || number == 0)
            throw Invalid();
        return (int)Math.Min(number, int.MaxValue);
    }

    private static void CheckProgress(int position, Action? checkpoint, ref long nextCheckpoint)
    {
        if (position < nextCheckpoint)
            return;
        checkpoint?.Invoke();
        nextCheckpoint = (long)position + AwkRuntimeLimits.CancellationCheckBytes;
    }

    private static bool IsBlank(byte value) => value is (byte)' ' or (byte)'\t';

    private static AwkRuntimeException Invalid() => new(AwkMessages.ErrorInvalidFieldWidths);
}
