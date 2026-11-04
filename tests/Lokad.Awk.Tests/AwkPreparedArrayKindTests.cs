namespace Lokad.Awk.Tests;

public sealed class AwkPreparedArrayKindTests
{
    private static readonly AwkArrayName Array = AwkArrayName.FromText("a");
    private static readonly AwkVariableName Variable = AwkVariableName.FromText("a");
    private static readonly AwkArrayKey Key = new("k");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuccessfulAccessesBindOnlyArrayKinds(bool slots)
    {
        var state = State(slots: slots);
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), AwkArrayBinding.Global(0));
        Assert.Equal(0, state.Counters.BoundGlobalArrayAccesses);
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.False(state.TryResolvePreparedGlobalNumber(0, out _));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)).AsNumber());
        Assert.True(state.ContainsArrayValue(Array, Key, AwkArrayBinding.Global(0)));
        state.SetCurrentRecord(AwkText.FromString("k tail"));
        Assert.Equal(7, state.GetArrayFieldValue(Array, 1, AwkArrayBinding.Global(0)).AsNumber());
        Assert.True(state.ContainsArrayField(Array, 1, AwkArrayBinding.Global(0)));
        Assert.Equal(slots ? 4 : 0, state.Counters.BoundGlobalArrayAccesses);
    }

    [Fact]
    public void BindingUsesTheCurrentReplacementDictionary()
    {
        var state = State();
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), AwkArrayBinding.Global(0));
        state.ClearArray(Array);
        Assert.False(state.ContainsArrayValue(Array, Key, AwkArrayBinding.Global(0)));
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(9));
        Assert.Equal(9, state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)).AsNumber());
        state.SplitIntoArray(AwkText.FromString("new words"), Array, AwkFieldSeparatorSpec.Default,
            AwkOptionalIdentifier.None);
        Assert.False(state.ContainsArrayValue(Array, Key, AwkArrayBinding.Global(0)));
        Assert.Equal("new", state.GetArrayAwkValue(Array, AwkArrayKey.FromOrdinal(1), AwkArrayBinding.Global(0)).AsString());
    }

    [Fact]
    public void FailedScalarUseDoesNotPublishAnArrayBinding()
    {
        var state = State();
        state.AssignVariableValue(Variable, AwkValue.FromNumber(3));
        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), AwkArrayBinding.Global(0)));
        Assert.Equal("awk: attempt to use scalar `a' as an array\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.False(state.TryGetBoundGlobalArray(0, out _));
        Assert.Equal(3, state.ResolveVariableValue(Variable).AsNumber());
    }

    [Fact]
    public void BoundArrayCannotBecomeAScalarOrANumericSlot()
    {
        var state = State();
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), AwkArrayBinding.Global(0));
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.False(state.TryResolvePreparedGlobalNumber(0, out _));
        Assert.True(state.TryResolveArrayArgument(Variable, out _));
        var read = Assert.Throws<AwkRuntimeException>(() => state.ResolvePreparedGlobalValue(Variable, 0));
        var write = Assert.Throws<AwkRuntimeException>(() =>
            state.AssignPreparedGlobalValue(Variable, 0, AwkValue.FromNumber(9)));
        const string expected = "awk: attempt to use array `a' in a scalar context\n";
        Assert.Equal(expected, AwkUtf8Codec.Decode(read.MessageBytes));
        Assert.Equal(expected, AwkUtf8Codec.Decode(write.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)).AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveFunctionFramesKeepOriginalNameResolution(bool alias)
    {
        var state = State();
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), AwkArrayBinding.Global(0));
        var other = AwkArrayName.FromText("b");
        state.SetArrayAwkValue(other, Key, AwkValue.FromNumber(9));
        var argument = alias ? AwkFunctionArgument.Array(other) : AwkFunctionArgument.Scalar(AwkValue.FromNumber(3));
        state.PushFunctionFrame(new([Variable]), [argument]);
        try
        {
            if (alias)
                Assert.Equal(9, state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)).AsNumber());
            else
            {
                var error = Assert.Throws<AwkRuntimeException>(() => state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)));
                Assert.Equal("awk: attempt to use scalar `a' as an array\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal(3, state.ResolvePreparedGlobalValue(Variable, 0).AsNumber());
                state.AssignPreparedGlobalValue(Variable, 0, AwkValue.FromNumber(4));
                Assert.Equal(4, state.ResolvePreparedGlobalValue(Variable, 0).AsNumber());
            }
            Assert.Equal(0, state.Counters.BoundGlobalArrayAccesses);
        }
        finally { state.PopFunctionFrame(); }
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)).AsNumber());
    }

    [Fact]
    public void BoundWritesKeepEntryPreflights()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(Options()))).State;
        state.InitializeGlobalReadSlots(1);
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), AwkArrayBinding.Global(0));
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.SetArrayAwkValue(Array, new("new"), AwkValue.FromNumber(9), AwkArrayBinding.Global(0)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)).AsNumber());
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void UnboundFunctionAccessCannotPublishGlobalArrayKind()
    {
        var state = State();
        state.AssignVariableValue(Variable, AwkValue.FromNumber(3));
        var other = AwkArrayName.FromText("b");
        state.SetArrayAwkValue(other, Key, AwkValue.FromNumber(9));
        state.PushFunctionFrame(new([Variable]), [AwkFunctionArgument.Array(other)]);
        try { Assert.Equal(9, state.GetArrayAwkValue(Array, Key, AwkArrayBinding.Global(0)).AsNumber()); }
        finally { state.PopFunctionFrame(); }
        Assert.False(state.TryGetBoundGlobalArray(0, out _));
        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7), AwkArrayBinding.Global(0)));
        Assert.Equal("awk: attempt to use scalar `a' as an array\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(3, state.ResolveVariableValue(Variable).AsNumber());
    }

    public static IEnumerable<object[]> PendingAccesses()
    {
        foreach (var mode in new[] { 0, 1, 2 })
        foreach (var outcome in new[] { "success", "fault", "cancel" })
        foreach (var governed in new[] { false, true })
            yield return [mode, outcome, governed];
    }

    [Theory]
    [MemberData(nameof(PendingAccesses))]
    public async Task NativePendingKeysPublishEntriesOnlyAfterSuccessAndReadCurrentOwners(
        int mode, string outcome, bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            cancellation.Token, memory.Configure(Options()))).State;
        var body = mode == 0 ? "a[selected()]=7" : mode == 1 ? "result=+a[selected()]" : "result=+a[$(selected())]";
        var region = Bind(state, "function selected(v){effects++;getline v<\"/side\";return " +
            (outcome == "fault" ? "1/0" : "v") + "}BEGIN{" + body + "}");
        if (mode != 0) state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("old"));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", mode == 2 ? "1\n" : "k\n");
        var reads = 0;
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, state.Counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.False(state.HasScalar(Variable));
            Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
            if (mode == 0)
                Assert.False(state.TryGetExistingArrayCount(Array, out var count) && count != 0);
            else
            {
                state.ClearArray(Array);
                state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(9));
                state.SetCurrentRecord(AwkText.FromSourceLiteralString("k"));
            }
            var held = memory.Budget.Reserved;
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(memory.Budget.Reserved >= held);
            release.TrySetResult();
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                if (mode == 0) Assert.Equal(7, state.GetArrayAwkValue(Array, Key).AsNumber());
                else Assert.Equal(9, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
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
                Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
                if (mode == 0) Assert.False(state.ContainsArrayValue(Array, Key));
            }
            if (mode != 0)
            {
                Assert.Equal(9, state.GetArrayAwkValue(Array, Key).AsNumber());
                Assert.False(state.ContainsArrayValue(Array, new("old")));
            }
            Assert.False(state.HasScalar(Variable));
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
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

    [Fact]
    public void NativeNumericReadRefusalDoesNotInsertTheMissingElement()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(Options()))).State;
        var region = Bind(state, "BEGIN{result=+a[\"missing\"]}");
        state.SetArrayAwkValue(Array, Key, AwkValue.FromNumber(7));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.False(state.ContainsArrayValue(Array, new("missing")));
        Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
        Assert.Equal(7, state.GetArrayAwkValue(Array, Key).AsNumber());
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
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

    [Fact]
    public async Task ParsedArrayArgumentsAndSpecialArraysKeepTheirOriginalPaths()
    {
        const string source = "function size(x){return length(x)} BEGIN{a[1]=7;print size(a),length(a);ARGV[1]=\"x\";print ARGV[1]}";
        var host = new MockFileSystem();
        var options = Options();
        var program = await AwkExecutor.CompileAsync(host, AwkTestHarness.BuildInvocation("awk", [source]), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var result = await program.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, result.Value);
        Assert.Equal("1 1\nx\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, options.Counters.BoundGlobalArrayAccesses);
    }

    private static AwkRuntimeOptions Options(AwkRuntimeLimits? limits = null) =>
        new(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
            (limits ?? AwkRuntimeLimits.CreateDefault()), regexEngine: AwkUtf8RegexEngine.Instance);

    private static AwkRuntimeState State(bool slots = true, AwkRuntimeLimits? limits = null)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, Options(limits));
        if (slots) state.InitializeGlobalReadSlots(1);
        return state;
    }

}
