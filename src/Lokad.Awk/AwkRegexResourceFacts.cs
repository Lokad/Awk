namespace Lokad.Awk;

internal static class AwkRegexResourceFacts
{
    public static void ThrowIfPatternExceedsLimits(
        string pattern,
        long maxPatternBytes,
        int maxGroups,
        Action? checkpoint = null)
    {
        if (AwkRegexConstruction.Utf8ByteCount(pattern, checkpoint) > maxPatternBytes)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexPatternLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        var groups = CountGroupConstructs(pattern, checkpoint);
        if (groups > maxGroups)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexGroupLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }
    }

    public static long EstimateCacheBytes(string pattern, Action? checkpoint = null)
    {
        var patternBytes = AwkRegexConstruction.Utf8ByteCount(pattern, checkpoint);
        var groupBytes = AwkRuntimeMemory.CheckedMultiply(CountGroupConstructs(pattern, checkpoint), 64);
        return AwkRuntimeMemory.CheckedAdd(
            1_024,
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedMultiply(patternBytes, 4), groupBytes));
    }

    private static int CountGroupConstructs(string pattern, Action? checkpoint)
    {
        var groups = 0;
        var escaped = false;
        var inCharacterClass = false;
        long nextCheckpoint = 0;
        for (var index = 0; index < pattern.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
            var ch = pattern[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == '[')
            {
                inCharacterClass = true;
                continue;
            }

            if (ch == ']' && inCharacterClass)
            {
                inCharacterClass = false;
                continue;
            }

            if (ch == '(' && !inCharacterClass)
                groups = AwkRuntimeMemory.CheckedAdd(groups, 1);
        }

        checkpoint?.Invoke();
        return groups;
    }
}
