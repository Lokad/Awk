using System;

namespace Lokad.Awk;

internal readonly record struct AwkFieldSeparatorSpec
{
    private readonly string? _customText;
    private readonly AwkFieldSeparatorKind _kind;

    private AwkFieldSeparatorSpec(string text)
    {
        _customText = text;
        _kind = AwkFieldSeparatorKind.Custom;
    }

    public static AwkFieldSeparatorSpec Default => default;

    public static AwkFieldSeparatorSpec FromText(string text)
    {
        return text == " " ? Default : new AwkFieldSeparatorSpec(text);
    }

    public string Text => _kind == AwkFieldSeparatorKind.Custom ? _customText ?? string.Empty : " ";

    public bool UsesWhitespaceSemantics => _kind == AwkFieldSeparatorKind.Default;

    private enum AwkFieldSeparatorKind
    {
        Default,
        Custom
    }
}
