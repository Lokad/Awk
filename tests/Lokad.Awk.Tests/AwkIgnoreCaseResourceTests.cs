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
        var a = state.CreateStringValue(left);
        var b = state.CreateStringValue(right);
        var committedBefore = state.MemoryAccounting.LiveBytes;
        var allocatedBefore = counters[AwkCounter.TextAllocations];
        for (var index = 0; index < 16; index++)
        {
            Assert.True(AwkComparisonOperations.Evaluate(a, b, AwkComparison.Equal, state));
            Assert.True(state.CompareByteText(left, right, sorting: true) < 0);
            Assert.Equal(2, state.IndexOfStringUnits(new(source), new(right)));
        }

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

        Assert.Equal(allocatedBefore, counters[AwkCounter.TextAllocations]);
        Assert.Equal(committedBefore, state.MemoryAccounting.LiveBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongFoldedScansObserveCancellationBetweenChunks(bool search)
    {
        var shortChecks = 0;
        Assert.Equal(0, Scan(16, () => shortChecks++));
        using var cancellation = new CancellationTokenSource();
        var checkpoints = 0;
        var error = Assert.Throws<OperationCanceledException>(() => Scan(4096, () =>
        {
            if (++checkpoints > shortChecks)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, Scan(4096, null));

        int Scan(int length, Action? checkpoint)
        {
            var strings = new AwkStringRuntime(AwkStringUnitMode.Utf8Rune, checkpoint);
            var source = AwkText.FromString(new string('A', length));
            var value = AwkText.FromString(search ? new string('a', length / 4) + "b" : new string('a', length));
            return search
                ? strings.IndexOfUnits(source.AsSpan(), value.AsSpan(), ignoreCase: true)
                : strings.Compare(source, value, ignoreCase: true, sorting: false);
        }
    }

    private static AwkRuntimeState CreateState(AwkRuntimeCounters counters, AwkRuntimeLimits limits)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase, AwkValue.FromNumber(1));
        return state;
    }
}
