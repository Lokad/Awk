namespace Lokad.Awk;

// One coarse execution charge. A pending host borrow conservatively delays refunds;
// actual host-task completion owns the final scope boundary, including failed waits.
internal sealed class AwkExecutionMemoryOwner : IAsyncDisposable
{
    private readonly IMemoryScope? _scope;
    private readonly IMemoryReservation? _reservation;
    private readonly CancellationTokenSource? _cancellation;
    private readonly List<Task> _hostTasks = [];
    private long _trackedBytes;
    private long _deferredRefundBytes;
    private bool _disposed;

    private AwkExecutionMemoryOwner() { }

    private AwkExecutionMemoryOwner(IMemoryScope scope, IMemoryReservation reservation,
        CancellationTokenSource cancellation)
    {
        _scope = scope;
        _reservation = reservation;
        _cancellation = cancellation;
    }

    public static AwkExecutionMemoryOwner Disabled { get; } = new();
    public bool IsEnabled => _scope is not null;
    public IMemoryScope Scope => _scope ?? throw new InvalidOperationException("No cooperative memory scope is active.");
    public CancellationToken Token(CancellationToken original) => _cancellation?.Token ?? original;

    public static AwkExecutionMemoryOwner Create(IMemoryBudget? budget, CancellationToken cancellationToken)
    {
        if (budget is null) return Disabled;
        var scope = budget.CreateScope();
        try
        {
            if (!scope.TryReserve(0, out var reservation))
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            try
            {
                return new(scope, reservation, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
            }
            catch
            {
                reservation.Dispose();
                throw;
            }
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    public Task<T> Track<T>(Task<T> task)
    {
        _hostTasks.RemoveAll(static completed =>
        {
            if (!completed.IsCompleted) return false;
            _ = completed.Exception;
            return true;
        });
        if (!task.IsCompleted) _hostTasks.Add(task);
        return task;
    }

    public bool TryResizeTracked(long bytes)
    {
        var reservation = _reservation;
        if (reservation is null) return true;
        var deferred = _hostTasks.Any(static task => !task.IsCompleted)
            ? AwkRuntimeMemory.CheckedAdd(_deferredRefundBytes, Math.Max(0, _trackedBytes - bytes))
            : 0;
        if (!reservation.TryResize(AwkRuntimeMemory.CheckedAdd(bytes, deferred))) return false;
        _trackedBytes = bytes;
        _deferredRefundBytes = deferred;
        return true;
    }

    public AwkMemoryPeakAdmission ReservePeak(long bytes)
    {
        if (_scope is not { } scope || bytes <= 0) return default;
        if (!scope.TryReserve(bytes, out var reservation))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        return new(reservation);
    }

    public async ValueTask DisposeAsync()
    {
        if (_scope is null || _disposed) return;
        _disposed = true;
        try
        {
            // A timeout ends a wait, not the host's use of borrowed memory.
            try { _cancellation?.Cancel(); }
            catch (AggregateException) { /* Still await storage users if a host cancellation callback fails. */ }
            if (_hostTasks.Count > 0)
            {
                try { await Task.WhenAll(_hostTasks).ConfigureAwait(false); }
                catch { /* The operation boundary already owns its failure diagnostic. */ }
            }
        }
        finally
        {
            _reservation?.Dispose();
            _scope.Dispose();
            _cancellation?.Dispose();
        }
    }
}

// A coarse temporary construction peak, distinct from the retained execution estimate.
internal readonly struct AwkMemoryPeakAdmission(IMemoryReservation? reservation) : IDisposable
{
    public void Resize(long bytes)
    {
        if (reservation is not null && !reservation.TryResize(bytes))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }
    public void Dispose() => reservation?.Dispose();
}
