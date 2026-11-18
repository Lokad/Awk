namespace Lokad.Awk;

internal enum AwkFieldTextKind
{
    Empty,
    Owned,
    RecordSlice
}

internal readonly struct AwkFieldText
{
    private readonly AwkText _ownedText;
    private readonly int _start;
    private readonly int _length;

    private AwkFieldText(AwkFieldTextKind kind, AwkText ownedText, int start, int length)
    {
        Kind = kind;
        _ownedText = ownedText;
        _start = start;
        _length = length;
    }

    public static AwkFieldText Empty { get; } =
        new(AwkFieldTextKind.Empty, AwkText.Empty, 0, 0);

    public static AwkFieldText FromOwned(AwkText text)
    {
        return text.ByteLength == 0
            ? Empty
            : new AwkFieldText(AwkFieldTextKind.Owned, text, 0, text.ByteLength);
    }

    public static AwkFieldText FromRecordSlice(int start, int length) =>
        length == 0
            ? Empty
            : new AwkFieldText(AwkFieldTextKind.RecordSlice, AwkText.Empty, start, length);

    public AwkFieldTextKind Kind { get; }

    public int ByteLength() =>
        Kind switch
        {
            AwkFieldTextKind.Empty => 0,
            AwkFieldTextKind.Owned => _ownedText.ByteLength,
            AwkFieldTextKind.RecordSlice => _length,
            _ => 0
        };

    internal ReadOnlySpan<byte> AsSpan(ReadOnlySpan<byte> recordBytes, Action? checkpoint = null) =>
        Kind switch
        {
            AwkFieldTextKind.Owned => _ownedText.AsSpan(checkpoint),
            AwkFieldTextKind.RecordSlice => recordBytes.Slice(_start, _length),
            _ => ReadOnlySpan<byte>.Empty
        };
}
