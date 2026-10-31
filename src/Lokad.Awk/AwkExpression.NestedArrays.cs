namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    public static AwkExpression ArrayElement(AwkArrayOperand parent, AwkArrayIndex index) =>
        new NestedArrayElementExpression(new(parent, index));

    public static AwkExpression ArrayMembership(AwkArrayIndex index, AwkArrayOperand array) =>
        new NestedArrayMembershipExpression(index, array);

    private sealed class NestedArrayMembershipExpression(AwkArrayIndex index, AwkArrayOperand operand) : AwkSynchronousExpression
    {
        private readonly (int Depth, int Calls) _shape = Shape(index, operand);
        private static (int Depth, int Calls) Shape(AwkArrayIndex index, AwkArrayOperand operand)
        {
            var key = index.GetSynchronousShape();
            var array = operand.GetSynchronousShape();
            return key.Depth == 0 || array.Depth == 0 ? (0, 0)
                : (1 + Math.Max(key.Depth, array.Depth), Math.Max(key.Calls, array.Calls));
        }

        internal override int SynchronousDepth => _shape.Depth;
        internal override int SynchronousCallDepth => _shape.Calls;

        internal override bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkSynchronousExpression expression, Action? preparationCheckpoint = null)
        {
            expression = this;
            if (remainingDepth <= 0) return false;
            if (SynchronousDepth > 0) return true;
            if (remainingDepth <= 1 ||
                !index.TryPrepareFunctionCalls(functions, remainingDepth - 1, out var preparedIndex, preparationCheckpoint) ||
                !operand.TryPrepareFunctionCalls(functions, remainingDepth - 1, out var preparedOperand, preparationCheckpoint))
                return false;
            expression = new NestedArrayMembershipExpression(preparedIndex, preparedOperand);
            return expression.SynchronousDepth is > 0 and <= 16;
        }

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            EvaluateMembershipAsync(context, synchronousOperands: true).GetAwaiter().GetResult();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            EvaluateMembershipAsync(context, synchronousOperands: false);

        private async ValueTask<AwkValue> EvaluateMembershipAsync(AwkEvaluationContext context, bool synchronousOperands)
        {
            // Both true branches use synchronous APIs: the prepared operation cannot suspend.
            var key = synchronousOperands ? index.ResolveKeySynchronous(context)
                : await index.ResolveKeyAsync(context).ConfigureAwait(false);
            using var heldKey = context.State.MemoryAccounting.ReserveTransient(key.RetainedTextMemoryBytes);
            var array = synchronousOperands ? operand.ResolveSynchronous(context)
                : await operand.ResolveAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(array.Slots.ContainsKey(key) ? 1 : 0);
        }
    }

    private sealed class NestedArrayElementExpression(AwkArrayPath path) : AwkSynchronousExpression
    {
        private readonly (int Depth, int Calls) _shape = path.GetSynchronousShape();
        internal override int SynchronousDepth => _shape.Depth > 0 ? _shape.Depth + 1 : 0;
        internal override int SynchronousCallDepth => _shape.Calls;

        internal override bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkSynchronousExpression expression, Action? preparationCheckpoint = null)
        {
            expression = this;
            if (remainingDepth <= 0) return false;
            if (SynchronousDepth > 0) return true;
            if (remainingDepth <= 1 ||
                !path.TryPrepareFunctionCalls(functions, remainingDepth - 1, out var prepared, preparationCheckpoint)) return false;
            expression = new NestedArrayElementExpression(prepared);
            return expression.SynchronousDepth is > 0 and <= 16;
        }

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) => path.ReadSynchronous(context);
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => path.ReadAsync(context);
    }
}

internal sealed record AwkNestedArrayElementReference : AwkReference
{
    private readonly AwkArrayPath _path;

    public AwkNestedArrayElementReference(AwkArrayOperand parent, AwkArrayIndex index) : this(new AwkArrayPath(parent, index)) { }
    private AwkNestedArrayElementReference(AwkArrayPath path) => _path = path;

    internal override (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null)
    {
        var shape = _path.GetSynchronousShape(checkpoint);
        return (shape.Depth > 0 ? shape.Depth + 1 : 0, shape.Calls);
    }

    internal override bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
        int remainingDepth, out AwkReference prepared, Action? checkpoint)
    {
        prepared = this;
        if (remainingDepth <= 1 || !_path.TryPrepareFunctionCalls(functions, remainingDepth - 1, out var path, checkpoint))
            return false;
        if (!ReferenceEquals(path, _path)) prepared = new AwkNestedArrayElementReference(path);
        return true;
    }

    internal override void AssignValueSynchronous(AwkEvaluationContext context, AwkValue value)
    {
        using var heldValue = context.State.MemoryAccounting.RetainValue(value);
        using var target = AwkResolvedReference.NestedArrayElement(_path.CaptureSynchronous(context));
        target.AssignValue(context.State, value);
    }

    internal override AwkValue ApplyCompoundAssignmentSynchronous(AwkEvaluationContext context,
        AwkSynchronousExpression value, AwkCompoundAssignmentOperation operation)
    {
        using var target = AwkResolvedReference.NestedArrayElement(_path.CaptureSynchronous(context));
        using var heldKey = context.State.MemoryAccounting.ReserveTransient(target.RetainedKeyBytes);
        var previous = target.ReadNumber(context.State);
        return AssignCompound(context, target, previous, value.ResolveNumberSynchronous(context), operation);
    }

    internal override AwkValue ApplyNumericUpdateSynchronous(AwkEvaluationContext context, int delta, bool post) =>
        ApplyNumericUpdate(context, AwkResolvedReference.NestedArrayElement(_path.CaptureSynchronous(context)),
            delta > 0 ? AwkNumericOperations.Increment : AwkNumericOperations.Decrement,
            post ? AwkNumericUpdateResult.Previous : AwkNumericUpdateResult.Updated);

    protected override async ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context) =>
        AwkResolvedReference.NestedArrayElement(await _path.CaptureAsync(context).ConfigureAwait(false));
}
