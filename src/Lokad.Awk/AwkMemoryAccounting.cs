namespace Lokad.Awk;

// Own-storage estimates; admission belongs exclusively to the host's shared allowance.
internal sealed class AwkMemoryAccounting
{

    private readonly AwkExecutionMemoryOwner _owner;

    private AwkMemoryAccounting(AwkExecutionMemoryOwner owner)
    {

        _owner = owner;
    }

    public static AwkMemoryAccounting Disabled { get; } =
        new(AwkExecutionMemoryOwner.Disabled);

    public bool IsEnabled => _owner.IsEnabled;

    public long CurrentReservedBytes { get; private set; }

    public long CurrentCommittedBytes { get; private set; }

    public static AwkMemoryAccounting Create(AwkExecutionMemoryOwner owner)
    {
        return !owner.IsEnabled
            ? Disabled
            : new AwkMemoryAccounting(owner);
    }

    public void Reserve(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return;

        var reserved = AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, byteCount);
        var liveBytes = AwkRuntimeMemory.CheckedAdd(reserved, CurrentCommittedBytes);
        ResizeOwner(liveBytes);
        CurrentReservedBytes = reserved;

    }

    public AwkMemoryMutationAdmission AdmitMutation(long byteCount)
    {
        return _owner.AdmitMutation(byteCount);
    }

    public void CommitReserved(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return;

        if (byteCount > CurrentReservedBytes)
            throw new InvalidOperationException("Cannot commit more bytes than have been reserved.");

        var reserved = CurrentReservedBytes - byteCount;
        var committed = AwkRuntimeMemory.CheckedAdd(CurrentCommittedBytes, byteCount);
        CurrentReservedBytes = reserved;
        CurrentCommittedBytes = committed;
    }

    public void ReleaseReserved(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return;

        if (byteCount > CurrentReservedBytes)
            throw new InvalidOperationException("Cannot release more reserved bytes than are currently reserved.");

        ResizeOwner(AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes - byteCount, CurrentCommittedBytes));
        CurrentReservedBytes -= byteCount;
    }

    public void ReleaseCommitted(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return;

        ResizeCommitted(byteCount, 0);
    }

    public AwkMemoryLease ReserveTransient(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return AwkMemoryLease.Disabled;

        var lease = new AwkMemoryLease(this);
        lease.Resize(byteCount);
        return lease;
    }

    // Evaluation can keep a scalar after a later operand replaces its original slot.
    // A scoped duplicate estimate avoids tracking aliases between storage owners.
    public AwkMemoryLease RetainValue(AwkValue value) =>
        ReserveTransient(IsEnabled ? value.RetainedTextMemoryBytes : 0);

    // Only for immediate charges. Allocation/publication stages keep their
    // separate reservation and commit so failure can preserve prior state.
    public void ReserveAndCommit(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return;

        ResizeCommitted(0, byteCount);
    }

    internal void ResizeCommitted(long previousBytes, long bytes)
    {
        if (previousBytes > CurrentCommittedBytes)
            throw new InvalidOperationException("Cannot release more committed bytes than are currently committed.");

        var committed = AwkRuntimeMemory.CheckedAdd(CurrentCommittedBytes - previousBytes, bytes);
        ResizeOwner(AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, committed));
        CurrentCommittedBytes = committed;
    }

    public AwkMemoryLease CreateTransientLease() => IsEnabled ? new AwkMemoryLease(this) : AwkMemoryLease.Disabled;

    private void ResizeOwner(long liveBytes)
    {
        if (!_owner.TryResizeTracked(liveBytes))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }

}

internal sealed class AwkMemoryLease : IDisposable
{
    private AwkMemoryAccounting? _accounting;
    private long _byteCount;

    private AwkMemoryLease()
    {
    }

    public AwkMemoryLease(AwkMemoryAccounting accounting) => _accounting = accounting;

    public static AwkMemoryLease Disabled { get; } = new();

    internal long Bytes => _byteCount;

    internal void Resize(long bytes)
    {
        if (ReferenceEquals(this, Disabled)) return;
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        var accounting = _accounting ?? throw new ObjectDisposedException(nameof(AwkMemoryLease));
        accounting.ResizeCommitted(_byteCount, bytes);
        _byteCount = bytes;
    }

    public void ReserveAdditional(long byteCount)
    {
        var accounting = _accounting;
        if (accounting is null || byteCount <= 0)
            return;

        var nextByteCount = AwkRuntimeMemory.CheckedAdd(_byteCount, byteCount);
        accounting.ReserveAndCommit(byteCount);
        _byteCount = nextByteCount;
    }

    public void Release(long byteCount)
    {
        var accounting = _accounting;
        if (accounting is null)
            return;
        if (byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Cannot release more bytes than remain in the memory lease.");
        accounting.ReleaseCommitted(byteCount);
        _byteCount -= byteCount;
    }

    public void Transfer(long byteCount)
    {
        if (_accounting is null)
            return;

        if (byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Cannot transfer more bytes than remain in the memory lease.");

        _byteCount -= byteCount;
    }

    // The former storage owner has already admitted these bytes. Its caller
    // relinquishes the same committed claim before any callback can observe it.
    internal void ReceiveTransferred(long byteCount)
    {
        if (_accounting is null) return;
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        _byteCount = AwkRuntimeMemory.CheckedAdd(_byteCount, byteCount);
    }

    public void MoveTo(AwkMemoryLease destination, long byteCount)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_accounting is null || ReferenceEquals(this, destination))
            return;
        if (!ReferenceEquals(_accounting, destination._accounting) || byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Memory can only move between live leases of the same accounting owner.");
        var nextDestinationBytes = AwkRuntimeMemory.CheckedAdd(destination._byteCount, byteCount);
        _byteCount -= byteCount;
        destination._byteCount = nextDestinationBytes;
    }

    public void Dispose()
    {
        var accounting = _accounting;
        if (accounting is null)
            return;

        _accounting = null;
        if (_byteCount > 0)
            accounting.ReleaseCommitted(_byteCount);
        _byteCount = 0;
    }
}
