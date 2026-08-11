using System.Collections.Generic;
using Lokad.Parsing.Lexer;

namespace Lokad.Awk;

internal static class AwkLexer
{
    public static List<AwkToken> Tokenize(string source)
    {
        var lexer = new Lexer(source);
        return lexer.Tokenize();
    }

    private sealed class Lexer
    {
        private readonly string _source;
        private readonly List<AwkToken> _lexemes;
        private readonly List<AwkToken> _tokens = [];
        private int _position;
        private int _lexemeIndex;
        private bool _canStartRegex = true;

        public Lexer(string source)
        {
            _source = source;
            _lexemes = ReadContextFreeLexemes(source);

            static List<AwkToken> ReadContextFreeLexemes(string source)
            {
                var reader = new ReflectionTokenReader<AwkTokenKind>();
                var result = reader.ReadAllTokens(source);
                var lexemes = new List<AwkToken>(result.Tokens.Count);
                foreach (var token in result.Tokens)
                {
                    lexemes.Add(AwkToken.FromSource(
                        token.Length == 0
                            ? token.Token
                            : AwkParsingSupport.ResolveTextKind(source.AsSpan(token.Start, token.Length)),
                        source,
                        new AwkTextSpan(token.Start, token.Length)));
                }

                return lexemes;
            }
        }

        public List<AwkToken> Tokenize()
        {
            while (true)
            {
                SkipLexemesBeforePosition();
                if (_lexemeIndex >= _lexemes.Count)
                    break;

                var lexeme = _lexemes[_lexemeIndex];
                if (lexeme.Kind == AwkTokenKind.EndOfFile)
                    break;

                if (lexeme.Span.Length == 0)
                {
                    if (lexeme.Kind == AwkTokenKind.NewLine &&
                        lexeme.Span.Start < _source.Length &&
                        _source[lexeme.Span.Start] is '\r' or '\n')
                    {
                        _position = lexeme.Span.Start;
                        ReadNewLine();
                        continue;
                    }

                    _lexemeIndex++;
                    continue;
                }

                if (lexeme.Span.Start > _position)
                {
                    _position = lexeme.Span.Start;
                }

                if (Current == '#')
                {
                    ReadComment();
                    continue;
                }

                if (Current == '_' && Peek(1) == '"')
                {
                    ReadInternationalizedString();
                    continue;
                }

                if (Current == '"')
                {
                    ReadString();
                    continue;
                }

                if (Current == '@' && Peek(1) == '/')
                {
                    ReadStrongRegex();
                    continue;
                }

                if (lexeme.Kind == AwkTokenKind.Slash && _canStartRegex)
                {
                    ReadRegex();
                    continue;
                }

                AddLexeme(lexeme);
            }

            AddToken(AwkTokenKind.EndOfFile, _position, 0);
            return _tokens;

            void ReadNewLine()
            {
                var start = _position;
                if (Current == '\r' && Peek(1) == '\n')
                {
                    _position += 2;
                }
                else
                {
                    _position++;
                }

                AddToken(AwkTokenKind.NewLine, start, _position - start);
            }

            void ReadComment()
            {
                var start = _position;
                while (_position < _source.Length && Current is not '\r' and not '\n')
                {
                    _position++;
                }

                AddToken(AwkTokenKind.Comment, start, _position - start);
            }

            void ReadString()
            {
                var start = _position;
                ReadQuotedString(start, AwkTokenKind.String);
            }

            void ReadInternationalizedString()
            {
                var start = _position;
                _position++;
                ReadQuotedString(start, AwkTokenKind.InternationalizedString);
            }

            void ReadRegex()
            {
                var start = _position;
                _position++;
                ReadRegexBody(start, AwkTokenKind.Regex);
            }

            void ReadStrongRegex()
            {
                var start = _position;
                _position += 2;
                ReadRegexBody(start, AwkTokenKind.StrongRegex);
            }

            void AddLexeme(AwkToken lexeme)
            {
                AddToken(
                    lexeme.Kind,
                    lexeme.Span.Start,
                    lexeme.Span.Length);
            }
        }

        private char Current => Peek(0);

        private char Peek(int offset)
        {
            var index = _position + offset;
            return index >= _source.Length ? '\0' : _source[index];
        }

        private void ReadQuotedString(int start, AwkTokenKind kind)
        {
            _position++;
            var escaped = false;
            while (_position < _source.Length)
            {
                var current = Current;
                _position++;
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == '"')
                {
                    AddToken(kind, start, _position - start);
                    return;
                }

                if (current is '\r' or '\n')
                {
                    AddToken(AwkTokenKind.BadToken, start, _position - start);
                    return;
                }
            }

            AddToken(AwkTokenKind.BadToken, start, _position - start);
        }

        private void ReadRegexBody(int start, AwkTokenKind kind)
        {
            var scan = AwkRegexLiteral.Scan(_source, _position);
            if (scan.Termination == AwkRegexLiteralTermination.Delimiter)
            {
                _position = scan.EndIndex + 1;
                AddToken(kind, start, _position - start);
                return;
            }

            _position = scan.EndIndex;
            AddToken(AwkTokenKind.BadToken, start, _position - start);
        }

        private void AddToken(AwkTokenKind kind, int start, int length)
        {
            _tokens.Add(AwkToken.FromSource(kind, _source, new AwkTextSpan(start, length)));
            _position = start + length;
            _canStartRegex = AwkRegexStart.CanStartAfter(kind);
            SkipLexemesBeforePosition();
        }

        private void SkipLexemesBeforePosition()
        {
            while (_lexemeIndex < _lexemes.Count && _lexemes[_lexemeIndex].Span.Start < _position)
            {
                _lexemeIndex++;
            }
        }
    }
}
