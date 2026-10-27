using System;
using System.Collections.Generic;
using System.Text;

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
            new AwkFrontendBudget(int.MaxValue));

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
            var text = token.TextSpan;
            var start = token.Kind == AwkTokenKind.StrongRegex ? 2 : 1;
            AwkRegexEscapes.ReportWarnings(text[start..^1], compatibilityMode, budget.RegexEscapeWarnings,
                warning => AddDiagnostic(new AwkDiagnostic(token.Span, warning)), message =>
                    AddDiagnostic(new AwkDiagnostic(token.Span, message, AwkDiagnosticId.WarningRegexEscape)), checkpoint);
            if (!AwkRegexSyntaxValidator.IsValidLiteral(
                token,
                compatibilityMode, checkpoint, budget.Memory.Budget, budget.RegexEngine, budget.RegexCancellationToken))
            {
                AddDiagnostic(new AwkDiagnostic(
                    token.Span,
                    "Invalid regular expression.",
                    AwkDiagnosticId.ParseInvalidRegex));
            }
        }

        void AddDiagnostic(AwkDiagnostic diagnostic)
        {

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

    public static bool IsValidLiteral(
        AwkToken token,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null, IMemoryBudget? memoryBudget = null,
        IAwkRegexEngine? regexEngine = null, CancellationToken cancellationToken = default)
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

        return IsValidPattern(pattern, compatibilityMode, checkpoint, memoryBudget, regexEngine, cancellationToken);
    }

    public static bool IsValidPattern(string pattern, IAwkRegexEngine? regexEngine = null)
        => IsValidPattern(pattern, AwkCompatibilityMode.GnuDefault, regexEngine: regexEngine);

    private static bool IsValidPattern(
        string pattern,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null, IMemoryBudget? memoryBudget = null,
        IAwkRegexEngine? regexEngine = null, CancellationToken cancellationToken = default)
    {
        checkpoint?.Invoke();
        try
        {
            checkpoint?.Invoke();
            var translated = AwkRegex.TranslatePattern(new AwkRegexPattern(pattern), compatibilityMode, checkpoint);
            checkpoint?.Invoke();
            if (regexEngine is null)
                throw new AwkRuntimeException(AwkMessages.ErrorRegexProviderRequired, AwkDiagnosticId.RuntimeRegexProvider);
            IAwkCompiledRegex? compiled = null;
            try
            {
                var supported = regexEngine.TryCompile(translated, false, memoryBudget, cancellationToken, out compiled);
                if (!supported)
                    throw new AwkRuntimeException(AwkMessages.ErrorRegexProviderUnsupported, AwkDiagnosticId.RuntimeRegexProvider);
                if (compiled is null || compiled.GroupCount < 1) return false;
            }
            finally { compiled?.Dispose(); }
            checkpoint?.Invoke();
            return true;
        }
        catch (AwkRegexAdmissionException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit, AwkDiagnosticId.RuntimeResourceLimit);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
