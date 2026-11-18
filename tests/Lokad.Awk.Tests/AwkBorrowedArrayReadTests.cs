namespace Lokad.Awk.Tests;

public sealed class AwkBorrowedArrayReadTests
{
    [Theory]
    [InlineData(false, false, false, 0)]
    [InlineData(true, false, false, 0)]
    [InlineData(false, true, false, 0)]
    [InlineData(true, true, false, 0)]
    [InlineData(false, false, true, 0)]
    [InlineData(true, false, true, 0)]
    [InlineData(false, true, true, 0)]
    [InlineData(true, true, true, 0)]
    [InlineData(false, false, false, 1)]
    [InlineData(true, false, false, 1)]
    [InlineData(false, true, false, 1)]
    [InlineData(true, true, false, 1)]
    [InlineData(false, false, true, 1)]
    [InlineData(true, false, true, 1)]
    [InlineData(false, true, true, 1)]
    [InlineData(true, true, true, 1)]
    [InlineData(false, false, false, 2)]
    [InlineData(true, false, false, 2)]
    [InlineData(false, true, false, 2)]
    [InlineData(true, true, false, 2)]
    [InlineData(false, false, true, 2)]
    [InlineData(true, false, true, 2)]
    [InlineData(false, true, true, 2)]
    [InlineData(true, true, true, 2)]
    public void OrdinaryMissOwnsUntypedKeyWithoutFieldClassificationOrRepeatedLookup(
        bool governed, bool composite, bool bound, int operation)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkArrayName.FromText("a");
        var binding = AwkArrayBinding.Global(0);
        state.Arrays.SetAwkValue(name, new AwkArrayKey("seed"), AwkValue.FromNumber(7), bound ? binding : default);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("0009 tail"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("0009\x1ctail"u8);

        AwkArrayUpdateTarget target = default;

        if (operation == 2)
            target = composite ? state.Arrays.PrepareUpdate(name, builder, binding) : state.Arrays.PrepareFieldUpdate(name, new(1), binding);
        else if (operation == 1)
            Assert.Equal(0, composite ? state.Arrays.GetNumber(name, builder, binding) : state.Arrays.GetFieldNumber(name, new(1), binding));
        else
        {
            var value = composite ? state.Arrays.GetAwkValue(name, builder, binding) : state.Arrays.GetFieldValue(name, new(1), binding);
            Assert.Equal(AwkRuntimeValueKind.Untyped, value.RuntimeKind);
            Assert.Equal("", value.AsString());
        }

        Assert.True(state.TryGetBoundGlobalArray(0, out var array));
        Assert.NotNull(array);
        var key = new AwkArrayKey(composite ? "0009\x1ctail" : "0009");
        Assert.True(array[key].IsUntypedValue);
        Assert.Equal(2, array.Count);
        builder.Clear();
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("changed record"));
        if (operation == 2)
        {
            Assert.Equal(0, target.Previous);
            Assert.Equal(key, target.Key);
            state.Arrays.DeleteValue(name, key);
            state.Arrays.CommitUpdate(name, in target, 9);
            Assert.Equal(9, state.Arrays.GetAwkValue(name, key).AsNumber());
        }
        else Assert.Equal("", state.Arrays.GetAwkValue(name, key).AsString());
        Assert.False(state.Arrays.ContainsValue(name, new AwkArrayKey("9")));
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 1)]
    [InlineData(false, false, 2)]
    [InlineData(true, false, 2)]
    [InlineData(false, true, 2)]
    [InlineData(true, true, 2)]
    public void ColdOrdinaryMissRejectsOrCancelsBeforeKeyAllocationAndBinding(bool composite, bool cancel, int operation)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters, cancellation.Token)).State;
        state.InitializeGlobalReadSlots(2);
        var name = AwkArrayName.FromText("a");
        var seed = new AwkArrayKey("seed");
        state.Arrays.SetAwkValue(name, seed, AwkValue.FromNumber(7), AwkArrayBinding.Global(1));
        Assert.True(state.TryGetBoundGlobalArray(1, out var array));
        Assert.NotNull(array);
        var binding = AwkArrayBinding.Global(0);
        var keyText = new string('x', 8192);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(keyText + " tail"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append(AwkUtf8Codec.Encode(keyText + "\x1ctail").Span);
        var allocations = counters[AwkCounter.TextAllocations];
        if (cancel) memory.Budget.OnGrowth = bytes => { if (bytes >= 8192) cancellation.Cancel(); };
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 8192;

        void Read()
        {
            if (operation == 2)
            {
                if (composite) state.Arrays.PrepareUpdate(name, builder, binding);
                else state.Arrays.PrepareFieldUpdate(name, new(1), binding);
            }
            else if (operation == 1)
            {
                if (composite) state.Arrays.GetNumber(name, builder, binding);
                else state.Arrays.GetFieldNumber(name, new(1), binding);
            }
            else if (composite) state.Arrays.GetAwkValue(name, builder, binding);
            else state.Arrays.GetFieldValue(name, new(1), binding);
        }

        try
        {
            if (cancel) Assert.ThrowsAny<OperationCanceledException>(Read);
            else
            {
                var error = Assert.Throws<AwkRuntimeException>(Read);
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            }
        }
        finally { memory.Budget.OnGrowth = null; }
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.False(state.TryGetBoundGlobalArray(0, out _));
        Assert.Single(array);
        Assert.True(array[seed].TryGetNumber(out var number));
        Assert.Equal(7, number);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void AssignmentMissOwnsKeyWithoutFieldClassificationOrRepeatedLookup(bool governed, bool composite, bool bound)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkArrayName.FromText("a");
        var binding = AwkArrayBinding.Global(0);
        state.Arrays.SetAwkValue(name, new AwkArrayKey("seed"), AwkValue.FromNumber(7), bound ? binding : default);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("café part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("café\x1cpart"u8);
        var replacement = AwkTestHarness.NumericInput("0009");

        if (composite) state.Arrays.SetBuilderValue(name, builder, replacement, binding);
        else state.Arrays.SetFieldValue(name, new(1), replacement, binding);

        // One lookup and one insertion hash.

        Assert.True(state.TryGetBoundGlobalArray(0, out _)); // Bind a cold name only after successful insertion.
        builder.Clear();
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("changed record"));
        var stored = state.Arrays.GetAwkValue(name, new AwkArrayKey(composite ? "café\x1cpart" : "café"));
        Assert.Equal("0009", stored.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, stored.RuntimeKind);
        Assert.Equal(9, stored.AsNumber());
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void BoundAssignmentMissPreflightsKeyAndValueBeforeAllocationOrPublication(bool composite, bool cancel, bool largeKey)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters, cancellation.Token)).State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkArrayName.FromText("a");
        var binding = AwkArrayBinding.Global(0);
        var seed = new AwkArrayKey("seed");
        state.Arrays.SetAwkValue(name, seed, AwkValue.FromNumber(7), binding);
        var keyText = largeKey ? new string('x', 8192) : "new";
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(keyText + " part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append(AwkUtf8Codec.Encode(keyText + "\x1cpart").Span);
        var replacement = largeKey ? AwkValue.FromNumber(9) : AwkValue.FromText(new string('x', 8192));
        var allocations = counters[AwkCounter.TextAllocations];
        if (cancel) memory.Budget.OnGrowth = bytes => { if (bytes >= 8192) cancellation.Cancel(); };
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 8192;

        void Assign()
        {
            if (composite) state.Arrays.SetBuilderValue(name, builder, replacement, binding);
            else state.Arrays.SetFieldValue(name, new(1), replacement, binding);
        }
        try
        {
            if (cancel) Assert.ThrowsAny<OperationCanceledException>(Assign);
            else
            {
                var error = Assert.Throws<AwkRuntimeException>(Assign);
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            }
        }
        finally { memory.Budget.OnGrowth = null; }
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        // Inspect the retained owner without a new cancellable language operation.
        Assert.True(state.TryGetBoundGlobalArray(0, out var array));
        Assert.NotNull(array);
        Assert.Single(array);
        Assert.True(array[seed].TryGetNumber(out var seedNumber));
        Assert.Equal(7, seedNumber);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadOnlySpecialAssignmentKeepsItsDiagnosticWithoutCopyingFieldKey(bool governed)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        state.InitializeFunctions(new Dictionary<AwkFunctionName, AwkUserFunction>());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("length"));
        _ = state.FieldCount;
        var allocations = counters[AwkCounter.TextAllocations];
        if (governed) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        var error = Assert.Throws<AwkRuntimeException>(() => state.Arrays.SetFieldValue(
            AwkArrayName.FromText("FUNCTAB"), new(1), AwkValue.FromNumber(9), default));

        Assert.Equal("awk: cannot modify special array `FUNCTAB'\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AssignmentConsumesLocatedCellWithoutRehashingOrCopyingKey(bool governed, bool composite)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey(composite ? "café\x1cpart" : "café");
        state.Arrays.SetAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("café part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("café\x1cpart"u8);
        var replacement = AwkTestHarness.NumericInput("0009");

        var allocations = counters[AwkCounter.TextAllocations];

        if (composite) state.Arrays.SetBuilderValue(name, builder, replacement, default);
        else state.Arrays.SetFieldValue(name, new(1), replacement, default);

        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        builder.Clear();
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("different record"));
        var stored = state.Arrays.GetAwkValue(name, key);
        Assert.Equal("0009", stored.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, stored.RuntimeKind);
        Assert.Equal(9, stored.AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AssignmentRefusalLeavesLocatedCellUnchanged(bool composite)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => State(memory.Owner, AwkRuntimeCounters.CreateEnabled())).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey(composite ? "hit\x1cpart" : "hit");
        state.Arrays.SetAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("hit\x1cpart"u8);
        var replacement = AwkValue.FromText(new string('x', 4096));
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (composite) state.Arrays.SetBuilderValue(name, builder, replacement, default);
            else state.Arrays.SetFieldValue(name, new(1), replacement, default);
        });

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.Arrays.GetAwkValue(name, key).AsNumber());
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericBuilderReadConsumesLocatedValueWithoutCopyingAKey(bool governed)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        var name = AwkArrayName.FromText("a");
        state.Arrays.SetAwkValue(name, new AwkArrayKey("café\x1cpart"), AwkTestHarness.NumericInput("007"));
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("café\x1cpart"u8);
        var allocations = counters[AwkCounter.TextAllocations];

        if (governed) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        Assert.Equal(7, state.Arrays.GetNumber(name, builder, default));

        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);

        Assert.Equal("007", state.Arrays.GetAwkValue(name, new AwkArrayKey("café\x1cpart")).AsString());
        builder.Clear();
        builder.Append("other"u8);
        if (governed)
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => state.Arrays.GetNumber(name, builder, default));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.False(state.Arrays.ContainsValue(name, new AwkArrayKey("other")));
        }
        else
        {
            Assert.Equal(0, state.Arrays.GetNumber(name, builder, default));
            builder.Clear();
            builder.Append("after"u8);
            Assert.True(state.Arrays.ContainsValue(name, new AwkArrayKey("other")));
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
        state.Arrays.SetAwkValue(name, hit, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.LiveBytes;
        var allocations = counters[AwkCounter.TextAllocations];
        Assert.Equal(7, state.Arrays.GetFieldValue(name, new(1)).AsNumber());
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        var bytes = "new tail"u8.ToArray();
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        Assert.Equal("", state.Arrays.GetFieldValue(name, new(1)).AsString());
        Assert.True(state.Arrays.ContainsValue(name, new AwkArrayKey("new")));
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        Assert.True(original.Span.StartsWith("xxx"u8));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("other record"));
        Assert.Equal(7, state.Arrays.GetAwkValue(name, hit).AsNumber());
        Assert.True(state.Arrays.ContainsValue(name, new AwkArrayKey("new")));
        Assert.False(state.Arrays.ContainsValue(name, new AwkArrayKey("xxx")));
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
        state.Arrays.SetAwkValue(name, key, AwkValue.FromNumber(7));
        var record = AwkText.FromSourceLiteralString("hit missing");
        state.SetCurrentRecord(record);
        Assert.Equal(7, state.Arrays.GetFieldValue(name, new(1)).AsNumber());
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.Arrays.GetFieldValue(name, new(2)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.Arrays.GetAwkValue(name, key).AsNumber());
        Assert.False(state.Arrays.ContainsValue(name, new AwkArrayKey("missing")));
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
    }

    [Fact]
    public void MissingBorrowedLookupDoesNotCreateAnArrayOrCommitStorage()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForAccounting = new CooperativeTestMemory(1024).InitializeRuntime(counters);
        var accounting = memoryForAccounting.Accounting;
        var variables = memoryForAccounting.State.Variables;
        var name = AwkArrayName.FromText("a");
        var retained = accounting.LiveBytes;
        Assert.False(variables.TryGetExistingArrayValue(name, "missing"u8, out _));
        Assert.False(variables.ContainsArray(name));
        Assert.Equal(retained, accounting.LiveBytes);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task NativePendingIndexSelectsCurrentRecordAndCapturesReadOrMembership(bool governed, bool membership, bool fail)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var state = memory.Initialize(() => State(memory.Owner, AwkRuntimeCounters.CreateEnabled())).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("hit");
        var source = "function selected(v){effects++;getline v<\"/side\";return " +
            (fail ? "1/0" : "v") + "}BEGIN{result=" +
            (membership ? "($(selected()) in a)" : "a[$(selected())]") + "}";
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource(source));
        Assert.True(binding.Success);
        var program = binding.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.Arrays.SetAwkValue(name, key, AwkValue.FromText("before"));
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
            AwkFileDescriptor.StdErr, CancellationToken.None, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting);
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
                Assert.Equal(0, state.Counters[AwkCounter.FieldsPrepared]);
            }
            else
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                var result = state.ResolveVariableValue(AwkVariableName.FromText("result"));
                state.Arrays.SetAwkValue(name, key, AwkValue.FromText("after"));
                if (membership) Assert.Equal(1, result.AsNumber());
                else Assert.Equal("before", result.AsString());
            }
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
            Assert.Equal(1, reads);
            Assert.False(state.Arrays.ContainsValue(name, new AwkArrayKey("miss")));
            Assert.True(state.TryGetExistingArrayCount(name, out var count));
            Assert.Equal(1, count);
            Assert.False(state.IsInsideFunction);
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
        state.Arrays.SetAwkValue(name, hit, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        _ = state.FieldCount;
        var allocations = counters[AwkCounter.TextAllocations];
        var committed = state.MemoryAccounting.LiveBytes;
        Assert.Equal(hit, state.Arrays.ResolveFieldKey(name, new(1)));
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        var bytes = "new tail"u8.ToArray();
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        var missing = state.Arrays.ResolveFieldKey(name, new(1));
        Assert.False(state.Arrays.ContainsValue(name, missing));
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        Assert.True(original.Span.StartsWith("xxx"u8));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("other record"));
        state.Arrays.SetAwkValue(name, missing, AwkValue.FromNumber(3));
        Assert.Equal(3, state.Arrays.GetAwkValue(name, new AwkArrayKey("new")).AsNumber());
        Assert.False(state.Arrays.ContainsValue(name, new AwkArrayKey("xxx")));
    }

    [Fact]
    public async Task NativeTargetHitUpdatesAndMissingAdmissionPrecedesRightEffects()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => State(memory.Owner, AwkRuntimeCounters.CreateEnabled())).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("hit");
        state.Arrays.SetAwkValue(name, key, AwkValue.FromNumber(7));
        var record = AwkText.FromSourceLiteralString("hit missing");
        state.SetCurrentRecord(record);
        var parsed = AwkTestHarness.ParseSource("BEGIN{++a[$1]}BEGIN{a[$2]+=++count}");
        Assert.True(AwkTestHarness.TryPrepareRule(parsed.Syntax.Rules[0], AwkLoweringScope.Global, -1, out var first));
        Assert.True(AwkTestHarness.TryPrepareRule(parsed.Syntax.Rules[1], AwkLoweringScope.Global, -1, out var missing));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        Assert.Equal(AwkStatementFlow.Continue, await first.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(8, state.Arrays.GetAwkValue(name, key).AsNumber());
        var count = AwkVariableName.FromText("count");
        state.AssignVariableValue(count, AwkValue.FromNumber(0));
        storage.Initialize(missing);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await missing.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(0, state.ResolveVariableValue(count).AsNumber());
        Assert.Equal(8, state.Arrays.GetAwkValue(name, key).AsNumber());
        Assert.False(state.Arrays.ContainsValue(name, new AwkArrayKey("missing")));
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, AwkRuntimeCounters counters,
        CancellationToken token = default)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits, regexEngine: AwkUtf8RegexEngine.Instance)
        { MemoryOwner = owner };
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, options);
    }

}
