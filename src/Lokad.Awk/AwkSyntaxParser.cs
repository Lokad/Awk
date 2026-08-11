using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSyntaxParser
{
    public static AwkSyntaxProgram Parse(string source)
    {
        var parser = new Parser(AwkParsingSource.Create(source), AwkLexer.Tokenize(source));
        return parser.ParseProgram();
    }

    private sealed class Parser
    {
        private readonly AwkParsingSource _source;
        private readonly IReadOnlyList<AwkToken> _tokens;
        private readonly List<AwkRuleSyntax> _rules = [];
        private readonly List<AwkFunctionSyntax> _functions = [];
        private readonly List<AwkDirectiveSyntax> _directives = [];
        private readonly List<AwkDiagnostic> _diagnostics = [];

        public Parser(AwkParsingSource source, IReadOnlyList<AwkToken> tokens)
        {
            _source = source;
            _tokens = tokens;
        }

        public AwkSyntaxProgram ParseProgram()
        {
            AddLexicalDiagnostics();

            if (AwkTopLevelSyntaxParser.TryParse(_source, _tokens, out var parsedProgram))
            {
                AddTopLevelItems(parsedProgram.Items);
                return new AwkSyntaxProgram(_source.Text, _rules, _functions, _directives, _diagnostics);
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
                }
            }

            void AddLexicalDiagnostics() =>
                AwkFrontendDiagnosticPolicy.AddLexicalDiagnostics(_source.Text, _tokens, _diagnostics);
        }
    }
}
