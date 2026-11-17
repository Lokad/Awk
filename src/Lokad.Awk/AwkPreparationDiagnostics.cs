namespace Lokad.Awk;

internal static class AwkPreparationDiagnostics
{
    internal static AwkDiagnostic ForStatement(AwkStatementSyntax syntax, Action? checkpoint = null)
    {
        static string FormatFailureDiagnostic(AwkStatementSyntax syntax)
        {
            if (AwkStatementNames.TryGetKeyword(syntax.Kind, out var keyword))
                return "Invalid `" + keyword + "' statement.";

            return "Invalid statement.";
        }

        if (syntax.Kind == AwkStatementKind.Getline)
            return AwkGetlineSyntaxFacts.CreateFailureDiagnostic(syntax, checkpoint);

        if (syntax.Kind == AwkStatementKind.Delete &&
            AwkStatementLoweringShapes.SingleExpression(syntax).TryGetValue(out var deleteTarget))
        {
            return new AwkDiagnostic(
                deleteTarget.Span,
                "Invalid delete target.",
                AwkDiagnosticId.BindInvalidProgram);
        }

        if ((AwkStatementLoweringFacts.IsExpressionBackedStatement(syntax.Kind) ||
                syntax.Kind is AwkStatementKind.Close or AwkStatementKind.Fflush) &&
            AwkStatementLoweringShapes.SingleExpression(syntax).TryGetValue(out var expression))
        {
            return ForExpression(expression, checkpoint);
        }

        if ((syntax.Kind is AwkStatementKind.Print or AwkStatementKind.Printf) &&
            AwkStatementLoweringShapes.Output(syntax).TryGetValue(out var output) &&
            AwkStatementLoweringShapes.RequiredExpression(output.Output).TryGetValue(out var outputExpression))
        {
            return ForExpression(outputExpression, checkpoint);
        }

        return new AwkDiagnostic(syntax.Span, FormatFailureDiagnostic(syntax), AwkDiagnosticId.BindInvalidProgram);
    }

    internal static AwkDiagnostic ForExpression(AwkExpressionSyntax expression, Action? checkpoint = null)
    {
        while (true)
        {
            checkpoint?.Invoke();
            if (expression.Kind == AwkExpressionKind.Group && expression.FirstChild is { } groupedExpression)
            {
                expression = groupedExpression;
                continue;
            }
            if (expression.Kind == AwkExpressionKind.Unknown)
                return AwkExpressionSyntaxParser.CreateSyntaxDiagnostic(expression);
            if (expression.Kind == AwkExpressionKind.FunctionCall)
            {
                if (AwkBuiltinCallShape.TryCreateKnownCallable(expression, out var call))
                {
                    if (!AwkBuiltinCallShape.AcceptsArgumentCount(call.Definition, call.ArgumentCount))
                        return InvalidExpression(expression, "Invalid number of arguments for " + call.Target.Name + ".");
                    if (AwkBuiltinCallShape.TryGetArgumentRoleFailure(call, out var failure))
                        return InvalidExpression(failure.Argument, AwkBuiltinCallShape.FormatArgumentRoleFailure(call, failure));
                }
                if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var callTarget) &&
                    AwkBuiltins.TryGetSpecialSyntax(callTarget.Name, out var specialSyntax) &&
                    specialSyntax.Id == AwkBuiltinId.Getline)
                {
                    var fallback = InvalidExpression(expression, "Invalid getline expression.");
                    if (!AwkGetlineSyntaxFacts.TryResolveExpressionCallParts(expression, out var parts)) return fallback;
                    if (AwkGetlineSyntaxFacts.TryGetFailureDiagnostic(parts, fallback, out var diagnostic)) return diagnostic;
                    // Target validation precedes source diagnostics. Only the present
                    // source continues this walk; no recursive diagnostic call is needed.
                    expression = parts.Source.Value;
                    continue;
                }
                return InvalidExpression(expression, "Invalid function call.");
            }
            if (expression.Kind == AwkExpressionKind.Assignment)
                return expression.Children.Count == 0
                    ? InvalidExpression(expression, "Invalid assignment expression.")
                    : InvalidExpression(expression.Children[0], "Invalid assignment target.");
            if (AwkExpressionSyntaxFacts.IsIncrementOrDecrementExpression(expression))
                return expression.Children.Count == 0
                    ? InvalidExpression(expression, "Invalid increment expression.")
                    : InvalidExpression(expression.Children[0], "Invalid increment target.");
            if (expression.Kind == AwkExpressionKind.ArrayElement &&
                expression.TargetExpression is { } targetExpression &&
                AwkExpressionSyntaxFacts.TryGetIdentifier(targetExpression, out var target) &&
                AwkIdentifiers.IsReservedName(target.Name))
                return InvalidExpression(targetExpression, "Reserved identifier `" + target.Name + "' cannot be used as an array name.");
            if (expression.Kind == AwkExpressionKind.Variable &&
                AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier) &&
                AwkIdentifiers.IsReservedName(identifier.Name))
                return InvalidExpression(expression, "Reserved identifier `" + identifier.Name + "' cannot be used as a variable.");
            return InvalidExpression(expression, "Invalid expression.");
        }
    }

    private static AwkDiagnostic InvalidExpression(AwkExpressionSyntax expression, string message) =>
        new(expression.Span, message, AwkDiagnosticId.BindInvalidProgram);
}
