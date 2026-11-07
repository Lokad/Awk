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
            key.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null);
    }

    public int GetHashCode(ReadOnlySpan<byte> bytes)
    {
        var checkpoint = bytes.Length >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null;
        checkpoint?.Invoke();
        var hash = new System.HashCode();
        hash.AddBytes(bytes);
        checkpoint?.Invoke();
        if (_counters.IsEnabled) _counters.RecordArrayHash(bytes.Length, borrowed: true);
        return hash.ToHashCode();
    }

    public bool Equals(AwkArrayKey left, AwkArrayKey right)
    {
        if (_counters.IsEnabled) _counters.RecordArrayEquality();
        return left.Equals(right, left.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ||
            right.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null);
    }

    public int GetHashCode(AwkArrayKey key)
    {
        // Small keys retain bounded initialization/inspection behavior. Long hashes
        // check before and after the framework call, before dictionary mutation.
        var bytes = key.ByteLength;
        var hash = key.GetRandomizedHashCode(bytes >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null);
        if (_counters.IsEnabled) _counters.RecordArrayHash(bytes, borrowed: false);
        return hash;
    }
}
