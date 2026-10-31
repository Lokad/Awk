namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class SynchronousAssignmentExpression(
        AwkResolvedReference target, AwkSynchronousExpression value) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => value.SynchronousDepth + 1;
        internal override int SynchronousCallDepth => value.SynchronousCallDepth;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var result = value.EvaluateSynchronous(context);
            target.AssignValue(context.State, result);
            return result.AsScalarCopy();
        }
    }

    private sealed class SynchronousCompoundAssignmentExpression(AwkResolvedReference target,
        AwkSynchronousExpression value, AwkCompoundAssignmentOperation operation) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => value.SynchronousDepth + 1;
        internal override int SynchronousCallDepth => value.SynchronousCallDepth;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var state = context.State;
            var previous = target.EvaluateNumber(state);
            var right = value.EvaluateSynchronous(context).AsNumber(state.CancellationCheckpoint);
            var result = AwkValue.FromNumber(AwkNumericOperations.ApplyCompoundAssignment(previous, right, operation));
            target.AssignValue(state, result);
            return result;
        }
    }

    private sealed class SynchronousIncrementExpression(
        AwkResolvedReference target, int delta, bool post) : AwkSynchronousExpression
    {
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var previous = target.EvaluateNumber(context.State);
            var value = AwkValue.FromNumber(previous + delta);
            target.AssignValue(context.State, value);
            return post ? AwkValue.FromNumber(previous) : value;
        }
    }
}
