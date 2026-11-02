namespace Lokad.Awk;

// One execution owner. Number scratch needs no value transport; spelling-bearing
// captures own their values and retain only the payload currently stored in each slot.
internal sealed class AwkRegionStorage : IDisposable
{
    private readonly AwkRuntimeState _state;
    private readonly AwkMemoryLease _capacity;
    private readonly AwkMemoryLease _payload;
    private double[] _numbers = [];
    private AwkValue[] _values = [];
    private AwkArrayUpdateTarget[] _targets = [];
    private AwkUtf8Builder[] _builders = [];
    private bool _disposed;

    internal AwkRegionStorage(AwkRuntimeState state)
    {
        _state = state;
        _capacity = state.MemoryAccounting.CreateTransientLease();
        _payload = state.MemoryAccounting.CreateTransientLease();
    }

    internal double[] Numbers => _numbers;
    internal AwkValue[] Values => _values;
    internal AwkArrayUpdateTarget[] Targets => _targets;
    internal AwkUtf8Builder[] Builders => _builders;

    internal void Prepare(int numbers, int values, int targets = 0, int builders = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (numbers <= _numbers.Length && values <= _values.Length && targets <= _targets.Length && builders <= _builders.Length) return;
        var growNumbers = numbers > _numbers.Length;
        var growValues = values > _values.Length;
        var growTargets = targets > _targets.Length;
        var growBuilders = builders > _builders.Length;
        // Unchanged buffers keep their current funding. Admit only the new buffers
        // alongside the old buffers that remain live until publication below.
        var bytes = AwkRuntimeMemory.CheckedAdd(growNumbers ? AwkRuntimeMemory.EstimateDoubleArrayBytes(numbers) : 0,
            AwkRuntimeMemory.CheckedAdd(growValues ? AwkRuntimeMemory.EstimateAwkValueArrayBytes(values) : 0,
                growTargets ? AwkRuntimeMemory.EstimateArrayUpdateTargetArrayBytes(targets) : 0));
        if (growBuilders)
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateUtf8BuilderArrayBytes(builders),
                AwkRuntimeMemory.CheckedMultiply(builders - _builders.Length, AwkRuntimeMemory.Utf8BuilderObjectBytes)));
        var replacedBytes = AwkRuntimeMemory.CheckedAdd(growNumbers ? AwkRuntimeMemory.EstimateDoubleArrayBytes(_numbers.Length) : 0,
            AwkRuntimeMemory.CheckedAdd(growValues ? AwkRuntimeMemory.EstimateAwkValueArrayBytes(_values.Length) : 0,
                growTargets ? AwkRuntimeMemory.EstimateArrayUpdateTargetArrayBytes(_targets.Length) : 0));
        if (growBuilders)
            replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateUtf8BuilderArrayBytes(_builders.Length));
        using var staging = _state.MemoryAccounting.ReserveTransient(bytes);
        _state.ThrowIfCancellationRequested();
        var numeric = growNumbers ? new double[numbers] : _numbers;
        var scalar = growValues ? new AwkValue[values] : _values;
        var captured = growTargets ? new AwkArrayUpdateTarget[targets] : _targets;
        var keyBuffers = _builders;
        if (growBuilders)
        {
            keyBuffers = new AwkUtf8Builder[builders];
            Array.Copy(_builders, keyBuffers, _builders.Length);
            for (var index = _builders.Length; index < builders; index++)
            {
                if ((index & 127) == 127) _state.ThrowIfCancellationRequested();
                keyBuffers[index] = new AwkUtf8Builder(_state.MemoryAccounting, capacity: 0);
            }
        }
        ClearValues();
        _numbers = numeric;
        _values = scalar;
        _targets = captured;
        _builders = keyBuffers;
        if (replacedBytes > 0) _capacity.Release(replacedBytes);
        staging.MoveTo(_capacity, bytes);
    }

    internal void StoreTarget(int index, AwkArrayUpdateTarget target)
    {
        var oldBytes = _state.MemoryAccounting.IsEnabled ? _targets[index].RetainedTargetBytes : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? target.RetainedTargetBytes : 0;
        if (nextBytes > oldBytes) _payload.ReserveAdditional(nextBytes - oldBytes);
        _state.ThrowIfCancellationRequested();
        _targets[index] = target;
        if (oldBytes > nextBytes) _payload.Release(oldBytes - nextBytes);
    }

    internal void ReleaseTarget(int index)
    {
        var bytes = _state.MemoryAccounting.IsEnabled ? _targets[index].RetainedTargetBytes : 0;
        _targets[index] = default;
        if (bytes > 0) _payload.Release(bytes);
    }

    internal void StoreValue(int index, AwkValue value)
    {
        value = value.AsScalarCopy();
        var oldBytes = _state.MemoryAccounting.IsEnabled ? _values[index].RetainedTextMemoryBytes : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? value.RetainedTextMemoryBytes : 0;
        if (nextBytes > oldBytes) _payload.ReserveAdditional(nextBytes - oldBytes);
        _state.ThrowIfCancellationRequested();
        _values[index] = value;
        if (oldBytes > nextBytes) _payload.Release(oldBytes - nextBytes);
    }

    internal void ReleaseValue(int index)
    {
        var bytes = _state.MemoryAccounting.IsEnabled ? _values[index].RetainedTextMemoryBytes : 0;
        _values[index] = default;
        if (bytes > 0) _payload.Release(bytes);
    }

    internal void ClearValues()
    {
        Array.Clear(_values);
        Array.Clear(_targets);
        foreach (var builder in _builders) builder.Clear();
        if (_payload.Bytes > 0) _payload.Release(_payload.Bytes);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearValues();
        _numbers = [];
        _values = [];
        _targets = [];
        foreach (var builder in _builders) builder.Dispose();
        _builders = [];
        _payload.Dispose();
        _capacity.Dispose();
    }
}
