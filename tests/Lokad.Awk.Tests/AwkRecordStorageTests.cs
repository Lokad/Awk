namespace Lokad.Awk.Tests;

public sealed class AwkRecordStorageTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void FailedOwnedStagingKeepsPublishedRecordAndRefundsCapacity(bool cancel, bool growing)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(1 << 20)
            .InitializeRuntime(AwkRuntimeCounters.Disabled, cancellation.Token);
        var baseline = memory.Budget.Reserved;
        var storage = memory.State.InputRecordStorage;
        if (growing) storage.StageValidated("prior"u8);
        storage.StageValidated("published"u8);
        if (cancel) memory.Budget.OnGrowth = _ => cancellation.Cancel();
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var bytes = new byte[32768];
        bytes.AsSpan().Fill((byte)'x');
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => storage.StageValidated(bytes));
        else Assert.Throws<AwkRuntimeException>(() => storage.StageValidated(bytes));
        Assert.Equal("published"u8.ToArray(), storage.Content.ToArray());
        storage.Dispose();
        Assert.Equal(baseline, memory.Budget.Reserved);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }
}
