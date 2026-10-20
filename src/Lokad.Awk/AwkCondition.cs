using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkCondition
{
    private readonly AwkConditionClause[][] _alternatives;

    public AwkCondition(IReadOnlyList<IReadOnlyList<AwkConditionClause>> alternatives)
    {
        _alternatives = AwkCollectionSnapshots.CopyNested(alternatives);
    }

    public ValueTask<bool> EvaluateAsync(AwkEvaluationContext context) =>
        _alternatives.Length == 1 && _alternatives[0].Length == 1
            ? _alternatives[0][0].EvaluateAsync(context)
            : EvaluateAlternativesAsync(context);

    private async ValueTask<bool> EvaluateAlternativesAsync(AwkEvaluationContext context)
    {
        if (_alternatives.Length == 0)
            return false;

        for (var alternativeIndex = 0; alternativeIndex < _alternatives.Length; alternativeIndex++)
        {
            var clauses = _alternatives[alternativeIndex];
            var matches = clauses.Length > 0;
            for (var clauseIndex = 0; clauseIndex < clauses.Length; clauseIndex++)
            {
                if (await clauses[clauseIndex].EvaluateAsync(context).ConfigureAwait(false))
                    continue;

                matches = false;
                break;
            }

            if (matches)
                return true;
        }

        return false;
    }

    internal bool TryGetSynchronousExpression(out AwkSynchronousExpression expression)
    {
        if (_alternatives.Length == 1 && _alternatives[0].Length == 1)
            return _alternatives[0][0].TryGetSynchronousExpression(out expression);
        expression = AwkSynchronousExpression.Unavailable;
        return false;
    }

    internal AwkCondition PrepareSynchronousCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions) =>
        _alternatives.Length == 1 && _alternatives[0].Length == 1
            ? new([[_alternatives[0][0].PrepareSynchronousCalls(functions)]]) : this;
}

internal sealed class AwkConditionClause
{
    private readonly AwkExpression _expression;

    private AwkConditionClause(AwkExpression expression)
    {
        _expression = expression;
    }

    internal AwkConditionClause PrepareSynchronousCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions) =>
        _expression.TryPrepareFunctionCalls(functions, 16, out var prepared) ? new(prepared) : this;

    public static AwkConditionClause Comparison(AwkExpression left, AwkExpression right, AwkComparison comparison) =>
        new(AwkExpression.Compare(left, right, comparison));

    public static AwkConditionClause Truthy(AwkExpression expression) =>
        new(expression);

    public ValueTask<bool> EvaluateAsync(AwkEvaluationContext context) =>
        _expression.EvaluateConditionAsync(context);

    internal bool TryGetSynchronousExpression(out AwkSynchronousExpression expression)
    {
        expression = _expression as AwkSynchronousExpression ?? AwkSynchronousExpression.Unavailable;
        return expression.SynchronousDepth > 0;
    }
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
