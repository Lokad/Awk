using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class AssignmentExpression(AwkReference target, AwkExpression valueExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var value = await valueExpression.EvaluateAsync(context).ConfigureAwait(false);
            await target.AssignValueAsync(context, value).ConfigureAwait(false);
            return value;
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler) =>
            TryEmitStore(compiler, AwkStoredValueRetention.Retain);

        internal override bool TryEmitExecutableStatement(AwkExecutableCompiler compiler) =>
            TryEmitStore(compiler, AwkStoredValueRetention.Discard);

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
