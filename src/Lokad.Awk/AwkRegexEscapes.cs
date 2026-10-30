using System.Text;

namespace Lokad.Awk;

internal static class AwkRegexEscapes
{
    public static AwkText Decode(AwkText pattern, AwkCompatibilityMode mode, Action? checkpoint,
        AwkMemoryAccounting memory, AwkMemoryLease results)
    {
        var bytes = pattern.AsSpan(checkpoint);
        var slash = AwkRegexConstruction.IndexOf(bytes, (byte)'\\', 0, checkpoint);
        if (slash < 0) return pattern;
        using var builder = new AwkUtf8Builder(memory, bytes.Length);
        builder.Append(bytes[..slash], checkpoint);
        Span<byte> encoded = stackalloc byte[4];
        long next = slash;
        for (var index = slash; index < bytes.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref next, checkpoint);
            if (bytes[index] != (byte)'\\' || index + 1 >= bytes.Length)
            {
                builder.Append(bytes[index]);
                continue;
            }
            var escaped = bytes[++index];
            if (ReadScalar(bytes, ref index, mode, out var scalar, out var unicode))
            {
                if (unicode == AwkUnicodeEscapeStatus.InvalidScalar) builder.Append(@"\?"u8);
                else
                {
                    if (scalar.IsBmp && mode == AwkCompatibilityMode.Traditional &&
                        ".*+(){}[]|?^$\\"u8.Contains((byte)scalar.Value) && scalar.Value <= 0x7F)
                        builder.Append((byte)'\\');
                    builder.Append(encoded[..scalar.EncodeToUtf8(encoded)]);
                }
                continue;
            }
            if (escaped is (byte)'a' or (byte)'b' or (byte)'f' or (byte)'n' or (byte)'r' or (byte)'t' or (byte)'v' &&
                AwkEscapes.TryDecodeSimpleEscape((char)escaped, out var simple)) builder.Append((byte)simple);
            else if (escaped == (byte)'y' && mode == AwkCompatibilityMode.GnuDefault) builder.Append(@"\b"u8);
            else builder.Append((byte)'\\').Append(escaped);
        }
        return AwkRegexConstruction.Finish(builder, results, checkpoint);
    }

    // Numeric escape spellings are ASCII and at most sixteen bytes for one UTF-8 scalar.
    // Reuse source escape rules in bounded stack scratch; never decode a pattern or allocate a string.
    private static bool ReadScalar(ReadOnlySpan<byte> pattern, ref int index, AwkCompatibilityMode mode,
        out Rune scalar, out AwkUnicodeEscapeStatus unicode)
    {
        Span<char> spelling = stackalloc char[16];
        var count = Math.Min(spelling.Length, pattern.Length - index);
        for (var offset = 0; offset < count; offset++) spelling[offset] = (char)pattern[index + offset];
        var consumed = 0;
        unicode = AwkEscapes.ReadUnicodeEscape(spelling[..count], ref consumed, count, mode, out scalar);
        if (unicode == AwkUnicodeEscapeStatus.None &&
            !AwkEscapes.TryReadNumericScalar(spelling[..count], ref consumed, count, mode, out scalar)) return false;
        index += consumed;
        return true;
    }

    public static bool HasNumericWarnings(AwkText pattern, AwkCompatibilityMode mode, Action? checkpoint = null)
    {
        if (mode == AwkCompatibilityMode.Posix) return false;
        var bytes = pattern.AsSpan(checkpoint);
        long next = 0;
        for (var index = 0; index + 1 < bytes.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref next, checkpoint);
            if (bytes[index] != (byte)'\\') continue;
            var escaped = bytes[++index];
            var numeric = ReadScalar(bytes, ref index, mode, out _, out var unicode);
            if (unicode is AwkUnicodeEscapeStatus.MissingDigits or AwkUnicodeEscapeStatus.InvalidScalar ||
                escaped == (byte)'x' && !numeric) return true;
        }
        checkpoint?.Invoke();
        return false;
    }

    public static void ReportWarnings(AwkText pattern, AwkCompatibilityMode mode,
        AwkRegexEscapeWarnings history, Action<AwkRegexEscapeWarning> reportOperator,
        Action<string>? reportUnicode, Action? checkpoint = null)
    {
        var bytes = pattern.AsSpan(checkpoint);
        long next = 0;
        for (var index = 0; index + 1 < bytes.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref next, checkpoint);
            if (bytes[index] != (byte)'\\') continue;
            var escaped = bytes[++index];
            var numeric = ReadScalar(bytes, ref index, mode, out _, out var unicode);
            if (unicode != AwkUnicodeEscapeStatus.None)
            {
                if (unicode == AwkUnicodeEscapeStatus.MissingDigits)
                    reportUnicode?.Invoke(AwkMessages.FormatStringUnicodeMissingDigitsWarning());
                else if (unicode == AwkUnicodeEscapeStatus.InvalidScalar)
                    reportUnicode?.Invoke(AwkMessages.FormatStringUnicodeInvalidScalarWarning());
                continue;
            }
            if (escaped == (byte)'x' && mode != AwkCompatibilityMode.Posix)
            {
                if (!numeric) reportUnicode?.Invoke(AwkMessages.FormatStringHexEscapeWarning());
                continue;
            }
            if (numeric || mode == AwkCompatibilityMode.Posix && escaped is (byte)'u' or (byte)'x' ||
                escaped is (byte)'a' or (byte)'b' or (byte)'f' or (byte)'n' or (byte)'r' or (byte)'t' or (byte)'v' ||
                "{}()|*+?.^$\\[]/-"u8.Contains(escaped) ||
                mode == AwkCompatibilityMode.GnuDefault && "<>`'BywWsS"u8.Contains(escaped)) continue;
            _ = Rune.DecodeFromUtf8(bytes[index..], out var character, out var consumed);
            index += consumed - 1;
            var warning = new AwkRegexEscapeWarning(escaped is (byte)'8' or (byte)'9'
                ? AwkRegexEscapeWarningKind.PlainDigit : AwkRegexEscapeWarningKind.UnknownOperator, character);
            if (history.HasReported(warning)) continue;
            checkpoint?.Invoke();
            reportOperator(warning);
            history.Record(warning);
        }
        checkpoint?.Invoke();
    }

    public static void ReportWarnings(ReadOnlySpan<char> pattern, AwkCompatibilityMode mode,
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

            _ = Rune.DecodeFromUtf16(pattern[index..], out var character, out var consumed);
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
