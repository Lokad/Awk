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

    public IDisposable ReserveTransient(long byteCount)
    {
        if (!_enabled || byteCount <= 0)
            return AwkNoopMemoryLease.Instance;

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

        return new AwkTransientMemoryLease(this, byteCount);
    }

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

    private sealed class AwkNoopMemoryLease : IDisposable
    {
        public static AwkNoopMemoryLease Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed class AwkTransientMemoryLease(AwkMemoryGovernor governor, long byteCount) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            governor.ReleaseCommitted(byteCount);
        }
    }
}
