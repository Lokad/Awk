using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class BinaryExpression(AwkExpression left, AwkExpression right, AwkNumericOperation operation) : AwkExpression
    {

        public override ValueTask<double> ResolveNumberAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            return EvaluateNumberCoreAsync(context);
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var pending = EvaluateNumberCoreAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkValue.FromNumber(pending.Result))
                : AwaitValueAsync(pending);
        }

        private static async ValueTask<AwkValue> AwaitValueAsync(ValueTask<double> pending) =>
            AwkValue.FromNumber(await pending.ConfigureAwait(false));

        private ValueTask<double> EvaluateNumberCoreAsync(AwkEvaluationContext context)
        {
            var pending = left.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? EvaluateRight(context, pending.Result)
                : AwaitLeftAsync(context, pending);
        }

        private ValueTask<double> EvaluateRight(AwkEvaluationContext context, double leftValue)
        {
            var pending = right.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkNumericOperations.Apply(leftValue, pending.Result, operation))
                : AwaitRightAsync(context, leftValue, pending);
        }

        private async ValueTask<double> AwaitLeftAsync(AwkEvaluationContext context, ValueTask<double> pending) =>
            await EvaluateRight(context, await pending.ConfigureAwait(false)).ConfigureAwait(false);

        private async ValueTask<double> AwaitRightAsync(
            AwkEvaluationContext context, double leftValue, ValueTask<double> pending) =>
            AwkNumericOperations.Apply(leftValue, await pending.ConfigureAwait(false), operation);
    }

}
