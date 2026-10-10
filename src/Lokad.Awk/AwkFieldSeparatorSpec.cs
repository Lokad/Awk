using System;

namespace Lokad.Awk;

internal readonly record struct AwkFieldSeparatorSpec
{
    private static readonly AwkText Space = AwkText.FromSourceLiteralString(" ");

    private readonly AwkText? _customText;
    private readonly AwkFieldSeparatorKind _kind;

    private AwkFieldSeparatorSpec(AwkText text, AwkRegexPatternOrigin origin = AwkRegexPatternOrigin.Runtime,
        object? warningSite = null)
    {
        _customText = text;
        _kind = AwkFieldSeparatorKind.Custom;
        PatternOrigin = origin;
        WarningSite = warningSite;
    }

    public static AwkFieldSeparatorSpec Default => default;

    public static AwkFieldSeparatorSpec FromText(string text)
    {
        return FromByteText(AwkText.FromSourceLiteralString(text));
    }

    public static AwkFieldSeparatorSpec FromByteText(AwkText text) =>
        text.ByteLength == 1 && text.EqualsAscii(" ") ? Default : new AwkFieldSeparatorSpec(text);

    public static AwkFieldSeparatorSpec FromRegexLiteral(string text) =>
        new(AwkText.FromSourceLiteralString(text), AwkRegexPatternOrigin.SourceLiteral);

    public static AwkFieldSeparatorSpec FromRegexPattern(AwkRegexPattern pattern, bool isLiteral) =>
        !isLiteral && pattern.Text == " " ? Default :
        new(AwkText.FromSourceLiteralString(pattern.Text), pattern.Origin, pattern.WarningSite);

    public AwkRegexPatternOrigin PatternOrigin { get; }
    public object? WarningSite { get; }

    public AwkText ByteText => _kind == AwkFieldSeparatorKind.Custom ? _customText ?? AwkText.Empty : Space;

    public bool UsesWhitespaceSemantics => _kind == AwkFieldSeparatorKind.Default;

    private enum AwkFieldSeparatorKind
    {
        Default,
        Custom
    }
}
