using System.Text;

namespace Lokad.Awk;

internal enum AwkStringEscapeWarningKind
{
    None,
    Unknown,
    MissingHexDigits,
    MissingUnicodeDigits,
    InvalidUnicodeScalar
}

internal readonly record struct AwkStringEscapeWarning(AwkStringEscapeWarningKind Kind, Rune Character = default)
{
    public string Message => Kind switch
    {
        AwkStringEscapeWarningKind.Unknown => AwkMessages.FormatStringEscapeWarning(Character),
        AwkStringEscapeWarningKind.MissingHexDigits => AwkMessages.FormatStringHexEscapeWarning(),
        AwkStringEscapeWarningKind.MissingUnicodeDigits => AwkMessages.FormatStringUnicodeMissingDigitsWarning(),
        AwkStringEscapeWarningKind.InvalidUnicodeScalar => AwkMessages.FormatStringUnicodeInvalidScalarWarning(),
        _ => string.Empty
    };

    // GNU awk remembers the first escaped UTF-8 byte. Keep the key byte-sized,
    // but render the entire scalar so diagnostics remain valid UTF-8.
    public byte HistoryKey => Character.Value switch
    {
        <= 0x7F => (byte)Character.Value,
        <= 0x7FF => (byte)(0xC0 | (Character.Value >> 6)),
        <= 0xFFFF => (byte)(0xE0 | (Character.Value >> 12)),
        _ => (byte)(0xF0 | (Character.Value >> 18))
    };
}

internal sealed class AwkStringEscapeWarnings
{
    // Fixed storage: no allocation or unbounded growth per warning/character.
    private ulong _first;
    private ulong _second;
    private ulong _third;
    private ulong _fourth;

    public bool HasReported(AwkStringEscapeWarning warning)
    {
        if (warning.Kind != AwkStringEscapeWarningKind.Unknown)
            return false;
        var key = warning.HistoryKey;
        var bits = (key >> 6) switch
        {
            0 => _first,
            1 => _second,
            2 => _third,
            _ => _fourth
        };
        return (bits & (1UL << (key & 63))) != 0;
    }

    public void Record(AwkStringEscapeWarning warning)
    {
        if (warning.Kind != AwkStringEscapeWarningKind.Unknown)
            return;
        var key = warning.HistoryKey;
        var mask = 1UL << (key & 63);
        switch (key >> 6)
        {
            case 0: _first |= mask; break;
            case 1: _second |= mask; break;
            case 2: _third |= mask; break;
            default: _fourth |= mask; break;
        }
    }
}
