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
    AwkToken? firstToken,
    AwkToken? lastToken)
{
    private IReadOnlyList<AwkToken>? _tokens = tokens;

    public AwkExpressionSyntax(
        AwkExpressionKind kind,
        IReadOnlyList<AwkToken> tokens,
        IReadOnlyList<AwkExpressionSyntax> children,
        AwkTextSpan span)
        : this(kind, tokens, [], [], [], children, span, null, null)
    {
    }

    public AwkExpressionSyntax(
        AwkExpressionKind kind,
        IReadOnlyList<AwkToken>? tokens,
        IReadOnlyList<AwkToken> operatorTokens,
        IReadOnlyList<AwkToken> delimiterTokens,
        IReadOnlyList<AwkToken> separatorTokens,
        IReadOnlyList<AwkExpressionSyntax> children,
        AwkTextSpan span)
        : this(kind, tokens, operatorTokens, delimiterTokens, separatorTokens, children, span, null, null)
    {
    }

    public AwkExpressionKind Kind { get; } = kind;

    public IReadOnlyList<AwkToken> Tokens
    {
        get
        {
            static int CopyTo(IReadOnlyList<AwkToken> source, AwkToken[] destination, int offset)
            {
                for (var index = 0; index < source.Count; index++)
                    destination[offset++] = source[index];

                return offset;
            }

            static AwkToken[] BuildTokens(AwkExpressionSyntax root)
            {
                var nodes = new Stack<AwkExpressionSyntax>();
                nodes.Push(root);
                var count = 0;
                while (nodes.Count > 0)
                {
                    var node = nodes.Pop();
                    if (node._tokens is { } materializedTokens)
                    {
                        count = checked(count + materializedTokens.Count);
                        continue;
                    }

                    count = checked(
                        count +
                        node.OperatorTokens.Count +
                        node.DelimiterTokens.Count +
                        node.SeparatorTokens.Count);
                    for (var index = 0; index < node.Children.Count; index++)
                        nodes.Push(node.Children[index]);
                }

                if (count == 0)
                    return [];

                var result = new AwkToken[count];
                var offset = 0;
                nodes.Push(root);
                while (nodes.Count > 0)
                {
                    var node = nodes.Pop();
                    if (node._tokens is { } materializedTokens)
                    {
                        offset = CopyTo(materializedTokens, result, offset);
                        continue;
                    }

                    offset = CopyTo(node.OperatorTokens, result, offset);
                    offset = CopyTo(node.DelimiterTokens, result, offset);
                    offset = CopyTo(node.SeparatorTokens, result, offset);
                    for (var index = 0; index < node.Children.Count; index++)
                        nodes.Push(node.Children[index]);
                }

                System.Array.Sort(
                    result,
                    static (left, right) =>
                    {
                        var start = left.Span.Start.CompareTo(right.Span.Start);
                        return start != 0 ? start : left.Span.Length.CompareTo(right.Span.Length);
                    });
                return result;
            }

            return _tokens ??= BuildTokens(this);
        }
    }

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

}
