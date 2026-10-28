using System;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkBuiltinCallSyntax(
    AwkBuiltinDefinition? definition,
    AwkExpressionSyntax? expression,
    AwkCallableTargetSyntax target)
{
    public AwkBuiltinDefinition Definition => definition ??
        throw new InvalidOperationException("The default builtin call has no definition.");

    public AwkExpressionSyntax Expression => expression ??
        throw new InvalidOperationException("The default builtin call has no expression.");

    public AwkCallableTargetSyntax Target { get; } = target;

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

internal readonly struct AwkBuiltinArgumentRoleFailure(
    int argumentIndex,
    AwkBuiltinArgumentRole role,
    AwkExpressionSyntax? argument)
{
    public int ArgumentIndex { get; } = argumentIndex;

    public AwkBuiltinArgumentRole Role { get; } = role;

    public AwkExpressionSyntax Argument => argument ??
        throw new InvalidOperationException("The default role failure has no argument.");
}

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

    public static bool AcceptsArgumentRoles(AwkBuiltinCallSyntax call, AwkLoweringScope scope) =>
        !TryGetArgumentRoleFailure(call, scope, out _);

    public static bool TryGetArgumentRoleFailure(
        AwkBuiltinCallSyntax call,
        out AwkBuiltinArgumentRoleFailure failure) =>
        TryGetArgumentRoleFailure(call, AwkLoweringScope.Global, out failure);

    public static bool TryGetArgumentRoleFailure(
        AwkBuiltinCallSyntax call,
        AwkLoweringScope scope,
        out AwkBuiltinArgumentRoleFailure failure)
    {
        static bool IsArrayName(AwkExpressionSyntax argument, AwkLoweringScope scope) =>
            AwkExpressionSyntaxFacts.TryGetArrayRoot(argument, out var identifier) &&
            (argument.Kind == AwkExpressionKind.Variable ||
                argument.Kind == AwkExpressionKind.ArrayElement && scope.CompatibilityMode == AwkCompatibilityMode.GnuDefault) &&
            !scope.IsReservedVariable(identifier.Name);

        static bool AcceptsArgumentRole(
            AwkBuiltinArgumentRole role,
            AwkExpressionSyntax argument,
            AwkLoweringScope scope) =>
            role switch
            {
                AwkBuiltinArgumentRole.ArrayName => IsArrayName(argument, scope),
                AwkBuiltinArgumentRole.LValue =>
                    AwkLValueLowerer.Lower(argument, scope).Succeeded,
                _ => true
            };

        for (var index = 0; index < call.ArgumentCount; index++)
        {
            if (!call.GetArgument(index).TryGetValue(out var argument))
                throw new InvalidOperationException("A counted builtin argument must be present.");

            if (!AcceptsArgumentRole(call.Definition.GetArgumentRole(index), argument, scope))
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
}
