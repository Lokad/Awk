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
                var kind = tokens[index].Text.StartsWith("/", StringComparison.Ordinal) ||
                    tokens[index].Text.StartsWith("@/", StringComparison.Ordinal)
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
                AddRegexDiagnosticIfInvalid(tokens[index], diagnostics);
            }
            else if (tokens[index].Kind is AwkTokenKind.String or AwkTokenKind.InternationalizedString)
            {
                AddStringEscapeWarnings(tokens[index], diagnostics);
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
                if (IsIndirectFunctionCall(source, tokens, index))
                {
                    errorMessage = AwkMessages.ErrorUnsupportedIndirectFunctionCall;
                    return true;
                }

                break;
            case AwkTokenKind.RightBracket:
                if (NextSignificantToken(source, tokens, index + 1).Kind == AwkTokenKind.LeftBracket)
                {
                    errorMessage = AwkMessages.ErrorUnsupportedArrayOfArrays;
                    return true;
                }

                break;
        }

        errorMessage = ReadOnlyMemory<byte>.Empty;
        return false;
    }

    private static bool IsDirective(
        IReadOnlyList<AwkToken> tokens,
        int index,
        string name) =>
        AwkDirectiveParser.TryParseAt(tokens, index, out var directive) &&
        string.Equals(directive.Name, name, StringComparison.Ordinal);

    private static bool IsIndirectFunctionCall(
        string source,
        IReadOnlyList<AwkToken> tokens,
        int index)
    {
        var token = tokens[index];
        var identifier = NextSignificantToken(source, tokens, index + 1);
        if (identifier.Kind != AwkTokenKind.Identifier || identifier.Span.Start != token.Span.End)
            return false;

        return NextSignificantToken(source, tokens, IndexAfter(tokens, identifier)).Kind == AwkTokenKind.LeftParen;
    }

    private static AwkToken NextSignificantToken(
        string source,
        IReadOnlyList<AwkToken> tokens,
        int index)
    {
        for (var current = index; current < tokens.Count; current++)
        {
            var token = tokens[current];
            if (token.Kind is AwkTokenKind.Comment or AwkTokenKind.NewLine)
                continue;

            return token;
        }

        return new AwkToken(AwkTokenKind.EndOfFile, string.Empty, new AwkTextSpan(source.Length, 0));
    }

    private static int IndexAfter(IReadOnlyList<AwkToken> tokens, AwkToken token)
    {
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Span.Start == token.Span.Start &&
                tokens[index].Span.Length == token.Span.Length &&
                tokens[index].Kind == token.Kind)
            {
                return index + 1;
            }
        }

        return tokens.Count;
    }

    private static void AddStringEscapeWarnings(
        AwkToken token,
        List<AwkDiagnostic> diagnostics)
    {
        var start = token.Kind == AwkTokenKind.InternationalizedString ? 2 : 1;
        var end = token.Text.Length - 1;
        for (var index = start; index < end; index++)
        {
            if (token.Text[index] != '\\' || index + 1 >= end)
                continue;

            index++;
            var escaped = token.Text[index];
            if (AwkEscapes.TryReadNumericEscape(token.Text, ref index, end, out _))
            {
                continue;
            }

            if (escaped == 'x')
            {
                diagnostics.Add(new AwkDiagnostic(
                    token.Span,
                    AwkMessages.FormatStringHexEscapeWarning(),
                    AwkDiagnosticKind.StringEscapeWarning));
                continue;
            }

            if (AwkLineContinuations.TrySkipAfterBackslash(token.Text, ref index, end))
                continue;

            if (AwkEscapes.TryDecodeSimpleEscape(escaped, out _))
                continue;

            diagnostics.Add(new AwkDiagnostic(
                token.Span,
                AwkMessages.FormatStringEscapeWarning(escaped),
                AwkDiagnosticKind.StringEscapeWarning));
        }
    }

    private static void AddRegexDiagnosticIfInvalid(
        AwkToken token,
        List<AwkDiagnostic> diagnostics)
    {
        if (!AwkRegexSyntaxValidator.IsValidLiteral(token))
            diagnostics.Add(new AwkDiagnostic(token.Span, "Invalid regular expression.", AwkDiagnosticKind.InvalidRegex));
    }
}

internal static class AwkRegexSyntaxValidator
{
    public static bool IsValidLiteral(AwkToken token)
    {
        string pattern;
        if (token.Kind == AwkTokenKind.StrongRegex)
        {
            pattern = token.Text.Length >= 3 ? token.Text[2..^1] : string.Empty;
        }
        else if (!AwkRegexLiteral.TryReadPattern(token.Text, out pattern))
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
