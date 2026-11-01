using System.Buffers;

namespace Lokad.Awk;

// Only AwkValue and AwkFunctionArgument staging use this storage. Language binding
// and frame activation stay with their callers; the execution owns both typed stores.
internal readonly struct AwkArgumentBuffer<T>(
    T[]? values, AwkMemoryLease? memory, AwkArgumentStorage<T>? storage) : IDisposable
{
    public T[] Values => values ??
        throw new InvalidOperationException("The default argument buffer has no array.");

    public void Retain(AwkValue value) => memory?.ReserveAdditional(value.RetainedTextMemoryBytes);

    internal static AwkArgumentBuffer<T> RentPooled(int count) =>
        new(count == 0 ? [] : ArrayPool<T>.Shared.Rent(count), AwkMemoryLease.Disabled, storage: null);

    public void Dispose()
    {
        if (values is { Length: > 0 })
        {
            if (storage is not null && memory is not null)
            {
                Array.Clear(values);
                storage.Return(values, memory);
                return;
            }
            ArrayPool<T>.Shared.Return(values, clearArray: true);
        }
        memory?.Dispose();
    }
}

internal sealed class AwkArgumentStorage<T>(
    AwkMemoryAccounting accounting, Func<int, long> estimateCapacity) : IDisposable
{
    private T[]? _inactive;
    private AwkMemoryLease? _inactiveMemory;

    public AwkArgumentBuffer<T> Rent(int count, Action checkpoint, out bool reused)
    {
        if (_inactive is { } values && _inactiveMemory is { } memory && values.Length == count)
        {
            _inactive = null;
            _inactiveMemory = null;
            reused = true;
            return new(values, memory, this);
        }

        // Normal size retirement, before admission. Never evict/retry after refusal.
        RetireInactive();
        var lease = accounting.ReserveTransient(estimateCapacity(count));
        try
        {
            checkpoint(); // Admission can itself request cancellation; do not then allocate.
            var buffer = new AwkArgumentBuffer<T>(new T[count], lease, this);
            reused = false;
            return buffer;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    internal void Return(T[] values, AwkMemoryLease memory)
    {
        // The caller cleared the array after its actual users finished.
        memory.Release(memory.Bytes - estimateCapacity(values.Length));
        if (_inactive is null)
        {
            _inactive = values;
            _inactiveMemory = memory;
        }
        else memory.Dispose();
    }

    private void RetireInactive()
    {
        _inactive = null;
        _inactiveMemory?.Dispose();
        _inactiveMemory = null;
    }

    public void Dispose() => RetireInactive();
}
