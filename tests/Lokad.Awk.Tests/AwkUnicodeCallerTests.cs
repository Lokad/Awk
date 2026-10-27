namespace Lokad.Awk.Tests;

public sealed class AwkUnicodeCallerTests
{
    private const string Warning = "awk: warning: no hex digits in `\\u' escape sequence\n";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task OptionWarningsRemainOrderedOnFreshConcurrentExecutions(int entry)
    {
        var source = new MockFileSystem();
        source.AddFile("/caller.awk", "# source\nBEGIN{print v;print \"\\u\";exit}\n");
        var command = AwkTestHarness.BuildInvocation("awk", "-v", @"v=\u", "-f", "/caller.awk");
        var compiled = await AwkExecutor.CompileAsync(source, command, CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        Assert.Equal(2, compiled.Warnings.Count);
        Assert.Null(compiled.Warnings[0].SourceLocation);
        var location = Assert.IsType<AwkSourceLocation>(compiled.Warnings[1].SourceLocation);
        Assert.Equal("/caller.awk", location.SourceName.Text);
        Assert.Equal(2, location.LineNumber.Value);
        Assert.Equal(string.Empty, source.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, source.OpenFileCount);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        async Task RunAsync()
        {
            var host = new MockFileSystem();
            host.AddFile("/caller.awk", "# source\nBEGIN{print v;print \"\\u\";exit}\n");
            var options = Options();
            var exit = entry == 0
                ? await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), deadline.Token, options)
                : await compiled.ExecuteAsync(host, deadline.Token, options);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("u\nu\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(Warning + "awk: /caller.awk:2:21: warning: no hex digits in `\\u' escape sequence\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(entry == 0 ? 1 : 0, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
        await Task.WhenAll(RunAsync(), RunAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdmissionFailurePreservesOptionWarningsWithoutInputIo(bool compiledEntry)
    {
        var source = new MockFileSystem();
        source.AddFile("/caller.awk", "{print}");
        var command = AwkTestHarness.BuildInvocation("awk", "-v", @"v=\u", "-f", "/caller.awk", "/data");
        var compiled = await AwkExecutor.CompileAsync(source, command, CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var host = new MockFileSystem();
        host.AddFile("/caller.awk", "{print}");
        host.AddFile("/data", "unread\n");
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = Options(limits) with { MemoryBudget = new CooperativeTestBudget(0) };
        var exit = compiledEntry ? await compiled.ExecuteAsync(host, CancellationToken.None, options)
            : await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: no hex digits in `\\u' escape sequence\nawk: memory limit exceeded\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, options.Counters.BytesRead);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OptionAndSourceWarningsRemainOrdered(bool sourceWarning)
    {
        var command = AwkTestHarness.BuildInvocation("awk", "-v", @"v=\u",
            sourceWarning ? "BEGIN{print \"\\u\";exit}" : "BEGIN{print v;exit}");
        var host = new MockFileSystem();
        var options = Options(AwkRuntimeLimits.CreateDefault());
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("u\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(sourceWarning ? Warning + Warning : Warning, host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Theory]
    [InlineData("--posix", "A.awk")]
    [InlineData("--traditional", "A.awk")]
    [InlineData("--posix", @"\u.awk")]
    [InlineData("--traditional", @"\u.awk")]
    public async Task UnsupportedIncludeProfilesFailBeforeOpeningTheName(string profile, string name)
    {
        var host = new MockFileSystem();
        host.AddFile("/A.awk", "BEGIN{print \"unread\"}");
        var command = AwkTestHarness.BuildInvocation("awk", profile, "@include \"" + name + "\"\nBEGIN{exit}");
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: @include is a GNU extension\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(@"\u0041.awk")]
    [InlineData(@"\ud800.awk")]
    public async Task MissingRawIncludeNameReturnsStructuredFailureWithoutOpeningDecodedNames(string name)
    {
        var host = new MockFileSystem();
        host.AddFile("/A.awk", "BEGIN{print \"unread\"}");
        var invocation = AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", "@include \"" + name + "\""));
        var resources = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled);
        var result = await AwkSourceLoader.LoadAsync(host, invocation, resources, CancellationToken.None);
        var failure = Assert.IsType<AwkSourceLoadResult.Failed>(result);
        Assert.Equal(AwkDiagnosticId.SourceLoadFailure, failure.Id);
        Assert.Equal($"awk: {name}: No such file or directory\n", AwkUtf8Codec.Decode(failure.ErrorMessage));
        Assert.Equal(1, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OperandDecodeBufferPreflightsBeforeReplacingTheVariable(bool counters)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            memory.Configure(Options(AwkRuntimeLimits.CreateDefault(),counters)))).State;
        var name = AwkIdentifier.FromTrustedName("v");
        state.AssignVariableFromExternalText(name, "keep");
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var assignment = AwkVariableAssignment.InputArgument(name, @"\u0041" + new string('a', 1 << 18));
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.AssignVariableFromExternalText(assignment));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("keep", state.FormatStringByteText(state.ResolveVariableValue(new AwkVariableName(name))).AsString(state.Counters));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongCallerEscapeWorkObservesCancellation(bool warnings)
    {
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (++checks == 2) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var raw = warnings ? new string('a', 4096) + @"\u" : @"\u0041" + new string('a', 4096);
        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            if (warnings)
                AwkEscapes.ReportUnicodeWarnings(raw, AwkCompatibilityMode.GnuDefault, _ => Assert.Fail(), Check);
            else
                AwkEscapes.DecodeSourceString(raw, AwkCompatibilityMode.GnuDefault, Check);
        });
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(2, checks);
    }

    private static AwkRuntimeOptions Options(AwkRuntimeLimits? limits = null,        bool counters = true) => new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
        new AwkRandomGenerator(), counters ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled,
        limits ?? AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
}
