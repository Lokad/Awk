using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkFrontendClassifier
{
    public static bool TryGetIdentifier(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax identifier) =>
        AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out identifier);

    public static bool TryGetAssignableIdentifier(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax identifier) =>
        AwkExpressionSyntaxFacts.TryGetAssignableIdentifier(expression, out identifier);

    public static bool TryGetArrayElementTarget(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax target) =>
        AwkExpressionSyntaxFacts.TryGetArrayElementTarget(expression, out target);

    public static bool TryGetAssignableArrayElementTarget(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax target) =>
        AwkExpressionSyntaxFacts.TryGetAssignableArrayElementTarget(expression, out target);

    public static bool TryGetCallableTarget(
        AwkExpressionSyntax expression,
        out AwkCallableTargetSyntax target) =>
        AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out target);

    public static bool IsFieldCountExpression(AwkExpressionSyntax expression) =>
        AwkExpressionSyntaxFacts.IsFieldCountExpression(expression);

    public static bool IsValidMutationTarget(AwkExpressionSyntax expression) =>
        AwkExpressionSyntaxFacts.IsValidMutationTarget(expression);

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
        out AwkIdentifierSyntax target) =>
        AwkExpressionSyntaxFacts.TryGetStaticArrayElementName(expression, out target);

    public static bool TryGetStaticArrayTargetName(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax target) =>
        AwkExpressionSyntaxFacts.TryGetStaticArrayTargetName(expression, out target);

    public static bool TryGetNamedCallableTarget(
        AwkExpressionSyntax expression,
        string name,
        out AwkCallableTargetSyntax target)
    {
        if (TryGetCallableTarget(expression, out target) &&
            string.Equals(target.Name, name, System.StringComparison.Ordinal))
        {
            return true;
        }

        target = default;
        return false;
    }

    public static bool TryGetBareCallableVariable(
        AwkExpressionSyntax expression,
        out AwkIdentifierSyntax identifier)
    {
        if (expression.Kind == AwkExpressionKind.Variable &&
            TryGetIdentifier(expression, out identifier) &&
            !AwkBuiltins.IsName(identifier.Name, AwkBuiltinId.Length) &&
            AwkBuiltins.IsKnownFunctionOrSpecialForm(identifier.Name))
        {
            return true;
        }

        identifier = default;
        return false;
    }

    public static AwkSymbolReferenceKind ClassifyVariableReference(
        AwkIdentifierSyntax identifier,
        AwkSymbolReferenceKind ordinaryKind)
    {
        if (AwkSpecialVariables.TryGet(identifier.Name, out var definition))
        {
            return definition is AwkSpecialArrayVariableDefinition
                ? AwkSymbolReferenceKind.SpecialArray
                : AwkSymbolReferenceKind.SpecialVariable;
        }

        return ordinaryKind;
    }

    public static AwkSymbolReferenceKind ClassifyCallableReference(
        AwkCallableTargetSyntax target,
        ISet<AwkIdentifier> functionNames)
    {
        if (AwkBuiltins.IsKnownFunctionOrSpecialForm(target.Name))
            return AwkSymbolReferenceKind.BuiltinFunction;

        return functionNames.Contains(target.Identifier)
            ? AwkSymbolReferenceKind.UserFunction
            : AwkSymbolReferenceKind.UnknownFunction;
    }

    public static AwkSyntaxOptional<AwkExpressionSyntax> SingleExpression(AwkStatementSyntax syntax) =>
        AwkStatementLoweringShapes.SingleExpression(syntax);

    public static AwkSyntaxOptional<AwkExpressionSyntax> RequiredExpression(
        AwkExpressionSlotSyntax slot) =>
        AwkStatementLoweringShapes.RequiredExpression(slot);

    public static AwkSyntaxOptional<AwkControlStatementLoweringShape> Control(AwkStatementSyntax syntax) =>
        AwkStatementLoweringShapes.Control(syntax);

    public static AwkSyntaxOptional<AwkForStatementLoweringShape> For(AwkStatementSyntax syntax) =>
        AwkStatementLoweringShapes.For(syntax);

    public static AwkSyntaxOptional<AwkOutputStatementLoweringShape> Output(AwkStatementSyntax syntax) =>
        AwkStatementLoweringShapes.Output(syntax);

    public static AwkSyntaxOptional<AwkSwitchStatementLoweringShape> Switch(AwkStatementSyntax syntax) =>
        AwkStatementLoweringShapes.Switch(syntax);

    public static AwkSyntaxOptional<AwkGetlineSyntax> Getline(AwkStatementSyntax syntax) =>
        AwkStatementLoweringShapes.Getline(syntax);

    public static bool TryGetWholeStatementCall(
        AwkStatementSyntax syntax,
        AwkBuiltinId id,
        out AwkBuiltinCallSyntax call) =>
        AwkStatementLoweringShapes.TryGetWholeStatementCall(syntax, id, out call);

    public static bool TryGetControlFlowRequirement(
        AwkStatementKind kind,
        out AwkControlFlowRequirement requirement,
        out string diagnostic) =>
        AwkStatementValidationFacts.TryGetControlFlowRequirement(kind, out requirement, out diagnostic);

    public static bool IsArgumentless(AwkStatementKind kind) =>
        AwkStatementValidationFacts.IsArgumentless(kind);

    public static string ArgumentlessDiagnostic(AwkStatementKind kind) =>
        AwkStatementValidationFacts.ArgumentlessDiagnostic(kind);

    public static bool TryGetMissingExpressionDiagnostic(AwkStatementKind kind, out string message) =>
        AwkStatementValidationFacts.TryGetMissingExpressionDiagnostic(kind, out message);

    public static bool IsLoopBodyKind(AwkStatementKind kind) =>
        AwkStatementValidationFacts.IsLoopBodyKind(kind);

    public static bool IsSwitchBodyKind(AwkStatementKind kind) =>
        AwkStatementValidationFacts.IsSwitchBodyKind(kind);
}
