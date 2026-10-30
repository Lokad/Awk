namespace Lokad.Awk;

// This validates Awk's named-class syntax before it reaches a backend whose
// ordinary bracket grammar would otherwise accept an unknown POSIX class name.
internal static class AwkRegexBracketClasses
{
    public static void Validate(AwkText pattern, Action? checkpoint = null, AwkMemoryLease? lease = null)
    {
        if (AwkRegexConstruction.IndexOf(pattern.AsSpan(checkpoint), "[:"u8, 0, checkpoint) < 0)
            return;
        var inBracket = false;
        var firstItem = -1;
        var whitespaceMode = false;
        var groupDepth = 0;
        byte[]? parentModes = null;
        long modeBytes = 0;
        long nextCheckpoint = 0;
        try
        {
            for (var index = 0; index < pattern.ByteLength; index++)
            {
                AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                var value = pattern.AsSpan()[index];
                if (value == (byte)'\\')
                {
                    index++;
                    continue;
                }
                if (inBracket)
                {
                    if (value == (byte)']' && index != firstItem)
                    {
                        inBracket = false;
                        continue;
                    }
                    if (value != (byte)'[' || index + 1 >= pattern.ByteLength)
                        continue;
                    var marker = pattern.AsSpan()[index + 1];
                    if (marker is not ((byte)':' or (byte)'=' or (byte)'.'))
                        continue;
                    var closing = AwkRegexConstruction.TokenEnd(pattern.AsSpan(), index, checkpoint);
                    if (marker == (byte)':')
                    {
                        if (closing < 0 || !IsClassName(pattern.AsSpan().Slice(index + 2, closing - index - 2)))
                            throw new ArgumentException("Invalid POSIX character class.", nameof(pattern));
                    }
                    if (closing >= 0)
                        index = closing + 1;
                    continue;
                }
                if (whitespaceMode && value == (byte)'#')
                {
                    var end = AwkRegexConstruction.IndexOf(pattern.AsSpan(), (byte)'\n', index + 1, checkpoint);
                    if (end < 0)
                        break;
                    index = end;
                    continue;
                }
                if (value == (byte)'[')
                {
                    inBracket = true;
                    firstItem = index + 1;
                    if (firstItem < pattern.ByteLength && pattern.AsSpan()[firstItem] == (byte)'^')
                        firstItem++;
                }
                else if (value == (byte)'(')
                {
                    if (index + 2 < pattern.ByteLength && pattern.AsSpan()[index + 1] == (byte)'?' && pattern.AsSpan()[index + 2] == (byte)'#')
                    {
                        var end = AwkRegexConstruction.IndexOf(pattern.AsSpan(), (byte)')', index + 3, checkpoint);
                        if (end < 0)
                            break; // The backend reports the malformed inherited comment.
                        index = end;
                        continue;
                    }
                    if (TryReadOptions(pattern, index, whitespaceMode, out var nextMode, out var endOptions,
                            checkpoint))
                    {
                        if (pattern.AsSpan()[endOptions] == (byte)':')
                            PushMode();
                        whitespaceMode = nextMode;
                        index = endOptions;
                    }
                    else
                        PushMode();
                }
                else if (value == (byte)')' && groupDepth > 0)
                {
                    groupDepth--;
                    whitespaceMode = parentModes is not null &&
                        (parentModes[groupDepth >> 3] & (1 << (groupDepth & 7))) != 0;
                }
            }
            checkpoint?.Invoke();
        }
        finally
        {
            if (modeBytes != 0)
                lease?.Release(modeBytes);
        }

        void PushMode()
        {
            if (whitespaceMode && parentModes is null)
            {
                var length = (int)(((long)pattern.ByteLength + 7) / 8);
                var bytes = AwkRuntimeMemory.EstimateRegexBracketModeArrayBytes(length);
                lease?.ReserveAdditional(bytes);
                modeBytes = lease is null ? 0 : bytes;
                checkpoint?.Invoke();
                parentModes = new byte[length];
                checkpoint?.Invoke();
            }
            if (parentModes is not null)
            {
                var mask = (byte)(1 << (groupDepth & 7));
                if (whitespaceMode)
                    parentModes[groupDepth >> 3] |= mask;
                else
                    parentModes[groupDepth >> 3] &= (byte)~mask;
            }
            groupDepth++;
        }
    }

    private static bool IsClassName(ReadOnlySpan<byte> name) =>
        name.SequenceEqual("alnum"u8) ||
        name.SequenceEqual("alpha"u8) ||
        name.SequenceEqual("blank"u8) ||
        name.SequenceEqual("cntrl"u8) ||
        name.SequenceEqual("digit"u8) ||
        name.SequenceEqual("graph"u8) ||
        name.SequenceEqual("lower"u8) ||
        name.SequenceEqual("print"u8) ||
        name.SequenceEqual("punct"u8) ||
        name.SequenceEqual("space"u8) ||
        name.SequenceEqual("upper"u8) ||
        name.SequenceEqual("xdigit"u8);

    private static bool TryReadOptions(AwkText pattern, int start, bool currentMode, out bool nextMode,
        out int end, Action? checkpoint)
    {
        nextMode = currentMode;
        end = start;
        if (start + 2 >= pattern.ByteLength || pattern.AsSpan()[start + 1] != (byte)'?')
            return false;
        var enabled = true;
        var hasOption = false;
        long nextCheckpoint = start;
        for (var index = start + 2; index < pattern.ByteLength; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
            var value = pattern.AsSpan()[index];
            if (value == (byte)'-' && enabled)
            {
                enabled = false;
                continue;
            }
            if (value is (byte)'i' or (byte)'m' or (byte)'n' or (byte)'s' or (byte)'x')
            {
                hasOption = true;
                if (value == (byte)'x')
                    nextMode = enabled;
                continue;
            }
            if (hasOption && value is (byte)')' or (byte)':')
            {
                end = index;
                return true;
            }
            return false;
        }
        return false;
    }
}
