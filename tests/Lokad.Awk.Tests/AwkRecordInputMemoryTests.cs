using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordInputMemoryTests
{
    [Theory]
    [InlineData("default", false)] [InlineData("default", true)]
    [InlineData("unsupported", false)] [InlineData("unsupported", true)]
    [InlineData("semantic", false)] [InlineData("semantic", true)]
    public async Task DecodedBatchAdmissionFailsBeforeRecordEffects(string engine, bool injectedRefusal)
    {
        var budget = new CooperativeTestBudget((injectedRefusal ? 16 : 2) << 20);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000))
        {
            OnRead = () => { if (injectedRefusal) budget.RefuseNextAdmissionMinimumGrowth = 1_500_000; }
        };
        var pattern = engine == "semantic" ? "x!|y" : "x.";
        var script = "BEGIN{RS=\"" + pattern + "\";print \"base\"}{print \"after\";exit}END{print \"end\"}";
        var exit = await Execute(script, host, budget, engine);
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
    [InlineData("default", false)] [InlineData("default", true)]
    [InlineData("unsupported", false)] [InlineData("unsupported", true)]
    [InlineData("semantic", false)] [InlineData("semantic", true)]
    public async Task DecodedBatchRemainsFundedThroughPendingOutput(string engine, bool cancel)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000))
        {
            PendingOutput = () => { entered.SetResult(); return release.Task; }
        };
        var pattern = engine == "semantic" ? "x!|y" : "x.";
        var execution = Execute("BEGIN{RS=\"" + pattern + "\"}{print \"held\";exit}", host, budget, engine, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.InRange(budget.Reserved, 3_000_000, budget.Limit);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.InRange(budget.Reserved, 3_000_000, budget.Limit);
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
        var exit = await Execute("BEGIN{RS=\"x.\"}{print $0;exit}", host, budget, "utf8");
        Assert.Equal(0, exit.Value);
        Assert.Equal("a\n", host.Output);
        Assert.Equal("", host.Error);
        // Producer scratch has its own admission; it is refunded before output.
        Assert.InRange(Assert.Single(host.OutputAdmission), 1_000_000, 1_500_000);
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("unsupported")]
    public async Task CompactionReleasesTheOldDecodeBeforeReadingAnotherBatch(string engine)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000), "x!b");
        var exit = await Execute("BEGIN{RS=\"x.\"}{if(NR==1)print \"first\";else print length($0)}END{print \"done\"}",
            host, budget, engine);
        Assert.Equal(0, exit.Value);
        Assert.Equal("first\n1000000\n1\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(2, host.Reads);
        // The raw buffer is still owned; its previous decoded view has been dropped.
        Assert.InRange(host.ReadAdmission[1], 1_000_000, 1_500_000);
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("unsupported")]
    public async Task SeparatorChangesKeepTheCachedDecodeFundedWithoutRepeatedAdmission(string engine)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!b:" + new string('q', 1_000_000));
        var script = "BEGIN{RS=\"x.\"}{if(NR==1){RS=\":\";print \"held\"}" +
            "else{RS=\"x.\";print \"retained\";exit}}END{print \"done\"}";
        Assert.Equal(0, (await Execute(script, host, budget, engine)).Value);
        Assert.Equal("held\nretained\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.All(host.OutputAdmission.Take(2), bytes => Assert.InRange(bytes, 3_000_000, 3_200_000));
        Assert.InRange(host.OutputAdmission[2], 1, 100_000);
        AssertRefunded(budget);
    }

    private static Task<AwkExitCode> Execute(string script, InputHost host, CooperativeTestBudget budget,
        string engine, CancellationToken token = default)
    {
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);
        if (engine == "unsupported") policy = policy.WithRegexEngine(new UnsupportedEngine());
        if (engine is "utf8" or "semantic") policy = policy.WithRegexEngine(Utf8Regex.AwkUtf8RegexEngine.Instance);
        return AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, policy, token);
    }

    private static void AssertRefunded(CooperativeTestBudget budget)
    {
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    private sealed class UnsupportedEngine : IAwkRegexEngine
    {
        public bool TryCompile(string pattern, bool ignoreCase, IMemoryBudget? memoryBudget,
            CancellationToken token, [NotNullWhen(true)] out IAwkCompiledRegex? compiledPattern)
        {
            token.ThrowIfCancellationRequested();
            compiledPattern = null;
            return false;
        }
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
        public async Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += Encoding.UTF8.GetString(content.Span);
                OutputAdmission.Add(budget.Reserved);
                if (PendingOutput is { } pending) await pending().ConfigureAwait(false);
            }
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            return AwkExitCode.Success;
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
