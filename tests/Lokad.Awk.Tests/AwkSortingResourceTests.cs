using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSortingResourceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("@val_type_asc")]
    [InlineData("@val_type_desc")]
    public async Task SortComparisonFailureLeavesSourceAndDestinationUntouched(string selector)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault()
            .WithMaxSortComparisons(1).WithMaxExecutionMemoryBytes(1 << 20));
        var source = AwkArrayName.FromText("source");
        var destination = AwkArrayName.FromText("destination");
        for (var index = 1; index <= 32; index++)
            state.SetArrayAwkValue(source, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(33 - index));
        var first = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(destination, first, "keep");
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            state.SortArrayValuesAsync(source, destination, AwkArraySortOrder.Parse(selector),
                new AwkEvaluationContext(state)).AsTask());

        Assert.Equal("awk: array sort comparison limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("keep", state.GetArrayByteText(destination, first).AsString());
        Assert.Equal(32, state.GetArrayAwkValue(source, first).AsNumber());
        Assert.True(state.GetArrayAwkValue(source, first).IsNumberValue);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("@val_type_asc")]
    [InlineData("@val_type_desc")]
    [InlineData("@unsorted")]
    public async Task MemoryPreflightLeavesTypedArraysUntouched(string selector)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        const int memoryLimit = 1 << 20;
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(memoryLimit));
        var source = AwkArrayName.FromText("source");
        var destination = AwkArrayName.FromText("destination");
        var first = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(source, new AwkArrayKey("key"), "value");
        state.SetArrayString(destination, first, "keep");
        using var pressure = state.MemoryAccounting.ReserveTransient(memoryLimit - state.MemoryAccounting.CurrentCommittedBytes -
            AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(1) + 1);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            state.SortArrayValuesAsync(source, destination, AwkArraySortOrder.Parse(selector),
                new AwkEvaluationContext(state)).AsTask());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("keep", state.GetArrayByteText(destination, first).AsString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task UnsortedTraversalUsesOnlyTheGovernedKeySnapshot()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20));
        var name = AwkArrayName.FromText("values");
        for (var index = 1; index <= 32; index++)
            state.SetArrayAwkValue(name, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));
        state.SetArrayString(AwkArrayName.FromText("PROCINFO"), new AwkArrayKey("sorted_in"), "@unsorted");
        var reservedBefore = state.MemoryAccounting.TotalReservedBytes;
        var committedBefore = state.MemoryAccounting.CurrentCommittedBytes;
        var stepsBefore = counters.ExecutionSteps;
        using (var snapshot = await state.SnapshotArrayKeyValuesAsync(name, new AwkEvaluationContext(state)))
        {
            Assert.Equal(32, snapshot.Count);
            Assert.Equal(committedBefore + AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(32), state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(32, snapshot.Distinct().Count());
        }
        Assert.Equal(AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(32), state.MemoryAccounting.TotalReservedBytes - reservedBefore);
        Assert.Equal(stepsBefore, counters.ExecutionSteps);
        Assert.Equal(committedBefore, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultNanFormattingStaysByteNativeAndPreservesSign(bool negative)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault());
        var value = AwkValue.FromNumber(Math.CopySign(double.NaN, negative ? -1 : 1));
        var text = state.FormatStringByteText(value);
        Assert.True(text.AsSpan().SequenceEqual(negative ? "-nan"u8 : "+nan"u8));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(1, counters.AwkTextAllocations);
    }

    private static AwkRuntimeState CreateState(AwkRuntimeCounters counters, AwkRuntimeLimits limits) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter));
}
