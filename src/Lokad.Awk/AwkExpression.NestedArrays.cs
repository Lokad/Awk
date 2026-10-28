namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    public static AwkExpression ArrayElement(AwkArrayOperand parent, AwkArrayIndex index) =>
        new NestedArrayElementExpression(parent, index);

    public static AwkExpression ArrayMembership(AwkArrayIndex index, AwkArrayOperand array) =>
        new NestedArrayMembershipExpression(index, array);

    private sealed class NestedArrayMembershipExpression(AwkArrayIndex index, AwkArrayOperand operand) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var key = await index.ResolveKeyAsync(context).ConfigureAwait(false);
            using var heldKey = context.State.MemoryAccounting.ReserveTransient(key.RetainedTextMemoryBytes);
            var array = await operand.ResolveAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(array.Slots.ContainsKey(key) ? 1 : 0);
        }
    }

    private sealed class NestedArrayElementExpression(AwkArrayOperand parent, AwkArrayIndex index) : AwkExpression
    {
        private readonly AwkArrayPath _path = new(parent, index);

        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            using var target = await _path.CaptureAsync(context).ConfigureAwait(false);
            return context.State.GetArrayAwkValue(target.ResolveOwner(context.State), target.Key);
        }
    }
}

internal sealed record AwkNestedArrayElementReference(AwkArrayOperand Parent, AwkArrayIndex Index) : AwkReference
{
    private readonly AwkArrayPath _path = new(Parent, Index);

    protected override async ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context)
        => AwkResolvedReference.NestedArrayElement(await _path.CaptureAsync(context).ConfigureAwait(false));
}
