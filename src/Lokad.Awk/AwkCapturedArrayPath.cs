namespace Lokad.Awk;

// Captured keys and their funded owner survive later computation and host suspension.
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
