namespace Lokad.Awk;

internal readonly record struct AwkRegexPattern
{
    private readonly AwkText? _text;

    public AwkRegexPattern(string text, AwkRegexPatternOrigin origin = AwkRegexPatternOrigin.Runtime,
        object? warningSite = null)
        : this(AwkText.FromSourceLiteralString(text), origin, warningSite)
    {
    }

    public AwkRegexPattern(AwkText text, AwkRegexPatternOrigin origin = AwkRegexPatternOrigin.Runtime,
        object? warningSite = null)
    {
        _text = text.ByteLength == 0 ? null : text;
        Origin = origin;
        WarningSite = warningSite;
    }

    public AwkText Text => _text ?? AwkText.Empty;
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
    private readonly AwkText? _text;

    public AwkRegexLiteralText(string text)
        : this(AwkText.FromSourceLiteralString(text))
    {
    }

    public AwkRegexLiteralText(AwkText text)
    {
        _text = text.ByteLength == 0 ? null : text;
    }

    public AwkText Text => _text ?? AwkText.Empty;
}
