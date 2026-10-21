namespace Lokad.Awk.Tests;

public sealed class AwkNumericFieldConsumptionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericReadConsumesBorrowedBytesWithoutCachingAndRespectsModifiedValues(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var counters = state.Counters;
        Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
        var bytes = "k 007"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        _ = state.FieldCount;
        Assert.Equal(governed, memory.Budget.Reserved > 0);
        var allocations = counters.AwkTextAllocations;
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        bytes[4] = (byte)'8';
        Assert.Equal(8, state.GetFieldNumber(2));
        state.AssignFieldValue(2, AwkValue.FromNumber(1.25));
        Assert.Equal(1.25, state.GetFieldNumber(2));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void CanceledNumericReadKeepsRecordAndReleasesTemporaryAdmission()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        Assert.True(state.MemoryAccounting.IsEnabled);
        var record = AwkText.FromString("k " + new string('1', 16_384));
        state.SetCurrentRecord(record);
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.GetFieldNumber(2));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(record, state.CurrentRecordByteText);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static CooperativeTestMemory State(bool governed, CancellationToken cancellation = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, cancellation, memory.Configure(options)));
    }
}
