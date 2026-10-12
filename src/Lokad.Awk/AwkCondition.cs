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

    internal bool TryEmitExecutable(AwkExecutableCompiler compiler)
    {
        if (_alternatives.Length != 1 || _alternatives[0].Length != 1)
            return false;

        return _alternatives[0][0].TryEmitExecutable(compiler);
    }
}

internal sealed class AwkConditionClause
{
    private readonly AwkExpression _expression;

    private AwkConditionClause(AwkExpression expression)
    {
        _expression = expression;
    }

    public static AwkConditionClause Comparison(AwkExpression left, AwkExpression right, AwkComparison comparison) =>
        new(AwkExpression.Compare(left, right, comparison));

    public static AwkConditionClause RegexMatch(
        AwkExpression left,
        AwkRegex regex,
        AwkRegexMatchPolarity polarity) =>
        new(AwkExpression.RegexMatch(left, regex, polarity));

    public static AwkConditionClause Truthy(AwkExpression expression) =>
        new(expression);

    public ValueTask<bool> EvaluateAsync(AwkEvaluationContext context)
    {
        var pending = _expression.EvaluateAsync(context);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(pending.Result.IsTruthy())
            : AwaitValueAsync(pending);
    }

    private static async ValueTask<bool> AwaitValueAsync(ValueTask<AwkValue> pending) =>
        (await pending.ConfigureAwait(false)).IsTruthy();

    internal bool TryEmitExecutable(AwkExecutableCompiler compiler)
    {
        if (!compiler.TryEmitExpression(_expression))
            return false;

        compiler.Emit(AwkOpcode.Truthiness);
        return true;
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
