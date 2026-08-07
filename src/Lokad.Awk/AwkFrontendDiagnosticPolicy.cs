using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

internal static class AwkFrontendDiagnosticPolicy
{
    public static void AddLexicalDiagnostics(
        string source,
        IReadOnlyList<AwkToken> tokens,
        List<AwkDiagnostic> diagnostics)
    {
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Kind == AwkTokenKind.BadToken)
            {
                var text = tokens[index].TextSpan;
                var kind = text.StartsWith("/", StringComparison.Ordinal) ||
                    text.StartsWith("@/", StringComparison.Ordinal)
                    ? AwkDiagnosticKind.InvalidRegex
                    : AwkDiagnosticKind.Syntax;
                diagnostics.Add(new AwkDiagnostic(tokens[index].Span, "Unrecognized awk token.", kind));
            }
            else if (TryGetUnsupportedDiagnostic(source, tokens, index, out var unsupportedMessage))
            {
                diagnostics.Add(new AwkDiagnostic(
                    tokens[index].Span,
                    AwkUtf8Codec.Decode(unsupportedMessage).TrimEnd('\r', '\n'),
                    AwkDiagnosticId.UnsupportedPolicy));
            }
            else if (tokens[index].Kind is AwkTokenKind.Regex or AwkTokenKind.StrongRegex)
            {
                AddRegexDiagnosticIfInvalid(tokens[index]);
            }
            else if (tokens[index].Kind is AwkTokenKind.String or AwkTokenKind.InternationalizedString)
            {
                AddStringEscapeWarnings(tokens[index]);
            }
        }

        void AddStringEscapeWarnings(AwkToken token)
        {
            var text = token.TextSpan;
            var start = token.Kind == AwkTokenKind.InternationalizedString ? 2 : 1;
            var end = text.Length - 1;
            for (var index = start; index < end; index++)
            {
                if (text[index] != '\\' || index + 1 >= end)
                    continue;

                index++;
                var escaped = text[index];
                if (AwkEscapes.TryReadNumericEscape(text, ref index, end, out _))
                    continue;

                if (escaped == 'x')
                {
                    diagnostics.Add(new AwkDiagnostic(
                        token.Span,
                        AwkMessages.FormatStringHexEscapeWarning(),
                        AwkDiagnosticKind.StringEscapeWarning));
                    continue;
                }

                if (AwkLineContinuations.TrySkipAfterBackslash(text, ref index, end))
                    continue;

                if (AwkEscapes.TryDecodeSimpleEscape(escaped, out _))
                    continue;

                diagnostics.Add(new AwkDiagnostic(
                    token.Span,
                    AwkMessages.FormatStringEscapeWarning(escaped),
                    AwkDiagnosticKind.StringEscapeWarning));
            }
        }

        void AddRegexDiagnosticIfInvalid(AwkToken token)
        {
            if (!AwkRegexSyntaxValidator.IsValidLiteral(token))
            {
                diagnostics.Add(new AwkDiagnostic(
                    token.Span,
                    "Invalid regular expression.",
                    AwkDiagnosticKind.InvalidRegex));
            }
        }
    }

    private static bool TryGetUnsupportedDiagnostic(
        string source,
        IReadOnlyList<AwkToken> tokens,
        int index,
        out ReadOnlyMemory<byte> errorMessage)
    {
        var token = tokens[index];
        if (IsDirective(tokens, index, "load"))
        {
            errorMessage = AwkMessages.ErrorUnsupportedDynamicLoad;
            return true;
        }

        if (IsDirective(tokens, index, "namespace"))
        {
            errorMessage = AwkMessages.ErrorUnsupportedNamespace;
            return true;
        }

        switch (token.Kind)
        {
            case AwkTokenKind.DoubleColon:
                errorMessage = AwkMessages.ErrorUnsupportedNamespace;
                return true;
            case AwkTokenKind.StrongRegex:
                errorMessage = AwkMessages.ErrorUnsupportedStronglyTypedRegex;
                return true;
            case AwkTokenKind.InternationalizedString:
                errorMessage = AwkMessages.ErrorUnsupportedInternationalization;
                return true;
            case AwkTokenKind.At:
                if (IsIndirectFunctionCall())
                {
                    errorMessage = AwkMessages.ErrorUnsupportedIndirectFunctionCall;
                    return true;
                }

                break;
            case AwkTokenKind.RightBracket:
                if (TokenAtOrEnd(
                        source,
                        tokens,
                        NextSignificantTokenIndex(tokens, index + 1))
                    .Kind == AwkTokenKind.LeftBracket)
                {
                    errorMessage = AwkMessages.ErrorUnsupportedArrayOfArrays;
                    return true;
                }

                break;
        }

        errorMessage = ReadOnlyMemory<byte>.Empty;
        return false;

        bool IsIndirectFunctionCall()
        {
            var identifierIndex = NextSignificantTokenIndex(tokens, index + 1);
            var identifier = TokenAtOrEnd(source, tokens, identifierIndex);
            if (identifier.Kind != AwkTokenKind.Identifier || identifier.Span.Start != token.Span.End)
                return false;

            return TokenAtOrEnd(
                    source,
                    tokens,
                    NextSignificantTokenIndex(tokens, identifierIndex + 1))
                .Kind == AwkTokenKind.LeftParen;
        }
    }

    private static bool IsDirective(
        IReadOnlyList<AwkToken> tokens,
        int index,
        string name) =>
        AwkDirectiveParser.TryParseAt(tokens, index, out var directive) &&
        string.Equals(directive.Name, name, StringComparison.Ordinal);

    private static int NextSignificantTokenIndex(IReadOnlyList<AwkToken> tokens, int index)
    {
        for (var current = index; current < tokens.Count; current++)
        {
            if (tokens[current].Kind is not (AwkTokenKind.Comment or AwkTokenKind.NewLine))
                return current;
        }

        return tokens.Count;
    }

    private static AwkToken TokenAtOrEnd(
        string source,
        IReadOnlyList<AwkToken> tokens,
        int index) =>
        index < tokens.Count
            ? tokens[index]
            : new AwkToken(AwkTokenKind.EndOfFile, string.Empty, new AwkTextSpan(source.Length, 0));

}

internal static class AwkRegexSyntaxValidator
{
    public static bool IsValidLiteral(AwkToken token)
    {
        var text = token.Text;
        string pattern;
        if (token.Kind == AwkTokenKind.StrongRegex)
        {
            pattern = text.Length >= 3 ? text[2..^1] : string.Empty;
        }
        else if (!AwkRegexLiteral.TryReadPattern(text, out pattern))
        {
            return false;
        }

        return IsValidPattern(pattern);
    }

    public static bool IsValidPattern(string pattern)
    {
        try
        {
            _ = new Regex(
                AwkRegex.TranslatePattern(new AwkRegexPattern(pattern)),
                RegexOptions.CultureInvariant,
                AwkRegex.MatchTimeout);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
