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
    private bool _disposed;

    internal AwkRegionStorage(AwkRuntimeState state)
    {
        _state = state;
        _capacity = state.MemoryAccounting.CreateTransientLease();
        _payload = state.MemoryAccounting.CreateTransientLease();
    }

    internal double[] Numbers => _numbers;
    internal AwkValue[] Values => _values;

    internal void Prepare(int numbers, int values)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (numbers <= _numbers.Length && values <= _values.Length) return;
        var nextNumbers = Math.Max(numbers, _numbers.Length);
        var nextValues = Math.Max(values, _values.Length);
        var bytes = checked((long)nextNumbers * sizeof(double) + 24 + AwkRuntimeMemory.EstimateAwkValueArrayBytes(nextValues));
        using var staging = _state.MemoryAccounting.ReserveTransient(bytes);
        _state.ThrowIfCancellationRequested();
        var numeric = nextNumbers == _numbers.Length ? _numbers : new double[nextNumbers];
        var scalar = nextValues == _values.Length ? _values : new AwkValue[nextValues];
        ClearValues();
        var oldBytes = _capacity.Bytes;
        _numbers = numeric;
        _values = scalar;
        if (oldBytes > 0) _capacity.Release(oldBytes);
        staging.MoveTo(_capacity, bytes);
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

    internal void ClearValues()
    {
        Array.Clear(_values);
        if (_payload.Bytes > 0) _payload.Release(_payload.Bytes);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearValues();
        _numbers = [];
        _values = [];
        _payload.Dispose();
        _capacity.Dispose();
    }
}
