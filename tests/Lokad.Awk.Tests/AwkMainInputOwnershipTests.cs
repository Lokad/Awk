namespace Lokad.Awk.Tests;

public sealed class AwkMainInputOwnershipTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task PendingRecordPipelinePreservesFilesRulesAndFunctionFlow(bool output, bool governed, bool cancel)
    {
        const string source = """
            function use(v){if(v=="skip")nextfile;return v}
            BEGINFILE{print "open",FILENAME}
            {print use($1)}
            {print "tail",FNR}
            ENDFILE{print "close",FNR}
            END{print "end",NR}
            """;
        const string prefix = "open one\none\ntail 1\nclose 2\nopen two\n";
        var host = new PausingPipelineHost(output);
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source, "one", "two"))
            .ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(prefix, host.Files.GetOutput(AwkFileDescriptor.StdOut));
            if (output) Assert.True(host.Borrowed.Span.SequenceEqual("two\n"u8));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel)
            {
                cancellation.Cancel();
                await host.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(run.IsCompleted);
                Assert.Equal(held, budget.Reserved);
                if (output) Assert.True(host.Borrowed.Span.SequenceEqual("two\n"u8));
            }
            host.Release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(prefix + (cancel ? output ? "two\n" : "" : "two\ntail 1\nclose 1\nend 3\n"),
                host.Files.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.Files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.Files.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            host.Release.TrySetResult();
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    private sealed class PausingPipelineHost : IAwkHost
    {
        private readonly bool _output;
        private AwkFileDescriptor? _second;
        public MockFileSystem Files { get; } = new();
        public ReadOnlyMemory<byte> Borrowed { get; private set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal PausingPipelineHost(bool output)
        {
            _output = output;
            Files.AddFile("/one", "one\nskip\nunread\n");
            Files.AddFile("/two", "two\n");
        }

        private async Task WaitAsync(CancellationToken token)
        {
            using var registration = token.Register(() => Canceled.TrySetResult());
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false); // The actual user deliberately ignores cancellation.
        }

        public async Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token)
        {
            var opened = await Files.OpenFileAsync(path, mode, token).ConfigureAwait(false);
            if (opened.Success && Files.ReadOpenCount == 2) _second = opened.FileDescriptor;
            return opened;
        }

        public async ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (!_output && _second is { } second && descriptor == second) await WaitAsync(token).ConfigureAwait(false);
            return await Files.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
        }

        public async Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            if (_output && descriptor == AwkFileDescriptor.StdOut && bytes.Span.SequenceEqual("two\n"u8))
            {
                Borrowed = bytes;
                try
                {
                    await WaitAsync(token).ConfigureAwait(false);
                    // Acceptance is an in-flight effect even when the caller canceled.
                    return await Files.AppendAsync(descriptor, bytes, CancellationToken.None).ConfigureAwait(false);
                }
                finally { Borrowed = default; }
            }
            return await Files.AppendAsync(descriptor, bytes, token).ConfigureAwait(false);
        }

        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Files.RunCommandAsync(command, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Files.CreatePipeAsync(token);
    }

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
        public async Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token)
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
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Files.ReadAsync(descriptor, count, token);
        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token) => Files.AppendAsync(descriptor, content, token);
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Files.RunCommandAsync(command, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Files.CreatePipeAsync(token);
    }
}
