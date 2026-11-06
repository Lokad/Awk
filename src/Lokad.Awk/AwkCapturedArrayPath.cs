namespace Lokad.Awk;

// Captured keys and their funded owner survive later computation and host suspension.
internal sealed class AwkCapturedArrayPath : IDisposable
{
    private const int OwnerBytes = 96;
    private AwkArrayName _rootName;
    private AwkArrayBinding _rootBinding;
    private AwkArrayKey[] _keys;
    private readonly AwkMemoryLease _heldKeys;
    private long _capacityBytes;
    private int _length;
    private bool _disposed;
    private AwkArrayIdentity? _owner;

    private AwkCapturedArrayPath(AwkArrayName rootName, AwkArrayBinding rootBinding,
        AwkArrayKey[] keys, AwkMemoryLease heldKeys, long capacityBytes)
    {
        _rootName = rootName;
        _rootBinding = rootBinding;
        _keys = keys;
        _heldKeys = heldKeys;
        _capacityBytes = capacityBytes;
        _length = keys.Length;
    }

    internal bool IsActive => _length > 0;

    internal static AwkCapturedArrayPath Begin(AwkRuntimeState state, AwkArrayName rootName,
        AwkArrayBinding rootBinding, int count)
    {
        var capacityBytes = AwkRuntimeMemory.CheckedAdd(OwnerBytes,
            AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count));
        var lease = state.MemoryAccounting.ReserveTransient(capacityBytes);
        try
        {
            state.ThrowIfCancellationRequested();
            var keys = new AwkArrayKey[count];
            state.Counters.RecordCapturedPathKeyBufferAllocation();
            var path = new AwkCapturedArrayPath(rootName, rootBinding, keys, lease, capacityBytes);
            state.Counters.RecordCapturedPathAllocation();
            return path;
        }
        catch { lease.Dispose(); throw; }
    }

    internal void Restart(AwkRuntimeState state, AwkArrayName rootName,
        AwkArrayBinding rootBinding, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Retire();
        if (count > _keys.Length)
        {
            var oldBytes = AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(_keys.Length);
            var nextBytes = AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count);
            var capacityBytes = AwkRuntimeMemory.CheckedAdd(_capacityBytes, nextBytes - oldBytes);
            // Both arrays remain funded until the replacement is published.
            _heldKeys.ReserveAdditional(nextBytes);
            AwkArrayKey[] keys;
            try
            {
                state.ThrowIfCancellationRequested();
                keys = new AwkArrayKey[count];
                state.Counters.RecordCapturedPathKeyBufferAllocation();
                state.ThrowIfCancellationRequested();
            }
            catch { _heldKeys.Release(nextBytes); throw; }
            _keys = keys;
            _capacityBytes = capacityBytes;
            _heldKeys.Release(oldBytes);
        }
        else state.ThrowIfCancellationRequested();
        _rootName = rootName;
        _rootBinding = rootBinding;
        _length = count;
    }

    // Capture each key and admit its intermediate child before the next key runs.
    // The final owner can be detached by that later computation; repair is logical.
    internal void Append(AwkRuntimeState state, int index, AwkArrayKey key)
    {
        CaptureKey(state, index, key);
        if (index == _length - 1)
        {
            _owner ??= state.ResolveOrdinaryArray(_rootName, _rootBinding);
            return;
        }
        _owner = index == 0 ? state.GetSubarray(_rootName, key, _rootBinding)
            : state.GetSubarray(_owner is { IsDetached: false } live ? live
                : ResolveOwner(state, _rootName, _rootBinding, _keys, index), key);
    }
    internal void CaptureKey(AwkRuntimeState state, int index, AwkArrayKey key)
    {
        _heldKeys.ReserveAdditional(key.RetainedTextMemoryBytes);
        state.ThrowIfCancellationRequested();
        _keys[index] = key;
    }

    internal void Delete(AwkRuntimeState state)
    {
        // Unlike ordinary access, all keys run before searching, without creating children.
        var array = state.FindSubarray(_rootName, _keys[0], _rootBinding);
        for (var index = 1; index < _length - 1 && array is not null; index++)
        {
            state.ThrowIfCancellationRequested();
            array = state.FindSubarray(array, _keys[index]);
        }
        state.ThrowIfCancellationRequested();
        if (array is not null) state.DeleteArrayValue(array, Key);
    }

    public AwkArrayKey Key => _keys[_length - 1];

    public AwkArrayIdentity ResolveOwner(AwkRuntimeState state)
    {
        if (_owner is { IsDetached: false } live) return live;
        return _owner = ResolveOwner(state, _rootName, _rootBinding, _keys, _length - 1);
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

    internal void Retire()
    {
        if (!IsActive) return; // Inactive owners retain funded capacity only.
        Array.Clear(_keys, 0, _length);
        _length = 0;
        _owner = null;
        var payload = _heldKeys.Bytes - _capacityBytes;
        if (payload > 0) _heldKeys.Release(payload);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Retire();
        _disposed = true;
        _keys = [];
        _capacityBytes = 0;
        _heldKeys.Dispose();
    }
}
