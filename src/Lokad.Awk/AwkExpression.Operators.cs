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
            var formattedParts = ArrayPool<AwkText>.Shared.Rent(_parts.Length);
            try
            {
                for (var index = 0; index < _parts.Length; index++)
                {
                    var pending = _parts[index].EvaluateAsync(context);
                    if (!pending.IsCompletedSuccessfully)
                        return EvaluateRemainingAsync(context, formattedParts, index, pending);

                    formattedParts[index] = context.State.FormatStringByteText(pending.Result);
                }

                var result = CreateValue(context, formattedParts);
                ReturnParts(formattedParts);
                return ValueTask.FromResult(result);
            }
            catch
            {
                ReturnParts(formattedParts);
                throw;
            }

            async ValueTask<AwkValue> EvaluateRemainingAsync(
                AwkEvaluationContext context,
                AwkText[] formattedParts,
                int currentIndex,
                ValueTask<AwkValue> pending)
            {
                try
                {
                    var current = await pending.ConfigureAwait(false);
                    formattedParts[currentIndex] = context.State.FormatStringByteText(current);
                    for (var index = currentIndex + 1; index < _parts.Length; index++)
                    {
                        var value = await _parts[index].EvaluateAsync(context).ConfigureAwait(false);
                        formattedParts[index] = context.State.FormatStringByteText(value);
                    }

                    return CreateValue(context, formattedParts);
                }
                finally
                {
                    ReturnParts(formattedParts);
                }
            }
        }

        private AwkValue CreateValue(AwkEvaluationContext context, AwkText[] formattedParts) =>
            context.State.CreateDeferredTextValue(AwkText.Concat(
                formattedParts.AsSpan(0, _parts.Length),
                context.State.Counters,
                context.State.MemoryAccounting,
                context.State.CancellationCheckpoint));

        private static void ReturnParts(AwkText[] formattedParts) =>
            ArrayPool<AwkText>.Shared.Return(formattedParts, clearArray: true);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(_parts[0]) || !compiler.TryEmitExpression(_parts[1]))
                return false;

            compiler.EmitFormatString(valueCount: 2);
            for (var index = 2; index < _parts.Length; index++)
            {
                if (!compiler.TryEmitExpression(_parts[index]))
                    return false;

                compiler.EmitFormatString(valueCount: 1);
            }

            compiler.EmitConcat(_parts.Length);
            compiler.Replace(popped: _parts.Length, pushed: 1);
            return true;
        }
    }

    private sealed class BinaryExpression(AwkExpression left, AwkExpression right, AwkNumericOperation operation) : AwkExpression
    {
        public override ValueTask<double> ResolveNumberAsync(AwkEvaluationContext context)
        {
            context.State.RecordExecutionStep();
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

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right))
                return false;

            compiler.Emit(ToNumericOpcode(operation));
            compiler.Replace(popped: 2, pushed: 1);
            return true;


        }
    }

    private sealed class NumberConversionExpression(AwkExpression expression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkValue.FromNumber((await expression.EvaluateAsync(context).ConfigureAwait(false)).AsNumber(context.State.CancellationCheckpoint));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(expression))
                return false;

            compiler.Emit(AwkOpcode.ToNumber);
            return true;
        }
    }

    private sealed class NegateExpression(AwkExpression expression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkValue.FromNumber(-(await expression.EvaluateAsync(context).ConfigureAwait(false)).AsNumber(context.State.CancellationCheckpoint));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(expression))
                return false;

            compiler.Emit(AwkOpcode.Negate);
            return true;
        }
    }

    private sealed class NotExpression(AwkExpression expression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkValue.FromNumber((await expression.EvaluateAsync(context).ConfigureAwait(false)).IsTruthy() ? 0 : 1);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(expression))
                return false;

            compiler.Emit(AwkOpcode.Not);
            return true;
        }
    }

    private class ComparisonExpression(
        AwkExpression left,
        AwkExpression right,
        AwkComparison comparison) : AwkExpression
    {
        protected AwkComparison Comparison => comparison;

        internal override ValueTask<bool> EvaluateConditionAsync(AwkEvaluationContext context)
        {
            context.State.RecordExecutionStep();
            var pending = left.EvaluateAsync(context);
            return pending.IsCompletedSuccessfully
                ? EvaluateConditionRight(context, pending.Result)
                : AwaitConditionLeftAsync(context, pending);
        }

        protected virtual ValueTask<bool> EvaluateConditionRight(AwkEvaluationContext context, AwkValue leftValue)
        {
            var pending = right.EvaluateAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkComparisonOperations.Evaluate(leftValue, pending.Result, comparison, context.State))
                : AwaitConditionRightAsync(context, leftValue, pending);
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

        protected virtual ValueTask<AwkValue> EvaluateRight(AwkEvaluationContext context, AwkValue leftValue)
        {
            var pending = right.EvaluateAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(Apply(context, leftValue, pending.Result))
                : AwaitRightAsync(context, leftValue, pending);
        }

        private async ValueTask<AwkValue> AwaitLeftAsync(AwkEvaluationContext context, ValueTask<AwkValue> pending) =>
            await EvaluateRight(context, await pending.ConfigureAwait(false)).ConfigureAwait(false);

        private async ValueTask<AwkValue> AwaitRightAsync(
            AwkEvaluationContext context, AwkValue leftValue, ValueTask<AwkValue> pending) =>
            Apply(context, leftValue, await pending.ConfigureAwait(false));

        private AwkValue Apply(AwkEvaluationContext context, AwkValue leftValue, AwkValue rightValue) =>
            AwkValue.FromNumber(AwkComparisonOperations.Evaluate(
                leftValue, rightValue, comparison, context.State) ? 1 : 0);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right))
                return false;

            compiler.EmitComparison(comparison);
            compiler.Replace(popped: 2, pushed: 1);
            return true;
        }

    }

    private sealed class FieldValueComparisonExpression(
        AwkExpression left,
        DynamicFieldIndexExpression field,
        AwkComparison comparison) : ComparisonExpression(left, field, comparison)
    {
        protected override ValueTask<bool> EvaluateConditionRight(AwkEvaluationContext context, AwkValue leftValue) =>
            field.CompareLeftAsync(context, leftValue, Comparison);

        protected override ValueTask<AwkValue> EvaluateRight(AwkEvaluationContext context, AwkValue leftValue)
        {
            var pending = field.CompareLeftAsync(context, leftValue, Comparison);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkValue.FromNumber(pending.Result ? 1 : 0))
                : AwaitComparisonAsync(pending);
        }

        private static async ValueTask<AwkValue> AwaitComparisonAsync(ValueTask<bool> pending) =>
            AwkValue.FromNumber(await pending.ConfigureAwait(false) ? 1 : 0);
    }

    private sealed class FieldLiteralComparisonExpression(
        AwkExpression fieldExpression,
        AwkExpression indexExpression,
        LiteralExpression literal,
        AwkComparison comparison) : AwkExpression
    {
        internal override ValueTask<bool> EvaluateConditionAsync(AwkEvaluationContext context)
        {
            context.State.RecordExecutionStep();
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
            context.State.RecordExecutionStep();
            var index = (int)await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
            return context.State.CompareFieldLiteral(index, literal.LiteralText, comparison);
        }

        private static async ValueTask<AwkValue> AwaitConditionAsync(ValueTask<bool> pending) =>
            AwkValue.FromNumber(await pending.ConfigureAwait(false) ? 1 : 0);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            // The opt-in engine keeps ordinary field evaluation and comparison.
            if (!compiler.TryEmitExpression(fieldExpression) || !compiler.TryEmitExpression(literal))
                return false;
            compiler.EmitComparison(comparison);
            compiler.Replace(popped: 2, pushed: 1);
            return true;
        }
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

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (operation == LogicalOperation.And)
                EmitAnd(compiler);
            else
                EmitOr(compiler);

            return true;

            void EmitAnd(AwkExecutableCompiler compiler)
            {
                var startDepth = compiler.StackDepth;
                compiler.EmitExpressionOrFallback(left);
                var falseJump = compiler.EmitJumpIfFalse();
                compiler.Pop();

                compiler.EmitExpressionOrFallback(right);
                compiler.Emit(AwkOpcode.Truthiness);
                var endJump = compiler.EmitJump();
                var resultDepth = compiler.StackDepth;

                compiler.SetStackDepth(startDepth);
                compiler.PatchJumpTarget(falseJump, compiler.CurrentOffset);
                EmitBooleanConstant(compiler, value: false);

                compiler.PatchJumpTarget(endJump, compiler.CurrentOffset);
                compiler.SetStackDepth(resultDepth);
            }

            void EmitOr(AwkExecutableCompiler compiler)
            {
                var startDepth = compiler.StackDepth;
                compiler.EmitExpressionOrFallback(left);
                var evaluateRightJump = compiler.EmitJumpIfFalse();
                compiler.Pop();

                EmitBooleanConstant(compiler, value: true);
                var endJump = compiler.EmitJump();
                var resultDepth = compiler.StackDepth;

                compiler.SetStackDepth(startDepth);
                compiler.PatchJumpTarget(evaluateRightJump, compiler.CurrentOffset);
                compiler.EmitExpressionOrFallback(right);
                compiler.Emit(AwkOpcode.Truthiness);

                compiler.PatchJumpTarget(endJump, compiler.CurrentOffset);
                compiler.SetStackDepth(resultDepth);
            }
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

    private sealed class FieldArrayMembershipExpression(DynamicFieldIndexExpression field, AwkArrayName arrayName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            context.State.RecordExecutionStep(); // The field expression still has its ordinary checkpoint.
            var index = (int)await field.IndexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.ContainsArrayField(arrayName, index) ? 1 : 0);
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(field))
                return false;
            compiler.EmitArrayContains(arrayName);
            return true;
        }
    }

    private sealed class ArrayMembershipExpression(AwkArrayIndex indexExpression, AwkArrayName arrayName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var key = await indexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.ContainsArrayValue(arrayName, key) ? 1 : 0);
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!indexExpression.TryEmitExecutableKey(compiler))
                return false;

            compiler.EmitArrayContains(arrayName);
            return true;
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

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            var startDepth = compiler.StackDepth;
            compiler.EmitExpressionOrFallback(condition);
            var falseJump = compiler.EmitJumpIfFalse();
            compiler.Pop();

            compiler.EmitExpressionOrFallback(whenTrue);
            var endJump = compiler.EmitJump();
            var resultDepth = compiler.StackDepth;

            compiler.SetStackDepth(startDepth);
            compiler.PatchJumpTarget(falseJump, compiler.CurrentOffset);
            compiler.EmitExpressionOrFallback(whenFalse);

            compiler.PatchJumpTarget(endJump, compiler.CurrentOffset);
            compiler.SetStackDepth(resultDepth);
            return true;
        }
    }

    private static void EmitBooleanConstant(AwkExecutableCompiler compiler, bool value)
    {
        compiler.EmitLoadConstant(AwkValue.FromNumber(value ? 1 : 0));
        compiler.Push();
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
