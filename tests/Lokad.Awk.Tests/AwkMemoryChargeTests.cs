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
        var error = Assert.Throws<AwkRuntimeException>(() => governor.ReserveAndCommit(89));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(40, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        governor.ReserveAndCommit(88);
        Assert.Equal(128, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        governor.ReleaseCommitted(88);
        existing.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
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
        lease.Dispose();
    }
}
