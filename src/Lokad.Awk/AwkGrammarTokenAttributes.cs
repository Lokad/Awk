using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Parsing.Parser;

namespace Lokad.Awk;

internal abstract class AwkTokenSetAttribute : TerminalAttribute
{
    protected AwkTokenSetAttribute(IEnumerable<AwkTokenKind> tokens)
        : base(tokens.Select(token => (int)token))
    {
    }
}

internal sealed class AwkStatementCaseLabelLeafAttribute : AwkTokenSetAttribute
{
    public AwkStatementCaseLabelLeafAttribute()
        : base(AwkGrammarTokenSets.StatementCaseLabelLeaf)
    {
    }
}

internal sealed class AwkStatementExpressionStartLeafAttribute : AwkTokenSetAttribute
{
    public AwkStatementExpressionStartLeafAttribute()
        : base(AwkGrammarTokenSets.StatementExpressionStartLeaf)
    {
    }
}

internal sealed class AwkStatementSimpleLeafAttribute : AwkTokenSetAttribute
{
    public AwkStatementSimpleLeafAttribute()
        : base(AwkGrammarTokenSets.StatementSimpleLeaf)
    {
    }
}

internal sealed class AwkStatementMalformedControlLeafAttribute : AwkTokenSetAttribute
{
    public AwkStatementMalformedControlLeafAttribute()
        : base(AwkGrammarTokenSets.StatementMalformedControlLeaf)
    {
    }
}

internal sealed class AwkStatementGroupLeafAttribute : AwkTokenSetAttribute
{
    public AwkStatementGroupLeafAttribute()
        : base(AwkGrammarTokenSets.StatementGroupLeaf)
    {
    }
}

internal sealed class AwkTopLevelDirectiveArgumentLeafAttribute : AwkTokenSetAttribute
{
    public AwkTopLevelDirectiveArgumentLeafAttribute()
        : base(AwkGrammarTokenSets.TopLevelDirectiveArgumentLeaf)
    {
    }
}

internal sealed class AwkTopLevelBlockLeafAttribute : AwkTokenSetAttribute
{
    public AwkTopLevelBlockLeafAttribute()
        : base(AwkGrammarTokenSets.TopLevelBlockLeaf)
    {
    }
}

internal sealed class AwkTopLevelParenthesizedLeafAttribute : AwkTokenSetAttribute
{
    public AwkTopLevelParenthesizedLeafAttribute()
        : base(AwkGrammarTokenSets.TopLevelParenthesizedLeaf)
    {
    }
}

internal sealed class AwkTopLevelBracketedLeafAttribute : AwkTokenSetAttribute
{
    public AwkTopLevelBracketedLeafAttribute()
        : base(AwkGrammarTokenSets.TopLevelBracketedLeaf)
    {
    }
}

internal sealed class AwkTopLevelPatternLeafAttribute : AwkTokenSetAttribute
{
    public AwkTopLevelPatternLeafAttribute()
        : base(AwkGrammarTokenSets.TopLevelPatternLeaf)
    {
    }
}

internal static class AwkGrammarTokenSets
{
    private static readonly AwkTokenKind[] LiteralTokens =
    [
        AwkTokenKind.Number,
        AwkTokenKind.String,
        AwkTokenKind.InternationalizedString,
        AwkTokenKind.Regex,
        AwkTokenKind.StrongRegex
    ];

    private static readonly AwkTokenKind[] NamedValueTokens =
    [
        AwkTokenKind.Identifier,
        ..LiteralTokens
    ];

    private static readonly AwkTokenKind[] RuleKeywords =
    [
        AwkTokenKind.Begin,
        AwkTokenKind.End,
        AwkTokenKind.BeginFile,
        AwkTokenKind.EndFile
    ];

    private static readonly AwkTokenKind[] ControlKeywords =
    [
        AwkTokenKind.If,
        AwkTokenKind.Else,
        AwkTokenKind.While,
        AwkTokenKind.Do,
        AwkTokenKind.For,
        AwkTokenKind.In
    ];

    private static readonly AwkTokenKind[] SimpleStatementKeywords =
    [
        AwkTokenKind.Break,
        AwkTokenKind.Continue,
        AwkTokenKind.Next,
        AwkTokenKind.NextFile,
        AwkTokenKind.Delete,
        AwkTokenKind.Exit,
        AwkTokenKind.Return,
        AwkTokenKind.Print,
        AwkTokenKind.Printf,
        AwkTokenKind.Getline
    ];

    private static readonly AwkTokenKind[] SwitchKeywords =
    [
        AwkTokenKind.Switch,
        AwkTokenKind.Case,
        AwkTokenKind.Default
    ];

