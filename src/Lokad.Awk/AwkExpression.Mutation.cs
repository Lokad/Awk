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
            TryEmitStoreScalar(compiler, keepValue: true);

        internal override bool TryEmitExecutableStatement(AwkExecutableCompiler compiler) =>
            TryEmitStoreScalar(compiler, keepValue: false);

        private bool TryEmitStoreScalar(AwkExecutableCompiler compiler, bool keepValue)
        {
            if (target.TryGetScalarName(out var variable))
            {
                if (!compiler.TryEmitExpression(valueExpression))
                    return false;

                compiler.Emit(AwkOpcode.StoreScalar, compiler.Tables.InternName(variable), keepValue ? 1 : 0);
                compiler.Pop();
                if (keepValue)
                    compiler.Push();

                return true;
            }

            if (!target.TryGetArrayElement(out var array, out var index) ||
                !compiler.TryEmitExpression(valueExpression) ||
                !index.TryEmitExecutableKey(compiler))
            {
                return false;
            }

            compiler.Emit(AwkOpcode.StoreArrayElement, compiler.Tables.InternArrayName(array), keepValue ? 1 : 0);
            compiler.Pop(2);
            if (keepValue)
                compiler.Push();

            return true;
        }
    }

    private sealed class CompoundAssignmentExpression(
        AwkReference target,
        AwkExpression valueExpression,
        AwkCompoundAssignmentOperation operation) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            await target.ApplyCompoundAssignmentAsync(context, valueExpression, operation).ConfigureAwait(false);
    }

    private sealed class PreIncrementExpression(AwkReference target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            await target.ApplyIncrementAsync(context).ConfigureAwait(false);
    }

    private sealed class PreDecrementExpression(AwkReference target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            await target.ApplyDecrementAsync(context).ConfigureAwait(false);
    }

    private sealed class PostIncrementExpression(AwkReference target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            await target.ApplyPostIncrementAsync(context).ConfigureAwait(false);
    }

    private sealed class PostDecrementExpression(AwkReference target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            await target.ApplyPostDecrementAsync(context).ConfigureAwait(false);
    }
}
