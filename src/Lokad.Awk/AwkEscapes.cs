using System.Text;

namespace Lokad.Awk;

internal static class AwkEscapes
{
    public static string DecodeSourceString(string text, AwkCompatibilityMode compatibilityMode,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var slash = text.IndexOf('\\', StringComparison.Ordinal);
        checkpoint?.Invoke();
        if (slash < 0)
            return text;

        var builder = new StringBuilder(text.Length);
        builder.Append(text, 0, slash);
        Span<char> scalarBuffer = stackalloc char[2];
        long nextCheckpoint = slash;

        for (var index = slash; index < text.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)index + 1024;
            }
            var current = text[index];
            if (current != '\\' || index + 1 >= text.Length)
            {
                builder.Append(current);
                continue;
            }

            index++;
            var escaped = text[index];
            if (ReadUnicodeEscape(text, ref index, text.Length, compatibilityMode, out var scalar) != AwkUnicodeEscapeStatus.None)
            {
                var length = scalar.EncodeToUtf16(scalarBuffer);
                builder.Append(scalarBuffer[..length]);
                continue;
            }

            if (TryReadNumericScalar(text, ref index, text.Length, compatibilityMode, out scalar,
                skipLineContinuations: true, checkpoint))
            {
                var length = scalar.EncodeToUtf16(scalarBuffer);
                builder.Append(scalarBuffer[..length]);
                continue;
            }

            if (AwkLineContinuations.TrySkipAfterBackslash(text, ref index, text.Length))
                continue;

