namespace Lokad.Awk;

internal readonly record struct AwkGetlineLoweringParts(
    AwkSyntaxOptional<AwkExpressionSyntax> Target,
    AwkSyntaxOptional<AwkExpressionSyntax> Source,
    bool RequiresInputRedirection);

internal readonly struct AwkGetlineLoweringResult
{
    private readonly bool _succeeded;
    private readonly AwkDiagnostic? _diagnostic;

    private AwkGetlineLoweringResult(
        bool succeeded,
        AwkGetlineTarget target,
        AwkGetlineSource source,
        AwkDiagnostic? diagnostic)
    {
        _succeeded = succeeded;
        Target = target;
        Source = source;
        _diagnostic = diagnostic;
    }

    public bool Succeeded => _succeeded;

    public AwkGetlineTarget Target { get; }

    public AwkGetlineSource Source { get; }

    public AwkDiagnostic Diagnostic =>
        _diagnostic ?? throw new InvalidOperationException("Successful getline lowering has no diagnostic.");

    public static AwkGetlineLoweringResult Success(
        AwkGetlineTarget target,
        AwkGetlineSource source) =>
        new(true, target, source, diagnostic: null);

    public static AwkGetlineLoweringResult Failure(AwkDiagnostic diagnostic) =>
        new(
            false,
            AwkGetlineTarget.CurrentRecord,
            AwkGetlineSource.MainInput,
            diagnostic);
}

internal static class AwkGetlineLowerer
{
    public static bool TryResolveStatementParts(
        AwkGetlineSyntax syntax,
        out AwkGetlineLoweringParts parts)
    {
        static bool TryResolveSlot(
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

    public static AwkGetlineLoweringResult Lower(
        AwkGetlineLoweringParts parts,
        AwkLoweringScope scope)
    {
        var target = AwkGetlineTarget.CurrentRecord;
        var source = AwkGetlineSource.MainInput;

        if (parts.Target.TryGetValue(out var targetExpression))
        {
            var targetResult = AwkLValueLowerer.Lower(targetExpression, scope);
            if (!targetResult.Succeeded)
                return AwkGetlineLoweringResult.Failure(
                    InvalidGetline(targetExpression.Span, "Invalid getline target."));

            target = AwkGetlineTarget.ReferenceTarget(targetResult.Reference);
        }

        if (parts.Source.TryGetValue(out var sourceExpressionSyntax))
        {
            if (!AwkExpressionLowerer.TryLower(
                    sourceExpressionSyntax,
                    scope,
                    out var sourceExpression,
                    out var diagnostic))
            {
                return AwkGetlineLoweringResult.Failure(diagnostic);
            }

            source = AwkGetlineSource.Redirection(sourceExpression);
        }

        if (parts.RequiresInputRedirection && source is not AwkRedirectionGetlineSource)
        {
            return AwkGetlineLoweringResult.Failure(InvalidGetline(
                parts.Source.TryGetValue(out var sourceExpression)
                    ? sourceExpression.Span
                    : ResolveFallbackSpan(parts),
                "Input redirection getline requires a source expression."));
        }

        return AwkGetlineLoweringResult.Success(target, source);
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkExpressionSyntax expression)
    {
        if (TryResolveExpressionCallParts(expression, out var parts))
        {
            var lowering = Lower(parts, AwkLoweringScope.Global);
            if (!lowering.Succeeded)
                return lowering.Diagnostic;
        }

        return InvalidGetline(expression.Span, "Invalid getline expression.");
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkStatementSyntax statement)
    {
        if (statement.Payload is AwkGetlineStatementSyntaxPayload payload &&
            TryResolveStatementParts(payload.Getline, out var parts))
        {
            var lowering = Lower(parts, AwkLoweringScope.Global);
            if (!lowering.Succeeded)
                return lowering.Diagnostic;
        }

        return InvalidGetline(statement.Span, "Invalid getline statement.");
    }

    private static AwkTextSpan ResolveFallbackSpan(AwkGetlineLoweringParts parts)
    {
        if (parts.Target.TryGetValue(out var target))
            return target.Span;

        if (parts.Source.TryGetValue(out var source))
            return source.Span;

        return new AwkTextSpan(0, 0);
    }

    private static AwkDiagnostic InvalidGetline(AwkTextSpan span, string message) =>
        new(span, message, AwkDiagnosticId.BindInvalidProgram);
}
