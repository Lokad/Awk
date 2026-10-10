namespace Lokad.Awk.Tests;

public sealed class AwkCompiledWarningTests
{
    private const string Warning = "awk: warning: escape sequence `\\q' treated as plain `q'\n";

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
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
            var options = Options(entry >= 2, utf8, AwkRuntimeCounters.CreateEnabled());
            var exit = entry switch
            {
                0 => await compiled.ExecuteAsync(host, deadline.Token),
                3 => await compiled.ExecuteAsync(host, deadline.Token, options, compiled.ExecutableProgram),
                _ => await compiled.ExecuteAsync(host, deadline.Token, options)
            };
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("q\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: /script.awk:2:13: warning: escape sequence `\\q' treated as plain `q'\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
            if (entry >= 2)
                Assert.True(options.Counters.VmBasicBlockEntries > 0 || options.Counters.VmPatternFallbacks > 0);
        }
        await Task.WhenAll(RunAsync(first), RunAsync(second));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task WarningsShareOutputAndHostAdmissionWithExecution(bool executable, bool countersEnabled, bool hostQuota)
    {
        var host = new MockFileSystem();
        host.AddFile("/data", "unread\n");
        var counters = countersEnabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var limits = hostQuota
            ? AwkRuntimeLimits.CreateDefault().WithMaxHostCalls(1)
            : AwkRuntimeLimits.CreateDefault().WithMaxOutputBytes(AwkUtf8Codec.Encode(Warning).Length - 1);
        var options = Options(executable, false, counters, limits);
        var compiled = await AwkExecutor.CompileAsync(host,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{unused=\"\\q\";getline v<\"/data\";print v}"),
            CancellationToken.None);

        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(hostQuota ? Warning + "awk: host call limit exceeded\n" : "awk: memory limit exceeded\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, counters.BytesRead);
        if (!hostQuota)
        {
            Assert.Equal(0, counters.VmBasicBlockEntries);
            Assert.Equal(0, counters.VmPatternFallbacks);
        }
    }

    [Theory]
    [InlineData(0, "cancel")]
    [InlineData(1, "cancel")]
    [InlineData(2, "cancel")]
    [InlineData(3, "cancel")]
    [InlineData(0, "timeout")]
    [InlineData(1, "timeout")]
    [InlineData(2, "timeout")]
    [InlineData(3, "timeout")]
    [InlineData(0, "reject")]
    [InlineData(1, "reject")]
    [InlineData(2, "reject")]
    [InlineData(3, "reject")]
    [InlineData(0, "throw")]
    [InlineData(1, "throw")]
    [InlineData(2, "throw")]
    [InlineData(3, "throw")]
    public async Task WarningAppendUsesHostBoundaryBeforeRuntimeStarts(int entry, string failure)
    {
        var host = new WarningHost(failure);
        var command = AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"\\q\"}");
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), command, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = Options(entry >= 2, false, counters,
            hostTimeout: failure == "timeout" ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource();
        var run = entry switch
        {
            0 => AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), cancellation.Token, options),
            3 => compiled.ExecuteAsync(host, cancellation.Token, options, compiled.ExecutableProgram),
            _ => compiled.ExecuteAsync(host, cancellation.Token, options)
        };
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (failure == "cancel")
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
            }
            else
            {
                Assert.Equal(AwkExitCode.FatalError, await run.WaitAsync(TimeSpan.FromSeconds(5)));
            }
            Assert.Equal(string.Empty, host.Files.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(failure switch
            {
                "timeout" => "awk: host operation timed out\n",
                "throw" => "awk: execution failed without exposing host exception details\n",
                _ => string.Empty
            }, host.Files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters.VmBasicBlockEntries);
            Assert.Equal(0, counters.VmPatternFallbacks);
            Assert.Equal(0, host.Files.ReadOpenCount);
            Assert.Equal(0, host.Files.OpenFileCount);
        }
        finally
        {
            host.Release.TrySetResult(AwkExitCode.Success);
            cancellation.Cancel();
        }
    }

    private static AwkRuntimeOptions Options(bool executable, bool utf8, AwkRuntimeCounters counters,
        AwkRuntimeLimits? limits = null, TimeSpan? hostTimeout = null) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits ?? AwkRuntimeLimits.CreateDefault(),
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            executable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter,
            hostOperationTimeout: hostTimeout);

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
