namespace Lokad.Awk.Tests;

public sealed class AwkRegexHistoryLiteralResourceTests
{
    [Theory]
    [InlineData(false, ".")]
    [InlineData(true, ".")]
    [InlineData(false, "[")]
    [InlineData(true, "[")]
    [InlineData(false, "\n")]
    [InlineData(true, "\n")]
    [InlineData(false, "large")]
    [InlineData(true, "large")]
    public void LiteralPreparationRejectsUnadmittedOutputAndAllowsRetry(bool utf8, string shape)
    {
        var text = shape == "large" ? new string('.', 8192) : shape;
        var governor = Governor(1);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(utf8, governor, counters);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var error = Assert.Throws<AwkRuntimeException>(() => runtime.CreateLiteral(new AwkRegexLiteralText(text)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(0, counters.RegexCompiles);
        }
        var admitted = Governor(1 << 24);
        var retry = Runtime(utf8, admitted, counters);
        var regex = retry.CreateLiteral(new AwkRegexLiteralText(text));
        Assert.Same(regex, retry.CreateLiteral(new AwkRegexLiteralText(text)));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, admitted.CurrentCommittedBytes);
        Assert.Equal(0, admitted.CurrentReservedBytes);
    }

    [Fact]
    public void WarningHistoryGrowthCancelsDuringCopyBeforeReportingAndPreservesOldEntries()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 24), counters);
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
    public void LiteralEscapingPreservesBclResultsAndRetainsOneExactString(string shape)
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
        var bytes = ReferenceEquals(text, expected) ? 0 : 24L + 2L * expected.Length;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 24), counters);
        using (var resultText = governor.CreateTransientLease())
        {
            var actual = AwkRegexConstruction.EscapeLiteral(text, () => { }, governor, resultText);
            Assert.Equal(expected, actual);
            Assert.Equal(bytes, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            if (bytes == 0) Assert.Same(text, actual);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        var peak = counters.MemoryPeakLiveBytes;
        // A uniform twice-input bound would reject this valid exact-budget literal.
        if (shape == "sparse") Assert.Equal(34, peak);
        var exact = Governor(Math.Max(1, peak));
        using (var resultText = exact.CreateTransientLease())
            Assert.Equal(expected, AwkRegexConstruction.EscapeLiteral(text, () => { }, exact, resultText));
        Assert.Equal(0, exact.CurrentCommittedBytes);
        if (peak == 0) return;
        var shortGovernor = Governor(peak - 1);
        using var rejected = shortGovernor.CreateTransientLease();
        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkRegexConstruction.EscapeLiteral(text, () => { }, shortGovernor, rejected));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
        Assert.Equal(0, shortGovernor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiteralCacheCancellationReleasesAdmissionWithoutPublishing(bool utf8)
    {
        const long bytes = 28; // Regex.Escape(".") returns two UTF-16 units.
        var probeGovernor = Governor(1 << 20);
        var heldChecks = 0;
        _ = Runtime(utf8, probeGovernor, AwkRuntimeCounters.Disabled, () =>
        {
            if (probeGovernor.CurrentCommittedBytes == bytes) heldChecks++;
        }).CreateLiteral(new AwkRegexLiteralText("."));
        Assert.True(heldChecks > 0);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20), counters);
        using var cancellation = new CancellationTokenSource();
        var armed = true;
        var checks = 0;
        var runtime = Runtime(utf8, governor, counters, () =>
        {
            if (armed && governor.CurrentCommittedBytes == bytes && ++checks == heldChecks) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        var error = Assert.Throws<OperationCanceledException>(() => runtime.CreateLiteral(new AwkRegexLiteralText(".")));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        var retry = runtime.CreateLiteral(new AwkRegexLiteralText("."));
        Assert.Same(retry, runtime.CreateLiteral(new AwkRegexLiteralText(".")));
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Equal(bytes, counters.MemoryPeakLiveBytes);
    }

    [Theory]
    [InlineData("measure")]
    [InlineData("construct")]
    public void LiteralCancellationReleasesScratchAndPreservesBorrowedResultCharge(string phase)
    {
        var text = new string('.', 8192);
        var bytes = 24L + 4L * text.Length;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 24), counters);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        using (var resultText = governor.CreateTransientLease())
        {
            resultText.ReserveAdditional(43);
            var error = Assert.Throws<OperationCanceledException>(() =>
                AwkRegexConstruction.EscapeLiteral(text, () =>
                {
                    if (phase == "measure" && ++checks == 3 || phase == "construct" && governor.CurrentCommittedBytes > 43)
                        cancellation.Cancel();
                    cancellation.Token.ThrowIfCancellationRequested();
                }, governor, resultText));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(43 + (phase == "construct" ? bytes : 0), governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        using var retry = governor.CreateTransientLease();
        Assert.Equal(System.Text.RegularExpressions.Regex.Escape(text),
            AwkRegexConstruction.EscapeLiteral(text, () => { }, governor, retry));
    }

    [Fact]
    public void WarningHistoryShortfallPreservesPriorSitesAndResetAllowsRetry()
    {
        var sites = Enumerable.Range(0, 4).Select(_ => new object()).ToArray();
        var target = new object();
        var probeCounters = AwkRuntimeCounters.CreateEnabled();
        var probeGovernor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20), probeCounters);
        var probe = new AwkRegexUnicodeWarnings(probeGovernor, AwkCompatibilityMode.GnuDefault, _ => { }, () => { });
        foreach (var site in sites.Append(target))
            probe.Report(new AwkRegexPattern(@"\u", warningSite: site), AwkRegexCaseSensitivity.CaseSensitive);
        var governor = Governor(probeCounters.MemoryPeakLiveBytes - 1);
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
        var governor = Governor(1 << 20);
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

    private static AwkMemoryAccounting Governor(long bytes) =>
        AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(bytes));

    private static AwkRegexRuntime Runtime(bool utf8, AwkMemoryAccounting governor, AwkRuntimeCounters counters,
        Action? checkpoint = null) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint ?? (() => { }), counters,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters), governor, regexEngine: AwkUtf8RegexEngine.Instance);
}
