namespace Lokad.Awk.Tests;

public sealed class AwkBorrowedArrayReadTests
{
    [Theory]
    [InlineData(false, false, false, 0)] [InlineData(true, false, false, 0)]
    [InlineData(false, true, false, 0)] [InlineData(true, true, false, 0)]
    [InlineData(false, false, true, 0)] [InlineData(true, false, true, 0)]
    [InlineData(false, true, true, 0)] [InlineData(true, true, true, 0)]
    [InlineData(false, false, false, 1)] [InlineData(true, false, false, 1)]
    [InlineData(false, true, false, 1)] [InlineData(true, true, false, 1)]
    [InlineData(false, false, true, 1)] [InlineData(true, false, true, 1)]
    [InlineData(false, true, true, 1)] [InlineData(true, true, true, 1)]
    [InlineData(false, false, false, 2)] [InlineData(true, false, false, 2)]
    [InlineData(false, true, false, 2)] [InlineData(true, true, false, 2)]
    [InlineData(false, false, true, 2)] [InlineData(true, false, true, 2)]
    [InlineData(false, true, true, 2)] [InlineData(true, true, true, 2)]
    public void OrdinaryMissOwnsUntypedKeyWithoutFieldClassificationOrRepeatedLookup(
        bool governed, bool composite, bool bound, int operation)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkArrayName.FromText("a");
        var binding = AwkArrayBinding.Global(0);
        state.SetArrayAwkValue(name, new AwkArrayKey("seed"), AwkValue.FromNumber(7), bound ? binding : default);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("0009 tail"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("0009\x1ctail"u8);
        var borrowedHashes = counters.ArrayBorrowedHashCalls;
        var ownedHashes = counters.ArrayOwnedHashCalls;
        var classifications = counters.TextValueClassifications;
        var fieldCaches = counters.FieldValueCacheBufferAllocations;
        AwkArrayUpdateTarget target = default;

        if (operation == 2)
            target = composite ? state.PrepareArrayUpdate(name, builder, binding) : state.PrepareArrayFieldUpdate(name, 1, binding);
        else if (operation == 1)
            Assert.Equal(0, composite ? state.GetArrayNumber(name, builder, binding) : state.GetArrayFieldNumber(name, 1, binding));
        else
        {
            var value = composite ? state.GetArrayAwkValue(name, builder, binding) : state.GetArrayFieldValue(name, 1, binding);
            Assert.Equal(AwkRuntimeValueKind.Untyped, value.RuntimeKind);
            Assert.Equal("", value.AsString());
        }

        Assert.Equal(borrowedHashes + 1, counters.ArrayBorrowedHashCalls);
        Assert.Equal(ownedHashes + 1, counters.ArrayOwnedHashCalls);
        Assert.Equal(classifications, counters.TextValueClassifications);
        Assert.Equal(fieldCaches, counters.FieldValueCacheBufferAllocations);
        Assert.True(state.TryGetBoundGlobalArray(0, out var array));
        Assert.NotNull(array);
        var key = new AwkArrayKey(composite ? "0009\x1ctail" : "0009");
        Assert.True(array.Slots[key].IsUntyped);
        Assert.Equal(2, array.Slots.Count);
        builder.Clear();
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("changed record"));
        if (operation == 2)
        {
            Assert.Equal(0, target.Previous);
            Assert.Equal(key, target.Key);
            state.DeleteArrayValue(name, key);
            state.CommitArrayUpdate(name, in target, 9);
            Assert.Equal(9, state.GetArrayAwkValue(name, key).AsNumber());
        }
        else Assert.Equal("", state.GetArrayAwkValue(name, key).AsString());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("9")));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, 0)] [InlineData(true, false, 0)]
    [InlineData(false, true, 0)] [InlineData(true, true, 0)]
    [InlineData(false, false, 1)] [InlineData(true, false, 1)]
    [InlineData(false, true, 1)] [InlineData(true, true, 1)]
    [InlineData(false, false, 2)] [InlineData(true, false, 2)]
    [InlineData(false, true, 2)] [InlineData(true, true, 2)]
    public void ColdOrdinaryMissRejectsOrCancelsBeforeKeyAllocationAndBinding(bool composite, bool cancel, int operation)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters, cancellation.Token)).State;
        state.InitializeGlobalReadSlots(2);
        var name = AwkArrayName.FromText("a");
        var seed = new AwkArrayKey("seed");
        state.SetArrayAwkValue(name, seed, AwkValue.FromNumber(7), AwkArrayBinding.Global(1));
        Assert.True(state.TryGetBoundGlobalArray(1, out var array));
        Assert.NotNull(array);
        var binding = AwkArrayBinding.Global(0);
        var keyText = new string('x', 8192);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(keyText + " tail"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append(AwkUtf8Codec.Encode(keyText + "\x1ctail").Span);
        var allocations = counters.AwkTextAllocations;
        if (cancel) memory.Budget.OnGrowth = bytes => { if (bytes >= 8192) cancellation.Cancel(); };
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 8192;

        void Read()
        {
            if (operation == 2)
            {
                if (composite) state.PrepareArrayUpdate(name, builder, binding);
                else state.PrepareArrayFieldUpdate(name, 1, binding);
            }
            else if (operation == 1)
            {
                if (composite) state.GetArrayNumber(name, builder, binding);
                else state.GetArrayFieldNumber(name, 1, binding);
            }
            else if (composite) state.GetArrayAwkValue(name, builder, binding);
            else state.GetArrayFieldValue(name, 1, binding);
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
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.False(state.TryGetBoundGlobalArray(0, out _));
        Assert.Single(array.Slots);
        Assert.True(array.Slots[seed].Slot.TryGetNumber(out var number));
        Assert.Equal(7, number);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    public void AssignmentMissOwnsKeyWithoutFieldClassificationOrRepeatedLookup(bool governed, bool composite, bool bound)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkArrayName.FromText("a");
        var binding = AwkArrayBinding.Global(0);
        state.SetArrayAwkValue(name, new AwkArrayKey("seed"), AwkValue.FromNumber(7), bound ? binding : default);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("café part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("café\x1cpart"u8);
        var replacement = AwkValue.FromNumericString("0009");
        var borrowedHashes = counters.ArrayBorrowedHashCalls;
        var ownedHashes = counters.ArrayOwnedHashCalls;
        var classifications = counters.TextValueClassifications;
        var fieldCaches = counters.FieldValueCacheBufferAllocations;

        if (composite) state.SetArrayBuilderValue(name, builder, replacement, binding);
        else state.SetArrayFieldValue(name, 1, replacement, binding);

        Assert.Equal(borrowedHashes + 1, counters.ArrayBorrowedHashCalls);
        Assert.Equal(ownedHashes + 1, counters.ArrayOwnedHashCalls); // Insertion still hashes its owned key.
        Assert.Equal(classifications, counters.TextValueClassifications);
        Assert.Equal(fieldCaches, counters.FieldValueCacheBufferAllocations);
        Assert.True(state.TryGetBoundGlobalArray(0, out _)); // Bind a cold name only after successful insertion.
        builder.Clear();
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("changed record"));
        var stored = state.GetArrayAwkValue(name, new AwkArrayKey(composite ? "café\x1cpart" : "café"));
        Assert.Equal("0009", stored.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, stored.RuntimeKind);
        Assert.Equal(9, stored.AsNumber());
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
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
        state.SetArrayAwkValue(name, seed, AwkValue.FromNumber(7), binding);
        var keyText = largeKey ? new string('x', 8192) : "new";
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(keyText + " part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append(AwkUtf8Codec.Encode(keyText + "\x1cpart").Span);
        var replacement = largeKey ? AwkValue.FromNumber(9) : AwkValue.FromText(new string('x', 8192));
        var allocations = counters.AwkTextAllocations;
        if (cancel) memory.Budget.OnGrowth = bytes => { if (bytes >= 8192) cancellation.Cancel(); };
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 8192;

        void Assign()
        {
            if (composite) state.SetArrayBuilderValue(name, builder, replacement, binding);
            else state.SetArrayFieldValue(name, 1, replacement, binding);
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
        Assert.Equal(allocations, counters.AwkTextAllocations);
        // Inspect the retained owner without a new cancellable language operation.
        Assert.True(state.TryGetBoundGlobalArray(0, out var array));
        Assert.NotNull(array);
        Assert.Single(array.Slots);
        Assert.True(array.Slots[seed].Slot.TryGetNumber(out var seedNumber));
        Assert.Equal(7, seedNumber);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReadOnlySpecialAssignmentKeepsItsDiagnosticWithoutCopyingFieldKey(bool governed)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        state.InitializeFunctions(new Dictionary<AwkFunctionName, AwkUserFunction>());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("length"));
        _ = state.FieldCount;
        var allocations = counters.AwkTextAllocations;
        if (governed) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        var error = Assert.Throws<AwkRuntimeException>(() => state.SetArrayFieldValue(
            AwkArrayName.FromText("FUNCTAB"), 1, AwkValue.FromNumber(9), default));

        Assert.Equal("awk: cannot modify special array `FUNCTAB'\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void AssignmentConsumesLocatedCellWithoutRehashingOrCopyingKey(bool governed, bool composite)
    {
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = memory.Initialize(() => State(memory.Owner, counters)).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey(composite ? "café\x1cpart" : "café");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("café part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("café\x1cpart"u8);
        var replacement = AwkValue.FromNumericString("0009");
        var borrowedHashes = counters.ArrayBorrowedHashCalls;
        var ownedHashes = counters.ArrayOwnedHashCalls;
        var allocations = counters.AwkTextAllocations;

        if (composite) state.SetArrayBuilderValue(name, builder, replacement, default);
        else state.SetArrayFieldValue(name, 1, replacement, default);

        Assert.Equal(borrowedHashes + 1, counters.ArrayBorrowedHashCalls);
        Assert.Equal(ownedHashes, counters.ArrayOwnedHashCalls);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        builder.Clear();
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("different record"));
        var stored = state.GetArrayAwkValue(name, key);
        Assert.Equal("0009", stored.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, stored.RuntimeKind);
        Assert.Equal(9, stored.AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AssignmentRefusalLeavesLocatedCellUnchanged(bool composite)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => State(memory.Owner, AwkRuntimeCounters.CreateEnabled())).State;
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey(composite ? "hit\x1cpart" : "hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit part"));
        _ = state.FieldCount;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("hit\x1cpart"u8);
        var replacement = AwkValue.FromText(new string('x', 4096));
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (composite) state.SetArrayBuilderValue(name, builder, replacement, default);
            else state.SetArrayFieldValue(name, 1, replacement, default);
        });

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        Assert.Equal("", state.GetArrayFieldValue(name, 1).AsString());
        Assert.True(state.ContainsArrayValue(name, new AwkArrayKey("new")));
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        Assert.True(original.Span.StartsWith("xxx"u8));
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
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void MissingBorrowedLookupDoesNotCreateAnArrayOrCommitStorage()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForAccounting = new CooperativeTestMemory(1024, counters);
        var accounting = memoryForAccounting.Accounting;
        var variables = new AwkVariables(accounting, counters,  static (_, _) => { });
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
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        var missing = state.GetArrayFieldKey(name, 1);
        Assert.False(state.ContainsArrayValue(name, missing));
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        AwkTestHarness.PublishBufferedRecord(state, "xxx tail"u8);
        Assert.True(original.Span.StartsWith("xxx"u8));
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
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
