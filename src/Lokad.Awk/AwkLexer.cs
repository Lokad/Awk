using System.Collections.Generic;
using System.Threading;
using Lokad.Parsing.Lexer;

namespace Lokad.Awk;

internal static class AwkLexer
{
    public static List<AwkToken> Tokenize(string source) =>
        Tokenize(source, CancellationToken.None);

    public static List<AwkToken> Tokenize(
        string source,
        CancellationToken cancellationToken,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
        => Tokenize(source,
            cancellationToken.CanBeCanceled ? cancellationToken.ThrowIfCancellationRequested : null,
            compatibilityMode);

    internal static List<AwkToken> Tokenize(
        string source,
        Action? checkpoint,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        AwkFrontendMemory? memory = null)
    {
        checkpoint?.Invoke();
        var frontend = memory ?? AwkFrontendMemory.Disabled;
        var lexer = new Lexer(source, checkpoint, compatibilityMode, frontend);
        var tokens = lexer.Tokenize();
        checkpoint?.Invoke();
        frontend.AdmitTokens(source.Length, tokens.Count);
        return tokens;
    }

    private sealed class Lexer
    {
        private readonly string _source;
        private readonly List<AwkToken> _lexemes;
        private readonly List<AwkToken> _tokens = [];
        private readonly Action? _checkpoint;
        private int _position;
        private int _lexemeIndex;
        private bool _canStartRegex = true;
        private AwkTokenKind _previousSignificant;
        private int _parenthesisDepth;
        private List<int>? _controlHeaders;
        private readonly AwkLexicalNames _names;

        public Lexer(string source, Action? checkpoint, AwkCompatibilityMode compatibilityMode,
            AwkFrontendMemory memory)
        {
            _source = source;
            _names = new AwkLexicalNames(compatibilityMode);
            _checkpoint = checkpoint;
            _lexemes = ReadContextFreeLexemes(source, checkpoint, memory);

            static List<AwkToken> ReadContextFreeLexemes(
                string source,
                Action? checkpoint,
                AwkFrontendMemory memory)
            {
                checkpoint?.Invoke();
                var reader = new ReflectionTokenReader<AwkTokenKind>();
                memory.AdmitLexing(source.Length);
                var result = reader.ReadAllTokens(source);
                checkpoint?.Invoke();
                memory.AdmitLexemes(source.Length, result.Tokens.Count);
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

                if (Current == '/' && (_canStartRegex ||
                    lexeme.Kind == AwkTokenKind.SlashEqual && FollowsNonAssignableExpression()))
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

        private bool FollowsNonAssignableExpression()
        {
            // /= can assign only after a variable. Other simple expressions
            // concatenate a regex beginning with '='. Classify that boundary
            // without entering the parser before delimiter-depth validation.
            // Nested argument/index expressions are complete operands here.
            var hasRightOperand = false;
            var hasOperand = false;
            var assignable = false;
            var mutated = false;
            var arithmetic = false;
            var power = false;
            var fieldIndexPower = false;
            var unaryIndex = false;
            for (var index = _tokens.Count - 1; index >= 0; index--)
            {
                _checkpoint?.Invoke();
                var kind = _tokens[index].Kind;
                if (kind == AwkTokenKind.Comment) continue;
                if (kind is AwkTokenKind.RightParen or AwkTokenKind.RightBracket)
                {
                    var close = kind;
                    var open = close == AwkTokenKind.RightParen
                        ? AwkTokenKind.LeftParen : AwkTokenKind.LeftBracket;
                    var depth = 1;
                    while (--index >= 0)
                    {
                        _checkpoint?.Invoke();
                        kind = _tokens[index].Kind;
                        if (kind == close) depth++;
                        else if (kind == open && --depth == 0) break;
                    }
                    if (index < 0) return false;
                    // Array subscripts belong to their preceding target.
                    if (close == AwkTokenKind.RightBracket) continue;
                    var before = index - 1;
                    while (before >= 0 && _tokens[before].Kind == AwkTokenKind.Comment)
                    {
                        _checkpoint?.Invoke();
                        before--;
                    }
                    if (before >= 0 && _tokens[before].Kind is AwkTokenKind.If or
                        AwkTokenKind.While or AwkTokenKind.For or AwkTokenKind.Switch)
                        break;
                    if (before >= 0 && _tokens[before].Kind == AwkTokenKind.Identifier &&
                        (_tokens[before].Span.End == _tokens[index].Span.Start ||
                         AwkBuiltins.IsKnownFunctionOrSpecialForm(_tokens[before].Text)))
                        index = before; // A call and its name form one operand.
                    kind = AwkTokenKind.RightParen;
                }
                if (kind is AwkTokenKind.Identifier or AwkTokenKind.Number or
                    AwkTokenKind.String or AwkTokenKind.InternationalizedString or
                    AwkTokenKind.Regex or AwkTokenKind.StrongRegex or AwkTokenKind.RightParen)
                {
                    if (hasRightOperand) return true;
                    hasRightOperand = true;
                    hasOperand = true;
                    assignable = kind == AwkTokenKind.Identifier && !mutated &&
                        !_tokens[index].TextSpan.SequenceEqual(AwkBuiltins.Name(AwkBuiltinId.Length));
                    mutated = false;
                    unaryIndex = false;
                    fieldIndexPower = false;
                }
                else if (kind == AwkTokenKind.Dollar)
                {
                    assignable = true;
                    // A signed/not field index consumes powers; $x^2 instead
                    // raises the field value and is not an assignment operand.
                    fieldIndexPower |= unaryIndex;
                    unaryIndex = false;
                }
                else if (kind == AwkTokenKind.Bang ||
                    kind is AwkTokenKind.Plus or AwkTokenKind.Minus && IsUnarySign(index))
                {
                    assignable = false;
                    unaryIndex = true;
                }
                else if (kind is AwkTokenKind.Plus or AwkTokenKind.Minus or AwkTokenKind.Star or
                    AwkTokenKind.Slash or AwkTokenKind.Percent or AwkTokenKind.Caret)
                {
                    if (kind == AwkTokenKind.Caret) power = true;
                    else arithmetic = true;
                    hasRightOperand = false;
                    assignable = false;
                    mutated = false;
                    unaryIndex = false;
                    fieldIndexPower = false;
                }
                else if (kind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus)
                {
                    assignable = false;
                    mutated = true;
                    unaryIndex = false;
                }
                else break;
            }
            return hasOperand && (!assignable || arithmetic || power && !fieldIndexPower);

            bool IsUnarySign(int index)
            {
                while (--index >= 0 && _tokens[index].Kind == AwkTokenKind.Comment)
                    _checkpoint?.Invoke();
                return index < 0 || AwkRegexStart.CanStartAfter(_tokens[index].Kind);
            }
        }

        private void AddToken(AwkTokenKind kind, int start, int length)
        {

            kind = _names.Classify(kind, _source.AsSpan(start, length));
            _tokens.Add(AwkToken.FromSource(kind, _source, new AwkTextSpan(start, length)));
            _position = start + length;
            _canStartRegex = AwkRegexStart.CanStartAfter(kind);
            if (AwkTokenNavigation.IsSignificant(kind))
            {
                if (kind == AwkTokenKind.LeftParen)
                {
                    _parenthesisDepth++;
                    if (_previousSignificant is AwkTokenKind.If or AwkTokenKind.While or AwkTokenKind.For)
                        (_controlHeaders ??= []).Add(_parenthesisDepth);
                }
                else if (kind == AwkTokenKind.RightParen)
                {
                    if (_controlHeaders is { Count: > 0 } headers && headers[^1] == _parenthesisDepth)
                    {
                        headers.RemoveAt(headers.Count - 1);
                        _canStartRegex = true;
                    }
                    _parenthesisDepth = Math.Max(0, _parenthesisDepth - 1);
                }
                _previousSignificant = kind;
            }
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
