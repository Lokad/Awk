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

    internal bool TryGetSynchronousExpression(out AwkSynchronousExpression expression)
    {
        expression = _expression as AwkSynchronousExpression ?? AwkSynchronousExpression.Unavailable;
        return expression.SynchronousDepth > 0;
    }

    internal AwkCondition PrepareSynchronousCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions, Action? preparationCheckpoint = null) =>
        Expression.TryPrepareFunctionCalls(functions, 16, out var prepared, preparationCheckpoint) ? new(prepared) : this;
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
