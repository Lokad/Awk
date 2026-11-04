namespace Lokad.Awk;

// An array context, distinct from reading an element as a scalar or an argument.
// Existing flat expressions keep their prepared/borrowed paths.
internal abstract record AwkArrayOperand
{
    public abstract ValueTask<AwkArrayIdentity> ResolveAsync(AwkEvaluationContext context);
    public abstract ValueTask<AwkArrayTarget> CaptureAsync(AwkEvaluationContext context);
    internal abstract AwkArrayIdentity ResolveSynchronous(AwkEvaluationContext context);
    internal abstract (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null);

    internal sealed record Named(AwkArrayName Name, AwkArrayBinding Binding) : AwkArrayOperand
    {
        public override ValueTask<AwkArrayIdentity> ResolveAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveOrdinaryArray(Name, Binding));

        public override ValueTask<AwkArrayTarget> CaptureAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(new AwkArrayTarget(Name));

        internal override AwkArrayIdentity ResolveSynchronous(AwkEvaluationContext context) =>
            context.State.ResolveOrdinaryArray(Name, Binding);

        internal override (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null) => (1, 0);
    }

    internal sealed record Element : AwkArrayOperand
    {
        internal AwkArrayPath Path { get; }

        public Element(AwkArrayOperand parent, AwkArrayIndex index) : this(new AwkArrayPath(parent, index)) { }

        private Element(AwkArrayPath path) => Path = path;

        public override async ValueTask<AwkArrayTarget> CaptureAsync(AwkEvaluationContext context) =>
            new(await Path.ReadAsync(context).ConfigureAwait(false));

        public override ValueTask<AwkArrayIdentity> ResolveAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            return Path.ResolveAsync(context);
        }

        internal override AwkArrayIdentity ResolveSynchronous(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            return Path.ResolveSynchronous(context);
        }

        internal override (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null)
        {
            var shape = Path.GetSynchronousShape(checkpoint);
            return (shape.Depth > 0 ? shape.Depth + 1 : 0, shape.Calls);
        }
    }
}
