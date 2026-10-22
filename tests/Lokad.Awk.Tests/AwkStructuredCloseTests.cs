using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkStructuredCloseTests
{
    public static IEnumerable<object[]> FailureCases()
    {
        foreach (var kind in new[] { "main", "renamed-main", "exit", "redirect-input", "redirect-output", "close", "fflush", "source", "include", "input-pipe", "output-pipe" })
        foreach (var retired in new[] { false, true })
        foreach (var details in new[] { "visible", "hidden", "oversize" })
            yield return [kind, retired, details];
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    public async Task ExpectedCloseFailuresKeepTheirCauseAndRetirementContract(string kind, bool retired, string details)
    {
        var host = new CloseHost { RetiredFailure = retired };
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);
        if (details == "hidden") policy = policy.WithHostErrorDetails(false);
        if (details == "oversize") policy = policy.WithMaxHostErrorBytes(4);
        var (arguments, output, identity) = Case(kind);
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal(output, host.Output.ToString());
        var prefix = kind is "source" or "include" ? "Close failed" : "close failed";
        Assert.Equal("awk: " + identity + ": " + prefix + ": " +
            (details == "visible" ? "storage unavailable" : "Operation failed.") + "\n", host.Error.ToString());
        Assert.Empty(host.Active);
        Assert.Equal(retired ? 1 : 2, host.CloseAttempts[kind == "output-pipe" ? 11 : 10]);
        Assert.Equal(0, host.SuccessfulCloseReplays);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EarlierReadOrEvaluationFailureWinsWhileOtherResourcesAreCleaned(bool readFailure)
    {
        var host = new CloseHost { RetiredFailure = true, FailReads = readFailure };
        var arguments = readFailure ? new[] { "{}", "data" } : new[] { "BEGIN{print 1>\"one\";print 2>\"two\";x=1/0}" };
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, CancellationToken.None));
        Assert.Equal(readFailure ? "awk: file descriptor 10: read failed: read denied\n" :
            "awk: division by zero attempted\n", host.Error.ToString());
        Assert.Empty(host.Active);
        Assert.Equal(0, host.SuccessfulCloseReplays);
    }

    [Fact]
    public async Task FailedDiagnosticOutputDoesNotSkipResourceCleanup()
    {
        var host = new CloseHost { RetiredFailure = true, RejectStderr = true };
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{print 1>\"one\";print 2>\"two\"}"], []));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, CancellationToken.None));
        Assert.Equal("", host.Error.ToString());
        Assert.Empty(host.Active);
        Assert.Equal(0, host.SuccessfulCloseReplays);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SourceReadFailureRemainsPrimaryWhenCloseAlsoReportsAHostCause(bool retired)
    {
        var host = new CloseHost { RetiredFailure = retired, FailReads = true };
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", ["-f", "/script.awk"], []));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, CancellationToken.None));
        Assert.Equal("awk: /script.awk: read denied\n", host.Error.ToString());
        Assert.Empty(host.Active);
        Assert.Equal(retired ? 1 : 2, host.CloseAttempts[10]);
    }

    [Fact]
    public async Task RetiredPipeFailureStillCancelsAndAwaitsTheActualCommandBeforeRefund()
    {
        var finish = new TaskCompletionSource<IReadOnlyList<AwkExitCode>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new CloseHost { RetiredFailure = true, CommandEnd = finish.Task };
        var budget = new CooperativeTestBudget(1 << 20);
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{print 1|\"sink a:b\";close(\"sink a:b\")}"], []));
        var run = tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
        try
        {
            await host.CommandCancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.True(budget.Reserved > 0);
        }
        finally { finish.TrySetResult([AwkExitCode.Success]); }
        Assert.Equal(AwkExitCode.FatalError, await run);
        Assert.Equal("awk: sink a:b: close failed: storage unavailable\n", host.Error.ToString());
        Assert.Equal(1, host.CloseAttempts[11]);
        Assert.Empty(host.Active);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingCloseRetainsFundingAndRetirementThroughCancellation(bool retired)
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new CloseHost { RetiredFailure = retired, BeforeFirstClose = async () => { entered.SetResult(); await finish.Task; } };
        var budget = new CooperativeTestBudget(1 << 20);
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", ["{}", "data"], []));
        var run = tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Single(host.Active);
            Assert.True(budget.Reserved > 0);
        }
        finally { finish.TrySetResult(); }
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Empty(host.Active);
        Assert.Equal(retired ? 1 : 2, host.CloseAttempts[10]);
        Assert.Equal("", host.Error.ToString());
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void DefaultCloseFailureLeavesOwnershipExplicit()
    {
        var result = default(AwkDescriptorCloseResult);
        Assert.False(result.Success);
        Assert.False(result.DescriptorRetired);
        Assert.True(AwkDescriptorCloseResult.Closed().Success);
        Assert.True(AwkDescriptorCloseResult.Closed().DescriptorRetired);
    }

    [Fact]
    public async Task BorrowedStandardInputIsNeverClosedByRuntimeCleanup()
    {
        var host = new CloseHost { RetiredFailure = true };
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", ["{}"], []));
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, CancellationToken.None));
        Assert.Empty(host.CloseAttempts);
        Assert.Equal("", host.Error.ToString());
    }

    private static (string[] Arguments, string Output, string Identity) Case(string kind) => kind switch
    {
        "main" => (["{print}", "data"], "row\n", "data"),
        "renamed-main" => (["{FILENAME=\"changed\";ARGV[1]=\"changed\";print}", "data"], "row\n", "data"),
        "exit" => (["{print;exit}", "data"], "row\n", "data"),
        "redirect-input" => (["BEGIN{print \"before\";getline x<\"data\"}"], "before\n", "/data"),
        "redirect-output" => (["BEGIN{print \"before\";print 1>\"out\"}"], "before\n", "/out"),
        "close" => (["BEGIN{print \"before\";print 1>\"out\";close(\"out\")}"], "before\n", "out"),
        "fflush" => (["BEGIN{print \"before\";print 1>\"out\";fflush(\"out\")}"], "before\n", "out"),
        "source" => (["-f", "/script.awk"], "", "/script.awk"),
        "include" => (["-i", "/include.awk", "BEGIN{print \"body\"}"], "", "/include.awk"),
        "input-pipe" => (["BEGIN{print \"before\";\"source a:b\"|getline x;close(\"source a:b\")}"], "before\n", "source a:b"),
        _ => (["BEGIN{print \"before\";print 1|\"sink a:b\";close(\"sink a:b\")}"], "before\n", "sink a:b")
    };

    private sealed class CloseHost : IAwkHost
    {
        private int _next = 10;
        private bool _failed;
        private readonly Dictionary<AwkFileDescriptor, byte[]> _inputs = [];
        public bool RetiredFailure { get; init; }
        public bool FailReads { get; init; }
        public bool RejectStderr { get; init; }
        public Func<Task>? BeforeFirstClose { get; init; }
        public Task<IReadOnlyList<AwkExitCode>>? CommandEnd { get; init; }
        public TaskCompletionSource CommandCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public HashSet<AwkFileDescriptor> Active { get; } = [];
        public Dictionary<int, int> CloseAttempts { get; } = [];
        public int SuccessfulCloseReplays { get; private set; }
        public StringBuilder Output { get; } = new();
        public StringBuilder Error { get; } = new();

        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        {
            var descriptor = NewDescriptor();
            _inputs[descriptor] = path.Path.EndsWith(".awk", StringComparison.Ordinal)
                ? "BEGIN{print \"body\"}"u8.ToArray() : "row\n"u8.ToArray();
            return Task.FromResult(AwkOpenedFile.Opened(descriptor));
        }
        public async Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            CloseAttempts[descriptor.Id] = CloseAttempts.GetValueOrDefault(descriptor.Id) + 1;
            if (!Active.Contains(descriptor)) { SuccessfulCloseReplays++; return AwkDescriptorCloseResult.Failure(default, true); }
            if (!_failed)
            {
                _failed = true;
                if (BeforeFirstClose is { } before) await before();
                if (RetiredFailure) Active.Remove(descriptor);
                return AwkDescriptorCloseResult.Failure(new("storage unavailable"), RetiredFailure);
            }
            Active.Remove(descriptor);
            return AwkDescriptorCloseResult.Closed();
        }
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (FailReads) return Task.FromResult(AwkReadResult.Failure(new("read denied")));
            return Task.FromResult(_inputs.Remove(descriptor, out var content)
                ? AwkReadResult.FromContent(content, [], AwkReadCompletion.EndOfFile) : AwkReadResult.EndOfFile);
        }
        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (descriptor == AwkFileDescriptor.StdErr && RejectStderr) return Task.FromResult(AwkExitCode.FatalError);
            if (descriptor == AwkFileDescriptor.StdOut) Output.Append(Encoding.UTF8.GetString(content.Span));
            if (descriptor == AwkFileDescriptor.StdErr) Error.Append(Encoding.UTF8.GetString(content.Span));
            return Task.FromResult(AwkExitCode.Success);
        }
        public async Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token)
        {
            using var registration = token.Register(() => CommandCancelled.TrySetResult());
            try { return CommandEnd is { } end ? await end : [AwkExitCode.Success]; }
            finally
            {
                // The host owns a consumer command's read endpoint; input pipes retain
                // their runtime-owned output endpoint for the runtime to close.
                foreach (var command in commands)
                    if (command.StdIn != AwkFileDescriptor.StdIn) Active.Remove(command.StdIn);
            }
        }
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token)
        {
            var read = NewDescriptor();
            _inputs[read] = "row\n"u8.ToArray();
            return Task.FromResult(AwkPipeCreationResult.Created(new(read, NewDescriptor())));
        }
        private AwkFileDescriptor NewDescriptor()
        {
            var result = new AwkFileDescriptor(_next++);
            Active.Add(result);
            return result;
        }
    }
}
