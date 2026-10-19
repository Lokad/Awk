using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkStringEscapeWarningTests
{
    private const string Warning = "awk: warning: escape sequence `\\q' treated as plain `q'\n";

    public static IEnumerable<object[]> CompiledEntries()
    {
        foreach (var profile in new[] { "", "--posix", "--traditional" })
            foreach (var optionFirst in new[] { false, true })

                foreach (var utf8 in new[] { false, true })
                    yield return [profile, optionFirst, utf8];
    }

    [Theory]
    [MemberData(nameof(CompiledEntries))]
    public async Task CompiledHistoryRetainsFirstOriginAndIsFreshForConcurrentRuns(
        string profile, bool optionFirst, bool utf8)
    {
        var source = new MockFileSystem();
        source.AddFile("/warning.awk", "# source\nBEGIN{print \"\\q\\q\"}END{print a,b}\n");
        string[] args = [.. profile.Length == 0 ? Array.Empty<string>() : new[] { profile },
            .. optionFirst ? new[] { "-v", @"v=\q" } : Array.Empty<string>(),
            "-f", "/warning.awk", @"a=\q", @"b=\q"];
        var compiled = await AwkExecutor.CompileAsync(source,
            AwkTestHarness.BuildInvocation("awk", args, []), CancellationToken.None);
        var warning = Assert.Single(compiled.Warnings);
        Assert.Equal(AwkStringEscapeWarningKind.Unknown, warning.StringEscapeWarning.Kind);
        Assert.Equal(new Rune('q'), warning.StringEscapeWarning.Character);
        if (optionFirst)
            Assert.Null(warning.SourceLocation);
        else
        {
            var location = Assert.IsType<AwkSourceLocation>(warning.SourceLocation);
            Assert.Equal("/warning.awk", location.SourceName.Text);
            Assert.Equal(2, location.LineNumber.Value);
            Assert.Equal(13, location.ColumnNumber.Value);
        }
        Assert.Equal(string.Empty, source.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, source.OpenFileCount);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        async Task RunAsync()
        {
            var host = new MockFileSystem();
            var options = Options(utf8);
            var exit = await compiled.ExecuteAsync(host, deadline.Token, options);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("qq\nq q\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(optionFirst ? Warning
                : "awk: /warning.awk:2:13: warning: escape sequence `\\q' treated as plain `q'\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
        await Task.WhenAll(RunAsync(), RunAsync());
    }

    public static IEnumerable<object[]> FrontendBudgets()
    {
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        foreach (var escaped in new[] { 'q', 'j', 'x', 'u' })
            yield return [profile, escaped];
    }

    [Theory]
    [MemberData(nameof(FrontendBudgets))]
    public async Task DeduplicationPrecedesFrontendAdmissionButNumericWarningsRemainPerOccurrence(
        string profile, char escaped)
    {
        var option = "v=" + '\\' + (escaped == 'j' ? 'q' : escaped);
        var script = "BEGIN{print \"" + '\\' + escaped + '\\' + escaped + "\";exit}";
        string[] args = [.. profile.Length == 0 ? Array.Empty<string>() : new[] { profile }, "-v", option, script];
        var host = new MockFileSystem();
        var options = Options(false, AwkRuntimeLimits.CreateDefault().WithMaxFrontendDiagnostics(1));
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", args, [])), CancellationToken.None, options);
        var perOccurrence = escaped is 'x' or 'u';
        var posixNumeric = profile == "--posix" && perOccurrence;
        var exceeded = escaped == 'j' || perOccurrence && !posixNumeric;
        var firstWarning = posixNumeric ? string.Empty : perOccurrence
            ? "awk: warning: no hex digits in `\\" + escaped + "' escape sequence\n" : Warning;
        Assert.Equal(exceeded ? AwkExitCode.FatalError : AwkExitCode.Success, exit);
        Assert.Equal(exceeded ? string.Empty : new string(escaped, 2) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(firstWarning + (exceeded ? "awk: frontend diagnostic limit exceeded\n" : string.Empty),
            host.GetOutput(AwkFileDescriptor.StdErr));
        AwkTestHarness.AssertNoClrExceptionLeak(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RejectedWarningDoesNotPoisonHistory(int mode)
    {
        var history = new AwkStringEscapeWarnings();
        Assert.Throws<AwkRuntimeException>(() => AwkEscapes.ReportStringWarnings(@"\q", (AwkCompatibilityMode)mode,
            history, _ => throw new AwkRuntimeException(Lokad.Awk.AwkMessages.ErrorFrontendDiagnosticLimit,
                AwkDiagnosticId.RuntimeResourceLimit)));
        List<AwkStringEscapeWarning> emitted = [];
        AwkEscapes.ReportStringWarnings(@"\q\q", (AwkCompatibilityMode)mode, history, emitted.Add);
        Assert.Single(emitted);
        Assert.Equal(Warning, emitted[0].Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void LongWarningScanObservesCancellationBeforeTheTail(int mode)
    {
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var raw = new string('a', 4096) + @"\q";
        var error = Assert.Throws<OperationCanceledException>(() => AwkEscapes.ReportStringWarnings(raw,
            (AwkCompatibilityMode)mode, new AwkStringEscapeWarnings(), _ => Assert.Fail(), Check));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(3, checks);
    }

    private static AwkRuntimeOptions Options(bool utf8, AwkRuntimeLimits? limits = null) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits ?? AwkRuntimeLimits.CreateDefault(),
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
regexEngine: AwkUtf8RegexEngine.Instance);
}
