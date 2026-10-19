using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract record AwkArrayIndex
{
    public static AwkArrayIndex Empty { get; } = new AwkEmptyArrayIndex();

    public static AwkArrayIndex Simple(AwkExpression expression) =>
        new AwkSimpleArrayIndex(expression);

    public static AwkArrayIndex Composite(IReadOnlyList<AwkExpression> expressions) =>
        expressions.Count == 1
            ? Simple(expressions[0])
            : new AwkCompositeArrayIndex(AwkCollectionSnapshots.Copy(expressions));

    /// <summary>Evaluates every component and returns the byte-native key used by an awk array.</summary>
    public abstract ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context);

    internal virtual ValueTask<AwkArrayKey> ResolveTargetKeyAsync(AwkEvaluationContext context, AwkArrayName array) =>
        ResolveKeyAsync(context);
}

internal sealed record AwkEmptyArrayIndex : AwkArrayIndex
{
    public override ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(new AwkArrayKey(AwkText.Empty));
}

internal sealed record AwkSimpleArrayIndex(AwkExpression Expression) : AwkArrayIndex
{
    public override async ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context)
    {
        var value = await Expression.EvaluateAsync(context).ConfigureAwait(false);
        return context.State.ToArrayKey(value);
    }

    internal override async ValueTask<AwkArrayKey> ResolveTargetKeyAsync(AwkEvaluationContext context, AwkArrayName array)
    {
        if (!AwkExpression.TryGetFieldIndexExpression(Expression, out var indexExpression))
            return await ResolveKeyAsync(context).ConfigureAwait(false);
        // Retain the field expression's checkpoint before evaluating its index.
        context.State.Checkpoint();
        var index = (int)await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
        return context.State.GetArrayFieldKey(array, index);
    }
}

internal sealed record AwkCompositeArrayIndex(IReadOnlyList<AwkExpression> Expressions) : AwkArrayIndex
{
    public override async ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context)
    {
        var separator = context.State.ResolveSubscriptSeparatorText();
        using var builder = new AwkUtf8Builder(context.State.Limits, context.State.MemoryAccounting);
        for (var index = 0; index < Expressions.Count; index++)
        {
            if (index > 0)
                builder.Append(separator, context.State.CancellationCheckpoint);

            var part = context.State.FormatStringByteText(
                await Expressions[index].EvaluateAsync(context).ConfigureAwait(false));
            builder.Append(part, context.State.CancellationCheckpoint);
        }

        return new AwkArrayKey(builder.CopyToAwkText(context.State.Counters, context.State.CancellationCheckpoint), context.State.CancellationCheckpoint);
    }
}
