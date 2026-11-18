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
        var runtime = Runtime(mode, counters);
        var pattern = new AwkRegexPattern(@"\w");
        var first = runtime.Create(pattern);
        Assert.Same(first, runtime.Create(pattern));

        Assert.Equal(mode == AwkCompatibilityMode.GnuDefault,
            runtime.IsMatch(first, AwkText.FromString("a")));
        Assert.True(runtime.IsMatch(first, AwkText.FromString("w")));

    }

    [Theory]
    [InlineData("[W]")]
    [InlineData("[WW]")]
    [InlineData("[Ww]")]
    [InlineData("[wWw]")]
    public void CanonicalLetterClassesKeepBooleanMatchingByteNative(string pattern)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(AwkCompatibilityMode.GnuDefault, counters);
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var input = AwkText.FromString("W");
        Assert.True(runtime.IsMatch(regex, input));

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

    }

    private static AwkRegexRuntime Runtime(AwkCompatibilityMode mode, AwkRuntimeCounters counters) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters,
            compatibilityMode: mode, regexEngine: AwkUtf8RegexEngine.Instance);
}
