namespace Lokad.Awk;

internal readonly record struct AwkRegexPattern
{
    private readonly string? _text;

    public AwkRegexPattern(string text)
    {
        _text = text.Length == 0 ? null : text;
    }

    public string Text => _text ?? string.Empty;
}

internal readonly record struct AwkRegexLiteralText
{
    private readonly string? _text;

    public AwkRegexLiteralText(string text)
    {
        _text = text.Length == 0 ? null : text;
    }

    public string Text => _text ?? string.Empty;
}
