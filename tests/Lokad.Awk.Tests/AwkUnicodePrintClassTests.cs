namespace Lokad.Awk.Tests;

public sealed class AwkUnicodePrintClassTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnicodeMembershipRetainsCacheAndByteNativeCounters(int profile)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(profile, counters);
        var graphPattern = new AwkRegexPattern("[[:graph:]]");
        var printPattern = new AwkRegexPattern("[[:print:]]");
        var graph = runtime.Create(graphPattern);
        var printable = runtime.Create(printPattern);
        Assert.Same(graph, runtime.Create(graphPattern));
        Assert.Same(printable, runtime.Create(printPattern));
        (string Text, bool Graph, bool Print)[] samples =
        [
            ("é", true, true), ("\U0001F600", true, true), ("\U00010400", true, true),
            ("\u0301", true, true), ("\u200D", true, true), ("\uE000", true, true),
            ("\U000F0000", true, true), ("\U000E0001", true, true),
            (" ", false, true), ("\u00A0", false, true), ("\u2007", false, true),
            ("\u202F", false, true), ("\u3000", false, true),
            ("\u2028", false, false), ("\u2029", false, false),
            ("\u0085", false, false), ("\t", false, false), ("\n", false, false),
            ("\0", false, false), ("\u0378", false, false), ("\U0010FFFF", false, false)
        ];
        foreach (var sample in samples)
        {
            var input = AwkText.FromString(sample.Text, counters);
            Assert.Equal(sample.Graph, runtime.IsMatch(graph, input));
            Assert.Equal(sample.Print, runtime.IsMatch(printable, input));
        }
        var matches = samples.Length * 2;
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Equal(matches, counters.RegexMatches);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ClassExpansionPreservesPublicCapturesAndCompleteScalarSpans(int profile)
    {
        var runtime = Runtime(profile, AwkRuntimeCounters.Disabled);
        var pattern = new AwkRegexPattern("([[:graph:]]+)([[:print:]])");
        var regex = runtime.Create(pattern);
        using var captures = runtime.MatchCaptureValue(regex, AwkText.FromString("\né😀 "), AwkMemoryAccounting.Disabled);
        Assert.Equal(3, captures.Match.Groups.Count);
        Assert.Equal("é😀", captures.Match.Groups[1].Value.AsString());
        Assert.Equal(" ", captures.Match.Groups[2].Value.AsString());
        Assert.Same(regex, runtime.Create(pattern));
        var match = runtime.MatchSpan(regex, AwkText.FromString("\né\U0001F600 ", AwkRuntimeCounters.Disabled));
        Assert.True(match.Success);
        Assert.Equal(1, match.ByteIndex);
        Assert.Equal(7, match.ByteLength);
    }

    private static AwkRegexRuntime Runtime(int profile, AwkRuntimeCounters counters)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        return new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, new AwkRuntimeResourceMonitor(limits, counters),
            compatibilityMode: (AwkCompatibilityMode)profile, regexEngine: AwkUtf8RegexEngine.Instance);
    }
}
