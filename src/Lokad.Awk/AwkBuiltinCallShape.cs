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
}

internal static class AwkBuiltinCallShape
{
    public static bool TryCreateKnownCallable(
        AwkExpressionSyntax expression,
        out AwkBuiltinCallSyntax call)
    {
        if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target))
            return TryCreateKnownCallable(expression, target, out call);

        call = default;
        return false;
    }

    internal static bool TryCreateKnownCallable(AwkExpressionSyntax expression, AwkCallableTargetSyntax target,
        out AwkBuiltinCallSyntax call)
    {
        if (!target.IsSpecialSyntax && AwkBuiltins.TryGetCallable(target.Name, out var definition))
        {
            call = new AwkBuiltinCallSyntax(definition, expression, target);
            return true;
        }

        call = default;
        return false;
    }

    public static bool TryGetDiagnostic(
        AwkBuiltinCallSyntax call,
        AwkLoweringScope scope,
        out AwkDiagnostic diagnostic)
    {
        if (!call.Definition.Arity.Accepts(call.ArgumentCount))
        {
            diagnostic = new(call.Expression.Span,
                "Invalid number of arguments for " + call.Target.Name, AwkDiagnosticId.BindInvalidProgram);
            return true;
        }

        static bool IsArrayName(AwkExpressionSyntax argument, AwkLoweringScope scope) =>
            AwkExpressionSyntaxFacts.TryGetArrayRoot(argument, out var identifier) &&
            (argument.Kind == AwkExpressionKind.Variable ||
                argument.Kind == AwkExpressionKind.ArrayElement && scope.CompatibilityMode == AwkCompatibilityMode.GnuDefault) &&
            !scope.IsReservedVariable(identifier.Identifier);

        // Arguments without an explicit role accept any scalar expression.
        var roles = call.Definition.ArgumentRoles;
        var count = Math.Min(call.ArgumentCount, roles.Count);
        for (var index = 0; index < count; index++)
        {
            var argument = call.Expression.Children[index + 1];
            var expected = roles[index] switch
            {
                AwkBuiltinArgumentRole.ArrayName when !IsArrayName(argument, scope) => "an array name",
                AwkBuiltinArgumentRole.LValue when !AwkExpressionSyntaxFacts.IsValidMutationTarget(argument, scope) =>
                    "an lvalue",
                _ => null
            };
            if (expected is null) continue;
            diagnostic = new(argument.Span, "Argument " + (index + 1).ToString(CultureInfo.InvariantCulture) +
                " of `" + call.Target.Name + "' must be " + expected + ".", AwkDiagnosticId.BindInvalidProgram);
            return true;
        }

        diagnostic = default;
        return false;
    }
}
