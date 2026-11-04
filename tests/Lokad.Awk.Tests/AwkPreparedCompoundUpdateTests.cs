namespace Lokad.Awk.Tests;

public sealed class AwkPreparedCompoundUpdateTests
{
    private const int Limit = 8 << 20;
    private static readonly AwkVariableName Value = AwkVariableName.FromText("value");
    private static readonly AwkVariableName Result = AwkVariableName.FromText("result");

    public static IEnumerable<object[]> Calls()
    {
        foreach (var (operation, expected) in new[] { ("=", 3d), ("+=", 10d), ("-=", 4d),
            ("*=", 21d), ("/=", 7d / 3), ("%=", 1d), ("^=", 343d) })
        foreach (var suspend in new[] { false, true })
        foreach (var governed in new[] { false, true })
        foreach (var outcome in new[] { "success", "fault", "cancel" })
            yield return [operation, expected, suspend, governed, outcome];
        foreach (var suspend in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return ["/=", 0d, suspend, governed, "division"];
    }

    [Theory]
    [MemberData(nameof(Calls))]
    public async Task NativeAssignmentAndCapturedUpdatesPreserveValuesAndHostLifetimes(
        string operation, double expected, bool suspend, bool governed, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(governed, cancellation.Token);
        var state = memory.State;
        var mutate = outcome == "success";
        var region = Bind(state, "function rhs(v){effects++;" +
            (mutate ? "SYMTAB[\"value\"]=100;" : "") +
            "getline v<\"/side\";return " + (outcome == "fault" ? "1/0" : "v") +
            "}BEGIN{result=(value" + operation + "rhs())}");
        // Numeric cancellation fixtures permit inspection without starting a new text parse
        // after cancellation; successful and faulted string cases retain the original spelling.
        var original = outcome == "cancel" ? "7" : "007";
        state.AssignVariableValue(Value, outcome == "cancel" ? AwkValue.FromNumber(7) : AwkValue.FromText("007"));
        state.AssignVariableValue(Result, AwkValue.FromText("keep"));
        var host = new MockFileSystem();
        host.AddFile("/side", outcome == "division" ? "0\n" : "3\n");
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
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (suspend)
            {
                Assert.False(run.IsCompleted);
                Assert.Equal(mutate ? "100" : original, state.ResolveVariableValue(Value).AsString());
                Assert.Equal("keep", state.ResolveVariableValue(Result).AsString());
                var held = state.MemoryAccounting.CurrentCommittedBytes;
                if (outcome == "cancel") cancellation.Cancel();
                Assert.False(run.IsCompleted);
                Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= held);
                release.TrySetResult();
            }
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(expected, state.ResolveVariableValue(Value).AsNumber());
                Assert.Equal(expected, state.ResolveVariableValue(Result).AsNumber());
                if (operation != "=") Assert.True(state.ResolveVariableValue(Result).IsNumberValue);
            }
            else
            {
                if (outcome == "cancel")
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                else
                {
                    var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                    Assert.Equal(outcome == "division" ? "awk: division by zero attempted in `/='\n" :
                        "awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                }
                Assert.Equal(original, state.ResolveVariableValue(Value).AsString());
                Assert.Equal("keep", state.ResolveVariableValue(Result).AsString());
            }
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
            Assert.Equal(1, reads);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; }
            catch (AwkRuntimeException) when (outcome is "fault" or "division") { }
            catch (OperationCanceledException) when (outcome == "cancel") { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData("=", "success")] [InlineData("+=", "success")]
    [InlineData("=", "cancel")] [InlineData("+=", "cancel")]
    [InlineData("=", "refuse")] [InlineData("+=", "refuse")]
    public async Task MissingTargetBindsAtItsReadOrPublicationAndSurvivesLateFailure(string operation, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        Assert.True(state.TryResolveArrayArgument(Value, out _));
        Assert.False(state.HasScalar(Value));
        var region = Bind(state, "function rhs(v){getline v<\"/side\";return v}" +
            "BEGIN{result=(value" + operation + "rhs())}");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", "3\n");
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        var pressure = AwkMemoryLease.Disabled;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(operation == "+=", state.HasScalar(Value));
            if (outcome == "refuse")
                pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(run.IsCompleted);
            release.TrySetResult();
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(3, state.ResolveVariableValue(Value).AsNumber());
            }
            else
            {
                if (outcome == "cancel")
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                else
                {
                    var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                    Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                }
                Assert.Equal(operation == "+=", state.HasScalar(Value));
                Assert.False(state.HasScalar(Result));
            }
            Assert.False(state.IsArray(new AwkArrayName(Value.Identifier)));
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            Assert.False(state.IsInsideFunction);
        }
        finally
        {
            release.TrySetResult();
            try { await run; }
            catch (AwkRuntimeException) when (outcome == "refuse") { }
            catch (OperationCanceledException) when (outcome == "cancel") { }
            pressure.Dispose();
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData("value+=rhs()")] [InlineData("++value")] [InlineData("value++")]
    public void ExistingArrayTypeFailureStopsRightEffectsAndPreservesEntries(string expression)
    {
        using var memory = State(true);
        var state = memory.State;
        var region = Bind(state, "function rhs(){effects++;return 3}BEGIN{result=(" + expression + ")}");
        var array = new AwkArrayName(Value.Identifier);
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(array, key, "kept");
        using var storage = new AwkRegionStorage(state);
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: attempt to use array `value' in a scalar context\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.False(state.HasScalar(Value));
        Assert.False(state.HasScalar(Result));
        Assert.False(state.HasScalar(AwkVariableName.FromText("effects")));
        Assert.Equal("kept", state.GetArrayAwkValue(array, key).AsString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("=")] [InlineData("+=")]
    public void StorageRefusalPrecedesRightEffectsAndTargetBinding(string operation)
    {
        using var memory = State(true);
        var state = memory.State;
        var region = Bind(state, "function rhs(){effects++;return 3}BEGIN{result=(value" + operation + "rhs())}");
        using var storage = new AwkRegionStorage(state);
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.False(state.HasScalar(Value));
        Assert.False(state.HasScalar(Result));
        Assert.False(state.HasScalar(AwkVariableName.FromText("effects")));
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        var memory = new CooperativeTestMemory(Limit, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token,
            memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
                AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault()))));
    }
    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host) => new(host, AwkPath.Root,
        AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None,
        state.Counters, state.Limits, state, static _ => []);
}
