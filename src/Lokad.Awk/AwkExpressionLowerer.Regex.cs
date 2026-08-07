using System;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    public static bool TryLowerRegexLiteral(
        AwkExpressionSyntax? expression,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!TryLowerRegexLiteralPattern(expression, out var regex))
            return false;

        lowered = AwkExpression.RegexMatch(AwkExpression.FieldZero(), regex, negate: false);
        return true;
    }

    private static bool TryLowerPatternExpression(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (TryReadRegexLiteralPattern(expression, out var pattern))
        {
            lowered = AwkExpression.Literal(pattern);
            return true;
        }

        return TryLower(expression, scope, out lowered);
    }

    public static bool TryLowerRegexLiteralPattern(
        AwkExpressionSyntax? expression,
        [NotNullWhen(true)] out AwkRegex? regex)
    {
        regex = null;
        if (!TryReadRegexLiteralPattern(expression, out var pattern))
            return false;

        try
        {
            regex = AwkRegex.Create(new AwkRegexPattern(pattern), ignoreCase: false);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryReadRegexLiteralPattern(
        AwkExpressionSyntax? expression,
        [NotNullWhen(true)] out string? pattern)
    {
        pattern = null;
        if (expression?.Kind != AwkExpressionKind.Regex ||
            expression.Tokens.Count != 1 ||
            expression.Tokens[0].Kind != AwkTokenKind.Regex)
        {
            return false;
        }

        var token = expression.Tokens[0];
        if (!AwkRegexLiteral.TryReadPattern(token.Text, out var literalPattern))
        {
            return false;
        }

        pattern = literalPattern;
        return true;
    }
}
