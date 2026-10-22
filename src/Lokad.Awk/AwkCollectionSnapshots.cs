using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkCollectionSnapshots
{
    public static T[] Copy<T>(IReadOnlyList<T> values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var copy = new T[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            if ((index & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            copy[index] = values[index];
        }

        return copy;
    }
}
