namespace Lokad.Awk.Tests;

public sealed class AwkSourceCloseTests
{
    public static IEnumerable<object[]> CloseCases()
    {
        foreach (var outcome in new[] { "success", "failure", "throw" })
        foreach (var readFailure in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [outcome, readFailure, governed];
    }

    [Theory]
    [MemberData(nameof(CloseCases))]
    public async Task SourceCloseKeepsOwnershipUntilCleanupAndPreservesThePrimaryDiagnostic(
        string outcome, bool readFailure, bool governed)
    {
        var host = new CloseHost(outcome, readFailure);
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);

        var exit = await Tool().ExecuteAsync(host, policy, CancellationToken.None);

        var success = outcome == "success" && !readFailure;
        Assert.Equal(success ? AwkExitCode.Success : AwkExitCode.FatalError, exit);
        Assert.Equal(success ? "body\n" : string.Empty, host.Files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(readFailure ? "awk: /script.awk: read denied\n" : success ? string.Empty :
            "awk: /script.awk: Close failed.\n", host.Files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.Files.OpenFileCount);
        Assert.Equal(0, host.SuccessfulCloseReplays);
        Assert.InRange(host.CloseAttempts, 1, 2);
        Assert.Equal(governed, budget.Peak > 0);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationDuringFailedCloseAwaitsCompletionAndCleansUp(bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource<AwkDescriptorCloseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new CloseHost("failure", false) { PendingClose = pending.Task };
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = Tool().ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await host.CloseEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Equal(1, host.Files.OpenFileCount);
            Assert.Equal(governed, budget.Reserved > 0);
            pending.TrySetResult(AwkDescriptorCloseResult.Failure(default, descriptorRetired: false));
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(0, host.Files.OpenFileCount);
            Assert.Equal(0, host.SuccessfulCloseReplays);
            Assert.Equal(string.Empty, host.Files.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.Files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            pending.TrySetResult(AwkDescriptorCloseResult.Failure(default, descriptorRetired: false));
            try { await run; }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }

    private static Awk Tool() => AwkTestHarness.ParseAwk(
        AwkCommandInvocation.CreateWithStandardDescriptors("awk", ["-f", "/script.awk"], []));

    private sealed class CloseHost : IAwkHost
    {
        private readonly string _outcome;
        private bool _closed;
        public MockFileSystem Files { get; } = new();
        public int CloseAttempts { get; private set; }
        public int SuccessfulCloseReplays { get; private set; }
        public Task<AwkDescriptorCloseResult>? PendingClose { get; init; }
        public TaskCompletionSource CloseEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CloseHost(string outcome, bool readFailure)
        {
            _outcome = outcome;
            Files.AddFile("/script.awk", "BEGIN { print \"body\" }");
            if (readFailure) Files.ReadLinesOverride = static (_, _) => AwkReadResult.Failure(new(AwkOperationErrorKind.PermissionDenied, "read denied"));
        }

        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            Assert.False(token.CanBeCanceled);
            if (_closed) SuccessfulCloseReplays++;
            if (++CloseAttempts == 1)
            {
                CloseEntered.TrySetResult();
                if (PendingClose is { } pending) return pending;
                if (_outcome == "failure") return Task.FromResult(AwkDescriptorCloseResult.Failure(default, descriptorRetired: false));
                if (_outcome == "throw") throw new IOException("Private host close details");
            }
            _closed = true;
            return Files.CloseDescriptorAsync(descriptor, token);
        }

        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) =>
            Files.OpenFileAsync(path, mode, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) =>
            Files.ReadLinesAsync(descriptor, count, token);
        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token) =>
            Files.AppendAsync(descriptor, content, token);
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) =>
            Files.RunCommandAsync(command, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) =>
            Files.CreatePipeAsync(token);
    }
}
