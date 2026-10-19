using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkArrayKeyComparer : IEqualityComparer<AwkArrayKey>,
    IAlternateEqualityComparer<AwkArrayKey, AwkArrayKey>, IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey>
{
    private readonly Action? _checkpoint;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkMemoryAccounting _accounting;

    private AwkArrayKeyComparer(Action? checkpoint, AwkRuntimeCounters counters, AwkMemoryAccounting accounting)
    {
        _checkpoint = checkpoint;
        _counters = counters;
        _accounting = accounting;
    }

    // The shared comparer is callback-free. Only runtime-owned dictionaries
    // retain their own execution callback.
    internal static AwkArrayKeyComparer ForRuntime(Action? checkpoint) =>
        checkpoint is null ? Instance : new(checkpoint, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled);

    internal static AwkArrayKeyComparer ForRuntime(Action? checkpoint, AwkRuntimeCounters counters,
        AwkMemoryAccounting accounting) => new(checkpoint, counters, accounting);

    public static AwkArrayKeyComparer Instance { get; } =
        new(null, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled);

    AwkArrayKey IAlternateEqualityComparer<AwkArrayKey, AwkArrayKey>.Create(AwkArrayKey key) => key;

    AwkArrayKey IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey>.Create(ReadOnlySpan<byte> bytes) =>
        new(AwkText.CopyTrustedBytes(bytes, _counters, _accounting, _checkpoint), _checkpoint);

    public bool Equals(ReadOnlySpan<byte> bytes, AwkArrayKey key) =>
        key.Equals(bytes, bytes.Length >= AwkRuntimeLimits.CancellationCheckBytes ||
            key.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null);

    public int GetHashCode(ReadOnlySpan<byte> bytes)
    {
        var checkpoint = bytes.Length >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null;
        checkpoint?.Invoke();
        var hash = new System.HashCode();
        hash.AddBytes(bytes);
        checkpoint?.Invoke();
        return hash.ToHashCode();
    }

    public bool Equals(AwkArrayKey left, AwkArrayKey right) =>
        left.Equals(right, left.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ||
            right.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null);

    public int GetHashCode(AwkArrayKey key) =>
        // Small keys retain bounded initialization/inspection behavior. Long hashes
        // check before and after the framework call, before dictionary mutation.
        key.GetRandomizedHashCode(key.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes ? _checkpoint : null);
}
