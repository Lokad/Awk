using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkStdinBorrowingTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task ExplicitMainFdAliasPreservesMixedReusableBuffers(bool buffered, bool moreData, bool governed)
    {
        var script = (buffered ? "BEGIN{RS=\"[\\n]\"}" : "") +
            "{print;if(NR==1){s=getline x<\"/dev/stdin\";print \"alias\",s,\"[\"x\"]\"}}";
        var expected = "a\nalias " + (moreData ? "1 [d]" : "0 []") + "\nb\nc\n";
        foreach (var reuse in new[] { false, true })
        {
            var budget = new CooperativeTestBudget(16 << 20);
            var host = new AliasHost(reuse, moreData);
            var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script, "/dev/fd/0"], []));
            var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
            if (governed) policy = policy.WithMemoryBudget(budget);
            Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, CancellationToken.None));
            Assert.Equal(expected, host.Output);
            Assert.Equal("", host.Error);
            Assert.Equal(0, budget.Reserved);
        }
    }

    public static IEnumerable<object[]> Configurations()
    {
        foreach (var alias in new[] { "/dev/stdin", "/dev/fd/0" })
        foreach (var reverse in new[] { false, true })
        foreach (var buffered in new[] { false, true })
        foreach (var moreData in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [alias, reverse, buffered, moreData, governed];
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public async Task IndependentStdinStreamsPreserveUnreadBytes(string alias, bool reverse, bool buffered, bool moreData, bool governed)
    {
        var readAlias = "s=getline x<\"" + alias + "\";print \"alias\",s,\"[\"x\"]\"";
        var script = (buffered ? "BEGIN{RS=\"[\\n]\"}" : "") + (reverse
            ? "BEGIN{" + readAlias + "}{print} END{" + readAlias + "}"
            : "{print;if(NR==1){" + readAlias + "}}");
        var expected = reverse
            ? "alias 1 [a]\n" + (moreData ? "d\ne\n" : "") + "alias 1 [b]\n"
            : "a\nalias " + (moreData ? "1 [d]" : "0 []") + "\nb\nc\n";
        foreach (var reuse in new[] { false, true })
        {
            var budget = new CooperativeTestBudget(16 << 20);
            var host = new AliasHost(reuse, moreData);
            var exit = await Execute(script, host, governed ? budget : null);
            Assert.Equal(0, exit.Value);
            Assert.Equal(expected, host.Output);
            Assert.Equal("", host.Error);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Fact]
    public async Task RefusalPrecedesTheReadThatWouldInvalidateAnUnreadTail()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new AliasHost(true, false, "a\n" + new string('b', 1_000_000) + "\n")
        { Before = () => budget.RefuseNextAdmissionMinimumGrowth = 100_000 };
        var exit = await Execute("{print \"before\";getline x<\"/dev/stdin\";print \"after\";exit}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, host.Reads);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingAliasReadKeepsThePreservedTailFunded(bool cancel)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new AliasHost(true, false, "a\n" + new string('b', 1_000_000) + "\n")
        { PendingRead = () => { entered.SetResult(); return completed.Task; } };
        var execution = Execute("{print \"before\";getline x<\"/dev/fd/0\";print \"after\";exit}", host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 1_000_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 1_000_000);
        }
        finally { completed.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else
        {
            Assert.Equal(0, (await execution).Value);
            Assert.Equal("before\nafter\n", host.Output);
        }
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task UnsharedNewlineReadsKeepTheBorrowedFastPath()
    {
        var host = new AliasHost(true, false);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance));
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn));
        var allocated = counters.AwkTextPayloadBytes;
        foreach (var expected in new[] { "a", "b", "c" })
            Assert.Equal(expected, (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Equal(3, counters.AwkTextPayloadBytes - allocated);
        Assert.Equal(0, counters.RecordSeparatorBytesScanned);
        Assert.Equal(1, host.Reads);
    }

    private static Task<AwkExitCode> Execute(string script, IAwkHost host, IMemoryBudget? budget, CancellationToken token = default) =>
        AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, budget is null ? AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance) : AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), token);

    private sealed class AliasHost(bool reuse, bool moreData, string input = "a\nb\nc\n") : IAwkHost
    {
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(input);
        public int Reads { get; private set; }
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public Action? Before { get; init; }
        public Func<Task>? PendingRead { get; init; }

        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            else if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += Encoding.UTF8.GetString(content.Span);
                if (content.Span.SequenceEqual("before\n"u8)) Before?.Invoke();
            }
            return Task.FromResult(AwkExitCode.Success);
        }

        public async Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            Assert.Equal(AwkFileDescriptor.StdIn, descriptor);
            Reads++;
            if (Reads == 1)
            {
                var offsets = new List<int>();
                for (var index = 0; index + 1 < _bytes.Length; index++)
                    if (_bytes[index] == (byte)'\n') offsets.Add(index + 1);
                return AwkReadResult.FromContent(_bytes, offsets.ToArray(),
                    moreData ? AwkReadCompletion.MoreData : AwkReadCompletion.EndOfFile);
            }
            if (PendingRead is { } pending) await pending().ConfigureAwait(false);
            if (reuse) _bytes.AsSpan().Fill((byte)'z');
            if (moreData && Reads == 2)
            {
                var next = reuse ? _bytes : new byte[6];
                "d\ne\n"u8.CopyTo(next);
                return AwkReadResult.FromContent(next.AsMemory(0, 4), [2], AwkReadCompletion.EndOfFile);
            }
            return AwkReadResult.EndOfFile;
        }

        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => Task.FromResult(AwkOpenedFile.MissingFile());
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkDescriptorCloseResult.Closed());
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Task.FromResult(AwkExitCode.Success);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Task.FromResult(AwkPipeCreationResult.NotSupported(default));
    }
}
