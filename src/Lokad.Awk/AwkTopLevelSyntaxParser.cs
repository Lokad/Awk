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
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
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
        [AwkT(AwkTokenKind.EndOfFile)] AwkTokenKind _)
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
    public TopLevelPart Separator([AwkT(AwkTokenKind.NewLine, AwkTokenKind.Semicolon, AwkTokenKind.Comment)] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return new(null);
    }

    [Rule]
    public TopLevelItem Directive(
        [AwkT(AwkTokenKind.At)] AwkTokenKind _,
        [AwkT(AwkTokenKind.Identifier)] string name,
        [AwkL(Min = 0)] DirectiveArgument[] arguments)
    {
        _checkpoint?.Invoke();
        return TopLevelItem.Directive(
            name,
            DirectiveTokens(arguments),
            SpanFrom(_originalTokens[StartToken], _originalTokens[EndToken]));
    }

    [Rule]
    public TopLevelItem Function(
        [AwkT(AwkTokenKind.Function)] AwkTokenKind _,
        [AwkT(AwkTokenKind.Identifier)] string name,
        [NT] BalancedParenthesized parameterList,
        [AwkL(Min = 0)] FunctionBodyGap[] __,
        [NT] BalancedBlock body)
    {
        _checkpoint?.Invoke();
        return TopLevelItem.Function(
            name,
            InnerTokens(parameterList.OpenIndex, parameterList.CloseIndex),
            InnerTokens(body.OpenIndex, body.CloseIndex),
            SpanFrom(_originalTokens[StartToken], _originalTokens[body.CloseIndex]));
    }

    [Rule]
    public FunctionBodyGap FunctionBodyGapRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return new();
    }

    [Rule]
    public TopLevelItem SpecialRule(
        [AwkT(AwkTokenKind.Begin, AwkTokenKind.End, AwkTokenKind.BeginFile, AwkTokenKind.EndFile)] AwkTokenKind keyword,
        [NT] BalancedBlock action)
    {
        _checkpoint?.Invoke();
        return TopLevelItem.Rule(
            ToRuleKind(keyword),
            AwkPatternSyntax.Empty,
            InnerTokens(action.OpenIndex, action.CloseIndex),
            hasExplicitAction: true,
            SpanFrom(_originalTokens[StartToken], _originalTokens[action.CloseIndex]));

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
            InnerTokens(action.OpenIndex, action.CloseIndex),
            hasExplicitAction: true,
            SpanFrom(_originalTokens[action.OpenIndex], _originalTokens[action.CloseIndex]));
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
            InnerTokens(action.OpenIndex, action.CloseIndex),
            hasExplicitAction: true,
            SpanFrom(pattern.Tokens[0], _originalTokens[action.CloseIndex]));
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
    public DirectiveArgument DirectiveArgumentRule([AwkTopLevelDirectiveArgumentLeaf] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return new(_originalTokens[StartToken]);
    }

    [Rule]
    public BalancedBlock Block(
        [AwkT(AwkTokenKind.LeftBrace)] AwkTokenKind _,
        [AwkL(Min = 0)] BlockPart[] __,
        [AwkT(AwkTokenKind.RightBrace)] AwkTokenKind ___)
    {
        _checkpoint?.Invoke();
        return new(StartToken, EndToken);
    }

    [Rule]
    public BlockPart NestedBlock([NT] BalancedBlock _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public BlockPart BlockLeaf([AwkTopLevelBlockLeaf] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    private IReadOnlyList<AwkToken> InnerTokens(int open, int close) =>
        AwkTokenNavigation.SliceIndexed(_originalTokens, open + 1, close - open - 1);

    [Rule]
    public BalancedParenthesized Parenthesized(
        [AwkT(AwkTokenKind.LeftParen)] AwkTokenKind _,
        [AwkL(Min = 0)] ParenthesizedPart[] __,
        [AwkT(AwkTokenKind.RightParen)] AwkTokenKind ___)
    {
        _checkpoint?.Invoke();
        return new(StartToken, EndToken);
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
    public ParenthesizedPart ParenthesizedLeaf([AwkTopLevelParenthesizedLeaf] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public BalancedBracketed Bracketed(
        [AwkT(AwkTokenKind.LeftBracket)] AwkTokenKind _,
        [AwkL(Min = 0)] BracketedPart[] __,
        [AwkT(AwkTokenKind.RightBracket)] AwkTokenKind ___)
    {
        _checkpoint?.Invoke();
        return new(StartToken, EndToken);
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
    public BracketedPart BracketedLeaf([AwkTopLevelBracketedLeaf] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    [Rule]
    public AwkPatternSyntax RangePattern(
        [AwkL(Min = 1)] PatternPart[] start,
        [AwkT(AwkTokenKind.Comma)] AwkTokenKind _,
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
        return new(parenthesized.OpenIndex, parenthesized.CloseIndex);
    }

    [Rule]
    public PatternPart PatternBracketed([NT] BalancedBracketed bracketed)
    {
        _checkpoint?.Invoke();
        return new(bracketed.OpenIndex, bracketed.CloseIndex);
    }

    [Rule]
    public PatternPart PatternLeaf([AwkTopLevelPatternLeaf] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return new(StartToken, StartToken);
    }

    [Rule]
    public PatternPart PatternContinuation(
        [AwkT(AwkTokenKind.AmpersandAmpersand, AwkTokenKind.PipePipe, AwkTokenKind.Question, AwkTokenKind.Colon)] AwkTokenKind _,
        [AwkL(Min = 1)] HeaderPatternGap[] __,
        [NT] PatternPart next)
    {
        _checkpoint?.Invoke();
        return new(StartToken, next.Last);
    }

    [Rule]
    public HeaderPatternGap HeaderPatternGapRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] AwkTokenKind _)
    {
        _checkpoint?.Invoke();
        return default;
    }

    public readonly struct HeaderPatternGap;

    private IReadOnlyList<AwkToken> PatternTokens(PatternPart[] parts) =>
        PatternTokens(parts[0].First, parts[^1].Last);

    private IReadOnlyList<AwkToken> PatternTokens(int first, int last) =>
        AwkTokenNavigation.SliceIndexed(_originalTokens, first, last - first + 1);

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

    public readonly record struct BalancedBlock(int OpenIndex, int CloseIndex);

    public readonly record struct BalancedParenthesized(int OpenIndex, int CloseIndex);

    public readonly record struct BalancedBracketed(int OpenIndex, int CloseIndex);

    public readonly struct BlockPart;
    public readonly struct ParenthesizedPart;
    public readonly struct BracketedPart;

    public readonly record struct PatternPart(int First, int Last);
}
