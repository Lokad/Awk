using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkDirectiveParser
{
    public static bool TryParseLine(string line, out AwkParsedDirective directive)
    {
        var tokens = AwkLexer.Tokenize(line);
        var atIndex = FindFirstSignificantIndex(tokens);
        if (atIndex < 0 || tokens[atIndex].Kind != AwkTokenKind.At)
        {
            directive = default;
            return false;
        }

        return TryParseAtCore(tokens, atIndex, out directive);
    }

    public static bool TryParseAt(
        IReadOnlyList<AwkToken> tokens,
        int atIndex,
        out AwkParsedDirective directive)
    {
        if (atIndex < 0 ||
            atIndex >= tokens.Count ||
            tokens[atIndex].Kind != AwkTokenKind.At ||
            !IsDirectivePosition(tokens, atIndex))
        {
            directive = default;
            return false;
        }

        return TryParseAtCore(tokens, atIndex, out directive);
    }

    public static bool TryParseIncludeLine(string line, out string path)
    {
        path = string.Empty;
        if (!TryParseLine(line, out var directive) ||
            directive.Kind != AwkDirectiveKind.Include ||
            directive.ArgumentTokens.Count == 0 ||
            directive.ArgumentTokens[0].Kind != AwkTokenKind.String)
        {
            return false;
        }

        var tokenText = directive.ArgumentTokens[0].Text;
        if (tokenText.Length < 2)
            return false;

        path = AwkEscapes.Decode(tokenText[1..^1]);
        return true;
    }

    public static AwkDirectiveKind ResolveKind(string name) =>
        name switch
        {
            "include" => AwkDirectiveKind.Include,
            "load" => AwkDirectiveKind.Load,
            "namespace" => AwkDirectiveKind.Namespace,
            _ => AwkDirectiveKind.Unknown
        };

    private static bool TryParseAtCore(
        IReadOnlyList<AwkToken> tokens,
        int atIndex,
        out AwkParsedDirective directive)
    {
        static List<AwkToken> ReadDirectiveArguments(
            IReadOnlyList<AwkToken> tokens,
            int startIndex)
        {
            var arguments = new List<AwkToken>();
            for (var index = startIndex; index < tokens.Count; index++)
            {
                if (tokens[index].Kind is AwkTokenKind.EndOfFile or AwkTokenKind.NewLine or AwkTokenKind.Semicolon)
                    break;

                arguments.Add(tokens[index]);
            }

            return arguments;
        }

        var atToken = tokens[atIndex];
        var nameIndex = FindNextSignificantIndex(tokens, atIndex + 1);
        if (nameIndex < 0 ||
            tokens[nameIndex].Kind != AwkTokenKind.Identifier ||
            tokens[nameIndex].Span.Start != atToken.Span.End)
        {
            directive = default;
            return false;
        }

        var nextIndex = FindNextSignificantIndex(tokens, nameIndex + 1);
        if (nextIndex >= 0 && tokens[nextIndex].Kind == AwkTokenKind.LeftParen)
        {
            directive = default;
            return false;
        }

        var arguments = ReadDirectiveArguments(tokens, nameIndex + 1);
        var endToken = arguments.Count == 0 ? tokens[nameIndex] : arguments[^1];
        directive = new AwkParsedDirective(
            ResolveKind(tokens[nameIndex].Text),
            tokens[nameIndex].Text,
            arguments,
            new AwkTextSpan(atToken.Span.Start, endToken.Span.End - atToken.Span.Start));
        return true;
    }

    private static bool IsDirectivePosition(IReadOnlyList<AwkToken> tokens, int index)
    {
        for (var current = index - 1; current >= 0; current--)
        {
            var token = tokens[current];
            if (token.Kind == AwkTokenKind.Comment)
                continue;

            return token.Kind is AwkTokenKind.EndOfFile or AwkTokenKind.NewLine or AwkTokenKind.Semicolon;
        }

        return true;
    }

    private static int FindFirstSignificantIndex(IReadOnlyList<AwkToken> tokens) =>
        FindNextSignificantIndex(tokens, 0);

    private static int FindNextSignificantIndex(IReadOnlyList<AwkToken> tokens, int startIndex)
    {
        for (var index = startIndex; index < tokens.Count; index++)
        {
            if (AwkTokenNavigation.IsSignificant(tokens[index].Kind) &&
                tokens[index].Kind != AwkTokenKind.EndOfFile)
            {
                return index;
            }
        }

        return -1;
    }
}

internal readonly record struct AwkParsedDirective
{
    private readonly string? _name;
    private readonly IReadOnlyList<AwkToken>? _argumentTokens;

    public AwkParsedDirective(
        AwkDirectiveKind kind,
        string name,
        IReadOnlyList<AwkToken> argumentTokens,
        AwkTextSpan span)
    {
        Kind = kind;
        _name = name;
        _argumentTokens = argumentTokens;
        Span = span;
    }

    public AwkDirectiveKind Kind { get; }

    public string Name => _name ?? string.Empty;

    public IReadOnlyList<AwkToken> ArgumentTokens => _argumentTokens ?? [];

    public AwkTextSpan Span { get; }
}
