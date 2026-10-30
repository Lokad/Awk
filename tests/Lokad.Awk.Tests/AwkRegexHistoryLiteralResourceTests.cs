namespace Lokad.Awk.Tests;

public sealed class AwkRegexHistoryLiteralResourceTests
{
    [Theory]
    [InlineData(".")]
    [InlineData("[")]
    [InlineData("\n")]
    [InlineData("large")]
    public void LiteralPreparationRejectsUnadmittedOutputAndAllowsRetry(string shape)
    {
        var text = shape == "large" ? new string('.', 8192) : shape;
        using var memoryForGovernor = Governor(1);
        var governor = memoryForGovernor.Accounting;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = Runtime(governor,counters);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var error = Assert.Throws<AwkRuntimeException>(() => runtime.CreateLiteral(new AwkRegexLiteralText(text)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(0, counters.RegexCompiles);
        }
        using var memoryForAdmitted = Governor(1 << 24);
        var admitted = memoryForAdmitted.Accounting;
        using var retry = Runtime(admitted,counters);
        var regex = retry.CreateLiteral(new AwkRegexLiteralText(text));
        Assert.Same(regex, retry.CreateLiteral(new AwkRegexLiteralText(text)));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.True(admitted.CurrentCommittedBytes >= AwkRuntimeMemory.EstimateTextBytes(regex.Pattern));
        retry.Dispose();
        Assert.Equal(0, admitted.CurrentCommittedBytes);
        Assert.Equal(0, admitted.CurrentReservedBytes);
    }

    [Fact]
    public void WarningHistoryGrowthCancelsDuringCopyBeforeReportingAndPreservesOldEntries()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 24, counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = false;
        var reports = 0;
        var history = new AwkRegexUnicodeWarnings(governor, AwkCompatibilityMode.GnuDefault,
            _ => reports++, () =>
            {
                if (armed && governor.CurrentReservedBytes > 0 && ++checks == 3) cancellation.Cancel();
                if (armed) cancellation.Token.ThrowIfCancellationRequested();
            });
        var sites = Enumerable.Range(0, 128).Select(_ => new object()).ToArray();
        foreach (var site in sites)
            history.Report(new AwkRegexPattern(@"\u", warningSite: site), AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(128, reports);
        var committed = governor.CurrentCommittedBytes;
        var target = new AwkRegexPattern(@"\u", warningSite: new object());
        armed = true;
        var error = Assert.Throws<OperationCanceledException>(() => history.Report(target, AwkRegexCaseSensitivity.CaseSensitive));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(128, reports);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        foreach (var site in sites)
            history.Report(new AwkRegexPattern(@"\u", warningSite: site), AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(128, reports);
        history.Report(target, AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(129, reports);
        history.Report(target, AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(129, reports);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("plain")]
    [InlineData("ascii")]
    [InlineData("mixed")]
    [InlineData("small")]
    [InlineData("sparse")]
    [InlineData("pool-growth")]
    public void LiteralEscapingPreservesSpellingAndAdmitsOwnedBytes(string shape)
    {
        var text = shape switch
        {
            "empty" => string.Empty,
            "plain" => "aé\U0001F600]}\v",
            "ascii" => new string(Enumerable.Range(0, 128).Select(value => (char)value).ToArray()),
            "mixed" => "aé\U0001F600.*[\\\t\n\r\f #]{|}$?()^+",
            "small" => ".",
            "sparse" => "abc.",
            "pool-growth" => new string('.', 4096),
            _ => throw new ArgumentException("Unknown escape shape.")
        };
        var expected = System.Text.RegularExpressions.Regex.Escape(text);
        var bytes = ReferenceEquals(text, expected) ? 0 : AwkRuntimeMemory.EstimateTextBytes(AwkText.FromString(expected));
        var input = AwkText.FromString(text);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 24, counters);
        var governor = memoryForGovernor.Accounting;
        using (var resultText = governor.CreateTransientLease())
        {
            var actual = AwkRegexConstruction.EscapeLiteral(input, () => { }, governor, resultText);
            Assert.Equal(expected, actual.AsString());
            Assert.Equal(bytes, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            if (bytes == 0) Assert.Same(input, actual);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        var peak = counters.MemoryPeakLiveBytes;
        using var memoryForExact = Governor(Math.Max(1, peak));
        var exact = memoryForExact.Accounting;
        using (var resultText = exact.CreateTransientLease())
            Assert.Equal(expected, AwkRegexConstruction.EscapeLiteral(input, () => { }, exact, resultText).AsString());
        Assert.Equal(0, exact.CurrentCommittedBytes);
        if (peak == 0) return;
        using var memoryForShortGovernor = Governor(peak - 1);
        var shortGovernor = memoryForShortGovernor.Accounting;
        using var rejected = shortGovernor.CreateTransientLease();
        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkRegexConstruction.EscapeLiteral(input, () => { }, shortGovernor, rejected));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
        Assert.Equal(0, shortGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void LiteralCacheCancellationReleasesAdmissionWithoutPublishing()
    {
        var bytes = AwkRuntimeMemory.EstimateTextBytes(AwkText.FromString(@"\."));
        using var memoryForProbeGovernor = Governor(1 << 20);
        var probeGovernor = memoryForProbeGovernor.Accounting;
        var heldChecks = 0;
        using var probe = Runtime(probeGovernor, AwkRuntimeCounters.Disabled, () => heldChecks++);
        _ = probe.CreateLiteral(new AwkRegexLiteralText("."));
        Assert.True(heldChecks > 0);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var armed = true;
        var checks = 0;
        using var runtime = Runtime(governor,counters,() =>
        {
            if (armed && ++checks == heldChecks) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        var error = Assert.Throws<OperationCanceledException>(() => runtime.CreateLiteral(new AwkRegexLiteralText(".")));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        var retry = runtime.CreateLiteral(new AwkRegexLiteralText("."));
        Assert.Same(retry, runtime.CreateLiteral(new AwkRegexLiteralText(".")));
        Assert.Equal(2, counters.RegexCompiles);
        Assert.True(counters.MemoryPeakLiveBytes >= bytes);
        runtime.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("measure")]
    [InlineData("construct")]
    public void LiteralCancellationReleasesScratchAndPreservesBorrowedResultCharge(string phase)
    {
        var text = new string('.', 8192);
        var input = AwkText.FromString(text);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 24, counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        using (var resultText = governor.CreateTransientLease())
        {
            resultText.ReserveAdditional(43);
            var error = Assert.Throws<OperationCanceledException>(() =>
                AwkRegexConstruction.EscapeLiteral(input, () =>
                {
                    if (phase == "measure" && ++checks == 3 || phase == "construct" && governor.CurrentCommittedBytes > 43)
                        cancellation.Cancel();
                    cancellation.Token.ThrowIfCancellationRequested();
                }, governor, resultText));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(43, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        using var retry = governor.CreateTransientLease();
        Assert.Equal(System.Text.RegularExpressions.Regex.Escape(text),
            AwkRegexConstruction.EscapeLiteral(input, () => { }, governor, retry).AsString());
    }

    [Fact]
    public void WarningHistoryShortfallPreservesPriorSitesAndResetAllowsRetry()
    {
        var sites = Enumerable.Range(0, 4).Select(_ => new object()).ToArray();
        var target = new object();
        var probeCounters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForProbeGovernor = new CooperativeTestMemory(1 << 20, probeCounters);
        var probeGovernor = memoryForProbeGovernor.Accounting;
        var probe = new AwkRegexUnicodeWarnings(probeGovernor, AwkCompatibilityMode.GnuDefault, _ => { }, () => { });
        foreach (var site in sites.Append(target))
            probe.Report(new AwkRegexPattern(@"\u", warningSite: site), AwkRegexCaseSensitivity.CaseSensitive);
        using var memoryForGovernor = Governor(probeCounters.MemoryPeakLiveBytes - 1);
        var governor = memoryForGovernor.Accounting;
        var reports = 0;
        var history = new AwkRegexUnicodeWarnings(governor, AwkCompatibilityMode.GnuDefault, _ => reports++, () => { });
        foreach (var site in sites)
            history.Report(new AwkRegexPattern(@"\u", warningSite: site), AwkRegexCaseSensitivity.CaseSensitive);
        var committed = governor.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            history.Report(new AwkRegexPattern(@"\u", warningSite: target), AwkRegexCaseSensitivity.CaseSensitive));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(4, reports);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        foreach (var site in sites)
            history.Report(new AwkRegexPattern(@"\u", warningSite: site), AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(4, reports);
        history.Reset(sites[0]);
        Assert.True(governor.CurrentCommittedBytes < committed);
        history.Report(new AwkRegexPattern(@"\u", warningSite: target), AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(5, reports);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void WarningProfileHistoryRetainsCaseAndResetContracts(int profile)
    {
        using var memoryForGovernor = Governor(1 << 20);
        var governor = memoryForGovernor.Accounting;
        var reports = 0;
        var history = new AwkRegexUnicodeWarnings(governor, (AwkCompatibilityMode)profile, _ => reports++, () => { });
        var site = new object();
        var pattern = new AwkRegexPattern(@"\u", warningSite: site);
        foreach (var sensitivity in new[] { AwkRegexCaseSensitivity.CaseSensitive, AwkRegexCaseSensitivity.IgnoreCase })
        {
            history.Report(pattern, sensitivity);
            history.Report(pattern, sensitivity);
        }
        Assert.Equal(profile == 1 ? 0 : 2, reports);
        var committed = governor.CurrentCommittedBytes;
        history.Reset(site);
        if (profile == 1)
        {
            Assert.Equal(0, committed);
        }
        else Assert.True(governor.CurrentCommittedBytes < committed);
        history.Report(pattern, AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(profile == 1 ? 0 : 3, reports);
        history.Report(new AwkRegexPattern(@"\u", AwkRegexPatternOrigin.SourceLiteral, site), AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(profile == 1 ? 0 : 3, reports);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    private static CooperativeTestMemory Governor(long bytes) =>
        new CooperativeTestMemory(bytes, AwkRuntimeCounters.Disabled);

    private static AwkRegexRuntime Runtime(AwkMemoryAccounting governor,AwkRuntimeCounters counters,        Action? checkpoint = null) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint ?? (() => { }), counters,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters), governor, regexEngine: AwkUtf8RegexEngine.Instance);
}
