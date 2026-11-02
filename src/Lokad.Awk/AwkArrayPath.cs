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
        using var target = AwkCapturedArrayPath.Begin(state, _root.Name, _root.Binding, _indices.Length);
        for (var index = 0; index < _indices.Length; index++)
        {
            state.Checkpoint();
            target.CaptureKey(state, index, await _indices[index].ResolveKeyAsync(context).ConfigureAwait(false));
        }
        target.Delete(state);
    }

    public ValueTask<AwkCapturedArrayPath> CaptureAsync(AwkEvaluationContext context) =>
        CaptureCoreAsync(context, synchronousIndices: false);

    // This branch resolves every index through its synchronous API; nothing can suspend.
    internal AwkCapturedArrayPath CaptureSynchronous(AwkEvaluationContext context) =>
        CaptureCoreAsync(context, synchronousIndices: true).GetAwaiter().GetResult();

    private async ValueTask<AwkCapturedArrayPath> CaptureCoreAsync(AwkEvaluationContext context, bool synchronousIndices)
    {
        var state = context.State;
        var target = AwkCapturedArrayPath.Begin(state, _root.Name, _root.Binding, _indices.Length);
        try
        {
            for (var index = 0; index < _indices.Length; index++)
            {
                state.Checkpoint();
                var key = synchronousIndices ? _indices[index].ResolveKeySynchronous(context)
                    : await _indices[index].ResolveKeyAsync(context).ConfigureAwait(false);
                target.Append(state, index, key);
            }
            return target;
        }
        catch { target.Dispose(); throw; }
    }
}

// Lexical staging, not retained array ownership. The existing lease funds all keys
// even if a later operand removes the source entries while host work is pending.
internal sealed class AwkCapturedArrayPath(
    AwkArrayName rootName, AwkArrayBinding rootBinding, AwkArrayKey[] keys,
    AwkMemoryLease heldKeys) : IDisposable
{
    private AwkArrayIdentity? _owner;

    internal static AwkCapturedArrayPath Begin(AwkRuntimeState state, AwkArrayName rootName,
        AwkArrayBinding rootBinding, int count)
    {
        var lease = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            64, AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count)));
        try
        {
            state.ThrowIfCancellationRequested();
            return new(rootName, rootBinding, new AwkArrayKey[count], lease);
        }
        catch { lease.Dispose(); throw; }
    }

    // Capture each key and admit its intermediate child before the next key runs.
    // The final owner can be detached by that later computation; repair is logical.
    internal void Append(AwkRuntimeState state, int index, AwkArrayKey key)
    {
        CaptureKey(state, index, key);
        if (index == keys.Length - 1)
        {
            _owner ??= state.ResolveOrdinaryArray(rootName, rootBinding);
            return;
        }
        _owner = index == 0 ? state.GetSubarray(rootName, key, rootBinding)
            : state.GetSubarray(_owner is { IsDetached: false } live ? live
                : ResolveOwner(state, rootName, rootBinding, keys, index), key);
    }
    internal void CaptureKey(AwkRuntimeState state, int index, AwkArrayKey key)
    {
        heldKeys.ReserveAdditional(key.RetainedTextMemoryBytes);
        state.ThrowIfCancellationRequested();
        keys[index] = key;
    }

    internal void Delete(AwkRuntimeState state)
    {
        // Unlike ordinary access, all keys run before searching, without creating children.
        var array = state.FindSubarray(rootName, keys[0], rootBinding);
        for (var index = 1; index < keys.Length - 1 && array is not null; index++)
        {
            state.ThrowIfCancellationRequested();
            array = state.FindSubarray(array, keys[index]);
        }
        state.ThrowIfCancellationRequested();
        if (array is not null) state.DeleteArrayValue(array, keys[^1]);
    }

    public AwkArrayKey Key => keys[^1];

    public AwkArrayIdentity ResolveOwner(AwkRuntimeState state)
    {
        if (_owner is { IsDetached: false } live) return live;
        return _owner = ResolveOwner(state, rootName, rootBinding, keys, keys.Length - 1);
    }

    internal static AwkArrayIdentity ResolveOwner(AwkRuntimeState state, AwkArrayName rootName,
        AwkArrayBinding rootBinding, AwkArrayKey[] keys, int count)
    {
        if (count == 0) return state.ResolveOrdinaryArray(rootName, rootBinding);
        var array = state.GetSubarray(rootName, keys[0], rootBinding);
        for (var index = 1; index < count; index++)
        {
            state.Checkpoint();
            array = state.GetSubarray(array, keys[index]);
        }
        return array;
    }

    public void Dispose() => heldKeys.Dispose();
}
