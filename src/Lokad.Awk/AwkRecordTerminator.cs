namespace Lokad.Awk;

internal readonly record struct AwkRecordTerminator
{
    private readonly AwkText? _text;

    public AwkRecordTerminator(AwkText text)
    {
        _text = text;
    }

    public static AwkRecordTerminator Empty => default;

    public static AwkRecordTerminator NewLine { get; } =
        new(AwkText.FromTrustedOwnedBytes([(byte)'\n']));

    public static AwkRecordTerminator FromText(AwkText text) =>
        text.ByteLength == 0 ? Empty : new AwkRecordTerminator(text);

    public AwkText ByteText => _text ?? AwkText.Empty;
}
