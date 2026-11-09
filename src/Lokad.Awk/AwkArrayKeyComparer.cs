using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkArrayKeyComparer : IEqualityComparer<AwkArrayKey>,
    IAlternateEqualityComparer<AwkArrayKey, AwkArrayKey>, IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey>
{
    private readonly Action? _checkpoint;
    private readonly AwkRuntimeCounters _counters;

    private AwkArrayKeyComparer(Action? checkpoint, AwkRuntimeCounters counters)
    {
        _checkpoint = checkpoint;
        _counters = counters;
    }

    // The shared comparer is callback-free. Only runtime-owned tables
    // retain their own execution callback.
    internal static AwkArrayKeyComparer ForRuntime(Action? checkpoint) =>
        checkpoint is null ? Instance : new(checkpoint, AwkRuntimeCounters.Disabled);

    internal static AwkArrayKeyComparer ForRuntime(Action? checkpoint, AwkRuntimeCounters counters) => new(checkpoint, counters);

    public static AwkArrayKeyComparer Instance { get; } =
        new(null, AwkRuntimeCounters.Disabled);

    AwkArrayKey IAlternateEqualityComparer<AwkArrayKey, AwkArrayKey>.Create(AwkArrayKey key) => key;

    // Insertion admits the key and value together at the array owner.
    AwkArrayKey IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey>.Create(ReadOnlySpan<byte> bytes) =>
        throw new InvalidOperationException("Array keys require explicit admitted insertion.");

    public bool Equals(ReadOnlySpan<byte> bytes, AwkArrayKey key)
    {
        if (_counters.IsEnabled) _counters.RecordArrayEquality();
        return key.Equals(bytes, bytes.Length >= AwkRuntimeLimits.CancellationCheckBytes ||
            key.NeedsCancellationCheckpoint ? _checkpoint : null);
    }

    public int GetHashCode(ReadOnlySpan<byte> bytes) => HashBytes(bytes, borrowed: true);

    private int HashBytes(ReadOnlySpan<byte> bytes, bool borrowed)
    {
        var hash = bytes.Length <= AwkArrayKey.InlineByteCapacity
            ? AwkArrayKey.HashShortRuntimeBytes(bytes) : HashLongBytes(bytes);
        if (_counters.IsEnabled) _counters.RecordArrayHash(bytes.Length, borrowed);
        return hash;
    }

    private int HashLongBytes(ReadOnlySpan<byte> bytes)
    {
        var checkpoint = bytes.Length >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null;
        checkpoint?.Invoke();
        var hash = new System.HashCode();
        hash.AddBytes(bytes);
        checkpoint?.Invoke();
        return hash.ToHashCode();
    }

    public bool Equals(AwkArrayKey left, AwkArrayKey right)
    {
        if (_counters.IsEnabled) _counters.RecordArrayEquality();
        return left.Equals(right, left.NeedsCancellationCheckpoint ||
            right.NeedsCancellationCheckpoint ? _checkpoint : null);
    }

    public int GetHashCode(AwkArrayKey key)
    {
        if (key.TryGetInlineRuntimeHash(out var hash, out var length))
        {
            if (_counters.IsEnabled) _counters.RecordArrayHash(length, borrowed: false);
            return hash;
        }
        return HashProjectedKey(key);
    }

    private int HashProjectedKey(AwkArrayKey key)
    {
        // Project once: ordinal formatting is also the actual hashing input.
        // The immutable text constructor already materialized any owned bytes.
        Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        return HashBytes(key.AsSpan(buffer), borrowed: false);
    }
}
