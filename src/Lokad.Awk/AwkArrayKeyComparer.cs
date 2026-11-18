namespace Lokad.Awk;

internal sealed class AwkArrayKeyComparer : IEqualityComparer<AwkArrayKey>,
    IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey>
{
    private readonly Action? _checkpoint;

    private AwkArrayKeyComparer(Action? checkpoint)
    {
        _checkpoint = checkpoint;

    }

    // The shared comparer is callback-free. Only runtime-owned tables
    // retain their own execution callback.
    internal static AwkArrayKeyComparer ForRuntime(Action? checkpoint) =>
        checkpoint is null ? Instance : new(checkpoint);

    public static AwkArrayKeyComparer Instance { get; } =
        new(null);

    // Insertion admits the key and value together at the array owner.
    AwkArrayKey IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey>.Create(ReadOnlySpan<byte> bytes) =>
        throw new InvalidOperationException("Array keys require explicit admitted insertion.");

    public bool Equals(ReadOnlySpan<byte> bytes, AwkArrayKey key)
    {

        return key.Equals(bytes, bytes.Length >= AwkRuntimeLimits.CancellationCheckBytes ||
            key.NeedsCancellationCheckpoint ? _checkpoint : null);
    }

    public int GetHashCode(ReadOnlySpan<byte> bytes) => HashBytes(bytes);

    private int HashBytes(ReadOnlySpan<byte> bytes)
    {
        var hash = bytes.Length <= AwkArrayKey.InlineByteCapacity
            ? AwkArrayKey.HashShortRuntimeBytes(bytes) : HashLongBytes(bytes);

        return hash;
    }

    private int HashLongBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= AwkRuntimeLimits.CancellationCheckBytes)
            return HashLargeBytes(bytes);
        var hash = new System.HashCode();
        hash.AddBytes(bytes);
        return hash.ToHashCode();
    }

    // Keep bounded progress and its callback out of ordinary key hashing. Full
    // windows are aligned; the final tail preserves the one-shot byte sequence.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private int HashLargeBytes(ReadOnlySpan<byte> bytes)
    {
        var checkpoint = _checkpoint;
        checkpoint?.Invoke();
        var hash = new System.HashCode();
        while (bytes.Length > AwkRuntimeLimits.CancellationCheckBytes)
        {
            hash.AddBytes(bytes[..AwkRuntimeLimits.CancellationCheckBytes]);
            bytes = bytes[AwkRuntimeLimits.CancellationCheckBytes..];
            checkpoint?.Invoke();
        }
        hash.AddBytes(bytes);
        checkpoint?.Invoke();
        return hash.ToHashCode();
    }

    public bool Equals(AwkArrayKey left, AwkArrayKey right)
    {

        return left.Equals(right, left.NeedsCancellationCheckpoint ||
            right.NeedsCancellationCheckpoint ? _checkpoint : null);
    }

    public int GetHashCode(AwkArrayKey key)
    {
        if (key.TryGetInlineRuntimeHash(out var hash, out _))
        {

            return hash;
        }
        return HashProjectedKey(key);
    }

    private int HashProjectedKey(AwkArrayKey key)
    {
        // Project once: ordinal formatting is also the actual hashing input.
        // The immutable text constructor already materialized any owned bytes.
        Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        return HashBytes(key.AsSpan(buffer));
    }
}
