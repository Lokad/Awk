using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkDirectiveParser
{
    public static bool TryParseLine(string line, out AwkParsedDirective directive, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        IReadOnlyList<AwkToken> tokens = AwkLexer.Tokenize(line, checkpoint);
        var atIndex = FindNextSignificantIndex(tokens, 0, checkpoint);
        checkpoint?.Invoke();
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        if (atIndex < 0 || tokens[atIndex].Kind != AwkTokenKind.At)
        {
            directive = default;
            return false;
        }

        return TryParseAtCore(tokens, atIndex, out directive, checkpoint);

    }

    public static bool TryParseAt(
        IReadOnlyList<AwkToken> tokens,
        int atIndex,
        out AwkParsedDirective directive, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        if (atIndex < 0 ||
            atIndex >= tokens.Count ||
            tokens[atIndex].Kind != AwkTokenKind.At ||
            !IsDirectivePosition(tokens, atIndex, checkpoint))
        {
            directive = default;
            return false;
        }

        return TryParseAtCore(tokens, atIndex, out directive, checkpoint);

        static bool IsDirectivePosition(IReadOnlyList<AwkToken> tokens, int index, Action? checkpoint)
        {
            for (var current = index - 1; current >= 0; current--)
            {
                checkpoint?.Invoke();
                var token = tokens[current];
                if (token.Kind == AwkTokenKind.Comment)
                    continue;

                return token.Kind is AwkTokenKind.EndOfFile or AwkTokenKind.NewLine or AwkTokenKind.Semicolon;
            }

            return true;
        }
    }

    public static bool TryParseIncludeLine(string line, out string path, Action? checkpoint = null)
    {
        path = string.Empty;
        if (!TryParseLine(line, out var directive, checkpoint) ||
            directive.Kind != AwkDirectiveKind.Include ||
            directive.ArgumentTokens.Count == 0 ||
            directive.ArgumentTokens[0].Kind != AwkTokenKind.String)
        {
            return false;
        }

        var tokenText = directive.ArgumentTokens[0].Text;
        if (tokenText.Length < 2)
            return false;

        checkpoint?.Invoke();
        path = tokenText[1..^1];
        checkpoint?.Invoke();
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
        out AwkParsedDirective directive, Action? checkpoint = null)
    {
        static List<AwkToken> ReadDirectiveArguments(
            IReadOnlyList<AwkToken> tokens,
            int startIndex, Action? checkpoint)
        {
            var arguments = new List<AwkToken>();
            for (var index = startIndex; index < tokens.Count; index++)
            {
                checkpoint?.Invoke();
                if (tokens[index].Kind is AwkTokenKind.EndOfFile or AwkTokenKind.NewLine or AwkTokenKind.Semicolon)
                    break;

                arguments.Add(tokens[index]);
            }

            return arguments;
        }

        var atToken = tokens[atIndex];
        var nameIndex = FindNextSignificantIndex(tokens, atIndex + 1, checkpoint);
        if (nameIndex < 0 ||
            tokens[nameIndex].Kind != AwkTokenKind.Identifier ||
            tokens[nameIndex].Span.Start != atToken.Span.End)
        {
            directive = default;
            return false;
        }

        var nextIndex = FindNextSignificantIndex(tokens, nameIndex + 1, checkpoint);
        if (nextIndex >= 0 && tokens[nextIndex].Kind == AwkTokenKind.LeftParen)
        {
            directive = default;
            return false;
        }

        var arguments = ReadDirectiveArguments(tokens, nameIndex + 1, checkpoint);
        var endToken = arguments.Count == 0 ? tokens[nameIndex] : arguments[^1];
        directive = new AwkParsedDirective(
            ResolveKind(tokens[nameIndex].Text),
            tokens[nameIndex].Text,
            arguments,
            new AwkTextSpan(atToken.Span.Start, endToken.Span.End - atToken.Span.Start));
        return true;
    }

    private static int FindNextSignificantIndex(IReadOnlyList<AwkToken> tokens, int startIndex, Action? checkpoint)
    {
        for (var index = startIndex; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
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
