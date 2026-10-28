using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkCsvHostTests
{
    public static IEnumerable<object[]> Sources()
    {
        foreach (var source in new[] { "stdin", "file", "pipe" })
        foreach (var governed in new[] { false, true })
            yield return [source, governed];
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public async Task ReusedBatchesPreserveQuotesNewlinesAndUtf8(string source, bool governed)
    {
        var input = Encoding.UTF8.GetBytes("a,\"b,\r\nc\",\"x\"\"y\"\r\né,\"😀\",0007\n,\nlast\r");
        const string body = "printf \"%d|%s|%s|%s\\n\",NF,$1,$2,$3";
        var script = source == "pipe" ? "BEGIN{while((\"source\"|getline)>0){" + body + "} close(\"source\")}" : "{" + body + "}";
        for (var chunk = 1; chunk <= input.Length; chunk++)
        {
            var host = new CsvHost(input, chunk, source);
            var budget = new CooperativeTestBudget(8 << 20);
            string[] args = source == "file" ? ["--csv", script, "/input"] : ["-k", script];
            var exit = await Execute(host, args, governed ? budget : null);
            Assert.Equal(0, exit.Value);
            Assert.Equal("3|a|b,\nc|x\"y\n3|é|😀|0007\n2|||\n1|last||\n", host.Output);
            Assert.Equal("", host.Error);
            Assert.Empty(host.OpenDescriptors);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MalformedUtf8IsRejectedBeforeCsvRecordPublication(bool normalize)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        byte[] bytes = normalize ? [(byte)'a', (byte)',', (byte)'"', 13, 10, 0xFF, (byte)'"', 10]
            : [(byte)'a', (byte)',', (byte)'"', 0xFF, (byte)'"', 10];
        var host = new CsvHost(bytes, 1);
        var exit = await Execute(host, ["--csv", "{print \"after\"}"], budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("", host.Output);
        Assert.Equal("awk: invalid UTF-8 input\n", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RefusedQuotedRecordGrowthDoesNotPublishARecordOrContinueReading()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new CsvHost(Encoding.UTF8.GetBytes("a,\"" + new string('x', 1_000_000) + "\"\n"), 500_000);
        host.BeforeRead = read =>
        {
            if (read == 2) budget.RefuseNextAdmissionMinimumGrowth = 100_000;
            return Task.CompletedTask;
        };
        var exit = await Execute(host, ["--csv", "BEGIN{print \"before\"}{print \"after\"}"], budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(2, host.Reads);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RefusedCsvSplitDoesNotRunFollowingSideEffects()
    {
        var budget = new CooperativeTestBudget(32 << 20);
        var input = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("x,", 100_000)) + "z\n");
        var host = new CsvHost(input, input.Length)
        {
            AfterOutput = () => budget.RefuseNextAdmissionMinimumGrowth = 100_000
        };
        var exit = await Execute(host, ["--csv", "{a[1]=\"old\"; print \"before\"; n=split($0,a); print \"after\",n}"], budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingReadKeepsTheRecordFundedUntilActualCompletion(bool cancel)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new CsvHost(Encoding.UTF8.GetBytes("a,\"" + new string('x', 300_000) + "\"\n"), 150_000);
        host.BeforeRead = read =>
        {
            if (read != 2) return Task.CompletedTask;
            entered.SetResult();
            return release.Task;
        };
        var execution = Execute(host, ["--csv", "{print length($2)}"], budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 150_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 150_000);
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else
        {
            Assert.Equal(0, (await execution).Value);
            Assert.Equal("300000\n", host.Output);
        }
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PreparedCsvStaysByteNativeAndScansLinearly(bool governed)
    {
        const int records = 1000;
        var bytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("a,\"b\"\"c\"\n", records)));
        var host = new CsvHost(bytes, 7);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), memoryBudget: governed ? budget : null);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["--csv", "{n+=length($2)} END{print n}"], []);
        await using var compiled = await AwkExecutor.CompileAsync(host, command, CancellationToken.None);
        Assert.Equal(0, (await compiled.ExecuteAsync(host, CancellationToken.None, options)).Value);
        Assert.Equal("3000\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.InRange(counters.RecordSeparatorBytesScanned, bytes.Length, bytes.Length + 1L);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.InRange(counters.AwkTextAllocations, records, records * 4L);
        Assert.Equal(0, budget.Reserved);
    }

    private static Task<AwkExitCode> Execute(CsvHost host, string[] arguments, IMemoryBudget? budget = null,
        CancellationToken token = default)
    {
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []);
        var awk = AwkTestHarness.ParseAwk(command);
        var policy = AwkExecutionPolicy.CreateDefault().WithTimeZone(TimeZoneInfo.Utc);
        if (budget is not null) policy = policy.WithMemoryBudget(budget);
        return awk.ExecuteAsync(host, policy, token);
    }

    private sealed class CsvHost(byte[] input, int chunkSize, string source = "stdin") : IAwkHost
    {
        private readonly byte[] _scratch = new byte[Math.Min(input.Length, chunkSize)];
        private int _position;
        public int Reads { get; private set; }
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public HashSet<AwkFileDescriptor> OpenDescriptors { get; } = [];
        public Func<int, Task>? BeforeRead { get; set; }
        public Action? AfterOutput { get; init; }
        public async Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int maxLines, CancellationToken token)
        {
            if (_position == input.Length || source == "pipe" && descriptor == AwkFileDescriptor.StdIn)
                return AwkReadResult.EndOfFile;
            Reads++;
            if (BeforeRead is { } before) await before(Reads);
            var count = Math.Min(chunkSize, input.Length - _position);
            input.AsSpan(_position, count).CopyTo(_scratch);
            _position += count;
            return AwkReadResult.FromContent(_scratch.AsMemory(0, count), [],
                _position == input.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData);
        }
        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += Encoding.UTF8.GetString(bytes.Span);
                AfterOutput?.Invoke();
            }
            else if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(bytes.Span);
            return Task.FromResult(AwkExitCode.Success);
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        {
            var descriptor = new AwkFileDescriptor(501);
            OpenDescriptors.Add(descriptor);
            return Task.FromResult(AwkOpenedFile.Opened(descriptor));
        }
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            OpenDescriptors.Remove(descriptor);
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        }
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token)
        {
            var pair = new AwkPipeDescriptors(new(501), new(502));
            OpenDescriptors.Add(pair.Read);
            OpenDescriptors.Add(pair.Write);
            return Task.FromResult(AwkPipeCreationResult.Created(pair));
        }
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) =>
            Task.FromResult(AwkExitCode.Success);
    }
}