    private static readonly AwkTokenKind[] OperatorsWithoutColon =
    [
        AwkTokenKind.Question,
        AwkTokenKind.DoubleColon,
        AwkTokenKind.Dollar,
        AwkTokenKind.At,
        AwkTokenKind.Plus,
        AwkTokenKind.Minus,
        AwkTokenKind.Star,
        AwkTokenKind.Slash,
        AwkTokenKind.Percent,
        AwkTokenKind.Caret,
        AwkTokenKind.Bang,
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
        AwkTokenKind.PipePipe,
        AwkTokenKind.PlusPlus,
        AwkTokenKind.MinusMinus
    ];

    private static readonly AwkTokenKind[] OperatorsWithColon =
    [
        AwkTokenKind.Question,
        AwkTokenKind.Colon,
        ..OperatorsWithoutColon[1..]
    ];

    private static readonly AwkTokenKind[] FullExpressionOperators =
    [
        AwkTokenKind.Comma,
        ..OperatorsWithColon
    ];

    public static IReadOnlyList<AwkTokenKind> StatementCaseLabelLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        ..NamedValueTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        ..ControlKeywords,
        ..SimpleStatementKeywords,
        AwkTokenKind.Switch,
        ..OperatorsWithoutColon
    ]);

    public static IReadOnlyList<AwkTokenKind> StatementExpressionStartLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        ..LiteralTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        AwkTokenKind.In,
        ..FullExpressionOperators
    ]);

    public static IReadOnlyList<AwkTokenKind> StatementSimpleLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        ..NamedValueTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        AwkTokenKind.In,
        ..SimpleStatementKeywords,
        ..FullExpressionOperators
    ]);

    public static IReadOnlyList<AwkTokenKind> StatementMalformedControlLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        ..NamedValueTokens,
        AwkTokenKind.In,
        ..SimpleStatementKeywords,
        ..FullExpressionOperators
    ]);

    public static IReadOnlyList<AwkTokenKind> StatementGroupLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        AwkTokenKind.NewLine,
        AwkTokenKind.Comment,
        ..NamedValueTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        ..ControlKeywords,
        ..SimpleStatementKeywords,
        ..SwitchKeywords,
        AwkTokenKind.LeftBrace,
        AwkTokenKind.RightBrace,
        AwkTokenKind.Semicolon,
        ..FullExpressionOperators
    ]);

    public static IReadOnlyList<AwkTokenKind> TopLevelDirectiveArgumentLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        ..NamedValueTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        ..ControlKeywords,
        ..SimpleStatementKeywords,
        ..SwitchKeywords,
        AwkTokenKind.LeftBrace,
        AwkTokenKind.RightBrace,
        AwkTokenKind.LeftParen,
        AwkTokenKind.RightParen,
        AwkTokenKind.LeftBracket,
        AwkTokenKind.RightBracket,
        ..FullExpressionOperators,
        AwkTokenKind.Comment
    ]);

    public static IReadOnlyList<AwkTokenKind> TopLevelBlockLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        AwkTokenKind.NewLine,
        AwkTokenKind.Comment,
        ..NamedValueTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        ..ControlKeywords,
        ..SimpleStatementKeywords,
        ..SwitchKeywords,
        AwkTokenKind.LeftParen,
        AwkTokenKind.RightParen,
        AwkTokenKind.LeftBracket,
        AwkTokenKind.RightBracket,
        AwkTokenKind.Semicolon,
        ..FullExpressionOperators
    ]);

    public static IReadOnlyList<AwkTokenKind> TopLevelParenthesizedLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        AwkTokenKind.NewLine,
        AwkTokenKind.Comment,
        ..NamedValueTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        ..ControlKeywords,
        ..SimpleStatementKeywords,
        ..SwitchKeywords,
        AwkTokenKind.LeftBrace,
        AwkTokenKind.RightBrace,
        AwkTokenKind.LeftBracket,
        AwkTokenKind.RightBracket,
        AwkTokenKind.Semicolon,
        ..FullExpressionOperators
    ]);

    public static IReadOnlyList<AwkTokenKind> TopLevelBracketedLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        AwkTokenKind.NewLine,
        AwkTokenKind.Comment,
        ..NamedValueTokens,
        ..RuleKeywords,
        AwkTokenKind.Function,
        ..ControlKeywords,
        ..SimpleStatementKeywords,
        ..SwitchKeywords,
        AwkTokenKind.LeftBrace,
        AwkTokenKind.RightBrace,
        AwkTokenKind.LeftParen,
        AwkTokenKind.RightParen,
        AwkTokenKind.Semicolon,
        ..FullExpressionOperators
    ]);

    public static IReadOnlyList<AwkTokenKind> TopLevelPatternLeaf { get; } = Array.AsReadOnly<AwkTokenKind>(
    [
        AwkTokenKind.BadToken,
        ..NamedValueTokens,
        AwkTokenKind.If,
        AwkTokenKind.Else,
        AwkTokenKind.While,
        AwkTokenKind.Do,
        AwkTokenKind.For,
        AwkTokenKind.In,
        ..SimpleStatementKeywords,
        ..SwitchKeywords,
        AwkTokenKind.RightBrace,
        AwkTokenKind.RightParen,
        ..OperatorsWithColon
    ]);
}
