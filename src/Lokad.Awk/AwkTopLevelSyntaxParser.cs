using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lokad.Parsing.Error;
using Lokad.Parsing.Parser;

namespace Lokad.Awk;

internal sealed class AwkTopLevelSyntaxParser
    : GrammarParser<AwkTopLevelSyntaxParser, AwkTokenKind, AwkTopLevelSyntaxParser.ParseResult>
{
    private readonly IReadOnlyList<AwkToken> _originalTokens;

    private readonly AwkParsingSource _source;
    private readonly Action? _checkpoint;

    private AwkTopLevelSyntaxParser(AwkParsingSource source, IReadOnlyList<AwkToken> originalTokens, Action? checkpoint)
        : base(AwkTokenNamer.Instance)
    {
        _source = source;
        _originalTokens = originalTokens;
        _checkpoint = checkpoint;
    }

    public static bool TryParse(
        AwkParsingSource source,
        IReadOnlyList<AwkToken> tokens,
        [NotNullWhen(true)] out ParseResult? result,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var parser = new AwkTopLevelSyntaxParser(source, tokens, checkpoint);
        parser.Tokens = AwkParsingSupport.ToLexerResult(source, tokens, checkpoint);
        try
        {
            checkpoint?.Invoke();
            result = StreamParser(parser, parser.Tokens);
            checkpoint?.Invoke();
            return true;
        }
        catch (ParseException)
        {
            result = null;
            return false;
        }
    }

    [Rule]
    public ParseResult Program(
        [AwkL(Min = 0)] TopLevelPart[] parts,
        [AwkT(AwkTokenKind.EndOfFile)] Pos<string> endOfFile)
    {
        _checkpoint?.Invoke();
        var items = new List<TopLevelItem>(parts.Length);
        for (var index = 0; index < parts.Length; index++)
        {
            _checkpoint?.Invoke();
            if (parts[index].Item is { } item)
                items.Add(item);
        }

        return new ParseResult(items);
    }

    [Rule]
    public TopLevelPart Item([NT] TopLevelItem item)
    {
        _checkpoint?.Invoke();
        return new(item);
    }

    [Rule]
    public TopLevelPart Separator([AwkT(AwkTokenKind.NewLine, AwkTokenKind.Semicolon, AwkTokenKind.Comment)] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new(null);
    }

    [Rule]
    public TopLevelItem Directive(
        [AwkT(AwkTokenKind.At)] Pos<string> at,
        [AwkT(AwkTokenKind.Identifier)] Pos<string> name,
        [AwkL(Min = 0)] DirectiveArgument[] arguments)
    {
        _checkpoint?.Invoke();
        var atToken = ToToken(AwkTokenKind.At, at);
        var nameToken = ToToken(AwkTokenKind.Identifier, name);
        return TopLevelItem.Directive(
            name.Value,
            DirectiveTokens(arguments),
            SpanFrom(atToken, arguments.Length == 0 ? nameToken : arguments[^1].Token));
    }

    [Rule]
    public TopLevelItem Function(
        [AwkT(AwkTokenKind.Function)] Pos<string> functionKeyword,
        [AwkT(AwkTokenKind.Identifier)] Pos<string> name,
        [NT] BalancedParenthesized parameterList,
        [AwkL(Min = 0)] FunctionBodyGap[] beforeBody,
        [NT] BalancedBlock body)
    {
        _checkpoint?.Invoke();
        var start = ToToken(AwkTokenKind.Function, functionKeyword);
        return TopLevelItem.Function(
            name.Value,
            parameterList.InnerTokens,
            body.InnerTokens,
            SpanFrom(start, body.CloseToken));
    }

    [Rule]
    public FunctionBodyGap FunctionBodyGapRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new();
    }

    [Rule]
    public TopLevelItem SpecialRule(
        [AwkT(AwkTokenKind.Begin, AwkTokenKind.End, AwkTokenKind.BeginFile, AwkTokenKind.EndFile)] Pos<string> keyword,
        [NT] BalancedBlock action)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkParsingSupport.ResolveTextKind(keyword.Value), keyword);
        return TopLevelItem.Rule(
            ToRuleKind(keywordToken.Kind),
            AwkPatternSyntax.Empty,
            action.InnerTokens,
            hasExplicitAction: true,
            SpanFrom(keywordToken, action.CloseToken));

        static AwkRuleKind ToRuleKind(AwkTokenKind kind) =>
            kind switch
            {
                AwkTokenKind.Begin => AwkRuleKind.Begin,
                AwkTokenKind.End => AwkRuleKind.End,
                AwkTokenKind.BeginFile => AwkRuleKind.BeginFile,
                AwkTokenKind.EndFile => AwkRuleKind.EndFile,
                _ => AwkRuleKind.Main
            };
    }

    [Rule]
    public TopLevelItem ActionOnlyRule([NT] BalancedBlock action)
    {
        _checkpoint?.Invoke();
        return TopLevelItem.Rule(
            AwkRuleKind.Main,
            AwkPatternSyntax.Empty,
            action.InnerTokens,
            hasExplicitAction: true,
            SpanFrom(action.OpenToken, action.CloseToken));
    }

    [Rule]
    public TopLevelItem PatternActionRule(
        [NT] AwkPatternSyntax pattern,
        [NT] BalancedBlock action)
    {
        _checkpoint?.Invoke();
        return TopLevelItem.Rule(
            AwkRuleKind.Main,
            pattern,
            action.InnerTokens,
            hasExplicitAction: true,
            SpanFrom(pattern.Tokens[0], action.CloseToken));
    }

    [Rule]
    public TopLevelItem PatternOnlyRule([NT] AwkPatternSyntax pattern)
    {
        _checkpoint?.Invoke();
        return TopLevelItem.Rule(
            AwkRuleKind.Main,
            pattern,
            [],
            hasExplicitAction: false,
            SpanFrom(pattern.Tokens[0], pattern.Tokens[^1]));
    }

    [Rule]
    public DirectiveArgument DirectiveArgumentRule([AwkTopLevelDirectiveArgumentLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new(AwkParsingSupport.RestoreToken(_originalTokens, token, _checkpoint));
    }

    [Rule]
    public BalancedBlock Block(
        [AwkT(AwkTokenKind.LeftBrace)] Pos<string> open,
        [AwkL(Min = 0)] BlockPart[] parts,
        [AwkT(AwkTokenKind.RightBrace)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var openToken = ToToken(AwkTokenKind.LeftBrace, open);
        var closeToken = ToToken(AwkTokenKind.RightBrace, close);
        var inner = Flatten(parts.Select(part => part.Tokens));
        return new BalancedBlock(openToken, closeToken, inner);
    }

    [Rule]
    public BlockPart NestedBlock([NT] BalancedBlock block)
    {
        _checkpoint?.Invoke();
        return new(block.FullTokens);
    }

    [Rule]
    public BlockPart BlockLeaf([AwkTopLevelBlockLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, _checkpoint)]);
    }

    [Rule]
    public BalancedParenthesized Parenthesized(
        [AwkT(AwkTokenKind.LeftParen)] Pos<string> open,
        [AwkL(Min = 0)] ParenthesizedPart[] parts,
        [AwkT(AwkTokenKind.RightParen)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var openToken = ToToken(AwkTokenKind.LeftParen, open);
        var closeToken = ToToken(AwkTokenKind.RightParen, close);
        var inner = Flatten(parts.Select(part => part.Tokens));
        return new BalancedParenthesized(openToken, closeToken, inner);
    }

    [Rule]
    public ParenthesizedPart NestedParenthesized([NT] BalancedParenthesized parenthesized)
    {
        _checkpoint?.Invoke();
        return new(parenthesized.FullTokens);
    }

    [Rule]
    public ParenthesizedPart NestedBracketedInParenthesized([NT] BalancedBracketed bracketed)
    {
        _checkpoint?.Invoke();
        return new(bracketed.FullTokens);
    }

    [Rule]
    public ParenthesizedPart ParenthesizedLeaf([AwkTopLevelParenthesizedLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, _checkpoint)]);
    }

    [Rule]
    public BalancedBracketed Bracketed(
        [AwkT(AwkTokenKind.LeftBracket)] Pos<string> open,
        [AwkL(Min = 0)] BracketedPart[] parts,
        [AwkT(AwkTokenKind.RightBracket)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var openToken = ToToken(AwkTokenKind.LeftBracket, open);
        var closeToken = ToToken(AwkTokenKind.RightBracket, close);
        var inner = Flatten(parts.Select(part => part.Tokens));
        return new BalancedBracketed(openToken, closeToken, inner);
    }

    [Rule]
    public BracketedPart NestedBracketed([NT] BalancedBracketed bracketed)
    {
        _checkpoint?.Invoke();
        return new(bracketed.FullTokens);
    }

    [Rule]
    public BracketedPart NestedParenthesizedInBracketed([NT] BalancedParenthesized parenthesized)
    {
        _checkpoint?.Invoke();
        return new(parenthesized.FullTokens);
    }

    [Rule]
    public BracketedPart BracketedLeaf([AwkTopLevelBracketedLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, _checkpoint)]);
    }

    [Rule]
    public AwkPatternSyntax RangePattern(
        [AwkL(Min = 1)] PatternPart[] start,
        [AwkT(AwkTokenKind.Comma)] Pos<string> comma,
        [AwkL(Min = 0)] HeaderPatternGap[] afterComma,
        [AwkL(Min = 1)] PatternPart[] end)
    {
        _checkpoint?.Invoke();
        var startTokens = PatternTokens(start);
        var endTokens = PatternTokens(end);
        var commaToken = ToToken(AwkTokenKind.Comma, comma);
        var tokens = Flatten([startTokens, [commaToken], Flatten(afterComma.Select(gap => gap.Tokens)), endTokens]);
        return new AwkRangePatternSyntax(
            tokens,
            new AwkPatternPartSyntax(startTokens, ParseExpression(startTokens)),
            new AwkPatternPartSyntax(endTokens, ParseExpression(endTokens)));
    }

    [Rule]
    public AwkPatternSyntax SinglePattern([AwkL(Min = 1)] PatternPart[] parts)
    {
        _checkpoint?.Invoke();
        var tokens = PatternTokens(parts);
        return new AwkSinglePatternSyntax(
            tokens,
            ParseExpression(tokens) ?? ParseExpressionList(tokens));

        AwkExpressionSyntax? ParseExpressionList(IReadOnlyList<AwkToken> tokens) =>
            AwkExpressionSyntaxParser.ParseList(_source, tokens, _checkpoint);
    }

    [Rule]
    public PatternPart PatternParenthesized([NT] BalancedParenthesized parenthesized)
    {
        _checkpoint?.Invoke();
        return new(parenthesized.FullTokens);
    }

    [Rule]
    public PatternPart PatternBracketed([NT] BalancedBracketed bracketed)
    {
        _checkpoint?.Invoke();
        return new(bracketed.FullTokens);
    }

    [Rule]
    public PatternPart PatternLeaf([AwkTopLevelPatternLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, _checkpoint)]);
    }

    [Rule]
    public PatternPart PatternContinuation(
        [AwkT(AwkTokenKind.AmpersandAmpersand, AwkTokenKind.PipePipe, AwkTokenKind.Question, AwkTokenKind.Colon)] Pos<string> op,
        [AwkL(Min = 1)] HeaderPatternGap[] gaps,
        [NT] PatternPart next)
    {
        _checkpoint?.Invoke();
        return new(Flatten([[AwkParsingSupport.RestoreToken(_originalTokens, op, _checkpoint)], Flatten(gaps.Select(gap => gap.Tokens)), next.Tokens]));
    }

    [Rule]
    public HeaderPatternGap HeaderPatternGapRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, _checkpoint)]);
    }

    public readonly struct HeaderPatternGap(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    private AwkToken[] PatternTokens(IReadOnlyList<PatternPart> pattern)
    {
        var tokens = new List<AwkToken>();
        foreach (var token in AwkTokenNavigation.Enumerate(Flatten(pattern.Select(part => part.Tokens)), _checkpoint))
        {
            if (token.Kind != AwkTokenKind.Comment)
                tokens.Add(token);
        }
        _checkpoint?.Invoke();
        var copied = tokens.ToArray();
        _checkpoint?.Invoke();
        return copied;
    }

    private AwkToken[] DirectiveTokens(IReadOnlyList<DirectiveArgument> arguments)
    {
        var tokens = new AwkToken[arguments.Count];
        for (var index = 0; index < arguments.Count; index++)
        {
            _checkpoint?.Invoke();
            tokens[index] = arguments[index].Token;
        }
        return tokens;
    }

    private IReadOnlyList<AwkToken> Flatten(IEnumerable<IReadOnlyList<AwkToken>> groups) =>
        AwkTokenNavigation.Flatten(groups, _checkpoint);

    private AwkExpressionSyntax? ParseExpression(IReadOnlyList<AwkToken> tokens) =>
        AwkExpressionSyntaxParser.Parse(_source, tokens, _checkpoint);

    private AwkToken ToToken(AwkTokenKind kind, Pos<string> value) =>
        AwkParsingSupport.ToToken(kind, value, _checkpoint);

    private static AwkTextSpan SpanFrom(AwkToken start, AwkToken end) =>
        new(start.Span.Start, end.Span.End - start.Span.Start);

    internal sealed class ParseResult(IReadOnlyList<TopLevelItem> items)
    {
        public IReadOnlyList<TopLevelItem> Items { get; } = items;
    }

    internal readonly struct TopLevelPart(TopLevelItem? item)
    {
        public TopLevelItem? Item { get; } = item;
    }

    internal sealed class TopLevelItem
    {
        private TopLevelItem(
            TopLevelItemKind kind,
            string name,
            AwkRuleKind ruleKind,
            AwkPatternSyntax? pattern,
            IReadOnlyList<AwkToken> firstTokens,
            IReadOnlyList<AwkToken> secondTokens,
            bool hasExplicitAction,
            AwkTextSpan span)
        {
            Kind = kind;
            Name = name;
            RuleKind = ruleKind;
            Pattern = pattern;
            FirstTokens = firstTokens;
            SecondTokens = secondTokens;
            HasExplicitAction = hasExplicitAction;
            Span = span;
        }

        public TopLevelItemKind Kind { get; }

        public string Name { get; }

        public AwkRuleKind RuleKind { get; }

        public AwkPatternSyntax? Pattern { get; }

        public IReadOnlyList<AwkToken> FirstTokens { get; }

        public IReadOnlyList<AwkToken> SecondTokens { get; }

        public bool HasExplicitAction { get; }

        public AwkTextSpan Span { get; }

        public static TopLevelItem Directive(
            string name,
            IReadOnlyList<AwkToken> arguments,
            AwkTextSpan span) =>
            new(TopLevelItemKind.Directive, name, AwkRuleKind.Main, null, arguments, [], false, span);

        public static TopLevelItem Function(
            string name,
            IReadOnlyList<AwkToken> parameters,
            IReadOnlyList<AwkToken> body,
            AwkTextSpan span) =>
            new(TopLevelItemKind.Function, name, AwkRuleKind.Main, null, parameters, body, false, span);

        public static TopLevelItem Rule(
            AwkRuleKind ruleKind,
            AwkPatternSyntax pattern,
            IReadOnlyList<AwkToken> actionTokens,
            bool hasExplicitAction,
            AwkTextSpan span) =>
            new(
                TopLevelItemKind.Rule,
                string.Empty,
                ruleKind,
                pattern,
                pattern.Tokens,
                actionTokens,
                hasExplicitAction,
                span);
    }

    internal enum TopLevelItemKind
    {
        Directive,
        Function,
        Rule
    }

    public readonly struct FunctionBodyGap;

    public readonly struct DirectiveArgument(AwkToken token)
    {
        public AwkToken Token { get; } = token;
    }

    public sealed class BalancedBlock(
        AwkToken openToken,
        AwkToken closeToken,
        IReadOnlyList<AwkToken> innerTokens) : AwkBalancedTokenGroup(openToken, closeToken, innerTokens);

    public sealed class BalancedParenthesized(
        AwkToken openToken,
        AwkToken closeToken,
        IReadOnlyList<AwkToken> innerTokens) : AwkBalancedTokenGroup(openToken, closeToken, innerTokens);

    public sealed class BalancedBracketed(
        AwkToken openToken,
        AwkToken closeToken,
        IReadOnlyList<AwkToken> innerTokens) : AwkBalancedTokenGroup(openToken, closeToken, innerTokens);

    public readonly struct BlockPart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct ParenthesizedPart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct BracketedPart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct PatternPart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }
}
