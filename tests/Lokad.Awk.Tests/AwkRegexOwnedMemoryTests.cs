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
            Assert.True(tokenGovernor.CurrentCommittedBytes >= bytes);
        }
        Assert.Equal(0, tokenGovernor.CurrentCommittedBytes);
        using var memoryForGovernor = Governor(1 << 24);
        var governor = memoryForGovernor.Accounting;
        var observed = 0;
        using var runtime = Runtime(governor,() =>
        {
            if (governor.CurrentCommittedBytes >= bytes) observed++;
        });
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        Assert.True(observed > 0);
        var retained = governor.CurrentCommittedBytes;
        Assert.True(retained >= AwkRuntimeMemory.EstimateTextBytes(regex.Pattern));
        Assert.Same(regex, runtime.Create(new AwkRegexPattern(pattern)));
        Assert.Equal(retained, governor.CurrentCommittedBytes);
        runtime.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        // Translation scratch must also be admitted before compilation.
        using var memoryForShortGovernor = Governor(bytes);
        // Translation scratch must also be admitted before compilation.
        var shortGovernor = memoryForShortGovernor.Accounting;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            Runtime(shortGovernor).Create(new AwkRegexPattern(pattern)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
    }

    [Fact]
    public void InvalidRegexReleasesTranslationAndAnalysisBeforeRetry()
    {
        using var memoryForGovernor = Governor(1 << 20);
        var governor = memoryForGovernor.Accounting;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = Runtime(governor,counters: counters);
        var warm = runtime.Create(new AwkRegexPattern("x"));
        var retained = governor.CurrentCommittedBytes;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var error = Assert.Throws<AwkRuntimeException>(() =>
                runtime.Create(new AwkRegexPattern("[[:digit:]](")));
            Assert.Equal(AwkDiagnosticId.RuntimeInvalidRegex, error.Id);
            Assert.Equal(AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorInvalidRegex), AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(retained, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(attempt + 1, counters.RegexCompiles);
            Assert.Same(warm, runtime.Create(new AwkRegexPattern("x")));
        }
    }

    [Fact]
    public void FinalCacheCancellationReleasesChainedTextAndDoesNotPublish()
    {
        const string pattern = "[[:alpha:][:digit:]]";
        using var memoryForProbeGovernor = Governor(1 << 24);
        var probeGovernor = memoryForProbeGovernor.Accounting;
        var totalChecks = 0;
        using var probe = Runtime(probeGovernor, () => totalChecks++);
        _ = probe.Create(new AwkRegexPattern(pattern));
        using var memoryForGovernor = Governor(1 << 24);
        var governor = memoryForGovernor.Accounting;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = true;
        using var runtime = Runtime(governor, () =>
        {
            if (armed && ++checks == totalChecks) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        }, counters);
        var error = Assert.Throws<OperationCanceledException>(() => runtime.Create(new AwkRegexPattern(pattern)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        var retry = runtime.Create(new AwkRegexPattern(pattern));
        Assert.Equal(2, counters.RegexCompiles);
        var retained = governor.CurrentCommittedBytes;
        Assert.Same(retry, runtime.Create(new AwkRegexPattern(pattern)));
        Assert.Equal(retained, governor.CurrentCommittedBytes);
        Assert.Equal(2, counters.RegexCompiles);
        runtime.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    private static CooperativeTestMemory Governor(long bytes) =>
        new CooperativeTestMemory(bytes, AwkRuntimeCounters.Disabled);

    private static AwkRegexRuntime Runtime(AwkMemoryAccounting governor,Action? checkpoint = null,        AwkRuntimeCounters? counters = null) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint ?? (() => { }), counters ?? AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters ?? AwkRuntimeCounters.Disabled), governor, regexEngine: AwkUtf8RegexEngine.Instance);
}
