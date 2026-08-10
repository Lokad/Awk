namespace Lokad.Awk;

internal static class AwkLValueLowerer
{
    public static AwkLValueLoweringResult Lower(AwkExpressionSyntax expression)
    {
        return Lower(expression, AwkLoweringScope.Global);
    }

    public static AwkLValueLoweringResult Lower(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope)
    {
        switch (expression.Kind)
        {
            case AwkExpressionKind.Variable:
                return LowerVariable(expression, scope);
            case AwkExpressionKind.ArrayElement:
                return LowerArrayElement(expression, scope);
            case AwkExpressionKind.Field:
                return LowerField(expression, scope);
            default:
                return AwkLValueLoweringResult.Invalid;
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
            return AwkLValueLoweringResult.Invalid;
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
            return AwkLValueLoweringResult.Invalid;
        }

        return AwkLValueLoweringResult.Valid(AwkReference.ArrayElement(new AwkArrayName(target.Identifier), index));
    }

    private static AwkLValueLoweringResult LowerField(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope)
    {
        if (expression.Children.Count != 1)
            return AwkLValueLoweringResult.Invalid;

        if (AwkExpressionSyntaxFacts.IsFieldCountExpression(expression.Children[0]))
        {
            return AwkLValueLoweringResult.Valid(AwkReference.Field(AwkExpression.FieldCount()));
        }

        if (!AwkExpressionLowerer.TryLower(expression.Children[0], scope, out var index))
            return AwkLValueLoweringResult.Invalid;

        return AwkLValueLoweringResult.Valid(AwkReference.Field(index));
    }
}

internal readonly struct AwkLValueLoweringResult
{
    private readonly AwkReference? _reference;

    private AwkLValueLoweringResult(AwkReference? reference)
    {
        _reference = reference;
    }

    public bool Succeeded => _reference is not null;

    public AwkReference Reference =>
        _reference ?? throw new System.InvalidOperationException("No lvalue reference is present.");

    public static AwkLValueLoweringResult Invalid { get; } =
        new(reference: null);

    public static AwkLValueLoweringResult Valid(AwkReference reference) =>
        new(reference);
}
