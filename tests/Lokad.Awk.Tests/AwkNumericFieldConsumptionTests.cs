namespace Lokad.Awk.Tests;

public sealed class AwkNumericFieldConsumptionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericReadConsumesBorrowedBytesWithoutCachingAndRespectsModifiedValues(bool finite)
    {
        var (state, counters) = State(finite);
        var bytes = "k 007"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        _ = state.FieldCount;
        var allocations = counters.AwkTextAllocations;
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        bytes[4] = (byte)'8';
        Assert.Equal(8, state.GetFieldNumber(2));
        state.AssignFieldValue(2, AwkValue.FromNumber(1.25));
        Assert.Equal(1.25, state.GetFieldNumber(2));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void CanceledNumericReadKeepsRecordAndReleasesTemporaryAdmission()
    {
        using var cancellation = new CancellationTokenSource();
        var (state, _) = State(true, cancellation.Token);
        var record = AwkText.FromString("k " + new string('1', 16_384));
        state.SetCurrentRecord(record);
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.GetFieldNumber(2));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(record, state.CurrentRecordByteText);
    }

    private static (AwkRuntimeState, AwkRuntimeCounters) State(bool finite, CancellationToken cancellation = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (finite)
            limits = limits.WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility);
        return (AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation, options), counters);
    }
}
