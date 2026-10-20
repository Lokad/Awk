using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSymbolCollector
{

    public static IReadOnlyList<AwkSymbolReference> Collect(
        AwkSyntaxProgram syntax,
        out IReadOnlySet<AwkVariableName> globalNames,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var collector = new Collector(syntax, mode, checkpoint);
        collector.CollectProgram(syntax);
        globalNames = collector.Globals;
        return collector.Symbols;
    }

    private sealed class Collector
    {
        private readonly AwkCompatibilityMode _mode;
        private readonly Action? _checkpoint;
        private readonly HashSet<AwkIdentifier> _functionNames;
        private AwkLoweringScope _scope;

        public Collector(AwkSyntaxProgram syntax, AwkCompatibilityMode mode, Action? checkpoint)
        {
            _mode = mode;
            _checkpoint = checkpoint;
            _scope = AwkLoweringScope.GlobalFor(mode, checkpoint);
            _functionNames = new(syntax.Functions.Count);
            foreach (var function in syntax.Functions)
            {
                _checkpoint?.Invoke();
                if (function.Identifier.HasValue)
                    _functionNames.Add(function.Identifier.Value);
            }
        }

        public List<AwkSymbolReference> Symbols { get; } = [];

        public HashSet<AwkVariableName> Globals { get; } = [];

        public void CollectProgram(AwkSyntaxProgram syntax)
        {
            foreach (var function in syntax.Functions)
            {
                _checkpoint?.Invoke();
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

                _scope = AwkLoweringScope.Function(new(function.ParameterBinding.Parameters, _checkpoint), _mode, _checkpoint);
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

        private void CollectStatements(IReadOnlyList<AwkStatementSyntax> statements)
        {
            foreach (var statement in statements)
            {
                _checkpoint?.Invoke();
                foreach (var expression in statement.EnumerateChildExpressions())
                    CollectExpression(expression);
                CollectStatements(statement.BodyStatements);
                CollectStatements(statement.ElseStatements);
                foreach (var switchCase in statement.SwitchCases)
                {
                    _checkpoint?.Invoke();
                    if (switchCase.Expression.TryGetValue(out var expression))
                        CollectExpression(expression);
                    CollectStatements(switchCase.BodyStatements);
                }
            }
        }

        private void CollectExpression(AwkExpressionSyntax? expression)
        {
            _checkpoint?.Invoke();
            if (expression is null)
                return;

            var firstChild = 0;
            switch (expression.Kind)
            {
                case AwkExpressionKind.Variable:
                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier))
                        AddVariableReference(identifier, AwkSymbolReferenceKind.Variable);
                    return;
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
                                : !local && _functionNames.Contains(callable.Identifier)
                                    ? AwkSymbolReferenceKind.UserFunction
                                    : AwkSymbolReferenceKind.UnknownFunction;
                            Symbols.Add(new AwkSymbolReference(callable.Identifier, kind, callable.Span, local));
                        }
                        firstChild = 1;
                    }
                    break;
            }

            for (var index = firstChild; index < expression.Children.Count; index++)
                CollectExpression(expression.Children[index]);
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