            builder.Append(TryDecodeSimpleEscape(escaped, out var decoded) ? decoded : escaped);
        }

        checkpoint?.Invoke();
        var decodedText = builder.ToString();
        checkpoint?.Invoke();
        return decodedText;
    }

    public static void ReportStringWarnings(ReadOnlySpan<char> text, AwkCompatibilityMode mode,
        AwkStringEscapeWarnings history, Action<AwkStringEscapeWarning> report, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        long nextCheckpoint = 0;
        for (var index = 0; index + 1 < text.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)index + 1024;
            }
            if (text[index] != '\\')
                continue;
            index++;
            var escaped = text[index];
            var unicode = ReadUnicodeEscape(text, ref index, text.Length, mode, out _);
            if (unicode != AwkUnicodeEscapeStatus.None)
            {
                if (unicode != AwkUnicodeEscapeStatus.Valid)
                    report(new(unicode == AwkUnicodeEscapeStatus.MissingDigits
                        ? AwkStringEscapeWarningKind.MissingUnicodeDigits
                        : AwkStringEscapeWarningKind.InvalidUnicodeScalar));
                continue;
            }
            if (mode == AwkCompatibilityMode.Posix && escaped is 'u' or 'x')
                continue;
            if (TryReadNumericEscape(text, ref index, text.Length, out _))
                continue;
            if (escaped == 'x')
            {
                report(new(AwkStringEscapeWarningKind.MissingHexDigits));
                continue;
            }
            if (AwkLineContinuations.TrySkipAfterBackslash(text, ref index, text.Length) ||
                TryDecodeSimpleEscape(escaped, out _))
                continue;
            var status = Rune.DecodeFromUtf16(text[index..], out var scalar, out var consumed);
            if (status != System.Buffers.OperationStatus.Done)
                scalar = Rune.ReplacementChar;
            index += Math.Max(1, consumed) - 1;
            var warning = new AwkStringEscapeWarning(AwkStringEscapeWarningKind.Unknown, scalar);
            if (!history.HasReported(warning))
            {
                report(warning);
                history.Record(warning);
            }
        }
        checkpoint?.Invoke();
    }

    public static void ReportUnicodeWarnings(string text, AwkCompatibilityMode mode, Action<string> report,
        Action? checkpoint = null)
    {
        if (mode == AwkCompatibilityMode.Posix)
            return;
        long nextCheckpoint = 0;
        for (var index = 0; index + 1 < text.Length; index++)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)index + 1024;
            }
            if (text[index] != '\\')
                continue;
            index++;
            var status = ReadUnicodeEscape(text, ref index, text.Length, mode, out _);
            if (status == AwkUnicodeEscapeStatus.MissingDigits)
                report(AwkMessages.FormatStringUnicodeMissingDigitsWarning());
            else if (status == AwkUnicodeEscapeStatus.InvalidScalar)
                report(AwkMessages.FormatStringUnicodeInvalidScalarWarning());
        }
    }

    public static AwkUnicodeEscapeStatus ReadUnicodeEscape(
        ReadOnlySpan<char> text,
        ref int index,
        int endExclusive,
        AwkCompatibilityMode compatibilityMode,
        out Rune value)
    {
        value = default;
        if (index >= endExclusive || text[index] != 'u' || compatibilityMode == AwkCompatibilityMode.Posix)
            return AwkUnicodeEscapeStatus.None;

        if (index + 1 >= endExclusive || !AwkCharacterFacts.IsHexDigit(text[index + 1]))
        {
            value = new Rune('u');
            return AwkUnicodeEscapeStatus.MissingDigits;
        }

        uint numeric = 0;
        var digits = 0;
        while (digits < 8 && index + 1 < endExclusive && AwkCharacterFacts.IsHexDigit(text[index + 1]))
        {
            index++;
            digits++;
            numeric = (numeric << 4) | (uint)AwkCharacterFacts.HexValue(text[index]);
        }

        if (numeric <= 0x10FFFF && Rune.TryCreate((int)numeric, out value))
            return AwkUnicodeEscapeStatus.Valid;

        value = new Rune('?');
        return AwkUnicodeEscapeStatus.InvalidScalar;
    }

    public static bool TryReadNumericEscape(
        ReadOnlySpan<char> text,
        ref int index,
        int endExclusive,
        out char value)
    {
        value = default;
        if (index >= endExclusive)
            return false;

        var escaped = text[index];
        if (AwkCharacterFacts.IsOctalDigit(escaped))
        {
            var numeric = escaped - '0';
            var digits = 1;
            while (digits < 3 && index + 1 < endExclusive && AwkCharacterFacts.IsOctalDigit(text[index + 1]))
            {
                index++;
                digits++;
                numeric = (numeric * 8) + (text[index] - '0');
            }

            value = (char)(numeric & 0xFF);
            return true;
        }

        if (escaped == 'x' && index + 1 < endExclusive && AwkCharacterFacts.IsHexDigit(text[index + 1]))
        {
            var numeric = 0;
            var digits = 0;
            while (digits < 2 && index + 1 < endExclusive && AwkCharacterFacts.IsHexDigit(text[index + 1]))
            {
                index++;
                digits++;
                numeric = (numeric * 16) + AwkCharacterFacts.HexValue(text[index]);
            }

            value = (char)(numeric & 0xFF);
            return true;
        }

        return false;
    }

    // Numeric escapes describe bytes. Aggregate only complete UTF-8 scalars and
    // leave failed lookahead unconsumed. Isolated/invalid bytes retain the existing
    // U+00xx mapping because AwkText cannot represent malformed UTF-8.
    public static bool TryReadNumericScalar(
        ReadOnlySpan<char> text,
        ref int index,
        int endExclusive,
        AwkCompatibilityMode compatibilityMode,
        out Rune value,
        bool skipLineContinuations = false,
        Action? checkpoint = null)
    {
        value = default;
        if (index >= endExclusive ||
            compatibilityMode == AwkCompatibilityMode.Posix && text[index] == 'x' ||
            !TryReadNumericEscape(text, ref index, endExclusive, out var first))
            return false;

        value = new Rune(first);
        if (first is < (char)0xC2 or > (char)0xF4)
            return true;

        var byteCount = first < 0xE0 ? 2 : first < 0xF0 ? 3 : 4;
        Span<byte> bytes = stackalloc byte[4];
        bytes[0] = (byte)first;
        var last = index;
        long nextCheckpoint = (long)last + 1024;
        for (var count = 1; count < byteCount; count++)
        {
            var next = last + 1;
            while (true)
            {
                if (next >= nextCheckpoint)
                {
                    checkpoint?.Invoke();
                    nextCheckpoint = (long)next + 1024;
                }
                if (next + 1 >= endExclusive || text[next] != '\\')
                    return true;
                var escapedIndex = next + 1;
                if (skipLineContinuations &&
                    AwkLineContinuations.TrySkipAfterBackslash(text, ref escapedIndex, endExclusive))
                {
                    next = escapedIndex + 1;
                    continue;
                }
                if (compatibilityMode == AwkCompatibilityMode.Posix && text[escapedIndex] == 'x' ||
                    !TryReadNumericEscape(text, ref escapedIndex, endExclusive, out var continuation) ||
                    continuation is < (char)0x80 or > (char)0xBF)
                    return true;
                bytes[count] = (byte)continuation;
                last = escapedIndex;
                break;
            }
        }
        if (Rune.DecodeFromUtf8(bytes[..byteCount], out var scalar, out _) == System.Buffers.OperationStatus.Done)
        {
            value = scalar;
            index = last;
        }
        return true;
    }

    public static bool TryDecodeSimpleEscape(char escaped, out char decoded)
    {
        decoded = escaped switch
        {
            '\\' => '\\',
            '"' => '"',
            'a' => '\a',
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'v' => '\v',
            _ => default
        };

        return decoded != default;
    }

}

internal enum AwkUnicodeEscapeStatus
{
    None,
    Valid,
    MissingDigits,
    InvalidScalar
}
