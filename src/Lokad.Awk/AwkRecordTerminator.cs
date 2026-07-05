namespace Lokad.Awk;

internal readonly record struct AwkRecordTerminator
{
    public AwkRecordTerminator(string text)
    {
        Text = text;
    }

    public static AwkRecordTerminator Empty { get; } = new(string.Empty);

    public static AwkRecordTerminator NewLine { get; } = new("\n");

    public static AwkRecordTerminator FromText(string text) =>
        text.Length == 0 ? Empty : new AwkRecordTerminator(text);

    public string Text { get; }
}
