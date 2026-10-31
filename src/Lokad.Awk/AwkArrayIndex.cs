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

    internal virtual (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null) => (0, 0);
    internal virtual AwkArrayKey ResolveKeySynchronous(AwkEvaluationContext context) =>
        throw new InvalidOperationException("Synchronous array index is required.");
    internal virtual bool TryPrepareFunctionCalls(
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
        int remainingDepth, out AwkArrayIndex prepared, Action? checkpoint)
    {
        prepared = this;
        return false;
    }

    internal virtual async ValueTask<AwkValue> ReadAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        context.State.GetArrayAwkValue(array, await ResolveKeyAsync(context).ConfigureAwait(false), globalIndex);

    internal virtual ValueTask<double> ReadNumberAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex)
    {
        var key = ResolveKeyAsync(context);
        return key.IsCompletedSuccessfully
            ? ValueTask.FromResult(context.State.GetArrayNumber(array, key.Result, globalIndex))
            : AwaitNumberAsync(context, array, globalIndex, key);
    }

    private static async ValueTask<double> AwaitNumberAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex, ValueTask<AwkArrayKey> pending) =>
        context.State.GetArrayNumber(array, await pending.ConfigureAwait(false), globalIndex);

    internal virtual async ValueTask<bool> ContainsAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        context.State.ContainsArrayValue(array, await ResolveKeyAsync(context).ConfigureAwait(false), globalIndex);

    internal virtual AwkValue ReadSynchronous(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        context.State.GetArrayAwkValue(array, ResolveKeySynchronous(context), globalIndex);

    internal virtual double ReadNumberSynchronous(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        context.State.GetArrayNumber(array, ResolveKeySynchronous(context), globalIndex);

    internal virtual bool ContainsSynchronous(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        context.State.ContainsArrayValue(array, ResolveKeySynchronous(context), globalIndex);

    protected static bool TryPrepareOperand(AwkExpression operand,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
        int remainingDepth, out AwkExpression prepared, Action? checkpoint)
    {
        checkpoint?.Invoke();
        prepared = operand;
        if (operand is AwkSynchronousExpression { SynchronousDepth: > 0 and < 16 }) return true;
        if (AwkExpression.TryGetFieldIndexExpression(operand, out var fieldIndex))
        {
            if (remainingDepth <= 1 ||
                !fieldIndex.TryPrepareFunctionCalls(functions, remainingDepth - 1, out var index, checkpoint)) return false;
            prepared = AwkExpression.FieldIndex(index);
            return true;
        }
        if (!operand.TryPrepareFunctionCalls(functions, remainingDepth, out var expression, checkpoint)) return false;
        prepared = expression;
        return true;
    }

    internal virtual ValueTask<AwkArrayKey> ResolveTargetKeyAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex = -1) =>
        ResolveKeyAsync(context);

    internal virtual AwkArrayUpdateTarget PrepareUpdateSynchronous(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex = -1) =>
        context.State.PrepareArrayUpdate(array, ResolveKeySynchronous(context), globalIndex);

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
    internal override (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null) =>
        Expression is AwkSynchronousExpression { SynchronousDepth: > 0 and < 16 } expression
            ? (expression.SynchronousDepth, expression.SynchronousCallDepth) : (0, 0);

    internal override bool TryPrepareFunctionCalls(
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
        int remainingDepth, out AwkArrayIndex prepared, Action? checkpoint)
    {
        prepared = this;
        if (!TryPrepareOperand(Expression, functions, remainingDepth, out var operand, checkpoint)) return false;
        if (!ReferenceEquals(operand, Expression)) prepared = new AwkSimpleArrayIndex(operand);
        return true;
    }

    internal override AwkArrayKey ResolveKeySynchronous(AwkEvaluationContext context) =>
        Expression is AwkSynchronousExpression expression
            ? context.State.ToArrayKey(expression.EvaluateSynchronous(context))
            : throw new InvalidOperationException("Synchronous array index is required.");

    private int ResolveFieldSynchronous(AwkEvaluationContext context)
    {
        context.State.Checkpoint(); // Field expression, before its index.
        return AwkExpression.TryGetFieldIndexExpression(Expression, out var operand) && operand is AwkSynchronousExpression index
            ? AwkFieldIndex.FromNumber(index.ResolveNumberSynchronous(context)).Value
            : throw new InvalidOperationException("Synchronous field index is required.");
    }

    internal override AwkValue ReadSynchronous(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        AwkExpression.TryGetFieldIndexExpression(Expression, out _)
            ? context.State.GetArrayFieldValue(array, ResolveFieldSynchronous(context), globalIndex)
            : base.ReadSynchronous(context, array, globalIndex);

    internal override double ReadNumberSynchronous(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        AwkExpression.TryGetFieldIndexExpression(Expression, out _)
            ? context.State.GetArrayFieldNumber(array, ResolveFieldSynchronous(context), globalIndex)
            : base.ReadNumberSynchronous(context, array, globalIndex);

    internal override bool ContainsSynchronous(AwkEvaluationContext context, AwkArrayName array, int globalIndex) =>
        AwkExpression.TryGetFieldIndexExpression(Expression, out _)
            ? context.State.ContainsArrayField(array, ResolveFieldSynchronous(context), globalIndex)
            : base.ContainsSynchronous(context, array, globalIndex);

    internal override ValueTask<AwkValue> ReadAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex)
    {
        if (!AwkExpression.TryGetFieldIndexExpression(Expression, out var index))
            return base.ReadAsync(context, array, globalIndex);
        context.State.Checkpoint();
        return AwaitFieldReadAsync(context, array, globalIndex, index.ResolveNumberAsync(context));
    }

    private static async ValueTask<AwkValue> AwaitFieldReadAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex, ValueTask<double> pending) =>
        context.State.GetArrayFieldValue(array, AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value, globalIndex);

    internal override ValueTask<double> ReadNumberAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex)
    {
        if (!AwkExpression.TryGetFieldIndexExpression(Expression, out var index))
            return base.ReadNumberAsync(context, array, globalIndex);
        context.State.Checkpoint();
        var pending = index.ResolveNumberAsync(context);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(context.State.GetArrayFieldNumber(array, AwkFieldIndex.FromNumber(pending.Result).Value, globalIndex))
            : AwaitFieldNumberAsync(context, array, globalIndex, pending);
    }

    private static async ValueTask<double> AwaitFieldNumberAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex, ValueTask<double> pending) =>
        context.State.GetArrayFieldNumber(array, AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value, globalIndex);

    internal override ValueTask<bool> ContainsAsync(AwkEvaluationContext context, AwkArrayName array, int globalIndex)
    {
        if (!AwkExpression.TryGetFieldIndexExpression(Expression, out var index))
            return base.ContainsAsync(context, array, globalIndex);
        context.State.Checkpoint();
        return AwaitFieldContainsAsync(context, array, globalIndex, index.ResolveNumberAsync(context));
    }

    private static async ValueTask<bool> AwaitFieldContainsAsync(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex, ValueTask<double> pending) =>
        context.State.ContainsArrayField(array, AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value, globalIndex);

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

    internal override AwkArrayUpdateTarget PrepareUpdateSynchronous(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex = -1) =>
        AwkExpression.TryGetFieldIndexExpression(Expression, out _)
            ? context.State.PrepareArrayFieldUpdate(array, ResolveFieldSynchronous(context), globalIndex)
            : base.PrepareUpdateSynchronous(context, array, globalIndex);

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
    internal override (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null)
    {
        var depth = 0; var calls = 0;
        foreach (var operand in Expressions)
        {
            checkpoint?.Invoke();
            if (operand is not AwkSynchronousExpression { SynchronousDepth: > 0 and < 16 } expression) return (0, 0);
            depth = Math.Max(depth, expression.SynchronousDepth);
            calls = Math.Max(calls, expression.SynchronousCallDepth);
        }
        return (depth, calls);
    }

    internal override bool TryPrepareFunctionCalls(
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
        int remainingDepth, out AwkArrayIndex prepared, Action? checkpoint)
    {
        prepared = this;
        var operands = new AwkExpression[Expressions.Count];
        var changed = false;
        for (var index = 0; index < operands.Length; index++)
        {
            if (!TryPrepareOperand(Expressions[index], functions, remainingDepth, out operands[index], checkpoint)) return false;
            changed |= !ReferenceEquals(operands[index], Expressions[index]);
        }
        if (changed) prepared = new AwkCompositeArrayIndex(operands);
        return true;
    }

    internal override AwkArrayKey ResolveKeySynchronous(AwkEvaluationContext context)
    {
        using var builder = BuildKeySynchronous(context);
        return new AwkArrayKey(builder.CopyToAwkText(context.State.Counters, context.State.CancellationCheckpoint),
            context.State.CancellationCheckpoint);
    }

    internal override AwkArrayUpdateTarget PrepareUpdateSynchronous(AwkEvaluationContext context,
        AwkArrayName array, int globalIndex = -1)
    {
        using var builder = BuildKeySynchronous(context);
        return context.State.PrepareArrayUpdate(array, builder, globalIndex);
    }

    private AwkUtf8Builder BuildKeySynchronous(AwkEvaluationContext context)
    {
        var separator = context.State.ResolveSubscriptSeparatorText();
        using var heldSeparator = context.State.MemoryAccounting.ReserveTransient(separator.EstimatedRetainedMemoryBytes);
        var builder = new AwkUtf8Builder(context.State.MemoryAccounting);
        try
        {
            for (var index = 0; index < Expressions.Count; index++)
            {
                if (index > 0) builder.Append(separator, context.State.CancellationCheckpoint);
                if (!Expressions[index].TryResolveSynchronousByteText(context, out var operand))
                    throw new InvalidOperationException("Synchronous array index is required.");
                builder.Append(operand.AsSpan(context.State.CancellationCheckpoint), context.State.CancellationCheckpoint);
            }
            return builder;
        }
        catch { builder.Dispose(); throw; }
    }

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
