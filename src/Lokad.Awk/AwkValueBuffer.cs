namespace Lokad.Awk;

internal readonly struct AwkValueBuffer(AwkArgumentBuffer<AwkValue> buffer) : IDisposable
{
    public AwkValue[] Values => buffer.Values;

    public void Set(int index, AwkValue value)
    {
        buffer.Retain(value);
        Values[index] = value;
    }

    public static AwkValueBuffer Rent(int count, AwkRuntimeState state)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(state);
        return new(state.RentValueArguments(count));
    }

    public void Dispose() => buffer.Dispose();
}
