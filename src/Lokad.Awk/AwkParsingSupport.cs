using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Parsing.Error;
using Lokad.Parsing.Lexer;
using Lokad.Parsing.Parser;

namespace Lokad.Awk;

internal static class AwkParsingSupport
{
    public static void AppendText(System.Text.StringBuilder builder, ReadOnlySpan<char> text, Action? checkpoint)
    {
        checkpoint?.Invoke();
        for (var offset = 0; offset < text.Length;)
        {
            checkpoint?.Invoke();
            var length = Math.Min(1024, text.Length - offset);
            builder.Append(text.Slice(offset, length));
            offset += length;
        }
        checkpoint?.Invoke();
    }

    public static LexerResult<AwkTokenKind> ToLexerResult(
        AwkParsingSource source,
        IReadOnlyList<AwkToken> tokens,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        var lexerTokens = new LexerToken<AwkTokenKind>[tokens.Count];
        var hasInvalidTokens = false;
        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
            var token = tokens[index];
            lexerTokens[index] = new LexerToken<AwkTokenKind>(token.Kind, token.Span.Start, token.Span.Length);
            hasInvalidTokens |= token.Kind == AwkTokenKind.BadToken;
        }

        return new LexerResult<AwkTokenKind>(
            source.Text,
            lexerTokens,
            source.NewLines,
            hasInvalidTokens);
    }

    public static LexerResult<AwkTokenKind> ToLexerResultWithEndOfFile(
        AwkParsingSource source,
        IReadOnlyList<AwkToken> tokens,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        if (tokens.Count > 0 && tokens[^1].Kind == AwkTokenKind.EndOfFile)
            return ToLexerResult(source, tokens, checkpoint);

        var end = 0;
        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
            end = Math.Max(end, tokens[index].Span.End);
        }

        var withEnd = new AwkToken[tokens.Count + 1];
        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
            withEnd[index] = tokens[index];
        }

        withEnd[^1] = new AwkToken(AwkTokenKind.EndOfFile, string.Empty, new AwkTextSpan(end, 0));
        return ToLexerResult(source, withEnd, checkpoint);
    }

    public static LexerResult<AwkTokenKind> ToLexerResult(IReadOnlyList<AwkToken> tokens, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var tokenList = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        var end = 0;
        for (var index = 0; index < tokenList.Count; index++)
        {
            checkpoint?.Invoke();
            end = Math.Max(end, tokenList[index].Span.End);
        }
        var buffer = new char[end];
        for (var offset = 0; offset < buffer.Length; offset += Math.Min(1024, buffer.Length - offset))
        {
            checkpoint?.Invoke();
            buffer.AsSpan(offset, Math.Min(1024, buffer.Length - offset)).Fill(' ');
        }
        for (var index = 0; index < tokenList.Count; index++)
        {
            checkpoint?.Invoke();
            var token = tokenList[index];
            for (var offset = 0; offset < token.Span.Length; offset += Math.Min(1024, token.Span.Length - offset))
            {
                checkpoint?.Invoke();
                var length = Math.Min(1024, token.Span.Length - offset);
                token.TextSpan.Slice(offset, length).CopyTo(buffer.AsSpan(token.Span.Start + offset, length));
            }
        }

        var withEnd = new AwkToken[tokenList.Count + 1];
        for (var index = 0; index < tokenList.Count; index++)
        {
            checkpoint?.Invoke();
            withEnd[index] = tokenList[index];
        }
        withEnd[^1] = new AwkToken(AwkTokenKind.EndOfFile, string.Empty, new AwkTextSpan(end, 0));
        checkpoint?.Invoke();
        var source = AwkParsingSource.Create(new string(buffer), checkpoint);
        return ToLexerResult(source, withEnd, checkpoint);
    }

    public static AwkToken ToToken(AwkTokenKind kind, Pos<string> value, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        return new(kind, value.Value, new AwkTextSpan(value.Location.Location.Position, value.Location.Length));
    }

    public static AwkToken ToToken(Pos<string> value, Action? checkpoint = null) =>
        ToToken(ResolveTextKind(value.Value, checkpoint), value, checkpoint);

    // Generic grammar leaves must retain the lexical classification selected by the
    // compatibility profile and function scope. Reclassifying their text loses it.
    public static AwkToken RestoreToken(IReadOnlyList<AwkToken> tokens, Pos<string> value, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        var start = value.Location.Location.Position;
        var low = 0;
        var high = tokens.Count - 1;
        while (low <= high)
        {
            checkpoint?.Invoke();
            var middle = low + ((high - low) / 2);
            var token = tokens[middle];
            if (token.Span.Start < start)
                low = middle + 1;
            else if (token.Span.Start > start)
                high = middle - 1;
            else if (token.Span.Length == value.Location.Length)
                return token;
            else
                break;
        }
        throw new InvalidOperationException("A grammar leaf must reference its original lexical token.");
    }

    public static AwkTokenKind ResolveTextKind(string text, Action? checkpoint = null)
    {
        return ResolveTextKind(text.AsSpan(), checkpoint);
    }

    public static AwkTokenKind ResolveTextKind(ReadOnlySpan<char> text, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        if (text.Length == 0)
            return AwkTokenKind.EndOfFile;

        if (text[0] == '\r' || text[0] == '\n')
            return AwkTokenKind.NewLine;

        if (text[0] == '#')
            return AwkTokenKind.Comment;

        if (text[0] == '"')
            return AwkTokenKind.String;

        if (text.Length >= 2 && text[0] == '_' && text[1] == '"')
            return AwkTokenKind.InternationalizedString;

        if (text.Length >= 2 && text[0] == '@' && text[1] == '/')
            return AwkTokenKind.StrongRegex;

        if (char.IsDigit(text[0]) || (text[0] == '.' && text.Length > 1))
            return AwkTokenKind.Number;

        if (TryResolveLiteralTokenKind(text, out var literalKind))
            return literalKind;

        return text[0] == '/'
            ? AwkTokenKind.Regex
            : IsIdentifierText(text, checkpoint) ? AwkTokenKind.Identifier : AwkTokenKind.BadToken;

        static bool TryResolveLiteralTokenKind(
            ReadOnlySpan<char> text,
            out AwkTokenKind kind)
        {
            if (text.Length == 1)
            {
                kind = text[0] switch
                {
                    '{' => AwkTokenKind.LeftBrace,
                    '}' => AwkTokenKind.RightBrace,
                    '(' => AwkTokenKind.LeftParen,
                    ')' => AwkTokenKind.RightParen,
                    '[' => AwkTokenKind.LeftBracket,
                    ']' => AwkTokenKind.RightBracket,
                    ',' => AwkTokenKind.Comma,
                    ';' => AwkTokenKind.Semicolon,
                    '?' => AwkTokenKind.Question,
                    ':' => AwkTokenKind.Colon,
                    '$' => AwkTokenKind.Dollar,
                    '@' => AwkTokenKind.At,
                    '+' => AwkTokenKind.Plus,
                    '-' => AwkTokenKind.Minus,
                    '*' => AwkTokenKind.Star,
                    '/' => AwkTokenKind.Slash,
                    '%' => AwkTokenKind.Percent,
                    '^' => AwkTokenKind.Caret,
                    '!' => AwkTokenKind.Bang,
                    '~' => AwkTokenKind.Tilde,
                    '=' => AwkTokenKind.Equal,
                    '<' => AwkTokenKind.Less,
                    '>' => AwkTokenKind.Greater,
                    '|' => AwkTokenKind.Pipe,
                    _ => default
                };
                return kind != default;
            }

            kind = default;
            if (TextEquals(text, "BEGIN"))
                kind = AwkTokenKind.Begin;
            else if (TextEquals(text, "END"))
                kind = AwkTokenKind.End;
            else if (TextEquals(text, "BEGINFILE"))
                kind = AwkTokenKind.BeginFile;
            else if (TextEquals(text, "ENDFILE"))
                kind = AwkTokenKind.EndFile;
            else if (TextEquals(text, "function") || TextEquals(text, "func"))
                kind = AwkTokenKind.Function;
            else if (TextEquals(text, "if"))
                kind = AwkTokenKind.If;
            else if (TextEquals(text, "else"))
                kind = AwkTokenKind.Else;
            else if (TextEquals(text, "while"))
                kind = AwkTokenKind.While;
            else if (TextEquals(text, "do"))
                kind = AwkTokenKind.Do;
            else if (TextEquals(text, "for"))
                kind = AwkTokenKind.For;
            else if (TextEquals(text, "in"))
                kind = AwkTokenKind.In;
            else if (TextEquals(text, "break"))
                kind = AwkTokenKind.Break;
            else if (TextEquals(text, "continue"))
                kind = AwkTokenKind.Continue;
            else if (TextEquals(text, "next"))
                kind = AwkTokenKind.Next;
            else if (TextEquals(text, "nextfile"))
                kind = AwkTokenKind.NextFile;
            else if (TextEquals(text, "delete"))
                kind = AwkTokenKind.Delete;
            else if (TextEquals(text, "exit"))
                kind = AwkTokenKind.Exit;
            else if (TextEquals(text, "return"))
                kind = AwkTokenKind.Return;
            else if (TextEquals(text, "print"))
                kind = AwkTokenKind.Print;
            else if (TextEquals(text, "printf"))
                kind = AwkTokenKind.Printf;
            else if (TextEquals(text, "switch"))
                kind = AwkTokenKind.Switch;
            else if (TextEquals(text, "case"))
                kind = AwkTokenKind.Case;
            else if (TextEquals(text, "default"))
                kind = AwkTokenKind.Default;
            else if (TextEquals(text, "getline"))
                kind = AwkTokenKind.Getline;
            else if (TextEquals(text, "::"))
                kind = AwkTokenKind.DoubleColon;
            else if (TextEquals(text, "!~"))
                kind = AwkTokenKind.BangTilde;
            else if (TextEquals(text, "+="))
                kind = AwkTokenKind.PlusEqual;
            else if (TextEquals(text, "-="))
                kind = AwkTokenKind.MinusEqual;
            else if (TextEquals(text, "*="))
                kind = AwkTokenKind.StarEqual;
            else if (TextEquals(text, "/="))
                kind = AwkTokenKind.SlashEqual;
            else if (TextEquals(text, "%="))
                kind = AwkTokenKind.PercentEqual;
            else if (TextEquals(text, "**"))
                kind = AwkTokenKind.Caret;
            else if (TextEquals(text, "^=") || TextEquals(text, "**="))
                kind = AwkTokenKind.CaretEqual;
            else if (TextEquals(text, "=="))
                kind = AwkTokenKind.EqualEqual;
            else if (TextEquals(text, "!="))
                kind = AwkTokenKind.BangEqual;
            else if (TextEquals(text, "<="))
                kind = AwkTokenKind.LessEqual;
            else if (TextEquals(text, ">="))
                kind = AwkTokenKind.GreaterEqual;
            else if (TextEquals(text, ">>"))
                kind = AwkTokenKind.AppendRedirect;
            else if (TextEquals(text, "|&"))
                kind = AwkTokenKind.PipeAmpersand;
            else if (TextEquals(text, "&&"))
                kind = AwkTokenKind.AmpersandAmpersand;
            else if (TextEquals(text, "||"))
                kind = AwkTokenKind.PipePipe;
            else if (TextEquals(text, "++"))
                kind = AwkTokenKind.PlusPlus;
            else if (TextEquals(text, "--"))
                kind = AwkTokenKind.MinusMinus;

            return kind != default;

            static bool TextEquals(ReadOnlySpan<char> text, string value) =>
                text.SequenceEqual(value.AsSpan());
        }

        static bool IsIdentifierText(ReadOnlySpan<char> text, Action? checkpoint)
        {
            if (text.Length == 0 || !(char.IsLetter(text[0]) || text[0] == '_'))
                return false;

            for (var index = 1; index < text.Length; index++)
            {
                checkpoint?.Invoke();
                if (!char.IsLetterOrDigit(text[index]) && text[index] != '_')
                    return false;
            }

            return true;
        }
    }
}

