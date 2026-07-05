using System;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    private static bool TryLowerUnary(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (expression.OperatorTokens.Count != 1 ||
            expression.Children.Count != 1)
        {
            return false;
        }

        var operatorKind = expression.OperatorTokens[0].Kind;
        if (operatorKind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus)
        {
            var target = AwkLValueLowerer.Lower(expression.Children[0], scope);
            if (!target.Succeeded)
                return false;

            lowered = operatorKind == AwkTokenKind.PlusPlus
                ? AwkExpression.PreIncrement(target.Reference)
                : AwkExpression.PreDecrement(target.Reference);
            return true;
        }

        if (!TryLower(expression.Children[0], scope, out var operand))
            return false;

        lowered = operatorKind switch
        {
            AwkTokenKind.Bang => AwkExpression.Not(operand),
            AwkTokenKind.Minus => AwkExpression.Negate(operand),
            AwkTokenKind.Plus => operand,
            _ => null
        };

        return lowered != null;
    }

    private static bool TryLowerPostfix(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        var target = expression.Children.Count == 1
            ? AwkLValueLowerer.Lower(expression.Children[0], scope)
            : AwkLValueLoweringResult.Invalid;
        if (expression.OperatorTokens.Count != 1 ||
            expression.Children.Count != 1 ||
            !target.Succeeded)
        {
            return false;
        }

        lowered = expression.OperatorTokens[0].Kind switch
        {
            AwkTokenKind.PlusPlus => AwkExpression.PostIncrement(target.Reference),
            AwkTokenKind.MinusMinus => AwkExpression.PostDecrement(target.Reference),
            _ => null
        };

        return lowered != null;
    }

    private static bool TryLowerAssignment(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        var target = expression.Children.Count == 2
            ? AwkLValueLowerer.Lower(expression.Children[0], scope)
            : AwkLValueLoweringResult.Invalid;
        if (expression.OperatorTokens.Count != 1 ||
            expression.Children.Count != 2 ||
            !target.Succeeded ||
            !TryLower(expression.Children[1], scope, out var value))
        {
            return false;
        }

        var operatorKind = expression.OperatorTokens[0].Kind;
        if (operatorKind == AwkTokenKind.Equal)
        {
            lowered = AwkExpression.Assignment(target.Reference, value);
            return true;
        }

        if (!AwkAssignmentOperators.TryGetCompoundOperation(operatorKind, out var operation))
            return false;

        lowered = AwkExpression.CompoundAssignment(target.Reference, value, operation);
        return true;
    }

    private static bool TryLowerBinary(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (expression.OperatorTokens.Count != 1 ||
            expression.Children.Count != 2)
        {
            return false;
        }

        var rightSyntax = expression.Children[1];
        var operatorKind = expression.OperatorTokens[0].Kind;
        if (operatorKind == AwkTokenKind.In)
        {
            if (!AwkFrontendClassifier.TryGetIdentifier(rightSyntax, out var arrayName) ||
                !TryLowerMembershipIndex(expression.Children[0], scope, out var membershipIndex))
            {
                return false;
            }

            lowered = AwkExpression.ArrayMembership(membershipIndex, arrayName.Name);
            return true;
        }

        if (!TryLower(expression.Children[0], scope, out var left))
            return false;

        if (!TryLower(rightSyntax, scope, out var right))
            return false;

        lowered = operatorKind switch
        {
            AwkTokenKind.PipePipe => AwkExpression.LogicalOr(left, right),
            AwkTokenKind.AmpersandAmpersand => AwkExpression.LogicalAnd(left, right),
            AwkTokenKind.Tilde => AwkExpression.RegexMatch(left, right, negate: false),
            AwkTokenKind.BangTilde => AwkExpression.RegexMatch(left, right, negate: true),
            AwkTokenKind.EqualEqual => AwkExpression.Compare(left, right, AwkComparison.Equal),
            AwkTokenKind.BangEqual => AwkExpression.Compare(left, right, AwkComparison.NotEqual),
            AwkTokenKind.Less => AwkExpression.Compare(left, right, AwkComparison.LessThan),
            AwkTokenKind.LessEqual => AwkExpression.Compare(left, right, AwkComparison.LessOrEqual),
            AwkTokenKind.Greater => AwkExpression.Compare(left, right, AwkComparison.GreaterThan),
            AwkTokenKind.GreaterEqual => AwkExpression.Compare(left, right, AwkComparison.GreaterOrEqual),
            AwkTokenKind.Plus => AwkExpression.Add(left, right),
            AwkTokenKind.Minus => AwkExpression.Subtract(left, right),
            AwkTokenKind.Star => AwkExpression.Multiply(left, right),
            AwkTokenKind.Slash => AwkExpression.Divide(left, right),
            AwkTokenKind.Percent => AwkExpression.Modulo(left, right),
            AwkTokenKind.Caret => AwkExpression.Power(left, right),
            _ => null
        };

        return lowered != null;
    }

    private static bool TryLowerMembershipIndex(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkArrayIndex? lowered)
    {
        lowered = null;
        if (expression.Kind == AwkExpressionKind.Group &&
            expression.FirstChild is { Kind: AwkExpressionKind.ExpressionList } expressionList)
        {
            return TryLowerCompositeIndexParts(expressionList.Children, scope, 0, out lowered);
        }

        if (expression.Kind == AwkExpressionKind.ExpressionList)
            return TryLowerCompositeIndexParts(expression.Children, scope, 0, out lowered);

        if (!TryLower(expression, scope, out var index))
            return false;

        lowered = AwkArrayIndex.Simple(index);
        return true;
    }

    private static bool TryLowerConditional(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (expression.Children.Count != 3 ||
            !TryLower(expression.Children[0], scope, out var condition) ||
            !TryLower(expression.Children[1], scope, out var whenTrue) ||
            !TryLower(expression.Children[2], scope, out var whenFalse))
        {
            return false;
        }

        lowered = AwkExpression.Ternary(condition, whenTrue, whenFalse);
        return true;
    }

    private static bool TryLowerBinaryExpression(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        Func<AwkExpression, AwkExpression, AwkExpression> factory,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (expression.Children.Count != 2 ||
            !TryLower(expression.Children[0], scope, out var left) ||
            !TryLower(expression.Children[1], scope, out var right))
        {
            return false;
        }

        lowered = factory(left, right);
        return true;
    }
}
