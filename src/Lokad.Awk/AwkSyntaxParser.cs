using System.Collections.Generic;
using System.Threading;

namespace Lokad.Awk;

internal static class AwkSyntaxParser
{
    public static AwkSyntaxProgram Parse(string source)
        => Parse(source, int.MaxValue);

    public static AwkSyntaxProgram Parse(string source, int maxSyntaxNestingDepth)
        => Parse(source, AwkFrontendBudget.Unlimited(maxSyntaxNestingDepth), CancellationToken.None);

    public static AwkSyntaxProgram Parse(
        string source,
        AwkFrontendBudget budget,
        CancellationToken cancellationToken,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsingSource = AwkParsingSource.Create(source);
        var tokens = AwkLexer.Tokenize(source, budget.MaxTokens, cancellationToken, compatibilityMode);
        cancellationToken.ThrowIfCancellationRequested();
        if (AwkSyntaxNesting.TryFindExcessiveDelimiterDepth(
                tokens,
                budget.MaxSyntaxNestingDepth,
                out var excessiveDelimiter))
        {
            return new AwkSyntaxProgram(
                source,
                [],
                [],
                [],
                [CreateNestingDiagnostic(excessiveDelimiter)]);
        }

        var parser = new Parser(
            parsingSource,
            tokens,
            budget,
            cancellationToken,
            compatibilityMode);
        return parser.ParseProgram();
    }

