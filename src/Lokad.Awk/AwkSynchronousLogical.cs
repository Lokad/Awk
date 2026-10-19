namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
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
