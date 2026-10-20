using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexProfileTests
{
    [Theory]
    [InlineData(AwkCompatibilityMode.GnuDefault, false)]
    [InlineData(AwkCompatibilityMode.Posix, false)]
    [InlineData(AwkCompatibilityMode.Traditional, false)]
    [InlineData(AwkCompatibilityMode.GnuDefault, true)]
    [InlineData(AwkCompatibilityMode.Posix, true)]
    [InlineData(AwkCompatibilityMode.Traditional, true)]
    internal void CachedPatternsRetainTheirProfile(AwkCompatibilityMode mode, bool engine)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var runtime = Runtime(mode, engine, limits, counters);
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
    [InlineData("[W]")]
    [InlineData("[WW]")]
    [InlineData("[Ww]")]
    [InlineData("[wWw]")]
    public void CanonicalLetterClassesKeepBooleanMatchingByteNative(string pattern)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(AwkCompatibilityMode.GnuDefault, true,
            AwkRuntimeLimits.CreateDefault(), counters);
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var input = AwkText.FromString("W", counters);
        Assert.True(runtime.IsMatch(regex, input));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(1, counters.RegexMatches);
        Assert.Equal(0, counters.RegexPatternGapFallbacks);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    private static AwkRegexRuntime Runtime(AwkCompatibilityMode mode, bool engine,
        AwkRuntimeLimits limits, AwkRuntimeCounters counters) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, new AwkRuntimeResourceMonitor(limits, counters),
            compatibilityMode: mode, regexEngine: engine ? AwkUtf8RegexEngine.Instance : null);
}
