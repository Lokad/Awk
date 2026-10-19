using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

internal static class AwkFrontendDiagnosticPolicy
{
    public static void AddLexicalDiagnostics(
        string source,
        IReadOnlyList<AwkToken> tokens,
        List<AwkDiagnostic> diagnostics) =>
        AddLexicalDiagnostics(
            source,
            tokens,
            diagnostics,
            AwkFrontendBudget.Unlimited(int.MaxValue));

    public static void AddLexicalDiagnostics(
        string source,
        IReadOnlyList<AwkToken> tokens,
        List<AwkDiagnostic> diagnostics,
        AwkFrontendBudget budget,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        tokens = AwkTokenNavigation.ForIndexedAccess(tokens, checkpoint);
        for (var index = 0; index < tokens.Count; index++)
        {
            checkpoint?.Invoke();
            if (tokens[index].Kind == AwkTokenKind.At &&
                AwkDirectiveParser.TryParseAt(tokens, index, out var directive, checkpoint) &&
                directive.Kind == AwkDirectiveKind.Include)
            {
                if (compatibilityMode != AwkCompatibilityMode.GnuDefault)
                    AddDiagnostic(new AwkDiagnostic(directive.Span, "@include is a GNU extension",
                        AwkDiagnosticId.BindInvalidProgram));
                // Include names are raw filename text rather than string expressions.
                while (index + 1 < tokens.Count && tokens[index + 1].Span.Start < directive.Span.End)
                {
                    checkpoint?.Invoke();
                    index++;
                }
                continue;
            }
            if (tokens[index].Kind == AwkTokenKind.Identifier &&
                Encoding.UTF8.GetByteCount(tokens[index].TextSpan) > budget.MaxIdentifierBytes)
            {
                throw new AwkRuntimeException(
                    AwkMessages.ErrorIdentifierLimit,
                    AwkDiagnosticId.RuntimeResourceLimit);
            }

            if (tokens[index].Kind == AwkTokenKind.BadToken)
            {
                var text = tokens[index].TextSpan;
                var id = text.StartsWith("/", StringComparison.Ordinal) ||
                    text.StartsWith("@/", StringComparison.Ordinal)
                    ? AwkDiagnosticId.ParseInvalidRegex
                    : AwkDiagnosticId.ParseSyntax;
                AddDiagnostic(new AwkDiagnostic(tokens[index].Span, "Unrecognized awk token.", id));
            }
            else if (TryGetUnsupportedDiagnostic(source, tokens, index, out var unsupportedMessage, checkpoint))
            {
                AddDiagnostic(new AwkDiagnostic(
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
            AwkEscapes.ReportStringWarnings(text[start..^1], compatibilityMode, budget.StringEscapeWarnings,
                warning => AddDiagnostic(new AwkDiagnostic(token.Span, warning)), checkpoint);
        }

        void AddRegexDiagnosticIfInvalid(AwkToken token)
        {
            var text = token.Text;
            var start = token.Kind == AwkTokenKind.StrongRegex ? 2 : 1;
            AwkRegexEscapes.ReportWarnings(text[start..^1], compatibilityMode, budget.RegexEscapeWarnings,
                warning => AddDiagnostic(new AwkDiagnostic(token.Span, warning)), message =>
                    AddDiagnostic(new AwkDiagnostic(token.Span, message, AwkDiagnosticId.WarningRegexEscape)), checkpoint);
            if (!AwkRegexSyntaxValidator.IsValidLiteral(
                token,
                budget.MaxRegexPatternBytes,
                budget.MaxRegexGroups,
                compatibilityMode, checkpoint))
            {
                AddDiagnostic(new AwkDiagnostic(
                    token.Span,
                    "Invalid regular expression.",
                    AwkDiagnosticId.ParseInvalidRegex));
            }
        }

        void AddDiagnostic(AwkDiagnostic diagnostic)
        {
            if (diagnostics.Count >= budget.MaxDiagnostics)
            {
                throw new AwkRuntimeException(
                    AwkMessages.ErrorFrontendDiagnosticLimit,
                    AwkDiagnosticId.RuntimeResourceLimit);
            }

            diagnostics.Add(diagnostic);
        }

        static bool TryGetUnsupportedDiagnostic(
            string source,
            IReadOnlyList<AwkToken> tokens,
            int index,
            out ReadOnlyMemory<byte> errorMessage, Action? checkpoint)
        {
            var token = tokens[index];
            if (token.Kind == AwkTokenKind.At &&
                AwkDirectiveParser.TryParseAt(tokens, index, out var directive, checkpoint))
            {
                if (directive.Kind == AwkDirectiveKind.Load)
                {
                    errorMessage = AwkMessages.ErrorUnsupportedDynamicLoad;
                    return true;
                }

                if (directive.Kind == AwkDirectiveKind.Namespace)
                {
                    errorMessage = AwkMessages.ErrorUnsupportedNamespace;
                    return true;
                }
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
                            NextSignificantTokenIndex(tokens, index + 1, checkpoint))
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
                var identifierIndex = NextSignificantTokenIndex(tokens, index + 1, checkpoint);
                var identifier = TokenAtOrEnd(source, tokens, identifierIndex);
                if (identifier.Kind != AwkTokenKind.Identifier || identifier.Span.Start != token.Span.End)
                    return false;

                return TokenAtOrEnd(
                        source,
                        tokens,
                        NextSignificantTokenIndex(tokens, identifierIndex + 1, checkpoint))
                    .Kind == AwkTokenKind.LeftParen;
            }
        }
    }

    private static int NextSignificantTokenIndex(IReadOnlyList<AwkToken> tokens, int index, Action? checkpoint)
    {
        for (var current = index; current < tokens.Count; current++)
        {
            checkpoint?.Invoke();
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
        => IsValidLiteral(token, long.MaxValue, int.MaxValue);

    public static bool IsValidLiteral(
        AwkToken token,
        long maxPatternBytes,
        int maxGroups,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var text = token.Text;
        string pattern;
        if (token.Kind == AwkTokenKind.StrongRegex)
        {
            pattern = text.Length >= 3 ? text[2..^1] : string.Empty;
        }
        else if (!AwkRegexLiteral.TryReadPattern(text, out pattern, checkpoint))
        {
            return false;
        }

        return IsValidPattern(pattern, maxPatternBytes, maxGroups, compatibilityMode, checkpoint);
    }

    public static bool IsValidPattern(string pattern)
        => IsValidPattern(pattern, long.MaxValue, int.MaxValue);

    private static bool IsValidPattern(
        string pattern,
        long maxPatternBytes,
        int maxGroups,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        try
        {
            checkpoint?.Invoke();
            AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(pattern, maxPatternBytes, maxGroups, checkpoint);
            var translated = AwkRegexScalarPattern.Translate(
                AwkRegex.TranslatePattern(new AwkRegexPattern(pattern), compatibilityMode, checkpoint), checkpoint);
            AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(translated, maxPatternBytes, maxGroups, checkpoint);
            checkpoint?.Invoke();
            _ = new Regex(
                translated,
                RegexOptions.CultureInvariant,
                AwkRegex.MatchTimeout);
            checkpoint?.Invoke();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
