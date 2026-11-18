namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRegexSpanIterationTests
{
    [Fact]
    public void PreparingWarmedSpanIterationDoesNotAllocateOrMatch()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = CreateRuntime(counters);
        var regex = runtime.Create(new AwkRegexPattern("[,;]"));
        var input = AwkText.FromString("alpha,béta;gamma");
        for (var i = 0; i < 256; i++)
            runtime.EnumerateMatchSpans(regex, input).Dispose();

        var textBefore = counters[AwkCounter.TextAllocations];
        var decodesBefore = counters[AwkCounter.StringDecodes];
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_024; i++)
            runtime.EnumerateMatchSpans(regex, input).Dispose();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);

        Assert.Equal(textBefore, counters[AwkCounter.TextAllocations]);
        Assert.Equal(decodesBefore, counters[AwkCounter.StringDecodes]);
    }

    [Fact]
    public void EmptyMatchesAdvanceAndCompletionAndDisposalAreStable()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = CreateRuntime(counters);
        var regex = runtime.Create(new AwkRegexPattern("x*"));
        var input = AwkText.FromString("éa");
        var spans = runtime.EnumerateMatchSpans(regex, input);
        foreach (var position in new[] { 0, 2, 3 })
        {
            Assert.True(spans.MoveNext(out var match));
            Assert.Equal(position, match.ByteIndex);
            Assert.Equal(0, match.ByteLength);
        }

        Assert.False(spans.MoveNext(out _));
        Assert.False(spans.MoveNext(out _));

        spans.Dispose();
        spans.Dispose();
        var exception = Assert.Throws<ObjectDisposedException>(() => spans.MoveNext(out _));
        Assert.Equal(typeof(AwkRegexMatchSpanEnumerator).FullName, exception.ObjectName);

    }

    private static AwkRegexRuntime CreateRuntime(AwkRuntimeCounters counters) => new(
        static () => AwkRegexCaseSensitivity.CaseSensitive,
        AwkStringUnitMode.Utf8Rune,
        static () => { },
        AwkTestHarness.CreateRuntimeOptions(counters));
}
