using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class ConcatExpression(AwkExpression left, AwkExpression right) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var leftValue = await left.EvaluateAsync(context).ConfigureAwait(false);
            var rightValue = await right.EvaluateAsync(context).ConfigureAwait(false);
            return context.State.CreateByteTextValue(context.State
                .Formatting.FormatStringByteText(leftValue)
                .Concat(
                    context.State.Formatting.FormatStringByteText(rightValue),
                    context.State.Counters,
                    context.State.MemoryGovernor));
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right))
                return false;

            compiler.Emit(AwkOpcode.Concat);
            compiler.Replace(popped: 2, pushed: 1);
            return true;
        }
    }

    private sealed class BinaryExpression(AwkExpression left, AwkExpression right, BinaryOperation operation) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var leftValue = (await left.EvaluateAsync(context).ConfigureAwait(false)).AsNumber();
            var rightValue = (await right.EvaluateAsync(context).ConfigureAwait(false)).AsNumber();

            return operation switch
            {
                BinaryOperation.Add => AwkValue.FromNumber(leftValue + rightValue),
                BinaryOperation.Subtract => AwkValue.FromNumber(leftValue - rightValue),
                BinaryOperation.Multiply => AwkValue.FromNumber(leftValue * rightValue),
                BinaryOperation.Divide => AwkValue.FromNumber(
                    AwkNumericOperations.Divide(leftValue, rightValue, AwkMessages.ErrorDivisionByZero)),
                BinaryOperation.Modulo => AwkValue.FromNumber(
                    AwkNumericOperations.Modulo(leftValue, rightValue, AwkMessages.ErrorDivisionByZeroInModulo)),
                BinaryOperation.Power => AwkValue.FromNumber(Math.Pow(leftValue, rightValue)),
                _ => AwkValue.FromText(string.Empty)
            };
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right))
                return false;

            compiler.Emit(ToOpcode(operation));
            compiler.Replace(popped: 2, pushed: 1);
            return true;
        }

        private static AwkOpcode ToOpcode(BinaryOperation operation) =>
            operation switch
            {
                BinaryOperation.Add => AwkOpcode.Add,
                BinaryOperation.Subtract => AwkOpcode.Subtract,
                BinaryOperation.Multiply => AwkOpcode.Multiply,
                BinaryOperation.Divide => AwkOpcode.Divide,
                BinaryOperation.Modulo => AwkOpcode.Modulo,
                BinaryOperation.Power => AwkOpcode.Power,
                _ => AwkOpcode.NoOp
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
            var result = CompareValues(leftValue, rightValue, comparison, context.State.Counters);
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

        private static bool CompareValues(
            AwkValue leftValue,
            AwkValue rightValue,
            AwkComparison comparison,
            AwkRuntimeCounters counters)
        {
            int result;
            result = AwkValue.Compare(leftValue, rightValue, counters);

            return comparison switch
            {
                AwkComparison.GreaterOrEqual => result >= 0,
                AwkComparison.GreaterThan => result > 0,
                AwkComparison.LessOrEqual => result <= 0,
                AwkComparison.LessThan => result < 0,
                AwkComparison.Equal => result == 0,
                AwkComparison.NotEqual => result != 0,
                _ => false
            };
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
            var matched = context.State.Regex.IsMatch(
                context.State.Regex.Create(new AwkRegexPattern(pattern)),
                input);
            return AwkValue.FromNumber(matched ^ negate ? 1 : 0);
        }
    }

    private sealed class PrecompiledRegexMatchExpression(
        AwkExpression valueExpression,
        Regex regex,
        bool negate) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            var matched = context.State.Regex.IsMatch(regex, input);
            return AwkValue.FromNumber(matched ^ negate ? 1 : 0);
        }
    }

    private sealed class ArrayMembershipExpression(AwkArrayIndex indexExpression, AwkArrayName arrayName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var key = await indexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.Arrays.Contains(arrayName, key) ? 1 : 0);
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

    private enum BinaryOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Power
    }

    private enum LogicalOperation
    {
        And,
        Or
    }
}
