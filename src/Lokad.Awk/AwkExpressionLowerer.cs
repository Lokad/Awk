using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    public static bool TryLower(
        AwkExpressionSyntax expression,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        return TryLower(expression, AwkLoweringScope.Global, out lowered);
    }

    public static bool TryLower(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        static bool TryLowerVariable(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (!AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var token))
                return false;

            var identifier = token.Name;
            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.RecordNumber))
            {
                lowered = AwkExpression.RecordNumber();
                return true;
            }

            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.FileRecordNumber))
            {
                lowered = AwkExpression.FileRecordNumber();
                return true;
            }

            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.FileName))
            {
                lowered = AwkExpression.FileName();
                return true;
            }

            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.FieldCount))
            {
                lowered = AwkExpression.FieldCount();
                return true;
            }

            if (AwkBuiltins.IsName(identifier, AwkBuiltinId.Length))
            {
                lowered = AwkExpression.Length(AwkExpression.FieldZero(), AwkOptionalIdentifier.None);
                return true;
            }

            if (AwkIdentifiers.IsReservedName(identifier))
                return false;

            var variableName = new AwkVariableName(token.Identifier);
            lowered = scope.TryGetFunctionLocalSlot(variableName, out var slotId)
                ? AwkExpression.LocalVariable(variableName, slotId)
                : AwkExpression.Variable(variableName);
            return true;
        }

        static bool TryLowerField(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (expression.Children.Count != 1)
                return false;

            if (AwkExpressionSyntaxFacts.IsFieldCountExpression(expression.Children[0]))
            {
                lowered = AwkExpression.FieldNf();
                return true;
            }

            if (!TryLower(expression.Children[0], scope, out var index))
                return false;

            lowered = AwkExpression.FieldIndex(index);
            return true;
        }

        static bool TryLowerArrayElement(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (expression.Children.Count < 2 ||
                !AwkExpressionSyntaxFacts.TryGetArrayElementTarget(expression, out var target))
            {
                return false;
            }

            if (!TryLowerArrayIndex(expression.Children, scope, out var index))
                return false;

            lowered = AwkExpression.ArrayElement(new AwkArrayName(target.Identifier), index);
            return true;
        }

        lowered = null;

        switch (expression.Kind)
        {
            case AwkExpressionKind.Number:
                return TryLowerNumber(expression, out lowered);
            case AwkExpressionKind.String:
                return TryLowerString(expression, out lowered);
            case AwkExpressionKind.Regex:
                return TryLowerRegexLiteral(expression, out lowered);
            case AwkExpressionKind.Variable:
                return TryLowerVariable(expression, scope, out lowered);
            case AwkExpressionKind.Group:
                return expression.FirstChild != null && TryLower(expression.FirstChild, scope, out lowered);
            case AwkExpressionKind.Field:
                return TryLowerField(expression, scope, out lowered);
            case AwkExpressionKind.ArrayElement:
                return TryLowerArrayElement(expression, scope, out lowered);
            case AwkExpressionKind.FunctionCall:
                return TryLowerFunctionCall(expression, scope, out lowered);
            case AwkExpressionKind.Unary:
                return TryLowerUnary(expression, scope, out lowered);
            case AwkExpressionKind.Postfix:
                return TryLowerPostfix(expression, scope, out lowered);
            case AwkExpressionKind.Binary:
                return TryLowerBinary(expression, scope, out lowered);
            case AwkExpressionKind.Concatenation:
                return TryLowerConcatenation(expression, scope, out lowered);
            case AwkExpressionKind.Conditional:
                return TryLowerConditional(expression, scope, out lowered);
            case AwkExpressionKind.Assignment:
                return TryLowerAssignment(expression, scope, out lowered);
            default:
                return false;
        }
    }

    public static bool TryLower(
        AwkExpressionSyntax expression,
        [NotNullWhen(true)] out AwkExpression? lowered,
        out AwkDiagnostic diagnostic)
    {
        return TryLower(expression, AwkLoweringScope.Global, out lowered, out diagnostic);
    }

    public static bool TryLower(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered,
        out AwkDiagnostic diagnostic)
    {
        if (TryLower(expression, scope, out lowered))
        {
            diagnostic = default;
            return true;
        }

        diagnostic = CreateFailureDiagnostic(expression);
        return false;
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkExpressionSyntax expression)
    {
        static AwkDiagnostic CreateCallFailureDiagnostic(AwkExpressionSyntax expression)
        {
            if (AwkBuiltinCallShape.TryCreateKnownCallable(expression, out var call))
            {
                if (!AwkBuiltinCallShape.AcceptsArgumentCount(call.Definition, call.ArgumentCount))
                {
                    return InvalidExpression(
                        expression,
                        "Invalid number of arguments for " + call.Target.Name + ".");
                }

                if (AwkBuiltinCallShape.TryGetArgumentRoleFailure(call, out var failure))
                {
                    return InvalidExpression(
                        failure.Argument,
                        AwkBuiltinCallShape.FormatArgumentRoleFailure(call, failure));
                }
            }

            if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) &&
                AwkBuiltins.TryGetSpecialSyntax(target.Name, out var specialSyntax) &&
                specialSyntax.Id == AwkBuiltinId.Getline)
            {
                return AwkGetlineLowerer.CreateFailureDiagnostic(expression);
            }

            return InvalidExpression(expression, "Invalid function call.");
        }

        if (expression.Kind == AwkExpressionKind.Group &&
            expression.FirstChild is { } groupedExpression)
        {
            return CreateFailureDiagnostic(groupedExpression);
        }

        if (expression.Kind == AwkExpressionKind.Assignment)
        {
            return expression.Children.Count == 0
                ? InvalidExpression(expression, "Invalid assignment expression.")
                : AwkLValueLowerer.CreateFailureDiagnostic(expression.Children[0], "Invalid assignment target.");
        }

        if (AwkExpressionSyntaxFacts.IsIncrementOrDecrementExpression(expression))
        {
            return expression.Children.Count == 0
                ? InvalidExpression(expression, "Invalid increment expression.")
                : AwkLValueLowerer.CreateFailureDiagnostic(expression.Children[0], "Invalid increment target.");
        }

        if (expression.Kind == AwkExpressionKind.FunctionCall)
            return CreateCallFailureDiagnostic(expression);

        if (expression.Kind == AwkExpressionKind.ArrayElement &&
            expression.TargetExpression is { } targetExpression &&
            AwkExpressionSyntaxFacts.TryGetIdentifier(targetExpression, out var target) &&
            AwkIdentifiers.IsReservedName(target.Name))
        {
            return InvalidExpression(targetExpression, "Reserved identifier `" + target.Name + "' cannot be used as an array name.");
        }

        if (expression.Kind == AwkExpressionKind.Variable &&
            AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier) &&
            AwkIdentifiers.IsReservedName(identifier.Name))
        {
            return InvalidExpression(expression, "Reserved identifier `" + identifier.Name + "' cannot be used as a variable.");
        }

        return InvalidExpression(expression, "Invalid expression.");
    }

    private static AwkDiagnostic InvalidExpression(AwkExpressionSyntax expression, string message) =>
        new(expression.Span, message, AwkDiagnosticId.BindInvalidProgram);

    public static bool TryLowerArrayIndex(
        IReadOnlyList<AwkExpressionSyntax> children,
        [NotNullWhen(true)] out AwkArrayIndex? lowered)
    {
        return TryLowerArrayIndex(children, AwkLoweringScope.Global, out lowered);
    }

    public static bool TryLowerArrayIndex(
        IReadOnlyList<AwkExpressionSyntax> children,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkArrayIndex? lowered)
    {
        return TryLowerCompositeIndexParts(children, scope, startIndex: 1, out lowered);
    }

    private static bool TryLowerCompositeIndexParts(
        IReadOnlyList<AwkExpressionSyntax> expressions,
        AwkLoweringScope scope,
        int startIndex,
        [NotNullWhen(true)] out AwkArrayIndex? lowered)
    {
        lowered = null;
        if (startIndex >= expressions.Count)
            return false;

        var parts = new List<AwkExpression>(expressions.Count - startIndex);
        for (var index = startIndex + 1; index < expressions.Count; index++)
        {
            if (!TryLower(expressions[index], scope, out var part))
                return false;

            parts.Add(part);
        }

        if (!TryLower(expressions[startIndex], scope, out var first))
            return false;

        parts.Insert(0, first);
        lowered = AwkArrayIndex.Composite(parts);
        return true;
    }

}
