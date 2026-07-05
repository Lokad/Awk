using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkLValueSyntax(AwkLValueKind kind, IReadOnlyList<AwkToken> tokens, AwkTextSpan span)
{
    public AwkLValueKind Kind { get; } = kind;

    public IReadOnlyList<AwkToken> Tokens { get; } = tokens;

    public AwkTextSpan Span { get; } = span;
}

internal sealed class AwkExpressionSyntax(
    AwkExpressionKind kind,
    IReadOnlyList<AwkToken>? tokens,
    IReadOnlyList<AwkToken> operatorTokens,
    IReadOnlyList<AwkToken> delimiterTokens,
    IReadOnlyList<AwkToken> separatorTokens,
    IReadOnlyList<AwkExpressionSyntax> children,
    AwkTextSpan span,
    AwkToken? firstToken = null,
    AwkToken? lastToken = null)
{
    private IReadOnlyList<AwkToken>? _tokens = tokens;

    public AwkExpressionSyntax(
        AwkExpressionKind kind,
        IReadOnlyList<AwkToken> tokens,
        IReadOnlyList<AwkExpressionSyntax> children,
        AwkTextSpan span)
        : this(kind, tokens, [], [], [], children, span)
    {
    }

    public AwkExpressionKind Kind { get; } = kind;

    public IReadOnlyList<AwkToken> Tokens => _tokens ??= BuildTokens();

    public IReadOnlyList<AwkToken> OperatorTokens { get; } = operatorTokens;

    public IReadOnlyList<AwkToken> DelimiterTokens { get; } = delimiterTokens;

    public IReadOnlyList<AwkToken> SeparatorTokens { get; } = separatorTokens;

    public IReadOnlyList<AwkExpressionSyntax> Children { get; } = children;

    public AwkTextSpan Span { get; } = span;

    public AwkToken FirstToken =>
        firstToken ?? (Tokens.Count == 0 ? default : Tokens[0]);

    public AwkToken LastToken =>
        lastToken ?? (Tokens.Count == 0 ? default : Tokens[^1]);

    public AwkExpressionSyntax? FirstChild => Children.Count > 0 ? Children[0] : null;

    public AwkExpressionSyntax? TargetExpression =>
        Kind is AwkExpressionKind.ArrayElement or AwkExpressionKind.FunctionCall
            ? FirstChild
            : null;

    public bool TryGetVariableIdentifier(out AwkToken token)
    {
        if (Kind == AwkExpressionKind.Variable &&
            Tokens.Count == 1 &&
            Tokens[0].Kind == AwkTokenKind.Identifier)
        {
            token = Tokens[0];
            return true;
        }

        token = default;
        return false;
    }

    private IReadOnlyList<AwkToken> BuildTokens()
    {
        var count = OperatorTokens.Count + DelimiterTokens.Count + SeparatorTokens.Count;
        for (var index = 0; index < Children.Count; index++)
            count += Children[index].Tokens.Count;

        if (count == 0)
            return [];

        var result = new AwkToken[count];
        var offset = 0;
        for (var index = 0; index < Children.Count; index++)
            offset = CopyTo(Children[index].Tokens, result, offset);

        offset = CopyTo(OperatorTokens, result, offset);
        offset = CopyTo(DelimiterTokens, result, offset);
        _ = CopyTo(SeparatorTokens, result, offset);

        System.Array.Sort(
            result,
            static (left, right) =>
            {
                var start = left.Span.Start.CompareTo(right.Span.Start);
                return start != 0 ? start : left.Span.Length.CompareTo(right.Span.Length);
            });
        return result;
    }

    private static int CopyTo(IReadOnlyList<AwkToken> source, AwkToken[] destination, int offset)
    {
        for (var index = 0; index < source.Count; index++)
            destination[offset++] = source[index];

        return offset;
    }
}
