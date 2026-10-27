namespace Lokad.Awk.Tests;

public sealed class AwkRegexOwnedMemoryTests
{

    [Theory]
    [InlineData("x", "longer")]
    [InlineData("longer", "x")]
    [InlineData("x", "")]
    [InlineData("x", "λ\U00010400")]
    [InlineData("x", "y")]
    public void ReplacementAdmitsOneExactStringAcrossCopyWindows(string oldValue, string newValue)
    {
        var text = new string('a', AwkRegexConstruction.ChunkCharacters - 1) + oldValue + oldValue +
            new string('b', AwkRegexConstruction.ChunkCharacters * 2);
        var expected = text.Replace(oldValue, newValue, StringComparison.Ordinal);
        var bytes = 24L + 2L * expected.Length;
        using var memoryForGovernor = Governor(bytes);
        var governor = memoryForGovernor.Accounting;
        using (var lease = governor.CreateTransientLease())
        {
            Assert.Equal(expected, AwkRegexConstruction.Replace(text, oldValue, newValue, () => { }, lease));
            Assert.Equal(bytes, governor.CurrentCommittedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        using var memoryForShortGovernor = Governor(bytes - 1);
        var shortGovernor = memoryForShortGovernor.Accounting;
        using (var lease = shortGovernor.CreateTransientLease())
        {
            var error = Assert.Throws<AwkRuntimeException>(() =>
                AwkRegexConstruction.Replace(text, oldValue, newValue, () => { }, lease));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
        }
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("identity")]
    [InlineData("erase")]
    public void ReplacementSharesNoOpAndEmptyResults(string shape)
    {
        var text = new string('a', 8192);
        var oldValue = shape == "absent" ? "x" : "a";
        var newValue = shape == "identity" ? "a" : "";
        using var memoryForGovernor = Governor(1);
        var governor = memoryForGovernor.Accounting;
        using var lease = governor.CreateTransientLease();
        var result = AwkRegexConstruction.Replace(text, oldValue, newValue, () => { }, lease);
        Assert.Same(shape == "erase" ? string.Empty : text, result);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("measure")]
    [InlineData("copy")]
    [InlineData("final")]
    public void ReplacementCancellationKeepsItsChargeUntilCallerDisposal(string phase)
    {
        var text = "x" + new string('a', 16384) + "x";
        var expected = text.Replace("x", "long", StringComparison.Ordinal);
        var bytes = 24L + 2L * expected.Length;
        var total = 0;
        _ = AwkRegexConstruction.Replace(text, "x", "long", () => total++);
        using var memoryForGovernor = Governor(bytes);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var copyChecks = 0;
        using (var lease = governor.CreateTransientLease())
        {
            var error = Assert.Throws<OperationCanceledException>(() =>
                AwkRegexConstruction.Replace(text, "x", "long", () =>
                {
                    checks++;
                    if (phase == "measure" && checks == 3 || phase == "final" && checks == total ||
                        phase == "copy" && governor.CurrentCommittedBytes > 0 && ++copyChecks == 6)
                        cancellation.Cancel();
                    cancellation.Token.ThrowIfCancellationRequested();
                }, lease));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(phase == "measure" ? 0 : bytes, governor.CurrentCommittedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        using var retry = governor.CreateTransientLease();
        Assert.Equal(expected, AwkRegexConstruction.Replace(text, "x", "long", () => { }, retry));
    }

    [Fact]
    public void ChainedPosixResultsRemainAdmittedThroughPublication()
    {
        const string pattern = "[[:alpha:][:digit:]]";
        // alpha replacement has fourteen UTF-16 units; digit replacement has eight.
        const long bytes = 52 + 40;
        using var memoryForTokenGovernor = Governor(bytes);
        var tokenGovernor = memoryForTokenGovernor.Accounting;
        using (var tokenText = tokenGovernor.CreateTransientLease())
        {
            _ = AwkRegex.TranslatePattern(new AwkRegexPattern(pattern), AwkCompatibilityMode.GnuDefault,
                () => { }, tokenText);
            Assert.Equal(bytes, tokenGovernor.CurrentCommittedBytes);
        }
        Assert.Equal(0, tokenGovernor.CurrentCommittedBytes);
        using var memoryForGovernor = Governor(1 << 24);
        var governor = memoryForGovernor.Accounting;
        var observed = 0;
        var runtime = Runtime(governor,() =>
        {
            if (governor.CurrentCommittedBytes >= bytes) observed++;
        });
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        Assert.True(observed > 0);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Same(regex, runtime.Create(new AwkRegexPattern(pattern)));
        // Exactly enough for token results is no longer enough for scalar scratch.
        using var memoryForShortGovernor = Governor(bytes);
        // Exactly enough for token results is no longer enough for scalar scratch.
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
        var runtime = Runtime(governor,counters: counters);
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var error = Assert.Throws<AwkRuntimeException>(() =>
                runtime.Create(new AwkRegexPattern("[[:digit:]](")));
            Assert.Equal(AwkDiagnosticId.RuntimeInvalidRegex, error.Id);
            Assert.Equal(AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorInvalidRegex), AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(attempt, counters.RegexCompiles);
        }
    }

    [Fact]
    public void FinalCacheCancellationReleasesChainedTextAndDoesNotPublish()
    {
        const string pattern = "[[:alpha:][:digit:]]";
        using var memoryForProbeGovernor = Governor(1 << 24);
        var probeGovernor = memoryForProbeGovernor.Accounting;
        var heldChecks = new List<long>();
        _ = Runtime(probeGovernor,() =>
        {
            heldChecks.Add(probeGovernor.CurrentCommittedBytes);
        }).Create(new AwkRegexPattern(pattern));
        var bytes = heldChecks.Last(value => value > 0);
        Assert.True(bytes > 92);
        var totalHeldChecks = heldChecks.Count(value => value == bytes);
        using var memoryForGovernor = Governor(1 << 24);
        var governor = memoryForGovernor.Accounting;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = true;
        var runtime = Runtime(governor,() =>
        {
            if (armed && governor.CurrentCommittedBytes == bytes && ++checks == totalHeldChecks)
                cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        },counters);
        var error = Assert.Throws<OperationCanceledException>(() => runtime.Create(new AwkRegexPattern(pattern)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        var retry = runtime.Create(new AwkRegexPattern(pattern));
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Same(retry, runtime.Create(new AwkRegexPattern(pattern)));
        Assert.Equal(2, counters.RegexCompiles);
    }

    private static CooperativeTestMemory Governor(long bytes) =>
        new CooperativeTestMemory(bytes, AwkRuntimeCounters.Disabled);

    private static AwkRegexRuntime Runtime(AwkMemoryAccounting governor,Action? checkpoint = null,        AwkRuntimeCounters? counters = null) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint ?? (() => { }), counters ?? AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters ?? AwkRuntimeCounters.Disabled), governor, regexEngine: AwkUtf8RegexEngine.Instance);
}
