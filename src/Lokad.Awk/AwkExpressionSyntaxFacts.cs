namespace Lokad.Awk;

internal static class AwkExpressionSyntaxFacts
{
    // Visit in source order. The visitor selects its first child or skips all children.
    // A narrow expression can stay on this loop without allocating a stack.
    internal static void Walk<TState>(AwkExpressionSyntax expression, Action? checkpoint,
        TState state, Func<AwkExpressionSyntax, TState, int> visit)
    {
        Stack<AwkExpressionSyntax>? pending = null;
        while (true)
        {
            checkpoint?.Invoke();
            var first = visit(expression, state);
            var children = expression.Children;
            if (first >= 0 && first < children.Count)
            {
                if (first + 1 < children.Count)
                {
                    pending ??= new();
                    for (var index = children.Count - 1; index > first; index--)
                    {
                        if ((index & 63) == 0) checkpoint?.Invoke();
                        pending.Push(children[index]);
                    }
                }
                expression = children[first];
                continue;
            }
            if (pending is null || !pending.TryPop(out var next)) return;
            expression = next;
        }
    }

    public static bool TryGetIdentifier(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax identifier)
    {
        if (expression.Kind == AwkExpressionKind.Variable &&
            expression.Tokens.Count == 1 &&
            expression.Tokens[0].Kind == AwkTokenKind.Identifier &&
            AwkIdentifier.TryParse(expression.Tokens[0].Text, out var parsed))
        {
            identifier = new AwkIdentifierSyntax(parsed, expression.Tokens[0]);
            return true;
        }

        identifier = default;
        return false;
    }

    public static bool TryGetAssignableIdentifier(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax identifier)
    {
        if (TryGetIdentifier(expression, out identifier))
        {
            return true;
        }

        identifier = default;
        return false;
    }

    public static bool TryGetArrayElementTarget(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax target)
    {
        if (expression.Kind == AwkExpressionKind.ArrayElement &&
            expression.TargetExpression is { } targetExpression &&
            TryGetIdentifier(targetExpression, out target))
        {
            return true;
        }

        target = default;
        return false;
    }

    public static bool TryGetCallableTarget(
        AwkExpressionSyntax expression,
        out AwkCallableTargetSyntax target)
    {
        if (expression.Kind == AwkExpressionKind.FunctionCall &&
            expression.TargetExpression is { } targetExpression &&
            TryGetIdentifier(targetExpression, out var identifier))
        {
            target = new AwkCallableTargetSyntax(
                identifier,
                expression.DelimiterTokens.Count > 0,
                expression.DelimiterTokens.Count == 0 &&
                    AwkBuiltins.TryGetSpecialSyntax(identifier.Name, out _));
            return true;
        }

        target = default;
        return false;
    }

    public static bool IsValidMutationTarget(AwkExpressionSyntax expression)
    {
        if (expression.Kind == AwkExpressionKind.Field)
            return expression.Children.Count == 1;

        if (TryGetAssignableIdentifier(expression, out _))
            return true;

        return expression.Kind == AwkExpressionKind.ArrayElement && TryGetArrayRoot(expression, out _) &&
            expression.Children.Count >= 2;
    }

    public static bool TryGetArrayRoot(AwkExpressionSyntax expression, out AwkIdentifierSyntax root)
    {
        while (expression.Kind == AwkExpressionKind.ArrayElement && expression.TargetExpression is { } target)
            expression = target;
        return TryGetIdentifier(expression, out root);
    }

    // A builtin's lvalue role describes the destination shape. Operand binding
    // belongs to the compiler, rather than constructing an executable tree here.
    public static bool IsValidMutationTarget(AwkExpressionSyntax expression, AwkLoweringScope scope)
    {
        scope.CheckCancellation();
        if (expression.Kind == AwkExpressionKind.Field) return expression.Children.Count == 1;
        if (expression.Kind == AwkExpressionKind.Variable)
            return TryGetAssignableIdentifier(expression, out var variable) && !scope.IsReservedVariable(variable.Name);
        if (expression.Kind != AwkExpressionKind.ArrayElement || expression.Children.Count < 2) return false;
        if (expression.Children[0].Kind == AwkExpressionKind.ArrayElement && scope.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
            return false;
        while (expression.Kind == AwkExpressionKind.ArrayElement)
        {
            scope.CheckCancellation();
            if (expression.Children.Count < 2) return false;
            expression = expression.Children[0];
        }
        return TryGetIdentifier(expression, out var array) && !scope.IsReservedVariable(array.Name);
    }

    public static bool IsIncrementOrDecrementExpression(AwkExpressionSyntax expression)
    {
        if (expression.Kind is not AwkExpressionKind.Unary and not AwkExpressionKind.Postfix)
            return false;

        for (var index = 0; index < expression.OperatorTokens.Count; index++)
        {
            if (expression.OperatorTokens[index].Kind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus)
                return true;
        }

        return false;
    }

    public static bool TryGetStaticArrayElementName(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax target)
    {
        if (expression.Kind == AwkExpressionKind.ArrayElement && TryGetArrayRoot(expression, out target))
            return true;

        target = default;
        return false;
    }

    public static bool TryGetStaticArrayTargetName(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax target)
    {
        if (TryGetStaticArrayElementName(expression, out target))
            return true;

        return TryGetIdentifier(expression, out target);
    }

}

internal readonly record struct AwkIdentifierSyntax(AwkIdentifier Identifier, AwkToken Token)
{
    public string Name => Identifier.Text;

    public AwkTextSpan Span => Token.Span;
}

internal readonly record struct AwkCallableTargetSyntax(
    AwkIdentifierSyntax IdentifierSyntax,
    bool HasDelimitedCall,
    bool IsSpecialSyntax)
{
    public AwkIdentifier Identifier => IdentifierSyntax.Identifier;

    public string Name => IdentifierSyntax.Name;

    public AwkTextSpan Span => IdentifierSyntax.Span;
}
