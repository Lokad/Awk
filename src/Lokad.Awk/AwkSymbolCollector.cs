namespace Lokad.Awk;

internal sealed class AwkCollectedSymbols(
    IReadOnlyList<AwkSymbolReference> references,
    IReadOnlySet<AwkVariableName> globals,
    IReadOnlySet<AwkIdentifier> functionNames,
    IReadOnlyList<AwkFunctionLocalLayout> functionLayouts)
{
    public IReadOnlyList<AwkSymbolReference> References { get; } = references;
    public IReadOnlySet<AwkVariableName> Globals { get; } = globals;
    public IReadOnlySet<AwkIdentifier> FunctionNames { get; } = functionNames;

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
        return new(collector.Symbols, collector.Globals, collector.FunctionNames, collector.FunctionLayouts);
    }

    private sealed class Collector
    {
        private readonly AwkCompatibilityMode _mode;
        private readonly Action? _checkpoint;
        private AwkLoweringScope _scope;

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

        public List<AwkSymbolReference> Symbols { get; } = [];

        public HashSet<AwkVariableName> Globals { get; } = [];
        public HashSet<AwkIdentifier> FunctionNames { get; }
        public AwkFunctionLocalLayout[] FunctionLayouts { get; }

        public void CollectProgram(AwkSyntaxProgram syntax)
        {
            for (var index = 0; index < syntax.Functions.Count; index++)
            {
                _checkpoint?.Invoke();
                var function = syntax.Functions[index];
                if (function.Identifier.HasValue)
                    Symbols.Add(new AwkSymbolReference(function.Identifier.Value,
                        AwkSymbolReferenceKind.FunctionDefinition, function.Span));

                foreach (var token in function.ParameterTokens)
                {
                    _checkpoint?.Invoke();
                    if (token.Kind == AwkTokenKind.Identifier &&
                        AwkIdentifier.TryParse(token.Text, out var identifier))
                        Symbols.Add(new AwkSymbolReference(identifier,
                            AwkSymbolReferenceKind.FunctionParameter, token.Span, isFunctionLocal: true));
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
                        AddVariableReference(identifier, AwkSymbolReferenceKind.Variable);
                    return -1;
                case AwkExpressionKind.ArrayElement:
                    if (AwkExpressionSyntaxFacts.TryGetArrayElementTarget(expression, out var target))
                    {
                        AddVariableReference(target, AwkSymbolReferenceKind.Array);
                        firstChild = 1;
                    }
                    break;
                case AwkExpressionKind.FunctionCall:
                    if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var callable))
                    {
                        if (!callable.IsSpecialSyntax)
                        {
                            var local = _scope.TryGetFunctionLocalSlot(new(callable.Identifier), out _);
                            var kind = !local && AwkBuiltins.TryGetCallable(callable.Name, out var builtin) &&
                                AwkCompatibilityNames.EnablesBuiltin(builtin, _mode)
                                ? AwkSymbolReferenceKind.BuiltinFunction
                                : !local && FunctionNames.Contains(callable.Identifier)
                                    ? AwkSymbolReferenceKind.UserFunction
                                    : AwkSymbolReferenceKind.UnknownFunction;
                            Symbols.Add(new AwkSymbolReference(callable.Identifier, kind, callable.Span, local));
                        }
                        firstChild = 1;
                    }
                    break;
            }
            return firstChild;
        }

        private void AddVariableReference(AwkIdentifierSyntax identifier, AwkSymbolReferenceKind ordinaryKind)
        {
            var name = new AwkVariableName(identifier.Identifier);
            var local = _scope.TryGetFunctionLocalSlot(name, out _);
            var kind = ordinaryKind;
            if (!local && AwkSpecialVariables.TryGet(identifier.Name, out var special) &&
                AwkCompatibilityNames.EnablesSpecial(special.Id, _mode))
                kind = special is AwkSpecialArrayVariableDefinition
                    ? AwkSymbolReferenceKind.SpecialArray
                    : AwkSymbolReferenceKind.SpecialVariable;

            Symbols.Add(new AwkSymbolReference(identifier.Identifier, kind, identifier.Span, local));
            if (!local && !(AwkSymbolTableArray.IsName(identifier.Name) &&
                AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.SymbolTable, _mode)))
                Globals.Add(name);
        }
    }
}

internal sealed class AwkSymbolReference(
    AwkIdentifier identifier,
    AwkSymbolReferenceKind kind,
    AwkTextSpan span,
    bool isFunctionLocal = false)
{
    public AwkSymbolReferenceKind Kind { get; } = kind;

    public AwkTextSpan Span { get; } = span;
    public AwkIdentifier Identifier { get; } = identifier;

    public string Name => Identifier.Text;

    public bool IsFunctionLocal { get; } = isFunctionLocal;
}

internal enum AwkSymbolReferenceKind
{
    Variable,
    SpecialVariable,
    Array,
    SpecialArray,
    BuiltinFunction,
    UserFunction,
    UnknownFunction,
    FunctionDefinition,
    FunctionParameter
}
