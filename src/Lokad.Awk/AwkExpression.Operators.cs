using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class ConcatExpression : AwkExpression
    {
        private readonly AwkExpression[] _parts;

        public ConcatExpression(IReadOnlyList<AwkExpression> parts)
        {
            if (parts.Count < 2)
                throw new ArgumentException("Concatenation requires at least two expressions.", nameof(parts));

            _parts = AwkCollectionSnapshots.Copy(parts);
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var memory = context.State.MemoryAccounting;
            long retainedBytes = 0;
            AwkText[]? rented = null;
            // Short completed expressions keep references on the managed stack. Pending
            // operands transfer the evaluated prefix to the existing continuation owner.
            Span<AwkText> formattedParts =
                [AwkText.Empty, AwkText.Empty, AwkText.Empty, AwkText.Empty,
                 AwkText.Empty, AwkText.Empty, AwkText.Empty, AwkText.Empty];
            try
            {
                // A later operand can suspend before we know that a buffer is needed.
                // Admit its small staging allowance before starting any such work.
                Retain(memory, ref retainedBytes, AwkRuntimeMemory.EstimateAwkTextArrayBytes(_parts.Length));
                if (_parts.Length > 8)
                {
                    rented = RentParts(context);
                    formattedParts = rented;
                }
                for (var index = 0; index < _parts.Length; index++)
                {
                    var pending = _parts[index].EvaluateAsync(context);
                    if (!pending.IsCompletedSuccessfully)
                    {
                        if (rented is null)
                        {
                            rented = RentParts(context);
                            formattedParts[..index].CopyTo(rented);
                        }
                        return EvaluateRemainingAsync(context, rented, index, pending, retainedBytes);
                    }

                    var text = context.State.FormatStringByteText(pending.Result);
                    Retain(memory, ref retainedBytes, text.EstimatedRetainedMemoryBytes);
                    formattedParts[index] = text;
                }

                var result = CreateValue(context, formattedParts);
                ReturnParts(rented, memory);
                memory.ReleaseCommitted(retainedBytes);
                return ValueTask.FromResult(result);
            }
            catch
            {
                ReturnParts(rented, memory);
                memory.ReleaseCommitted(retainedBytes);
                throw;
            }

            async ValueTask<AwkValue> EvaluateRemainingAsync(
                AwkEvaluationContext context,
                AwkText[] formattedParts,
                int currentIndex,
                ValueTask<AwkValue> pending, long retainedBytes)
            {
                var memory = context.State.MemoryAccounting;
                try
                {
                    var current = await pending.ConfigureAwait(false);
                    formattedParts[currentIndex] = context.State.FormatStringByteText(current);
                    Retain(memory, ref retainedBytes, formattedParts[currentIndex].EstimatedRetainedMemoryBytes);
                    for (var index = currentIndex + 1; index < _parts.Length; index++)
                    {
                        var value = await _parts[index].EvaluateAsync(context).ConfigureAwait(false);
                        formattedParts[index] = context.State.FormatStringByteText(value);
                        Retain(memory, ref retainedBytes, formattedParts[index].EstimatedRetainedMemoryBytes);
                    }

                    return CreateValue(context, formattedParts);
                }
                finally
                {
                    ReturnParts(formattedParts, memory);
                    memory.ReleaseCommitted(retainedBytes);
                }
            }
        }

        private AwkValue CreateValue(AwkEvaluationContext context, ReadOnlySpan<AwkText> formattedParts) =>
            context.State.CreateDeferredTextValue(AwkText.Concat(
                formattedParts[.._parts.Length],
                context.State.Counters,
                context.State.MemoryAccounting,
                context.State.CancellationCheckpoint));

        private AwkText[] RentParts(AwkEvaluationContext context)
        {
            var memory = context.State.MemoryAccounting;
            // Small pooled metadata can differ from the estimate by a few slots. Large
            // governed buffers have exact backing admitted before allocation.
            var parts = UsePool(memory) ? ArrayPool<AwkText>.Shared.Rent(_parts.Length) : new AwkText[_parts.Length];
            context.State.Counters.RecordConcatenationArgumentBufferRent();
            return parts;
        }

        private bool UsePool(AwkMemoryAccounting memory) =>
            !memory.IsEnabled || AwkRuntimeMemory.EstimateAwkTextArrayBytes(_parts.Length) < AwkRuntimeLimits.CancellationCheckBytes;

        private void ReturnParts(AwkText[]? formattedParts, AwkMemoryAccounting memory)
        {
            if (formattedParts is null) return;
            if (UsePool(memory)) ArrayPool<AwkText>.Shared.Return(formattedParts, clearArray: true);
            else Array.Clear(formattedParts);
        }

        // Keep this batch's live charge in local state: completed short concatenations
        // need neither a heap-allocated lease nor a separate temporary buffer.
        private static void Retain(AwkMemoryAccounting memory, ref long retainedBytes, long bytes)
        {
            if (!memory.IsEnabled) return;
            var total = AwkRuntimeMemory.CheckedAdd(retainedBytes, bytes);
            memory.ReserveAndCommit(bytes);
            retainedBytes = total;
        }
    }

    private sealed class BinaryExpression(AwkExpression left, AwkExpression right, AwkNumericOperation operation) : AwkExpression
    {
        internal override bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkSynchronousExpression expression)
        {
            if (remainingDepth > 0 && left.TryPrepareFunctionCalls(functions, remainingDepth - 1, out var first) &&
                right.TryPrepareFunctionCalls(functions, remainingDepth - 1, out var second) &&
                TryGetSynchronousPair(first, second, out var syncLeft, out var syncRight))
            {
                expression = new SynchronousBinaryExpression(syncLeft, syncRight, operation);
                return true;
            }
            expression = AwkSynchronousExpression.Unavailable;
            return false;
        }

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

    private sealed class NumberConversionExpression(AwkExpression expression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkValue.FromNumber((await expression.EvaluateAsync(context).ConfigureAwait(false)).AsNumber(context.State.CancellationCheckpoint));
    }

    private sealed class NegateExpression(AwkExpression expression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkValue.FromNumber(-(await expression.EvaluateAsync(context).ConfigureAwait(false)).AsNumber(context.State.CancellationCheckpoint));
    }

    private sealed class NotExpression(AwkExpression expression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkValue.FromNumber((await expression.EvaluateAsync(context).ConfigureAwait(false)).IsTruthy() ? 0 : 1);
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
            var field = (int)index.ResolveNumberSynchronous(context);
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
            var index = (int)await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
            return context.State.CompareFieldLiteral(index, literal.LiteralText, comparison);
        }

        private static async ValueTask<AwkValue> AwaitConditionAsync(ValueTask<bool> pending) =>
            AwkValue.FromNumber(await pending.ConfigureAwait(false) ? 1 : 0);
    }

    private sealed class LogicalExpression(
        AwkExpression left,
        AwkExpression right,
        LogicalOperation operation) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var leftTruthy = (await left.EvaluateAsync(context).ConfigureAwait(false)).IsTruthy();
            if (operation == LogicalOperation.And && !leftTruthy)
                return AwkValue.FromNumber(0);

            if (operation == LogicalOperation.Or && leftTruthy)
                return AwkValue.FromNumber(1);

            return AwkValue.FromNumber((await right.EvaluateAsync(context).ConfigureAwait(false)).IsTruthy() ? 1 : 0);
        }
    }

    private sealed class RegexMatchExpression(
        AwkExpression valueExpression,
        AwkExpression patternExpression,
        AwkRegexMatchPolarity polarity) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            using var heldInput = context.State.MemoryAccounting.ReserveTransient(input.EstimatedRetainedMemoryBytes);
            var pattern = await patternExpression.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            var matched = context.State.IsRegexMatch(
                context.State.CreateRegex(pattern),
                input);
            return AwkValue.FromNumber(matched != (polarity is AwkRegexMatchPolarity.Negated) ? 1 : 0);
        }
    }

    private sealed class PrecompiledRegexMatchExpression(
        AwkExpression valueExpression,
        AwkRegex regex,
        AwkRegexMatchPolarity polarity) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            var matched = context.State.IsRegexMatch(regex, input);
            return AwkValue.FromNumber(matched != (polarity is AwkRegexMatchPolarity.Negated) ? 1 : 0);
        }
    }

    private sealed class FieldArrayMembershipExpression(DynamicFieldIndexExpression field, AwkArrayName arrayName, int globalIndex) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint(); // The field expression still has its ordinary checkpoint.
            var index = (int)await field.IndexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.ContainsArrayField(arrayName, index, globalIndex) ? 1 : 0);
        }
    }

    private sealed class ArrayMembershipExpression(AwkArrayIndex indexExpression, AwkArrayName arrayName, int globalIndex) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var key = await indexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.ContainsArrayValue(arrayName, key, globalIndex) ? 1 : 0);
        }
    }

    private sealed class TernaryExpression(
        AwkExpression condition,
        AwkExpression whenTrue,
        AwkExpression whenFalse) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            (await condition.EvaluateAsync(context).ConfigureAwait(false)).IsTruthy()
                ? await whenTrue.EvaluateAsync(context).ConfigureAwait(false)
                : await whenFalse.EvaluateAsync(context).ConfigureAwait(false);
    }

    private enum LogicalOperation
    {
        And,
        Or
    }
}

internal enum AwkRegexMatchPolarity
{
    Positive,
    Negated
}
