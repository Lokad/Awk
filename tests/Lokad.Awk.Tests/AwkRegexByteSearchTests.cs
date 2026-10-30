namespace Lokad.Awk.Tests;

public sealed class AwkRegexByteSearchTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedCapturePositionsReuseScalarProgress(bool emptyMatches)
    {
        const int count = 256;
        var input = AwkText.FromSourceLiteralString(string.Concat(Enumerable.Repeat("😀", count)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = Runtime(counters);
        var regex = runtime.Create(new AwkRegexPattern(emptyMatches ? "x*" : "(😀)"));
        using var matches = runtime.EnumerateMatchValues(regex, input, AwkMemoryAccounting.Disabled,
            AwkRegexCapturePlan.All);
        var seen = 0;
        while (matches.MoveNext(out var match))
        {
            Assert.Equal(seen, match.Index);
            Assert.Equal(emptyMatches ? 0 : 1, match.Length);
            seen++;
        }
        Assert.Equal(count + (emptyMatches ? 1 : 0), seen);
        Assert.Equal(input.ByteLength, counters.RegexCoordinateBytes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Fact]
    public void SpanAndReplacementCaptureRequestsDoNotCountLanguagePositions()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = Runtime(counters);
        var regex = runtime.Create(new AwkRegexPattern("(😀)"));
        var input = AwkText.FromSourceLiteralString(new string('a', 8192) + "😀😀");
        Assert.Equal(8192, runtime.MatchSpan(regex, input).ByteIndex);
        using var matches = runtime.EnumerateMatchValues(regex, input, AwkMemoryAccounting.Disabled,
            AwkRegexCapturePlan.ThroughGroup(1));
        var count = 0;
        while (matches.MoveNext(out var match))
        {
            Assert.True(match.Groups[1].Value.AsSpan().SequenceEqual("😀"u8));
            count++;
        }
        Assert.Equal(2, count);
        Assert.Equal(0, counters.RegexCoordinateBytes);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public void ScalarCaptureCoordinatesIncludeGroupsBeforeTheWholeMatch()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = Runtime(counters);
        var regex = runtime.Create(new AwkRegexPattern("(?<=(é))😀"));
        using var match = runtime.MatchCaptureValue(regex, AwkText.FromSourceLiteralString("é😀x"),
            AwkMemoryAccounting.Disabled);
        Assert.Equal(1, match.Match.Index);
        Assert.Equal(1, match.Match.Length);
        Assert.Equal(0, match.Match.Groups[1].Index);
        Assert.Equal(1, match.Match.Groups[1].Length);
        Assert.Equal(6, counters.RegexCoordinateBytes);
    }

    private static AwkRegexRuntime Runtime(AwkRuntimeCounters counters) =>
        new(static () => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, regexEngine: AwkUtf8RegexEngine.Instance);
}
