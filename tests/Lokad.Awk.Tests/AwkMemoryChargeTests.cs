namespace Lokad.Awk.Tests;

public sealed class AwkMemoryChargeTests
{
    [Fact]
    public void RejectedImmediateChargePreservesExistingAdmissionAndCounters()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(128), counters);
        using var existing = governor.ReserveTransient(40);
        var reserves = governor.ReserveCount;
        var commits = governor.CommitCount;
        var error = Assert.Throws<AwkRuntimeException>(() => governor.ReserveAndCommit(89));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(40, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(reserves, governor.ReserveCount);
        Assert.Equal(commits, governor.CommitCount);
        governor.ReserveAndCommit(88);
        Assert.Equal(128, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(128, counters.MemoryReservedBytes);
        Assert.Equal(128, counters.MemoryCommittedBytes);
        governor.ReleaseCommitted(88);
        existing.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(counters.MemoryCommittedBytes, counters.MemoryReleasedCommittedBytes);
    }

    [Fact]
    public void LeaseGrowthFailureKeepsPriorChargeAndReleasesItExactlyOnce()
    {
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(100));
        using var lease = governor.CreateTransientLease();
        lease.ReserveAdditional(40);
        Assert.Throws<AwkRuntimeException>(() => lease.ReserveAdditional(61));
        Assert.Equal(40, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        lease.ReserveAdditional(60);
        Assert.Equal(100, governor.CurrentCommittedBytes);
        lease.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(100, governor.TotalReleasedCommittedBytes);
        lease.Dispose();
        Assert.Equal(100, governor.TotalReleasedCommittedBytes);
    }
}
