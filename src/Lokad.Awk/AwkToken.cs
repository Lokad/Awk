using System;
using System.Globalization;
using Lokad.Parsing.Lexer;

namespace Lokad.Awk;

internal enum AwkTokenFromAccess
{
    Public,
    Private
}

internal sealed class AwkTokenFromAttribute : FromAttribute
{
    public AwkTokenFromAttribute(AwkTokenKind parent, AwkTokenFromAccess access)
        : base((int)parent, access is AwkTokenFromAccess.Private)
    {
    }
}

internal readonly struct AwkTextSpan
{
    public AwkTextSpan(int start, int length)
        : this(new AwkTextOffset(start), new AwkTextLength(length))
    {
    }

    public AwkTextSpan(AwkTextOffset start, AwkTextLength length)
    {
        StartOffset = start;
        TextLength = length;
    }

    public AwkTextOffset StartOffset { get; }

    public int Start => StartOffset.Value;

    public AwkTextLength TextLength { get; }

    public int Length => TextLength.Value;

    public int End => Start + Length;
}

internal readonly record struct AwkTextOffset
{
    public AwkTextOffset(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Text offsets must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal readonly record struct AwkTextLength
{
    public AwkTextLength(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Text lengths must be zero or positive.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal readonly struct AwkToken
{
    private readonly string? _text;
    private readonly string? _source;
    private readonly bool _sourceBacked;

    public AwkToken(AwkTokenKind kind, string text, AwkTextSpan span)
    {
        Kind = kind;
        _text = text ?? string.Empty;
        _source = string.Empty;
        _sourceBacked = false;
        Span = span;
    }

    public AwkTokenKind Kind { get; }

    public string Text =>
        !_sourceBacked
            ? _text ?? string.Empty
            : Span.Length == 0
                ? string.Empty
                : Source.Substring(Span.Start, Span.Length);

    public ReadOnlySpan<char> TextSpan =>
        !_sourceBacked
            ? (_text ?? string.Empty).AsSpan()
            : Span.Length == 0
                ? []
                : Source.AsSpan(Span.Start, Span.Length);

    public AwkTextSpan Span { get; }

    private string Source => _source ?? string.Empty;

    public static AwkToken FromSource(
        AwkTokenKind kind,
        string source,
        AwkTextSpan span) =>
        new(kind, source, span, sourceBacked: true);

    private AwkToken(AwkTokenKind kind, string source, AwkTextSpan span, bool sourceBacked)
    {
        Kind = kind;
        _text = string.Empty;
        _source = source ?? string.Empty;
        _sourceBacked = sourceBacked;
        Span = span;
    }
}

[Tokens(Comments = "(?!)")]
internal enum AwkTokenKind
{
    [End] EndOfFile,
    [Error] BadToken,
    [EndOfLine] NewLine,
    Comment,
    [Pattern("[A-Za-z_][A-Za-z0-9_]*", Start = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_")]
    Identifier,
    // Expression-parser-only: a spaced variable before '(' cannot start a user call.
    SpacedIdentifier,
    [Pattern("(?:0[xX][0-9A-Fa-f]+)|(?:(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)(?:[eE][+-]?[0-9]+)?)", Start = "0123456789.")]
    Number,
    String,
    InternationalizedString,
    Regex,
    StrongRegex,
    [Any("BEGIN"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Begin,
    [Any("END"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    End,
    [Any("BEGINFILE"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    BeginFile,
    [Any("ENDFILE"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    EndFile,
    [Any("function", "func"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Function,
    [Any("if"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    If,
    [Any("else"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Else,
    [Any("while"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    While,
    [Any("do"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Do,
    [Any("for"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    For,
    [Any("in"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    In,
    [Any("break"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Break,
    [Any("continue"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Continue,
    [Any("next"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Next,
    [Any("nextfile"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    NextFile,
    [Any("delete"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Delete,
    [Any("exit"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Exit,
    [Any("return"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Return,
    [Any("print"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Print,
    [Any("printf"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Printf,
    [Any("switch"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Switch,
    [Any("case"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Case,
    [Any("default"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Default,
    [Any("getline"), AwkTokenFrom(Identifier, AwkTokenFromAccess.Private)]
    Getline,
    [Any("{")]
    LeftBrace,
    [Any("}")]
    RightBrace,
    [Any("(")]
    LeftParen,
    [Any(")")]
    RightParen,
    [Any("[")]
    LeftBracket,
    [Any("]")]
    RightBracket,
    [Any(",")]
    Comma,
    [Any(";")]
    Semicolon,
    [Any("?")]
    Question,
    [Any(":")]
    Colon,
    [Any("::")]
    DoubleColon,
    [Any("$")]
    Dollar,
    [Any("@")]
    At,
    [Any("+")]
    Plus,
    [Any("-")]
    Minus,
    [Any("*")]
    Star,
    [Any("/")]
    Slash,
    [Any("%")]
    Percent,
    [Any("^", "**")]
    Caret,
    [Any("!")]
    Bang,
    [Any("~")]
    Tilde,
    [Any("!~")]
    BangTilde,
    [Any("=")]
    Equal,
    [Any("+=")]
    PlusEqual,
    [Any("-=")]
    MinusEqual,
    [Any("*=")]
    StarEqual,
    [Any("/=")]
    SlashEqual,
    [Any("%=")]
    PercentEqual,
    [Any("^=", "**=")]
    CaretEqual,
    [Any("==")]
    EqualEqual,
    [Any("!=")]
    BangEqual,
    [Any("<")]
    Less,
    [Any("<=")]
    LessEqual,
    [Any(">")]
    Greater,
    [Any(">=")]
    GreaterEqual,
    [Any(">>")]
    AppendRedirect,
    [Any("|")]
    Pipe,
    [Any("|&")]
    PipeAmpersand,
    [Any("&&")]
    AmpersandAmpersand,
    [Any("||")]
    PipePipe,
    [Any("++")]
    PlusPlus,
    [Any("--")]
    MinusMinus,
    // Expression-parser-only mutation tokens remove the prefix/postfix ambiguity.
    PostfixPlusPlus,
    PostfixMinusMinus
}
