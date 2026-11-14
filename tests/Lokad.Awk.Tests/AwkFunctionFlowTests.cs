using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkFunctionFlowTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task PendingRecordOutputKeepsAndRestoresTheEnclosingPhase(bool governed, bool cancel)
    {
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        await using (var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, cancellation.Token))
        {
            var binding = AwkBinder.Bind(AwkParser.Parse("function f(){print \"body\";next}{f();print \"bad\"}"));
            Assert.True(binding.Success);
            var program = binding.Program.Program;
            var counters = AwkRuntimeCounters.CreateEnabled();
            var limits = AwkRuntimeLimits.CreateDefault();
            var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits) { MemoryOwner = owner };
            using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token, options);
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            state.InitializeFunctions(program.Functions);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var host = new TestOperationVisitor([])
            {
                AppendOverride = async (descriptor, bytes, _) =>
                {
                    Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                    Assert.True(bytes.Span.SequenceEqual("body\n"u8));
                    state.ValidateFunctionFlow(AwkStatementFlow.NextRecord);
                    entered.TrySetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual("body\n"u8));
                    return AwkAppendResult.Accepted();
                }
            };
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, cancellation.Token, counters, limits, state, static _ => []);
            using var executor = new AwkRecordTestExecution(program, state);
            using (state.EnterActionPhase(AwkActionPhase.BeginFile))
            {
                Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
                var record = AwkInputRecordResult.Record(AwkText.FromString("row"),
                    AwkRecordTerminator.NewLine, AwkRecordFieldSplittingMode.Ordinary);
                var pending = executor.ProcessRecordAsync(state, io, record).AsTask();
                try
                {
                    await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    Assert.False(pending.IsCompleted);
                    state.ValidateFunctionFlow(AwkStatementFlow.NextRecord);
                    if (governed) Assert.True(budget.Reserved > 0);
                    if (cancel) cancellation.Cancel();
                    Assert.False(pending.IsCompleted);
                    state.ValidateFunctionFlow(AwkStatementFlow.NextRecord);
                    release.TrySetResult();
                    if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
                    else Assert.Equal(AwkStatementFlow.Continue, await pending);
                    Assert.False(state.IsInsideFunction);
                    Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
                    state.ValidateFunctionFlow(AwkStatementFlow.NextFile);
                }
                finally
                {
                    release.TrySetResult();
                    try { await pending; }
                    catch (OperationCanceledException) when (cancel) { }
                    await io.CloseForCleanupAsync();
                }
            }
            Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextFile));
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedFlowUnwindsFunctionLeases(bool skipFile)
    {
        var keyword = skipFile ? "nextfile" : "next";
        var binding = AwkBinder.Bind(AwkParser.Parse("function leaf(a){a[1]=\"local\";" + keyword + "} " +
            "function outer(v){return leaf()+v} {x=outer(1);print \"bad\"}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault()
            .WithMaxFunctionCallDepth(2);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None, counters, limits, state, static _ => []);
        using var executor = new AwkRecordTestExecution(program, state);
        var record = AwkInputRecordResult.Record(AwkText.FromString("row"), AwkRecordTerminator.NewLine, AwkRecordFieldSplittingMode.Ordinary);
        var expectedFlow = skipFile ? AwkStatementFlow.NextFile : AwkStatementFlow.Continue;

        Assert.Equal(expectedFlow, await executor.ProcessRecordAsync(state, io, record));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        for (var index = 1; index < 256; index++)
        {
            Assert.Equal(expectedFlow, await executor.ProcessRecordAsync(state, io, record));
            Assert.False(state.IsInsideFunction);
            Assert.False(state.TryGetExecutionContext(out _));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        }
        var error = Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
        Assert.Equal("awk: next used outside input action\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        await io.CloseAsync();
    }

    [Fact]
    public async Task AsynchronousReadUnwindsArgumentsAndClosesDescriptors()
    {
        const string source = "function skip(){getline value < \"/side\";next} " +
            "function consume(a,b){print \"bad\"} {consume(skip(),9);print \"bad\"} END{print NR}";
        var host = new MockFileSystem();
        host.AddFile("/side", "side\n");
        host.SetStandardInput("a\nb\n");
        var reads = 0;
        host.ReadAsyncOverride = ReadAsync;
        async ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            reads++;
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, count, token); }
            finally { host.ReadAsyncOverride = ReadAsync; }
        }
        var compiled = await AwkExecutor.CompileAsync(host, AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(2), regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, exit.Value);
        Assert.True(reads >= 2);
        Assert.Equal("2\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }
}
