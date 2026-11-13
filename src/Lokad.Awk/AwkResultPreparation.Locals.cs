namespace Lokad.Awk;

internal static partial class AwkResultPreparation
{
    private static void PrepareLocalLengths(AwkFunctionSyntax syntax, AwkUserFunction function,
        int[] writes, LocalTextDefinition?[] definitions, AwkCompatibilityMode mode, Action? checkpoint)
    {
        var candidates = new Dictionary<AwkVariableName, LocalTextDefinition>();
        for (var index = 0; index < definitions.Length; index++)
        {
            checkpoint?.Invoke();
            if (writes[index] != 1 || definitions[index] is not { } definition) continue;
            var value = Ungroup(definition.Value, checkpoint);
            if (AwkExpressionSyntaxFacts.TryGetCallableTarget(value, out var call) &&
                !function.LocalLayout.TryGetSlot(new(call.Identifier), out _) &&
                AwkBuiltins.TryGetCallable(call.Name, out var builtin) &&
                AwkCompatibilityNames.EnablesBuiltin(builtin, mode) &&
                builtin.Id is AwkBuiltinId.ToLower or AwkBuiltinId.ToUpper)
                candidates.Add(function.Parameters[index], definition);
        }
        if (candidates.Count == 0) return;
        var pending = new Stack<(AwkExpressionSyntax Expression, bool Count)>();
        foreach (var (statement, _) in Statements(syntax.BodyStatements, checkpoint))
        {
            foreach (var expression in statement.EnumerateChildExpressions())
                pending.Push((expression, statement.Kind == AwkStatementKind.Return));
            foreach (var arm in statement.SwitchCases)
                if (arm.Expression.TryGetValue(out var expression)) pending.Push((expression, false));
            while (pending.TryPop(out var work))
            {
                checkpoint?.Invoke();
                var expression = Ungroup(work.Expression, checkpoint);
                if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var name))
                {
                    var variable = new AwkVariableName(name.Identifier);
                    if (candidates.TryGetValue(variable, out var definition) &&
                        !definition.Target.Equals(name.Span) && name.Span.Start >= definition.End && !work.Count)
                        candidates.Remove(variable);
                    continue;
                }
                if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var call) &&
                    !function.LocalLayout.TryGetSlot(new(call.Identifier), out _) &&
                    AwkBuiltins.TryGetCallable(call.Name, out var builtin) &&
                    AwkCompatibilityNames.EnablesBuiltin(builtin, mode))
                {
                    for (var index = 1; index < expression.Children.Count; index++)
                        pending.Push((expression.Children[index],
                            builtin.Id == AwkBuiltinId.Length && expression.Children.Count == 2 ||
                            builtin.Id == AwkBuiltinId.Substr && work.Count && index == 1));
                    continue;
                }
                // Only explicit length/slice consumers accept an existing count.
                // General branches, calls, observations and escapes need real text.
                foreach (var child in expression.Children) pending.Push((child, false));
            }
        }
        if (candidates.Count != 0) function.PrepareLocalLengths(candidates.Keys.ToArray());
    }
}
