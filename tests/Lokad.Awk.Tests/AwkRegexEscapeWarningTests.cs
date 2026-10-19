namespace Lokad.Awk.Tests;

public sealed class AwkRegexEscapeWarningTests
{
    public static IEnumerable<object[]> Modes()
    {
        foreach (var profile in new[] { "", "--posix", "--traditional" })

            foreach (var utf8 in new[] { false, true })
                yield return [profile, utf8];
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task CompiledWarningHistoryIsFreshAndSharedWithDynamicPatterns(
        string profile, bool utf8)
    {
        var source = new MockFileSystem();
        source.AddFile("/warning.awk", "function dead(){return /\\q\\q/}\nBEGIN{print \"q\"~\"\\\\q\";print \"d\"~\"\\\\d\";print \"d\"~\"\\\\d\"}");
        string[] args = [.. profile.Length == 0 ? Array.Empty<string>() : new[] { profile }, "-f", "/warning.awk"];
        var compiled = await AwkExecutor.CompileAsync(source,
            AwkTestHarness.BuildInvocation("awk", args, []), CancellationToken.None);
        var diagnostic = Assert.Single(compiled.Warnings);
        Assert.Equal(AwkDiagnosticId.WarningRegexEscape, diagnostic.Id);
        Assert.Equal(AwkRegexEscapeWarningKind.UnknownOperator, diagnostic.RegexEscapeWarning.Kind);
        Assert.Equal(new System.Text.Rune('q'), diagnostic.RegexEscapeWarning.Character);
        Assert.Equal(string.Empty, source.GetOutput(AwkFileDescriptor.StdErr));
        async Task RunAsync()
        {
            var host = new MockFileSystem();
            var options = new AwkRuntimeOptions(AwkUnixTimestampSeconds.FromSystemClock, TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault(),
                utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
regexEngine: AwkUtf8RegexEngine.Instance);
            var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("1\n1\n1\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: /warning.awk:1:24: warning: regexp escape sequence `\\q' is not a known regexp operator\n" +
                "awk: warning: regexp escape sequence `\\d' is not a known regexp operator\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
        await Task.WhenAll(RunAsync(), RunAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RejectedOperatorWarningRemainsEligible(int mode)
    {
        var history = new AwkRegexEscapeWarnings();
        Assert.Throws<AwkRuntimeException>(() => AwkRegexEscapes.ReportWarnings(@"\q",
            (AwkCompatibilityMode)mode, history, _ => throw new AwkRuntimeException(
                Lokad.Awk.AwkMessages.ErrorMemoryLimit), null));
        List<AwkRegexEscapeWarning> warnings = [];
        AwkRegexEscapes.ReportWarnings(@"\q\q\8\8\9", (AwkCompatibilityMode)mode, history, warnings.Add, null);
        Assert.Equal(new[] { 'q', '8', '9' }, warnings.Select(w => (char)w.Character.Value));
        Assert.Equal(AwkRegexEscapeWarningKind.UnknownOperator, warnings[0].Kind);
        Assert.All(warnings.Skip(1), w => Assert.Equal(AwkRegexEscapeWarningKind.PlainDigit, w.Kind));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void LongOperatorWarningScanCancelsBeforePublishingTail(int mode)
    {
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var history = new AwkRegexEscapeWarnings();
        void Check()
        {
            if (++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => AwkRegexEscapes.ReportWarnings(
            new string('a', 8192) + @"\q", (AwkCompatibilityMode)mode, history, _ => Assert.Fail(), null, Check));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.False(history.HasReported(new AwkRegexEscapeWarning(AwkRegexEscapeWarningKind.UnknownOperator,
            new System.Text.Rune('q'))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Utf8WarningHistoryUsesFirstByteAndRendersWholeScalars(int mode)
    {
        var history = new AwkRegexEscapeWarnings();
        List<AwkRegexEscapeWarning> warnings = [];
        AwkRegexEscapes.ReportWarnings(@"\é\ê\β\γ\𐐀\😀", (AwkCompatibilityMode)mode, history, warnings.Add, null);
        Assert.Equal(new[] { new System.Text.Rune('é'), new System.Text.Rune('β'), new System.Text.Rune(0x10400) },
            warnings.Select(w => w.Character));
        Assert.All(warnings, w => Assert.NotEmpty(new System.Text.UTF8Encoding(false, true).GetBytes(w.Message)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void MixedNumericAndOperatorWarningsKeepScanOrder(int mode)
    {
        List<string> warnings = [];
        AwkRegexEscapes.ReportWarnings(@"\q\u\x\z\q\u", (AwkCompatibilityMode)mode,
            new AwkRegexEscapeWarnings(), w => warnings.Add(w.Message), warnings.Add);
        Assert.Equal(new[]
        {
            "awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n",
            "awk: warning: no hex digits in `\\u' escape sequence\n",
            "awk: warning: no hex digits in `\\x' escape sequence\n",
            "awk: warning: regexp escape sequence `\\z' is not a known regexp operator\n",
            "awk: warning: no hex digits in `\\u' escape sequence\n"
        }, warnings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AllowedEscapesDoNotConsumeHistory(int mode)
    {
        List<AwkRegexEscapeWarning> warnings = [];
        List<string> numeric = [];
        AwkRegexEscapes.ReportWarnings(@"\a\b\f\n\r\t\v\101\x41\u0041\{\}\(\)\|\*\+\?\.\^\$\\\[\]\/\-",
            (AwkCompatibilityMode)mode, new AwkRegexEscapeWarnings(), warnings.Add, numeric.Add);
        Assert.Empty(warnings);
        Assert.Empty(numeric);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("", true)]
    [InlineData("--posix", false)]
    [InlineData("--posix", true)]
    [InlineData("--traditional", false)]
    [InlineData("--traditional", true)]
    public async Task DistinctEscapesProduceDistinctWarnings(string profile, bool distinct)
    {
        var host = new MockFileSystem();
        var script = distinct ? @"BEGIN{if(0)print /\q\d/;print 1}" : @"BEGIN{if(0)print /\q\q/;print 1}";
        string[] args = [.. profile.Length == 0 ? Array.Empty<string>() : new[] { profile }, script];
        var options = new AwkRuntimeOptions(AwkUnixTimestampSeconds.FromSystemClock, TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
            AwkRuntimeLimits.CreateDefault(), AwkRegexEngineKind.DotNetCompatibility);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", args, [])), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        var warnings = "awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n";
        if (distinct) warnings += "awk: warning: regexp escape sequence `\\d' is not a known regexp operator\n";
        Assert.Equal(warnings,
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public void TypedDiagnosticFitsItsGovernedElementEstimate()
    {
        Assert.True(System.Runtime.CompilerServices.Unsafe.SizeOf<AwkDiagnostic>() <= 64);
    }
}
