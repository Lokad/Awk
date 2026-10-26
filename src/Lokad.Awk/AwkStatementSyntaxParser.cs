using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Parsing.Error;
using Lokad.Parsing.Parser;

namespace Lokad.Awk;

internal sealed class AwkStatementSyntaxParser
    : GrammarParser<AwkStatementSyntaxParser, AwkTokenKind, AwkStatementSyntaxParser.ParseResult>
{
    private readonly IReadOnlyList<AwkToken> _originalTokens;

    private readonly AwkParsingSource? _source;
    private readonly Action? _checkpoint;

    private AwkStatementSyntaxParser(AwkParsingSource? source, IReadOnlyList<AwkToken> originalTokens, Action? checkpoint)
        : base(AwkTokenNamer.Instance)
    {
        _source = source;
        _originalTokens = originalTokens;
        _checkpoint = checkpoint;
    }

    private static AwkStatementParseResult ParseCore(
        AwkParsingSource? source,
        IReadOnlyList<AwkToken> tokens,
        Action? checkpoint)
    {
        checkpoint?.Invoke();
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        var parser = new AwkStatementSyntaxParser(source, tokens, checkpoint);
        parser.Tokens = source == null
            ? AwkParsingSupport.ToLexerResult(tokens, checkpoint)
            : AwkParsingSupport.ToLexerResultWithEndOfFile(source, tokens, checkpoint);
        try
        {
            checkpoint?.Invoke();
            var statements = StreamParser(parser, parser.Tokens).Statements;
            checkpoint?.Invoke();
            return AwkStatementParseResult.Success(
                statements,
                CollectParseDiagnostics(statements, parser));
        }
        catch (ParseException)
        {
            return AwkStatementParseResult.InvalidResult;
        }

        static List<AwkDiagnostic> CollectParseDiagnostics(
            IReadOnlyList<AwkStatementSyntax> statements, AwkStatementSyntaxParser parser)
        {
            var diagnostics = new List<AwkDiagnostic>();
            parser.AddStatementDiagnostics(statements, diagnostics);
            return diagnostics;
        }
    }

    public static IReadOnlyList<AwkStatementSyntax> ParseStatements(
        AwkParsingSource source,
        IReadOnlyList<AwkToken> tokens,
        List<AwkDiagnostic> diagnostics,
        string invalidMessage,
        Action? checkpoint = null)
    {
        var result = ParseCore(source, tokens, checkpoint);
        if (result is AwkStatementParseResult.Parsed parsed)
        {
            checkpoint?.Invoke();
            diagnostics.AddRange(parsed.Diagnostics);
            checkpoint?.Invoke();
            return parsed.Statements;
        }

        AddInvalidStatementDiagnostic(tokens, diagnostics, invalidMessage, checkpoint);
        return [];
    }

    public static IReadOnlyList<AwkStatementSyntax> ParseStatements(
        IReadOnlyList<AwkToken> tokens,
        List<AwkDiagnostic> diagnostics,
        string invalidMessage,
        Action? checkpoint = null)
    {
        var result = ParseCore(null, tokens, checkpoint);
        if (result is AwkStatementParseResult.Parsed parsed)
        {
            checkpoint?.Invoke();
            diagnostics.AddRange(parsed.Diagnostics);
            checkpoint?.Invoke();
            return parsed.Statements;
        }

        AddInvalidStatementDiagnostic(tokens, diagnostics, invalidMessage, checkpoint);
        return [];
    }

    private static void AddInvalidStatementDiagnostic(
        IReadOnlyList<AwkToken> tokens,
        List<AwkDiagnostic> diagnostics,
        string invalidMessage, Action? checkpoint)
    {
        var first = AwkTokenNavigation.FindFirstSignificant(tokens, checkpoint);
        if (first != null)
            diagnostics.Add(new AwkDiagnostic(first.Value.Span, invalidMessage));
    }

    [Rule]
    public ParseResult StatementListRoot(
        [AwkL(Min = 0)] StatementListItem[] items,
        [AwkT(AwkTokenKind.EndOfFile)] Pos<string> endOfFile)
    {
        _checkpoint?.Invoke();
        return new(StatementsFrom(items));
    }

    [Rule]
    public StatementListItem StatementItem([NT] AwkStatementSyntax statement)
    {
        _checkpoint?.Invoke();
        return new(statement, statement.Tokens, null);
    }

    [Rule]
    public StatementListItem SeparatorItem([NT] StatementSeparator separator)
    {
        _checkpoint?.Invoke();
        return new(null, separator.Tokens, null);
    }

    [Rule]
    public StatementListItem ElseContinuationItem([NT] ElseContinuation continuation)
    {
        _checkpoint?.Invoke();
        return new(null, continuation.Tokens, continuation);
    }

    [Rule]
    public StatementSeparator StatementSeparatorRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Semicolon, AwkTokenKind.Comment)] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    [Rule]
    public HeaderGap HeaderGapRule([AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    [Rule]
    public ContinuationSeparator ContinuationSeparatorRule(
        [AwkT(AwkTokenKind.NewLine, AwkTokenKind.Comment)] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    [Rule]
    public AwkStatementSyntax IfWithoutElse(
        [AwkT(AwkTokenKind.If)] Pos<string> keyword,
        [NT] ParenthesizedGroup header,
        [AwkL(Min = 0)] HeaderGap[] beforeBody,
        [NT] StatementBody body)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.If, keyword);
        var headerSlot = ParseExpressionSlot(header.InnerTokens);
        return CreateStatement(
            AwkStatementKind.If,
            Flatten([[keywordToken], header.FullTokens, Flatten(beforeBody), body.Tokens]),
            ExpressionsFromSlots([headerSlot]),
            body.Statements,
            [],
            [],
            new AwkStatementSyntaxOptions
            {
                Header = headerSlot,
                HasUnbracedBody = IsUnbracedBody(body)
            });
    }

    [Rule]
    public AwkStatementSyntax IfWithElse(
        [AwkT(AwkTokenKind.If)] Pos<string> keyword,
        [NT] ParenthesizedGroup header,
        [AwkL(Min = 0)] HeaderGap[] beforeBody,
        [NT] StatementBody body,
        [NT] ElseIntro elseIntro,
        [NT] StatementBody elseBody)
    {
        _checkpoint?.Invoke();
        var statement = IfWithoutElse(keyword, header, beforeBody, body);
        var continuation = new ElseContinuation(
            Flatten([elseIntro.Tokens, elseBody.Tokens]),
            elseBody.Statements,
            IsUnbracedBody(elseBody));
        return TryAttachElse(statement, [], continuation, out var attached)
            ? attached
            : CreateStatement(AwkStatementKind.Unknown,
                Flatten([statement.Tokens, continuation.Tokens]), [], [], [], []);
    }

    [Rule]
    public ElseIntro ElseIntroWithoutSemicolon(
        [AwkT(AwkTokenKind.Else)] Pos<string> elseKeyword,
        [AwkL(Min = 0)] HeaderGap[] afterElse)
    {
        _checkpoint?.Invoke();
        var elseToken = ToToken(AwkTokenKind.Else, elseKeyword);
        return new ElseIntro(Flatten([[elseToken], Flatten(afterElse)]));
    }

    [Rule]
    public ElseContinuation ElseContinuationRule(
        [AwkT(AwkTokenKind.Else)] Pos<string> elseKeyword,
        [AwkL(Min = 0)] HeaderGap[] afterElse,
        [NT] StatementBody elseBody)
    {
        _checkpoint?.Invoke();
        var elseToken = ToToken(AwkTokenKind.Else, elseKeyword);
        return new ElseContinuation(
            Flatten([[elseToken], Flatten(afterElse), elseBody.Tokens]),
            elseBody.Statements,
            IsUnbracedBody(elseBody));
    }

    [Rule]
    public AwkStatementSyntax MalformedIfStatement(
        [AwkT(AwkTokenKind.If)] Pos<string> keyword,
        [AwkL(Min = 0)] MalformedControlPart[] parts)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.If, keyword);
        return CreateStatement(
            AwkStatementKind.If,
            [keywordToken, .. Flatten(parts.Select(part => part.Tokens))],
            [],
            [],
            [],
            []);
    }

    [Rule]
    public AwkStatementSyntax WhileStatement(
        [AwkT(AwkTokenKind.While)] Pos<string> keyword,
        [NT] ParenthesizedGroup header,
        [AwkL(Min = 0)] HeaderGap[] beforeBody,
        [NT] StatementBody body)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.While, keyword);
        var headerSlot = ParseExpressionSlot(header.InnerTokens);
        return CreateStatement(
            AwkStatementKind.While,
            Flatten([[keywordToken], header.FullTokens, Flatten(beforeBody), body.Tokens]),
            ExpressionsFromSlots([headerSlot]),
            body.Statements,
            [],
            [],
            new AwkStatementSyntaxOptions { Header = headerSlot, HasUnbracedBody = IsUnbracedBody(body) });
    }

    [Rule]
    public AwkStatementSyntax DoWhileStatement(
        [AwkT(AwkTokenKind.Do)] Pos<string> keyword,
        [AwkL(Min = 0)] HeaderGap[] beforeBody,
        [NT] StatementBody body,
        [AwkL(Min = 0)] StatementSeparator[] beforeWhile,
        [AwkT(AwkTokenKind.While)] Pos<string> whileKeyword,
        [NT] ParenthesizedGroup header)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.Do, keyword);
        var whileToken = ToToken(AwkTokenKind.While, whileKeyword);
        var headerSlot = ParseExpressionSlot(header.InnerTokens);
        return CreateStatement(
            AwkStatementKind.DoWhile,
            Flatten([
                [keywordToken],
                Flatten(beforeBody),
                body.Tokens,
                Flatten(beforeWhile),
                [whileToken],
                header.FullTokens
            ]),
            ExpressionsFromSlots([headerSlot]),
            body.Statements,
            [],
            [],
            new AwkStatementSyntaxOptions { Header = headerSlot });
    }

    [Rule]
    public AwkStatementSyntax MalformedDoWhileStatement(
        [AwkT(AwkTokenKind.Do)] Pos<string> keyword,
        [AwkL(Min = 0)] HeaderGap[] beforeBody,
        [NT] StatementBody body,
        [AwkL(Min = 0)] StatementSeparator[] beforeWhile,
        [AwkT(AwkTokenKind.While)] Pos<string> whileKeyword,
        [AwkL(Min = 0)] MalformedControlPart[] condition)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.Do, keyword);
        var whileToken = ToToken(AwkTokenKind.While, whileKeyword);
        return CreateStatement(
            AwkStatementKind.DoWhile,
            Flatten([
                [keywordToken],
                Flatten(beforeBody),
                body.Tokens,
                Flatten(beforeWhile),
                [whileToken],
                Flatten(condition.Select(part => part.Tokens))
            ]),
            [],
            body.Statements,
            [],
            []);
    }

    [Rule]
    public AwkStatementSyntax ForStatement(
        [AwkT(AwkTokenKind.For)] Pos<string> keyword,
        [NT] ParenthesizedGroup header,
        [AwkL(Min = 0)] HeaderGap[] beforeBody,
        [NT] StatementBody body)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.For, keyword);
        var forHeader = ParseForHeader(header.InnerTokens);
        return CreateStatement(
            AwkStatementKind.For,
            Flatten([[keywordToken], header.FullTokens, Flatten(beforeBody), body.Tokens]),
            ExpressionsFromSlots(forHeader.Parts),
            body.Statements,
            [],
            [],
            new AwkStatementSyntaxOptions { ForHeader = forHeader, HasUnbracedBody = IsUnbracedBody(body) });

        AwkForHeaderSyntax ParseForHeader(IReadOnlyList<AwkToken> tokens)
        {
            var inIndex = AwkTokenNavigation.FindTopLevel(tokens, AwkTokenKind.In, 0, tokens.Count, checkpoint: _checkpoint);
            if (inIndex >= 0)
            {
                return new AwkForHeaderSyntax(
                    isForIn: true,
                    ParseExpressionSlots(
                    [
                        AwkTokenNavigation.Slice(tokens, 0, inIndex, checkpoint: _checkpoint),
                    AwkTokenNavigation.Slice(tokens, inIndex + 1, tokens.Count - inIndex - 1, checkpoint: _checkpoint)
                    ], allowLeadingNewlines: false));
            }

            return new AwkForHeaderSyntax(
                isForIn: false,
                ParseExpressionSlots(AwkTokenNavigation.SplitTopLevel(tokens, AwkTokenKind.Semicolon, checkpoint: _checkpoint), allowLeadingNewlines: true));

            AwkExpressionSlotSyntax[] ParseExpressionSlots(
                IReadOnlyList<IReadOnlyList<AwkToken>> segments,
                bool allowLeadingNewlines)
            {
                var slots = new AwkExpressionSlotSyntax[segments.Count];
                for (var index = 0; index < segments.Count; index++)
                {
                    _checkpoint?.Invoke();
                    var start = 0;
                    if (allowLeadingNewlines && index > 0)
                    {
                        while (start < segments[index].Count &&
                            segments[index][start].Kind is AwkTokenKind.NewLine or AwkTokenKind.Comment)
                        {
                            _checkpoint?.Invoke();
                            start++;
                        }
                    }
                    slots[index] = new AwkExpressionSlotSyntax(segments[index],
                        AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpression(
                            start == 0 ? segments[index] : AwkTokenNavigation.Slice(segments[index], start, segments[index].Count - start, checkpoint: _checkpoint))));
                }

                return slots;
            }
        }
    }

    [Rule]
    public AwkStatementSyntax SwitchStatement(
        [AwkT(AwkTokenKind.Switch)] Pos<string> keyword,
        [NT] ParenthesizedGroup header,
        [AwkL(Min = 0)] HeaderGap[] beforeBody,
        [NT] SwitchBlock body)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.Switch, keyword);
        var headerSlot = ParseExpressionSlot(header.InnerTokens);
        return CreateStatement(
            AwkStatementKind.Switch,
            Flatten([[keywordToken], header.FullTokens, Flatten(beforeBody), body.Tokens]),
            ExpressionsFromSlots([headerSlot]),
            [],
            [],
            body.Cases,
            new AwkStatementSyntaxOptions { Header = headerSlot });
    }

    [Rule]
    public StatementBody BracedStatementBody(
        [AwkT(AwkTokenKind.LeftBrace)] Pos<string> open,
        [AwkL(Min = 0)] StatementListItem[] items,
        [AwkT(AwkTokenKind.RightBrace)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var openToken = ToToken(AwkTokenKind.LeftBrace, open);
        var closeToken = ToToken(AwkTokenKind.RightBrace, close);
        var innerTokens = Flatten(items.Select(item => item.Tokens));
        return new StatementBody(
            Flatten([[openToken], innerTokens, [closeToken]]),
            StatementsFrom(items));
    }

    [Rule]
    public StatementBody SingleStatementBody([NT] AwkStatementSyntax statement)
    {
        _checkpoint?.Invoke();
        return new(statement.Tokens,
            statement.Kind is AwkStatementKind.Block ? statement.BodyStatements : [statement]);
    }

    [Rule]
    public StatementBody NullStatementBody([AwkT(AwkTokenKind.Semicolon)] Pos<string> semicolon)
    {
        _checkpoint?.Invoke();
        return new([ToToken(AwkTokenKind.Semicolon, semicolon)], []);
    }

    [Rule]
    public AwkStatementSyntax StandaloneBlock(
        [AwkT(AwkTokenKind.LeftBrace)] Pos<string> open,
        [AwkL(Min = 0)] StatementListItem[] items,
        [AwkT(AwkTokenKind.RightBrace)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var body = BracedStatementBody(open, items, close);
        return CreateStatement(AwkStatementKind.Block, body.Tokens, [], body.Statements, [], []);
    }

    [Rule]
    public SwitchBlock SwitchBlockRule(
        [AwkT(AwkTokenKind.LeftBrace)] Pos<string> open,
        [AwkL(Min = 0)] SwitchBlockItem[] items,
        [AwkT(AwkTokenKind.RightBrace)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var openToken = ToToken(AwkTokenKind.LeftBrace, open);
        var closeToken = ToToken(AwkTokenKind.RightBrace, close);
        var cases = new List<AwkSwitchCaseSyntax>();
        for (var index = 0; index < items.Length; index++)
        {
            _checkpoint?.Invoke();
            if (items[index].Case is { } switchCase)
                cases.Add(switchCase);
        }

        return new SwitchBlock(
            Flatten([[openToken], Flatten(items.Select(item => item.Tokens)), [closeToken]]),
            cases);
    }

    [Rule]
    public SwitchBlockItem SwitchCaseItem([NT] SwitchCasePart switchCase)
    {
        _checkpoint?.Invoke();
        return new(switchCase.Syntax, switchCase.Tokens);
    }

    [Rule]
    public SwitchBlockItem SwitchSeparatorItem([NT] StatementSeparator separator)
    {
        _checkpoint?.Invoke();
        return new(null, separator.Tokens);
    }

    [Rule]
    public SwitchCasePart CaseClause(
        [AwkT(AwkTokenKind.Case)] Pos<string> keyword,
        [AwkL(Min = 0)] CaseLabelPart[] label,
        [AwkT(AwkTokenKind.Colon)] Pos<string> colon,
        [AwkL(Min = 0)] StatementListItem[] body)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.Case, keyword);
        var colonToken = ToToken(AwkTokenKind.Colon, colon);
        var labelTokens = Flatten(label.Select(part => part.Tokens));
        var bodyTokens = Flatten(body.Select(item => item.Tokens));
        var expression = ParseExpression(labelTokens);
        var labelSyntax = expression == null
            ? AwkSwitchCaseLabelSyntax.Missing
            : AwkSwitchCaseLabelSyntax.FromExpression(expression);
        var syntax = new AwkSwitchCaseSyntax(
            labelSyntax,
            StatementsFrom(body));
        return new SwitchCasePart(
            syntax,
            Flatten([[keywordToken], labelTokens, [colonToken], bodyTokens]));
    }

    [Rule]
    public SwitchCasePart DefaultClause(
        [AwkT(AwkTokenKind.Default)] Pos<string> keyword,
        [AwkT(AwkTokenKind.Colon)] Pos<string> colon,
        [AwkL(Min = 0)] StatementListItem[] body)
    {
        _checkpoint?.Invoke();
        var keywordToken = ToToken(AwkTokenKind.Default, keyword);
        var colonToken = ToToken(AwkTokenKind.Colon, colon);
        var bodyTokens = Flatten(body.Select(item => item.Tokens));
        var syntax = new AwkSwitchCaseSyntax(
            AwkSwitchCaseLabelSyntax.Default,
            StatementsFrom(body));
        return new SwitchCasePart(
            syntax,
            Flatten([[keywordToken, colonToken], bodyTokens]));
    }

    [Rule]
    public CaseLabelPart CaseLabelParenthesized([NT] ParenthesizedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public CaseLabelPart CaseLabelBracketed([NT] BracketedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public CaseLabelPart CaseLabelLeaf([AwkStatementCaseLabelLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    [Rule]
    public AwkStatementSyntax PrintStatement(
        [AwkT(AwkTokenKind.Print)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Print, ToToken(AwkTokenKind.Print, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax PrintfStatement(
        [AwkT(AwkTokenKind.Printf)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Printf, ToToken(AwkTokenKind.Printf, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax DeleteStatement(
        [AwkT(AwkTokenKind.Delete)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Delete, ToToken(AwkTokenKind.Delete, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax GetlineStatement(
        [AwkT(AwkTokenKind.Getline)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Getline, ToToken(AwkTokenKind.Getline, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax BreakStatement(
        [AwkT(AwkTokenKind.Break)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Break, ToToken(AwkTokenKind.Break, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax ContinueStatement(
        [AwkT(AwkTokenKind.Continue)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Continue, ToToken(AwkTokenKind.Continue, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax NextStatement(
        [AwkT(AwkTokenKind.Next)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Next, ToToken(AwkTokenKind.Next, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax NextFileStatement(
        [AwkT(AwkTokenKind.NextFile)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.NextFile, ToToken(AwkTokenKind.NextFile, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax ExitStatement(
        [AwkT(AwkTokenKind.Exit)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Exit, ToToken(AwkTokenKind.Exit, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax ReturnStatement(
        [AwkT(AwkTokenKind.Return)] Pos<string> keyword,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Return, ToToken(AwkTokenKind.Return, keyword), tails);
    }

    [Rule]
    public AwkStatementSyntax IdentifierStartedStatement(
        [AwkT(AwkTokenKind.Identifier)] Pos<string> identifier,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        var identifierToken = ToToken(AwkTokenKind.Identifier, identifier);
        var kind = AwkStatementSyntaxFacts.TryResolveIdentifier(identifierToken.Text, out var resolvedKind)
            ? resolvedKind
            : AwkStatementKind.Expression;
        return CreateSimpleStatement(kind, identifierToken, tails);
    }

    [Rule]
    public AwkStatementSyntax ExpressionStartedStatement(
        [NT] ExpressionStart first,
        [AwkL(Min = 0)] SimpleTail[] tails)
    {
        _checkpoint?.Invoke();
        return CreateSimpleStatement(AwkStatementKind.Expression, first.Tokens, tails);
    }

    [Rule]
    public ExpressionStart ExpressionStartParenthesized([NT] ParenthesizedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public ExpressionStart ExpressionStartBracketed([NT] BracketedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public ExpressionStart ExpressionStartLeaf([AwkStatementExpressionStartLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    [Rule]
    public SimpleTail SimpleTailElement([NT] SimpleElement element)
    {
        _checkpoint?.Invoke();
        return new(element.Tokens);
    }

    [Rule]
    public SimpleTail SimpleTailComment([AwkT(AwkTokenKind.Comment)] Pos<string> comment)
    {
        _checkpoint?.Invoke();
        return new([ToToken(AwkTokenKind.Comment, comment)]);
    }

    [Rule]
    public SimpleTail SimpleContinuation(
        [AwkT(
            AwkTokenKind.Comma,
            AwkTokenKind.Question,
            AwkTokenKind.Colon,
            AwkTokenKind.Plus,
            AwkTokenKind.Minus,
            AwkTokenKind.Star,
            AwkTokenKind.Slash,
            AwkTokenKind.Percent,
            AwkTokenKind.Caret,
            AwkTokenKind.Tilde,
            AwkTokenKind.BangTilde,
            AwkTokenKind.Equal,
            AwkTokenKind.PlusEqual,
            AwkTokenKind.MinusEqual,
            AwkTokenKind.StarEqual,
            AwkTokenKind.SlashEqual,
            AwkTokenKind.PercentEqual,
            AwkTokenKind.CaretEqual,
            AwkTokenKind.EqualEqual,
            AwkTokenKind.BangEqual,
            AwkTokenKind.Less,
            AwkTokenKind.LessEqual,
            AwkTokenKind.Greater,
            AwkTokenKind.GreaterEqual,
            AwkTokenKind.AppendRedirect,
            AwkTokenKind.Pipe,
            AwkTokenKind.PipeAmpersand,
            AwkTokenKind.AmpersandAmpersand,
            AwkTokenKind.PipePipe)] Pos<string> continuation,
        [AwkL(Min = 1)] ContinuationSeparator[] separators,
        [NT] SimpleElement element)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, continuation, checkpoint: _checkpoint), .. FlattenSeparators(separators), .. element.Tokens]);
        IReadOnlyList<AwkToken> FlattenSeparators(IEnumerable<ContinuationSeparator> separators) =>
            Flatten(separators.Select(separator => separator.Tokens));
    }
    [Rule]
    public SimpleElement SimpleParenthesized([NT] ParenthesizedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public SimpleElement SimpleBracketed([NT] BracketedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public SimpleElement SimpleLeaf([AwkStatementSimpleLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    [Rule]
    public MalformedControlPart MalformedControlLeaf([AwkStatementMalformedControlLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    [Rule]
    public ParenthesizedGroup ParenthesizedGroupRule(
        [AwkT(AwkTokenKind.LeftParen)] Pos<string> open,
        [AwkL(Min = 0)] GroupPart[] parts,
        [AwkT(AwkTokenKind.RightParen)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var openToken = ToToken(AwkTokenKind.LeftParen, open);
        var closeToken = ToToken(AwkTokenKind.RightParen, close);
        var inner = Flatten(parts.Select(part => part.Tokens));
        return new ParenthesizedGroup(openToken, closeToken, inner);
    }

    [Rule]
    public BracketedGroup BracketedGroupRule(
        [AwkT(AwkTokenKind.LeftBracket)] Pos<string> open,
        [AwkL(Min = 0)] GroupPart[] parts,
        [AwkT(AwkTokenKind.RightBracket)] Pos<string> close)
    {
        _checkpoint?.Invoke();
        var openToken = ToToken(AwkTokenKind.LeftBracket, open);
        var closeToken = ToToken(AwkTokenKind.RightBracket, close);
        var inner = Flatten(parts.Select(part => part.Tokens));
        return new BracketedGroup(openToken, closeToken, inner);
    }

    [Rule]
    public GroupPart NestedParenthesizedGroup([NT] ParenthesizedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public GroupPart NestedBracketedGroup([NT] BracketedGroup group)
    {
        _checkpoint?.Invoke();
        return new(group.FullTokens);
    }

    [Rule]
    public GroupPart GroupLeaf([AwkStatementGroupLeaf] Pos<string> token)
    {
        _checkpoint?.Invoke();
        return new([AwkParsingSupport.RestoreToken(_originalTokens, token, checkpoint: _checkpoint)]);
    }

    private AwkStatementSyntax CreateSimpleStatement(
        AwkStatementKind kind,
        AwkToken firstToken,
        IReadOnlyList<SimpleTail> tails) =>
        CreateSimpleStatement(kind, [firstToken], tails);

    private AwkStatementSyntax CreateSimpleStatement(
        AwkStatementKind kind,
        IReadOnlyList<AwkToken> firstTokens,
        IReadOnlyList<SimpleTail> tails)
    {
        var tokens = Flatten([firstTokens, Flatten(tails.Select(tail => tail.Tokens))]);
        var outputParts = ResolveOutputStatementParts(tokens, kind);
        var getline = ResolveGetlineSyntax(tokens, kind);
        var inputPipeGetline = ResolveInputPipeGetlineSyntax(tokens, kind);
        var expressions = ResolveStatementExpressions(tokens, kind, outputParts);
        return CreateStatement(
            kind,
            CopyStatementTokens(tokens),
            expressions,
            [],
            [],
            [],
            new AwkStatementSyntaxOptions
            {
                OutputPayload = outputParts.Payload,
                OutputRedirection = outputParts.Redirection,
                Getline = getline,
                InputPipeGetline = inputPipeGetline,
                Diagnostics = ResolveStatementDiagnostics(kind, tokens, expressions, getline)
            });

        IReadOnlyList<AwkExpressionSyntax> ResolveStatementExpressions(
            IReadOnlyList<AwkToken> tokens,
            AwkStatementKind kind,
            OutputStatementParts outputParts)
        {
            if (kind == AwkStatementKind.Expression)
                return ExpressionListFromParsed(ParseExpression(tokens));

            if (kind is AwkStatementKind.Print or AwkStatementKind.Printf)
                return outputParts.Payload.TryGetValue(out var payload)
                    ? ExpressionListFromSlot(payload)
                    : [];

            if (kind == AwkStatementKind.Delete)
                return ExpressionListFromParsed(ParseExpressionList(AwkTokenNavigation.Slice(tokens, 1, tokens.Count - 1, checkpoint: _checkpoint)));

            if (AwkStatementSyntaxFacts.HasWholeStatementExpression(kind))
                return ExpressionListFromParsed(ParseExpression(tokens));

            if (AwkStatementKindFacts.IsOptionalValue(kind))
                return ExpressionListFromParsed(ParseExpression(AwkTokenNavigation.Slice(tokens, 1, tokens.Count - 1, checkpoint: _checkpoint)));

            return [];

            IReadOnlyList<AwkExpressionSyntax> ExpressionListFromParsed(AwkExpressionSyntax? expression) =>
                expression == null ? [] : [expression];

            IReadOnlyList<AwkExpressionSyntax> ExpressionListFromSlot(AwkExpressionSlotSyntax slot) =>
                slot.Expression.TryGetValue(out var expression) ? [expression] : [];
        }

        IReadOnlyList<AwkDiagnostic> ResolveStatementDiagnostics(
            AwkStatementKind kind,
            IReadOnlyList<AwkToken> tokens,
            IReadOnlyList<AwkExpressionSyntax> expressions,
            AwkSyntaxOptional<AwkGetlineSyntax> getline)
        {
            var diagnostic = ResolveStatementDiagnostic(kind, tokens, expressions, getline);
            return diagnostic.HasValue
                ? [new AwkDiagnostic(diagnostic.Value.Token.Span, diagnostic.Value.Message)]
                : [];

            BoundaryDiagnostic? ResolveStatementDiagnostic(
                AwkStatementKind kind,
                IReadOnlyList<AwkToken> tokens,
                IReadOnlyList<AwkExpressionSyntax> expressions,
                AwkSyntaxOptional<AwkGetlineSyntax> getline)
            {
                if (AwkStatementValidationFacts.IsArgumentless(kind))
                {
                    var trailing = AwkTokenNavigation.FindNextSignificant(tokens, 1, checkpoint: _checkpoint);
                    return trailing == null
                        ? null
                        : new BoundaryDiagnostic(trailing.Value, AwkStatementValidationFacts.ArgumentlessDiagnostic(kind));
                }

                if (kind == AwkStatementKind.Delete)
                    return FirstExpression(expressions).TryGetValue(out var deleteExpression) &&
                        FindUnexpectedSimpleTargetTrailingToken(deleteExpression) is { } deleteTrailing
                            ? new BoundaryDiagnostic(deleteTrailing, "Unexpected token after delete target.")
                            : null;

                if (kind == AwkStatementKind.Getline)
                    return getline.TryGetValue(out var getlineSyntax) &&
                        getlineSyntax.Target.Expression.TryGetValue(out var getlineTarget) &&
                        FindUnexpectedSimpleTargetTrailingToken(getlineTarget) is { } getlineTrailing
                            ? new BoundaryDiagnostic(getlineTrailing, "Unexpected token after getline target.")
                            : null;

                if (AwkStatementSyntaxFacts.RequiresCallBoundary(kind) &&
                    FirstExpression(expressions).TryGetValue(out var callExpression) &&
                    FindUnexpectedCallTrailingToken(callExpression, tokens[0].Text) is { } callTrailing)
                {
                    return new BoundaryDiagnostic(callTrailing, "Unexpected token after " + tokens[0].Text + " statement.");
                }

                return null;

                AwkSyntaxOptional<AwkExpressionSyntax> FirstExpression(IReadOnlyList<AwkExpressionSyntax> expressions) =>
                    expressions.Count == 0
                        ? AwkSyntaxOptional<AwkExpressionSyntax>.None
                        : AwkSyntaxOptional<AwkExpressionSyntax>.Some(expressions[0]);

                AwkToken? FindUnexpectedSimpleTargetTrailingToken(AwkExpressionSyntax expression)
                {
                    if (IsSimpleTargetExpression(expression))
                        return null;

                    if (expression.Kind == AwkExpressionKind.ExpressionList)
                        return null;

                    if (expression.Kind == AwkExpressionKind.Concatenation &&
                        expression.Children.Count > 1 &&
                        IsSimpleTargetExpression(expression.Children[0]))
                    {
                        return FirstToken(expression.Children[1]);
                    }

                    if (expression.Kind == AwkExpressionKind.Binary &&
                        expression.Children.Count > 0 &&
                        IsSimpleTargetExpression(expression.Children[0]) &&
                        expression.OperatorTokens.Count > 0)
                    {
                        return expression.OperatorTokens[0];
                    }

                    return null;

                    bool IsSimpleTargetExpression(AwkExpressionSyntax expression) =>
                        expression.Kind is AwkExpressionKind.Variable or
                            AwkExpressionKind.ArrayElement or
                            AwkExpressionKind.Field or
                            AwkExpressionKind.Group;
                }

                AwkToken? FindUnexpectedCallTrailingToken(AwkExpressionSyntax expression, string name)
                {
                    if (IsCallExpression(expression, name))
                        return null;

                    if (expression.Kind == AwkExpressionKind.Concatenation &&
                        expression.Children.Count > 1 &&
                        IsCallExpression(expression.Children[0], name))
                    {
                        return FirstToken(expression.Children[1]);
                    }

                    return null;

                    bool IsCallExpression(AwkExpressionSyntax expression, string name) =>
                        AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) &&
                        string.Equals(target.Name, name, StringComparison.Ordinal);
                }
            }
        }

        OutputStatementParts ResolveOutputStatementParts(
            IReadOnlyList<AwkToken> tokens,
            AwkStatementKind kind)
        {
            if (kind is not (AwkStatementKind.Print or AwkStatementKind.Printf))
                return default;

            var payloadTokens = AwkTokenNavigation.Slice(tokens, startIndex: 1, count: tokens.Count - 1, checkpoint: _checkpoint);
            var redirectionIndex = FindTopLevelOutputRedirection(payloadTokens, out var redirectionKind);
            var expressionTokens = redirectionIndex < 0
                ? payloadTokens
                : AwkTokenNavigation.Slice(payloadTokens, startIndex: 0, count: redirectionIndex, checkpoint: _checkpoint);
            var payload = new AwkExpressionSlotSyntax(
                expressionTokens,
                AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpressionList(expressionTokens)));
            if (redirectionIndex < 0)
                return new OutputStatementParts(
                    payload,
                    AwkSyntaxOptional<AwkOutputRedirectionSyntax>.None);

            var targetTokens = AwkTokenNavigation.Slice(
                payloadTokens,
                redirectionIndex + 1,
                payloadTokens.Count - redirectionIndex - 1, checkpoint: _checkpoint);
            var redirection = new AwkOutputRedirectionSyntax(
                redirectionKind,
                new AwkExpressionSlotSyntax(
                    targetTokens,
                    AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpression(targetTokens))));
            return new OutputStatementParts(payload, redirection);

            int FindTopLevelOutputRedirection(
                IReadOnlyList<AwkToken> tokens,
                out AwkOutputRedirectionKind kind)
            {
                var parenDepth = 0;
                var bracketDepth = 0;
                var braceDepth = 0;
                for (var index = 0; index < tokens.Count; index++)
                {
                    _checkpoint?.Invoke();
                    if (parenDepth == 0 &&
                        bracketDepth == 0 &&
                        braceDepth == 0 &&
                        TryResolveOutputRedirectionKind(tokens[index].Kind, out kind))
                    {
                        return index;
                    }

                    AwkTokenNavigation.UpdateDepth(tokens[index].Kind, ref parenDepth, ref bracketDepth, ref braceDepth);
                }

                kind = AwkOutputRedirectionKind.Write;
                return -1;

                bool TryResolveOutputRedirectionKind(
                    AwkTokenKind tokenKind,
                    out AwkOutputRedirectionKind kind)
                {
                    kind = tokenKind switch
                    {
                        AwkTokenKind.AppendRedirect => AwkOutputRedirectionKind.Append,
                        AwkTokenKind.Pipe => AwkOutputRedirectionKind.Pipe,
                        AwkTokenKind.PipeAmpersand => AwkOutputRedirectionKind.Coprocess,
                        AwkTokenKind.Greater => AwkOutputRedirectionKind.Write,
                        _ => (AwkOutputRedirectionKind)(-1)
                    };
                    return kind != (AwkOutputRedirectionKind)(-1);
                }
            }
        }

        AwkSyntaxOptional<AwkGetlineSyntax> ResolveGetlineSyntax(
            IReadOnlyList<AwkToken> tokens,
            AwkStatementKind kind)
        {
            if (kind != AwkStatementKind.Getline)
                return AwkSyntaxOptional<AwkGetlineSyntax>.None;

            var payloadTokens = AwkTokenNavigation.Slice(tokens, startIndex: 1, count: tokens.Count - 1, checkpoint: _checkpoint);
            var redirectionIndex = AwkTokenNavigation.FindTopLevel(
                payloadTokens,
                AwkTokenKind.Less,
                0,
                payloadTokens.Count, checkpoint: _checkpoint);
            var targetTokens = redirectionIndex < 0
                ? payloadTokens
                : AwkTokenNavigation.Slice(payloadTokens, startIndex: 0, count: redirectionIndex, checkpoint: _checkpoint);
            var sourceTokens = redirectionIndex < 0
                ? []
                : AwkTokenNavigation.Slice(payloadTokens, redirectionIndex + 1, payloadTokens.Count - redirectionIndex - 1, checkpoint: _checkpoint);

            return new AwkGetlineSyntax(
                new AwkExpressionSlotSyntax(
                    targetTokens,
                    AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpression(targetTokens))),
                new AwkExpressionSlotSyntax(
                    sourceTokens,
                    AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpression(sourceTokens))),
                redirectionIndex >= 0);
        }

        AwkSyntaxOptional<AwkInputPipeGetlineSyntax> ResolveInputPipeGetlineSyntax(
            IReadOnlyList<AwkToken> tokens,
            AwkStatementKind kind)
        {
            int FindFirstTopLevelPipe(IReadOnlyList<AwkToken> source)
            {
                source = AwkTokenNavigation.ForIndexedAccess(source, _checkpoint);
                var parenDepth = 0;
                var bracketDepth = 0;
                var braceDepth = 0;
                for (var index = 0; index < source.Count; index++)
                {
                    _checkpoint?.Invoke();
                    var tokenKind = source[index].Kind;
                    if (tokenKind is (AwkTokenKind.Pipe or AwkTokenKind.PipeAmpersand) &&
                        parenDepth == 0 &&
                        bracketDepth == 0 &&
                        braceDepth == 0)
                    {
                        return index;
                    }

                    AwkTokenNavigation.UpdateDepth(
                        tokenKind,
                        ref parenDepth,
                        ref bracketDepth,
                        ref braceDepth);
                }

                return -1;
            }

            if (kind != AwkStatementKind.Expression)
                return AwkSyntaxOptional<AwkInputPipeGetlineSyntax>.None;

            var pipeIndex = FindFirstTopLevelPipe(tokens);
            if (pipeIndex < 0)
                return AwkSyntaxOptional<AwkInputPipeGetlineSyntax>.None;

            // Assignments and status operators surrounding pipe getline belong to
            // the expression grammar. Retain the statement outline only for a
            // complete standalone read (and the explicit unsupported coprocess).
            if (tokens[pipeIndex].Kind == AwkTokenKind.Pipe &&
                ParseExpression(tokens) is { } expression &&
                (expression.Kind != AwkExpressionKind.FunctionCall ||
                 expression.OperatorTokens.Count != 1 ||
                 expression.OperatorTokens[0].Kind != AwkTokenKind.Pipe))
            {
                return AwkSyntaxOptional<AwkInputPipeGetlineSyntax>.None;
            }

            var rightIndex = AwkTokenNavigation.SkipNonSignificant(tokens, pipeIndex + 1, tokens.Count, checkpoint: _checkpoint);
            if (rightIndex >= tokens.Count ||
                tokens[rightIndex].Kind is not (AwkTokenKind.Identifier or AwkTokenKind.Getline) ||
                !AwkBuiltins.IsName(tokens[rightIndex].Text, AwkBuiltinId.Getline))
            {
                return AwkSyntaxOptional<AwkInputPipeGetlineSyntax>.None;
            }

            var commandTokens = AwkTokenNavigation.Slice(tokens, 0, pipeIndex, checkpoint: _checkpoint);
            var targetStart = AwkTokenNavigation.SkipNonSignificant(tokens, rightIndex + 1, tokens.Count, checkpoint: _checkpoint);
            var targetTokens = targetStart >= tokens.Count
                ? []
                : AwkTokenNavigation.Slice(tokens, targetStart, tokens.Count - targetStart, checkpoint: _checkpoint);

            return new AwkInputPipeGetlineSyntax(
                tokens[pipeIndex],
                new AwkExpressionSlotSyntax(
                    commandTokens,
                    AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpression(commandTokens))),
                new AwkExpressionSlotSyntax(
                    targetTokens,
                    AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpression(targetTokens))));
        }

        List<AwkToken> CopyStatementTokens(IReadOnlyList<AwkToken> tokens)
        {
            var result = new List<AwkToken>(tokens.Count);
            for (var index = 0; index < tokens.Count; index++)
            {
                _checkpoint?.Invoke();
                var token = tokens[index];
                if (token.Kind is not AwkTokenKind.NewLine)
                    result.Add(token);
            }

            return result;
        }
    }

    private AwkStatementSyntax CreateStatement(
        AwkStatementKind kind,
        IReadOnlyList<AwkToken> tokens,
        IReadOnlyList<AwkExpressionSyntax> expressions,
        IReadOnlyList<AwkStatementSyntax> bodyStatements,
        IReadOnlyList<AwkStatementSyntax> elseStatements,
        IReadOnlyList<AwkSwitchCaseSyntax> switchCases) =>
        CreateStatement(
            kind,
            tokens,
            expressions,
            bodyStatements,
            elseStatements,
            switchCases,
            AwkStatementSyntaxOptions.Empty);

    private AwkStatementSyntax CreateStatement(
        AwkStatementKind kind,
        IReadOnlyList<AwkToken> tokens,
        IReadOnlyList<AwkExpressionSyntax> expressions,
        IReadOnlyList<AwkStatementSyntax> bodyStatements,
        IReadOnlyList<AwkStatementSyntax> elseStatements,
        IReadOnlyList<AwkSwitchCaseSyntax> switchCases,
        AwkStatementSyntaxOptions options)
    {
        _checkpoint?.Invoke();
        var first = FindFirstSignificant(tokens);
        var last = FindLastSignificant(tokens);
        var lValue = AwkSyntaxOptional<AwkLValueSyntax>.FromNullable(
            ResolveLValue(expressions.Count == 0 ? null : expressions[0]));
        var payload = CreatePayload(
            kind,
            lValue,
            expressions,
            bodyStatements,
            elseStatements,
            switchCases,
            options.ForHeader,
            options.Header,
            options.OutputPayload,
            options.OutputRedirection,
            options.Getline,
            options.InputPipeGetline,
            options.HasUnbracedBody);
        if (first == null || last == null)
        {
            return new AwkStatementSyntax(
                kind,
                tokens,
                payload,
                new AwkTextSpan(0, 0),
                diagnostics: options.Diagnostics);
        }

        return new AwkStatementSyntax(
            kind,
            tokens,
            payload,
            new AwkTextSpan(first.Value.Span.Start, last.Value.Span.End - first.Value.Span.Start),
            diagnostics: options.Diagnostics);

        AwkStatementSyntaxPayload CreatePayload(
            AwkStatementKind kind,
            AwkSyntaxOptional<AwkLValueSyntax> lValue,
            IReadOnlyList<AwkExpressionSyntax> expressions,
            IReadOnlyList<AwkStatementSyntax> bodyStatements,
            IReadOnlyList<AwkStatementSyntax> elseStatements,
            IReadOnlyList<AwkSwitchCaseSyntax> switchCases,
            AwkSyntaxOptional<AwkForHeaderSyntax> forHeader,
            AwkSyntaxOptional<AwkExpressionSlotSyntax> header,
            AwkSyntaxOptional<AwkExpressionSlotSyntax> outputPayload,
            AwkSyntaxOptional<AwkOutputRedirectionSyntax> outputRedirection,
            AwkSyntaxOptional<AwkGetlineSyntax> getline,
            AwkSyntaxOptional<AwkInputPipeGetlineSyntax> inputPipeGetline,
            bool hasUnbracedBody)
        {
            if (kind is AwkStatementKind.Block)
                return new AwkBlockStatementSyntaxPayload(bodyStatements);

            if (kind is AwkStatementKind.If or AwkStatementKind.While or AwkStatementKind.DoWhile)
            {
                return new AwkControlStatementSyntaxPayload(
                    header.GetValueOr(AwkExpressionSlotSyntax.Empty),
                    expressions,
                    bodyStatements,
                    elseStatements,
                    hasUnbracedBody);
            }

            if (kind == AwkStatementKind.For)
            {
                return new AwkForStatementSyntaxPayload(
                    forHeader.GetValueOr(AwkForHeaderSyntax.Empty),
                    expressions,
                    bodyStatements,
                    hasUnbracedBody);
            }

            if (kind == AwkStatementKind.Switch)
            {
                return new AwkSwitchStatementSyntaxPayload(
                    header.GetValueOr(AwkExpressionSlotSyntax.Empty),
                    expressions,
                    switchCases);
            }

            if (kind is AwkStatementKind.Print or AwkStatementKind.Printf)
            {
                return new AwkOutputStatementSyntaxPayload(
                    outputPayload.GetValueOr(AwkExpressionSlotSyntax.Empty),
                    outputRedirection,
                    expressions);
            }

            if (kind == AwkStatementKind.Getline &&
                getline.TryGetValue(out var getlineSyntax))
            {
                return new AwkGetlineStatementSyntaxPayload(getlineSyntax, expressions);
            }

            if (kind == AwkStatementKind.Expression &&
                inputPipeGetline.TryGetValue(out var inputPipeSyntax))
            {
                return new AwkInputPipeExpressionStatementSyntaxPayload(inputPipeSyntax, lValue, expressions);
            }

            return new AwkSimpleStatementSyntaxPayload(lValue, expressions);
        }

        AwkLValueSyntax? ResolveLValue(AwkExpressionSyntax? expression)
        {
            if (expression == null)
                return null;

            var target = expression.Kind switch
            {
                AwkExpressionKind.Assignment when expression.Children.Count > 0 => expression.Children[0],
                AwkExpressionKind.Postfix when expression.Children.Count > 0 => expression.Children[0],
                AwkExpressionKind.Unary when expression.Children.Count > 0 &&
                    expression.OperatorTokens.Count > 0 &&
                    expression.OperatorTokens[0].Kind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus => expression.Children[0],
                _ => null
            };
            if (target == null)
                return null;

            var kind = target.Kind switch
            {
                AwkExpressionKind.Variable => AwkLValueKind.Variable,
                AwkExpressionKind.ArrayElement => AwkLValueKind.ArrayElement,
                AwkExpressionKind.Field => AwkLValueKind.Field,
                _ => (AwkLValueKind?)null
            };
            if (kind == null || target.Span.Length == 0)
                return null;

            return new AwkLValueSyntax(kind.Value, target.Span);
        }
    }

    private sealed class AwkStatementSyntaxOptions
    {
        public static AwkStatementSyntaxOptions Empty { get; } = new();

        public AwkSyntaxOptional<AwkForHeaderSyntax> ForHeader { get; init; }

        public AwkSyntaxOptional<AwkExpressionSlotSyntax> Header { get; init; }

        public AwkSyntaxOptional<AwkExpressionSlotSyntax> OutputPayload { get; init; }

        public AwkSyntaxOptional<AwkOutputRedirectionSyntax> OutputRedirection { get; init; }

        public AwkSyntaxOptional<AwkGetlineSyntax> Getline { get; init; }

        public AwkSyntaxOptional<AwkInputPipeGetlineSyntax> InputPipeGetline { get; init; }

        public bool HasUnbracedBody { get; init; }

        public IReadOnlyList<AwkDiagnostic> Diagnostics { get; init; } = [];
    }

    private AwkToken? FirstToken(AwkExpressionSyntax expression)
    {
        var tokens = expression.GetTokens(_checkpoint);
        return tokens.Count == 0 ? null : tokens[0];
    }

    private AwkExpressionSlotSyntax ParseExpressionSlot(IReadOnlyList<AwkToken> tokens) =>
        new(tokens, AwkSyntaxOptional<AwkExpressionSyntax>.FromNullable(ParseExpression(tokens)));

    private List<AwkExpressionSyntax> ExpressionsFromSlots(
        IReadOnlyList<AwkExpressionSlotSyntax> slots)
    {
        var expressions = new List<AwkExpressionSyntax>();
        for (var index = 0; index < slots.Count; index++)
        {
            _checkpoint?.Invoke();
            if (slots[index].Expression.TryGetValue(out var expression))
                expressions.Add(expression);
        }

        return expressions;
    }

    private bool IsUnbracedBody(StatementBody body) =>
        body.Statements.Count == 1 &&
        FindFirstSignificant(body.Tokens)?.Kind != AwkTokenKind.LeftBrace;

    private List<AwkStatementSyntax> StatementsFrom(StatementListItem[] items)
    {
        var statements = new List<AwkStatementSyntax>();
        var pendingSeparators = new List<AwkToken>();
        for (var index = 0; index < items.Length; index++)
        {
            _checkpoint?.Invoke();
            var item = items[index];
            if (item.Statement != null)
            {
                statements.Add(item.Statement);
                pendingSeparators.Clear();
                continue;
            }

            if (item.ElseContinuation != null)
            {
                if (statements.Count > 0 &&
                    TryAttachElse(statements[^1], pendingSeparators, item.ElseContinuation.Value, out var attached))
                {
                    statements[^1] = attached;
                    pendingSeparators.Clear();
                    continue;
                }

                statements.Add(CreateStatement(
                    AwkStatementKind.Unknown,
                    Flatten([pendingSeparators, item.ElseContinuation.Value.Tokens]),
                    [],
                    [],
                    [],
                    []));
                pendingSeparators.Clear();
                continue;
            }

            foreach (var token in AwkTokenNavigation.Enumerate(item.Tokens, _checkpoint))
                pendingSeparators.Add(token);
        }

        return statements;
    }

    private bool TryAttachElse(
        AwkStatementSyntax statement,
        IReadOnlyList<AwkToken> separatorTokens,
        ElseContinuation continuation,
        out AwkStatementSyntax attached)
    {
        attached = statement;
        AwkStatementSyntaxPayload payload;
        if (statement.Payload is AwkControlStatementSyntaxPayload control)
        {
            if (statement.Kind is AwkStatementKind.If && control.HasElse)
            {
                if (!control.HasUnbracedElseBody || control.ElseStatements.Count != 1 ||
                    !TryAttachElse(control.ElseStatements[0], separatorTokens, continuation, out var child))
                {
                    return false;
                }

                payload = control.WithElse([child], hasUnbracedElseBody: true);
            }
            else if (control.HasUnbracedBody && control.BodyStatements.Count == 1 &&
                TryAttachElse(control.BodyStatements[0], separatorTokens, continuation, out var child))
            {
                payload = control.WithBody([child]);
            }
            else if (statement.Kind is AwkStatementKind.If)
            {
                if (!CanAttachElse(statement, separatorTokens))
                    return false;
                payload = control.WithElse(continuation.Statements, continuation.HasUnbracedBody);
            }
            else
            {
                return false;
            }
        }
        else if (statement.Payload is AwkForStatementSyntaxPayload loop && loop.HasUnbracedBody &&
            loop.BodyStatements.Count == 1 &&
            TryAttachElse(loop.BodyStatements[0], separatorTokens, continuation, out var child))
        {
            payload = loop.WithBody([child]);
        }
        else
        {
            return false;
        }

        var tokens = Flatten([statement.Tokens, separatorTokens, continuation.Tokens]);
        _checkpoint?.Invoke();
        var first = FindFirstSignificant(tokens);
        var last = FindLastSignificant(tokens);
        attached = statement.WithPayload(tokens, payload,
            first != null && last != null
                ? new AwkTextSpan(first.Value.Span.Start, last.Value.Span.End - first.Value.Span.Start)
                : statement.Span);
        return true;
    }

    private bool CanAttachElse(AwkStatementSyntax statement, IReadOnlyList<AwkToken> separators)
    {
        var statementTokens = AwkTokenNavigation.ForIndexedAccess(statement.Tokens, _checkpoint);
        separators = AwkTokenNavigation.ForIndexedAccess(separators, _checkpoint);
        var last = statementTokens.Count - 1;
        while (last >= 0 && statementTokens[last].Kind is AwkTokenKind.NewLine or AwkTokenKind.Comment)
        {
            _checkpoint?.Invoke();
            last--;
        }
        var terminated = last >= 0 && statementTokens[last].Kind is AwkTokenKind.RightBrace or AwkTokenKind.Semicolon;
        for (var index = 0; index < separators.Count; index++)
        {
            _checkpoint?.Invoke();
            var kind = separators[index].Kind;
            if (kind == AwkTokenKind.Semicolon)
            {
                if (terminated)
                    return false;
                terminated = true;
            }
            else if (kind == AwkTokenKind.NewLine)
            {
                terminated = true;
            }
        }
        return terminated;
    }

    private void AddStatementDiagnostics(
        IReadOnlyList<AwkStatementSyntax> statements,
        List<AwkDiagnostic> diagnostics)
    {
        for (var index = 0; index < statements.Count; index++)
        {
            _checkpoint?.Invoke();
            AddNestedStatementDiagnostics(statements[index], diagnostics);
        }

        void AddNestedStatementDiagnostics(
            AwkStatementSyntax statement,
            List<AwkDiagnostic> diagnostics)
        {
            for (var index = 0; index < statement.Diagnostics.Count; index++)
            {
                _checkpoint?.Invoke();
                diagnostics.Add(statement.Diagnostics[index]);
            }

            foreach (var expression in statement.EnumerateChildExpressions())
            {
                _checkpoint?.Invoke();
                if (expression.Kind == AwkExpressionKind.Unknown)
                    diagnostics.Add(AwkExpressionSyntaxParser.CreateSyntaxDiagnostic(expression));
            }

            AddStatementDiagnostics(statement.BodyStatements, diagnostics);
            AddStatementDiagnostics(statement.ElseStatements, diagnostics);
            for (var index = 0; index < statement.SwitchCases.Count; index++)
            {
                _checkpoint?.Invoke();
                AddStatementDiagnostics(statement.SwitchCases[index].BodyStatements, diagnostics);
            }
        }
    }

    private AwkToken? FindFirstSignificant(IReadOnlyList<AwkToken> tokens) =>
        AwkTokenNavigation.FindFirstSignificant(tokens, checkpoint: _checkpoint);

    private AwkToken? FindLastSignificant(IReadOnlyList<AwkToken> tokens) =>
        AwkTokenNavigation.FindLastSignificant(tokens, checkpoint: _checkpoint);

    private IReadOnlyList<AwkToken> Flatten(IEnumerable<IReadOnlyList<AwkToken>> groups) =>
        AwkTokenNavigation.Flatten(groups, checkpoint: _checkpoint);

    private IReadOnlyList<AwkToken> Flatten(IEnumerable<HeaderGap> gaps) =>
        Flatten(gaps.Select(gap => gap.Tokens));

    private IReadOnlyList<AwkToken> Flatten(IEnumerable<StatementSeparator> separators) =>
        Flatten(separators.Select(separator => separator.Tokens));

    private AwkExpressionSyntax? ParseExpression(IReadOnlyList<AwkToken> tokens) =>
        _source == null
            ? AwkExpressionSyntaxParser.Parse(tokens, _checkpoint)
            : AwkExpressionSyntaxParser.Parse(_source, tokens, _checkpoint);

    private AwkExpressionSyntax? ParseExpressionList(IReadOnlyList<AwkToken> tokens) =>
        _source == null
            ? AwkExpressionSyntaxParser.ParseList(tokens, _checkpoint)
            : AwkExpressionSyntaxParser.ParseList(_source, tokens, _checkpoint);

    private AwkToken ToToken(AwkTokenKind kind, Pos<string> token) =>
        AwkParsingSupport.ToToken(kind, token, checkpoint: _checkpoint);

    internal sealed class ParseResult(IReadOnlyList<AwkStatementSyntax> statements)
    {
        public IReadOnlyList<AwkStatementSyntax> Statements { get; } = statements;
    }

    private abstract class AwkStatementParseResult
    {
        public static Invalid InvalidResult { get; } = new();

        public static Parsed Success(
            IReadOnlyList<AwkStatementSyntax> statements,
            IReadOnlyList<AwkDiagnostic> diagnostics) =>
            new(statements, diagnostics);

        public sealed class Parsed(
            IReadOnlyList<AwkStatementSyntax> statements,
            IReadOnlyList<AwkDiagnostic> diagnostics) : AwkStatementParseResult
        {
            public IReadOnlyList<AwkStatementSyntax> Statements { get; } = statements;

            public IReadOnlyList<AwkDiagnostic> Diagnostics { get; } = diagnostics;
        }

        public sealed class Invalid : AwkStatementParseResult
        {
        }
    }

    public readonly struct StatementListItem(
        AwkStatementSyntax? statement,
        IReadOnlyList<AwkToken>? tokens,
        ElseContinuation? elseContinuation)
    {
        public AwkStatementSyntax? Statement { get; } = statement;

        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];

        public ElseContinuation? ElseContinuation { get; } = elseContinuation;
    }

    public readonly struct StatementSeparator(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct HeaderGap(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct ContinuationSeparator(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct StatementBody(
        IReadOnlyList<AwkToken>? tokens,
        IReadOnlyList<AwkStatementSyntax>? statements)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];

        public IReadOnlyList<AwkStatementSyntax> Statements => statements ?? [];
    }

    public readonly struct ElseIntro(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct ElseContinuation(
        IReadOnlyList<AwkToken>? tokens,
        IReadOnlyList<AwkStatementSyntax>? statements,
        bool hasUnbracedBody)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];

        public IReadOnlyList<AwkStatementSyntax> Statements => statements ?? [];

        public bool HasUnbracedBody { get; } = hasUnbracedBody;
    }

    public readonly struct SwitchBlock(
        IReadOnlyList<AwkToken>? tokens,
        IReadOnlyList<AwkSwitchCaseSyntax>? cases)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];

        public IReadOnlyList<AwkSwitchCaseSyntax> Cases => cases ?? [];
    }

    public readonly struct SwitchBlockItem(AwkSwitchCaseSyntax? switchCase, IReadOnlyList<AwkToken>? tokens)
    {
        public AwkSwitchCaseSyntax? Case { get; } = switchCase;

        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct SwitchCasePart(AwkSwitchCaseSyntax? syntax, IReadOnlyList<AwkToken>? tokens)
    {
        public AwkSwitchCaseSyntax Syntax => syntax ??
            throw new InvalidOperationException("The default switch case has no syntax.");

        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct CaseLabelPart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct ExpressionStart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct SimpleElement(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public readonly struct SimpleTail(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    private readonly struct OutputStatementParts(
        AwkSyntaxOptional<AwkExpressionSlotSyntax> payload,
        AwkSyntaxOptional<AwkOutputRedirectionSyntax> redirection)
    {
        public AwkSyntaxOptional<AwkExpressionSlotSyntax> Payload { get; } = payload;

        public AwkSyntaxOptional<AwkOutputRedirectionSyntax> Redirection { get; } = redirection;
    }

    private readonly struct BoundaryDiagnostic(AwkToken token, string? message)
    {
        public AwkToken Token { get; } = token;

        public string Message => message ?? string.Empty;
    }

    public readonly struct MalformedControlPart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }

    public sealed class ParenthesizedGroup(
        AwkToken openToken,
        AwkToken closeToken,
        IReadOnlyList<AwkToken> innerTokens) : AwkBalancedTokenGroup(openToken, closeToken, innerTokens);

    public sealed class BracketedGroup(
        AwkToken openToken,
        AwkToken closeToken,
        IReadOnlyList<AwkToken> innerTokens) : AwkBalancedTokenGroup(openToken, closeToken, innerTokens);

    public readonly struct GroupPart(IReadOnlyList<AwkToken>? tokens)
    {
        public IReadOnlyList<AwkToken> Tokens => tokens ?? [];
    }
}
