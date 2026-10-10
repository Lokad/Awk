using System;

namespace Lokad.Awk;

internal readonly record struct AwkFieldSeparatorSpec
{
    private static readonly AwkText Space = AwkText.FromSourceLiteralString(" ");

    private readonly AwkText? _customText;
    private readonly AwkFieldSeparatorKind _kind;

    private AwkFieldSeparatorSpec(AwkText text)
    {
        _customText = text;
        _kind = AwkFieldSeparatorKind.Custom;
    }

    public static AwkFieldSeparatorSpec Default => default;

    public static AwkFieldSeparatorSpec FromText(string text)
    {
        return FromByteText(AwkText.FromSourceLiteralString(text));
    }

    public static AwkFieldSeparatorSpec FromByteText(AwkText text) =>
        text.EqualsAscii(" ") ? Default : new AwkFieldSeparatorSpec(text);

    public static AwkFieldSeparatorSpec FromRegexLiteral(string text) =>
        new(AwkText.FromSourceLiteralString(text));

    public AwkText ByteText => _kind == AwkFieldSeparatorKind.Custom ? _customText ?? AwkText.Empty : Space;

    public bool UsesWhitespaceSemantics => _kind == AwkFieldSeparatorKind.Default;

    private enum AwkFieldSeparatorKind
    {
        Default,
        Custom
    }
}
