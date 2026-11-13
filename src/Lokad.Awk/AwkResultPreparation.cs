namespace Lokad.Awk;

// Prepare result observations and choose existing owners/locations. Counts denote
// length operations, never numeric substitutes stored in logical string values.
internal static partial class AwkResultPreparation
{
    internal static Dictionary<int, List<AwkScalarResult>> Prepare(AwkSyntaxProgram syntax,
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions,
        IReadOnlyList<AwkSymbolReference>? symbols, AwkRuntimeArgumentPlan? arguments,
        AwkCompatibilityMode mode, AwkStringUnitMode stringUnits, Action? checkpoint)
    {
        Dictionary<int, List<AwkScalarResult>> result = [];
        // Standalone/internal preparation can have callers that observe its globals.
        if (arguments is null || symbols is null || functions.Count == 0) return result;
        foreach (var symbol in symbols)
        {
            checkpoint?.Invoke();
            // Dynamic argument assignments and symbol-table access defeat a closed range.
            if (symbol.Name is "ARGV" or "SYMTAB") return result;
        }

        var definitions = new Dictionary<string, Definition>(StringComparer.Ordinal);
        var lengthReads = new HashSet<AwkTextSpan>();
        var statements = new Stack<(AwkStatementSyntax Statement, bool Unconditional)>();
        var expressions = new Stack<AwkExpressionSyntax>();
        for (var ruleIndex = 0; ruleIndex < syntax.Rules.Count; ruleIndex++)
        {
            checkpoint?.Invoke();
            var rule = syntax.Rules[ruleIndex];
            if (rule.Kind != AwkRuleKind.Main) continue;
            foreach (var statement in rule.ActionStatements) statements.Push((statement, true));
            while (statements.TryPop(out var pending))
            {
                checkpoint?.Invoke();
                var statement = pending.Statement;
                if (pending.Unconditional && statement.Kind == AwkStatementKind.Expression &&
                    statement.Expression.TryGetValue(out var assignment))
                {
                    assignment = Ungroup(assignment);
                    if (assignment.Kind == AwkExpressionKind.Assignment && assignment.Children.Count == 2 &&
                        assignment.OperatorTokens.Count == 1 && assignment.OperatorTokens[0].Kind == AwkTokenKind.Equal &&
                        AwkExpressionSyntaxFacts.TryGetIdentifier(assignment.Children[0], out var name) &&
                        !AwkSpecialVariables.TryGet(name.Name, out _) &&
                        AwkExpressionSyntaxFacts.TryGetCallableTarget(Ungroup(assignment.Children[1]), out var call) &&
                        functions.TryGetValue(new(call.Identifier), out var function))
                        definitions.TryAdd(name.Name, new(ruleIndex, rule.Span, name.Span, assignment.Span.End, function));
                }
                foreach (var expression in statement.EnumerateChildExpressions()) expressions.Push(expression);
                while (expressions.TryPop(out var expression))
                {
                    checkpoint?.Invoke();
                    if (expression.Children.Count == 2 &&
                        AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var call) &&
                        AwkBuiltins.IsName(call.Name, AwkBuiltinId.Length) &&
                        AwkExpressionSyntaxFacts.TryGetIdentifier(Ungroup(expression.Children[1]), out var name))
                        lengthReads.Add(name.Span);
                    foreach (var child in expression.Children) expressions.Push(child);
                }
                var unconditional = pending.Unconditional && statement.Kind == AwkStatementKind.Block;
                foreach (var child in statement.BodyStatements) statements.Push((child, unconditional));
                foreach (var child in statement.ElseStatements) statements.Push((child, false));
                foreach (var arm in statement.SwitchCases)
                    foreach (var child in arm.BodyStatements) statements.Push((child, false));
            }
        }
        foreach (var symbol in symbols)
        {
            checkpoint?.Invoke();
            if (symbol.IsFunctionLocal || !definitions.TryGetValue(symbol.Name, out var definition)) continue;
            if (symbol.Kind == AwkSymbolReferenceKind.Variable && symbol.Span.Equals(definition.Target)) continue;
            if (symbol.Kind == AwkSymbolReferenceKind.Variable && symbol.Span.Start >= definition.End &&
                symbol.Span.End <= definition.Rule.End && lengthReads.Contains(symbol.Span))
                definition.Reads++;
            else definitions.Remove(symbol.Name);
        }
        foreach (var variable in arguments.Variables)
        {
            checkpoint?.Invoke();
            definitions.Remove(variable.Identifier.Text);
        }
        // Borrow argument prefixes rather than copying host-controlled assignment names.
        var names = definitions.GetAlternateLookup<ReadOnlySpan<char>>();
        foreach (var argument in arguments.RuntimeArguments)
        {
            checkpoint?.Invoke();
            var equal = argument.IndexOf('=');
            if (equal > 0) names.Remove(argument.AsSpan(0, equal));
        }
        foreach (var (name, definition) in definitions)
        {
            checkpoint?.Invoke();
            if (definition.Reads == 0) continue;
            if (!result.TryGetValue(definition.RuleIndex, out var values))
                result.Add(definition.RuleIndex, values = []);
            values.Add(new(AwkVariableName.FromText(name), definition.Function));
        }
        PrepareFunctions(syntax, functions, definitions, symbols, mode, stringUnits, checkpoint);
        return result;

        AwkExpressionSyntax Ungroup(AwkExpressionSyntax expression)
        {
            while (expression.Kind == AwkExpressionKind.Group && expression.Children.Count == 1)
            {
                checkpoint?.Invoke();
                expression = expression.Children[0];
            }
            return expression;
        }
    }

    private sealed class Definition(int ruleIndex, AwkTextSpan rule, AwkTextSpan target, int end, AwkUserFunction function)
    {
        internal int RuleIndex { get; } = ruleIndex;
        internal AwkTextSpan Rule { get; } = rule;
        internal AwkTextSpan Target { get; } = target;
        internal int End { get; } = end;
        internal AwkUserFunction Function { get; } = function;
        internal int Reads { get; set; }
    }
}

internal sealed record AwkScalarResult(AwkVariableName Name, AwkUserFunction Function);