    private sealed class Parser(
        AwkParsingSource source,
        IReadOnlyList<AwkToken> tokens,
        AwkFrontendBudget budget,
        CancellationToken cancellationToken,
        AwkCompatibilityMode compatibilityMode)
    {
        private readonly AwkParsingSource _source = source;
        private readonly IReadOnlyList<AwkToken> _tokens = tokens;
        private readonly List<AwkRuleSyntax> _rules = [];
        private readonly List<AwkFunctionSyntax> _functions = [];
        private readonly List<AwkDirectiveSyntax> _directives = [];
        private readonly List<AwkDiagnostic> _diagnostics = [];

        public AwkSyntaxProgram ParseProgram()
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddLexicalDiagnostics();

            if (AwkTopLevelSyntaxParser.TryParse(_source, _tokens, out var parsedProgram))
            {
                AddTopLevelItems(parsedProgram.Items);
                cancellationToken.ThrowIfCancellationRequested();
                var program = new AwkSyntaxProgram(
                    _source.Text,
                    _rules,
                    _functions,
                    _directives,
                    _diagnostics);
                if (AwkSyntaxNesting.TryFindExcessiveTreeDepth(
                        program,
                        budget.MaxSyntaxNestingDepth,
                        out var excessiveNode))
                {
                    _diagnostics.Add(CreateNestingDiagnostic(excessiveNode));
                }

                return program;
            }

            AddTopLevelParseFailureDiagnostic();
            return new AwkSyntaxProgram(_source.Text, _rules, _functions, _directives, _diagnostics);

            void AddTopLevelParseFailureDiagnostic()
            {
                for (var index = 0; index < _tokens.Count; index++)
                {
                    if (_tokens[index].Kind != AwkTokenKind.Function)
                        continue;

                    var nameIndex = FindNextSignificantIndex(index + 1);
                    if (nameIndex < 0 || _tokens[nameIndex].Kind != AwkTokenKind.Identifier)
                    {
                        AddDiagnostic(nameIndex >= 0 ? _tokens[nameIndex] : _tokens[index], "Expected function name.");
                        return;
                    }
                }

                for (var index = 0; index < _tokens.Count; index++)
                {
                    if (!IsSpecialRuleKeyword(_tokens[index].Kind))
                        continue;

                    var actionIndex = FindNextSignificantIndex(index + 1);
                    if (actionIndex < 0 || _tokens[actionIndex].Kind != AwkTokenKind.LeftBrace)
                    {
                        AddDiagnostic(actionIndex >= 0 ? _tokens[actionIndex] : _tokens[index], "Expected awk action block.");
                        return;
                    }
                }

                var unclosedBlock = FindLastUnclosedBlock();
                if (unclosedBlock != null)
                {
                    _diagnostics.Add(new AwkDiagnostic(unclosedBlock.Value.Span, "Unterminated action block."));
                    return;
                }

                var first = AwkTokenNavigation.FindFirstSignificant(_tokens);
                if (first != null)
                    _diagnostics.Add(new AwkDiagnostic(first.Value.Span, "Invalid pattern."));

                int FindNextSignificantIndex(int startIndex)
                {
                    for (var index = startIndex; index < _tokens.Count; index++)
                    {
                        if (AwkTokenNavigation.IsSignificant(_tokens[index].Kind) &&
                            _tokens[index].Kind != AwkTokenKind.EndOfFile)
                        {
                            return index;
                        }
                    }

                    return -1;
                }

                AwkToken? FindLastUnclosedBlock()
                {
                    var stack = new List<AwkToken>();
                    for (var index = 0; index < _tokens.Count; index++)
                    {
                        if (_tokens[index].Kind == AwkTokenKind.LeftBrace)
                        {
                            stack.Add(_tokens[index]);
                            continue;
                        }

                        if (_tokens[index].Kind == AwkTokenKind.RightBrace && stack.Count > 0)
                            stack.RemoveAt(stack.Count - 1);
                    }

                    return stack.Count == 0 ? null : stack[^1];
                }

                static bool IsSpecialRuleKeyword(AwkTokenKind kind) =>
                    kind is AwkTokenKind.Begin or
                        AwkTokenKind.End or
                        AwkTokenKind.BeginFile or
                        AwkTokenKind.EndFile;

                void AddDiagnostic(AwkToken token, string message)
                {
                    _diagnostics.Add(new AwkDiagnostic(token.Span, message));
                }
            }

            void AddTopLevelItems(IReadOnlyList<AwkTopLevelSyntaxParser.TopLevelItem> items)
            {
                for (var index = 0; index < items.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = items[index];
                    switch (item.Kind)
                    {
                        case AwkTopLevelSyntaxParser.TopLevelItemKind.Directive:
                            _directives.Add(new AwkDirectiveSyntax(
                                AwkDirectiveParser.ResolveKind(item.Name),
                                item.Name,
                                item.FirstTokens,
                                item.Span));
                            break;
                        case AwkTopLevelSyntaxParser.TopLevelItemKind.Function:
                            if (AwkExpressionSyntaxParser.TryGetSeparatorDiagnostic(item.FirstTokens, out var parameterDiagnostic))
                                _diagnostics.Add(parameterDiagnostic);
                            _functions.Add(new AwkFunctionSyntax(
                                item.Name,
                                item.FirstTokens,
                                item.SecondTokens,
                                AwkStatementSyntaxParser.ParseStatements(
                                    _source,
                                    item.SecondTokens,
                                    _diagnostics,
                                    "Invalid function declaration."),
                                item.Span));
                            break;
                        case AwkTopLevelSyntaxParser.TopLevelItemKind.Rule:
                            if (item.Pattern is { } pattern)
                            {
                                foreach (var expression in pattern.EnumerateChildExpressions())
                                {
                                    if (expression.Kind == AwkExpressionKind.Unknown)
                                        _diagnostics.Add(AwkExpressionSyntaxParser.CreateSyntaxDiagnostic(expression));
                                }
                            }
                            _rules.Add(new AwkRuleSyntax(
                                item.RuleKind,
                                item.Pattern ?? AwkPatternSyntax.Empty,
                                item.SecondTokens,
                                AwkStatementSyntaxParser.ParseStatements(
                                    _source,
                                    item.SecondTokens,
                                    _diagnostics,
                                    "Invalid action block."),
                                item.HasExplicitAction,
                                item.Span));
                            break;
                    }

                    if (_diagnostics.Count > budget.MaxDiagnostics)
                    {
                        throw new AwkRuntimeException(
                            AwkMessages.ErrorFrontendDiagnosticLimit,
                            AwkDiagnosticId.RuntimeResourceLimit);
                    }
                }
            }

            void AddLexicalDiagnostics() =>
                AwkFrontendDiagnosticPolicy.AddLexicalDiagnostics(
                    _source.Text,
                    _tokens,
                    _diagnostics,
                    budget,
                    compatibilityMode);
        }
    }

    private static AwkDiagnostic CreateNestingDiagnostic(AwkTextSpan span) =>
        new(
            span,
            "Program nesting limit exceeded.",
            AwkDiagnosticId.RuntimeResourceLimit);
}
