using System.Text.Json;

namespace Lokad.Awk.Tests;

public sealed class AwkMemoryAccountingTransitionTests
{
    [Fact]
    public async Task ReservationTransfersKeepLiveUsageAndFundTheOwner()
    {
        var budget = new CooperativeTestBudget(100);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var ledger = AwkMemoryAccounting.Create(counters, owner);
        ledger.Reserve(40);
        Assert.Equal(40, ledger.CurrentReservedBytes);
        Assert.Equal(0, ledger.CurrentCommittedBytes);
        ledger.CommitReserved(10);
        Assert.Equal(30, ledger.CurrentReservedBytes);
        Assert.Equal(10, ledger.CurrentCommittedBytes);
        ledger.Reserve(30);
        ledger.CommitReserved(50);
        ledger.ReleaseReserved(5);
        ledger.ReleaseCommitted(30);
        Assert.Equal(5, ledger.CurrentReservedBytes);
        Assert.Equal(30, ledger.CurrentCommittedBytes);
        Assert.Equal(70, counters.MemoryPeakLiveBytes);
        Assert.Equal(35, budget.Reserved);
    }

    [Fact]
    public async Task ImmediateChargesKeepPendingUsersFunded()
    {
        var budget = new CooperativeTestBudget(256);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var ledger = AwkMemoryAccounting.Create(counters, owner);
        ledger.Reserve(40);
        ledger.ReserveAndCommit(20);
        Assert.Equal(40, ledger.CurrentReservedBytes);
        Assert.Equal(20, ledger.CurrentCommittedBytes);
        Assert.Equal(60, budget.Reserved);
        Assert.Equal(60, counters.MemoryPeakLiveBytes);
        ledger.ReleaseReserved(40);
        Assert.Equal(20, budget.Reserved);
        ledger.ReleaseCommitted(20);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RefusedSharedGrowthDoesNotPublishAccountingOrRetry(bool immediate)
    {
        var budget = new CooperativeTestBudget(256);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var ledger = AwkMemoryAccounting.Create(AwkRuntimeCounters.CreateEnabled(), owner);
        ledger.Reserve(40);
        ledger.CommitReserved(20);
        var before = Snapshot(ledger);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        AssertMemory(() =>
        {
            if (immediate) ledger.ReserveAndCommit(1);
            else ledger.Reserve(1);
        });
        Assert.Equal(before, Snapshot(ledger));
        Assert.Equal(40, budget.Reserved);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
    }

    private static string Snapshot(AwkMemoryAccounting ledger) => JsonSerializer.Serialize(ledger);
    private static void AssertMemory(Action action) => Assert.Equal("awk: memory limit exceeded\n",
        AwkUtf8Codec.Decode(Assert.Throws<AwkRuntimeException>(action).MessageBytes));
}
