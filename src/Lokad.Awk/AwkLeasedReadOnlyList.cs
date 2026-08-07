using System;
using System.Collections;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkLeasedReadOnlyList<T> : IReadOnlyList<T>, IDisposable
{
    private readonly IReadOnlyList<T> _values;
    private readonly IDisposable _lease;
    private bool _disposed;

    public AwkLeasedReadOnlyList(IReadOnlyList<T> values, IDisposable lease)
    {
        _values = values;
        _lease = lease;
    }

    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return _values.Count;
        }
    }

    public T this[int index]
    {
        get
        {
            ThrowIfDisposed();
            return _values[index];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        ThrowIfDisposed();
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lease.Dispose();
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
