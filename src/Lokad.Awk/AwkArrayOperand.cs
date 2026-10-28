namespace Lokad.Awk;

// An array context, distinct from reading an element as a scalar or an argument.
// Existing flat expressions keep their prepared/borrowed paths.
internal abstract record AwkArrayOperand
{
    public abstract ValueTask<AwkArrayIdentity> ResolveAsync(AwkEvaluationContext context);
    public abstract ValueTask<AwkArrayTarget> CaptureAsync(AwkEvaluationContext context);

    internal sealed record Named(AwkArrayName Name, int GlobalIndex) : AwkArrayOperand
    {
        public override ValueTask<AwkArrayIdentity> ResolveAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveOrdinaryArray(Name, GlobalIndex));

        public override ValueTask<AwkArrayTarget> CaptureAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(new AwkArrayTarget(Name));
    }

    internal sealed record Element(AwkArrayOperand Parent, AwkArrayIndex Index) : AwkArrayOperand
    {
        private readonly AwkArrayPath? _path = Parent is Named ? null : new(Parent, Index);

        public override async ValueTask<AwkArrayTarget> CaptureAsync(AwkEvaluationContext context)
        {
            if (Parent is Named named)
            {
                var key = await Index.ResolveKeyAsync(context).ConfigureAwait(false);
                return new(context.State.GetArrayAwkValue(named.Name, key, named.GlobalIndex));
            }
            using var target = await NestedPath.CaptureAsync(context).ConfigureAwait(false);
            return new(context.State.GetArrayAwkValue(target.ResolveOwner(context.State), target.Key));
        }
        public override async ValueTask<AwkArrayIdentity> ResolveAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            if (Parent is Named named)
            {
                // The first index is evaluated before the root acquires array type;
                // subsequent indices observe the children already created before them.
                var firstKey = await Index.ResolveKeyAsync(context).ConfigureAwait(false);
                return context.State.GetSubarray(named.Name, firstKey, named.GlobalIndex);
            }
            using var target = await NestedPath.CaptureAsync(context).ConfigureAwait(false);
            return context.State.GetSubarray(target.ResolveOwner(context.State), target.Key);
        }

        private AwkArrayPath NestedPath => _path ?? throw new InvalidOperationException("A named parent has no nested path.");
    }
}
