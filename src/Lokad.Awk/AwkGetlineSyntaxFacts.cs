namespace Lokad.Awk;

internal enum AwkGetlineSourceKind
{
    MainInput,
    RedirectedFile,
    InputPipe
}

internal readonly record struct AwkGetlineParts(
    AwkSyntaxOptional<AwkExpressionSyntax> Target,
    AwkSyntaxOptional<AwkExpressionSyntax> Source,
    AwkGetlineSourceKind SourceKind)
{
    public bool HasRedirection => SourceKind != AwkGetlineSourceKind.MainInput;
}

internal static class AwkGetlineSyntaxFacts
{
    internal static bool IsPostfixFieldTarget(AwkExpressionSyntax target) =>
        target.Kind == AwkExpressionKind.Postfix && target.Children.Count == 1 &&
        target.Children[0].Kind == AwkExpressionKind.Field &&
        AwkExpressionSyntaxFacts.IsIncrementOrDecrementExpression(target);

    internal static AwkDiagnostic PostfixFieldTargetDiagnostic(AwkExpressionSyntax target) =>
        new(target.Span,
            "Postfix field mutation is not a getline destination; put index mutations inside $(...).",
            AwkDiagnosticId.BindInvalidProgram);

    public static bool TryResolveStatementParts(
        AwkGetlineSyntax syntax,
        out AwkGetlineParts parts)
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

        parts = new AwkGetlineParts(target, source, syntax.HasInputRedirection
            ? AwkGetlineSourceKind.RedirectedFile
            : AwkGetlineSourceKind.MainInput);
        return true;
    }

    public static bool TryResolveExpressionCallParts(
        AwkExpressionSyntax expression,
        out AwkGetlineParts parts)
    {
        parts = default;
        if (expression.DelimiterTokens.Count != 0 ||
            expression.Children.Count is < 1 or > 3)
        {
            return false;
        }

        var sourceKind = AwkGetlineSourceKind.MainInput;
        for (var index = 0; index < expression.OperatorTokens.Count; index++)
        {
            if (expression.OperatorTokens[index].Kind == AwkTokenKind.Less)
            {
                sourceKind = AwkGetlineSourceKind.RedirectedFile;
                break;
            }
            if (expression.OperatorTokens[index].Kind == AwkTokenKind.Pipe)
            {
                sourceKind = AwkGetlineSourceKind.InputPipe;
                break;
            }
        }

        var target = AwkSyntaxOptional<AwkExpressionSyntax>.None;
        var source = AwkSyntaxOptional<AwkExpressionSyntax>.None;
        if (sourceKind != AwkGetlineSourceKind.MainInput)
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

        parts = new AwkGetlineParts(target, source, sourceKind);
        return true;
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkStatementSyntax statement, Action? checkpoint = null)
    {
        var fallback = InvalidGetline(statement.Span, "Invalid getline statement.");
        if (statement.Payload is not AwkGetlineStatementSyntaxPayload payload ||
            !TryResolveStatementParts(payload.Getline, out var parts)) return fallback;
        return TryGetFailureDiagnostic(parts, fallback, out var diagnostic) ? diagnostic
            : AwkPreparationDiagnostics.ForExpression(parts.Source.Value, checkpoint);
    }

    // A false result means the present source expression must be diagnosed next.
    internal static bool TryGetFailureDiagnostic(AwkGetlineParts parts, AwkDiagnostic fallback, out AwkDiagnostic diagnostic)
    {
        if (parts.Target.TryGetValue(out var target))
        {
            if (IsPostfixFieldTarget(target)) { diagnostic = PostfixFieldTargetDiagnostic(target); return true; }
            if (!AwkExpressionSyntaxFacts.IsValidMutationTarget(target, AwkLoweringScope.Global))
            { diagnostic = InvalidGetline(target.Span, "Invalid getline target."); return true; }
        }
        if (parts.Source.HasValue) { diagnostic = default; return false; }
        diagnostic = parts.HasRedirection
            ? InvalidGetline(parts.Target.TryGetValue(out target) ? target.Span : new(0, 0),
                "Input redirection getline requires a source expression.") : fallback;
        return true;
    }

    private static AwkDiagnostic InvalidGetline(AwkTextSpan span, string message) =>
        new(span, message, AwkDiagnosticId.BindInvalidProgram);
}
