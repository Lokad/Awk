namespace Lokad.Awk;

internal readonly record struct AwkGetlineLoweringParts(
    AwkSyntaxOptional<AwkExpressionSyntax> Target,
    AwkSyntaxOptional<AwkExpressionSyntax> Source,
    bool RequiresInputRedirection);

internal static class AwkGetlineLowerer
{
    public static bool TryResolveStatementParts(
        AwkGetlineSyntax syntax,
        out AwkGetlineLoweringParts parts)
    {
        parts = default;
        if (!TryResolveSlot(syntax.Target, out var target) ||
            !TryResolveSlot(syntax.Source, out var source))
        {
            return false;
        }

        parts = new AwkGetlineLoweringParts(target, source, syntax.HasInputRedirection);
        return true;
    }

    public static bool TryResolveExpressionCallParts(
        AwkExpressionSyntax expression,
        out AwkGetlineLoweringParts parts)
    {
        parts = default;
        if (expression.DelimiterTokens.Count != 0 ||
            expression.Children.Count is < 1 or > 3)
        {
            return false;
        }

        var hasSource = false;
        for (var index = 0; index < expression.OperatorTokens.Count; index++)
        {
            if (expression.OperatorTokens[index].Kind == AwkTokenKind.Less)
            {
                hasSource = true;
                break;
            }
        }

        var target = AwkSyntaxOptional<AwkExpressionSyntax>.None;
        var source = AwkSyntaxOptional<AwkExpressionSyntax>.None;
        if (hasSource)
        {
            if (expression.Children.Count == 3)
                target = expression.Children[1];
            else if (expression.Children.Count != 2)
                return false;

            source = expression.Children[^1];
        }
        else if (expression.Children.Count == 2)
        {
            target = expression.Children[1];
        }
        else if (expression.Children.Count != 1)
        {
            return false;
        }

        parts = new AwkGetlineLoweringParts(target, source, hasSource);
        return true;
    }

    public static bool TryLower(
        AwkGetlineLoweringParts parts,
        out AwkGetlineTarget target,
        out AwkGetlineSource source)
    {
        return TryLower(parts, AwkLoweringScope.Global, out target, out source, out _);
    }

    public static bool TryLower(
        AwkGetlineLoweringParts parts,
        AwkLoweringScope scope,
        out AwkGetlineTarget target,
        out AwkGetlineSource source)
    {
        return TryLower(parts, scope, out target, out source, out _);
    }

    public static bool TryLower(
        AwkGetlineLoweringParts parts,
        out AwkGetlineTarget target,
        out AwkGetlineSource source,
        out AwkDiagnostic diagnostic)
    {
        return TryLower(parts, AwkLoweringScope.Global, out target, out source, out diagnostic);
    }

    public static bool TryLower(
        AwkGetlineLoweringParts parts,
        AwkLoweringScope scope,
        out AwkGetlineTarget target,
        out AwkGetlineSource source,
        out AwkDiagnostic diagnostic)
    {
        target = AwkGetlineTarget.CurrentRecord;
        source = AwkGetlineSource.MainInput;
        diagnostic = default;

        if (parts.Target.TryGetValue(out var targetExpression))
        {
            var targetResult = AwkLValueLowerer.Lower(targetExpression, scope);
            if (!targetResult.Succeeded)
            {
                diagnostic = InvalidGetline(targetExpression.Span, "Invalid getline target.");
                return false;
            }

            target = AwkGetlineTarget.ReferenceTarget(targetResult.Reference);
        }

        if (parts.Source.TryGetValue(out var sourceExpressionSyntax))
        {
            if (!AwkExpressionLowerer.TryLower(sourceExpressionSyntax, scope, out var sourceExpression, out diagnostic))
                return false;

            source = AwkGetlineSource.Redirection(sourceExpression);
        }

        if (parts.RequiresInputRedirection && source is not AwkRedirectionGetlineSource)
        {
            diagnostic = InvalidGetline(
                parts.Source.TryGetValue(out var sourceExpression)
                    ? sourceExpression.Span
                    : ResolveFallbackSpan(parts),
                "Input redirection getline requires a source expression.");
            return false;
        }

        return true;
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkExpressionSyntax expression)
    {
        if (TryResolveExpressionCallParts(expression, out var parts) &&
            !TryLower(parts, out _, out _, out var diagnostic) &&
            HasDiagnostic(diagnostic))
        {
            return diagnostic;
        }

        return InvalidGetline(expression.Span, "Invalid getline expression.");
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkStatementSyntax statement)
    {
        if (statement.Payload is AwkGetlineStatementSyntaxPayload payload &&
            TryResolveStatementParts(payload.Getline, out var parts) &&
            !TryLower(parts, out _, out _, out var diagnostic) &&
            HasDiagnostic(diagnostic))
        {
            return diagnostic;
        }

        return InvalidGetline(statement.Span, "Invalid getline statement.");
    }

    private static bool TryResolveSlot(
        AwkExpressionSlotSyntax slot,
        out AwkSyntaxOptional<AwkExpressionSyntax> expression)
    {
        expression = AwkSyntaxOptional<AwkExpressionSyntax>.None;
        if (slot.IsEmpty)
            return true;

        if (!slot.Expression.TryGetValue(out var syntax))
            return false;

        expression = syntax;
        return true;
    }

    private static AwkTextSpan ResolveFallbackSpan(AwkGetlineLoweringParts parts)
    {
        if (parts.Target.TryGetValue(out var target))
            return target.Span;

        if (parts.Source.TryGetValue(out var source))
            return source.Span;

        return new AwkTextSpan(0, 0);
    }

    private static bool HasDiagnostic(AwkDiagnostic diagnostic) =>
        diagnostic.Message != null;

    private static AwkDiagnostic InvalidGetline(AwkTextSpan span, string message) =>
        new(span, message, AwkDiagnosticId.BindInvalidProgram);
}
