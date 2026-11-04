namespace Lokad.Awk.Tests;

public sealed partial class AwkArrayCompoundUpdateTests
{
    private static readonly AwkArrayKey Key = new("k");

    public static IEnumerable<object[]> ArithmeticCalls()
    {
        foreach (var (operation, expected) in new[] { ("+=", 10d), ("-=", 4d), ("*=", 21d),
            ("/=", 7d / 3), ("%=", 1d), ("^=", 343d) })
        foreach (var suspend in new[] { false, true })
        foreach (var governed in new[] { false, true })
        foreach (var outcome in new[] { "success", "fault", "cancel" })
            yield return [operation, expected, suspend, governed, outcome];
        foreach (var suspend in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return ["/=", 0d, suspend, governed, "division"];
    }

    [Theory]
    [MemberData(nameof(ArithmeticCalls))]
    public async Task NativeUpdatesCaptureBeforeRightEffectsAndPreserveFailures(
        string operation, double expected, bool suspend, bool governed, string outcome)
    {
        var source = "function rhs(v){effects++;" + (outcome == "success" ? "values[\"k\"]=100;" : "") +
            "getline v<\"/rhs\";return " + (outcome == "fault" ? "1/0" : "v") +
            "}BEGIN{result=(values[\"k\"]" + operation + "rhs())}";
        await using var execution = new NativeArrayExecution(source, governed,
            new InputGate("/rhs", outcome == "division" ? "0\n" : "3\n", suspend));
        var state = execution.State;
        var array = AwkArrayName.FromText("values");
        var original = outcome == "cancel" ? "7" : "007";
        state.SetArrayAwkValue(array, Key, outcome == "cancel" ? AwkValue.FromNumber(7) : AwkValue.FromText("007"));
        state.AssignVariableValue(AwkVariableName.FromText("result"), AwkValue.FromText("keep"));
        execution.CancelOnReadCompletion = outcome == "cancel";
        var run = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (suspend)
        {
            Assert.False(run.IsCompleted);
            Assert.Equal(outcome == "success" ? "100" : original, state.GetArrayAwkValue(array, Key).AsString());
            if (outcome == "cancel") execution.Cancel();
            Assert.False(run.IsCompleted);
            execution.Inputs[0].Release.TrySetResult();
        }
        if (outcome == "success")
        {
            Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            var result = state.ResolveVariableValue(AwkVariableName.FromText("result"));
            Assert.True(result.IsNumberValue);
            Assert.Equal(expected, result.AsNumber());
            Assert.Equal(expected, state.GetArrayAwkValue(array, Key).AsNumber());
        }
        else
        {
            if (outcome == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else
            {
                var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(outcome == "division" ? "awk: division by zero attempted in `/='\n" :
                    "awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            }
            Assert.Equal(original, state.GetArrayAwkValue(array, Key).AsString());
            Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("result")).AsString());
        }
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
        Assert.Equal(1, execution.Reads);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task NativeKeyCompletesBeforeRightMutationAndCommit(bool pendingKey, bool pendingRight)
    {
        var source = "function key(v){order=order \"K\";getline v<\"/key\";return v}" +
            "function rhs(v){order=order \"R\";values[\"k\"]=100;getline v<\"/rhs\";return v}" +
            "BEGIN{result=(values[key()]+=rhs())}";
        await using var execution = new NativeArrayExecution(source, true,
            new InputGate("/key", "k\n", pendingKey), new InputGate("/rhs", "3\n", pendingRight));
        var state = execution.State;
        var array = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(array, Key, AwkValue.FromNumber(7));
        var run = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (pendingKey)
        {
            Assert.False(execution.Inputs[1].Entered.Task.IsCompleted);
            Assert.Equal(7, state.GetArrayAwkValue(array, Key).AsNumber());
            execution.Inputs[0].Release.TrySetResult();
        }
        await execution.Inputs[1].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (pendingRight)
        {
            Assert.False(run.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(array, Key).AsNumber());
            execution.Inputs[1].Release.TrySetResult();
        }
        Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(10, state.GetArrayAwkValue(array, Key).AsNumber());
        Assert.Equal("KR", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
        Assert.Equal(2, execution.Reads);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task NativeKeyFailureStopsRightEffects(bool suspend, bool cancel)
    {
        var source = "function key(v){keys++;getline v<\"/key\";return 1/0}" +
            "function rhs(){rights++;return 3}BEGIN{result=(values[key()]+=rhs())}";
        await using var execution = new NativeArrayExecution(source, true, new InputGate("/key", "k\n", suspend));
        var state = execution.State;
        var array = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(array, Key, AwkValue.FromNumber(7));
        execution.CancelOnReadCompletion = cancel;
        var run = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (suspend)
        {
            Assert.False(run.IsCompleted);
            if (cancel) execution.Cancel();
            execution.Inputs[0].Release.TrySetResult();
        }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
        else
        {
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        }
        Assert.Equal(7, state.GetArrayAwkValue(array, Key).AsNumber());
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("keys")).AsNumber());
        Assert.False(state.HasScalar(AwkVariableName.FromText("rights")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
        Assert.Equal(1, execution.Reads);
    }

    [Fact]
    public async Task NativeMissingAdmissionStopsRightEffectsAndPublication()
    {
        await using var execution = new NativeArrayExecution(
            "function rhs(){effects++;return 3}BEGIN{result=(values[\"missing\"]+=rhs())}", true);
        var state = execution.State;
        var array = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(array, Key, AwkValue.FromNumber(7));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(execution.Region);
        execution.Memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var failure = Assert.Throws<AwkRuntimeException>(() => execution.Region.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(array, Key).AsNumber());
        Assert.False(state.ContainsArrayValue(array, new AwkArrayKey("missing")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("effects")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
        Assert.Equal(0, execution.Memory.Budget.GrowthAttemptsAfterRefusal);
    }

    private sealed class InputGate(string path, string content, bool suspend)
    {
        internal string Path => path;
        internal string Content => content;
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = CreateRelease(suspend);
        private static TaskCompletionSource CreateRelease(bool pending)
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!pending) release.SetResult();
            return release;
        }
    }

    // Actual prepared execution and MockFileSystem reads only; no expression evaluator.
    private sealed class NativeArrayExecution : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly AwkIo _io;
        private readonly AwkOutputSink _output;
        private Task<AwkStatementFlow>? _run;
        internal NativeArrayExecution(string source, bool governed, params InputGate[] inputs)
        {
            Inputs = inputs;
            Memory = new CooperativeTestMemory(8 << 20, enabled: governed);
            State = Memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
                _cancellation.Token, Memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                    new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault())))).State;
            var binding = AwkBinder.Bind(AwkParser.Parse(source));
            Assert.True(binding.Success);
            var program = binding.Program.Program;
            State.InitializeGlobalNames(program.GlobalNames);
            State.InitializeGlobalReadSlots(program.GlobalReadCount);
            State.InitializeFunctions(program.Functions);
            Region = Assert.Single(program.BeginActions);
            foreach (var gate in inputs) Host.AddFile(gate.Path, gate.Content);
            Host.ReadAsyncOverride = Read;
            _io = new(Host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, CancellationToken.None, State.Counters, State.Limits, State, static _ => []);
            _output = new(State.MemoryAccounting, State.Counters);
        }
        internal CooperativeTestMemory Memory { get; }
        internal AwkRuntimeState State { get; }
        internal AwkRegion Region { get; }
        internal MockFileSystem Host { get; } = new();
        internal InputGate[] Inputs { get; }
        internal int Reads { get; private set; }
        internal bool CancelOnReadCompletion { get; set; }
        internal void Cancel() => _cancellation.Cancel();
        internal Task<AwkStatementFlow> Start()
        {
            Assert.Null(_run);
            return _run = Region.ExecuteAsync(State, _io, _output).AsTask();
        }
        private async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            var index = Reads++;
            Assert.InRange(index, 0, Inputs.Length - 1);
            var gate = Inputs[index];
            Assert.True(gate.Entered.TrySetResult());
            await gate.Release.Task.ConfigureAwait(false);
            if (CancelOnReadCompletion) _cancellation.Cancel();
            Host.ReadAsyncOverride = null;
            try { return await Host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false); }
            finally { Host.ReadAsyncOverride = Read; }
        }
        public async ValueTask DisposeAsync()
        {
            foreach (var gate in Inputs) gate.Release.TrySetResult();
            try
            {
                if (_run is { } run)
                {
                    try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
                    catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
                    catch (AwkRuntimeException) { }
                }
            }
            finally
            {
                await _io.CloseForCleanupAsync();
                _output.Dispose();
                Memory.Dispose();
                _cancellation.Dispose();
                Assert.Equal(0, Host.OpenFileCount);
                Assert.Equal(0, Memory.Budget.Reserved);
            }
        }
    }

}
