namespace Lokad.Awk.Tests;

public sealed class AwkConcatStagingTests
{
    public static IEnumerable<object[]> CompletedCases()
    {
        foreach (var governed in new[] { false, true })
        foreach (var arity in new[] { 2, 5, 8, 9, 17 })
            yield return [governed, arity];
    }

    [Theory]
    [MemberData(nameof(CompletedCases))]
    public void NativeCompletedOperandsKeepOrderAndUtf8(bool governed, int arity)
    {
        using var memory = State(governed);
        var state = memory.State;
        var region = Bind(state, Source(arity, pendingIndex: -1, "success"));
        SetParts(state, arity, largePrefix: false);
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(Expected(arity, false), state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
        Assert.Equal(string.Concat(Enumerable.Range(0, arity)), state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    public static IEnumerable<object[]> PendingCases()
    {
        foreach (var governed in new[] { false, true })
        foreach (var (arity, pendingIndex) in new[] { (8, 0), (8, 3), (8, 7), (9, 3) })
        foreach (var outcome in new[] { "success", "fault", "cancel" })
            yield return [governed, arity, pendingIndex, outcome];
    }

    [Theory]
    [MemberData(nameof(PendingCases))]
    public async Task NativePrefixSurvivesSourceDeletionNestedConcatAndActualHostCompletion(
        bool governed, int arity, int pendingIndex, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(governed, cancellation.Token);
        var state = memory.State;
        var region = Bind(state, Source(arity, pendingIndex, outcome));
        SetParts(state, arity, largePrefix: true);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", "ready\n");
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
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(string.Concat(Enumerable.Range(0, pendingIndex + 1)),
                state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
            Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
            for (var index = 0; index < pendingIndex; index++)
                Assert.False(state.ContainsArrayValue(AwkArrayName.FromText("values"), AwkArrayKey.FromOrdinal(index)));
            Assert.Equal(string.Concat(Enumerable.Repeat("noise", 9)),
                state.ResolveVariableValue(AwkVariableName.FromText("scratch")).AsString());
            var held = state.MemoryAccounting.CurrentCommittedBytes;
            if (governed) Assert.True(held >= baseline);
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= held);
            release.TrySetResult();
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(Expected(arity, true), state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
                Assert.Equal(string.Concat(Enumerable.Range(0, arity)),
                    state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
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
                Assert.Equal(string.Concat(Enumerable.Range(0, pendingIndex + 1)),
                    state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
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

    [Theory]
    [InlineData(5)] [InlineData(9)]
    public async Task LateAdmissionFailureKeepsTargetAndEarlierEffectsWithoutRetry(int arity)
    {
        using var memory = State(true);
        var state = memory.State;
        var source = "function arg(n){effects++;if(n==" + (arity - 1) + ")print \"ready\";return part}" +
            "BEGIN{target=" + string.Join(' ', Enumerable.Range(0, arity).Select(index => "arg(" + index + ")")) + "}";
        var region = Bind(state, source);
        state.AssignVariableValue(AwkVariableName.FromText("target"), AwkValue.FromNumber(7));
        state.AssignVariableValue(AwkVariableName.FromText("part"), AwkValue.FromText(new string('x', 8192)));
        var host = new MockFileSystem { AfterAppend = () => memory.Budget.RefuseNextAdmissionMinimumGrowth = 1 };
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        try
        {
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => region.ExecuteAsync(state, io, output).AsTask());
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
            Assert.Equal(arity, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
            Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(1, memory.Budget.Refusals);
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData(2)] [InlineData(9)]
    public async Task StorageRefusalDoesNotStartArgumentsOrOpenInput(int arity)
    {
        using var memory = State(true);
        var state = memory.State;
        var region = Bind(state, Source(arity, 0, "success"));
        SetParts(state, arity, false);
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        using var pressure = state.MemoryAccounting.ReserveTransient((1 << 20) - state.MemoryAccounting.CurrentCommittedBytes);
        await Assert.ThrowsAsync<AwkRuntimeException>(() => region.ExecuteAsync(state, io, output).AsTask());
        Assert.False(state.HasScalar(AwkVariableName.FromText("order")));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static string Source(int arity, int pendingIndex, string outcome) =>
        "function arg(n){order=order n;return values[n]}" +
        "function delayed(n,v,i){order=order n;scratch=" + string.Join(' ', Enumerable.Repeat("\"noise\"", 9)) +
        ";for(i=0;i<n;i++)delete values[i];getline v<\"/side\";return " +
        (outcome == "fault" ? "1/0" : "values[n]") + "}" +
        "BEGIN{target=\"keep\";target=" + string.Join(' ', Enumerable.Range(0, arity)
            .Select(index => (index == pendingIndex ? "delayed" : "arg") + "(" + index + ")")) + "}";

    private static string Part(int index, bool largePrefix) => index == 0 && largePrefix ? new string('x', 32_768) : "é" + index;
    private static string Expected(int arity, bool largePrefix) => string.Concat(Enumerable.Range(0, arity).Select(index => Part(index, largePrefix)));
    private static void SetParts(AwkRuntimeState state, int arity, bool largePrefix)
    {
        for (var index = 0; index < arity; index++)
            state.SetArrayAwkValue(AwkArrayName.FromText("values"), AwkArrayKey.FromOrdinal(index), AwkValue.FromText(Part(index, largePrefix)));
    }
    private static AwkRegion Bind(AwkRuntimeState state, string source)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        return Assert.Single(program.BeginActions);
    }
    private static CooperativeTestMemory State(bool governed, CancellationToken token = default)
    {
        var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token,
            memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
                AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault()))));
    }
    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host) => new(host, AwkPath.Root,
        AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None,
        state.Counters, state.Limits, state, static _ => []);
}
