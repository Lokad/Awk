using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkCondition
{
    private readonly IReadOnlyList<IReadOnlyList<AwkConditionClause>> _alternatives;

    public AwkCondition(IReadOnlyList<IReadOnlyList<AwkConditionClause>> alternatives)
    {
        _alternatives = AwkExecutableCollections.CopyNested(alternatives);
    }

    public async ValueTask<bool> EvaluateAsync(AwkEvaluationContext context)
    {
        if (_alternatives.Count == 0)
            return false;

        for (var alternativeIndex = 0; alternativeIndex < _alternatives.Count; alternativeIndex++)
        {
            var clauses = _alternatives[alternativeIndex];
            var matches = clauses.Count > 0;
            for (var clauseIndex = 0; clauseIndex < clauses.Count; clauseIndex++)
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
        if (_alternatives.Count != 1 || _alternatives[0].Count != 1)
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

    public static AwkConditionClause RegexMatch(AwkExpression left, Regex regex, bool negate) =>
        new(AwkExpression.RegexMatch(left, regex, negate));

    public static AwkConditionClause ArrayMembership(AwkArrayIndex index, string arrayName) =>
        new(AwkExpression.ArrayMembership(index, arrayName));

    public static AwkConditionClause Truthy(AwkExpression expression) =>
        new(expression);

    public async ValueTask<bool> EvaluateAsync(AwkEvaluationContext context) =>
        (await _expression.EvaluateAsync(context).ConfigureAwait(false)).IsTruthy();

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
