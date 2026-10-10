using System.Text;

namespace Lokad.Awk;

internal enum AwkRegexEscapeWarningKind
{
    None,
    UnknownOperator,
    PlainDigit
}

internal readonly record struct AwkRegexEscapeWarning(AwkRegexEscapeWarningKind Kind, Rune Character = default)
{
    public string Message => Kind switch
    {
        AwkRegexEscapeWarningKind.UnknownOperator => AwkMessages.FormatRegexEscapeWarning(Character),
        AwkRegexEscapeWarningKind.PlainDigit => AwkMessages.FormatRegexDigitEscapeWarning(Character),
        _ => string.Empty
    };

    // Match GNU's first UTF-8 byte history while rendering complete valid scalars.
    public byte HistoryKey => Character.Value switch
    {
        <= 0x7F => (byte)Character.Value,
        <= 0x7FF => (byte)(0xC0 | (Character.Value >> 6)),
        <= 0xFFFF => (byte)(0xE0 | (Character.Value >> 12)),
        _ => (byte)(0xF0 | (Character.Value >> 18))
    };
}

internal sealed class AwkRegexEscapeWarnings
{
    private ulong _first;
    private ulong _second;
    private ulong _third;
    private ulong _fourth;

    public bool HasReported(AwkRegexEscapeWarning warning)
    {
        if (warning.Kind == AwkRegexEscapeWarningKind.None)
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

    public void Record(AwkRegexEscapeWarning warning)
    {
        if (warning.Kind == AwkRegexEscapeWarningKind.None)
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
