using System;

namespace Lokad.Awk;

internal readonly record struct AwkFieldSeparatorSpec
{
    private readonly string _text;
    private readonly bool _isCustom;

    private AwkFieldSeparatorSpec(string text, bool isCustom)
    {
        _text = text;
        _isCustom = isCustom;
    }

    public static AwkFieldSeparatorSpec Default => default;

    public static AwkFieldSeparatorSpec Custom(string text)
    {
        return new AwkFieldSeparatorSpec(text, isCustom: true);
    }

    public string Text => _isCustom ? _text : " ";

    public bool UseDefaultSeparator => !_isCustom;
}
