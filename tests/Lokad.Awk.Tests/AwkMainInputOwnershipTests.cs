namespace Lokad.Awk.Tests;

public sealed class AwkMainInputOwnershipTests
{
    [Theory]
    [InlineData("{print FILENAME,$0}", "one row\ntwo row\nthree row\n")]
    [InlineData("BEGIN{while((getline)>0)print FILENAME,$0}", "one row\ntwo row\nthree row\n")]
    [InlineData("BEGINFILE{if(FILENAME==\"two\")nextfile} {print FILENAME,$0}", "one row\nthree row\n")]
    [InlineData("{print FILENAME,$0;exit}", "one row\n")]
    public async Task MainFilesHaveOneOwnerAndRetireBeforeTheNextOpen(string source, string expected)
    {
        var host = new MainHost();
        var budget = new CooperativeTestBudget(1 << 20);
        Assert.Equal(AwkExitCode.Success, await Tool(source).ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None));
        Assert.Equal(expected, host.Files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.Files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, host.MaximumActive);
        Assert.Empty(host.Active);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingMainCloseKeepsOwnershipThroughCancellation(bool fail)
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<AwkDescriptorCloseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MainHost { FirstClose = () => { entered.SetResult(); return completion.Task; } };
        var budget = new CooperativeTestBudget(1 << 20);
        var run = Tool("{print FILENAME,$0}").ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Single(host.Active);
            Assert.True(budget.Reserved > 0);
        }
        finally { completion.TrySetResult(fail ? AwkDescriptorCloseResult.Failure(default, descriptorRetired: false) : AwkDescriptorCloseResult.Closed()); }
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal("one row\n", host.Files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.Files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(fail ? 2 : 1, host.CloseAttempts);
        Assert.Empty(host.Active);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task RefusalAfterOpenCleansTheAcquiredMainDescriptor()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new MainHost { AfterOpen = () => budget.RefuseNextAdmissionMinimumGrowth = 1 };
        Assert.Equal(AwkExitCode.FatalError, await Tool("{print FILENAME,$0}").ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None));
        Assert.Equal("", host.Files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.Files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, host.CloseAttempts);
        Assert.Empty(host.Active);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
    }

    private static Awk Tool(string source) => AwkTestHarness.ParseAwk(
        AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source, "one", "two", "three"], []));

    private sealed class MainHost : IAwkHost
    {
        public MockFileSystem Files { get; } = new();
        public HashSet<AwkFileDescriptor> Active { get; } = [];
        public int MaximumActive { get; private set; }
        public int CloseAttempts { get; private set; }
        public Action? AfterOpen { get; init; }
        public Func<Task<AwkDescriptorCloseResult>>? FirstClose { get; init; }

        public MainHost()
        {
            foreach (var name in new[] { "one", "two", "three" }) Files.AddFile("/" + name, "row\n");
        }
        public async Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        {
            var result = await Files.OpenFileAsync(path, mode, token);
            if (result.Success)
            {
                Assert.True(Active.Add(result.FileDescriptor));
                MaximumActive = Math.Max(MaximumActive, Active.Count);
                AfterOpen?.Invoke();
            }
            return result;
        }
        public async Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            Assert.False(token.CanBeCanceled);
            Assert.Contains(descriptor, Active);
            if (++CloseAttempts == 1 && FirstClose is { } first)
            {
                var status = await first();
                if (!status.Success) return status;
            }
            var closed = await Files.CloseDescriptorAsync(descriptor, token);
            if (closed.DescriptorRetired) Assert.True(Active.Remove(descriptor));
            return closed;
        }
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Files.ReadLinesAsync(descriptor, count, token);
        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token) => Files.AppendAsync(descriptor, content, token);
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Files.RunCommandAsync(command, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Files.CreatePipeAsync(token);
    }
}
