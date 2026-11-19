namespace Lokad.Awk.Tests;

public sealed class AwkRegexOwnedMemoryTests
{

    [Fact]
    public void ChainedPosixResultsRemainAdmittedThroughPublication()
    {
        const string pattern = "[[:alpha:][:digit:]]";
        var bytes = AwkRuntimeMemory.EstimateTextBytes(AwkText.FromString(@"[\p{L}0-9]"));
        using var memoryForTokenGovernor = Governor(1 << 20);
        var tokenGovernor = memoryForTokenGovernor.Accounting;
        using (var tokenText = tokenGovernor.CreateTransientLease())
        {
            _ = AwkRegex.TranslatePattern(new AwkRegexPattern(pattern), AwkCompatibilityMode.GnuDefault,
                () => { }, tokenText, tokenGovernor);
            Assert.True(tokenGovernor.LiveBytes >= bytes);
        }
        Assert.Equal(0, tokenGovernor.LiveBytes);
        using var memoryForGovernor = Governor(1 << 24);
        var governor = memoryForGovernor.Accounting;
        var observed = 0;
        using var runtime = Runtime(governor, () =>
        {
            if (governor.LiveBytes >= bytes) observed++;
        });
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        Assert.True(observed > 0);
        var retained = governor.LiveBytes;
        Assert.True(retained >= AwkRuntimeMemory.EstimateTextBytes(regex.Pattern));
        Assert.Same(regex, runtime.Create(new AwkRegexPattern(pattern)));
        Assert.Equal(retained, governor.LiveBytes);
        runtime.Dispose();
        Assert.Equal(0, governor.LiveBytes);
        // Translation scratch must also be admitted before compilation.
        using var memoryForShortGovernor = Governor(bytes);
        // Translation scratch must also be admitted before compilation.
        var shortGovernor = memoryForShortGovernor.Accounting;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            Runtime(shortGovernor).Create(new AwkRegexPattern(pattern)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, shortGovernor.LiveBytes);
    }

    [Fact]
    public void InvalidRegexReleasesTranslationAndAnalysisBeforeRetry()
    {
        using var memoryForGovernor = Governor(1 << 20);
        var governor = memoryForGovernor.Accounting;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = Runtime(governor, counters: counters);
        var warm = runtime.Create(new AwkRegexPattern("x"));
        var retained = governor.LiveBytes;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var error = Assert.Throws<AwkRuntimeException>(() =>
                runtime.Create(new AwkRegexPattern("[[:digit:]](")));
            Assert.Equal(AwkDiagnosticId.RuntimeInvalidRegex, error.Id);
            Assert.Equal(AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorInvalidRegex), AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(retained, governor.LiveBytes);

            Assert.Same(warm, runtime.Create(new AwkRegexPattern("x")));
        }
    }

    private static CooperativeTestMemory Governor(long bytes) =>
        new CooperativeTestMemory(bytes);

    private static AwkRegexRuntime Runtime(AwkMemoryAccounting governor, Action? checkpoint = null, AwkRuntimeCounters? counters = null) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint ?? (() => { }), AwkTestHarness.CreateRuntimeOptions(counters ?? AwkRuntimeCounters.Disabled),
            governor);
}
