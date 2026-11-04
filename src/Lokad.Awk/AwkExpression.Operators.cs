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

    private class ComparisonExpression(
        AwkExpression left,
        AwkExpression right,
        AwkComparison comparison) : AwkExpression
    {
        protected AwkComparison Comparison => comparison;

        internal override ValueTask<bool> EvaluateConditionAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            var pending = left.EvaluateAsync(context);
            return pending.IsCompletedSuccessfully
                ? EvaluateConditionRight(context, pending.Result)
                : AwaitConditionLeftAsync(context, pending);
        }

        private ValueTask<bool> EvaluateConditionRight(AwkEvaluationContext context, AwkValue leftValue)
        {
            var retained = context.State.MemoryAccounting.RetainValue(leftValue);
            try
            {
                var pending = EvaluateConditionRightCore(context, leftValue);
                if (!pending.IsCompletedSuccessfully) return AwaitRetainedRightAsync(pending, retained);
                retained.Dispose();
                return pending;
            }
            catch
            {
                retained.Dispose();
                throw;
            }
        }

        protected virtual ValueTask<bool> EvaluateConditionRightCore(AwkEvaluationContext context, AwkValue leftValue)
        {
            var pending = right.EvaluateAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkComparisonOperations.Evaluate(leftValue, pending.Result, comparison, context.State))
                : AwaitConditionRightAsync(context, leftValue, pending);
        }

        private static async ValueTask<bool> AwaitRetainedRightAsync(ValueTask<bool> pending, AwkMemoryLease retained)
        {
            using (retained) return await pending.ConfigureAwait(false);
        }

        private async ValueTask<bool> AwaitConditionLeftAsync(AwkEvaluationContext context, ValueTask<AwkValue> pending) =>
            await EvaluateConditionRight(context, await pending.ConfigureAwait(false)).ConfigureAwait(false);

        private async ValueTask<bool> AwaitConditionRightAsync(
            AwkEvaluationContext context, AwkValue leftValue, ValueTask<AwkValue> pending) =>
            AwkComparisonOperations.Evaluate(leftValue, await pending.ConfigureAwait(false), comparison, context.State);

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var pending = left.EvaluateAsync(context);
            return pending.IsCompletedSuccessfully
                ? EvaluateRight(context, pending.Result)
                : AwaitLeftAsync(context, pending);
        }

        private ValueTask<AwkValue> EvaluateRight(AwkEvaluationContext context, AwkValue leftValue)
        {
            var pending = EvaluateConditionRight(context, leftValue);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkValue.FromNumber(pending.Result ? 1 : 0))
                : AwaitComparisonAsync(pending);
        }

        private async ValueTask<AwkValue> AwaitLeftAsync(AwkEvaluationContext context, ValueTask<AwkValue> pending) =>
            await EvaluateRight(context, await pending.ConfigureAwait(false)).ConfigureAwait(false);

        private static async ValueTask<AwkValue> AwaitComparisonAsync(ValueTask<bool> pending) =>
            AwkValue.FromNumber(await pending.ConfigureAwait(false) ? 1 : 0);

    }

    private sealed class FieldValueComparisonExpression(
        AwkExpression left,
        DynamicFieldIndexExpression field,
        AwkComparison comparison) : ComparisonExpression(left, field, comparison)
    {
        protected override ValueTask<bool> EvaluateConditionRightCore(AwkEvaluationContext context, AwkValue leftValue) =>
            field.CompareLeftAsync(context, leftValue, Comparison);
    }

    private sealed class FieldLiteralComparisonExpression(
        AwkExpression indexExpression,
        LiteralExpression literal,
        AwkComparison comparison) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => indexExpression is AwkSynchronousExpression { SynchronousDepth: > 0 and <= 14 } index
            ? index.SynchronousDepth + 2 : 0;
        internal override int SynchronousCallDepth => indexExpression is AwkSynchronousExpression index ? index.SynchronousCallDepth : 0;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            if (indexExpression is not AwkSynchronousExpression index)
                throw new InvalidOperationException("Synchronous field index is required.");
            context.State.Checkpoint(); // The original field expression, before its index.
            var field = AwkFieldIndex.FromNumber(index.ResolveNumberSynchronous(context)).Value;
            return AwkValue.FromNumber(context.State.CompareFieldLiteral(field, literal.LiteralText, comparison) ? 1 : 0);
        }

        internal override ValueTask<bool> EvaluateConditionAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            return EvaluateFieldConditionAsync(context);
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var pending = EvaluateFieldConditionAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkValue.FromNumber(pending.Result ? 1 : 0))
                : AwaitConditionAsync(pending);
        }

        private async ValueTask<bool> EvaluateFieldConditionAsync(AwkEvaluationContext context)
        {
            // Retain field-expression admission before evaluating its index once.
            context.State.Checkpoint();
            var index = AwkFieldIndex.FromNumber(await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false)).Value;
            return context.State.CompareFieldLiteral(index, literal.LiteralText, comparison);
        }

        private static async ValueTask<AwkValue> AwaitConditionAsync(ValueTask<bool> pending) =>
            AwkValue.FromNumber(await pending.ConfigureAwait(false) ? 1 : 0);
    }

}
