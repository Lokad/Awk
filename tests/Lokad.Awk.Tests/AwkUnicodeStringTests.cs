namespace Lokad.Awk.Tests;

public sealed class AwkUnicodeStringTests
{
    [Theory]
    [InlineData(0, "\\u", "awk: warning: no hex digits in `\\u' escape sequence\n")]
    [InlineData(2, "\\u", "awk: warning: no hex digits in `\\u' escape sequence\n")]
    [InlineData(0, "\\ud800", "awk: warning: invalid `\\u' escape sequence\n")]
    [InlineData(2, "\\u110000", "awk: warning: invalid `\\u' escape sequence\n")]
    public void UnicodeWarningsRetainStringTokenSpans(int mode, string escape, string expected)
    {
        var literal = "\"" + escape + "\"";
        var source = "BEGIN{if(0)print " + literal + "}";
        var parse = AwkParser.Parse(source, AwkFrontendBudget.Unlimited(int.MaxValue),
            CancellationToken.None, (AwkCompatibilityMode)mode);
        Assert.True(parse.Success);
        var warning = Assert.Single(parse.Warnings);
        Assert.True(warning.IsWarning);
        Assert.Equal(AwkDiagnosticId.WarningStringEscape, warning.Id);
        Assert.Equal(expected, warning.Message);
        Assert.Equal(new AwkTextSpan(source.IndexOf('"'), literal.Length), warning.Span);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnicodeDiagnosticsRespectFrontendBudget(int mode)
    {
        const string source = "BEGIN{print \"\\u\\ud800\"}";
        var budget = new AwkFrontendBudget(int.MaxValue, int.MaxValue, 1, int.MaxValue,
            long.MaxValue, int.MaxValue);
        if ((AwkCompatibilityMode)mode == AwkCompatibilityMode.Posix)
        {
            Assert.Empty(AwkParser.Parse(source, budget, CancellationToken.None,
                AwkCompatibilityMode.Posix).Warnings);
            return;
        }
        var error = Assert.Throws<AwkRuntimeException>(() => AwkParser.Parse(source, budget,
            CancellationToken.None, (AwkCompatibilityMode)mode));
        Assert.Equal(AwkDiagnosticId.RuntimeResourceLimit, error.Id);
        Assert.Equal("awk: frontend diagnostic limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    public static IEnumerable<object[]> ValueByteLimits()
    {

        foreach (var utf8 in new[] { false, true })
            foreach (var counters in new[] { false, true })
                foreach (var limit in new[] { 3, 4 })
                    yield return [utf8, counters, limit];
    }

    [Theory]
    [MemberData(nameof(ValueByteLimits))]
    public async Task SupplementaryLiteralUsesFourUtf8Bytes(bool utf8, bool enabled, int limit)
    {
        const string source = "BEGIN{print 1;v=\"\\u1f600\";printf \"%s\",v;exit}";
        var counters = enabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(limit),
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
regexEngine: AwkUtf8RegexEngine.Instance);
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        Assert.Empty(compiled.Warnings);
        var host = new MockFileSystem();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exit = await compiled.ExecuteAsync(host, deadline.Token, options);
        Assert.Equal(limit == 4 ? AwkExitCode.Success : AwkExitCode.FatalError, exit);
        Assert.Equal(limit == 4 ? "1\n\U0001f600" : "1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(limit == 4 ? string.Empty : "awk: memory limit exceeded\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task CompiledUnicodeWarningRetainsMappedSourceLocation()
    {
        var sourceHost = new MockFileSystem();
        sourceHost.AddFile("/unicode.awk", "# source\nBEGIN{print \"\\ud800\"}\n");
        var compiled = await AwkExecutor.CompileAsync(sourceHost,
            AwkTestHarness.BuildInvocation("awk", "-f", "/unicode.awk"), CancellationToken.None);
        var warning = Assert.Single(compiled.Warnings);
        var location = Assert.IsType<AwkSourceLocation>(warning.SourceLocation);
        Assert.Equal("/unicode.awk", location.SourceName.Text);
        Assert.Equal(2, location.LineNumber.Value);
        Assert.Equal(13, location.ColumnNumber.Value);
        Assert.Equal(0, sourceHost.OpenFileCount);
        Assert.Equal(string.Empty, sourceHost.GetOutput(AwkFileDescriptor.StdErr));
        var host = new MockFileSystem();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("?\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: /unicode.awk:2:13: warning: invalid `\\u' escape sequence\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
    }
}
