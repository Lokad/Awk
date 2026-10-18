namespace Lokad.Awk.Tests;

public sealed class AwkUnicodePrintClassTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void UnicodeMembershipRetainsCacheAndByteNativeCounters(int profile, bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(profile, utf8, counters);
        var graphPattern = new AwkRegexPattern("[[:graph:]]");
        var printPattern = new AwkRegexPattern("[[:print:]]");
        var graph = runtime.Create(graphPattern);
        var printable = runtime.Create(printPattern);
        Assert.Same(graph, runtime.Create(graphPattern));
        Assert.Same(printable, runtime.Create(printPattern));
        Assert.Equal(1, graph.GroupCount);
        Assert.Equal(1, printable.GroupCount);
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
        Assert.Equal(utf8 ? 0 : samples.Length, counters.StringDecodes);
        Assert.Equal(utf8 ? 0 : matches, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.RegexPatternGapFallbacks);
        Assert.Equal(utf8 ? 0 : matches, counters.RegexDotNetTextMatches);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void ClassExpansionPreservesPublicCapturesAndCompleteScalarSpans(int profile, bool utf8)
    {
        var runtime = Runtime(profile, utf8, AwkRuntimeCounters.Disabled);
        var pattern = new AwkRegexPattern("([[:graph:]]+)([[:print:]])");
        var regex = runtime.Create(pattern);
        Assert.Equal(3, regex.GroupCount);
        Assert.Same(regex, runtime.Create(pattern));
        var match = runtime.MatchSpan(regex, AwkText.FromString("\né\U0001F600 ", AwkRuntimeCounters.Disabled));
        Assert.True(match.Success);
        Assert.Equal(1, match.ByteIndex);
        Assert.Equal(7, match.ByteLength);
        Assert.Equal(1, match.Utf16Index);
        Assert.Equal(4, match.Utf16Length);
    }

    private static AwkRegexRuntime Runtime(int profile, bool utf8, AwkRuntimeCounters counters)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        return new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(limits, counters), compatibilityMode: (AwkCompatibilityMode)profile, regexEngine: AwkUtf8RegexEngine.Instance);
    }
}
