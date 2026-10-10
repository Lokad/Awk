namespace Lokad.Awk;

// This validates Awk's named-class syntax before it reaches a backend whose
// ordinary bracket grammar would otherwise accept an unknown POSIX class name.
internal static class AwkRegexBracketClasses
{
    public static void Validate(string pattern, Action? checkpoint = null, AwkMemoryLease? lease = null)
    {
        if (AwkRegexConstruction.IndexOf(pattern, "[:", 0, checkpoint) < 0)
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
            for (var index = 0; index < pattern.Length; index++)
            {
                AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
                var value = pattern[index];
                if (value == '\\')
                {
                    index++;
                    continue;
                }
                if (inBracket)
                {
                    if (value == ']' && index != firstItem)
                    {
                        inBracket = false;
                        continue;
                    }
                    if (value != '[' || index + 1 >= pattern.Length)
                        continue;
                    var marker = pattern[index + 1];
                    if (marker is not (':' or '=' or '.'))
                        continue;
                    var delimiter = marker switch { ':' => ":]", '.' => ".]", _ => "=]" };
                    var closing = AwkRegexConstruction.IndexOf(pattern, delimiter, index + 2, checkpoint);
                    if (marker == ':')
                    {
                        if (closing < 0 || !IsClassName(pattern.AsSpan(index + 2, closing - index - 2)))
                            throw new ArgumentException("Invalid POSIX character class.", nameof(pattern));
                    }
                    if (closing >= 0)
                        index = closing + 1;
                    continue;
                }
                if (whitespaceMode && value == '#')
                {
                    var end = AwkRegexConstruction.IndexOf(pattern, '\n', index + 1, checkpoint);
                    if (end < 0)
                        break;
                    index = end;
                    continue;
                }
                if (value == '[')
                {
                    inBracket = true;
                    firstItem = index + 1;
                    if (firstItem < pattern.Length && pattern[firstItem] == '^')
                        firstItem++;
                }
                else if (value == '(')
                {
                    if (index + 2 < pattern.Length && pattern[index + 1] == '?' && pattern[index + 2] == '#')
                    {
                        var end = AwkRegexConstruction.IndexOf(pattern, ')', index + 3, checkpoint);
                        if (end < 0)
                            break; // The backend reports the malformed inherited comment.
                        index = end;
                        continue;
                    }
                    if (TryReadOptions(pattern, index, whitespaceMode, out var nextMode, out var endOptions,
                            checkpoint))
                    {
                        if (pattern[endOptions] == ':')
                            PushMode();
                        whitespaceMode = nextMode;
                        index = endOptions;
                    }
                    else
                        PushMode();
                }
                else if (value == ')' && groupDepth > 0)
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
                var length = (int)(((long)pattern.Length + 7) / 8);
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

    private static bool IsClassName(ReadOnlySpan<char> name) => name is
        "alnum" or "alpha" or "blank" or "cntrl" or "digit" or "graph" or
        "lower" or "print" or "punct" or "space" or "upper" or "xdigit";

    private static bool TryReadOptions(string pattern, int start, bool currentMode, out bool nextMode,
        out int end, Action? checkpoint)
    {
        nextMode = currentMode;
        end = start;
        if (start + 2 >= pattern.Length || pattern[start + 1] != '?')
            return false;
        var enabled = true;
        var hasOption = false;
        long nextCheckpoint = start;
        for (var index = start + 2; index < pattern.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
            var value = pattern[index];
            if (value == '-' && enabled)
            {
                enabled = false;
                continue;
            }
            if (value is 'i' or 'm' or 'n' or 's' or 'x')
            {
                hasOption = true;
                if (value == 'x')
                    nextMode = enabled;
                continue;
            }
            if (hasOption && value is ')' or ':')
            {
                end = index;
                return true;
            }
            return false;
        }
        return false;
    }
}
