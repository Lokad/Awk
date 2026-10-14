using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkIgnoreCaseResourceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FoldedOperationsStayByteNativeWithoutProjectionBuffers(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault());
        var left = AwkText.FromString("Ä𐐀K");
        var right = AwkText.FromString("ä𐐨k");
        var source = AwkText.FromString("xÄ𐐀K");
        var a = state.CreateByteTextValue(left);
        var b = state.CreateByteTextValue(right);
        var committedBefore = state.MemoryAccounting.CurrentCommittedBytes;
        var reservedBefore = state.MemoryAccounting.TotalReservedBytes;
        var allocatedBefore = counters.AwkTextAllocations;
        for (var index = 0; index < 16; index++)
        {
            Assert.True(AwkComparisonOperations.Evaluate(a, b, AwkComparison.Equal, state));
            Assert.True(state.CompareByteText(left, right, sorting: true) < 0);
            Assert.Equal(2, state.IndexOfStringUnits(source, right));
        }
        Assert.Equal(observed ? 16 : 0, counters.ByteTextComparisons);
        Assert.Equal(observed ? 48 : 0, counters.ExecutionSteps);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(allocatedBefore, counters.AwkTextAllocations);
        Assert.Equal(reservedBefore, state.MemoryAccounting.TotalReservedBytes);
        Assert.Equal(committedBefore, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("@val_type_asc")]
    [InlineData("@val_str_asc")]
    [InlineData("@val_num_asc")]
    [InlineData("@ind_str_asc")]
    public async Task FoldedSortWorkFailurePreservesBothArrays(string selector)
    {
        var state = CreateState(AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(1));
        var source = AwkArrayName.FromText("source");
        var destination = AwkArrayName.FromText("destination");
        var first = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(source, new AwkArrayKey("b"), "b");
        state.SetArrayString(source, new AwkArrayKey("A"), "A");
        state.SetArrayString(destination, first, "keep");
        var committedBefore = state.MemoryAccounting.CurrentCommittedBytes;
        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => state.SortArrayValuesAsync(
            source, destination, AwkArraySortOrder.Parse(selector), new AwkEvaluationContext(state)).AsTask());
        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal("keep", state.GetArrayByteText(destination, first).AsString());
        Assert.Equal("b", state.GetArrayByteText(source, new AwkArrayKey("b")).AsString());
        Assert.Equal("A", state.GetArrayByteText(source, new AwkArrayKey("A")).AsString());
        Assert.Equal(committedBefore, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongFoldedScansObserveCancellationBetweenChunks(bool search)
    {
        using var cancellation = new CancellationTokenSource();
        var work = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled, cancellation.Token);
        var checkpoints = 0;
        var strings = new AwkStringRuntime(AwkStringUnitMode.Utf8Rune, bytes =>
        {
            if (++checkpoints == 2)
                cancellation.Cancel();
            work.RecordByteWork(bytes);
        });
        var source = AwkText.FromString(new string('A', 4096));
        var value = AwkText.FromString(search ? new string('a', 1024) + "b" : new string('a', 4096));
        Assert.Throws<OperationCanceledException>(() =>
        {
            if (search)
                strings.IndexOfUnits(source, value, ignoreCase: true);
            else
                strings.Compare(source, value, ignoreCase: true, sorting: false);
        });
        Assert.Equal(2, checkpoints);
    }

    [Fact]
    public void EarlyMismatchDoesNotChargeUnexaminedSuffix()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(1));
        var suffix = new string('x', 100_000);
        Assert.True(state.CompareByteText(AwkText.FromString("b" + suffix), AwkText.FromString("A" + suffix)) > 0);
        Assert.Equal(1, counters.ExecutionSteps);
    }

    private static AwkRuntimeState CreateState(AwkRuntimeCounters counters, AwkRuntimeLimits limits)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase, AwkValue.FromNumber(1));
        return state;
    }
}
