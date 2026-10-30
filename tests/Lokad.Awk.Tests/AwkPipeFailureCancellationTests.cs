namespace Lokad.Awk.Tests;

public sealed class AwkPipeFailureCancellationTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, true, false)]
    [InlineData(true, false, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(false, true, true)]
    [InlineData(true, false, true)] [InlineData(true, true, true)]
    public async Task ParentFailureCancelsPipeWorkAndAwaitsItsActualCompletion(bool input, bool governed, bool mainFile)
    {
        using var cancellation = new CancellationTokenSource();
        var host = new PendingCommandHost();
        var budget = new CooperativeTestBudget(1 << 20);
        if (governed) host.OnPipeActivity = () => budget.RefuseNextAdmissionMinimumGrowth = 1;
        var body = input ? "\"worker a:b\"|getline x; a[1]=1;print a"
            : "print \"pipe\"|\"worker a:b\";a[1]=1;print a";
        var source = (mainFile ? "{" : "BEGIN{") + body + "}";
        var tool = AwkTestHarness.ParseAwk(mainFile ? AwkTestHarness.BuildInvocation("awk", source, "/data")
            : AwkTestHarness.BuildInvocation("awk", source));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var execution = tool.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await host.Closing.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(cancellation.IsCancellationRequested);
            Assert.True(host.CommandToken.IsCancellationRequested);
            Assert.False(host.ObservedUncanceledCommandDuringCleanup);
            Assert.False(execution.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            Assert.Equal(0, budget.ScopesDisposed);
            host.Release.TrySetResult();
            Assert.Equal(AwkExitCode.FatalError, await execution.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(governed ? "awk: memory limit exceeded\n" : "awk: attempt to use array `a' in a scalar context\n",
                host.Files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Empty(host.Files.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Contains(new AwkFileDescriptor(input ? 80 : 81), host.Closed);
            if (input) Assert.Contains(new AwkFileDescriptor(81), host.Closed);
            if (mainFile) Assert.Contains(new AwkFileDescriptor(100), host.Closed);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
        }
        finally
        {
            cancellation.Cancel();
            host.Release.TrySetResult();
            try { await execution; } catch (OperationCanceledException) { }
        }
    }

    private sealed class PendingCommandHost : IAwkHost
    {
        public MockFileSystem Files { get; } = new();
        public TaskCompletionSource Closing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<AwkFileDescriptor> Closed { get; } = [];
        public Action? OnPipeActivity { get; set; }
        public CancellationToken CommandToken { get; private set; }
        public bool ObservedUncanceledCommandDuringCleanup { get; private set; }

        public async Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token)
        {
            CommandToken = token;
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { await Release.Task; }
            return AwkExitCode.Success;
        }

        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) =>
            Task.FromResult(AwkPipeCreationResult.Created(new(new(80), new(81))));

        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            Assert.False(token.CanBeCanceled);
            ObservedUncanceledCommandDuringCleanup |= !CommandToken.IsCancellationRequested;
            Closed.Add(descriptor);
            Closing.TrySetResult();
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        }

        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            if (descriptor.Id == 80) OnPipeActivity?.Invoke();
            return ValueTask.FromResult(AwkReadBuffer.FromContent("x\n"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { }));
        }

        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            if (descriptor.Id != 81) return Files.AppendAsync(descriptor, bytes, token);
            OnPipeActivity?.Invoke();
            return Task.FromResult(AwkAppendResult.Accepted());
        }

        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token) =>
            Task.FromResult(AwkOpenedFile.Opened(new(100), AwkRawPath.Parse(path.Path).ToAbsolute(path.CurrentDirectory)));
    }
}
