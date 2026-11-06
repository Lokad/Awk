namespace Lokad.Awk.Tests;

public sealed class AwkBorrowedArrayReadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericBuilderReadConsumesLocatedValueWithoutCopyingAKey(bool governed)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        var name = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(name, new AwkArrayKey("café\x1cpart"), AwkValue.FromNumericString("007"));
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("café\x1cpart"u8);
        var allocations = counters.AwkTextAllocations;
        var borrowedHashes = counters.ArrayBorrowedHashCalls;
        var ownedHashes = counters.ArrayOwnedHashCalls;
        if (governed) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        Assert.Equal(7, state.GetArrayNumber(name, builder, default));

        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(borrowedHashes + 1, counters.ArrayBorrowedHashCalls);
        Assert.Equal(ownedHashes, counters.ArrayOwnedHashCalls);
        Assert.Equal("007", state.GetArrayAwkValue(name, new AwkArrayKey("café\x1cpart")).AsString());
        builder.Clear();
        builder.Append("other"u8);
        if (governed)
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => state.GetArrayNumber(name, builder, default));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("other")));
        }
        else
        {
            Assert.Equal(0, state.GetArrayNumber(name, builder, default));
            builder.Clear();
            builder.Append("after"u8);
            Assert.True(state.ContainsArrayValue(name, new AwkArrayKey("other")));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task HitsAvoidCopiesAndMissesRetainOwnedKeysAfterRecordReuse(int mode)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(owner, counters);
        var name = AwkArrayName.FromText("a");
        var hit = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, hit, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        Assert.Equal(7, state.GetArrayFieldValue(name, 1).AsNumber());
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        var bytes = "new tail"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        Assert.Equal("", state.GetArrayFieldValue(name, 1).AsString());
        Assert.True(state.ContainsArrayValue(name, new AwkArrayKey("new")));
        Array.Fill(bytes, (byte)'x');
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("other record"));
        Assert.Equal(7, state.GetArrayAwkValue(name, hit).AsNumber());
        Assert.True(state.ContainsArrayValue(name, new AwkArrayKey("new")));
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("xxx")));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(2)]
    public async Task HitsSucceedAndMissingReadRefusalLeavesArrayUnchanged(int mode)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(owner, counters);
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var record = AwkText.FromSourceLiteralString("hit missing");
        state.SetCurrentRecord(record);
        Assert.Equal(7, state.GetArrayFieldValue(name, 1).AsNumber());
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.GetArrayFieldValue(name, 2));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("missing")));
        Assert.Same(record, state.CurrentRecordByteText);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void MissingBorrowedLookupDoesNotCreateAnArrayOrCommitStorage()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForAccounting = new CooperativeTestMemory(1024, counters);
        var accounting = memoryForAccounting.Accounting;
        var variables = new AwkVariables(accounting, counters, AwkValue.FromText, static text => AwkValue.FromNumericString(text), static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), static (_, _) => { });
        var name = AwkArrayName.FromText("a");
        var retained = accounting.CurrentCommittedBytes;
        Assert.False(variables.TryGetExistingArrayValue(name, "missing"u8, out _));
        Assert.False(variables.ContainsArray(name));
        Assert.Equal(retained, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    public async Task NativePendingIndexSelectsCurrentRecordAndCapturesReadOrMembership(bool governed, bool membership, bool fail)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var state = memory.Initialize(() => State(memory.Owner, AwkRuntimeCounters.CreateEnabled())).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("hit");
        var source = "function selected(v){effects++;getline v<\"/side\";return " +
            (fail ? "1/0" : "v") + "}BEGIN{result=" +
            (membership ? "($(selected()) in a)" : "a[$(selected())]") + "}";
        var binding = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.SetArrayAwkValue(name, key, AwkValue.FromText("before"));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("miss old"));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", "1\n");
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
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit after"));
            release.TrySetResult();
            if (fail)
            {
                var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal("awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
                Assert.Equal(0, state.Counters.FieldsMaterialized);
            }
            else
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                var result = state.ResolveVariableValue(AwkVariableName.FromText("result"));
                state.SetArrayAwkValue(name, key, AwkValue.FromText("after"));
                if (membership) Assert.Equal(1, result.AsNumber());
                else Assert.Equal("before", result.AsString());
            }
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
            Assert.Equal(1, reads);
            Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("miss")));
            Assert.True(state.TryGetExistingArrayCount(name, out var count));
            Assert.Equal(1, count);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (AwkRuntimeException) when (fail) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task TargetKeysReuseHitsAndCopyMissesBeforeRecordReuse(int mode)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(owner, counters);
        var name = AwkArrayName.FromText("a");
        var hit = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, hit, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        _ = state.FieldCount;
        var allocations = counters.AwkTextAllocations;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.Equal(hit, state.GetArrayFieldKey(name, 1));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        var bytes = "new tail"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        var missing = state.GetArrayFieldKey(name, 1);
        Assert.False(state.ContainsArrayValue(name, missing));
        Array.Fill(bytes, (byte)'x');
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("other record"));
        state.SetArrayAwkValue(name, missing, AwkValue.FromNumber(3));
        Assert.Equal(3, state.GetArrayAwkValue(name, new AwkArrayKey("new")).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("xxx")));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void NativeTargetHitUpdatesAndMissingAdmissionPrecedesRightEffects()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => State(memory.Owner, AwkRuntimeCounters.CreateEnabled())).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var record = AwkText.FromSourceLiteralString("hit missing");
        state.SetCurrentRecord(record);
        var parsed = AwkParser.Parse("BEGIN{++a[$1]}BEGIN{a[$2]+=++count}");
        Assert.True(AwkTestHarness.TryPrepareRule(parsed.Syntax.Rules[0], AwkLoweringScope.Global, -1, out var first));
        Assert.True(AwkTestHarness.TryPrepareRule(parsed.Syntax.Rules[1], AwkLoweringScope.Global, -1, out var missing));
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, first.Execute(state, storage));
        Assert.Equal(8, state.GetArrayAwkValue(name, key).AsNumber());
        var count = AwkVariableName.FromText("count");
        state.AssignVariableValue(count, AwkValue.FromNumber(0));
        storage.Initialize(missing);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var failure = Assert.Throws<AwkRuntimeException>(() => missing.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(0, state.ResolveVariableValue(count).AsNumber());
        Assert.Equal(8, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("missing")));
        Assert.Same(record, state.CurrentRecordByteText);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, AwkRuntimeCounters counters)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits, regexEngine: AwkUtf8RegexEngine.Instance)
        { MemoryOwner = owner };
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }

}
