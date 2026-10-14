using System.Buffers;

namespace Lokad.Awk;

internal readonly struct AwkValueBuffer(
    AwkValue[]? values,
    AwkMemoryLease? lease,
    bool pooled) : IDisposable
{
    public AwkValue[] Values => values ??
        throw new InvalidOperationException("The default value buffer has no array.");

    public static AwkValueBuffer Rent(int count, AwkMemoryAccounting governor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(governor);
        if (count == 0)
            return new AwkValueBuffer([], AwkMemoryLease.Disabled, pooled: false);
        if (!governor.IsEnabled)
            return new AwkValueBuffer(ArrayPool<AwkValue>.Shared.Rent(count), AwkMemoryLease.Disabled, pooled: true);

        var lease = governor.ReserveTransient(AwkRuntimeMemory.EstimateAwkValueArrayBytes(count));
        try
        {
            return new AwkValueBuffer(new AwkValue[count], lease, pooled: false);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (values is not null)
        {
            if (pooled)
                ArrayPool<AwkValue>.Shared.Return(values, clearArray: true);
            else
                Array.Clear(values);
        }
        lease?.Dispose();
    }
}
