using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexProfileTests
{
    [Theory]
    [InlineData(AwkCompatibilityMode.GnuDefault)]
    [InlineData(AwkCompatibilityMode.Posix)]
    [InlineData(AwkCompatibilityMode.Traditional)]
    internal void CachedPatternsRetainTheirProfile(AwkCompatibilityMode mode)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var runtime = Runtime(mode,limits,counters);
        var pattern = new AwkRegexPattern(@"\w");
        var first = runtime.Create(pattern);
        Assert.Same(first, runtime.Create(pattern));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(mode == AwkCompatibilityMode.GnuDefault,
            runtime.IsMatch(first, AwkText.FromString("a", counters)));
        Assert.True(runtime.IsMatch(first, AwkText.FromString("w", counters)));
        Assert.Equal(2, counters.RegexMatches);
    }

    [Theory]
    [InlineData("[W]", Skip = AwkRegexProviderGaps.SkipReason)]
    [InlineData("[WW]", Skip = AwkRegexProviderGaps.SkipReason)]
    [InlineData("[Ww]", Skip = AwkRegexProviderGaps.SkipReason)]
    [InlineData("[wWw]", Skip = AwkRegexProviderGaps.SkipReason)]
    public void CanonicalLetterClassesKeepBooleanMatchingByteNative(string pattern)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(AwkCompatibilityMode.GnuDefault,            AwkRuntimeLimits.CreateDefault(),counters);
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var input = AwkText.FromString("W", counters);
        Assert.True(runtime.IsMatch(regex, input));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(1, counters.RegexMatches);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    private static AwkRegexRuntime Runtime(AwkCompatibilityMode mode,        AwkRuntimeLimits limits,AwkRuntimeCounters counters) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, new AwkRuntimeResourceMonitor(limits, counters),
            compatibilityMode: mode, regexEngine: AwkUtf8RegexEngine.Instance);
}
