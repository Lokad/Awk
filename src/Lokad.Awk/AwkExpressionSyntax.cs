using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkLValueSyntax(AwkLValueKind kind, AwkTextSpan span)
{
    public AwkLValueKind Kind { get; } = kind;

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

    public IReadOnlyList<AwkToken> Tokens => GetTokens();

    public IReadOnlyList<AwkToken> GetTokens(Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var cached = Volatile.Read(ref _tokens);
        if (cached != null)
            return cached;
        static int CopyTo(IReadOnlyList<AwkToken> source, AwkToken[] destination, int offset, Action? checkpoint)
        {
            source = AwkTokenNavigation.ForIndexedAccess(source, checkpoint);
            for (var index = 0; index < source.Count; index++)
            {
                checkpoint?.Invoke();
                destination[offset++] = source[index];
            }

            return offset;
        }

        static AwkToken[] BuildTokens(AwkExpressionSyntax root, Action? checkpoint)
        {
            var nodes = new Stack<AwkExpressionSyntax>();
            nodes.Push(root);
            var count = 0;
            while (nodes.Count > 0)
            {
                checkpoint?.Invoke();
                var node = nodes.Pop();
                if (Volatile.Read(ref node._tokens) is { } materializedTokens)
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
                {
                    checkpoint?.Invoke();
                    nodes.Push(node.Children[index]);
                }
            }

            if (count == 0)
                return [];

            var result = new AwkToken[count];
            var offset = 0;
            nodes.Push(root);
            while (nodes.Count > 0)
            {
                checkpoint?.Invoke();
                var node = nodes.Pop();
                if (Volatile.Read(ref node._tokens) is { } materializedTokens)
                {
                    offset = CopyTo(materializedTokens, result, offset, checkpoint);
                    continue;
                }

                offset = CopyTo(node.OperatorTokens, result, offset, checkpoint);
                offset = CopyTo(node.DelimiterTokens, result, offset, checkpoint);
                offset = CopyTo(node.SeparatorTokens, result, offset, checkpoint);
                for (var index = 0; index < node.Children.Count; index++)
                {
                    checkpoint?.Invoke();
                    nodes.Push(node.Children[index]);
                }
            }

            checkpoint?.Invoke();
            System.Array.Sort(
                result,
                static (left, right) =>
                {
                    var start = left.Span.Start.CompareTo(right.Span.Start);
                    return start != 0 ? start : left.Span.Length.CompareTo(right.Span.Length);
                });
            checkpoint?.Invoke();
            return result;
        }

        var built = BuildTokens(this, checkpoint);
        checkpoint?.Invoke();
        return Interlocked.CompareExchange(ref _tokens, built, null) ?? built;
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
