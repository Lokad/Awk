namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class SynchronousAssignmentExpression(
        AwkReference target, AwkSynchronousExpression value, Action? checkpoint = null) : AwkSynchronousExpression
    {
        private readonly (int Depth, int Calls) _targetShape = target.GetSynchronousShape(checkpoint);
        internal override int SynchronousDepth => Math.Max(_targetShape.Depth, value.SynchronousDepth) + 1;
        internal override int SynchronousCallDepth => Math.Max(_targetShape.Calls, value.SynchronousCallDepth);

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var result = value.EvaluateSynchronous(context);
            target.AssignValueSynchronous(context, result);
            return result.AsScalarCopy();
        }
    }

    private sealed class SynchronousCompoundAssignmentExpression(AwkReference target,
        AwkSynchronousExpression value, AwkCompoundAssignmentOperation operation,
        Action? checkpoint = null) : AwkSynchronousExpression
    {
        private readonly (int Depth, int Calls) _targetShape = target.GetSynchronousShape(checkpoint);
        internal override int SynchronousDepth => Math.Max(_targetShape.Depth, value.SynchronousDepth) + 1;
        internal override int SynchronousCallDepth => Math.Max(_targetShape.Calls, value.SynchronousCallDepth);

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            target.ApplyCompoundAssignmentSynchronous(context, value, operation);
    }

    private sealed class SynchronousIncrementExpression(
        AwkReference target, int delta, bool post, Action? checkpoint = null) : AwkSynchronousExpression
    {
        private readonly (int Depth, int Calls) _targetShape = target.GetSynchronousShape(checkpoint);
        internal override int SynchronousDepth => _targetShape.Depth + 1;
        internal override int SynchronousCallDepth => _targetShape.Calls;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            target.ApplyNumericUpdateSynchronous(context, delta, post);
    }
}
