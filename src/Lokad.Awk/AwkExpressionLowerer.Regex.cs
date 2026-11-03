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
        if (AwkSourceLiterals.TryReadRegexLiteralPattern(expression, out var pattern, scope.Checkpoint))
        {
            lowered = AwkExpression.SourceRegexPattern(pattern);
            return true;
        }

        return TryLower(expression, scope, out lowered);
    }

}
