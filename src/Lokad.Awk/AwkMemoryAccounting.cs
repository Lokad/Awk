using System;

namespace Lokad.Awk;

// Own-storage estimates; admission belongs exclusively to the host's shared allowance.
internal sealed class AwkMemoryAccounting
{
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkExecutionMemoryOwner _owner;

    private AwkMemoryAccounting(AwkRuntimeCounters counters,
        AwkExecutionMemoryOwner owner)
    {
        _counters = counters;
        _owner = owner;
    }

    public static AwkMemoryAccounting Disabled { get; } =
        new(AwkRuntimeCounters.Disabled, AwkExecutionMemoryOwner.Disabled);

    public bool IsEnabled => _owner.IsEnabled;

    public long CurrentReservedBytes { get; private set; }

    public long CurrentCommittedBytes { get; private set; }

    public static AwkMemoryAccounting Create(AwkRuntimeCounters counters,
        AwkExecutionMemoryOwner owner)
    {
        return !owner.IsEnabled
            ? Disabled
            : new AwkMemoryAccounting(counters, owner);
    }

    public void Reserve(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return;

        var reserved = AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, byteCount);
        var liveBytes = AwkRuntimeMemory.CheckedAdd(reserved, CurrentCommittedBytes);
        ResizeOwner(liveBytes);
        CurrentReservedBytes = reserved;
        _counters.RecordMemoryLiveBytes(liveBytes);
    }

    public AwkMemoryPeakAdmission ReserveAllocationPeak(long byteCount)
    {
        return _owner.ReservePeak(byteCount);
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

        if (byteCount > CurrentCommittedBytes)
            throw new InvalidOperationException("Cannot release more committed bytes than are currently committed.");

        ResizeOwner(AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, CurrentCommittedBytes - byteCount));
        CurrentCommittedBytes -= byteCount;
    }

    public AwkMemoryLease ReserveTransient(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return AwkMemoryLease.Disabled;

        ReserveAndCommit(byteCount);
        return new AwkMemoryLease(this, byteCount);
    }

    // Evaluation can keep a scalar after a later operand replaces its original slot.
    // A scoped duplicate estimate avoids tracking aliases between storage owners.
    public AwkMemoryLease RetainValue(AwkValue value) =>
        ReserveTransient(IsEnabled ? value.RetainedTextMemoryBytes : 0);

    // Only for immediate charges. Allocation/publication stages keep their
    // separate reservation and commit so failure can preserve prior state.
    public void ReserveAndCommit(long byteCount)
    {
        Reserve(byteCount);
        try
        {
            CommitReserved(byteCount);
        }
        catch
        {
            ReleaseReserved(byteCount);
            throw;
        }
    }

    public AwkMemoryLease CreateTransientLease() =>
        IsEnabled ? new AwkMemoryLease(this, 0) : AwkMemoryLease.Disabled;

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

    public AwkMemoryLease(AwkMemoryAccounting accounting, long byteCount)
    {
        _accounting = accounting;
        _byteCount = byteCount;
    }

    public static AwkMemoryLease Disabled { get; } = new();

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
