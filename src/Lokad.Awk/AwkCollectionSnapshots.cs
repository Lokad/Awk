using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkCollectionSnapshots
{
    public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
    {
        var copy = new T[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            copy[index] = values[index];
        }

        return copy;
    }

    public static IReadOnlyList<IReadOnlyList<T>> CopyNested<T>(IReadOnlyList<IReadOnlyList<T>> values)
    {
        var copy = new IReadOnlyList<T>[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            copy[index] = Copy(values[index]);
        }

        return copy;
    }
}
