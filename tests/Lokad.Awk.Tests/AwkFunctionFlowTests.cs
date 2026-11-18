using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkFunctionFlowTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PendingRecordOutputKeepsAndRestoresTheEnclosingPhase(bool governed, bool cancel)
    {
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        await using (var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, cancellation.Token))
        {
            const string source = "function f(){print \"body\";next}{f();print \"bad\"}";
            var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource(source));
            Assert.True(binding.Success);
            var program = binding.Program;
            var counters = AwkRuntimeCounters.CreateEnabled();
            var limits = AwkRuntimeLimits.CreateDefault();
            var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits)
            { MemoryOwner = owner };
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
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent("row\n"u8.ToArray(),
                AwkReadCompletion.EndOfFile, static _ => { }));
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, cancellation.Token, limits, state, static _ => []);
            using var executor = new AwkRuntimeActionExecutor(program, state);
            var input = AwkTestHarness.BindRegionInput(source, executor, state, io, cancellation.Token);
            using (state.EnterActionPhase(AwkActionPhase.BeginFile))
            {
                Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
                var pending = input.ProcessRemainingInputAsync();
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
                    else await pending;
                    Assert.False(state.IsInsideFunction);
                    Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
                    state.ValidateFunctionFlow(AwkStatementFlow.NextFile);
                }
                finally
                {
                    release.TrySetResult();
                    try { await pending; }
                    catch (OperationCanceledException) when (cancel) { }
                    await input.CloseRemainingDescriptorsForCleanupAsync();
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
        var source = "function leaf(a){a[1]=\"local\";" + keyword + "} " +
            "function outer(v){return leaf()+v} {x=outer(1);print \"bad\"}";
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source,
            new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));
        Assert.True(binding.Success);
        var program = binding.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault()
            .WithMaxFunctionCallDepth(2);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None, limits, state, static _ => []);
        using var executor = new AwkRuntimeActionExecutor(program, state);
        state.RecordNumber = 0;

        await ProcessAsync();
        var committed = state.MemoryAccounting.LiveBytes;
        for (var index = 1; index < 256; index++)
        {
            await ProcessAsync();
            Assert.False(state.IsInsideFunction);
            Assert.False(state.TryGetExecutionContext(out _));
            Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        }
        Assert.Equal(skipFile ? 256 : 512, state.RecordNumber);
        var error = Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
        Assert.Equal("awk: next used outside input action\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        await io.CloseAsync();

        async Task ProcessAsync()
        {
            host.SetStandardInput("row\nextra\n");
            var input = AwkTestHarness.BindRegionInput(source, executor, state, io);
            try { await input.ProcessRemainingInputAsync(); }
            finally { await input.CloseRemainingDescriptorsForCleanupAsync(); }
        }
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
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(2), regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None, options);
        Assert.Equal(0, exit.Value);
        Assert.True(reads >= 2);
        Assert.Equal("2\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }
}
