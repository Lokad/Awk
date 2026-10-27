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
        var input = AwkText.FromString("alpha,béta;gamma", counters);
        for (var i = 0; i < 256; i++)
            runtime.EnumerateMatchSpans(regex, input).Dispose();
        var matchesBefore = counters.RegexMatches;
        var textBefore = counters.AwkTextAllocations;
        var decodesBefore = counters.StringDecodes;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_024; i++)
            runtime.EnumerateMatchSpans(regex, input).Dispose();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(matchesBefore, counters.RegexMatches);
        Assert.Equal(textBefore, counters.AwkTextAllocations);
        Assert.Equal(decodesBefore, counters.StringDecodes);
    }

    [Fact]
    public void EmptyMatchesAdvanceAndCompletionAndDisposalAreStable()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = CreateRuntime(counters);
        var regex = runtime.Create(new AwkRegexPattern("x*"));
        var input = AwkText.FromString("éa", counters);
        var spans = runtime.EnumerateMatchSpans(regex, input);
        foreach (var position in new[] { 0, 2, 3 })
        {
            Assert.True(spans.MoveNext(out var match));
            Assert.Equal(position, match.ByteIndex);
            Assert.Equal(0, match.ByteLength);
        }
        var matchesAtEnd = counters.RegexMatches;
        Assert.False(spans.MoveNext(out _));
        Assert.False(spans.MoveNext(out _));
        Assert.Equal(matchesAtEnd, counters.RegexMatches);
        spans.Dispose();
        spans.Dispose();
        var exception = Assert.Throws<ObjectDisposedException>(() => spans.MoveNext(out _));
        Assert.Equal(typeof(AwkRegexMatchSpanEnumerator).FullName, exception.ObjectName);
        Assert.Equal(matchesAtEnd, counters.RegexMatches);
    }

    private static AwkRegexRuntime CreateRuntime(AwkRuntimeCounters counters) => new(
        static () => AwkRegexCaseSensitivity.CaseSensitive,
        AwkStringUnitMode.Utf8Rune,
        static () => { },
        counters, regexEngine: AwkUtf8RegexEngine.Instance);
}
