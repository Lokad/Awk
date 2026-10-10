using System;

namespace Lokad.Awk;

internal sealed class AwkMemoryGovernor
{
    private readonly bool _enabled;
    private readonly long _maxLiveBytes;
    private readonly AwkRuntimeCounters _counters;

    private AwkMemoryGovernor(bool enabled, long maxLiveBytes, AwkRuntimeCounters counters)
    {
        _enabled = enabled;
        _maxLiveBytes = maxLiveBytes;
        _counters = counters;
    }

    public static AwkMemoryGovernor Disabled { get; } =
        new(enabled: false, AwkRuntimeLimits.Unlimited, AwkRuntimeCounters.Disabled);

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

    public static AwkMemoryGovernor Create(AwkRuntimeLimits limits)
    {
        return Create(limits, AwkRuntimeCounters.Disabled);
    }

    public static AwkMemoryGovernor Create(AwkRuntimeLimits limits, AwkRuntimeCounters counters)
    {
        return limits.MaxExecutionMemoryBytes == AwkRuntimeLimits.Unlimited
            ? Disabled
            : new AwkMemoryGovernor(enabled: true, limits.MaxExecutionMemoryBytes, counters);
    }

    public void Reserve(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return;

        var reserved = AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, byteCount);
        ThrowIfLiveWouldExceedLimit(reserved, CurrentCommittedBytes);
        var totalReserved = AwkRuntimeMemory.CheckedAdd(TotalReservedBytes, byteCount);
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
        ReleaseCommittedCount++;
        CurrentCommittedBytes -= byteCount;
        _counters.RecordMemoryReleaseCommitted(byteCount);
    }

    public AwkMemoryLease ReserveTransient(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return AwkMemoryLease.Disabled;

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

        return new AwkMemoryLease(this, byteCount);
    }

    public AwkMemoryLease CreateTransientLease() =>
        _enabled ? new AwkMemoryLease(this, 0) : AwkMemoryLease.Disabled;

    private long LiveBytes => AwkRuntimeMemory.CheckedAdd(CurrentReservedBytes, CurrentCommittedBytes);

    private void UpdatePeakLiveBytes()
    {
        PeakLiveBytes = Math.Max(PeakLiveBytes, LiveBytes);
    }

    private void ThrowIfLiveWouldExceedLimit(long reservedBytes, long committedBytes)
    {
        if (AwkRuntimeMemory.CheckedAdd(reservedBytes, committedBytes) > _maxLiveBytes)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }

}

internal sealed class AwkMemoryLease : IDisposable
{
    private AwkMemoryGovernor? _governor;
    private long _byteCount;

    private AwkMemoryLease()
    {
    }

    public AwkMemoryLease(AwkMemoryGovernor governor, long byteCount)
    {
        _governor = governor;
        _byteCount = byteCount;
    }

    public static AwkMemoryLease Disabled { get; } = new();

    public void ReserveAdditional(long byteCount)
    {
        var governor = _governor;
        if (governor is null || byteCount <= 0)
            return;

        var nextByteCount = AwkRuntimeMemory.CheckedAdd(_byteCount, byteCount);
        governor.Reserve(byteCount);
        try
        {
            governor.CommitReserved(byteCount);
        }
        catch
        {
            governor.ReleaseReserved(byteCount);
            throw;
        }
        _byteCount = nextByteCount;
    }

    public void Release(long byteCount)
    {
        var governor = _governor;
        if (governor is null)
            return;
        if (byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Cannot release more bytes than remain in the memory lease.");
        governor.ReleaseCommitted(byteCount);
        _byteCount -= byteCount;
    }

    public void Transfer(long byteCount)
    {
        if (_governor is null)
            return;

        if (byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Cannot transfer more bytes than remain in the memory lease.");

        _byteCount -= byteCount;
    }

    public void MoveTo(AwkMemoryLease destination, long byteCount)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_governor is null || ReferenceEquals(this, destination))
            return;
        if (!ReferenceEquals(_governor, destination._governor) || byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Memory can only move between live leases of the same governor.");
        var nextDestinationBytes = AwkRuntimeMemory.CheckedAdd(destination._byteCount, byteCount);
        _byteCount -= byteCount;
        destination._byteCount = nextDestinationBytes;
    }

    public void Dispose()
    {
        var governor = _governor;
        if (governor is null)
            return;

        _governor = null;
        if (_byteCount > 0)
            governor.ReleaseCommitted(_byteCount);
        _byteCount = 0;
    }
}
