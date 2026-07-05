using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkDirectiveSyntax
{
    public AwkDirectiveSyntax(
        AwkDirectiveKind kind,
        string name,
        IReadOnlyList<AwkToken> argumentTokens,
        AwkTextSpan span)
    {
        Kind = kind;
        Name = name;
        ArgumentTokens = argumentTokens;
        Span = span;
    }

    public AwkDirectiveKind Kind { get; }

    public string Name { get; }

    public IReadOnlyList<AwkToken> ArgumentTokens { get; }

    public AwkTextSpan Span { get; }
}
