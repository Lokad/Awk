namespace Lokad.Awk.Tests;

public sealed class AwkPrintfFieldArgumentTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(3)] [InlineData(5)]
    [InlineData(8)] [InlineData(12)] [InlineData(20)] [InlineData(80)]
    public async Task ExactFuelAndPartialOutputMatchOwnedFields(int fuel)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        if (fuel != 0) limits = limits.WithMaxExecutionSteps(fuel);
        Assert.Equal(await RunAsync(true, limits), await RunAsync(false, limits));
    }

    [Theory]
    [InlineData("fields")] [InlineData("builder")] [InlineData("unused")]
    public async Task AllFieldsPreflightBeforeAnyObservableOutput(string refusal)
    {
        var limits = refusal == "builder" ? AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(2)
            : AwkRuntimeLimits.CreateDefault().WithMaxFields(1);
        var format = refusal == "unused" ? "unused" : "%s:%04d\\n";
        var borrowed = await RunAsync(false, limits, format);
        Assert.Equal(await RunAsync(true, limits, format), borrowed);
        Assert.Equal(2, borrowed.Exit);
        Assert.Equal("", borrowed.Output);
        Assert.DoesNotContain("Exception", borrowed.Error);
        Assert.Contains("limit exceeded", borrowed.Error);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingHostBorrowsOwnedFormattingAfterRecordChanges(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(2 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, cancellation.Token);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, limits, AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter)
            { MemoryOwner = owner };
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token, options);
        var host = new PendingHost();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, cancellation.Token, counters, limits, state, static () => []);
        var statement = AwkStatement.Printf(AwkExpression.Literal("%s:%04d\n"),
            [AwkExpression.FieldIndex(AwkExpression.Number("1", 1)), AwkExpression.FieldIndex(AwkExpression.Number("2", 2))]);
        var program = new AwkProgram([], [new(null, [statement])], [], [], [],
            new Dictionary<AwkFunctionName, AwkUserFunction>());
        using var executor = new AwkRuntimeActionExecutor(program, null, state);
        var run = executor.ProcessRecordAsync(state, io, AwkText.FromSourceLiteralString("before 7")).AsTask();
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("before:0007\n", System.Text.Encoding.UTF8.GetString(host.Bytes.Span));
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("changed 999"));
            Assert.Equal("before:0007\n", System.Text.Encoding.UTF8.GetString(host.Bytes.Span));
            Assert.True(budget.Reserved > 0);
            if (cancel)
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.True(budget.Reserved > 0);
                Assert.Equal("before:0007\n", System.Text.Encoding.UTF8.GetString(host.Bytes.Span));
            }
            host.Release.TrySetResult(AwkExitCode.Success);
            if (!cancel) Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            host.Release.TrySetResult(AwkExitCode.Success);
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
        var invocation = AwkTestHarness.BuildInvocation("awk", "-v", "i=1", "-v", "j=2",
            "{printf \"" + format + "\"," + fields + "}");
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), invocation, CancellationToken.None);
        var host = new MockFileSystem();
        host.SetStandardInput("head 007\nother -3\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(2 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, limits, AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter,
            memoryBudget: budget);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
        return new(exit.Value, host.GetOutput(AwkFileDescriptor.StdOut), host.GetOutput(AwkFileDescriptor.StdErr),
            counters.ExecutionSteps, counters.HostCalls, counters.FieldsMaterialized, counters.FieldSplits);
    }

    private sealed class PendingHost : IAwkHost
    {
        private readonly MockFileSystem _files = new();
        public ReadOnlyMemory<byte> Bytes { get; private set; }
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<AwkExitCode> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            Bytes = bytes;
            Entered.TrySetResult(true);
            return Release.Task; // Deliberately ignore cancellation: bytes remain borrowed until actual completion.
        }
        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token) => _files.RunCommandsAsync(commands, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => _files.OpenFileAsync(path, mode, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => _files.ReadLinesAsync(descriptor, count, token);
        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => _files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) => _files.CreatePipeAsync(mode, token);
    }

    private readonly record struct Result(int Exit, string Output, string Error, long Steps, long Calls, long Fields, long Splits);
}
