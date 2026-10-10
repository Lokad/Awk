using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lokad.Parsing.Error;
using Lokad.Parsing.Lexer;
using Lokad.Parsing.Parser;

namespace Lokad.Awk;

internal sealed class AwkExpressionSyntaxParser
    : GrammarParser<AwkExpressionSyntaxParser, AwkTokenKind, AwkExpressionSyntaxParser.RootParseResult>
{
    private AwkExpressionSyntaxParser()
        : base(AwkTokenNamer.Instance)
    {
    }

    public static AwkExpressionSyntax? Parse(AwkParsingSource source, IReadOnlyList<AwkToken> tokens) =>
        SingleExpressionOrNull(ParseCore(source, tokens));

    public static AwkExpressionSyntax? Parse(IReadOnlyList<AwkToken> tokens) =>
        SingleExpressionOrNull(ParseCore(null, tokens));

    public static AwkExpressionSyntax? ParseList(AwkParsingSource source, IReadOnlyList<AwkToken> tokens) =>
        ParsedExpressionOrNull(ParseCore(source, tokens));

    public static AwkExpressionSyntax? ParseList(IReadOnlyList<AwkToken> tokens) =>
        ParsedExpressionOrNull(ParseCore(null, tokens));

    private static AwkExpressionParseResult ParseCore(AwkParsingSource? source, IReadOnlyList<AwkToken> tokens)
    {
        static bool TryCreateLexerResult(
            AwkParsingSource source,
            IReadOnlyList<AwkToken> tokens,
            [NotNullWhen(true)] out LexerResult<AwkTokenKind>? result)
        {
            static bool IsExpressionToken(AwkTokenKind kind) =>
                kind is not AwkTokenKind.Comment and
                    not AwkTokenKind.NewLine and
                    not AwkTokenKind.Semicolon;

            var expressionTokenCount = 0;
            var hasEndOfFile = false;
            for (var index = 0; index < tokens.Count; index++)
            {
                if (!IsExpressionToken(tokens[index].Kind))
                    continue;

                expressionTokenCount++;
                hasEndOfFile |= tokens[index].Kind == AwkTokenKind.EndOfFile;
            }

            if (expressionTokenCount == 0)
            {
                result = null;
                return false;
            }

            var lexerTokens = new LexerToken<AwkTokenKind>[
                expressionTokenCount + (hasEndOfFile ? 0 : 1)];
            var next = 0;
            var end = 0;
            var hasInvalidTokens = false;
            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                if (!IsExpressionToken(token.Kind))
                    continue;

                lexerTokens[next++] = new LexerToken<AwkTokenKind>(
                    ResolveExpressionTokenKind(tokens, index),
                    token.Span.Start,
                    token.Span.Length);
                end = Math.Max(end, token.Span.End);
                hasInvalidTokens |= token.Kind == AwkTokenKind.BadToken;
            }

            if (!hasEndOfFile)
            {
                lexerTokens[^1] = new LexerToken<AwkTokenKind>(
                    AwkTokenKind.EndOfFile,
                    end,
                    0);
            }

            result = new LexerResult<AwkTokenKind>(
                source.Text,
                lexerTokens,
                source.NewLines,
                hasInvalidTokens);
            return true;
        }

        var parser = new AwkExpressionSyntaxParser();
        if (source != null)
        {
            if (!TryCreateLexerResult(source, tokens, out var lexerResult))
                return AwkExpressionParseResult.EmptyResult;

            parser.Tokens = lexerResult;
        }
        else
        {
            var significantTokens = tokens
                .Where(token => token.Kind is not AwkTokenKind.Comment and not AwkTokenKind.NewLine and not AwkTokenKind.Semicolon)
                .ToArray();
            if (significantTokens.Length == 0)
                return AwkExpressionParseResult.EmptyResult;

            for (var index = 0; index < significantTokens.Length; index++)
            {
                var token = significantTokens[index];
                var kind = ResolveExpressionTokenKind(significantTokens, index);
                if (kind != token.Kind)
                    significantTokens[index] = new AwkToken(kind, token.Text, token.Span);
            }

            parser.Tokens = AwkParsingSupport.ToLexerResult(significantTokens);
        }
        try
        {
            return StreamParser(parser, parser.Tokens) switch
            {
                RootParseResult.Expression expression =>
                    AwkExpressionParseResult.Single(expression.Syntax),
                RootParseResult.ExpressionList expressionList =>
                    AwkExpressionParseResult.List(expressionList.Syntax),
                _ => AwkExpressionParseResult.InvalidResult
            };
        }
        catch (ParseException)
        {
            return AwkExpressionParseResult.InvalidResult;
        }
    }

    private static AwkTokenKind ResolveExpressionTokenKind(IReadOnlyList<AwkToken> tokens, int index)
    {
        var token = tokens[index];
        if (token.Kind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus)
        {
            var previous = index - 1;
            while (previous >= 0 && !AwkTokenNavigation.IsSignificant(tokens[previous].Kind))
                previous--;

            if (previous >= 0 && tokens[previous].Kind is
                AwkTokenKind.Identifier or AwkTokenKind.SpacedIdentifier or
                AwkTokenKind.Number or AwkTokenKind.String or AwkTokenKind.InternationalizedString or
                AwkTokenKind.Regex or AwkTokenKind.StrongRegex or
                AwkTokenKind.RightBracket or AwkTokenKind.RightParen)
            {
                return token.Kind is AwkTokenKind.PlusPlus
                    ? AwkTokenKind.PostfixPlusPlus
                    : AwkTokenKind.PostfixMinusMinus;
            }
        }

        if (token.Kind is not AwkTokenKind.Identifier)
            return token.Kind;

        var next = AwkTokenNavigation.FindNextSignificant(tokens, index + 1);
        if (next is { Kind: AwkTokenKind.LeftParen } &&
            token.Span.End < next.Value.Span.Start &&
            !AwkBuiltins.IsKnownFunctionOrSpecialForm(token.Text))
        {
            return AwkTokenKind.SpacedIdentifier;
        }

        return token.Kind;
    }

    private static AwkExpressionSyntax? SingleExpressionOrNull(AwkExpressionParseResult result) =>
        result is AwkExpressionParseResult.Expression expression
            ? expression.Syntax
            : null;

    private static AwkExpressionSyntax? ParsedExpressionOrNull(AwkExpressionParseResult result) =>
        result switch
        {
            AwkExpressionParseResult.Expression expression => expression.Syntax,
            AwkExpressionParseResult.ExpressionList expressionList => expressionList.Syntax,
            _ => null
        };

    [Rule]
    public RootParseResult ExpressionRoot(
        [NT] AwkExpressionSyntax expression,
        [AwkT(AwkTokenKind.EndOfFile)] Pos<string> endOfFile) =>
        new RootParseResult.Expression(expression);

    [Rule]
    public RootParseResult ExpressionListRoot(
        [NT] ExpressionListSyntax expressionList,
        [AwkT(AwkTokenKind.EndOfFile)] Pos<string> endOfFile) =>
        new RootParseResult.ExpressionList(expressionList.Expression);

    [Rule]
    public ExpressionListSyntax ExpressionList(
        [NT] AwkExpressionSyntax first,
        [AwkL(Min = 1)] ExpressionListTail[] tails)
    {
        var expressions = new List<AwkExpressionSyntax>(tails.Length + 1) { first };
        var separators = new List<AwkToken>(tails.Length);
        for (var index = 0; index < tails.Length; index++)
        {
            separators.Add(tails[index].Separator);
            expressions.Add(tails[index].Expression);
        }

        return new ExpressionListSyntax(CreateNode(
            AwkExpressionKind.ExpressionList,
            expressions[0].FirstToken,
            expressions[^1].LastToken,
            expressions,
            [],
            [],
            separators));
    }

    [Rule]
    public ExpressionListTail ExpressionListTailRule(
        [AwkT(AwkTokenKind.Comma)] Pos<string> comma,
        [NT] AwkExpressionSyntax expression) =>
        new(ToToken(AwkTokenKind.Comma, comma), expression);

    [Rule]
    public AwkExpressionSyntax NumberLiteral([AwkT(AwkTokenKind.Number)] Pos<string> token) =>
        CreateTokenNode(AwkExpressionKind.Number, ToToken(AwkTokenKind.Number, token));

    [Rule]
    public AwkExpressionSyntax StringLiteral([AwkT(AwkTokenKind.String, AwkTokenKind.InternationalizedString)] Pos<string> token) =>
        CreateTokenNode(AwkExpressionKind.String, AwkParsingSupport.ToToken(token));

    [Rule]
    public AwkExpressionSyntax RegexLiteral([AwkT(AwkTokenKind.Regex, AwkTokenKind.StrongRegex)] Pos<string> token) =>
        CreateTokenNode(AwkExpressionKind.Regex, AwkParsingSupport.ToToken(token));

    [Rule]
    public AwkExpressionSyntax Variable(
        [AwkT(AwkTokenKind.Identifier, AwkTokenKind.SpacedIdentifier)] Pos<string> token) =>
        CreateTokenNode(AwkExpressionKind.Variable, ToToken(AwkTokenKind.Identifier, token));

    [Rule]
    public AwkExpressionSyntax Group(
        [AwkT(AwkTokenKind.LeftParen)] Pos<string> open,
        [NTO] ExpressionListSyntax? list,
        [NT] AwkExpressionSyntax expression,
        [AwkT(AwkTokenKind.RightParen)] Pos<string> close)
    {
        var openToken = ToToken(AwkTokenKind.LeftParen, open);
        var closeToken = ToToken(AwkTokenKind.RightParen, close);
        var child = list?.Expression ?? expression;
        return CreateNode(
            AwkExpressionKind.Group,
            openToken,
            closeToken,
            [child],
            [],
            [openToken, closeToken],
            []);
    }

    [Rule]
    public AwkExpressionSyntax GroupList(
        [AwkT(AwkTokenKind.LeftParen)] Pos<string> open,
        [NT] ExpressionListSyntax list,
        [AwkT(AwkTokenKind.RightParen)] Pos<string> close)
    {
        var openToken = ToToken(AwkTokenKind.LeftParen, open);
        var closeToken = ToToken(AwkTokenKind.RightParen, close);
        return CreateNode(
            AwkExpressionKind.Group,
            openToken,
            closeToken,
            [list.Expression],
            [],
            [openToken, closeToken],
            []);
    }

    [Rule]
    public AwkExpressionSyntax Field(
        [AwkT(AwkTokenKind.Dollar)] Pos<string> dollar,
        [NT(0)] AwkExpressionSyntax index)
    {
        var dollarToken = ToToken(AwkTokenKind.Dollar, dollar);
        if (index.Kind == AwkExpressionKind.Postfix &&
            index.Children.Count == 1 &&
            index.OperatorTokens.Count == 1)
        {
            var field = CreateNode(AwkExpressionKind.Field, dollarToken, index.Children[0], [dollarToken]);
            return CreateNode(AwkExpressionKind.Postfix, field, index.OperatorTokens[0], [index.OperatorTokens[0]]);
        }

        return CreateNode(AwkExpressionKind.Field, dollarToken, index, [dollarToken]);
    }

    [Rule]
    public AwkExpressionSyntax FieldWithUnaryIndex(
        [AwkT(AwkTokenKind.Dollar)] Pos<string> dollar,
        [AwkT(AwkTokenKind.Bang, AwkTokenKind.Plus, AwkTokenKind.Minus)] Pos<string> op,
        [NT(2)] AwkExpressionSyntax operand) =>
        Field(dollar, Unary(op, operand));

    [Rule]
    public AwkExpressionSyntax FunctionCallNoArguments(
        [AwkT(AwkTokenKind.Identifier)] Pos<string> target,
        [AwkT(AwkTokenKind.LeftParen)] Pos<string> open,
        [AwkT(AwkTokenKind.RightParen)] Pos<string> close)
    {
        var targetToken = ToToken(AwkTokenKind.Identifier, target);
        var openToken = ToToken(AwkTokenKind.LeftParen, open);
        var closeToken = ToToken(AwkTokenKind.RightParen, close);
        var targetExpression = CreateTokenNode(AwkExpressionKind.Variable, targetToken);
        return CreateNode(
            AwkExpressionKind.FunctionCall,
            targetToken,
            closeToken,
            [targetExpression],
            [],
            [openToken, closeToken],
            []);
    }

    [Rule]
    public AwkExpressionSyntax FunctionCall(
        [AwkT(AwkTokenKind.Identifier)] Pos<string> target,
        [AwkT(AwkTokenKind.LeftParen)] Pos<string> open,
        [NT] AwkExpressionSyntax firstArgument,
        [AwkL(Min = 0)] ArgumentTail[] tails,
        [AwkT(AwkTokenKind.RightParen)] Pos<string> close)
    {
        var targetToken = ToToken(AwkTokenKind.Identifier, target);
        var openToken = ToToken(AwkTokenKind.LeftParen, open);
        var closeToken = ToToken(AwkTokenKind.RightParen, close);
        var targetExpression = CreateTokenNode(AwkExpressionKind.Variable, targetToken);
        var children = new List<AwkExpressionSyntax>(tails.Length + 2) { targetExpression, firstArgument };
        var separators = new List<AwkToken>(tails.Length);
        for (var index = 0; index < tails.Length; index++)
        {
            separators.Add(tails[index].Separator);
            children.Add(tails[index].Expression);
        }

        return CreateNode(
            AwkExpressionKind.FunctionCall,
            targetToken,
            closeToken,
            children,
            [],
            [openToken, closeToken],
            separators);
    }

    [Rule]
    public ArgumentTail ArgumentTailRule(
        [AwkT(AwkTokenKind.Comma)] Pos<string> comma,
        [NT] AwkExpressionSyntax expression) =>
        new(ToToken(AwkTokenKind.Comma, comma), expression);

    [Rule]
    public AwkExpressionSyntax ArrayElementNoIndex(
        [NT(0)] AwkExpressionSyntax target,
        [AwkT(AwkTokenKind.LeftBracket)] Pos<string> open,
        [AwkT(AwkTokenKind.RightBracket)] Pos<string> close)
    {
        var openToken = ToToken(AwkTokenKind.LeftBracket, open);
        var closeToken = ToToken(AwkTokenKind.RightBracket, close);
        return CreateNode(
            AwkExpressionKind.ArrayElement,
            target.FirstToken,
            closeToken,
            [target],
            [],
            [openToken, closeToken],
            []);
    }

    [Rule]
    public AwkExpressionSyntax ArrayElement(
        [NT(0)] AwkExpressionSyntax target,
        [AwkT(AwkTokenKind.LeftBracket)] Pos<string> open,
        [NT] AwkExpressionSyntax firstIndex,
        [AwkL(Min = 0)] ArgumentTail[] tails,
        [AwkT(AwkTokenKind.RightBracket)] Pos<string> close)
    {
        var openToken = ToToken(AwkTokenKind.LeftBracket, open);
        var closeToken = ToToken(AwkTokenKind.RightBracket, close);
        var children = new List<AwkExpressionSyntax>(tails.Length + 2) { target, firstIndex };
        var separators = new List<AwkToken>(tails.Length);
        for (var index = 0; index < tails.Length; index++)
        {
            separators.Add(tails[index].Separator);
            children.Add(tails[index].Expression);
        }

        return CreateNode(
            AwkExpressionKind.ArrayElement,
            target.FirstToken,
            closeToken,
            children,
            [],
            [openToken, closeToken],
            separators);
    }

    [Rule]
    public AwkExpressionSyntax PostfixMutation(
        [NT(0)] AwkExpressionSyntax target,
        [AwkT(AwkTokenKind.PostfixPlusPlus, AwkTokenKind.PostfixMinusMinus)] Pos<string> op) =>
        CreateNode(AwkExpressionKind.Postfix, target, AwkParsingSupport.ToToken(op), [AwkParsingSupport.ToToken(op)]);

    [Rule]
    public AwkExpressionSyntax PrefixMutation(
        [AwkT(AwkTokenKind.PlusPlus, AwkTokenKind.MinusMinus)] Pos<string> op,
        [NT(0)] AwkExpressionSyntax target)
    {
        var operatorToken = AwkParsingSupport.ToToken(op);
        return CreateNode(AwkExpressionKind.Unary, operatorToken, target, [operatorToken]);
    }

    [Rule]
    public AwkExpressionSyntax GetlineOnly([AwkT(AwkTokenKind.Getline)] Pos<string> getline) =>
        CreateGetlineCall(getline, null, null, null);

    [Rule]
    public AwkExpressionSyntax GetlineTarget(
        [AwkT(AwkTokenKind.Getline)] Pos<string> getline,
        [NT(0)] AwkExpressionSyntax target) =>
        CreateGetlineCall(getline, target, null, null);

    [Rule]
    public AwkExpressionSyntax GetlineSource(
        [AwkT(AwkTokenKind.Getline)] Pos<string> getline,
        [AwkT(AwkTokenKind.Less)] Pos<string> less,
        [NT] AwkExpressionSyntax source) =>
        CreateGetlineCall(getline, null, less, source);

    [Rule]
    public AwkExpressionSyntax GetlineTargetSource(
        [AwkT(AwkTokenKind.Getline)] Pos<string> getline,
        [NT(0)] AwkExpressionSyntax target,
        [AwkT(AwkTokenKind.Less)] Pos<string> less,
        [NT] AwkExpressionSyntax source) =>
        CreateGetlineCall(getline, target, less, source);

    [Rule(Rank = 1)]
    public AwkExpressionSyntax Power(
        [NT(0)] AwkExpressionSyntax left,
        [AwkT(AwkTokenKind.Caret)] Pos<string> op,
        [NT(2)] AwkExpressionSyntax right) =>
        CreateNode(AwkExpressionKind.Binary, left, right, [ToToken(AwkTokenKind.Caret, op)]);

    [Rule(Rank = 2)]
    public AwkExpressionSyntax Unary(
        [AwkT(AwkTokenKind.Bang, AwkTokenKind.Plus, AwkTokenKind.Minus)] Pos<string> op,
        [NT(2)] AwkExpressionSyntax operand)
    {
        var operatorToken = AwkParsingSupport.ToToken(op);
        return CreateNode(AwkExpressionKind.Unary, operatorToken, operand, [operatorToken]);
    }

    [Rule]
    public MultiplicativeTailPart MultiplicativeTail(
        [AwkT(AwkTokenKind.Star, AwkTokenKind.Slash, AwkTokenKind.Percent)] Pos<string> op,
        [NT(2)] AwkExpressionSyntax right) =>
        new(AwkParsingSupport.ToToken(op), right);

    [Rule(Rank = 3)]
    public AwkExpressionSyntax Multiplicative(
        [NT(2)] AwkExpressionSyntax left,
        [AwkL(Min = 1)] MultiplicativeTailPart[] tails) =>
        FoldLeftBinary(left, tails);

    [Rule]
    public AdditiveTailPart AdditiveTail(
        [AwkT(AwkTokenKind.Plus, AwkTokenKind.Minus)] Pos<string> op,
        [NT(3)] AwkExpressionSyntax right) =>
        new(AwkParsingSupport.ToToken(op), right);

    [Rule(Rank = 4)]
    public AwkExpressionSyntax Additive(
        [NT(3)] AwkExpressionSyntax left,
        [AwkL(Min = 1)] AdditiveTailPart[] tails) =>
        FoldLeftBinary(left, tails);

    [Rule]
    public ConcatenationTail ConcatenationTailRule([NT(4)] AwkExpressionSyntax right) => new(right);

    [Rule(Rank = 5)]
    public AwkExpressionSyntax Concatenation(
        [NT(4)] AwkExpressionSyntax left,
        [AwkL(Min = 1)] ConcatenationTail[] tails)
    {
        var current = left;
        for (var index = 0; index < tails.Length; index++)
            current = CreateConcatenationNode(AwkExpressionKind.Concatenation, current, tails[index].Right);

        return current;

        static AwkExpressionSyntax CreateConcatenationNode(AwkExpressionKind kind, AwkExpressionSyntax left, AwkExpressionSyntax right) =>
            CreateNode(kind, left, right, []);
    }

    [Rule]
    public ComparisonTailPart ComparisonTail(
        [AwkT(
            AwkTokenKind.Tilde,
            AwkTokenKind.BangTilde,
            AwkTokenKind.In,
            AwkTokenKind.EqualEqual,
            AwkTokenKind.BangEqual,
            AwkTokenKind.Less,
            AwkTokenKind.LessEqual,
            AwkTokenKind.Greater,
            AwkTokenKind.GreaterEqual)] Pos<string> op,
        [NT(5)] AwkExpressionSyntax right) =>
        new(AwkParsingSupport.ToToken(op), right);

    [Rule(Rank = 6)]
    public AwkExpressionSyntax Comparison(
        [NT(5)] AwkExpressionSyntax left,
        [AwkL(Min = 1)] ComparisonTailPart[] tails) =>
        FoldLeftBinary(left, tails);

    [Rule]
    public LogicalAndTailPart LogicalAndTail(
        [AwkT(AwkTokenKind.AmpersandAmpersand)] Pos<string> op,
        [NT(6)] AwkExpressionSyntax right) =>
        new(ToToken(AwkTokenKind.AmpersandAmpersand, op), right);

    [Rule(Rank = 7)]
    public AwkExpressionSyntax LogicalAnd(
        [NT(6)] AwkExpressionSyntax left,
        [AwkL(Min = 1)] LogicalAndTailPart[] tails) =>
        FoldLeftBinary(left, tails);

    [Rule]
    public LogicalOrTailPart LogicalOrTail(
        [AwkT(AwkTokenKind.PipePipe)] Pos<string> op,
        [NT(7)] AwkExpressionSyntax right) =>
        new(ToToken(AwkTokenKind.PipePipe, op), right);

    [Rule(Rank = 8)]
    public AwkExpressionSyntax LogicalOr(
        [NT(7)] AwkExpressionSyntax left,
        [AwkL(Min = 1)] LogicalOrTailPart[] tails) =>
        FoldLeftBinary(left, tails);

    [Rule(Rank = 9)]
    public AwkExpressionSyntax Conditional(
        [NT(8)] AwkExpressionSyntax condition,
        [AwkT(AwkTokenKind.Question)] Pos<string> question,
        [NT] AwkExpressionSyntax whenTrue,
        [AwkT(AwkTokenKind.Colon)] Pos<string> colon,
        [NT(9)] AwkExpressionSyntax whenFalse)
    {
        return CreateConditionalNode(
            AwkExpressionKind.Conditional,
            condition,
            whenTrue,
            whenFalse,
            [ToToken(AwkTokenKind.Question, question), ToToken(AwkTokenKind.Colon, colon)]);
        static AwkExpressionSyntax CreateConditionalNode(
            AwkExpressionKind kind,
            AwkExpressionSyntax first,
            AwkExpressionSyntax second,
            AwkExpressionSyntax third,
            IReadOnlyList<AwkToken> operatorTokens) =>
            CreateNode(kind, first.FirstToken, third.LastToken, [first, second, third], operatorTokens: operatorTokens);
    }
    [Rule(Rank = 10)]
    public AwkExpressionSyntax Assignment(
        [NT(9)] AwkExpressionSyntax left,
        [AwkT(
            AwkTokenKind.Equal,
            AwkTokenKind.PlusEqual,
            AwkTokenKind.MinusEqual,
            AwkTokenKind.StarEqual,
            AwkTokenKind.SlashEqual,
            AwkTokenKind.PercentEqual,
            AwkTokenKind.CaretEqual)] Pos<string> op,
        [NT(10)] AwkExpressionSyntax right)
    {
        var operatorToken = AwkParsingSupport.ToToken(op);
        if (!AcceptsAssignmentOperand(left))
            return CreateNode(AwkExpressionKind.Assignment, left, right, [operatorToken]);

        // Awk's logical, comparison, and conditional operands admit a full expression,
        // whereas assignment's left operand must be an lvalue. Restore that grammar
        // after ranked parsing: 1 && x = y || z means 1 && (x = (y || z)).
        var containers = new Stack<AwkExpressionSyntax>();
        var target = left;
        while (AcceptsAssignmentOperand(target))
        {
            containers.Push(target);
            target = target.Children[^1];
        }

        var expression = CreateNode(AwkExpressionKind.Assignment, target, right, [operatorToken]);
        while (containers.TryPop(out var container))
        {
            expression = container.Kind == AwkExpressionKind.Conditional
                ? CreateNode(AwkExpressionKind.Conditional, container.FirstToken, expression.LastToken,
                    [container.Children[0], container.Children[1], expression], container.OperatorTokens)
                : CreateNode(AwkExpressionKind.Binary, container.Children[0], expression, container.OperatorTokens);
        }
        return expression;

        static bool AcceptsAssignmentOperand(AwkExpressionSyntax expression) =>
            expression.Kind == AwkExpressionKind.Conditional ||
            expression.Kind == AwkExpressionKind.Binary && expression.OperatorTokens[0].Kind is
                AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe or
                AwkTokenKind.Tilde or AwkTokenKind.BangTilde or
                AwkTokenKind.EqualEqual or AwkTokenKind.BangEqual or
                AwkTokenKind.Less or AwkTokenKind.LessEqual or
                AwkTokenKind.Greater or AwkTokenKind.GreaterEqual;
    }

    private static AwkExpressionSyntax CreateGetlineCall(
        Pos<string> getline,
        AwkExpressionSyntax? target,
        Pos<string>? less,
        AwkExpressionSyntax? source)
    {
        var getlineToken = ToToken(AwkTokenKind.Identifier, getline);
        var children = new List<AwkExpressionSyntax> { CreateTokenNode(AwkExpressionKind.Variable, getlineToken) };
        if (target != null)
            children.Add(target);

        var operators = new List<AwkToken>();
        if (less.HasValue)
        {
            operators.Add(ToToken(AwkTokenKind.Less, less.Value));
            if (source != null)
                children.Add(source);
        }

        var last = children[^1].LastToken;
        return CreateNode(
            AwkExpressionKind.FunctionCall,
            getlineToken,
            last,
            children,
            operators,
            [],
            []);
    }

    private static AwkExpressionSyntax FoldLeftBinary<TTail>(
        AwkExpressionSyntax left,
        IReadOnlyList<TTail> tails)
        where TTail : IBinaryTailPart
    {
        var current = left;
        for (var index = 0; index < tails.Count; index++)
            current = CreateNode(AwkExpressionKind.Binary, current, tails[index].Right, [tails[index].Operator]);

        return current;
    }

    private static AwkExpressionSyntax CreateTokenNode(AwkExpressionKind kind, AwkToken token) =>
        new(kind, [token], [], token.Span);

    private static AwkExpressionSyntax CreateNode(
        AwkExpressionKind kind,
        AwkExpressionSyntax left,
        AwkExpressionSyntax right,
        IReadOnlyList<AwkToken> operatorTokens) =>
        CreateNode(kind, left.FirstToken, right.LastToken, [left, right], operatorTokens: operatorTokens);

    private static AwkExpressionSyntax CreateNode(
        AwkExpressionKind kind,
        AwkExpressionSyntax first,
        AwkToken last,
        IReadOnlyList<AwkToken> operatorTokens) =>
        CreateNode(kind, first.FirstToken, last, [first], operatorTokens: operatorTokens);

    private static AwkExpressionSyntax CreateNode(
        AwkExpressionKind kind,
        AwkToken first,
        AwkExpressionSyntax child,
        IReadOnlyList<AwkToken> operatorTokens) =>
        CreateNode(kind, first, child.LastToken, [child], operatorTokens: operatorTokens);

    private static AwkExpressionSyntax CreateNode(
        AwkExpressionKind kind,
        AwkToken first,
        AwkToken last,
        IReadOnlyList<AwkExpressionSyntax> children,
        IReadOnlyList<AwkToken> operatorTokens) =>
        CreateNode(kind, first, last, children, operatorTokens, [], []);

    private static AwkExpressionSyntax CreateNode(
        AwkExpressionKind kind,
        AwkToken first,
        AwkToken last,
        IReadOnlyList<AwkExpressionSyntax> children,
        IReadOnlyList<AwkToken> operatorTokens,
        IReadOnlyList<AwkToken> delimiterTokens,
        IReadOnlyList<AwkToken> separatorTokens)
    {
        return new AwkExpressionSyntax(
            kind,
            null,
            operatorTokens,
            delimiterTokens,
            separatorTokens,
            children,
            new AwkTextSpan(first.Span.Start, last.Span.End - first.Span.Start),
            first,
            last);
    }

    private static AwkToken ToToken(AwkTokenKind kind, Pos<string> token) =>
        AwkParsingSupport.ToToken(kind, token);

    private abstract class AwkExpressionParseResult
    {
        public static Empty EmptyResult { get; } = new();

        public static Invalid InvalidResult { get; } = new();

        public static Expression Single(AwkExpressionSyntax expression) => new(expression);

        public static ExpressionList List(AwkExpressionSyntax expression) => new(expression);

        public sealed class Empty : AwkExpressionParseResult;

        public sealed class Invalid : AwkExpressionParseResult;

        public sealed class Expression(AwkExpressionSyntax syntax) : AwkExpressionParseResult
        {
            public AwkExpressionSyntax Syntax { get; } = syntax;
        }

        public sealed class ExpressionList(AwkExpressionSyntax syntax) : AwkExpressionParseResult
        {
            public AwkExpressionSyntax Syntax { get; } = syntax;
        }
    }

    internal abstract class RootParseResult
    {
        private RootParseResult()
        {
        }

        internal sealed class Expression(AwkExpressionSyntax syntax) : RootParseResult
        {
            public AwkExpressionSyntax Syntax { get; } = syntax;
        }

        internal sealed class ExpressionList(AwkExpressionSyntax syntax) : RootParseResult
        {
            public AwkExpressionSyntax Syntax { get; } = syntax;
        }
    }

    internal sealed class ExpressionListSyntax(AwkExpressionSyntax expression)
    {
        public AwkExpressionSyntax Expression { get; } = expression;
    }

    public readonly struct ExpressionListTail(AwkToken separator, AwkExpressionSyntax? expression)
    {
        public AwkToken Separator { get; } = separator;

        public AwkExpressionSyntax Expression => expression ??
            throw new InvalidOperationException("The default expression-list tail has no expression.");
    }

    public readonly struct ArgumentTail(AwkToken separator, AwkExpressionSyntax? expression)
    {
        public AwkToken Separator { get; } = separator;

        public AwkExpressionSyntax Expression => expression ??
            throw new InvalidOperationException("The default argument tail has no expression.");
    }

    public interface IBinaryTailPart
    {
        AwkToken Operator { get; }

        AwkExpressionSyntax Right { get; }
    }

    public readonly struct MultiplicativeTailPart(AwkToken op, AwkExpressionSyntax? right) : IBinaryTailPart
    {
        public AwkToken Operator { get; } = op;

        public AwkExpressionSyntax Right => right ??
            throw new InvalidOperationException("The default multiplicative tail has no right operand.");
    }

    public readonly struct AdditiveTailPart(AwkToken op, AwkExpressionSyntax? right) : IBinaryTailPart
    {
        public AwkToken Operator { get; } = op;

        public AwkExpressionSyntax Right => right ??
            throw new InvalidOperationException("The default additive tail has no right operand.");
    }

    public readonly struct ComparisonTailPart(AwkToken op, AwkExpressionSyntax? right) : IBinaryTailPart
    {
        public AwkToken Operator { get; } = op;

        public AwkExpressionSyntax Right => right ??
            throw new InvalidOperationException("The default comparison tail has no right operand.");
    }

    public readonly struct LogicalAndTailPart(AwkToken op, AwkExpressionSyntax? right) : IBinaryTailPart
    {
        public AwkToken Operator { get; } = op;

        public AwkExpressionSyntax Right => right ??
            throw new InvalidOperationException("The default logical-and tail has no right operand.");
    }

    public readonly struct LogicalOrTailPart(AwkToken op, AwkExpressionSyntax? right) : IBinaryTailPart
    {
        public AwkToken Operator { get; } = op;

        public AwkExpressionSyntax Right => right ??
            throw new InvalidOperationException("The default logical-or tail has no right operand.");
    }

    public readonly struct ConcatenationTail(AwkExpressionSyntax? right)
    {
        public AwkExpressionSyntax Right => right ??
            throw new InvalidOperationException("The default concatenation tail has no right operand.");
    }
}
