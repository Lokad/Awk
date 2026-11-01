using System.Buffers;

namespace Lokad.Awk;

internal readonly struct AwkValueBuffer(
    AwkValue[]? values,
    AwkMemoryLease? lease,
    AwkRuntimeState? owner) : IDisposable
{
    public AwkValue[] Values => values ??
        throw new InvalidOperationException("The default value buffer has no array.");

    public void Set(int index, AwkValue value)
    {
        lease?.ReserveAdditional(value.RetainedTextMemoryBytes);
        Values[index] = value;
    }

    public static AwkValueBuffer Rent(int count, AwkRuntimeState state)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(state);
        if (count == 0)
            return new AwkValueBuffer([], AwkMemoryLease.Disabled, owner: null);
        return state.MemoryAccounting.IsEnabled
            ? state.RentGovernedArguments(count)
            : new AwkValueBuffer(ArrayPool<AwkValue>.Shared.Rent(count), AwkMemoryLease.Disabled, owner: null);
    }

    public void Dispose()
    {
        if (values is { Length: > 0 })
        {
            if (owner is not null && lease is not null)
            {
                Array.Clear(values);
                owner.ReturnArguments(values, lease);
                return;
            }
            ArrayPool<AwkValue>.Shared.Return(values, clearArray: true);
        }
        lease?.Dispose();
    }
}
