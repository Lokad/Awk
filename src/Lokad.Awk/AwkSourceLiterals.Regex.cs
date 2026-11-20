using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkSourceLiterals
{
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

        // Parse has already validated the literal. Lowering retains the raw recipe
        // and leaves translation for the execution-owned cache.
        regex = AwkRegex.FromValidatedPattern(AwkText.FromString(pattern, checkpoint), compatibilityMode, checkpoint);
        checkpoint?.Invoke();
        return true;
    }

    internal static bool TryReadRegexLiteralPattern(
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
