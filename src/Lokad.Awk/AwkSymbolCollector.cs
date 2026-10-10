using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSymbolCollector
{
    public static IReadOnlyList<AwkSymbolReference> Collect(AwkSyntaxProgram syntax) =>
        Collect(syntax, out _);

    public static IReadOnlyList<AwkSymbolReference> Collect(
        AwkSyntaxProgram syntax,
        out IReadOnlySet<AwkVariableName> globalNames,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault)
    {
        var collector = new Collector(syntax, mode);
        collector.CollectProgram(syntax);
        globalNames = collector.Globals;
        return collector.Symbols;
    }

    private sealed class Collector
    {
        private readonly AwkCompatibilityMode _mode;
        private readonly HashSet<AwkIdentifier> _functionNames;
        private AwkLoweringScope _scope;

        public Collector(AwkSyntaxProgram syntax, AwkCompatibilityMode mode)
        {
            _mode = mode;
            _scope = AwkLoweringScope.GlobalFor(mode);
            _functionNames = new(syntax.Functions.Count);
            foreach (var function in syntax.Functions)
            {
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
                if (function.Identifier.HasValue)
                    Symbols.Add(new AwkNamedSymbolReference(function.Identifier.Value,
                        AwkSymbolReferenceKind.FunctionDefinition, function.Span));

                foreach (var token in function.ParameterTokens)
                {
                    if (token.Kind == AwkTokenKind.Identifier &&
                        AwkIdentifier.TryParse(token.Text, out var identifier))
                        Symbols.Add(new AwkNamedSymbolReference(identifier,
                            AwkSymbolReferenceKind.FunctionParameter, token.Span, isFunctionLocal: true));
                }

                _scope = AwkLoweringScope.Function(new(function.ParameterBinding.Parameters), _mode);
                CollectStatements(function.BodyStatements);
            }

            _scope = AwkLoweringScope.GlobalFor(_mode);
            foreach (var rule in syntax.Rules)
            {
                foreach (var expression in rule.PatternSyntax.EnumerateChildExpressions())
                    CollectExpression(expression);
                CollectStatements(rule.ActionStatements);
            }
        }

        private void CollectStatements(IReadOnlyList<AwkStatementSyntax> statements)
        {
            foreach (var statement in statements)
            {
                foreach (var expression in statement.EnumerateChildExpressions())
                    CollectExpression(expression);
                CollectStatements(statement.BodyStatements);
                CollectStatements(statement.ElseStatements);
                foreach (var switchCase in statement.SwitchCases)
                {
                    if (switchCase.Expression.TryGetValue(out var expression))
                        CollectExpression(expression);
                    CollectStatements(switchCase.BodyStatements);
                }
            }
        }

        private void CollectExpression(AwkExpressionSyntax? expression)
        {
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
                case AwkExpressionKind.Field:
                    Symbols.Add(new AwkFieldSymbolReference(expression.Span));
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
                            Symbols.Add(new AwkNamedSymbolReference(callable.Identifier, kind, callable.Span, local));
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

            Symbols.Add(new AwkNamedSymbolReference(identifier.Identifier, kind, identifier.Span, local));
            if (!local && !(AwkSymbolTableArray.IsName(identifier.Name) &&
                AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.SymbolTable, _mode)))
                Globals.Add(name);
        }
    }
}

internal abstract class AwkSymbolReference(
    AwkSymbolReferenceKind kind,
    AwkTextSpan span)
{
    public AwkSymbolReferenceKind Kind { get; } = kind;

    public AwkTextSpan Span { get; } = span;
}

internal sealed class AwkNamedSymbolReference(
    AwkIdentifier identifier,
    AwkSymbolReferenceKind kind,
    AwkTextSpan span,
    bool isFunctionLocal = false)
    : AwkSymbolReference(kind, span)
{
    public AwkIdentifier Identifier { get; } = identifier;

    public string Name => Identifier.Text;

    public bool IsFunctionLocal { get; } = isFunctionLocal;
}

internal sealed class AwkFieldSymbolReference(AwkTextSpan span)
    : AwkSymbolReference(AwkSymbolReferenceKind.Field, span)
{
}

internal enum AwkSymbolReferenceKind
{
    Variable,
    SpecialVariable,
    Array,
    SpecialArray,
    Field,
    BuiltinFunction,
    UserFunction,
    UnknownFunction,
    FunctionDefinition,
    FunctionParameter
}
