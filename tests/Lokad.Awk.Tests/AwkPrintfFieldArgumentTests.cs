namespace Lokad.Awk.Tests;

public sealed class AwkPrintfFieldArgumentTests
{
    [Fact]
    public async Task OutputMatchesOwnedFields()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        Assert.Equal(await RunAsync(true, limits), await RunAsync(false, limits));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PendingHostBorrowsOwnedFormattingAfterRecordChanges(bool cancel, bool fieldList)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(2 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, cancellation.Token);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance)
        { MemoryOwner = owner };
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token, options);
        var host = new PendingHost();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, cancellation.Token, counters, limits, state, static _ => []);
        AwkExpression[] fields = [AwkExpression.FieldIndex(AwkExpression.Number(1)), AwkExpression.FieldIndex(AwkExpression.Number(2))];
        var statement = fieldList ? AwkStatement.Print(fields)
            : AwkStatement.Printf(AwkExpression.Literal("%s:%04d\n"), fields);
        var expected = fieldList ? "before 7\n" : "before:0007\n";
        var program = new AwkProgram([], [new(null, [statement])], [], [], [], new Dictionary<AwkFunctionName, AwkUserFunction>());
        using var executor = new AwkRuntimeActionExecutor(program, state);
        var run = executor.ProcessRecordAsync(state, io, AwkText.FromSourceLiteralString("before 7")).AsTask();
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(host.Bytes.Span));
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("changed 999"));
            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(host.Bytes.Span));
            Assert.True(budget.Reserved > 0);
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(run.IsCompleted);
                Assert.True(budget.Reserved > 0);
                Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(host.Bytes.Span));
            }
            host.Release.TrySetResult(AwkAppendResult.Accepted());
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            host.Release.TrySetResult(AwkAppendResult.Accepted());
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseAsync();
            executor.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, budget.Reserved);
        }
    }

    private static async Task<Result> RunAsync(bool owned, AwkRuntimeLimits limits, string format = "%s:%04d\\n")
    {
        var fields = owned ? "$i,$j" : "$1,$2";
        var invocation = AwkTestHarness.BuildInvocation("awk", "-v", "i=1", "-v", "j=2", "{printf \"" + format + "\"," + fields + "}");
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), invocation, CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var host = new MockFileSystem();
        host.SetStandardInput("head 007\nother -3\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(2 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, memoryBudget: budget, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
        return new(exit.Value, host.GetOutput(AwkFileDescriptor.StdOut), host.GetOutput(AwkFileDescriptor.StdErr), counters.HostCalls, counters.FieldsMaterialized, counters.FieldSplits);
    }

    private sealed class PendingHost : IAwkHost
    {
        private readonly MockFileSystem _files = new();
        public ReadOnlyMemory<byte> Bytes { get; private set; }
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<AwkAppendResult> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            Bytes = bytes;
            Entered.TrySetResult(true);
            return Release.Task; // Deliberately ignore cancellation: bytes remain borrowed until actual completion.
        }
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => _files.RunCommandAsync(command, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token) => _files.OpenFileAsync(path, mode, token);
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => _files.ReadAsync(descriptor, count, token);
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => _files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => _files.CreatePipeAsync(token);
    }

    private readonly record struct Result(int Exit, string Output, string Error, long Calls, long Fields, long Splits);
}
