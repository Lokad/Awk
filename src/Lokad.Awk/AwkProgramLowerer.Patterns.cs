using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkProgramLowerer
{
    private static bool TryLowerSyntaxRegexPattern(
        AwkRuleSyntax rule,
        [NotNullWhen(true)] out AwkPattern? pattern)
    {
        pattern = null;
        if (rule.PatternSyntax is AwkSinglePatternSyntax singlePattern &&
            IsSingleRegexPattern(singlePattern) &&
            AwkExpressionLowerer.TryLowerRegexLiteralPattern(singlePattern.Expression, out var regex))
        {
            pattern = AwkPattern.Regex(regex);
            return true;
        }

        if (rule.PatternSyntax is not AwkRangePatternSyntax rangePattern ||
            !AwkExpressionLowerer.TryLowerRegexLiteralPattern(rangePattern.Start.Expression, out var start) ||
            !AwkExpressionLowerer.TryLowerRegexLiteralPattern(rangePattern.End.Expression, out var end))
        {
            return false;
        }

        var startCondition = start ?? throw new InvalidOperationException();
        var endCondition = end ?? throw new InvalidOperationException();
        pattern = AwkPattern.Range(startCondition, endCondition);
        return true;
    }

    private static bool TryLowerSyntaxExpressionRangePattern(
        AwkRuleSyntax rule,
        [NotNullWhen(true)] out AwkPattern? pattern)
    {
        pattern = null;
        if (rule.PatternSyntax is not AwkRangePatternSyntax rangePattern)
            return false;

        if (!TryLowerSyntaxPatternCondition(rangePattern.Start, out var start) ||
            !TryLowerSyntaxPatternCondition(rangePattern.End, out var end))
        {
            return false;
        }

        var startCondition = start ?? throw new InvalidOperationException();
        var endCondition = end ?? throw new InvalidOperationException();
        pattern = AwkPattern.Range(startCondition, endCondition);
        return true;
    }

    private static bool TryLowerSyntaxExpressionPattern(
        AwkRuleSyntax rule,
        [NotNullWhen(true)] out AwkCondition? condition)
    {
        condition = null;
        if (rule.PatternSyntax is not AwkSinglePatternSyntax singlePattern ||
            singlePattern.Expression == null ||
            singlePattern.Expression.Kind == AwkExpressionKind.Regex ||
            !AwkExpressionLowerer.TryLower(singlePattern.Expression, out var expression))
        {
            return false;
        }

        condition = new AwkCondition([[AwkConditionClause.Truthy(expression)]]);
        return true;
    }

    private static bool TryLowerSyntaxPatternCondition(
        AwkPatternPartSyntax part,
        [NotNullWhen(true)] out AwkCondition? condition)
    {
        condition = null;
        var expressionSyntax = part.Expression;
        if (expressionSyntax == null ||
            expressionSyntax.Kind == AwkExpressionKind.Regex ||
            !AwkExpressionLowerer.TryLower(expressionSyntax, out var expression))
        {
            return false;
        }

        condition = new AwkCondition([[AwkConditionClause.Truthy(expression)]]);
        return true;
    }
    private static bool IsSingleRegexPattern(AwkPatternSyntax pattern)
    {
        AwkToken? significantToken = null;
        for (var index = 0; index < pattern.Tokens.Count; index++)
        {
            if (!AwkTokenNavigation.IsSignificant(pattern.Tokens[index].Kind))
                continue;

            if (significantToken.HasValue)
                return false;

            significantToken = pattern.Tokens[index];
        }

        return significantToken.HasValue && significantToken.Value.Kind == AwkTokenKind.Regex;
    }

}
