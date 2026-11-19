namespace Lokad.Awk;

internal sealed class AwkCollectedSymbols(
    IReadOnlyList<AwkIdentifierSyntax> functionConflicts,
    IReadOnlyList<AwkIdentifierSyntax> undefinedCalls,
    IReadOnlySet<AwkVariableName> globals,
    IReadOnlySet<AwkIdentifier> functionNames,
    IReadOnlyList<AwkFunctionLocalLayout> functionLayouts,
    bool hasDynamicVariables,
    AwkUnobservedRecordHeaders unobservedRecordHeaders)
{
    public IReadOnlyList<AwkIdentifierSyntax> FunctionConflicts { get; } = functionConflicts;
    public IReadOnlyList<AwkIdentifierSyntax> UndefinedCalls { get; } = undefinedCalls;
    public IReadOnlySet<AwkVariableName> Globals { get; } = globals;
    public IReadOnlySet<AwkIdentifier> FunctionNames { get; } = functionNames;
    public bool HasDynamicVariables { get; } = hasDynamicVariables;
    public AwkUnobservedRecordHeaders UnobservedRecordHeaders { get; } = unobservedRecordHeaders;

    // Indexed in syntax order, including declarations that validation rejects.
    public IReadOnlyList<AwkFunctionLocalLayout> FunctionLayouts { get; } = functionLayouts;
}

internal static class AwkSymbolCollector
{
    public static AwkCollectedSymbols Collect(
        AwkSyntaxProgram syntax,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var collector = new Collector(syntax, mode, checkpoint);
        collector.CollectProgram(syntax);
        return collector.Complete();
    }

    private sealed class Collector
    {
        private readonly AwkCompatibilityMode _mode;
        private readonly Action? _checkpoint;
        private AwkLoweringScope _scope;
        private List<AwkIdentifierSyntax>? _functionConflicts;
        private List<AwkIdentifierSyntax>? _undefinedCalls;
        private bool _hasDynamicVariables;
        private AwkUnobservedRecordHeaders _unobservedRecordHeaders = AwkUnobservedRecordHeaders.All;

        public Collector(AwkSyntaxProgram syntax, AwkCompatibilityMode mode, Action? checkpoint)
        {
            _mode = mode;
            _checkpoint = checkpoint;
            _scope = AwkLoweringScope.GlobalFor(mode, checkpoint);
            FunctionNames = new(syntax.Functions.Count);
            FunctionLayouts = syntax.Functions.Count == 0 ? [] : new AwkFunctionLocalLayout[syntax.Functions.Count];
            foreach (var function in syntax.Functions)
            {
                _checkpoint?.Invoke();
                if (function.Identifier.HasValue)
                    FunctionNames.Add(function.Identifier.Value);
            }
        }

        public HashSet<AwkVariableName> Globals { get; } = [];
        public HashSet<AwkIdentifier> FunctionNames { get; }
        public AwkFunctionLocalLayout[] FunctionLayouts { get; }

        public AwkCollectedSymbols Complete() =>
            new(_functionConflicts is { } conflicts ? conflicts : [], _undefinedCalls is { } calls ? calls : [],
                Globals, FunctionNames, FunctionLayouts, _hasDynamicVariables, _unobservedRecordHeaders);

        public void CollectProgram(AwkSyntaxProgram syntax)
        {
            for (var index = 0; index < syntax.Functions.Count; index++)
            {
                _checkpoint?.Invoke();
                var function = syntax.Functions[index];
                if (function.Identifier.HasValue)
                    ObserveName(function.Name);

                foreach (var token in function.ParameterTokens)
                {
                    _checkpoint?.Invoke();
                    if (token.TryGetIdentifier(out var identifier))
                        ObserveName(identifier.Text);
                }

                var layout = new AwkFunctionLocalLayout(function.ParameterBinding.Parameters, _checkpoint);
                FunctionLayouts[index] = layout;
                _scope = AwkLoweringScope.Function(layout, _mode, _checkpoint);
                CollectStatements(function.BodyStatements);
            }

            _scope = AwkLoweringScope.GlobalFor(_mode, _checkpoint);
            foreach (var rule in syntax.Rules)
            {
                _checkpoint?.Invoke();
                foreach (var expression in rule.PatternSyntax.EnumerateChildExpressions())
                    CollectExpression(expression);
                CollectStatements(rule.ActionStatements);
            }
        }

        private void CollectStatements(IReadOnlyList<AwkStatementSyntax> statements) =>
            AwkStatementSyntax.Walk(statements, _checkpoint, this,
                static (statement, collector) =>
                {
                    foreach (var expression in statement.EnumerateChildExpressions())
                        collector.CollectExpression(expression);
                    return collector;
                },
                static (arm, collector) =>
                {
                    if (arm.Expression.TryGetValue(out var label)) collector.CollectExpression(label);
                    return collector;
                });

        private void CollectExpression(AwkExpressionSyntax expression) =>
            AwkExpressionSyntaxFacts.Walk(expression, _checkpoint, this,
                static (node, collector) => collector.CollectExpressionNode(node));

        private int CollectExpressionNode(AwkExpressionSyntax expression)
        {
            var firstChild = 0;
            switch (expression.Kind)
            {
                case AwkExpressionKind.Variable:
                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier))
                        AddVariable(identifier);
                    return -1;
                case AwkExpressionKind.ArrayElement:
                    if (AwkExpressionSyntaxFacts.TryGetArrayElementTarget(expression, out var target))
                    {
                        AddVariable(target);
                        firstChild = 1;
                    }
                    break;
                case AwkExpressionKind.FunctionCall:
                    if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var callable))
                    {
                        if (!callable.IsSpecialSyntax)
                        {
                            ObserveName(callable.Name);
                            if (!_scope.TryGetFunctionLocalSlot(new(callable.Identifier), out _) &&
                                !(AwkBuiltins.TryGetCallable(callable.Name, out var builtin) &&
                                  AwkCompatibilityNames.EnablesBuiltin(builtin, _mode)) &&
                                !FunctionNames.Contains(callable.Identifier))
                            {
                                (_undefinedCalls ??= []).Add(callable.IdentifierSyntax);
                            }
                        }
                        firstChild = 1;
                    }
                    break;
            }
            return firstChild;
        }

        private void AddVariable(AwkIdentifierSyntax identifier)
        {
            var name = new AwkVariableName(identifier.Identifier);
            ObserveName(identifier.Name);
            if (_scope.TryGetFunctionLocalSlot(name, out _))
                return;

            if (FunctionNames.Count != 0 && FunctionNames.Contains(identifier.Identifier) &&
                !(AwkSpecialVariables.TryGet(identifier.Name, out var special) &&
                  AwkCompatibilityNames.EnablesSpecial(special.Id, _mode)))
                (_functionConflicts ??= []).Add(identifier);

            if (!(AwkSymbolTableArray.IsName(identifier.Name) &&
                  AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.SymbolTable, _mode)))
                Globals.Add(name);
        }

        private void ObserveName(string name)
        {
            switch (name)
            {
                case "ARGV" or "SYMTAB":
                    _hasDynamicVariables = true;
                    _unobservedRecordHeaders = AwkUnobservedRecordHeaders.None;
                    break;
                case "NR":
                    _unobservedRecordHeaders &= ~AwkUnobservedRecordHeaders.NR;
                    break;
                case "FNR":
                    _unobservedRecordHeaders &= ~AwkUnobservedRecordHeaders.FNR;
                    break;
                case "RT":
                    _unobservedRecordHeaders &= ~AwkUnobservedRecordHeaders.RT;
                    break;
            }
        }
    }
}
