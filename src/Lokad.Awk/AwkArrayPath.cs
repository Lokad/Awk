namespace Lokad.Awk;

// Preparation keeps bracket levels separate from SUBSEP composite indices.
// Delete searches after evaluating all indices. Ordinary access instead admits each
// intermediate child before evaluating the following index.
internal sealed class AwkArrayPath
{
    private readonly AwkArrayOperand.Named _root;
    private readonly AwkArrayIndex[] _indices;

    public AwkArrayPath(AwkArrayOperand parent, AwkArrayIndex lastIndex)
    {
        if (parent is AwkArrayOperand.Named root)
        {
            _root = root;
            _indices = [lastIndex];
            return;
        }
        if (parent is not AwkArrayOperand.Element element)
            throw new InvalidOperationException("An array path requires a named root.");
        _root = element.Path._root;
        _indices = new AwkArrayIndex[element.Path._indices.Length + 1];
        element.Path._indices.CopyTo(_indices, 0);
        _indices[^1] = lastIndex;
    }

    private AwkArrayPath(AwkArrayOperand.Named root, AwkArrayIndex[] indices) =>
        (_root, _indices) = (root, indices);

    internal (int Depth, int Calls) GetSynchronousShape(Action? checkpoint = null)
    {
        var depth = 0;
        var calls = 0;
        foreach (var index in _indices)
        {
            checkpoint?.Invoke();
            var shape = index.GetSynchronousShape(checkpoint);
            if (shape.Depth == 0) return (0, 0);
            depth = Math.Max(depth, shape.Depth);
            calls = Math.Max(calls, shape.Calls);
        }
        return (depth, calls);
    }

