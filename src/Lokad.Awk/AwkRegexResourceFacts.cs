using System.Text;

namespace Lokad.Awk;

internal static class AwkRegexResourceFacts
{
    public static void ThrowIfPatternExceedsLimits(
        string pattern,
        long maxPatternBytes,
        int maxGroups)
    {
        if (Encoding.UTF8.GetByteCount(pattern) > maxPatternBytes)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexPatternLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        var groups = CountGroupConstructs(pattern);
        if (groups > maxGroups)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexGroupLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }
    }

    public static long EstimateCacheBytes(string pattern)
    {
        var patternBytes = Encoding.UTF8.GetByteCount(pattern);
        var groupBytes = AwkRuntimeMemory.CheckedMultiply(CountGroupConstructs(pattern), 64);
        return AwkRuntimeMemory.CheckedAdd(
            1_024,
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedMultiply(patternBytes, 4), groupBytes));
    }

    private static int CountGroupConstructs(string pattern)
    {
        var groups = 0;
        var escaped = false;
        var inCharacterClass = false;
        for (var index = 0; index < pattern.Length; index++)
        {
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

        return groups;
    }
}
