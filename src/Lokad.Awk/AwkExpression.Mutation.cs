using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private static bool TryPrepareSynchronousIncrement(AwkReference target, out Action<AwkEvaluationContext> action)
    {
        if (target.TryGetSynchronousScalar(out var location))
        {
            action = context =>
            {
                var state = context.State;
                state.Checkpoint();
                var previous = location.EvaluateNumber(state);
                location.AssignValue(state, AwkValue.FromNumber(previous + 1));
            };
            return true;
        }
        action = static _ => { };
        return false;
    }

    private sealed class AssignmentExpression(AwkReference target, AwkExpression valueExpression) : AwkExpression
    {
        internal override bool TryGetFieldCopy(out int source, out int destination)
        {
            source = destination = 0;
            return target is AwkFieldReference field &&
                field.FieldIndexExpression.TryGetIntegerConstant(out destination) && destination > 0 &&
                TryGetLiteralFieldIndex(valueExpression, out source);
        }

        internal override bool TryGetLocalAssignment(
            out (AwkVariableName Name, AwkFunctionLocalSlotId Slot) local, out AwkExpression value)
        {
            if (target is AwkLocalScalarReference scalar)
            {
                local = (scalar.Variable, scalar.SlotId);
                value = valueExpression;
                return true;
            }
            return base.TryGetLocalAssignment(out local, out value);
        }

        internal override AwkExpression PrepareSynchronousCallAction(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions) =>
            valueExpression.TryPrepareFunctionCalls(functions, 16, out var value) && !ReferenceEquals(value, valueExpression)
                ? new AssignmentExpression(target, value) : this;

        internal override bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action)
        {
            if (valueExpression is AwkSynchronousExpression { SynchronousDepth: > 0 } value &&
                target.TryGetSynchronousScalar(out var location))
            {
                action = context =>
                {
                    context.State.Checkpoint();
                    location.AssignValue(context.State, value.EvaluateSynchronous(context));
                };
                return true;
            }
            return base.TryPrepareSynchronousAction(out action);
        }

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
        internal override AwkExpression PrepareSynchronousCallAction(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions) =>
            valueExpression.TryPrepareFunctionCalls(functions, 16, out var value) && !ReferenceEquals(value, valueExpression)
                ? new CompoundAssignmentExpression(target, value, operation) : this;

        internal override bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action)
        {
            if (valueExpression is AwkSynchronousExpression { SynchronousDepth: > 0 and <= 16 } value &&
                target.TryGetSynchronousScalar(out var location))
            {
                action = context =>
                {
                    var state = context.State;
                    state.Checkpoint();
                    var previous = location.EvaluateNumber(state);
                    var right = value.EvaluateSynchronous(context).AsNumber(state.CancellationCheckpoint);
                    location.AssignValue(state, AwkValue.FromNumber(
                        AwkNumericOperations.ApplyCompoundAssignment(previous, right, operation)));
                };
                return true;
            }
            action = static _ => { };
            return false;
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyCompoundAssignmentAsync(context, valueExpression, operation);
    }

    private sealed class PreIncrementExpression(AwkReference target) : AwkExpression
    {
        internal override bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action) =>
            TryPrepareSynchronousIncrement(target, out action);

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
        internal override bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action) =>
            TryPrepareSynchronousIncrement(target, out action);

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyPostIncrementAsync(context);
    }

    private sealed class PostDecrementExpression(AwkReference target) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            target.ApplyPostDecrementAsync(context);
    }
}
