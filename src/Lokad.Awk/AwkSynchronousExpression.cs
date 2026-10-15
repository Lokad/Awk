namespace Lokad.Awk;

// Prepared CPU-only expression regions. Async/host nodes never enter this tree.
internal abstract class AwkSynchronousExpression : AwkExpression
{
    internal virtual int SynchronousDepth => 1;
    internal virtual int SynchronousCallDepth => 0;
    internal static AwkSynchronousExpression Unavailable { get; } = new UnavailableExpression();

    internal AwkValue EvaluateSynchronous(AwkEvaluationContext context)
    {
        context.State.RecordExecutionStep();
        return EvaluateSynchronousCore(context);
    }

    internal double ResolveNumberSynchronous(AwkEvaluationContext context)
    {
        context.State.RecordExecutionStep();
        return EvaluateNumberSynchronousCore(context);
    }

    protected virtual double EvaluateNumberSynchronousCore(AwkEvaluationContext context) =>
        EvaluateSynchronousCore(context).AsNumber(context.State.CancellationCheckpoint);

    protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
    {
        context.State.Counters.RecordSynchronousExpression();
        return ValueTask.FromResult(EvaluateSynchronousCore(context));
    }

    protected abstract AwkValue EvaluateSynchronousCore(AwkEvaluationContext context);

    private sealed class UnavailableExpression : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => 0;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            throw new InvalidOperationException("No synchronous expression was prepared.");
    }
}

internal abstract partial class AwkExpression
{
    private static AwkOpcode ToNumericOpcode(AwkNumericOperation operation) =>
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

    private static AwkExpression CreateBinary(AwkExpression left, AwkExpression right, AwkNumericOperation operation) =>
        TryGetSynchronousPair(left, right, out var syncLeft, out var syncRight)
            ? new SynchronousBinaryExpression(syncLeft, syncRight, operation)
            : new BinaryExpression(left, right, operation);

    private static bool TryGetSynchronousPair(AwkExpression left, AwkExpression right,
        out AwkSynchronousExpression syncLeft, out AwkSynchronousExpression syncRight)
    {
        syncLeft = left as AwkSynchronousExpression ?? AwkSynchronousExpression.Unavailable;
        syncRight = right as AwkSynchronousExpression ?? AwkSynchronousExpression.Unavailable;
        return syncLeft.SynchronousDepth is > 0 and < 16 && syncRight.SynchronousDepth is > 0 and < 16;
    }

    private sealed class SynchronousBinaryExpression(
        AwkSynchronousExpression left, AwkSynchronousExpression right, AwkNumericOperation operation) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth { get; } = 1 + Math.Max(left.SynchronousDepth, right.SynchronousDepth);
        internal override int SynchronousCallDepth { get; } = Math.Max(left.SynchronousCallDepth, right.SynchronousCallDepth);

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var leftValue = left.ResolveNumberSynchronous(context);
            var rightValue = right.ResolveNumberSynchronous(context);
            return AwkValue.FromNumber(AwkNumericOperations.Apply(leftValue, rightValue, operation));
        }

        protected override double EvaluateNumberSynchronousCore(AwkEvaluationContext context)
        {
            var leftValue = left.ResolveNumberSynchronous(context);
            var rightValue = right.ResolveNumberSynchronous(context);
            return AwkNumericOperations.Apply(leftValue, rightValue, operation);
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right)) return false;
            compiler.Emit(ToNumericOpcode(operation));
            compiler.Replace(popped: 2, pushed: 1);
            return true;
        }
    }

    private sealed class SynchronousComparisonExpression(
        AwkSynchronousExpression left, AwkSynchronousExpression right, AwkComparison comparison) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth { get; } = 1 + Math.Max(left.SynchronousDepth, right.SynchronousDepth);

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var leftValue = left.EvaluateSynchronous(context);
            var rightValue = right.EvaluateSynchronous(context);
            return AwkValue.FromNumber(AwkComparisonOperations.Evaluate(leftValue, rightValue, comparison, context.State) ? 1 : 0);
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(left) || !compiler.TryEmitExpression(right)) return false;
            compiler.EmitComparison(comparison);
            compiler.Replace(popped: 2, pushed: 1);
            return true;
        }
    }
}
