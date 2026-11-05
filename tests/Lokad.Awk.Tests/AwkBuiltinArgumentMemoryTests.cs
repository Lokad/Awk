namespace Lokad.Awk.Tests;

public sealed partial class AwkBuiltinArgumentMemoryTests
{
    private const int Limit = 1 << 20;

    public static IEnumerable<object[]> PendingCalls()
    {
        foreach (var (name, count) in new[] { ("index", 2), ("substr", 3), ("tolower", 1),
            ("toupper", 1), ("sqrt", 1), ("atan2", 2), ("and", 3), ("sprintf", 4) })
        for (var index = 0; index < count; index++)
        foreach (var outcome in new[] { "success", "fault", "cancel" })
            yield return [name, index, outcome];
    }

    [Theory]
    [MemberData(nameof(PendingCalls))]
    public async Task NativeBuiltinArgumentsPreserveOrderAcrossActualHostCompletion(
        string name, int pendingIndex, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(observed: true, cancellation.Token);
        var state = memory.State;
        var (values, expected) = name switch
        {
            "index" => (new[] { "\"abcdef\"", "\"bc\"" }, "2"),
            "substr" => (new[] { "\"abcdef\"", "2", "3" }, "bcd"),
            "tolower" => (new[] { "\"ABC\"" }, "abc"),
            "toupper" => (new[] { "\"abc\"" }, "ABC"),
            "sqrt" => (new[] { "4" }, "2"),
            "atan2" => (new[] { "0", "1" }, "0"),
            "and" => (new[] { "7", "8", "9" }, "0"),
            "sprintf" => (new[] { "\"%d%d%d\"", "7", "8", "9" }, "789"),
            _ => throw new InvalidOperationException("Unknown test builtin.")
        };
        var arguments = string.Join(',', values.Select((value, index) =>
            (index == pendingIndex ? "delayed" : "arg") + "(" + index + "," + value + ")"));
        var region = Bind(state, "function arg(n,v){order=order n;return v}" +
            "function delayed(n,v){order=order n;getline ignored<\"/side\";return " +
            (outcome == "fault" ? "1/0" : "v") + "}" +
            "BEGIN{target=\"keep\";target=" + name + "(" + arguments + ")} ");
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
        var prefix = string.Concat(Enumerable.Range(0, pendingIndex + 1));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(prefix, state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
            Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
            var held = memory.Budget.Reserved;
            Assert.True(held > 0);
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(memory.Budget.Reserved >= held);
            release.TrySetResult();
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(string.Concat(Enumerable.Range(0, values.Length)),
                    state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
                Assert.Equal(expected, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
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
                Assert.Equal(prefix, state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
                Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
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
    [InlineData(false)] [InlineData(true)]
    public void NativeBuiltinStorageAdmissionPrecedesArgumentEffects(bool observed)
    {
        using var memory = State(observed, CancellationToken.None);
        var state = memory.State;
        var name = AwkVariableName.FromText("count");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        var parsed = AwkParser.Parse("BEGIN{result=int(++count)}");
        Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(parsed.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
        using var storage = new AwkRegionStorage(state);
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(0, state.ResolveVariableValue(name).AsNumber());
        Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
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

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host) => new(host, AwkPath.Root,
        AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None,
        state.Counters, state.Limits, state, static _ => []);

    private static CooperativeTestMemory State(bool observed, CancellationToken token)
    {

        var testMemory = new CooperativeTestMemory(Limit);
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, testMemory.Configure(options)));
    }
}
