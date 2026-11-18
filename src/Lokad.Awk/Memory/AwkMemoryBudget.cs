using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

/// <summary>A thread-safe fixed allowance shared by scopes and reservations.</summary>
/// <remarks>Reuse one budget across cooperating executions. This counts admitted estimates,
/// not process memory; allow headroom for untracked storage and delayed collection.
/// Applications with an existing governor can implement <see cref="IMemoryBudget"/> instead.</remarks>
public sealed class AwkMemoryBudget : IMemoryBudget
{
    private readonly object _gate = new();
    private long _reservedBytes;
    private long _peakBytes;

    private AwkMemoryBudget(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        LimitBytes = bytes;
    }

    /// <summary>Creates one nonnegative root allowance, without allocating the payload capacity.</summary>
    public static AwkMemoryBudget CreateFixed(long bytes) => new(bytes);

    /// <summary>Gets the fixed allowance in estimated bytes.</summary>
    public long LimitBytes { get; }

    /// <summary>Gets the currently admitted estimate.</summary>
    public long ReservedBytes { get { lock (_gate) return _reservedBytes; } }

    /// <summary>Gets the largest admitted estimate since creation.</summary>
    public long PeakBytes { get { lock (_gate) return _peakBytes; } }

    /// <inheritdoc/>
    public IMemoryScope CreateScope() => new Scope(this, null);

    /// <inheritdoc/>
    public bool TryReserve(long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation) =>
        TryReserve(null, bytes, out reservation);

    private bool TryReserve(Scope? scope, long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        lock (_gate)
        {
            scope?.ThrowIfDisposed();
            if (bytes > LimitBytes - _reservedBytes) { reservation = null; return false; }
            // Construct before publishing the charge: an allocation failure changes nothing.
            reservation = new Reservation(this, scope, bytes);
            Adjust(scope, bytes);
            return true;
        }
    }

    // Called under the root lock. Every live scope holds its subtree's aggregate;
    // the root retains no list of scopes or individual reservations.
    private void Adjust(Scope? scope, long delta)
    {
        _reservedBytes += delta;
        _peakBytes = Math.Max(_peakBytes, _reservedBytes);
        for (var current = scope; current is not null; current = current.Parent)
            current.ReservedBytes += delta;
    }

    private sealed class Scope(AwkMemoryBudget root, Scope? parent) : IMemoryScope
    {
        private bool _disposed;
        public Scope? Parent { get; } = parent;
        public long ReservedBytes { get; set; }

        public bool IsAlive()
        {
            for (Scope? current = this; current is not null; current = current.Parent)
                if (current._disposed) return false;
            return true;
        }

        public void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(!IsAlive(), this);

        public IMemoryScope CreateScope()
        {
            lock (root._gate)
            {
                ThrowIfDisposed();
                return new Scope(root, this);
            }
        }

        public bool TryReserve(long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation) =>
            root.TryReserve(this, bytes, out reservation);

        public void Dispose()
        {
            lock (root._gate)
            {
                if (_disposed) return;
                if (IsAlive()) root.Adjust(Parent, -ReservedBytes);
                ReservedBytes = 0;
                _disposed = true;
            }
        }
    }

    private sealed class Reservation(AwkMemoryBudget root, Scope? scope, long bytes) : IMemoryReservation
    {
        private long _bytes = bytes;
        private bool _disposed;

        public long Bytes
        {
            get
            {
                lock (root._gate) return _disposed || scope is not null && !scope.IsAlive() ? 0 : _bytes;
            }
        }

        public bool TryResize(long bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);
            lock (root._gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                scope?.ThrowIfDisposed();
                var growth = bytes - _bytes;
                if (growth > root.LimitBytes - root._reservedBytes) return false;
                root.Adjust(scope, growth);
                _bytes = bytes;
                return true;
            }
        }

        public void Dispose()
        {
            lock (root._gate)
            {
                if (_disposed) return;
                if (scope is null || scope.IsAlive()) root.Adjust(scope, -_bytes);
                _bytes = 0;
                _disposed = true;
            }
        }
    }
}
