namespace Lokad.Awk.Tests;

public sealed partial class AwkWarningTests
{
    private const string Warning = "awk: warning: escape sequence `\\q' treated as plain `q'\n";

    [Theory]
    [InlineData("reject")]
    [InlineData("pending")]
    [InlineData("cancel")]
    public async Task BoundRegionResumesAfterDiagnosticAppendBeforeLaterActions(string outcome)
    {
        var host = new WarningHost(outcome);
        host.Files.SetStandardInput("row\n");
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        var command = AwkTestHarness.BuildInvocation("awk",
            ["-v", "pattern=\\\\q", "{FS=pattern;effect=1}{print effect>\"/after\"}END{print \"end\"}"]);
        var execution = AwkTestHarness.ParseAwk(command).ExecuteAsync(host,
            AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(AwkFileDescriptor.StdErr, host.FirstDescriptor);
            Assert.Contains("escape sequence", AwkUtf8Codec.Decode(host.FirstBytes));
            Assert.False(host.Files.FileExists("/after"));
            if (outcome == "reject") Assert.Equal(2, (await execution).Value);
            else if (outcome == "cancel")
            {
                Assert.False(execution.IsCompleted);
                Assert.True(budget.Reserved > 0);
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            }
            else
            {
                Assert.False(execution.IsCompleted);
                Assert.True(budget.Reserved > 0);
                host.Release.TrySetResult(AwkAppendResult.Accepted());
                Assert.Equal(AwkExitCode.Success, await execution);
                Assert.Equal("1\n", host.Files.ReadFile("/after"));
                Assert.Equal("end\n", host.Files.GetOutput(AwkFileDescriptor.StdOut));
            }
            if (outcome != "pending") Assert.False(host.Files.FileExists("/after"));
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.Files.OpenFileCount);
        }
        finally
        {
            host.Release.TrySetResult(AwkAppendResult.Accepted());
            cancellation.Cancel();
            try { await execution; } catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData("reject")]
    [InlineData("pending")]
    [InlineData("cancel")]
    public async Task FunctionMathWarningCompletesBeforeLaterEffects(string outcome)
    {
        var host = new WarningHost(outcome);
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        var command = AwkTestHarness.BuildInvocation("awk",
            "function score(x){sqrt(x);return 7}function later(){print \"after\" > \"/after\";return 8}BEGIN{print score(-1),later()}");
        var execution = AwkTestHarness.ParseAwk(command).ExecuteAsync(host,
            AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(AwkFileDescriptor.StdErr, host.FirstDescriptor);
            Assert.Equal("awk: warning: sqrt: received negative argument -1\n", AwkUtf8Codec.Decode(host.FirstBytes));
            Assert.False(host.Files.FileExists("/after"));
            Assert.Equal("", host.Files.GetOutput(AwkFileDescriptor.StdOut));
            if (outcome == "reject") Assert.Equal(2, (await execution).Value);
            else
            {
                Assert.False(execution.IsCompleted);
                Assert.True(budget.Reserved > 0);
                if (outcome == "cancel")
                {
                    cancellation.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
                }
                else
                {
                    host.Release.TrySetResult(AwkAppendResult.Accepted());
                    Assert.Equal(AwkExitCode.Success, await execution);
                    Assert.Equal("after\n", host.Files.ReadFile("/after"));
                    Assert.Equal("7 8\n", host.Files.GetOutput(AwkFileDescriptor.StdOut));
                }
            }
            if (outcome != "pending") Assert.False(host.Files.FileExists("/after"));
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.Files.OpenFileCount);
            Assert.Equal("", host.Files.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally
        {
            host.Release.TrySetResult(AwkAppendResult.Accepted());
            cancellation.Cancel();
            try { await execution; } catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task ConcurrentInvocationsLoadTheirOwnSourceAndEmitWarningsOnce()
    {
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "-f", "/script.awk"));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var first = new MockFileSystem();
        var second = new MockFileSystem();
        async Task RunAsync(MockFileSystem host)
        {
            host.AddFile("/script.awk", "# source\nBEGIN{print \"\\q\"}\n");
            var exit = await awk.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), deadline.Token);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("q\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: /script.awk:2:13: warning: escape sequence `\\q' treated as plain `q'\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(1, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
        await Task.WhenAll(RunAsync(first), RunAsync(second));
    }

    [Fact]
    public async Task AdmissionFailurePreservesAlreadyEmittedWarnings()
    {
        var host = new MockFileSystem();
        host.AddFile("/data", "unread\n");
        var budget = new CooperativeTestBudget(8 << 20);
        host.AfterAppend = () =>
        {
            // Admission before the warning remains funded; refuse subsequent runtime growth.
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            host.AfterAppend = null;
        };
        var command = AwkTestHarness.BuildInvocation("awk", "BEGIN{unused=\"\\q\";getline v<\"/data\";print v}");
        var exit = await AwkTestHarness.ParseAwk(command).ExecuteAsync(host,
            AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(Warning + "awk: memory limit exceeded\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, "cancel")]
    [InlineData(true, "cancel")]
    [InlineData(false, "reject")]
    [InlineData(true, "reject")]
    [InlineData(false, "throw")]
    [InlineData(true, "throw")]
    public async Task WarningAppendUsesHostBoundaryBeforeRuntimeStarts(bool governed, string failure)
    {
        var host = new WarningHost(failure);
        var command = AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"\\q\"}");
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        using var cancellation = new CancellationTokenSource();
        var run = AwkTestHarness.ParseAwk(command).ExecuteAsync(host, policy, cancellation.Token);
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
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            host.Release.TrySetResult(AwkAppendResult.Accepted());
            cancellation.Cancel();
        }
    }

    [Fact]
    public async Task PosixByteOverrideWarningIsEmittedOncePerInvocation()
    {
        const string warning = "awk: warning: `--posix' overrides `--characters-as-bytes'\n";
        var awk = AwkTestHarness.ParseAwk(
            AwkTestHarness.BuildInvocation("awk", "-b", "--posix", "BEGIN{print \"ok\"}"));
        for (var run = 0; run < 2; run++)
        {
            var host = new MockFileSystem();
            var exit = await awk.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("ok\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(warning, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData("FS")]
    [InlineData("FPAT")]
    [InlineData("RS")]
    public async Task SpecialRegexWarningsAreFreshForEveryInvocation(string variable)
    {
        const string warning = "awk: warning: no hex digits in `\\u' escape sequence\n";
        var invocation = AwkTestHarness.BuildInvocation("awk", "BEGIN{" + variable + "=\"\\\\u\";print \"ok\";exit}");
        var awk = AwkTestHarness.ParseAwk(invocation);
        foreach (var host in new[] { new MockFileSystem(), new MockFileSystem() })
        {
            var exit = await awk.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("ok\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(warning + warning, host.GetOutput(AwkFileDescriptor.StdErr));
        }
    }

    private sealed class WarningHost(string failure) : IAwkHost
    {
        private bool _first = true;
        public MockFileSystem Files { get; } = new();
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<AwkAppendResult> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public AwkFileDescriptor FirstDescriptor { get; private set; }
        public byte[] FirstBytes { get; private set; } = [];

        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            if (!_first) return Files.AppendAsync(descriptor, bytes, token);
            _first = false;
            FirstDescriptor = descriptor;
            FirstBytes = bytes.ToArray();
            Entered.TrySetResult(true);
            return failure switch
            {
                "throw" => throw new IOException("Private host exception must not escape."),
                "reject" => Task.FromResult(AwkAppendResult.Failure(default)),
                _ => Release.Task.WaitAsync(token)
            };
        }
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command,
            CancellationToken token) => Files.RunCommandAsync(command, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode,
            CancellationToken token) => Files.OpenFileAsync(path, mode, token);
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count,
            CancellationToken token) => Files.ReadAsync(descriptor, count, token);
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor,
            CancellationToken token) => Files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Files.CreatePipeAsync(token);
    }
}
