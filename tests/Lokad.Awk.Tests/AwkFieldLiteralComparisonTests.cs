namespace Lokad.Awk.Tests;

public sealed class AwkFieldLiteralComparisonTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComparisonConsumesBorrowedBytesWithoutCachingOrRetainingThem(bool finite)
    {
        var (state, counters) = State(finite);
        var bytes = "first second"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        _ = state.FieldCount;
        var allocations = counters.AwkTextAllocations;
        Assert.True(state.CompareFieldLiteral(1, AwkText.FromString("first"), AwkComparison.Equal));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Array.Fill(bytes, (byte)'X');
        Assert.False(state.CompareFieldLiteral(1, AwkText.FromString("first"), AwkComparison.Equal));
        state.SetCurrentRecord(AwkText.FromString("first next"));
        Assert.True(state.CompareFieldLiteral(1, AwkText.FromString("first"), AwkComparison.Equal));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void LiteralByteRefusalPreservesRecordAndReleasesTemporaryAdmission()
    {
        var (state, counters) = State(true, valueLimit: 3);
        state.SetCurrentRecord(AwkText.FromString("ok"));
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var literal = AwkText.FromString("long");
        Assert.Throws<AwkRuntimeException>(() => state.CompareFieldLiteral(1, literal, AwkComparison.Equal));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal("ok", state.CurrentRecordByteText.AsString());
        Assert.Equal(0, counters.DeferredComparisonLiterals);
        Assert.True(state.CompareFieldLiteral(1, AwkText.FromString("ok"), AwkComparison.Equal));
        Assert.Equal(1, counters.DeferredComparisonLiterals);
    }

    [Fact]
    public void CallerCancellationPreservesAlreadySplitRecordAndAdmission()
    {
        using var cancellation = new CancellationTokenSource();
        var (state, _) = State(true, cancellation: cancellation.Token);
        var text = new string('a', 16_384) + " tail";
        state.SetCurrentRecord(AwkText.FromString(text));
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var literal = AwkText.FromString(new string('a', 16_384));
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.CompareFieldLiteral(1, literal, AwkComparison.Equal));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(text, state.CurrentRecordByteText.AsString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes - 1)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes + 1)]
    public void LiteralAdmissionAndThresholdPreserveValueWork(int length)
    {
        var (state, counters) = State(true);
        state.SetCurrentRecord(AwkText.FromString("x"));
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var values = counters.ValuesCreated;
        Assert.False(state.CompareFieldLiteral(1, AwkText.FromString(new string('1', length)), AwkComparison.Equal));
        Assert.Equal(1, counters.ValuesCreated - values);
        Assert.Equal(length < AwkRuntimeLimits.CancellationCheckBytes ? 1 : 0, counters.DeferredComparisonLiterals);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal("x", state.CurrentRecordByteText.AsString());
    }

    private static (AwkRuntimeState, AwkRuntimeCounters) State(bool finite,
        int valueLimit = int.MaxValue, CancellationToken cancellation = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(valueLimit);
        if (finite)
            limits = limits.WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility);
        return (AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation, options), counters);
    }
}
