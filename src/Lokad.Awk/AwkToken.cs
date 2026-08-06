using System;
using Lokad.Parsing.Lexer;

namespace Lokad.Awk;

internal sealed class AwkTokenFromAttribute : FromAttribute
{
    public AwkTokenFromAttribute(AwkTokenKind parent)
        : this(parent, isPrivate: false)
    {
    }

    public AwkTokenFromAttribute(AwkTokenKind parent, bool isPrivate)
        : base((int)parent, isPrivate)
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

    public override string ToString() => Value.ToString();
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

    public override string ToString() => Value.ToString();
}

internal readonly struct AwkToken
{
    private readonly string _text;
    private readonly string _source;
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
                : _source.Substring(Span.Start, Span.Length);

    public ReadOnlySpan<char> TextSpan =>
        !_sourceBacked
            ? (_text ?? string.Empty).AsSpan()
            : Span.Length == 0
                ? []
                : _source.AsSpan(Span.Start, Span.Length);

    public AwkTextSpan Span { get; }

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
    [Pattern("(?:0[xX][0-9A-Fa-f]+)|(?:(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)(?:[eE][+-]?[0-9]+)?)", Start = "0123456789.")]
    Number,
    String,
    InternationalizedString,
    Regex,
    StrongRegex,
    [Any("BEGIN"), AwkTokenFrom(Identifier, true)]
    Begin,
    [Any("END"), AwkTokenFrom(Identifier, true)]
    End,
    [Any("BEGINFILE"), AwkTokenFrom(Identifier, true)]
    BeginFile,
    [Any("ENDFILE"), AwkTokenFrom(Identifier, true)]
    EndFile,
    [Any("function", "func"), AwkTokenFrom(Identifier, true)]
    Function,
    [Any("if"), AwkTokenFrom(Identifier, true)]
    If,
    [Any("else"), AwkTokenFrom(Identifier, true)]
    Else,
    [Any("while"), AwkTokenFrom(Identifier, true)]
    While,
    [Any("do"), AwkTokenFrom(Identifier, true)]
    Do,
    [Any("for"), AwkTokenFrom(Identifier, true)]
    For,
    [Any("in"), AwkTokenFrom(Identifier, true)]
    In,
    [Any("break"), AwkTokenFrom(Identifier, true)]
    Break,
    [Any("continue"), AwkTokenFrom(Identifier, true)]
    Continue,
    [Any("next"), AwkTokenFrom(Identifier, true)]
    Next,
    [Any("nextfile"), AwkTokenFrom(Identifier, true)]
    NextFile,
    [Any("delete"), AwkTokenFrom(Identifier, true)]
    Delete,
    [Any("exit"), AwkTokenFrom(Identifier, true)]
    Exit,
    [Any("return"), AwkTokenFrom(Identifier, true)]
    Return,
    [Any("print"), AwkTokenFrom(Identifier, true)]
    Print,
    [Any("printf"), AwkTokenFrom(Identifier, true)]
    Printf,
    [Any("switch"), AwkTokenFrom(Identifier, true)]
    Switch,
    [Any("case"), AwkTokenFrom(Identifier, true)]
    Case,
    [Any("default"), AwkTokenFrom(Identifier, true)]
    Default,
    [Any("getline"), AwkTokenFrom(Identifier, true)]
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
    [Any("^")]
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
    [Any("^=")]
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
    MinusMinus
}
