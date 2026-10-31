namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private enum UnaryOperation { Number, Negate, Not }

    private sealed class SynchronousUnaryExpression(
        AwkSynchronousExpression value, UnaryOperation operation) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => value.SynchronousDepth + 1;
        internal override int SynchronousCallDepth => value.SynchronousCallDepth;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var result = value.EvaluateSynchronous(context);
            return AwkValue.FromNumber(operation switch
            {
                UnaryOperation.Not => result.IsTruthy() ? 0 : 1,
                UnaryOperation.Negate => -result.AsNumber(context.State.CancellationCheckpoint),
                _ => result.AsNumber(context.State.CancellationCheckpoint)
            });
        }
    }

    private sealed class SynchronousTernaryExpression(AwkSynchronousExpression condition,
        AwkSynchronousExpression yes, AwkSynchronousExpression no) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => 1 + Math.Max(condition.SynchronousDepth,
            Math.Max(yes.SynchronousDepth, no.SynchronousDepth));
        internal override int SynchronousCallDepth => Math.Max(condition.SynchronousCallDepth,
            Math.Max(yes.SynchronousCallDepth, no.SynchronousCallDepth));

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            condition.EvaluateSynchronous(context).IsTruthy()
                ? yes.EvaluateSynchronous(context) : no.EvaluateSynchronous(context);
    }

    private static AwkExpression CreateLogical(AwkExpression left, AwkExpression right, LogicalOperation operation) =>
        TryGetSynchronousPair(left, right, out var first, out var second)
            ? new SynchronousLogicalExpression(first, second, operation)
            : new LogicalExpression(left, right, operation);

    private sealed class SynchronousLogicalExpression(AwkSynchronousExpression left,
        AwkSynchronousExpression right, LogicalOperation operation) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth { get; } = 1 + Math.Max(left.SynchronousDepth, right.SynchronousDepth);
        internal override int SynchronousCallDepth { get; } = Math.Max(left.SynchronousCallDepth, right.SynchronousCallDepth);
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var truthy = left.EvaluateSynchronous(context).IsTruthy();
            if (operation == LogicalOperation.And && !truthy) return AwkValue.FromNumber(0);
            if (operation == LogicalOperation.Or && truthy) return AwkValue.FromNumber(1);
            return AwkValue.FromNumber(right.EvaluateSynchronous(context).IsTruthy() ? 1 : 0);
        }
    }
}
