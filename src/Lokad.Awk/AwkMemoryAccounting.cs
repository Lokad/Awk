using System;

namespace Lokad.Awk;

// Own-storage estimates and an optional execution quota; the host owns the shared allowance.
internal sealed class AwkMemoryAccounting
{
    private readonly bool _enabled;
    private readonly long _maxOwnedBytes;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkExecutionMemoryOwner _owner;

    private AwkMemoryAccounting(bool enabled, long maxOwnedBytes, AwkRuntimeCounters counters,
        AwkExecutionMemoryOwner owner)
    {
        _enabled = enabled;
        _maxOwnedBytes = maxOwnedBytes;
        _counters = counters;
        _owner = owner;
    }

    public static AwkMemoryAccounting Disabled { get; } =
        new(enabled: false, AwkRuntimeLimits.Unlimited, AwkRuntimeCounters.Disabled, AwkExecutionMemoryOwner.Disabled);

    public bool IsEnabled => _enabled;

    public long CurrentReservedBytes { get; private set; }

    public long PeakReservedBytes { get; private set; }

    public long CurrentCommittedBytes { get; private set; }

    public long PeakCommittedBytes { get; private set; }

    public long PeakLiveBytes { get; private set; }

    public long ReserveCount { get; private set; }

    public long CommitCount { get; private set; }

    public long ReleaseReservedCount { get; private set; }

    public long ReleaseCommittedCount { get; private set; }

    public long TotalReservedBytes { get; private set; }

    public long TotalCommittedBytes { get; private set; }

    public long TotalReleasedReservedBytes { get; private set; }

    public long TotalReleasedCommittedBytes { get; private set; }

    public static AwkMemoryAccounting Create(AwkRuntimeLimits limits)
    {
        return Create(limits, AwkRuntimeCounters.Disabled);
    }

    public static AwkMemoryAccounting Create(AwkRuntimeLimits limits, AwkRuntimeCounters counters) =>
        Create(limits, counters, AwkExecutionMemoryOwner.Disabled);

    public static AwkMemoryAccounting Create(AwkRuntimeLimits limits, AwkRuntimeCounters counters,
        AwkExecutionMemoryOwner owner)
    {
        return limits.MaxExecutionMemoryBytes == AwkRuntimeLimits.Unlimited && !owner.IsEnabled
            ? Disabled
            : new AwkMemoryAccounting(enabled: true, limits.MaxExecutionMemoryBytes, counters, owner);
    }

    public void Reserve(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        var reserved = AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, byteCount);
        ThrowIfLiveWouldExceedLimit(reserved, CurrentCommittedBytes);
        var totalReserved = AwkRuntimeMemory.CheckedAdd(TotalReservedBytes, byteCount);
        ResizeOwner(reserved, CurrentCommittedBytes);
        CurrentReservedBytes = reserved;
        TotalReservedBytes = totalReserved;
        ReserveCount++;
        PeakReservedBytes = Math.Max(PeakReservedBytes, CurrentReservedBytes);
        UpdatePeakLiveBytes();
        _counters.RecordMemoryReserve(byteCount, LiveBytes);
    }

    public void PreflightAllocation(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        ThrowIfLiveWouldExceedLimit(
            AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, byteCount),
            CurrentCommittedBytes);
    }

    public AwkMemoryPeakAdmission ReserveAllocationPeak(long byteCount)
    {
        PreflightAllocation(byteCount);
        return _owner.ReservePeak(byteCount);
    }

    public AwkMemoryMutationAdmission AdmitMutation(long byteCount)
    {
        PreflightAllocation(byteCount);
        return _owner.AdmitMutation(byteCount);
    }

    public void CommitReserved(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        if (byteCount > CurrentReservedBytes)
            throw new InvalidOperationException("Cannot commit more bytes than have been reserved.");

        var reserved = CurrentReservedBytes - byteCount;
        var committed = AwkRuntimeMemory.CheckedAdd(CurrentCommittedBytes, byteCount);
        ThrowIfLiveWouldExceedLimit(reserved, committed);
        var totalCommitted = AwkRuntimeMemory.CheckedAdd(TotalCommittedBytes, byteCount);
        CurrentReservedBytes = reserved;
        CurrentCommittedBytes = committed;
        TotalCommittedBytes = totalCommitted;
        CommitCount++;
        PeakCommittedBytes = Math.Max(PeakCommittedBytes, CurrentCommittedBytes);
        UpdatePeakLiveBytes();
        _counters.RecordMemoryCommit(byteCount, LiveBytes);
    }

    public void ReleaseReserved(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        if (byteCount > CurrentReservedBytes)
            throw new InvalidOperationException("Cannot release more reserved bytes than are currently reserved.");

        TotalReleasedReservedBytes = AwkRuntimeMemory.CheckedAdd(TotalReleasedReservedBytes, byteCount);
        ResizeOwner(CurrentReservedBytes - byteCount, CurrentCommittedBytes);
        ReleaseReservedCount++;
        CurrentReservedBytes -= byteCount;
        _counters.RecordMemoryReleaseReserved(byteCount);
    }

    public void ReleaseCommitted(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        if (byteCount > CurrentCommittedBytes)
            throw new InvalidOperationException("Cannot release more committed bytes than are currently committed.");

        TotalReleasedCommittedBytes = AwkRuntimeMemory.CheckedAdd(TotalReleasedCommittedBytes, byteCount);
        ResizeOwner(CurrentReservedBytes, CurrentCommittedBytes - byteCount);
        ReleaseCommittedCount++;
        CurrentCommittedBytes -= byteCount;
        _counters.RecordMemoryReleaseCommitted(byteCount);
    }

    public AwkMemoryLease ReserveTransient(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return AwkMemoryLease.Disabled;

        ReserveAndCommit(byteCount);
        return new AwkMemoryLease(this, byteCount);
    }

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
        _enabled ? new AwkMemoryLease(this, 0) : AwkMemoryLease.Disabled;

    private long LiveBytes => AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, CurrentCommittedBytes);

    private void ResizeOwner(long reserved, long committed)
    {
        if (!_owner.TryResizeTracked(AwkRuntimeMemory.CheckedAdd(reserved, committed)))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }

    private void UpdatePeakLiveBytes()
    {
        PeakLiveBytes = Math.Max(PeakLiveBytes, LiveBytes);
    }

    private void ThrowIfLiveWouldExceedLimit(long reservedBytes, long committedBytes)
    {
        if (AwkRuntimeMemory.CheckedAdd(reservedBytes, committedBytes) > _maxOwnedBytes)
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
