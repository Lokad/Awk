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
        var count = 1;
        var current = parent;
        while (current is AwkArrayOperand.Element element)
        {
            count++;
            current = element.Parent;
        }
        _root = current as AwkArrayOperand.Named ?? throw new InvalidOperationException("An array path requires a named root.");
        _indices = new AwkArrayIndex[count];
        _indices[^1] = lastIndex;
        var offset = count - 2;
        while (parent is AwkArrayOperand.Element element)
        {
            _indices[offset--] = element.Index;
            parent = element.Parent;
        }
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
        var array = state.FindSubarray(_root.Name, keys[0], _root.GlobalIndex);
        for (var index = 1; index < keys.Length - 1 && array is not null; index++)
        {
            state.Checkpoint();
            array = state.FindSubarray(array, keys[index]);
        }
        if (array is not null) state.DeleteArrayValue(array, keys[^1]);
    }

    public async ValueTask<AwkCapturedArrayPath> CaptureAsync(AwkEvaluationContext context)
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
                var key = await _indices[index].ResolveKeyAsync(context).ConfigureAwait(false);
                heldKeys.ReserveAdditional(key.RetainedTextMemoryBytes);
                keys[index] = key;
                if (index == keys.Length - 1) break;
                array = index == 0
                    ? state.GetSubarray(_root.Name, key, _root.GlobalIndex)
                    : state.GetSubarray(array is { IsDetached: false } live
                        ? live : ResolveOwner(state, keys, index), key);
            }
            if (array is null) throw new InvalidOperationException("A nested element requires a child array.");
            return new AwkCapturedArrayPath(this, keys, array, heldKeys);
        }
        catch { heldKeys.Dispose(); throw; }
    }

    internal AwkArrayIdentity ResolveOwner(AwkRuntimeState state, AwkArrayKey[] keys, int count)
    {
        var array = state.GetSubarray(_root.Name, keys[0], _root.GlobalIndex);
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
