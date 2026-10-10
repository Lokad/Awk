using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkArrayKeyComparer : IEqualityComparer<AwkArrayKey>
{
    private readonly Action? _checkpoint;

    private AwkArrayKeyComparer(Action? checkpoint = null)
    {
        _checkpoint = checkpoint;
    }

    // The shared comparer is callback-free. Only runtime-owned dictionaries
    // retain their own execution callback.
    internal static AwkArrayKeyComparer ForRuntime(Action? checkpoint) =>
        checkpoint is null ? Instance : new(checkpoint);

    public static AwkArrayKeyComparer Instance { get; } = new();

    public bool Equals(AwkArrayKey left, AwkArrayKey right) =>
        left.Equals(right, left.ByteLength >= AwkExecutionGovernor.WorkChunkBytes ||
            right.ByteLength >= AwkExecutionGovernor.WorkChunkBytes ? _checkpoint : null);

    public int GetHashCode(AwkArrayKey key) =>
        // Small keys retain bounded initialization/inspection behavior. Long hashes
        // check before and after the framework call, before dictionary mutation.
        key.GetRandomizedHashCode(key.ByteLength >= AwkExecutionGovernor.WorkChunkBytes ? _checkpoint : null);
}
