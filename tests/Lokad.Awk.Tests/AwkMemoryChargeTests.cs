namespace Lokad.Awk.Tests;

public sealed class AwkMemoryChargeTests
{
    [Fact]
    public void RejectedGrowthPreservesExistingAdmission()
    {

        using var memoryForGovernor = new CooperativeTestMemory(128);
        var governor = memoryForGovernor.Accounting;
        using var existing = governor.ReserveTransient(40);
        var error = Assert.Throws<AwkRuntimeException>(() => governor.Charge(89));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(40, governor.LiveBytes);
        governor.Charge(88);
        Assert.Equal(128, governor.LiveBytes);
        governor.Refund(88);
        existing.Dispose();
        Assert.Equal(0, governor.LiveBytes);
    }

    [Fact]
    public void LeaseGrowthFailureKeepsPriorChargeAndReleasesItExactlyOnce()
    {
        using var memoryForGovernor = new CooperativeTestMemory(100);
        var governor = memoryForGovernor.Accounting;
        using var lease = governor.CreateTransientLease();
        lease.ReserveAdditional(40);
        Assert.Throws<AwkRuntimeException>(() => lease.ReserveAdditional(61));
        Assert.Equal(40, governor.LiveBytes);
        lease.ReserveAdditional(60);
        Assert.Equal(100, governor.LiveBytes);
        lease.Dispose();
        Assert.Equal(0, governor.LiveBytes);
        lease.Dispose();
    }
}
