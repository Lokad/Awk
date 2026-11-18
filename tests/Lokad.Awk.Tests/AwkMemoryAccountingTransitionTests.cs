namespace Lokad.Awk.Tests;

public sealed class AwkMemoryAccountingTransitionTests
{
    [Fact]
    public async Task RefusedGrowthPreservesTheSharedChargeWithoutRetry()
    {
        var budget = new CooperativeTestBudget(256);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(owner);
        accounting.Charge(40);
        budget.RefuseNextAdmissionMinimumGrowth = 1;

        var failure = Assert.Throws<AwkRuntimeException>(() => accounting.Charge(1));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(40, budget.Reserved);
        Assert.Equal(40, accounting.LiveBytes);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        accounting.Refund(40);
        Assert.Equal(0, budget.Reserved);
    }
}
