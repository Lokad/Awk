using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk.Tests;

// Test host adapter, deliberately outside the library's production contracts.
internal sealed class CooperativeTestBudget(long limit) : IMemoryBudget
{
    private readonly long _limit = limit;
    private readonly object _gate = new();
    private long _reserved;
    public long Reserved { get { lock (_gate) return _reserved; } }
    public long Peak { get; private set; }
    public int ScopesCreated { get; private set; }
    public int ScopesDisposed { get; private set; }

    public IMemoryScope CreateScope() { lock (_gate) return NewScope(null); }
    private Scope NewScope(Scope? parent)
    {
        var result = new Scope(this, parent);
        parent?.Children.Add(result);
        ScopesCreated++;
        return result;
    }

    public bool TryReserve(long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation)
    {
        lock (_gate) return Reserve(null, bytes, out reservation);
    }

    private bool Reserve(Scope? scope, long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        scope?.EnsureAlive();
        if (bytes > _limit - _reserved) { reservation = null; return false; }
        var result = new Reservation(this, scope, bytes);
        scope?.Reservations.Add(result);
        _reserved += bytes;
        Peak = Math.Max(Peak, _reserved);
        reservation = result;
        return true;
    }

    private sealed class Scope(CooperativeTestBudget root, Scope? parent) : IMemoryScope
    {
        private bool _disposed;
        public List<Scope> Children { get; } = [];
        public List<Reservation> Reservations { get; } = [];
        public void EnsureAlive()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            parent?.EnsureAlive();
        }
        public IMemoryScope CreateScope() { lock (root._gate) { EnsureAlive(); return root.NewScope(this); } }
        public bool TryReserve(long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation)
        { lock (root._gate) return root.Reserve(this, bytes, out reservation); }
        public void Dispose()
        {
            lock (root._gate)
            {
                if (_disposed) return;
                foreach (var child in Children) child.Dispose();
                foreach (var reservation in Reservations) reservation.Dispose();
                _disposed = true;
                root.ScopesDisposed++;
            }
        }
    }

    private sealed class Reservation(CooperativeTestBudget root, Scope? scope, long bytes) : IMemoryReservation
    {
        private long _bytes = bytes;
        private bool _disposed;
        public long Bytes { get { lock (root._gate) return _bytes; } }
        public bool TryResize(long next)
        {
            lock (root._gate)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(next);
                ObjectDisposedException.ThrowIf(_disposed, this);
                scope?.EnsureAlive();
                var growth = next - _bytes;
                if (growth > root._limit - root._reserved) return false;
                root._reserved += growth;
                _bytes = next;
                root.Peak = Math.Max(root.Peak, root._reserved);
                return true;
            }
        }
        public void Dispose()
        {
            lock (root._gate)
            {
                if (_disposed) return;
                root._reserved -= _bytes;
                _bytes = 0;
                _disposed = true;
            }
        }
    }
}
