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
        private readonly IReadOnlyList<AwkExpression> _parts;

        public ConcatExpression(IReadOnlyList<AwkExpression> parts)
        {
            if (parts.Count < 2)
                throw new ArgumentException("Concatenation requires at least two expressions.", nameof(parts));

            _parts = AwkExecutableCollections.Copy(parts);
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var formattedParts = ArrayPool<AwkText>.Shared.Rent(_parts.Count);
            try
            {
                for (var index = 0; index < _parts.Count; index++)
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
        }

        private async ValueTask<AwkValue> EvaluateRemainingAsync(
            AwkEvaluationContext context,
            AwkText[] formattedParts,
            int currentIndex,
            ValueTask<AwkValue> pending)
        {
            try
            {
                var current = await pending.ConfigureAwait(false);
                formattedParts[currentIndex] = context.State.FormatStringByteText(current);
                for (var index = currentIndex + 1; index < _parts.Count; index++)
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

        private AwkValue CreateValue(AwkEvaluationContext context, AwkText[] formattedParts) =>
            context.State.CreateByteTextValue(AwkText.Concat(
                formattedParts.AsSpan(0, _parts.Count),
                context.State.Counters,
                context.State.MemoryGovernor));

        private static void ReturnParts(AwkText[] formattedParts) =>
            ArrayPool<AwkText>.Shared.Return(formattedParts, clearArray: true);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(_parts[0]) || !compiler.TryEmitExpression(_parts[1]))
                return false;

            compiler.Emit(AwkOpcode.FormatString, operand0: 2);
            for (var index = 2; index < _parts.Count; index++)
            {
                if (!compiler.TryEmitExpression(_parts[index]))
                    return false;

                compiler.Emit(AwkOpcode.FormatString, operand0: 1);
            }

            compiler.Emit(AwkOpcode.Concat, operand0: _parts.Count);
            compiler.Replace(popped: _parts.Count, pushed: 1);
            return true;
        }
    }

    private sealed class BinaryExpression(AwkExpression left, AwkExpression right, AwkNumericOperation operation) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var leftValue = (await left.EvaluateAsync(context).ConfigureAwait(false)).AsNumber();
            var rightValue = (await right.EvaluateAsync(context).ConfigureAwait(false)).AsNumber();

            return AwkValue.FromNumber(AwkNumericOperations.Apply(leftValue, rightValue, operation));
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right))
                return false;

            compiler.Emit(ToOpcode(operation));
            compiler.Replace(popped: 2, pushed: 1);
            return true;
        }

        private static AwkOpcode ToOpcode(AwkNumericOperation operation) =>
            operation switch
            {
                AwkNumericOperation.Add => AwkOpcode.Add,
                AwkNumericOperation.Subtract => AwkOpcode.Subtract,
                AwkNumericOperation.Multiply => AwkOpcode.Multiply,
                AwkNumericOperation.Divide => AwkOpcode.Divide,
                AwkNumericOperation.Modulo => AwkOpcode.Modulo,
                AwkNumericOperation.Power => AwkOpcode.Power,
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
    }

    private sealed class NegateExpression(AwkExpression expression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkValue.FromNumber(-(await expression.EvaluateAsync(context).ConfigureAwait(false)).AsNumber());

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

    private sealed class ComparisonExpression(
        AwkExpression left,
        AwkExpression right,
        AwkComparison comparison) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var leftValue = await left.EvaluateAsync(context).ConfigureAwait(false);
            var rightValue = await right.EvaluateAsync(context).ConfigureAwait(false);
            var result = AwkComparisonOperations.Evaluate(
                leftValue,
                rightValue,
                comparison,
                context.State.Counters);
            return AwkValue.FromNumber(result ? 1 : 0);
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right))
                return false;

            compiler.Emit(AwkOpcode.Compare, (int)comparison);
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
        }

        private void EmitAnd(AwkExecutableCompiler compiler)
        {
            var startDepth = compiler.StackDepth;
            compiler.EmitExpressionOrFallback(left);
            var falseJump = compiler.Emit(AwkOpcode.JumpIfFalse);
            compiler.Pop();

            compiler.EmitExpressionOrFallback(right);
            compiler.Emit(AwkOpcode.Truthiness);
            var endJump = compiler.Emit(AwkOpcode.Jump);
            var resultDepth = compiler.StackDepth;

            compiler.SetStackDepth(startDepth);
            compiler.PatchOperand0(falseJump, compiler.CurrentOffset);
            EmitBooleanConstant(compiler, value: false);

            compiler.PatchOperand0(endJump, compiler.CurrentOffset);
            compiler.SetStackDepth(resultDepth);
        }

        private void EmitOr(AwkExecutableCompiler compiler)
        {
            var startDepth = compiler.StackDepth;
            compiler.EmitExpressionOrFallback(left);
            var evaluateRightJump = compiler.Emit(AwkOpcode.JumpIfFalse);
            compiler.Pop();

            EmitBooleanConstant(compiler, value: true);
            var endJump = compiler.Emit(AwkOpcode.Jump);
            var resultDepth = compiler.StackDepth;

            compiler.SetStackDepth(startDepth);
            compiler.PatchOperand0(evaluateRightJump, compiler.CurrentOffset);
            compiler.EmitExpressionOrFallback(right);
            compiler.Emit(AwkOpcode.Truthiness);

            compiler.PatchOperand0(endJump, compiler.CurrentOffset);
            compiler.SetStackDepth(resultDepth);
        }
    }

    private sealed class RegexMatchExpression(
        AwkExpression valueExpression,
        AwkExpression patternExpression,
        bool negate) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var input = (await valueExpression.EvaluateAsync(context).ConfigureAwait(false)).ByteText;
            var pattern = (await patternExpression.EvaluateAsync(context).ConfigureAwait(false))
                .AsString(context.State.Counters);
            var matched = context.State.IsRegexMatch(
                context.State.CreateRegex(new AwkRegexPattern(pattern)),
                input);
            return AwkValue.FromNumber(matched ^ negate ? 1 : 0);
        }
    }

    private sealed class PrecompiledRegexMatchExpression(
        AwkExpression valueExpression,
        AwkRegex regex,
        bool negate) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            var matched = context.State.IsRegexMatch(regex, input);
            return AwkValue.FromNumber(matched ^ negate ? 1 : 0);
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

            compiler.Emit(AwkOpcode.ArrayContains, compiler.Tables.InternArrayName(arrayName));
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
            var falseJump = compiler.Emit(AwkOpcode.JumpIfFalse);
            compiler.Pop();

            compiler.EmitExpressionOrFallback(whenTrue);
            var endJump = compiler.Emit(AwkOpcode.Jump);
            var resultDepth = compiler.StackDepth;

            compiler.SetStackDepth(startDepth);
            compiler.PatchOperand0(falseJump, compiler.CurrentOffset);
            compiler.EmitExpressionOrFallback(whenFalse);

            compiler.PatchOperand0(endJump, compiler.CurrentOffset);
            compiler.SetStackDepth(resultDepth);
            return true;
        }
    }

    private static void EmitBooleanConstant(AwkExecutableCompiler compiler, bool value)
    {
        compiler.Emit(AwkOpcode.LoadConstant, compiler.Tables.InternConstant(AwkValue.FromNumber(value ? 1 : 0)));
        compiler.Push();
    }

    private enum LogicalOperation
    {
        And,
        Or
    }
}
