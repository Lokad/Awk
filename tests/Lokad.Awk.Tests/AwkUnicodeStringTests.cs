namespace Lokad.Awk.Tests;

public sealed class AwkUnicodeStringTests
{
    [Theory]
    [InlineData(0, "\\u", "no hex digits in `\\u' escape sequence")]
    [InlineData(2, "\\u", "no hex digits in `\\u' escape sequence")]
    [InlineData(0, "\\ud800", "invalid `\\u' escape sequence")]
    [InlineData(2, "\\u110000", "invalid `\\u' escape sequence")]
    public void UnicodeWarningsRetainStringTokenSpans(int mode, string escape, string expected)
    {
        var literal = "\"" + escape + "\"";
        var source = "BEGIN{if(0)print " + literal + "}";
        var parse = AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance },
            CancellationToken.None, (AwkCompatibilityMode)mode);
        Assert.True(parse.Success);
        var warning = Assert.Single(parse.Warnings);
        Assert.True(warning.IsWarning);
        Assert.Equal(AwkDiagnosticId.WarningStringEscape, warning.Id);
        Assert.Equal(expected, warning.Message);
        Assert.Equal(new AwkTextSpan(source.IndexOf('"'), literal.Length), warning.Span);
    }

    public static IEnumerable<object[]> UnicodeModes()
    {

        foreach (var counters in new[] { false, true })
            yield return [counters];
    }

    [Theory]
    [MemberData(nameof(UnicodeModes))]
    public async Task SupplementaryLiteralUsesFourUtf8Bytes(bool enabled)
    {
        const string source = "BEGIN{print 1;v=\"\\u1f600\";printf \"%s\",v;exit}";
        var counters = enabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var host = new MockFileSystem();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exit = await AwkExecutor.ExecuteAsync(host, command, deadline.Token, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1\n\U0001f600", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty,
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task UnicodeWarningRetainsMappedSourceLocation()
    {
        var host = new MockFileSystem();
        host.AddFile("/unicode.awk", "# source\nBEGIN{print \"\\ud800\"}\n");
        var command = AwkTestHarness.BuildInvocation("awk", "-f", "/unicode.awk");
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("?\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: /unicode.awk:2:13: warning: invalid `\\u' escape sequence\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, host.ReadOpenCount);
    }
}
