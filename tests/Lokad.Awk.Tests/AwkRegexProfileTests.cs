using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexProfileTests
{
    [Theory]
    [InlineData(AwkCompatibilityMode.GnuDefault, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.Posix, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.Traditional, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.GnuDefault, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkCompatibilityMode.Posix, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkCompatibilityMode.Traditional, AwkRegexEngineKind.Utf8Adapter)]
    internal void CachedPatternsRetainTheirProfile(AwkCompatibilityMode mode, AwkRegexEngineKind engine)
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
    [InlineData(AwkCompatibilityMode.GnuDefault, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.Posix, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.Traditional, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.GnuDefault, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkCompatibilityMode.Posix, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkCompatibilityMode.Traditional, AwkRegexEngineKind.Utf8Adapter)]
    internal void TranslatedPatternLimitsPreflightCacheRetention(AwkCompatibilityMode mode, AwkRegexEngineKind engine)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxRegexPatternBytes(2);
        var governor = AwkMemoryGovernor.Create(limits, counters);
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, counters, engine,
            new AwkRuntimeResourceMonitor(limits, counters), governor, mode);
        var pattern = new AwkRegexPattern(@"\w");
        if (mode == AwkCompatibilityMode.GnuDefault)
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => runtime.Create(pattern));
            Assert.Equal(AwkDiagnosticId.RuntimeResourceLimit, failure.Id);
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Throws<AwkRuntimeException>(() => runtime.Create(pattern));
            Assert.Equal(2, counters.RegexCompiles);
        }
        else
        {
            var first = runtime.Create(pattern);
            Assert.Same(first, runtime.Create(pattern));
            Assert.Equal(1, counters.RegexCompiles);
        }
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData("[W]")]
    [InlineData("[WW]")]
    [InlineData("[Ww]")]
    [InlineData("[wWw]")]
    public void CanonicalLetterClassesKeepBooleanMatchingByteNative(string pattern)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(AwkCompatibilityMode.GnuDefault, AwkRegexEngineKind.Utf8Adapter,
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

    private static AwkRegexRuntime Runtime(AwkCompatibilityMode mode, AwkRegexEngineKind engine,
        AwkRuntimeLimits limits, AwkRuntimeCounters counters) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, engine, new AwkRuntimeResourceMonitor(limits, counters),
            compatibilityMode: mode);
}
