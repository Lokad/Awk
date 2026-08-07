namespace Lokad.Awk;

internal static class AwkLValueLowerer
{
    public static AwkLValueLoweringResult Lower(AwkExpressionSyntax? expression)
    {
        return Lower(expression, AwkLoweringScope.Global);
    }

    public static AwkLValueLoweringResult Lower(
        AwkExpressionSyntax? expression,
        AwkLoweringScope scope)
    {
        if (expression == null)
            return AwkLValueLoweringResult.Invalid;

        switch (expression.Kind)
        {
            case AwkExpressionKind.Variable:
                return LowerVariable(expression, scope);
            case AwkExpressionKind.ArrayElement:
                return LowerArrayElement(expression, scope);
            case AwkExpressionKind.Field:
                return LowerField(expression, scope);
            default:
                return AwkLValueLoweringResult.InvalidTarget(expression, "Invalid lvalue target.");
        }
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkExpressionSyntax expression, string message) =>
        new(expression.Span, message, AwkDiagnosticId.BindInvalidProgram);

    private static AwkLValueLoweringResult LowerVariable(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope)
    {
        if (!AwkExpressionSyntaxFacts.TryGetAssignableIdentifier(expression, out var identifier))
        {
            return AwkLValueLoweringResult.InvalidTarget(expression, "Invalid lvalue target.");
        }

        var variableName = new AwkVariableName(identifier.Identifier);
        return AwkLValueLoweringResult.Valid(
            scope.TryGetFunctionLocalSlot(variableName, out var slotId)
                ? AwkReference.LocalScalar(variableName, slotId)
                : AwkReference.Scalar(variableName));
    }

    private static AwkLValueLoweringResult LowerArrayElement(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope)
    {
        if (expression.Children.Count < 2 ||
            !AwkExpressionSyntaxFacts.TryGetAssignableArrayElementTarget(expression, out var target) ||
            !AwkExpressionLowerer.TryLowerArrayIndex(expression.Children, scope, out var index))
        {
            return AwkLValueLoweringResult.InvalidTarget(expression, "Invalid lvalue target.");
        }

        return AwkLValueLoweringResult.Valid(AwkReference.ArrayElement(new AwkArrayName(target.Identifier), index));
    }

    private static AwkLValueLoweringResult LowerField(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope)
    {
        if (expression.Children.Count != 1)
            return AwkLValueLoweringResult.InvalidTarget(expression, "Invalid field lvalue target.");

        if (AwkExpressionSyntaxFacts.IsFieldCountExpression(expression.Children[0]))
        {
            return AwkLValueLoweringResult.Valid(AwkReference.Field(AwkExpression.FieldCount()));
        }

        if (!AwkExpressionLowerer.TryLower(expression.Children[0], scope, out var index))
            return AwkLValueLoweringResult.InvalidTarget(expression.Children[0], "Invalid field lvalue index.");

        return AwkLValueLoweringResult.Valid(AwkReference.Field(index));
    }
}

internal readonly struct AwkLValueLoweringResult
{
    private AwkLValueLoweringResult(bool succeeded, object reference, AwkDiagnostic diagnostic)
    {
        Succeeded = succeeded;
        _reference = reference;
        Diagnostic = diagnostic;
    }

    private readonly object _reference;

    public bool Succeeded { get; }

    public AwkReference Reference =>
        Succeeded
            ? (AwkReference)_reference
            : throw new System.InvalidOperationException("No lvalue reference is present.");

    public AwkDiagnostic Diagnostic { get; }

    public static AwkLValueLoweringResult Invalid { get; } =
        new(succeeded: false, NoReference.Instance, default);

    public static AwkLValueLoweringResult InvalidTarget(AwkExpressionSyntax expression, string message) =>
        new(succeeded: false, NoReference.Instance, AwkLValueLowerer.CreateFailureDiagnostic(expression, message));

    public static AwkLValueLoweringResult Valid(AwkReference reference) =>
        new(succeeded: true, reference, default);

    private sealed class NoReference
    {
        public static readonly NoReference Instance = new();
    }
}
