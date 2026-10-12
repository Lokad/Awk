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

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler) =>
            TryEmitStore(compiler, AwkStoredValueRetention.Retain);

        internal override bool TryEmitExecutableStatement(AwkExecutableCompiler compiler)
        {
            // This store bypasses EmitExpressionOrFallback's ordinary expression checkpoint.
            compiler.Emit(AwkOpcode.CheckBudget);
            return TryEmitStore(compiler, AwkStoredValueRetention.Discard);
        }

        private bool TryEmitStore(AwkExecutableCompiler compiler, AwkStoredValueRetention retention)
        {
            if (target.TryGetScalarName(out var variable))
            {
                if (!compiler.TryEmitExpression(valueExpression))
                    return false;

                compiler.EmitStoreScalar(variable, retention);
                compiler.Pop();
                if (retention == AwkStoredValueRetention.Retain)
                    compiler.Push();

                return true;
            }

            if (!target.TryGetArrayElement(out var array, out var index) ||
                !compiler.TryEmitExpression(valueExpression) ||
                !index.TryEmitExecutableKey(compiler))
            {
                return false;
            }

            compiler.EmitStoreArrayElement(array, retention);
            compiler.Pop(2);
            if (retention == AwkStoredValueRetention.Retain)
                compiler.Push();

            return true;
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
