namespace Lokad.Awk;

// One coarse execution charge. Storage owners keep their leases until actual users finish;
// tracked tasks keep the scope alive, without retaining unrelated released storage.
internal sealed class AwkExecutionMemoryOwner : IAsyncDisposable
{
    private readonly IMemoryScope? _scope;
    private readonly IMemoryReservation? _reservation;
    private readonly CancellationTokenSource? _cancellation;
    private readonly List<Task> _hostTasks = [];
    private long _trackedBytes;
    private long _temporaryBytes;
    private long _mutationFloor;
    private bool _disposed;

    private AwkExecutionMemoryOwner() { }

    private AwkExecutionMemoryOwner(IMemoryScope scope, IMemoryReservation reservation,
        CancellationTokenSource cancellation)
    {
        _scope = scope;
        _reservation = reservation;
        _cancellation = cancellation;
        FrontendMemory = new AwkFrontendMemory(scope);
    }

    public static AwkExecutionMemoryOwner Disabled { get; } = new();
    public AwkFrontendMemory FrontendMemory { get; } = AwkFrontendMemory.Disabled;
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

    public bool TryResizeTracked(long bytes) => TryResizeCharge(bytes, _temporaryBytes);

    private bool TryResizeCharge(long trackedBytes, long temporaryBytes)
    {
        var reservation = _reservation;
        if (reservation is null) return true;
        var current = AwkRuntimeMemory.CheckedAdd(trackedBytes, temporaryBytes);
        if (!reservation.TryResize(Math.Max(_mutationFloor, current))) return false;
        _trackedBytes = trackedBytes;
        _temporaryBytes = temporaryBytes;
        return true;
    }

    // Admit a compound mutation once, before its first observable change. Its component
    // charges consume this same reservation, rather than competing with a second charge.
    public AwkMemoryMutationAdmission AdmitMutation(long growth)
    {
        if (_reservation is not { } reservation || growth <= 0) return default;
        var previous = _mutationFloor;
        var floor = Math.Max(previous, AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedAdd(_trackedBytes, _temporaryBytes), growth));
        if (!reservation.TryResize(Math.Max(reservation.Bytes, floor)))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        _mutationFloor = floor;
        return new(this, previous);
    }

    internal void EndMutation(long previousFloor)
    {
        _mutationFloor = previousFloor;
        if (!TryResizeTracked(_trackedBytes))
            throw new InvalidOperationException("Releasing mutation admission cannot require more memory.");
    }

    public AwkMemoryPeakAdmission ReservePeak(long bytes)
    {
        if (_scope is null || bytes <= 0) return default;
        var lease = new AwkMemoryPeakLease(this, bytes);
        if (!TryResizeCharge(_trackedBytes, AwkRuntimeMemory.CheckedAdd(_temporaryBytes, bytes)))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        return new(lease);
    }

    internal bool TryResizePeak(long previousBytes, long bytes) =>
        TryResizeCharge(_trackedBytes, AwkRuntimeMemory.CheckedAdd(_temporaryBytes - previousBytes, bytes));

    internal void ReleasePeak(long bytes)
    {
        if (!TryResizePeak(bytes, 0))
            throw new InvalidOperationException("Releasing peak admission cannot require more memory.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_scope is null || _disposed) return;
        _disposed = true;
        try
        {
            // Cancellation does not end the host's use of borrowed memory.
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
            FrontendMemory.Dispose();
            _reservation?.Dispose();
            _scope.Dispose();
            _cancellation?.Dispose();
        }
    }
}

internal readonly struct AwkMemoryMutationAdmission(AwkExecutionMemoryOwner? owner, long previousFloor) : IDisposable
{
    public bool IsActive => owner is not null;
    public void Dispose() => owner?.EndMutation(previousFloor);
}

// A coarse temporary construction peak, distinct from the retained execution estimate.
internal readonly struct AwkMemoryPeakAdmission(AwkMemoryPeakLease? lease) : IDisposable
{
    public void Resize(long bytes) => lease?.Resize(bytes);
    public void Dispose() => lease?.Dispose();
}

// Copies of the scoped handle share its resize/dispose state; host admission remains
// one execution reservation, including simultaneous construction peaks and pending I/O.
internal sealed class AwkMemoryPeakLease(AwkExecutionMemoryOwner owner, long bytes) : IDisposable
{
    private AwkExecutionMemoryOwner? _owner = owner;
    private long _bytes = bytes;

    public void Resize(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        var owner = _owner ?? throw new ObjectDisposedException(nameof(AwkMemoryPeakLease));
        if (!owner.TryResizePeak(_bytes, bytes))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        _bytes = bytes;
    }

    public void Dispose()
    {
        if (_owner is not { } owner) return;
        owner.ReleasePeak(_bytes);
        _bytes = 0;
        _owner = null;
    }
}
