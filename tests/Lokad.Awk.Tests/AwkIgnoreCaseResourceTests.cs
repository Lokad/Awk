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
        var allocatedBefore = counters.AwkTextAllocations;
        for (var index = 0; index < 16; index++)
        {
            Assert.True(AwkComparisonOperations.Evaluate(a, b, AwkComparison.Equal, state));
            Assert.True(state.CompareByteText(left, right, sorting: true) < 0);
            Assert.Equal(2, state.IndexOfStringUnits(source, right));
        }
        Assert.Equal(observed ? 16 : 0, counters.ByteTextComparisons);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(allocatedBefore, counters.AwkTextAllocations);
        Assert.Equal(committedBefore, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongFoldedScansObserveCancellationBetweenChunks(bool search)
    {
        using var cancellation = new CancellationTokenSource();
        var work = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled, cancellation.Token);
        var checkpoints = 0;
        var strings = new AwkStringRuntime(AwkStringUnitMode.Utf8Rune, () =>
        {
            if (++checkpoints == 2)
                cancellation.Cancel();
            work.Checkpoint();
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

    private static AwkRuntimeState CreateState(AwkRuntimeCounters counters, AwkRuntimeLimits limits)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase, AwkValue.FromNumber(1));
        return state;
    }
}