internal sealed class AwkParsingSource
{
    private AwkParsingSource(string text, IReadOnlyList<int> newLines)
    {
        Text = text;
        NewLines = newLines;
    }

    public string Text { get; }

    // Lokad.Parsing needs absolute newline offsets for diagnostics. Keeping the
    // immutable source metadata here prevents each nested grammar parse from
    // rescanning the complete program.
    public IReadOnlyList<int> NewLines { get; }

    public static AwkParsingSource Create(string text, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var newLines = new List<int>();
        var position = 0;
        while (position < text.Length)
        {
            checkpoint?.Invoke();
            var length = Math.Min(1024, text.Length - position);
            var relative = text.AsSpan(position, length).IndexOf('\n');
            if (relative < 0)
            {
                position += length;
                continue;
            }

            var newline = position + relative;
            newLines.Add(newline);
            position = newline + 1;
        }

        return new AwkParsingSource(text, newLines);
    }
}

internal sealed class AwkTokenNamer : ITokenNamer<AwkTokenKind>
{
    public static readonly AwkTokenNamer Instance = new();

    private AwkTokenNamer()
    {
    }

    public bool IsFolded(AwkTokenKind token, ICollection<AwkTokenKind> expected) => false;

    public string TokenName(AwkTokenKind token, ICollection<AwkTokenKind> expected)
    {
        return token switch
        {
            AwkTokenKind.EndOfFile => "end of file",
            AwkTokenKind.NewLine => "newline",
            AwkTokenKind.Identifier or AwkTokenKind.SpacedIdentifier => "identifier",
            AwkTokenKind.Number => "number",
            AwkTokenKind.String => "string",
            AwkTokenKind.Regex => "regular expression",
            _ => token.ToString()
        };
    }
}

internal sealed class AwkTAttribute : TerminalAttribute
{
    public AwkTAttribute(params AwkTokenKind[] tokens)
        : base(tokens.Select(token => (int)token))
    {
    }
}

internal sealed class AwkOAttribute : TerminalAttribute
{
    public AwkOAttribute(params AwkTokenKind[] tokens)
        : base(tokens.Select(token => (int)token), optional: true)
    {
    }
}

internal sealed class AwkLAttribute : ListAttribute
{
    public AwkLAttribute()
        : this(-1)
    {
    }

    public AwkLAttribute(int maxRank)
        : base(maxRank)
    {
    }

}
