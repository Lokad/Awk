using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordInputMemoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderMatchAdmissionFailsBeforeRecordEffects(bool injectedRefusal)
    {
        var budget = new CooperativeTestBudget((injectedRefusal ? 16 : 2) << 20);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000))
        {
            OnRead = () => { if (injectedRefusal) budget.RefuseNextAdmissionMinimumGrowth = 1_500_000; }
        };
        var exit = await Execute("BEGIN{RS=\"x.\";print \"base\"}{print \"after\";exit}END{print \"end\"}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("base\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, host.Reads);
        if (injectedRefusal)
        {
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        }
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BorrowedInputRemainsFundedThroughPendingOutput(bool cancel)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000))
        {
            PendingOutput = () => { entered.SetResult(); return release.Task; }
        };
        var execution = Execute("BEGIN{RS=\"x.\"}{print \"held\";exit}", host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.InRange(budget.Reserved, 1_000_000, 1_500_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.InRange(budget.Reserved, 1_000_000, 1_500_000);
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal("held\n", host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task Utf8SearchDoesNotAdmitAnUnusedDecodedBatch()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000));
        var exit = await Execute("BEGIN{RS=\"x.\"}{print $0;exit}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("a\n", host.Output);
        Assert.Equal("", host.Error);
        // Provider scratch is refunded before output; only the raw input remains funded.
        Assert.InRange(Assert.Single(host.OutputAdmission), 1_000_000, 1_500_000);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task CompactionReleasesTheOldViewBeforeReadingAnotherBatch()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000), "x!b");
        var exit = await Execute("BEGIN{RS=\"x.\"}{if(NR==1)print \"first\";else print length($0)}END{print \"done\"}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("first\n1000000\n1\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(2, host.Reads);
        Assert.InRange(host.ReadAdmission[1], 1_000_000, 1_500_000);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task SeparatorChangesKeepTheBorrowedInputFunded()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!b:" + new string('q', 1_000_000));
        var script = "BEGIN{RS=\"x.\"}{if(NR==1){RS=\":\";print \"held\"}" +
            "else{RS=\"x.\";print \"retained\";exit}}END{print \"done\"}";
        Assert.Equal(0, (await Execute(script, host, budget)).Value);
        Assert.Equal("held\nretained\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.All(host.OutputAdmission.Take(2), bytes => Assert.InRange(bytes, 1_000_000, 1_500_000));
        Assert.InRange(host.OutputAdmission[2], 1, 100_000);
        AssertRefunded(budget);
    }

    private static Task<AwkExitCode> Execute(string script, InputHost host, CooperativeTestBudget budget,
        CancellationToken token = default) =>
        AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), token);

    private static void AssertRefunded(CooperativeTestBudget budget)
    {
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    private sealed class InputHost(CooperativeTestBudget budget, params string[] batches) : IAwkHost
    {
        private readonly byte[][] _batches = batches.Select(Encoding.UTF8.GetBytes).ToArray();
        public int Reads { get; private set; }
        public List<long> ReadAdmission { get; } = [];
        public List<long> OutputAdmission { get; } = [];
        public Func<Task>? PendingOutput { get; init; }
        public Action? OnRead { get; init; }
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public async Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += Encoding.UTF8.GetString(content.Span);
                OutputAdmission.Add(budget.Reserved);
                if (PendingOutput is { } pending) await pending().ConfigureAwait(false);
            }
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            return AwkAppendResult.Accepted();
        }
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            ReadAdmission.Add(budget.Reserved);
            OnRead?.Invoke();
            if (Reads == _batches.Length) return Task.FromResult(AwkReadResult.EndOfFile);
            var batch = _batches[Reads++];
            return Task.FromResult(AwkReadResult.FromContent(batch, [],
                Reads == _batches.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData));
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => Task.FromResult(AwkOpenedFile.MissingFile());
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkDescriptorCloseResult.Closed());
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Task.FromResult(AwkExitCode.Success);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Task.FromResult(AwkPipeCreationResult.NotSupported(default));
    }
}
