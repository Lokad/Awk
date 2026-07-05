namespace Lokad.Awk;

internal readonly record struct AwkRegexPattern
{
    public AwkRegexPattern(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

internal readonly record struct AwkRegexLiteralText
{
    public AwkRegexLiteralText(string text)
    {
        Text = text;
    }

    public string Text { get; }
}
