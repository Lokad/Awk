namespace Lokad.Awk.Tests;

public sealed class AwkUnicodeRegexTests
{
    private const string Warning = "awk: warning: no hex digits in `\\u' escape sequence\n";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void LiteralWarningsKeepRegexTokenSpansAndBudget(int mode)
    {
        const string literal = "/\\u\\ud800/";
        const string source = "BEGIN{if(0)print " + literal + "}";
        var profile = (AwkCompatibilityMode)mode;
        var parse = AwkParser.Parse(source, AwkFrontendBudget.Unlimited(int.MaxValue),
            CancellationToken.None, profile);
        Assert.True(parse.Success);
        if (profile == AwkCompatibilityMode.Posix)
        {
            Assert.Empty(parse.Warnings);
            return;
        }
        Assert.Equal(2, parse.Warnings.Count);
        foreach (var warning in parse.Warnings)
        {
            Assert.Equal(AwkDiagnosticId.WarningRegexEscape, warning.Id);
            Assert.True(warning.IsWarning);
            Assert.Equal(new AwkTextSpan(source.IndexOf('/'), literal.Length), warning.Span);
        }
        Assert.Equal(Warning, parse.Warnings[0].Message);
        Assert.Equal("awk: warning: invalid `\\u' escape sequence\n", parse.Warnings[1].Message);
        var budget = new AwkFrontendBudget(int.MaxValue, int.MaxValue, 1, int.MaxValue,
            long.MaxValue, int.MaxValue, static () => { });
        var error = Assert.Throws<AwkRuntimeException>(() => AwkParser.Parse(source, budget,
            CancellationToken.None, profile));
        Assert.Equal(AwkDiagnosticId.RuntimeResourceLimit, error.Id);
        Assert.Equal("awk: frontend diagnostic limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void RuntimeWarningsPrecedeSharedCacheHits(int mode, bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var messages = new List<string>();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, counters,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(limits, counters), compatibilityMode: (AwkCompatibilityMode)mode,
            reportUnicodeWarning: messages.Add);
        var first = new AwkRegexPattern(@"\u", warningSite: new object());
        var second = new AwkRegexPattern(@"\u", warningSite: new object());
        var regex = runtime.Create(first);
        Assert.Same(regex, runtime.Create(first));
        Assert.Same(regex, runtime.Create(second));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(mode == 1 ? 0 : 2, messages.Count);
        Assert.All(messages, message => Assert.Equal(Warning, message));
        Assert.Equal(0, counters.StringDecodes);
        runtime.Create(new AwkRegexPattern(@"\u", AwkRegexPatternOrigin.SourceLiteral, new object()));
        Assert.Equal(mode == 1 ? 0 : 2, messages.Count);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CompiledRegexWarningsAreFreshForConcurrentHosts(bool executable, bool utf8)
    {
        var source = new MockFileSystem();
        source.AddFile("/regex.awk", "# source\nBEGIN{if(0)print /\\u/;p=\"\\\\u\";for(i=0;i<3;i++)print \"u\"~p;exit}");
        var compiled = await AwkExecutor.CompileAsync(source,
            AwkTestHarness.BuildInvocation("awk", "-f", "/regex.awk"), CancellationToken.None);
        var warning = Assert.Single(compiled.Warnings);
        var location = Assert.IsType<AwkSourceLocation>(warning.SourceLocation);
        Assert.Equal("/regex.awk", location.SourceName.Text);
        Assert.Equal(2, location.LineNumber.Value);
        Assert.Equal(18, location.ColumnNumber.Value);
        Assert.Equal(string.Empty, source.GetOutput(AwkFileDescriptor.StdErr));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        async Task RunAsync()
        {
            var host = new MockFileSystem();
            var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault(),
                utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
                executable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter);
            var exit = await compiled.ExecuteAsync(host, deadline.Token, options);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("1\n1\n1\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: /regex.awk:2:18: warning: no hex digits in `\\u' escape sequence\n" + Warning,
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, host.ReadOpenCount);
        }
        await Task.WhenAll(RunAsync(), RunAsync());
    }

    [Fact]
    public void WarningHistoryMemoryPreflightPreservesPreviousEntry()
    {
        var governor = AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4096));
        var messages = new List<string>();
        var history = new AwkRegexUnicodeWarnings(governor, AwkCompatibilityMode.GnuDefault,
            messages.Add, static () => { });
        var site = new object();
        var previous = new AwkRegexPattern(@"\u", warningSite: site);
        history.Report(previous, AwkRegexCaseSensitivity.CaseSensitive);
        var committed = governor.CurrentCommittedBytes;
        Assert.True(committed > 0);
        Assert.Throws<AwkRuntimeException>(() => history.Report(
            new AwkRegexPattern(@"\u" + new string('z', 5000), warningSite: site),
            AwkRegexCaseSensitivity.CaseSensitive));
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        history.Report(previous, AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Single(messages);
        history.Report(new AwkRegexPattern("A", warningSite: site), AwkRegexCaseSensitivity.CaseSensitive);
        Assert.True(governor.CurrentCommittedBytes < committed);
        history.Report(previous, AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(2, messages.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecodeBufferAdmissionFailsBeforeCompilationOrCacheRetention(bool utf8)
    {
        const string pattern = @"\u0041";
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(
            AwkRuntimeMemory.EstimateEscapeDecodingBytes(pattern.Length) - 1);
        var governor = AwkMemoryGovernor.Create(limits);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, static () => { }, counters,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(limits, counters), governor);
        for (var i = 0; i < 2; i++)
        {
            var error = Assert.Throws<AwkRuntimeException>(() => runtime.Create(new AwkRegexPattern(pattern)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(0, governor.CurrentCommittedBytes);
        }
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WarningCallbackFailureReleasesReservationAndAllowsRetry(bool grow)
    {
        var governor = AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(16384));
        var calls = 0;
        var fail = false;
        var history = new AwkRegexUnicodeWarnings(governor, AwkCompatibilityMode.GnuDefault,
            _ => { calls++; if (fail) throw new AwkRuntimeException(Lokad.Awk.AwkMessages.ErrorMemoryLimit); }, static () => { });
        var site = new object();
        for (var i = 0; i < (grow ? 4 : 1); i++)
            history.Report(new AwkRegexPattern(@"\u", warningSite: i == 0 ? site : new object()),
                AwkRegexCaseSensitivity.CaseSensitive);
        var beforeCalls = calls;
        var beforeBytes = governor.CurrentCommittedBytes;
        var pattern = new AwkRegexPattern(@"\ug", warningSite: grow ? new object() : site);
        fail = true;
        Assert.Throws<AwkRuntimeException>(() => history.Report(pattern, AwkRegexCaseSensitivity.CaseSensitive));
        Assert.Equal(beforeBytes, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        fail = false;
        history.Report(pattern, AwkRegexCaseSensitivity.CaseSensitive);
        history.Report(pattern, AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(beforeCalls + 2, calls);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongEscapeWorkObservesCancellation(bool diagnostics)
    {
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (++checks == 2)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var pattern = diagnostics ? new string('z', 4096) + @"\u" : @"\u0041" + new string('z', 4096);
        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            if (diagnostics)
                AwkRegexEscapes.ReportUnicodeWarnings(pattern, AwkCompatibilityMode.GnuDefault,
                    _ => Assert.Fail("Cancellation must precede the trailing warning."), Check);
            else
                AwkRegexEscapes.Decode(pattern, AwkCompatibilityMode.GnuDefault, Check);
        });
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(2, checks);
    }
}
