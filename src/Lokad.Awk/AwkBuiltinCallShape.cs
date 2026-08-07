using System;
using System.Globalization;

namespace Lokad.Awk;

internal readonly record struct AwkBuiltinCallSyntax(
    AwkBuiltinDefinition Definition,
    AwkExpressionSyntax Expression,
    AwkCallableTargetSyntax Target)
{
    public int ArgumentCount => Math.Max(0, Expression.Children.Count - 1);

    public AwkSyntaxOptional<AwkExpressionSyntax> GetArgument(int argumentIndex) =>
        argumentIndex >= 0 && argumentIndex < ArgumentCount
            ? AwkSyntaxOptional<AwkExpressionSyntax>.Some(Expression.Children[argumentIndex + 1])
            : AwkSyntaxOptional<AwkExpressionSyntax>.None;

    public bool TryGetIdentifierArgument(int argumentIndex, out AwkIdentifier identifier)
    {
        identifier = default;
        if (!GetArgument(argumentIndex).TryGetValue(out var argument) ||
            !AwkExpressionSyntaxFacts.TryGetAssignableIdentifier(argument, out var syntax))
        {
            return false;
        }

        identifier = syntax.Identifier;
        return true;
    }

    public bool TryGetArrayNameArgument(int argumentIndex, out AwkArrayName name)
    {
        name = default;
        if (!TryGetIdentifierArgument(argumentIndex, out var identifier))
            return false;

        name = new AwkArrayName(identifier);
        return true;
    }

    public bool TryGetOptionalArrayIdentifierArgument(
        int argumentIndex,
        out AwkOptionalIdentifier identifier)
    {
        identifier = AwkOptionalIdentifier.None;
        if (argumentIndex >= ArgumentCount)
            return true;

        if (!TryGetIdentifierArgument(argumentIndex, out var parsed))
            return false;

        identifier = AwkOptionalIdentifier.Some(parsed);
        return true;
    }
}

internal readonly record struct AwkBuiltinArgumentRoleFailure(
    int ArgumentIndex,
    AwkBuiltinArgumentRole Role,
    AwkExpressionSyntax Argument);

internal static class AwkBuiltinCallShape
{
    public static bool TryCreate(
        AwkBuiltinDefinition definition,
        AwkExpressionSyntax expression,
        out AwkBuiltinCallSyntax call)
    {
        call = default;
        if (!AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) ||
            !string.Equals(target.Name, definition.Name, StringComparison.Ordinal))
        {
            return false;
        }

        call = new AwkBuiltinCallSyntax(definition, expression, target);
        return true;
    }

    public static bool TryCreateKnownCallable(
        AwkExpressionSyntax expression,
        out AwkBuiltinCallSyntax call)
    {
        call = default;
        if (!AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) ||
            target.IsSpecialSyntax ||
            !AwkBuiltins.TryGetCallable(target.Name, out var definition))
        {
            return false;
        }

        call = new AwkBuiltinCallSyntax(definition, expression, target);
        return true;
    }

    public static bool TryCreateStatementCall(
        AwkStatementSyntax statement,
        AwkBuiltinId id,
        out AwkBuiltinCallSyntax call)
    {
        call = default;
        if (statement.Expressions.Count != 1 ||
            !AwkBuiltins.TryGetStatementDefinition(statement.Kind, out var definition) ||
            definition.Id != id ||
            !TryCreate(definition, statement.Expressions[0], out call) ||
            !AcceptsArgumentCount(definition, call.ArgumentCount) ||
            !AcceptsArgumentRoles(call))
        {
            return false;
        }

        return true;
    }

    public static bool AcceptsArgumentCount(AwkBuiltinDefinition definition, int argumentCount) =>
        definition.Arity.Accepts(argumentCount);

    public static bool AcceptsArgumentRoles(AwkBuiltinCallSyntax call)
    {
        return !TryGetArgumentRoleFailure(call, out _);
    }

    public static bool TryGetArgumentRoleFailure(
        AwkBuiltinCallSyntax call,
        out AwkBuiltinArgumentRoleFailure failure)
    {
        for (var index = 0; index < call.ArgumentCount; index++)
        {
            if (!call.GetArgument(index).TryGetValue(out var argument) ||
                !AcceptsArgumentRole(call.Definition.GetArgumentRole(index), argument))
            {
                failure = new AwkBuiltinArgumentRoleFailure(
                    index,
                    call.Definition.GetArgumentRole(index),
                    argument);
                return true;
            }
        }

        failure = default;
        return false;
    }

    public static string FormatArgumentRoleFailure(
        AwkBuiltinCallSyntax call,
        AwkBuiltinArgumentRoleFailure failure)
    {
        var expected = failure.Role switch
        {
            AwkBuiltinArgumentRole.ArrayName => "an array name",
            AwkBuiltinArgumentRole.LValue => "an lvalue",
            AwkBuiltinArgumentRole.ScalarOrArray => "a scalar or array expression",
            _ => "a scalar expression"
        };

        return "Argument " + (failure.ArgumentIndex + 1).ToString(CultureInfo.InvariantCulture) +
            " of `" + call.Target.Name + "' must be " + expected + ".";
    }

    private static bool AcceptsArgumentRole(
        AwkBuiltinArgumentRole role,
        AwkExpressionSyntax argument) =>
        role switch
        {
            AwkBuiltinArgumentRole.ArrayName => IsArrayName(argument),
            AwkBuiltinArgumentRole.LValue => AwkExpressionSyntaxFacts.IsValidMutationTarget(argument),
            _ => true
        };

    private static bool IsArrayName(AwkExpressionSyntax argument) =>
        AwkExpressionSyntaxFacts.TryGetAssignableIdentifier(argument, out _);
}
