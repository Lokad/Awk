using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract record AwkArrayIndex
{
    public static AwkArrayIndex Simple(AwkExpression expression) =>
        new AwkSimpleArrayIndex(expression);

    public static AwkArrayIndex Composite(IReadOnlyList<AwkExpression> expressions) =>
        expressions.Count == 1
            ? Simple(expressions[0])
            : new AwkCompositeArrayIndex(AwkCollectionSnapshots.Copy(expressions));

    /// <summary>Evaluates every component and returns the byte-native key used by an awk array.</summary>
    public abstract ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context);

    internal virtual ValueTask<AwkArrayKey> ResolveTargetKeyAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex = -1) =>
        ResolveKeyAsync(context);

    internal virtual ValueTask<AwkArrayUpdateTarget> PrepareUpdateAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex = -1)
    {
        var key = ResolveTargetKeyAsync(context, array, globalIndex);
        return key.IsCompletedSuccessfully
            ? ValueTask.FromResult(context.State.PrepareArrayUpdate(array, key.Result, globalIndex))
            : AwaitUpdateKeyAsync(context, array, globalIndex, key);
    }

    private static async ValueTask<AwkArrayUpdateTarget> AwaitUpdateKeyAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex, ValueTask<AwkArrayKey> pending) =>
        context.State.PrepareArrayUpdate(array, await pending.ConfigureAwait(false), globalIndex);
}

internal sealed record AwkSimpleArrayIndex(AwkExpression Expression) : AwkArrayIndex
{
    internal override ValueTask<AwkArrayUpdateTarget> PrepareUpdateAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex = -1)
    {
        if (!AwkExpression.TryGetFieldIndexExpression(Expression, out var indexExpression))
            return base.PrepareUpdateAsync(context, array, globalIndex);
        context.State.Checkpoint();
        var index = indexExpression.ResolveNumberAsync(context);
        return index.IsCompletedSuccessfully
            ? ValueTask.FromResult(context.State.PrepareArrayFieldUpdate(array, AwkFieldIndex.FromNumber(index.Result).Value, globalIndex))
            : AwaitFieldUpdateAsync(context, array, globalIndex, index);
    }

    private static async ValueTask<AwkArrayUpdateTarget> AwaitFieldUpdateAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex, ValueTask<double> pending) =>
        context.State.PrepareArrayFieldUpdate(array, AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value, globalIndex);

    public override async ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context)
    {
        var value = await Expression.EvaluateAsync(context).ConfigureAwait(false);
        return context.State.ToArrayKey(value);
    }

    internal override async ValueTask<AwkArrayKey> ResolveTargetKeyAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex = -1)
    {
        if (!AwkExpression.TryGetFieldIndexExpression(Expression, out var indexExpression))
            return await ResolveKeyAsync(context).ConfigureAwait(false);
        // Retain the field expression's checkpoint before evaluating its index.
        context.State.Checkpoint();
        var index = AwkFieldIndex.FromNumber(await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false)).Value;
        return context.State.GetArrayFieldKey(array, index, globalIndex);
    }
}

internal sealed record AwkCompositeArrayIndex(IReadOnlyList<AwkExpression> Expressions) : AwkArrayIndex
{
    public override async ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context)
    {
        using var builder = await BuildKeyAsync(context).ConfigureAwait(false);
        return new AwkArrayKey(builder.CopyToAwkText(context.State.Counters, context.State.CancellationCheckpoint),
            context.State.CancellationCheckpoint);
    }

    internal override async ValueTask<AwkArrayKey> ResolveTargetKeyAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex = -1)
    {
        using var builder = await BuildKeyAsync(context).ConfigureAwait(false);
        return context.State.ResolveArrayKey(array, builder, globalIndex);
    }

    internal override async ValueTask<AwkArrayUpdateTarget> PrepareUpdateAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex = -1)
    {
        using var builder = await BuildKeyAsync(context).ConfigureAwait(false);
        return context.State.PrepareArrayUpdate(array, builder, globalIndex);
    }

    private async ValueTask<AwkUtf8Builder> BuildKeyAsync(AwkEvaluationContext context)
    {
        var separator = context.State.ResolveSubscriptSeparatorText();
        using var heldSeparator = context.State.MemoryAccounting.ReserveTransient(separator.EstimatedRetainedMemoryBytes);
        var builder = new AwkUtf8Builder(context.State.MemoryAccounting);
        try
        {
            for (var index = 0; index < Expressions.Count; index++)
            {
                if (index > 0) builder.Append(separator, context.State.CancellationCheckpoint);
                await Expressions[index].AppendByteTextAsync(context, builder).ConfigureAwait(false);
            }
            return builder; // The caller owns admission and disposal after synchronous lookup/copy.
        }
        catch { builder.Dispose(); throw; }
    }
}
