using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSymbolCollector
{
    public static IReadOnlyList<AwkSymbolReference> Collect(AwkSyntaxProgram syntax) =>
        Collect(syntax, out _);

    public static IReadOnlyList<AwkSymbolReference> Collect(
        AwkSyntaxProgram syntax,
        out IReadOnlySet<AwkVariableName> globalNames)
    {
        var functionNames = new HashSet<AwkIdentifier>(syntax.Functions.Count);
        for (var index = 0; index < syntax.Functions.Count; index++)
        {
            var function = syntax.Functions[index];
            if (function.Identifier.HasValue)
                functionNames.Add(function.Identifier.Value);
        }

        var symbols = new List<AwkSymbolReference>();
        var globals = new HashSet<AwkVariableName>();
        for (var index = 0; index < syntax.Functions.Count; index++)
        {
            var function = syntax.Functions[index];
            if (function.Identifier.HasValue)
            {
                symbols.Add(new AwkNamedSymbolReference(
                    function.Identifier.Value,
                    AwkSymbolReferenceKind.FunctionDefinition,
                    function.Span));
            }

            CollectFunctionParameters(function, symbols);
            var firstBodySymbol = symbols.Count;
            CollectStatements(function.BodyStatements, symbols, functionNames);
            AddGlobals(firstBodySymbol, function.ParameterBinding.Parameters);
        }

        for (var index = 0; index < syntax.Rules.Count; index++)
        {
            var rule = syntax.Rules[index];
            var firstRuleSymbol = symbols.Count;
            foreach (var expression in rule.PatternSyntax.EnumerateChildExpressions())
                CollectExpression(expression, symbols, functionNames);

            CollectStatements(rule.ActionStatements, symbols, functionNames);
            AddGlobals(firstRuleSymbol, []);
        }

        globalNames = globals;
        return symbols;

        void AddGlobals(int firstSymbol, IReadOnlyList<AwkVariableName> parameters)
        {
            HashSet<AwkVariableName>? locals = parameters.Count == 0 ? null : new(parameters);
            for (var index = firstSymbol; index < symbols.Count; index++)
            {
                if (symbols[index] is not AwkNamedSymbolReference named ||
                    named.Kind is not (AwkSymbolReferenceKind.Variable or AwkSymbolReferenceKind.Array or
                        AwkSymbolReferenceKind.SpecialVariable or AwkSymbolReferenceKind.SpecialArray))
                    continue;

                var name = new AwkVariableName(named.Identifier);
                if (locals?.Contains(name) != true && !AwkSymbolTableArray.IsName(name.Text))
                    globals.Add(name);
            }
        }

        static void CollectFunctionParameters(
            AwkFunctionSyntax function,
            List<AwkSymbolReference> symbols)
        {
            for (var index = 0; index < function.ParameterTokens.Count; index++)
            {
                var token = function.ParameterTokens[index];
                if (token.Kind != AwkTokenKind.Identifier ||
                    !AwkIdentifier.TryParse(token.Text, out var identifier))
                    continue;

                symbols.Add(new AwkNamedSymbolReference(
                    identifier,
                    AwkSymbolReferenceKind.FunctionParameter,
                    token.Span));
            }
        }
    }

    private static void CollectStatements(
        IReadOnlyList<AwkStatementSyntax> statements,
        List<AwkSymbolReference> symbols,
        ISet<AwkIdentifier> functionNames)
    {
        for (var index = 0; index < statements.Count; index++)
        {
            var statement = statements[index];
            foreach (var expression in statement.EnumerateChildExpressions())
                CollectExpression(expression, symbols, functionNames);

            CollectStatements(statement.BodyStatements, symbols, functionNames);
            CollectStatements(statement.ElseStatements, symbols, functionNames);
            for (var switchCaseIndex = 0; switchCaseIndex < statement.SwitchCases.Count; switchCaseIndex++)
            {
                CollectExpression(statement.SwitchCases[switchCaseIndex].Expression, symbols, functionNames);
                CollectStatements(statement.SwitchCases[switchCaseIndex].BodyStatements, symbols, functionNames);
            }
        }

        static void CollectExpression(
            AwkSyntaxOptional<AwkExpressionSyntax> expression,
            List<AwkSymbolReference> symbols,
            ISet<AwkIdentifier> functionNames)
        {
            if (expression.TryGetValue(out var syntax))
                CollectExpressionCore(syntax, symbols, functionNames);
        }
    }

    private static void CollectExpression(
        AwkExpressionSyntax? expression,
        List<AwkSymbolReference> symbols,
        ISet<AwkIdentifier> functionNames)
    {
        if (expression == null)
            return;

        CollectExpressionCore(expression, symbols, functionNames);
    }

    private static void CollectExpressionCore(
        AwkExpressionSyntax expression,
        List<AwkSymbolReference> symbols,
        ISet<AwkIdentifier> functionNames)
    {
        if (expression.Kind == AwkExpressionKind.Variable)
        {
            if (AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier))
                AddVariableReference(identifier, symbols, AwkSymbolReferenceKind.Variable);

            return;
        }

        if (expression.Kind == AwkExpressionKind.ArrayElement)
        {
            CollectArrayElement(expression, symbols, functionNames);
            return;
        }

        if (expression.Kind == AwkExpressionKind.Field)
        {
            symbols.Add(new AwkFieldSymbolReference(expression.Span));
            CollectChildren(expression, symbols, functionNames, 0);
            return;
        }

        if (expression.Kind == AwkExpressionKind.FunctionCall)
        {
            CollectFunctionCall(expression, symbols, functionNames);
            return;
        }

        CollectChildren(expression, symbols, functionNames, 0);

        static void CollectArrayElement(
            AwkExpressionSyntax expression,
            List<AwkSymbolReference> symbols,
            ISet<AwkIdentifier> functionNames)
        {
            if (AwkExpressionSyntaxFacts.TryGetArrayElementTarget(expression, out var target))
            {
                AddVariableReference(target, symbols, AwkSymbolReferenceKind.Array);
                CollectChildren(expression, symbols, functionNames, 1);
                return;
            }

            CollectChildren(expression, symbols, functionNames, 0);
        }

        static void CollectFunctionCall(
            AwkExpressionSyntax expression,
            List<AwkSymbolReference> symbols,
            ISet<AwkIdentifier> functionNames)
        {
            if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target))
            {
                if (target.IsSpecialSyntax)
                {
                    CollectChildren(expression, symbols, functionNames, 1);
                    return;
                }

                var kind = ClassifyCallableReference(target, functionNames);
                symbols.Add(new AwkNamedSymbolReference(target.Identifier, kind, target.Span));
                CollectChildren(expression, symbols, functionNames, 1);
                return;
            }

            CollectChildren(expression, symbols, functionNames, 0);

            static AwkSymbolReferenceKind ClassifyCallableReference(
                AwkCallableTargetSyntax target,
                ISet<AwkIdentifier> functionNames)
            {
                if (AwkBuiltins.IsKnownFunctionOrSpecialForm(target.Name))
                    return AwkSymbolReferenceKind.BuiltinFunction;

                return functionNames.Contains(target.Identifier)
                    ? AwkSymbolReferenceKind.UserFunction
                    : AwkSymbolReferenceKind.UnknownFunction;
            }
        }
    }

    private static void CollectChildren(
        AwkExpressionSyntax expression,
        List<AwkSymbolReference> symbols,
        ISet<AwkIdentifier> functionNames,
        int startIndex)
    {
        for (var index = startIndex; index < expression.Children.Count; index++)
        {
            CollectExpression(expression.Children[index], symbols, functionNames);
        }
    }

    private static void AddVariableReference(
        AwkIdentifierSyntax identifier,
        List<AwkSymbolReference> symbols,
        AwkSymbolReferenceKind ordinaryKind)
    {
        symbols.Add(new AwkNamedSymbolReference(
            identifier.Identifier,
            ClassifyVariableReference(identifier, ordinaryKind),
            identifier.Span));

        static AwkSymbolReferenceKind ClassifyVariableReference(
            AwkIdentifierSyntax identifier,
            AwkSymbolReferenceKind ordinaryKind)
        {
            if (AwkSpecialVariables.TryGet(identifier.Name, out var definition))
            {
                return definition is AwkSpecialArrayVariableDefinition
                    ? AwkSymbolReferenceKind.SpecialArray
                    : AwkSymbolReferenceKind.SpecialVariable;
            }

            return ordinaryKind;
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
    AwkTextSpan span)
    : AwkSymbolReference(kind, span)
{
    public AwkIdentifier Identifier { get; } = identifier;

    public string Name => Identifier.Text;
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