    internal bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
        int remainingDepth, out AwkArrayPath prepared, Action? checkpoint)
    {
        prepared = this;
        if (remainingDepth <= 0) return false;
        AwkArrayIndex[]? indices = null;
        for (var offset = 0; offset < _indices.Length; offset++)
        {
            checkpoint?.Invoke();
            if (!_indices[offset].TryPrepareFunctionCalls(functions, remainingDepth, out var index, checkpoint))
                return false;
            if (!ReferenceEquals(index, _indices[offset])) indices ??= (AwkArrayIndex[])_indices.Clone();
            if (indices is not null) indices[offset] = index;
        }
        if (indices is not null) prepared = new(_root, indices);
        return prepared.GetSynchronousShape(checkpoint).Depth > 0;
    }

    internal async ValueTask<AwkValue> ReadAsync(AwkEvaluationContext context)
    {
        if (_indices.Length == 1)
            return context.State.GetArrayAwkValue(_root.Name,
                await _indices[0].ResolveKeyAsync(context).ConfigureAwait(false), _root.Binding);
        using var target = await CaptureAsync(context).ConfigureAwait(false);
        return context.State.GetArrayAwkValue(target.ResolveOwner(context.State), target.Key);
    }

    internal AwkValue ReadSynchronous(AwkEvaluationContext context)
    {
        if (_indices.Length == 1)
            return context.State.GetArrayAwkValue(_root.Name, _indices[0].ResolveKeySynchronous(context), _root.Binding);
        using var target = CaptureSynchronous(context);
        return context.State.GetArrayAwkValue(target.ResolveOwner(context.State), target.Key);
    }

    internal async ValueTask<AwkArrayIdentity> ResolveAsync(AwkEvaluationContext context)
    {
        if (_indices.Length == 1)
            return context.State.GetSubarray(_root.Name,
                await _indices[0].ResolveKeyAsync(context).ConfigureAwait(false), _root.Binding);
        using var target = await CaptureAsync(context).ConfigureAwait(false);
        return context.State.GetSubarray(target.ResolveOwner(context.State), target.Key);
    }

    internal AwkArrayIdentity ResolveSynchronous(AwkEvaluationContext context)
    {
        if (_indices.Length == 1)
            return context.State.GetSubarray(_root.Name, _indices[0].ResolveKeySynchronous(context), _root.Binding);
        using var target = CaptureSynchronous(context);
        return context.State.GetSubarray(target.ResolveOwner(context.State), target.Key);
    }

    public async ValueTask DeleteAsync(AwkEvaluationContext context)
    {
        var state = context.State;
        using var heldKeys = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(_indices.Length));
        var keys = new AwkArrayKey[_indices.Length];
        for (var index = 0; index < _indices.Length; index++)
        {
            state.Checkpoint();
            var key = await _indices[index].ResolveKeyAsync(context).ConfigureAwait(false);
            heldKeys.ReserveAdditional(key.RetainedTextMemoryBytes);
            keys[index] = key;
        }
        var array = state.FindSubarray(_root.Name, keys[0], _root.Binding);
        for (var index = 1; index < keys.Length - 1 && array is not null; index++)
        {
            state.Checkpoint();
            array = state.FindSubarray(array, keys[index]);
        }
        if (array is not null) state.DeleteArrayValue(array, keys[^1]);
    }

    public ValueTask<AwkCapturedArrayPath> CaptureAsync(AwkEvaluationContext context) =>
        CaptureCoreAsync(context, synchronousIndices: false);

    // This branch resolves every index through its synchronous API; nothing can suspend.
    internal AwkCapturedArrayPath CaptureSynchronous(AwkEvaluationContext context) =>
        CaptureCoreAsync(context, synchronousIndices: true).GetAwaiter().GetResult();

    private async ValueTask<AwkCapturedArrayPath> CaptureCoreAsync(AwkEvaluationContext context, bool synchronousIndices)
    {
        var state = context.State;
        var heldKeys = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            64, AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(_indices.Length)));
        try
        {
            var keys = new AwkArrayKey[_indices.Length];
            AwkArrayIdentity? array = null;
            for (var index = 0; index < keys.Length; index++)
            {
                state.Checkpoint();
                var key = synchronousIndices ? _indices[index].ResolveKeySynchronous(context)
                    : await _indices[index].ResolveKeyAsync(context).ConfigureAwait(false);
                heldKeys.ReserveAdditional(key.RetainedTextMemoryBytes);
                keys[index] = key;
                if (index == keys.Length - 1) break;
                array = index == 0
                    ? state.GetSubarray(_root.Name, key, _root.Binding)
                    : state.GetSubarray(array is { IsDetached: false } live
                        ? live : ResolveOwner(state, keys, index), key);
            }
            array ??= state.ResolveOrdinaryArray(_root.Name, _root.Binding);
            return new AwkCapturedArrayPath(this, keys, array, heldKeys);
        }
        catch { heldKeys.Dispose(); throw; }
    }

    internal AwkArrayIdentity ResolveOwner(AwkRuntimeState state, AwkArrayKey[] keys, int count)
    {
        if (count == 0) return state.ResolveOrdinaryArray(_root.Name, _root.Binding);
        var array = state.GetSubarray(_root.Name, keys[0], _root.Binding);
        for (var index = 1; index < count; index++)
        {
            state.Checkpoint();
            array = state.GetSubarray(array, keys[index]);
        }
        return array;
    }
}

// Lexical staging, not retained array ownership. The existing lease funds all keys
// even if a later operand removes the source entries while host work is pending.
internal sealed class AwkCapturedArrayPath(
    AwkArrayPath path, AwkArrayKey[] keys, AwkArrayIdentity owner, AwkMemoryLease heldKeys) : IDisposable
{
    private AwkArrayIdentity _owner = owner;
    public AwkArrayKey Key => keys[^1];

    public AwkArrayIdentity ResolveOwner(AwkRuntimeState state)
    {
        if (_owner.IsDetached) _owner = path.ResolveOwner(state, keys, keys.Length - 1);
        return _owner;
    }

    public void Dispose() => heldKeys.Dispose();
}
