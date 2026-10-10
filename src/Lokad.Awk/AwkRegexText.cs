namespace Lokad.Awk;

internal readonly record struct AwkRegexPattern
{
    private readonly string? _text;

    public AwkRegexPattern(string text, AwkRegexPatternOrigin origin = AwkRegexPatternOrigin.Runtime,
        object? warningSite = null)
    {
        _text = text.Length == 0 ? null : text;
        Origin = origin;
        WarningSite = warningSite;
    }

    public string Text => _text ?? string.Empty;
    public AwkRegexPatternOrigin Origin { get; }
    public object? WarningSite { get; }
}

internal enum AwkRegexPatternOrigin
{
    Runtime,
    SourceLiteral
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
