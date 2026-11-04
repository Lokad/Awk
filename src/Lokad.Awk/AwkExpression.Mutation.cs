using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{

    private sealed class AssignmentExpression(AwkReference target, AwkExpression valueExpression) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var pending = valueExpression.EvaluateAsync(context);
            return pending.IsCompletedSuccessfully
                ? Assign(context, pending.Result)
                : AwaitValueAsync(context, pending);
        }

        private ValueTask<AwkValue> Assign(AwkEvaluationContext context, AwkValue value)
        {
            var pending = target.AssignValueAsync(context, value);
            if (!pending.IsCompletedSuccessfully)
                return AwaitAssignmentAsync(pending, value);

            pending.GetAwaiter().GetResult();
            return ValueTask.FromResult(value.AsScalarCopy());
        }

        private async ValueTask<AwkValue> AwaitValueAsync(AwkEvaluationContext context, ValueTask<AwkValue> pending) =>
            await Assign(context, await pending.ConfigureAwait(false)).ConfigureAwait(false);

        private static async ValueTask<AwkValue> AwaitAssignmentAsync(ValueTask pending, AwkValue value)
        {
            await pending.ConfigureAwait(false);
            return value.AsScalarCopy();
        }
    }

    private sealed class CompoundAssignmentExpression(
        AwkReference target,
        AwkExpression valueExpression,
        AwkCompoundAssignmentOperation operation) : AwkExpression
    {

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyCompoundAssignmentAsync(context, valueExpression, operation);
    }

    private sealed class PreIncrementExpression(AwkReference target) : AwkExpression
    {

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyIncrementAsync(context);
    }

    private sealed class PreDecrementExpression(AwkReference target) : AwkExpression
    {

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyDecrementAsync(context);
    }

    private sealed class PostIncrementExpression(AwkReference target) : AwkExpression
    {

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyPostIncrementAsync(context);
    }

    private sealed class PostDecrementExpression(AwkReference target) : AwkExpression
    {

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyPostDecrementAsync(context);
    }
}
