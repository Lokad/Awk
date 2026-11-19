using Lokad.Parsing.Lexer;

namespace Lokad.Awk;

// The contextual lexer consumes descriptors in source order, including those
// covered by a quoted literal, regexp or comment. Keep only the next descriptor.
internal struct AwkLexemeCursor(string source, Action? checkpoint)
{
    private static readonly IReadOnlyList<LexerRule<AwkTokenKind>> Rules =
        new ReflectionTokenReader<AwkTokenKind>().Rules;
    private static readonly LexerRule<AwkTokenKind>[][] AsciiRules = CreateAsciiRules();

    private int _position;
    private long _nextCheckpoint;
    private int _lastLexemeEnd;
    private bool _sawToken;
    private bool _lastWasNewLine;

    internal bool TryRead(out LexerToken<AwkTokenKind> lexeme)
    {
        while (_position < source.Length)
        {
            Checkpoint(_position);
            var start = _position;
            var first = source[start];
            if (first == '\n')
            {
                _position++;
                if (!_sawToken) continue;
                _lastWasNewLine = true;
                lexeme = new(AwkTokenKind.NewLine, start, 0);
                return true;
            }
            if (first is ' ' or '\t' or '\r')
            {
                _position++;
                continue;
            }

            var ascii = char.IsAscii(first);
            var candidates = ascii ? AsciiRules[first] : Rules;
            var bestLength = 0;
            var best = default(LexerRule<AwkTokenKind>);
            for (var index = 0; index < candidates.Count; index++)
            {
                var rule = candidates[index];
                if (rule.Definition.MaximumLength <= bestLength ||
                    !ascii && !rule.Definition.StartsWith(first))
                    continue;
                var length = rule.Token switch
                {
                    AwkTokenKind.Identifier => IdentifierLength(start),
                    AwkTokenKind.Number => NumberLength(start),
                    _ => rule.Definition.MatchLength(source, start),
                };
                if (length <= bestLength) continue;
                best = rule;
                bestLength = length;
            }
            _sawToken = true;
            _lastWasNewLine = false;
            if (bestLength == 0)
            {
                _position++;
                _lastLexemeEnd = _position;
                lexeme = new(AwkTokenKind.BadToken, start, 1);
                return true;
            }

            var kind = best.Token;
            if (best.SubTokens is { } children)
            {
                foreach (var child in children)
                {
                    if (child.Definition.MaximumLength >= bestLength &&
                        child.Definition.StartsWith(first) &&
                        child.Definition.MatchLength(source, start) == bestLength)
                        kind = child.Token;
                }
            }
            _position += bestLength;
            _lastLexemeEnd = _position;
            lexeme = new(kind, start, bestLength);
            return true;
        }
        // The descriptor reader inserts a final newline before trailing blanks.
        // A bare CR at this position is a real newline to the contextual lexer.
        if (_sawToken && !_lastWasNewLine)
        {
            _lastWasNewLine = true;
            lexeme = new(AwkTokenKind.NewLine, _lastLexemeEnd, 0);
            return true;
        }
        lexeme = default;
        return false;
    }

    private int IdentifierLength(int start)
    {
        var end = start + 1;
        while (end < source.Length && (char.IsAsciiLetterOrDigit(source[end]) || source[end] == '_'))
        {
            Checkpoint(end);
            end++;
        }
        return end - start;
    }

    private int NumberLength(int start)
    {
        if (source[start] == '0' && source.Length - start >= 3 &&
            source[start + 1] is 'x' or 'X' && char.IsAsciiHexDigit(source[start + 2]))
        {
            var hexEnd = start + 3;
            while (hexEnd < source.Length && char.IsAsciiHexDigit(source[hexEnd]))
            {
                Checkpoint(hexEnd);
                hexEnd++;
            }
            return hexEnd - start;
        }

        var end = ReadDigits(start);
        if (end < source.Length && source[end] == '.')
        {
            var fraction = ReadDigits(end + 1);
            if (end == start && fraction == end + 1)
                return 0;
            end = fraction;
        }
        if (end < source.Length && source[end] is 'e' or 'E')
        {
            var digits = end + 1;
            if (digits < source.Length && source[digits] is '+' or '-')
                digits++;
            var exponent = ReadDigits(digits);
            if (exponent > digits)
                end = exponent;
        }
        return end - start;
    }

    private int ReadDigits(int start)
    {
        var end = start;
        while (end < source.Length && char.IsAsciiDigit(source[end]))
        {
            Checkpoint(end);
            end++;
        }
        return end;
    }

    private void Checkpoint(int position)
    {
        if (position < _nextCheckpoint) return;
        checkpoint?.Invoke();
        _nextCheckpoint = (long)position + 1024;
    }

    private static LexerRule<AwkTokenKind>[][] CreateAsciiRules()
    {
        var groups = new LexerRule<AwkTokenKind>[128][];
        for (var index = 0; index < groups.Length; index++)
        {
            var first = (char)index;
            groups[index] = Rules.Where(rule => rule.Definition.StartsWith(first)).ToArray();
        }
        return groups;
    }
}
