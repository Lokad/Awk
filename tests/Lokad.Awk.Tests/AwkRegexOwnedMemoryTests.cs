namespace Lokad.Awk.Tests;

public sealed class AwkRegexOwnedMemoryTests
{
    [Theory]
    [InlineData(false, "nested")]
    [InlineData(true, "nested")]
    [InlineData(false, "wide")]
    [InlineData(true, "wide")]
    [InlineData(false, "posix")]
    [InlineData(true, "posix")]
    [InlineData(false, "mixed")]
    [InlineData(true, "mixed")]
    public void RuntimeRejectsUnadmittedOwnedPreparationAndAllowsRetry(bool utf8, string shape)
    {
        var governor = Governor(1);
        var runtime = Runtime(utf8, governor);
        var pattern = new AwkRegexPattern(Pattern(shape));
        var error = Assert.Throws<AwkRuntimeException>(() => runtime.Create(pattern));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        var retryGovernor = Governor(1 << 20);
        var retry = Runtime(utf8, retryGovernor);
        var regex = retry.Create(pattern);
        Assert.Equal(0, retryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, retryGovernor.CurrentReservedBytes);
        Assert.Same(regex, retry.Create(pattern));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 216)]
    [InlineData(4, 216)]
    [InlineData(5, 624)]
    [InlineData(8, 624)]
    [InlineData(9, 1200)]
    [InlineData(16, 1200)]
    [InlineData(17, 2352)]
    [InlineData(65, 9264)]
    public void AnalysisAdmitsTheOldAndNewBuffersDuringGrowth(int depth, long expectedPeak)
    {
        var text = new string('(', depth) + "a|aa" + new string(')', depth);
        var governor = Governor(Math.Max(1, expectedPeak));
        var selection = AwkRegexSelection.Analyze(text, governor: governor);
        Assert.Equal(2, selection.MaximumScalars);
        Assert.True(selection.RequiresLongest);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        if (expectedPeak > 0)
        {
            var shortGovernor = Governor(expectedPeak - 1);
            var error = Assert.Throws<AwkRuntimeException>(() =>
                AwkRegexSelection.Analyze(text, governor: shortGovernor));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
            Assert.Equal(0, shortGovernor.CurrentReservedBytes);
        }
    }

    [Theory]
    [InlineData("scan")]
    [InlineData("copy")]
    [InlineData("end")]
    [InlineData("malformed")]
    public void AnalysisCancellationReleasesAllAdmittedBuffers(string phase)
    {
        var text = phase switch
        {
            "scan" => "(" + new string('a', 16384) + ")",
            "copy" => new string('(', 65) + "a" + new string(')', 65),
            "end" => "((a))",
            "malformed" => "((a)",
            _ => throw new ArgumentException("Unknown cancellation phase.")
        };
        var governor = Governor(1 << 20);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var error = Assert.Throws<OperationCanceledException>(() =>
            AwkRegexSelection.Analyze(text, () =>
            {
                var live = governor.CurrentCommittedBytes;
                var watched = phase == "copy" ? live == 9264 : live > 0;
                if (watched && ++checks == (phase == "scan" ? 3 : 2)) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }, governor));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        _ = AwkRegexSelection.Analyze(text, () => { }, governor);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

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
        var governor = Governor(bytes);
        using (var lease = governor.CreateTransientLease())
        {
            Assert.Equal(expected, AwkRegexConstruction.Replace(text, oldValue, newValue, () => { }, lease));
            Assert.Equal(bytes, governor.CurrentCommittedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        var shortGovernor = Governor(bytes - 1);
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
        var governor = Governor(1);
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
        var governor = Governor(bytes);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChainedPosixResultsRemainAdmittedThroughPublication(bool utf8)
    {
        const string pattern = "[[:alpha:][:digit:]]";
        // alpha replacement has fourteen UTF-16 units; digit replacement has eight.
        const long bytes = 52 + 40;
        var tokenGovernor = Governor(bytes);
        using (var tokenText = tokenGovernor.CreateTransientLease())
        {
            _ = AwkRegex.TranslatePattern(new AwkRegexPattern(pattern), AwkCompatibilityMode.GnuDefault,
                () => { }, tokenText);
            Assert.Equal(bytes, tokenGovernor.CurrentCommittedBytes);
        }
        Assert.Equal(0, tokenGovernor.CurrentCommittedBytes);
        var governor = Governor(1 << 24);
        var observed = 0;
        var runtime = Runtime(utf8, governor, () =>
        {
            if (governor.CurrentCommittedBytes >= bytes) observed++;
        });
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        Assert.True(observed > 0);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Same(regex, runtime.Create(new AwkRegexPattern(pattern)));
        // Exactly enough for token results is no longer enough for scalar scratch.
        var shortGovernor = Governor(bytes);
        var error = Assert.Throws<AwkRuntimeException>(() =>
            Runtime(utf8, shortGovernor).Create(new AwkRegexPattern(pattern)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidRegexReleasesTranslationAndAnalysisBeforeRetry(bool utf8)
    {
        var governor = Governor(1 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(utf8, governor, counters: counters);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FinalCacheCancellationReleasesChainedTextAndDoesNotPublish(bool utf8)
    {
        const string pattern = "[[:alpha:][:digit:]]";
        var probeGovernor = Governor(1 << 24);
        var heldChecks = new List<long>();
        _ = Runtime(utf8, probeGovernor, () =>
        {
            heldChecks.Add(probeGovernor.CurrentCommittedBytes);
        }).Create(new AwkRegexPattern(pattern));
        var bytes = heldChecks.Last(value => value > 0);
        Assert.True(bytes > 92);
        var totalHeldChecks = heldChecks.Count(value => value == bytes);
        var governor = Governor(1 << 24);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = true;
        var runtime = Runtime(utf8, governor, () =>
        {
            if (armed && governor.CurrentCommittedBytes == bytes && ++checks == totalHeldChecks)
                cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        }, counters);
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

    private static string Pattern(string shape) => shape switch
    {
        "nested" => new string('(', 16) + "a" + new string(')', 16),
        "wide" => string.Concat(Enumerable.Repeat("(a)", 16)),
        "posix" => "[[:digit:]]",
        "mixed" => "[[:alpha:]][[:digit:]]",
        _ => throw new ArgumentException("Unknown pattern shape.")
    };

    private static AwkMemoryAccounting Governor(long bytes) =>
        AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(bytes));

    private static AwkRegexRuntime Runtime(bool utf8, AwkMemoryAccounting governor, Action? checkpoint = null,
        AwkRuntimeCounters? counters = null) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint ?? (() => { }), counters ?? AwkRuntimeCounters.Disabled,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters ?? AwkRuntimeCounters.Disabled), governor, regexEngine: AwkUtf8RegexEngine.Instance);
}
