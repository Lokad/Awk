using System;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    private static bool TryLowerPatternExpression(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (TryReadRegexLiteralPattern(expression, out var pattern, scope.Checkpoint))
        {
            lowered = AwkExpression.SourceRegexPattern(pattern);
            return true;
        }

        return TryLower(expression, scope, out lowered);
    }

    public static bool TryLowerRegexLiteralPattern(
        AwkExpressionSyntax? expression,
        [NotNullWhen(true)] out AwkRegex? regex,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        regex = null;
        if (!TryReadRegexLiteralPattern(expression, out var pattern, checkpoint))
            return false;

        try
        {
            checkpoint?.Invoke();
            var prepared = AwkRegex.Create(
                new AwkRegexPattern(pattern),
                AwkRegexCaseSensitivity.CaseSensitive,
                compatibilityMode, checkpoint);
            checkpoint?.Invoke();
            regex = prepared;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryReadRegexLiteralPattern(
        AwkExpressionSyntax? expression,
        [NotNullWhen(true)] out string? pattern,
        Action? checkpoint = null)
    {
        pattern = null;
        if (expression?.Kind != AwkExpressionKind.Regex)
            return false;

        var tokens = expression.GetTokens(checkpoint);
        if (tokens.Count != 1 || tokens[0].Kind != AwkTokenKind.Regex)
        {
            return false;
        }

        var token = tokens[0];
        if (!AwkRegexLiteral.TryReadPattern(token.Text, out var literalPattern, checkpoint))
        {
            return false;
        }

        pattern = literalPattern;
        return true;
    }
}
