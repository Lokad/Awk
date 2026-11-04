namespace Lokad.Awk.Tests;

public sealed class AwkStringRegionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CompleteStringActionPreservesStateAndByteNativeProcessing(bool governed)
    {
        var program = Bind("BEGIN{$0=\"id:beta:tail\";s=substr($0,4,4);total=length(s)+index($0,\"beta\")+length(tolower(s))}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(program, counters, governed);
        var state = memory.State;
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal("beta", state.ResolveVariableValue(AwkVariableName.FromText("s")).AsString());
        Assert.Equal(12, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task PendingStringArgumentsPublishOnceAfterActualRead(bool governed, bool length, bool cancel)
    {
        var expression = length ? "length(text_value())" : "substr(text_value(),2,2)";
        var program = Bind("function text_value(v){reads++;getline v<\"/side\";return v}" +
            "BEGIN{target=\"keep\";target=" + expression + "}");
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(program, counters, governed, cancellation.Token);
        var state = memory.State;
        var host = new MockFileSystem();
        host.AddFile("/side", length ? "café\n" : "abcd\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("reads")).AsNumber());
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(run.IsCompleted);
                if (governed) Assert.True(memory.Budget.Reserved > 0);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            var target = state.ResolveVariableValue(AwkVariableName.FromText("target"));
            if (cancel) Assert.Equal("keep", target.AsString());
            else if (length) Assert.Equal(4, target.AsNumber());
            else Assert.Equal("bc", target.AsString());
            Assert.Equal(1, reads);
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("reads")).AsNumber());
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    private static AwkProgram Bind(string source)
    {
        var result = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(result.Success);
        return result.Program.Program;
    }

    private static CooperativeTestMemory State(AwkProgram program, AwkRuntimeCounters counters, bool governed,
        CancellationToken token = default)
    {
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        return memory.Initialize(() =>
        {
            var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
                AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
            var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, memory.Configure(options));
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            state.InitializeFunctions(program.Functions);
            return state;
        });
    }

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host) => new(host, AwkPath.Root,
        AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None,
        state.Counters, state.Limits, state, static _ => []);
}
