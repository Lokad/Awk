using System.Text.Json;

namespace Lokad.Awk.Tests;

public sealed class AwkMemoryAccountingTransitionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReservationTransfersKeepLiveUsageAndFundTheOwner(bool shared)
    {
        var budget = new CooperativeTestBudget(100);
        await using var owner = AwkExecutionMemoryOwner.Create(shared ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var ledger = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(100), counters, owner);
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
        Assert.Equal(shared ? 35 : 0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuotaRejectionPrecedesHostGrowthAndLeavesTheLedgerUnchanged(bool shared)
    {
        var budget = new CooperativeTestBudget(256);
        await using var owner = AwkExecutionMemoryOwner.Create(shared ? budget : null, CancellationToken.None);
        var ledger = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(64),
            AwkRuntimeCounters.CreateEnabled(), owner);
        ledger.Reserve(40);
        ledger.CommitReserved(20);
        var before = Snapshot(ledger);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        ledger.PreflightAllocation(24);
        AssertMemory(() => ledger.PreflightAllocation(25));
        AssertMemory(() => ledger.Reserve(25));
        Assert.Equal(before, Snapshot(ledger));
        Assert.Equal(0, budget.Refusals);
        Assert.Equal(shared ? 40 : 0, budget.Reserved);
    }

    [Fact]
    public async Task RefusedSharedGrowthDoesNotPublishAccountingOrRetry()
    {
        var budget = new CooperativeTestBudget(256);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var ledger = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.CreateEnabled(), owner);
        ledger.Reserve(40);
        ledger.CommitReserved(20);
        var before = Snapshot(ledger);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        AssertMemory(() => ledger.Reserve(1));
        Assert.Equal(before, Snapshot(ledger));
        Assert.Equal(40, budget.Reserved);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
    }

    [Fact]
    public async Task LedgerRefundKeepsActualPendingUsersAdmitted()
    {
        var budget = new CooperativeTestBudget(100);
        var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var ledger = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.CreateEnabled(), owner);
        ledger.Reserve(40);
        ledger.CommitReserved(40);
        var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = owner.Track(pending.Task);
        ledger.ReleaseCommitted(40);
        Assert.Equal(0, ledger.CurrentCommittedBytes);
        Assert.Equal(40, budget.Reserved);
        var closing = owner.DisposeAsync();
        Assert.False(closing.IsCompleted);
        pending.SetResult(0);
        await closing;
        Assert.Equal(0, budget.Reserved);
    }

    private static string Snapshot(AwkMemoryAccounting ledger) => JsonSerializer.Serialize(ledger);
    private static void AssertMemory(Action action) => Assert.Equal("awk: memory limit exceeded\n",
        AwkUtf8Codec.Decode(Assert.Throws<AwkRuntimeException>(action).MessageBytes));
}
