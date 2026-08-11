using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkArrayKeyComparer : IEqualityComparer<AwkArrayKey>
{
    private AwkArrayKeyComparer()
    {
    }

    public static AwkArrayKeyComparer Instance { get; } = new();

    public bool Equals(AwkArrayKey left, AwkArrayKey right) => left.Equals(right);

    public int GetHashCode(AwkArrayKey key) => key.GetRandomizedHashCode();
}
