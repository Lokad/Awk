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
            var pending = new Stack<(IReadOnlyList<AwkStatementSyntax> Statements, int Position, AwkSyntaxOptional<AwkExpressionSyntax> Label)>();
            pending.Push((statements, 0, default));
            while (pending.TryPop(out var frame))
            {
                _checkpoint?.Invoke();
                if (frame.Label.TryGetValue(out var label)) CollectExpression(label);
                if (frame.Position >= frame.Statements.Count) continue;
                var statement = frame.Statements[frame.Position];
                if (frame.Position + 1 < frame.Statements.Count)
                    pending.Push((frame.Statements, frame.Position + 1, default));
                foreach (var expression in statement.EnumerateChildExpressions())
                    CollectExpression(expression);
                // Preserve body/else/arm and symbol order without recursive descent.
                for (var index = statement.SwitchCases.Count - 1; index >= 0; index--)
                {
                    _checkpoint?.Invoke();
                    var arm = statement.SwitchCases[index];
                    pending.Push((arm.BodyStatements, 0, arm.Expression));
                }
                if (statement.ElseStatements.Count != 0) pending.Push((statement.ElseStatements, 0, default));
                if (statement.BodyStatements.Count != 0) pending.Push((statement.BodyStatements, 0, default));
            }
        }

        private void CollectExpression(AwkExpressionSyntax expression)
        {
            Stack<AwkExpressionSyntax>? pending = null;
            while (true)
            {
                _checkpoint?.Invoke();
                var firstChild = 0;
                switch (expression.Kind)
                {
                    case AwkExpressionKind.Variable:
                        if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier))
                            AddVariableReference(identifier, AwkSymbolReferenceKind.Variable);
                        goto NextExpression;
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

                if (firstChild < expression.Children.Count)
                {
                    if (firstChild + 1 < expression.Children.Count)
                    {
                        pending ??= new();
                        for (var index = expression.Children.Count - 1; index > firstChild; index--)
                        {
                            if ((index & 63) == 0) _checkpoint?.Invoke();
                            pending.Push(expression.Children[index]);
                        }
                    }
                    expression = expression.Children[firstChild];
                    continue;
                }
            NextExpression:
                if (pending is null || !pending.TryPop(out var next)) return;
                expression = next;
            }
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
