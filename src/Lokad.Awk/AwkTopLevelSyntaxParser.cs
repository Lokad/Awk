using System.Diagnostics.CodeAnalysis;
using Lokad.Parsing.Error;
using Lokad.Parsing.Parser;

namespace Lokad.Awk;

internal sealed class AwkTopLevelSyntaxParser
    : GrammarParser<AwkTopLevelSyntaxParser, AwkTokenKind, AwkTopLevelSyntaxParser.ParseResult>
{
    private readonly IReadOnlyList<AwkToken> _originalTokens;

    private readonly string _source;
    private readonly Action? _checkpoint;

    private AwkTopLevelSyntaxParser(string source, IReadOnlyList<AwkToken> originalTokens, Action? checkpoint)
        : base(AwkTokenNamer.Instance)
    {
        _source = source;
        _originalTokens = originalTokens;
        _checkpoint = checkpoint;
    }

    public static bool TryParse(
        string source,
        IReadOnlyList<AwkToken> tokens,
        [NotNullWhen(true)] out ParseResult? result,
        out AwkTextSpan failureSpan,
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
            failureSpan = default;
            return true;
        }
        catch (ParseException error)
        {
            result = null;
            failureSpan = AwkParsingSupport.FailureSpan(error, source);
            return false;
        }
    }

    [Rule]
    public ParseResult Program(
        [AwkL(Min = 0)] TopLevelPart[] parts,
        [AwkT(AwkTokenKind.EndOfFile)] Pos<string> _)
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
    public TopLevelPart Separator([AwkT(AwkTokenKind.NewLine, AwkTokenKind.Semicolon, AwkTokenKind.Comment)] Pos<string> _)
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
        [AwkL(Min = 0)] FunctionBodyGap[] _,
        [NT] BalancedBlock body)
    {
        _checkpoint?.Invoke();
        var start = ToToken(AwkTokenKind.Function, functionKeyword);
        return TopLevelItem.Function(
            name.Value,
            GroupTokens(parameterList.OpenToken, parameterList.CloseToken).InnerTokens,
            GroupTokens(body.OpenToken, body.CloseToken).InnerTokens,
            SpanFrom(start, body.CloseToken));
    }

    [Rule]
    public FunctionBodyGap FunctionBodyGapRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] Pos<string> _)
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
            GroupTokens(action.OpenToken, action.CloseToken).InnerTokens,
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
            GroupTokens(action.OpenToken, action.CloseToken).InnerTokens,
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
            GroupTokens(action.OpenToken, action.CloseToken).InnerTokens,
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
        [AwkL(Min = 0)] BlockPart[] _,
        [AwkT(AwkTokenKind.RightBrace)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        return new(ToToken(AwkTokenKind.LeftBrace, open), ToToken(AwkTokenKind.RightBrace, close));
    }

    [Rule]
    public BlockPart NestedBlock([NT] BalancedBlock _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public BlockPart BlockLeaf([AwkTopLevelBlockLeaf] Pos<string> _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    private AwkParsingSupport.TokenGroup GroupTokens(AwkToken open, AwkToken close) =>
        AwkParsingSupport.ResolveTokenGroup(_originalTokens, open.Span, close.Span, _checkpoint);

    [Rule]
    public BalancedParenthesized Parenthesized(
        [AwkT(AwkTokenKind.LeftParen)] Pos<string> open,
        [AwkL(Min = 0)] ParenthesizedPart[] _,
        [AwkT(AwkTokenKind.RightParen)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        return new(ToToken(AwkTokenKind.LeftParen, open), ToToken(AwkTokenKind.RightParen, close));
    }

    [Rule]
    public ParenthesizedPart NestedParenthesized([NT] BalancedParenthesized _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public ParenthesizedPart NestedBracketedInParenthesized([NT] BalancedBracketed _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public ParenthesizedPart ParenthesizedLeaf([AwkTopLevelParenthesizedLeaf] Pos<string> _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public BalancedBracketed Bracketed(
        [AwkT(AwkTokenKind.LeftBracket)] Pos<string> open,
        [AwkL(Min = 0)] BracketedPart[] _,
        [AwkT(AwkTokenKind.RightBracket)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        return new(ToToken(AwkTokenKind.LeftBracket, open), ToToken(AwkTokenKind.RightBracket, close));
    }

    [Rule]
    public BracketedPart NestedBracketed([NT] BalancedBracketed _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public BracketedPart NestedParenthesizedInBracketed([NT] BalancedParenthesized _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public BracketedPart BracketedLeaf([AwkTopLevelBracketedLeaf] Pos<string> _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public AwkPatternSyntax RangePattern(
        [AwkL(Min = 1)] PatternPart[] start,
        [AwkT(AwkTokenKind.Comma)] Pos<string> _,
        [AwkL(Min = 0)] HeaderPatternGap[] __,
        [AwkL(Min = 1)] PatternPart[] end)
    {
        _checkpoint?.Invoke();
        var startSource = PatternTokens(start);
        var endSource = PatternTokens(end);
        var startTokens = AwkTokenNavigation.WithoutKind(startSource, AwkTokenKind.Comment, _checkpoint);
        var endTokens = AwkTokenNavigation.WithoutKind(endSource, AwkTokenKind.Comment, _checkpoint);
        var tokens = PatternTokens(start[0].First, end[^1].Last);
        if (!ReferenceEquals(startSource, startTokens) || !ReferenceEquals(endSource, endTokens))
        {
            // The comma and the continuation tokens after it belong to the range,
            // even when either endpoint drops its own comments.
            var separator = AwkTokenNavigation.SliceIndexed(tokens, startSource.Count,
                tokens.Count - startSource.Count - endSource.Count);
            tokens = AwkTokenNavigation.Flatten([startTokens, separator, endTokens], _checkpoint);
        }
        var startExpression = AwkExpressionSyntaxParser.Parse(_source, startTokens, out var startFailure, _checkpoint);
        var endExpression = AwkExpressionSyntaxParser.Parse(_source, endTokens, out var endFailure, _checkpoint);
        return new AwkRangePatternSyntax(tokens, startExpression, endExpression, startFailure ?? endFailure);
    }

    [Rule]
    public AwkPatternSyntax SinglePattern([AwkL(Min = 1)] PatternPart[] parts)
    {
        _checkpoint?.Invoke();
        var tokens = AwkTokenNavigation.WithoutKind(PatternTokens(parts), AwkTokenKind.Comment, _checkpoint);
        return new AwkSinglePatternSyntax(tokens, AwkExpressionSyntaxParser.ParseList(_source, tokens, _checkpoint));
    }

    [Rule]
    public PatternPart PatternParenthesized([NT] BalancedParenthesized parenthesized)
    {
        _checkpoint?.Invoke();
        return new(parenthesized.OpenToken.Span, parenthesized.CloseToken.Span);
    }

    [Rule]
    public PatternPart PatternBracketed([NT] BalancedBracketed bracketed)
    {
        _checkpoint?.Invoke();
        return new(bracketed.OpenToken.Span, bracketed.CloseToken.Span);
    }

    [Rule]
    public PatternPart PatternLeaf([AwkTopLevelPatternLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        var span = AwkParsingSupport.SourceSpan(token);
        return new(span, span);
    }

    [Rule]
    public PatternPart PatternContinuation(
        [AwkT(AwkTokenKind.AmpersandAmpersand, AwkTokenKind.PipePipe, AwkTokenKind.Question, AwkTokenKind.Colon)] Pos<string> op,
        [AwkL(Min = 1)] HeaderPatternGap[] _,
        [NT] PatternPart next)
    {
        _checkpoint?.Invoke();
        return new(AwkParsingSupport.SourceSpan(op), next.Last);
    }

    [Rule]
    public HeaderPatternGap HeaderPatternGapRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] Pos<string> _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    public readonly struct HeaderPatternGap;

    private IReadOnlyList<AwkToken> PatternTokens(PatternPart[] parts) =>
        PatternTokens(parts[0].First, parts[^1].Last);

    private IReadOnlyList<AwkToken> PatternTokens(AwkTextSpan first, AwkTextSpan last) =>
        AwkParsingSupport.ResolveTokenGroup(_originalTokens, first, last, _checkpoint).FullTokens;

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

    public readonly record struct BalancedBlock(AwkToken OpenToken, AwkToken CloseToken);

    public readonly record struct BalancedParenthesized(AwkToken OpenToken, AwkToken CloseToken);

    public readonly record struct BalancedBracketed(AwkToken OpenToken, AwkToken CloseToken);

    public readonly struct BlockPart;
    public readonly struct ParenthesizedPart;
    public readonly struct BracketedPart;

    public readonly record struct PatternPart(AwkTextSpan First, AwkTextSpan Last);
}
