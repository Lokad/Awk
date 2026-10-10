using System.Text;

namespace Lokad.Awk;

internal static class AwkRegexEscapes
{
    public static string Decode(string pattern, AwkCompatibilityMode mode, Action? checkpoint = null)
    {
        var slash = AwkRegexConstruction.IndexOf(pattern, '\\', 0, checkpoint);
        if (slash < 0)
            return pattern;
        var builder = new StringBuilder(pattern.Length);
        AwkRegexConstruction.Append(builder, pattern, 0, slash, checkpoint);
        Span<char> buffer = stackalloc char[2];
        long nextCheckpoint = slash;
        for (var index = slash; index < pattern.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
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
        return AwkRegexConstruction.Finish(builder, checkpoint);
    }

    private static bool IsMetacharacter(char value) => ".*+(){}[]|?^$\\".Contains(value);

    public static bool HasNumericWarnings(string pattern, AwkCompatibilityMode mode, Action? checkpoint = null)
    {
        if (mode == AwkCompatibilityMode.Posix)
            return false;
        long nextCheckpoint = 0;
        for (var index = 0; index + 1 < pattern.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
            if (pattern[index] != '\\')
                continue;
            var escaped = pattern[++index];
            var status = AwkEscapes.ReadUnicodeEscape(pattern, ref index, pattern.Length, mode, out _);
            if (status is AwkUnicodeEscapeStatus.MissingDigits or AwkUnicodeEscapeStatus.InvalidScalar)
                return true;
            if (status == AwkUnicodeEscapeStatus.None && escaped == 'x' &&
                !AwkEscapes.TryReadNumericEscape(pattern, ref index, pattern.Length, out _))
                return true;
        }
        return false;
    }

    public static void ReportWarnings(string pattern, AwkCompatibilityMode mode,
        AwkRegexEscapeWarnings history, Action<AwkRegexEscapeWarning> reportOperator,
        Action<string>? reportUnicode, Action? checkpoint = null)
    {
        const string ereEscapes = "{}()|*+?.^$\\[]/-";
        const string gnuEscapes = "<>`'BywWsS";
        long nextCheckpoint = 0;
        for (var index = 0; index + 1 < pattern.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, checkpoint);
            if (pattern[index] != '\\')
                continue;
            var escaped = pattern[++index];
            var status = AwkEscapes.ReadUnicodeEscape(pattern, ref index, pattern.Length, mode, out _);
            if (status != AwkUnicodeEscapeStatus.None)
            {
                if (status == AwkUnicodeEscapeStatus.MissingDigits)
                    reportUnicode?.Invoke(AwkMessages.FormatStringUnicodeMissingDigitsWarning());
                else if (status == AwkUnicodeEscapeStatus.InvalidScalar)
                    reportUnicode?.Invoke(AwkMessages.FormatStringUnicodeInvalidScalarWarning());
                continue;
            }
            if (escaped == 'x' && mode != AwkCompatibilityMode.Posix)
            {
                if (!AwkEscapes.TryReadNumericEscape(pattern, ref index, pattern.Length, out _))
                    reportUnicode?.Invoke(AwkMessages.FormatStringHexEscapeWarning());
                continue;
            }
            if (escaped is 'u' or 'x' && mode == AwkCompatibilityMode.Posix ||
                AwkEscapes.TryReadNumericEscape(pattern, ref index, pattern.Length, out _) ||
                escaped is 'a' or 'b' or 'f' or 'n' or 'r' or 't' or 'v' or 'x' ||
                ereEscapes.Contains(escaped) ||
                mode == AwkCompatibilityMode.GnuDefault && gnuEscapes.Contains(escaped))
                continue;

            _ = Rune.DecodeFromUtf16(pattern.AsSpan(index), out var character, out var consumed);
            if (consumed > 1)
                index += consumed - 1;
            var warning = new AwkRegexEscapeWarning(escaped is '8' or '9'
                ? AwkRegexEscapeWarningKind.PlainDigit : AwkRegexEscapeWarningKind.UnknownOperator, character);
            if (history.HasReported(warning))
                continue;
            checkpoint?.Invoke();
            reportOperator(warning);
            history.Record(warning);
        }
        checkpoint?.Invoke();
    }

    public static void ReportUnicodeWarnings(string pattern, AwkCompatibilityMode mode, Action<string> report,
        Action? checkpoint = null) =>
        AwkEscapes.ReportUnicodeWarnings(pattern, mode, report, checkpoint);
}
