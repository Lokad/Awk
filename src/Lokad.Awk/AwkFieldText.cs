namespace Lokad.Awk;

internal enum AwkFieldTextKind
{
    Empty,
    Owned,
    RecordSlice
}

internal readonly struct AwkFieldText
{
    private const int OwnedTextStart = -1;
    private const int PendingScalarStart = -2;
    private readonly AwkText _ownedText;
    private readonly int _start;
    private readonly int _length;

    private AwkFieldText(AwkText ownedText, int start, int length)
    {
        _ownedText = ownedText;
        _start = start;
        _length = length;
    }

    public static AwkFieldText Empty => default;

    // A fixed-field print formats the captured scalar after resolving OFS and ORS.
    internal static AwkFieldText PendingScalar => new(default, PendingScalarStart, 0);
    internal bool NeedsFormatting => _start == PendingScalarStart;

    public static AwkFieldText FromOwned(AwkText text)
    {
        var length = text.ByteLength;
        return length == 0 ? Empty : new AwkFieldText(text, OwnedTextStart, length);
    }

    public static AwkFieldText FromRecordSlice(int start, int length) =>
        length == 0 ? Empty : new AwkFieldText(default, start, length);

    public int ByteLength() => _length;

    internal ReadOnlySpan<byte> AsSpan(ReadOnlySpan<byte> recordBytes, Action? checkpoint = null) =>
        _start switch
        {
            OwnedTextStart => _ownedText.AsSpan(checkpoint),
            PendingScalarStart => throw new InvalidOperationException("A print field has not been formatted."),
            _ => recordBytes.Slice(_start, _length)
        };
}
