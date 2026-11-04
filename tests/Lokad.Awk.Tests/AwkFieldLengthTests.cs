namespace Lokad.Awk.Tests;

public sealed class AwkFieldLengthTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsciiLengthReadsAvoidFieldCopiesAndRetainedCacheGrowth(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var counters = state.Counters;
        Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
        state.SetCurrentRecord(AwkText.FromString("0007 alpha beta"));
        Assert.Equal(governed, memory.Budget.Reserved > 0);
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        var values = counters.ValuesCreated;
        for (var index = 0; index < 100; index++)
            Assert.Equal(4, state.CountFieldStringUnits(1));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(values, counters.ValuesCreated);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal("0007", state.GetFieldValue(1).AsString());
        Assert.Equal(allocations + 1, counters.AwkTextAllocations);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonAsciiReadsRetainTheExistingCachedValue(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var counters = state.Counters;
        Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
        state.SetCurrentRecord(AwkText.FromString("café 😀"));
        Assert.Equal(governed, memory.Budget.Reserved > 0);
        Assert.Equal(4, state.CountFieldStringUnits(1));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        for (var index = 0; index < 100; index++)
            Assert.Equal(4, state.CountFieldStringUnits(1));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, "success")] [InlineData(true, "success")]
    [InlineData(false, "fault")] [InlineData(true, "fault")]
    [InlineData(false, "cancel")] [InlineData(true, "cancel")]
    public async Task NativeIndexCompletionSelectsCurrentRecordAndFailureDoesNotSplit(bool suspend, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("before"));
        state.AssignVariableValue(AwkVariableName.FromText("target"), AwkValue.FromText("keep"));
        var binding = AwkBinder.Bind(AwkParser.Parse("function field(v){getline v<\"/side\";return " +
            (outcome == "fault" ? "1/0" : "v") + "}BEGIN{target=length($(field()))}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        host.AddFile("/side", "1\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            if (suspend) await release.Task.ConfigureAwait(false);
            if (outcome == "cancel") cancellation.Cancel();
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, state.Counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (suspend)
            {
                Assert.False(run.IsCompleted);
                Assert.Equal(0, state.Counters.FieldSplits);
                state.SetCurrentRecord(AwkText.FromSourceLiteralString("after next"));
                if (outcome == "cancel") cancellation.Cancel();
                Assert.False(run.IsCompleted);
                Assert.True(memory.Budget.Reserved > 0);
                release.TrySetResult();
            }
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(suspend ? 5 : 6, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
            }
            else
            {
                if (outcome == "cancel")
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                else
                {
                    var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                    Assert.Equal("awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                }
                Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
                Assert.Equal(0, state.Counters.FieldSplits);
            }
            Assert.Equal(1, reads);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; }
            catch (AwkRuntimeException) when (outcome == "fault") { }
            catch (OperationCanceledException) when (outcome == "cancel") { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    private static CooperativeTestMemory State(bool governed, CancellationToken token = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, token, memory.Configure(options)));
    }
}
