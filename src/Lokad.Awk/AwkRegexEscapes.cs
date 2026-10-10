using System.Text;

namespace Lokad.Awk;

internal static class AwkRegexEscapes
{
    public static string Decode(string pattern, AwkCompatibilityMode mode, Action? checkpoint = null)
    {
        var slash = pattern.IndexOf('\\', StringComparison.Ordinal);
        if (slash < 0)
            return pattern;
        var builder = new StringBuilder(pattern.Length);
        builder.Append(pattern, 0, slash);
        Span<char> buffer = stackalloc char[2];
        var nextCheckpoint = slash;
        for (var index = slash; index < pattern.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = index + 1024;
            }
            if (pattern[index] != '\\' || index + 1 >= pattern.Length)
            {
                builder.Append(pattern[index]);
                continue;
            }
            var escaped = pattern[++index];
            var unicode = AwkEscapes.ReadUnicodeEscape(pattern, ref index, pattern.Length, mode, out var scalar);
            if (unicode != AwkUnicodeEscapeStatus.None)
            {
                if (unicode == AwkUnicodeEscapeStatus.InvalidScalar)
                    builder.Append(@"\?");
                else
                {
                    if (scalar.IsBmp && mode == AwkCompatibilityMode.Traditional && IsMetacharacter((char)scalar.Value))
                        builder.Append('\\');
                    var length = scalar.EncodeToUtf16(buffer);
                    builder.Append(buffer[..length]);
                }
                continue;
            }
            if (!(mode == AwkCompatibilityMode.Posix && escaped == 'x') &&
                AwkEscapes.TryReadNumericEscape(pattern, ref index, pattern.Length, out var numeric))
            {
                if (mode == AwkCompatibilityMode.Traditional && IsMetacharacter(numeric))
                    builder.Append('\\');
                builder.Append(numeric);
                continue;
            }
            if (escaped is 'a' or 'b' or 'f' or 'n' or 'r' or 't' or 'v' &&
                AwkEscapes.TryDecodeSimpleEscape(escaped, out var simple))
                builder.Append(simple);
            else if (escaped == 'y' && mode == AwkCompatibilityMode.GnuDefault)
                builder.Append(@"\b");
            else
                builder.Append('\\').Append(escaped);
        }
        return builder.ToString();
    }

    private static bool IsMetacharacter(char value) => ".*+(){}[]|?^$\\".Contains(value);

    public static bool HasUnicodeWarnings(string pattern, AwkCompatibilityMode mode, Action? checkpoint = null)
    {
        if (mode == AwkCompatibilityMode.Posix)
            return false;
        var nextCheckpoint = 0;
        for (var index = 0; index + 1 < pattern.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = index + 1024;
            }
            if (pattern[index] != '\\')
                continue;
            index++;
            var status = AwkEscapes.ReadUnicodeEscape(pattern, ref index, pattern.Length, mode, out _);
            if (status is AwkUnicodeEscapeStatus.MissingDigits or AwkUnicodeEscapeStatus.InvalidScalar)
                return true;
        }
        return false;
    }

    public static void ReportUnicodeWarnings(string pattern, AwkCompatibilityMode mode, Action<string> report,
        Action? checkpoint = null)
    {
        if (mode == AwkCompatibilityMode.Posix)
            return;
        var nextCheckpoint = 0;
        for (var index = 0; index + 1 < pattern.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = index + 1024;
            }
            if (pattern[index] != '\\')
                continue;
            index++;
            var status = AwkEscapes.ReadUnicodeEscape(pattern, ref index, pattern.Length, mode, out _);
            if (status == AwkUnicodeEscapeStatus.MissingDigits)
                report(AwkMessages.FormatStringUnicodeMissingDigitsWarning());
            else if (status == AwkUnicodeEscapeStatus.InvalidScalar)
                report(AwkMessages.FormatStringUnicodeInvalidScalarWarning());
        }
    }
}
