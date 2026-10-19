namespace Lokad.Awk.Tests;

public sealed class AwkCompiledWarningTests
{
    private const string Warning = "awk: warning: escape sequence `\\q' treated as plain `q'\n";

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    public async Task EveryCompiledEntryEmitsWarningsOnceOnFreshConcurrentHosts(int entry, bool utf8)
    {
        var source = new MockFileSystem();
        source.AddFile("/script.awk", "# source\nBEGIN{print \"\\q\"}\n");
        var compiled = await AwkExecutor.CompileAsync(source,
            AwkTestHarness.BuildInvocation("awk", "-f", "/script.awk"), CancellationToken.None);
        var diagnostic = Assert.Single(compiled.Warnings);
        var location = Assert.IsType<AwkSourceLocation>(diagnostic.SourceLocation);
        Assert.Equal("/script.awk", location.SourceName.Text);
        Assert.Equal(2, location.LineNumber.Value);
        Assert.Equal(13, location.ColumnNumber.Value);
        Assert.Equal(string.Empty, source.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, source.OpenFileCount);

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var first = new MockFileSystem();
        var second = new MockFileSystem();
        async Task RunAsync(MockFileSystem host)
        {
            var options = Options(utf8, AwkRuntimeCounters.CreateEnabled());
            var exit = entry switch
            {
                0 => await compiled.ExecuteAsync(host, deadline.Token),
                _ => await compiled.ExecuteAsync(host, deadline.Token, options)
            };
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("q\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: /script.awk:2:13: warning: escape sequence `\\q' treated as plain `q'\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
        await Task.WhenAll(RunAsync(first), RunAsync(second));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdmissionFailurePreservesAlreadyEmittedWarnings(bool countersEnabled)
    {
        var host = new MockFileSystem();
        host.AddFile("/data", "unread\n");
        var counters = countersEnabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = Options(false, counters, limits) with { MemoryBudget = new CooperativeTestBudget(0) };
        var compiled = await AwkExecutor.CompileAsync(host,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{unused=\"\\q\";getline v<\"/data\";print v}"),
            CancellationToken.None);

        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(Warning + "awk: memory limit exceeded\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, counters.BytesRead);
    }

    [Theory]
    [InlineData(0, "cancel")]
    [InlineData(1, "cancel")]
    [InlineData(0, "reject")]
    [InlineData(1, "reject")]
    [InlineData(0, "throw")]
    [InlineData(1, "throw")]
    public async Task WarningAppendUsesHostBoundaryBeforeRuntimeStarts(int entry, string failure)
    {
        var host = new WarningHost(failure);
        var command = AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"\\q\"}");
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), command, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = Options(false, counters);
        using var cancellation = new CancellationTokenSource();
        var run = entry switch
        {
            0 => AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), cancellation.Token, options),
            _ => compiled.ExecuteAsync(host, cancellation.Token, options)
        };
        var observationTimeout = TimeSpan.FromSeconds(30);
        try
        {
            await host.Entered.Task.WaitAsync(observationTimeout);
            if (failure == "cancel")
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(observationTimeout));
            }
            else
            {
                Assert.Equal(AwkExitCode.FatalError, await run.WaitAsync(observationTimeout));
            }
            Assert.Equal(string.Empty, host.Files.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(failure switch
            {
                "throw" => "awk: execution failed without exposing host exception details\n",
                _ => string.Empty
            }, host.Files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.Files.ReadOpenCount);
            Assert.Equal(0, host.Files.OpenFileCount);
        }
        finally
        {
            host.Release.TrySetResult(AwkExitCode.Success);
            cancellation.Cancel();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PosixByteOverrideWarningIsRetainedOncePerCompiledExecution(bool utf8)
    {
        const string warning = "awk: warning: `--posix' overrides `--characters-as-bytes'\n";
        var source = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(source,
            AwkTestHarness.BuildInvocation("awk", "-b", "--posix", "BEGIN{print \"ok\"}"),
            CancellationToken.None);
        Assert.Equal(warning, Assert.Single(compiled.Warnings).Message);
        Assert.Equal(string.Empty, source.GetOutput(AwkFileDescriptor.StdErr));
        for (var run = 0; run < 2; run++)
        {
            var host = new MockFileSystem();
            var counters = AwkRuntimeCounters.CreateEnabled();
            var options = Options(utf8, counters);
            var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("ok\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(warning, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData("FS", false)]
    [InlineData("FS", true)]
    [InlineData("FPAT", false)]
    [InlineData("FPAT", true)]
    [InlineData("RS", false)]
    [InlineData("RS", true)]
    public async Task SpecialRegexWarningsAreFreshForEveryCompiledExecution(string variable, bool utf8)
    {
        const string warning = "awk: warning: no hex digits in `\\u' escape sequence\n";
        var source = new MockFileSystem();
        var invocation = AwkTestHarness.BuildInvocation("awk", "BEGIN{" + variable + "=\"\\\\u\";print \"ok\";exit}");
        var compiled = await AwkExecutor.CompileAsync(source, invocation, CancellationToken.None);
        Assert.Empty(compiled.Warnings);
        foreach (var host in new[] { new MockFileSystem(), new MockFileSystem() })
        {
            var options = Options(utf8, AwkRuntimeCounters.CreateEnabled());
            var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("ok\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(warning + warning, host.GetOutput(AwkFileDescriptor.StdErr));
        }
    }

    private static AwkRuntimeOptions Options(bool utf8, AwkRuntimeCounters counters,
        AwkRuntimeLimits? limits = null) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits ?? AwkRuntimeLimits.CreateDefault(),
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            regexEngine: AwkUtf8RegexEngine.Instance);

    private sealed class WarningHost(string failure) : IAwkHost
    {
        private bool _first = true;
        public MockFileSystem Files { get; } = new();
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<AwkExitCode> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            if (!_first) return Files.AppendAsync(descriptor, bytes, token);
            _first = false;
            Entered.TrySetResult(true);
            return failure switch
            {
                "throw" => throw new IOException("Private host exception must not escape."),
                "reject" => Task.FromResult(AwkExitCode.FatalError),
                _ => Release.Task.WaitAsync(token)
            };
        }
        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands,
            CancellationToken token) => Files.RunCommandsAsync(commands, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode,
            CancellationToken token) => Files.OpenFileAsync(path, mode, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count,
            CancellationToken token) => Files.ReadLinesAsync(descriptor, count, token);
        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor,
            CancellationToken token) => Files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode,
            CancellationToken token) => Files.CreatePipeAsync(mode, token);
    }
}
