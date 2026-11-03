using System.Text;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRecordCursorContextTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task PendingRefillsKeepPartialRecordsAndFundingUntilActualCompletion(
        bool governed, bool afterPrefix, bool cancel)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("prefix\n" + string.Concat(Enumerable.Repeat("a😀", 4096)) + "\ntail\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        host.ReadAsyncOverride = ReadAsync;
        async ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            using var registration = token.Register(() => canceled.TrySetResult());
            if (++reads == (afterPrefix ? 9 : 1))
            {
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false); // The host deliberately ignores cancellation here.
            }
            else await Task.Yield();
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, Math.Min(maximum, 128), token).ConfigureAwait(false); }
            finally { host.ReadAsyncOverride = ReadAsync; }
        }
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        using var cancellation = new CancellationTokenSource();
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "{print length($0),NR}END{print \"end\"}")).ExecuteAsync(host, policy, cancellation.Token);
        var before = afterPrefix ? "6 1\n" : "";
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(before, host.GetOutput(AwkFileDescriptor.StdOut));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel)
            {
                cancellation.Cancel();
                await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(run.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? before : "6 1\n8192 2\n4 3\nend\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            release.TrySetResult();
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    public static IEnumerable<object[]> Configurations()
    {
        foreach (var golden in AwkGoldenCorpus.DirectCases.Where(c => c.Name.StartsWith("cursor-reader-", StringComparison.Ordinal)))
        foreach (var source in new[] { "stdin", "file", "pipe" })
        foreach (var chunkBytes in new[] { 1, 128 })
        foreach (var governed in new[] { false, true })
            yield return [golden, source, chunkBytes, governed];
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public async Task CursorContextDoesNotDependOnHostBatches(AwkGoldenCase golden, string source, int chunkBytes, bool governed)
    {
        var script = golden.Arguments[0];
        if (source == "pipe") script = script.Replace("(getline)", "(\"source\" | getline)", StringComparison.Ordinal);
        var arguments = source == "file" ? new[] { script, "/input" } : new[] { script };
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new CursorHost(golden.StandardInput, chunkBytes, source);
        var awk = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(golden.ExpectedExitCode, exit.Value);
        Assert.Equal(golden.ExpectedStdOut, host.Output);
        Assert.Equal(golden.ExpectedStdErr, host.Error);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RegexRecordWindowsBorrowTheUtf8Batch(bool governed)
    {
        const int count = 2048;
        var budget = new CooperativeTestBudget(8 << 20);
        var input = string.Concat(Enumerable.Repeat("ax", count)) + "q\n";
        var host = new CursorHost(input, 128, "stdin");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            AwkRuntimeLimits.CreateDefault(), memoryBudget: governed ? budget : null, regexEngine: AwkUtf8RegexEngine.Instance);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{RS=\"x|\\\\<b\"}{n++}END{print n}"], []);
        var compiled = await AwkExecutor.CompileAsync(host, command, CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        await using (compiled)
            Assert.Equal(0, (await compiled.ExecuteAsync(host, CancellationToken.None, options)).Value);
        Assert.Equal((count + 1) + "\n", host.Output);
        Assert.Equal("", host.Error);
        // Record/RT copies stay linear; searches borrow UTF-8 without a decoded batch.
        Assert.InRange(counters.AwkTextPayloadBytes, input.Length, input.Length * 2L);
        Assert.Equal(0, counters.StringDecodes); // Patterns and record searches remain byte-native in core.
        if (governed) Assert.True(budget.Peak > 0);
        Assert.Equal(0, budget.Reserved);
    }

    private sealed class CursorHost(string input, int chunkBytes, string source) : IAwkHost
    {
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(input);
        private int _position;
        public HashSet<AwkFileDescriptor> OpenDescriptors { get; } = [];
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int maxBytes, CancellationToken token)
        {
            if (_position == _bytes.Length || source == "pipe" && descriptor == AwkFileDescriptor.StdIn)
                return ValueTask.FromResult(AwkReadBuffer.EndOfFile);
            var length = Math.Min(Math.Min(chunkBytes, maxBytes), _bytes.Length - _position);
            return ValueTask.FromResult(AwkReadBuffer.FromContent(_bytes.AsMemory(_position, length),
                _position + length == _bytes.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData,
                consumed => _position += consumed));
        }
        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdOut) Output += Encoding.UTF8.GetString(content.Span);
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            return Task.FromResult(AwkAppendResult.Accepted());
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token)
        {
            var descriptor = new AwkFileDescriptor(501);
            OpenDescriptors.Add(descriptor);
            return Task.FromResult(AwkOpenedFile.Opened(descriptor, AwkRawPath.Parse(path.Path).ToAbsolute(path.CurrentDirectory)));
        }
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            OpenDescriptors.Remove(descriptor);
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        }
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token)
        {
            var descriptors = new AwkPipeDescriptors(new(501), new(502));
            OpenDescriptors.Add(descriptors.Read);
            OpenDescriptors.Add(descriptors.Write);
            return Task.FromResult(AwkPipeCreationResult.Created(descriptors));
        }
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) =>
            Task.FromResult(AwkExitCode.Success);
    }
}
