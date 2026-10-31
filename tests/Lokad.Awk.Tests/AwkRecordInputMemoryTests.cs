using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordInputMemoryTests
{
    [Fact]
    public async Task LaterActionsReuseRelinquishedPayloadUnderSharedPressure()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        IMemoryReservation? pressure = null;
        var host = new InputHost(budget, "row\n")
        {
            PendingOutput = () =>
            {
                if (pressure is null)
                {
                    Assert.True(budget.TryReserve(budget.Limit - budget.Reserved - (32 << 10), out var reserved));
                    pressure = reserved;
                }
                return Task.CompletedTask;
            }
        };
        try
        {
            var exit = await Execute("BEGIN{pad=sprintf(\"%100000s\",\"\")}{print \"held\";pad=\"\";need=sprintf(\"%25000s\",\"\");print length(need);exit}", host, budget);
            Assert.Equal(0, exit.Value);
            Assert.Equal("held\n25000\n", host.Output);
            Assert.Equal("", host.Error);
            Assert.Equal(0, budget.Refusals);
        }
        finally { pressure?.Dispose(); }
        AssertRefunded(budget);
    }

    [Fact]
    public async Task LaterHostUsersCanFundTheirBufferAfterMainPayloadIsRelinquished()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        IMemoryReservation? pressure = null;
        var appends = 0;
        var host = new InputHost(budget, "row\n")
        {
            PendingOutput = () =>
            {
                if (++appends == 1)
                {
                    Assert.True(budget.TryReserve(budget.Limit - budget.Reserved - (32 << 10), out var reserved));
                    pressure = reserved;
                }
                else
                {
                    Assert.True(budget.TryReserve(50000, out var buffer));
                    using (buffer) { } // The host's completed use returns its borrowed root allowance.
                }
                return Task.CompletedTask;
            }
        };
        try
        {
            var exit = await Execute("BEGIN{pad=sprintf(\"%100000s\",\"\")}{print \"held\";pad=\"\";print \"released\";exit}", host, budget);
            Assert.Equal(0, exit.Value);
            Assert.Equal("held\nreleased\n", host.Output);
            Assert.Equal("", host.Error);
            Assert.Equal(0, budget.Refusals);
        }
        finally { pressure?.Dispose(); }
        AssertRefunded(budget);
    }

    [Fact]
    public async Task RelinquishedBeginPayloadRefundsBeforeLaterMainActions()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "row\n");
        var exit = await Execute("BEGIN{pad=sprintf(\"%100000s\",\"\")}{print \"held\";pad=\"\";print \"released\";exit}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("held\nreleased\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(2, host.OutputAdmission.Count);
        Assert.True(host.OutputAdmission[0] - host.OutputAdmission[1] >= 90000,
            "Relinquished scalar capacity remains charged across later actions: " + string.Join(",", host.OutputAdmission));
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderMatchAdmissionFailsBeforeRecordEffects(bool injectedRefusal)
    {
        var budget = new CooperativeTestBudget(injectedRefusal ? 16 << 20 : 192 << 10);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000))
        {
            OnRead = () => { if (injectedRefusal) budget.RefuseNextAdmissionMinimumGrowth = 100_000; }
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
            Assert.InRange(budget.Reserved, AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10));
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.InRange(budget.Reserved, AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10));
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
        Assert.InRange(Assert.Single(host.OutputAdmission), AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10));
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
        Assert.True(host.Reads > 2);
        Assert.InRange(host.ReadAdmission[^1], 1_000_000, 1_500_000);
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
        Assert.All(host.OutputAdmission.Take(2), bytes => Assert.InRange(bytes, AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10)));
        Assert.InRange(host.OutputAdmission[2], 1, 100_000);
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BufferedCurrentRecordStaysFundedThroughActualPendingOutput(bool cancel)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appends = 0;
        var host = new InputHost(budget, "a 0007\nb 8\na 10\n")
        {
            PendingOutput = () =>
            {
                if (++appends != 1) return Task.CompletedTask;
                entered.SetResult();
                return release.Task;
            }
        };
        var execution = Execute("{sum[$1]+=$2}END{print sum[\"a\"];print \"done\"}", host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var held = budget.Reserved;
            Assert.True(held > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.Equal(held, budget.Reserved);
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal(cancel ? "17\n" : "17\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task RefusedLargerInputStopsBeforeLaterRecordAndEndEffects()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        var reads = 0;
        var host = new InputHost(budget, "keep 7\n", new string('x', 3000) + " 9\n")
        {
            OnRead = () => { if (++reads == 2) budget.RefuseNextAdmissionMinimumGrowth = 1; }
        };
        var exit = await Execute("{print $1}END{print \"end\"}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("keep\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        AssertRefunded(budget);
    }

    [Fact]
    public void MainInputResultKeepsItsCompactTransport()
    {
        Assert.Equal(32, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkInputRecordResult>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InactiveBufferGrowthFailurePreservesTheCurrentRecord(bool cancel)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var cancellation = new CancellationTokenSource();
        using var storage = new AwkRecordStorage(memory.Accounting, AwkRuntimeCounters.Disabled,
            cancellation.Token.ThrowIfCancellationRequested);
        Assert.True(storage.TryStage("old 0007"u8));
        var original = storage.Content;
        if (cancel) memory.Budget.OnGrowth = _ => cancellation.Cancel();
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var next = Encoding.UTF8.GetBytes(new string('x', 3000));
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => storage.TryStage(next));
        else Assert.Throws<AwkRuntimeException>(() => storage.TryStage(next));
        Assert.Equal("old 0007", Encoding.UTF8.GetString(storage.Content.Span));
        Assert.Equal(original, storage.Content);
        storage.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void RecordBufferRetentionIsBoundedAndOversizedInputKeepsTheOwnedPath()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var storage = new AwkRecordStorage(memory.Accounting, AwkRuntimeCounters.Disabled, static () => { });
        foreach (var count in new[] { 1, 32, 4096, 128, 2048, 1, 4096, 16 })
        {
            var bytes = Encoding.UTF8.GetBytes(new string('x', count));
            Assert.True(storage.TryStage(bytes));
            Assert.True(storage.Content.Span.SequenceEqual(bytes));
            Assert.InRange(memory.Budget.Reserved, 1, 8192);
        }
        var held = memory.Budget.Reserved;
        var last = storage.Content;
        Assert.False(storage.TryStage(new byte[8192]));
        Assert.Equal(last, storage.Content);
        Assert.Equal(held, memory.Budget.Reserved);
        storage.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
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
        private int _batchIndex;
        private int _batchOffset;
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
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            ReadAdmission.Add(budget.Reserved);
            OnRead?.Invoke();
            Reads++;
            if (_batchIndex == _batches.Length) return ValueTask.FromResult(AwkReadBuffer.EndOfFile);
            var batch = _batches[_batchIndex];
            var length = Math.Min(count, batch.Length - _batchOffset);
            var content = batch.AsMemory(_batchOffset, length);
            return ValueTask.FromResult(AwkReadBuffer.FromContent(content,
                _batchIndex + 1 == _batches.Length && _batchOffset + length == batch.Length
                    ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData, consumed =>
                {
                    _batchOffset += consumed;
                    if (_batchOffset == batch.Length) { _batchIndex++; _batchOffset = 0; }
                }));
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token) => Task.FromResult(AwkOpenedFile.MissingFile());
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkDescriptorCloseResult.Closed());
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Task.FromResult(AwkExitCode.Success);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Task.FromResult(AwkPipeCreationResult.NotSupported(default));
    }
}
