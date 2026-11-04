using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkCondition(AwkExpression expression)
{
    private readonly AwkExpression? _expression = expression;

    private AwkExpression Expression => _expression ??
        throw new InvalidOperationException("The default condition has no expression.");

    public ValueTask<bool> EvaluateAsync(AwkEvaluationContext context) =>
        Expression.EvaluateConditionAsync(context);
}

internal enum AwkComparison
{
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    Equal,
    NotEqual
}
