using System;
using System.Collections;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkLeasedReadOnlyList<T>(
    IReadOnlyList<T> values,
    IDisposable lease) : IReadOnlyList<T>, IDisposable
{
    private bool _disposed;

    public int Count => values.Count;

    public T this[int index] => values[index];

    public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        lease.Dispose();
    }
}
