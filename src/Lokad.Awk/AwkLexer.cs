using System.Collections.Generic;
using System.Threading;
using Lokad.Parsing.Lexer;

namespace Lokad.Awk;

internal static class AwkLexer
{
    public static List<AwkToken> Tokenize(string source) =>
        Tokenize(source, int.MaxValue, CancellationToken.None);

    public static List<AwkToken> Tokenize(
        string source,
        int maxTokens,
        CancellationToken cancellationToken,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
        => Tokenize(source, maxTokens,
            cancellationToken.CanBeCanceled ? cancellationToken.ThrowIfCancellationRequested : null,
            compatibilityMode);

    internal static List<AwkToken> Tokenize(
        string source,
        int maxTokens,
        Action? checkpoint,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
    {
        checkpoint?.Invoke();
        var lexer = new Lexer(source, maxTokens, checkpoint, compatibilityMode);
        var tokens = lexer.Tokenize();
        checkpoint?.Invoke();
        return tokens;
    }

    private sealed class Lexer
    {
        private readonly string _source;
        private readonly List<AwkToken> _lexemes;
        private readonly List<AwkToken> _tokens = [];
        private readonly int _maxTokens;
        private readonly Action? _checkpoint;
        private int _position;
        private int _lexemeIndex;
        private bool _canStartRegex = true;
        private readonly AwkLexicalNames _names;

        public Lexer(string source, int maxTokens, Action? checkpoint, AwkCompatibilityMode compatibilityMode)
        {
            _source = source;
            _names = new AwkLexicalNames(compatibilityMode);
            _maxTokens = maxTokens;
            _checkpoint = checkpoint;
            _lexemes = ReadContextFreeLexemes(source, checkpoint);

            static List<AwkToken> ReadContextFreeLexemes(
                string source,
                Action? checkpoint)
            {
                checkpoint?.Invoke();
                var reader = new ReflectionTokenReader<AwkTokenKind>();
                var result = reader.ReadAllTokens(source);
                checkpoint?.Invoke();
                var lexemes = new List<AwkToken>(result.Tokens.Count);
                var index = 0;
                foreach (var token in result.Tokens)
                {
                    if ((index++ & 1023) == 0)
                        checkpoint?.Invoke();

                    lexemes.Add(AwkToken.FromSource(
                        token.Length == 0
                            ? token.Token
                            : AwkParsingSupport.ResolveTextKind(source.AsSpan(token.Start, token.Length), checkpoint),
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
                if ((_tokens.Count & 1023) == 0)
                    _checkpoint?.Invoke();

                SkipLexemesBeforePosition();
                if (_lexemeIndex >= _lexemes.Count)
                    break;

                var lexeme = _lexemes[_lexemeIndex];
                if (lexeme.Kind == AwkTokenKind.EndOfFile)
                    break;

                if (lexeme.Kind == AwkTokenKind.NewLine && lexeme.Span.Start < _source.Length &&
                    _source[lexeme.Span.Start] is '\r' or '\n')
                {
                    var start = lexeme.Span.Start;
                    if (start > _position && _source[start] == '\n' && _source[start - 1] == '\r')
                        start--;
                    _position = start;
                    ReadNewLine();
                    continue;
                }

                if (lexeme.Span.Length == 0)
                {
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

                if (Current == '\\')
                {
                    var escapedIndex = _position + 1;
                    if (AwkLineContinuations.TrySkipAfterBackslash(_source, ref escapedIndex, _source.Length))
                    {
                        _position = escapedIndex + 1;
                        continue;
                    }
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
                long nextCheckpoint = start;
                while (_position < _source.Length && Current is not '\r' and not '\n')
                {
                    CheckScanProgress(ref nextCheckpoint);
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
            long nextCheckpoint = _position;
            while (_position < _source.Length)
            {
                CheckScanProgress(ref nextCheckpoint);
                var current = Current;
                _position++;
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    if (AwkLineContinuations.TrySkipAfterBackslash(_source, ref _position, _source.Length))
                    {
                        _position++;
                        continue;
                    }
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
            var scan = AwkRegexLiteral.Scan(_source, _position, _checkpoint);
            if (scan.Termination == AwkRegexLiteralTermination.Delimiter)
            {
                _position = scan.EndIndex + 1;
                AddToken(kind, start, _position - start);
                return;
            }

            _position = scan.EndIndex;
            AddToken(AwkTokenKind.BadToken, start, _position - start);
        }

        private void CheckScanProgress(ref long nextCheckpoint)
        {
            if (_position >= nextCheckpoint)
            {
                _checkpoint?.Invoke();
                nextCheckpoint = (long)_position + 1024;
            }
        }

        private void AddToken(AwkTokenKind kind, int start, int length)
        {
            if (_tokens.Count >= _maxTokens)
            {
                throw new AwkRuntimeException(
                    AwkMessages.ErrorFrontendTokenLimit,
                    AwkDiagnosticId.RuntimeResourceLimit);
            }

            kind = _names.Classify(kind, _source.AsSpan(start, length));
            _tokens.Add(AwkToken.FromSource(kind, _source, new AwkTextSpan(start, length)));
            _position = start + length;
            _canStartRegex = AwkRegexStart.CanStartAfter(kind);
            SkipLexemesBeforePosition();
        }

        private void SkipLexemesBeforePosition()
        {
            while (_lexemeIndex < _lexemes.Count && _lexemes[_lexemeIndex].Span.Start < _position)
            {
                if ((_lexemeIndex & 1023) == 0)
                    _checkpoint?.Invoke();
                _lexemeIndex++;
            }
        }
    }
}
