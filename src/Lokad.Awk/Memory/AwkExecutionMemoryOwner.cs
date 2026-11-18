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
    internal long TrackedBytes => _trackedBytes;
    public IMemoryScope Scope => _scope ?? throw new InvalidOperationException("No cooperative memory scope is active.");
    public CancellationToken Token(CancellationToken original) => _cancellation?.Token ?? original;

    public static AwkExecutionMemoryOwner Create(IMemoryBudget? budget, CancellationToken cancellationToken)
    {
        if (budget is null) return Disabled;
        IMemoryScope scope;
        try { scope = budget.CreateScope(); }
        catch (Exception error) { throw new AwkHostException(error); }
        try
        {
            if (!AwkMemoryCallbacks.TryReserve(scope, 0, out var reservation))
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            try
            {
                return new(scope, reservation, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
            }
            catch
            {
                AwkMemoryCallbacks.Dispose(reservation);
                throw;
            }
        }
        catch
        {
            AwkMemoryCallbacks.Dispose(scope);
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
        var charge = Math.Max(_mutationFloor, bytes);
        // Components of a mutation consume its admitted floor without contacting
        // the provider again. Crossing back from above the floor refunds the excess.
        var admitted = _mutationFloor > 0 && bytes <= _mutationFloor && _trackedBytes <= _mutationFloor;
        // Outside the floor, check unchanged requests too when funding may have ended.
        if (!admitted && (bytes != _trackedBytes || charge == 0 || charge != AwkMemoryCallbacks.Bytes(reservation)) &&
            !AwkMemoryCallbacks.TryResize(reservation, charge)) return false;
        _trackedBytes = bytes;
        return true;
    }

    // Admit a compound mutation once, before its first observable change. Its component
    // charges consume this same reservation, rather than competing with a second charge.
    public AwkMemoryMutationAdmission AdmitMutation(long growth)
    {
        if (_reservation is not { } reservation || growth <= 0) return default;
        var previous = _mutationFloor;
        var floor = Math.Max(previous, AwkRuntimeMemory.CheckedAdd(_trackedBytes, growth));
        // If the floor grows, it exceeds both the prior floor and actual live
        // charges. Otherwise the existing owned admission already funds it.
        if (floor > previous && !AwkMemoryCallbacks.TryResize(reservation, floor))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        _mutationFloor = floor;
        return new(this, previous);
    }

    internal void EndMutation(long previousFloor)
    {
        var heldFloor = _mutationFloor;
        _mutationFloor = previousFloor;
        var charge = Math.Max(previousFloor, _trackedBytes);
        if (charge < heldFloor && _reservation is { } reservation && !AwkMemoryCallbacks.TryResize(reservation, charge))
            throw new InvalidOperationException("Releasing mutation admission cannot require more memory.");
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
            try { FrontendMemory.Dispose(); }
            finally
            {
                try { AwkMemoryCallbacks.Dispose(_reservation); }
                finally
                {
                    try { AwkMemoryCallbacks.Dispose(_scope); }
                    finally { _cancellation?.Dispose(); }
                }
            }
        }
    }
}

internal readonly struct AwkMemoryMutationAdmission(AwkExecutionMemoryOwner? owner, long previousFloor) : IDisposable
{
    public bool IsActive => owner is not null;
    public void Dispose() => owner?.EndMutation(previousFloor);
}
