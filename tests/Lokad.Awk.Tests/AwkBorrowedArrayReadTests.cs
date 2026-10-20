namespace Lokad.Awk.Tests;

public sealed class AwkBorrowedArrayReadTests
{
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
        Assert.False(variables.TryGetExistingArrayValue(name, "missing"u8, out _));
        Assert.False(variables.ContainsArray(name));
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task PendingIndexSelectsItsRecordBeforeBorrowingAndReturnsAValueSnapshot()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromText("before"));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("miss old"));
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expression = AwkExpression.ArrayElement(name, AwkArrayIndex.Simple(AwkExpression.FieldIndex(new PendingIndex(completion.Task))));
        var pending = expression.EvaluateAsync(new AwkEvaluationContext(state));
        Assert.False(pending.IsCompleted);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit after"));
        completion.SetResult(AwkValue.FromNumber(1));
        var value = await pending;
        state.SetArrayAwkValue(name, key, AwkValue.FromText("after"));
        Assert.Equal("before", value.AsString());
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

    [Theory]
    [InlineData(2)]
    public async Task TargetHitUpdatesAndMissingAdmissionPrecedesRightEffects(int mode)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None);
        var state = State(owner, AwkRuntimeCounters.CreateEnabled());
        var name = AwkArrayName.FromText("a");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var record = AwkText.FromSourceLiteralString("hit missing");
        state.SetCurrentRecord(record);
        var context = new AwkEvaluationContext(state);
        AwkReference Target(int index) => AwkReference.ArrayElement(name, AwkArrayIndex.Simple(
            AwkExpression.FieldIndex(AwkExpression.Number(index.ToString(System.Globalization.CultureInfo.InvariantCulture), index))));
        Assert.Equal(8, (await Target(1).ApplyIncrementAsync(context)).AsNumber());
        var count = AwkVariableName.FromText("count");
        state.AssignVariableValue(count, AwkValue.FromNumber(0));
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await Target(2).ApplyCompoundAssignmentAsync(
            context, AwkExpression.PreIncrement(AwkReference.Scalar(count)), AwkCompoundAssignmentOperation.Add));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, state.ResolveVariableValue(count).AsNumber());
        Assert.Equal(8, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("missing")));
        Assert.Same(record, state.CurrentRecordByteText);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, AwkRuntimeCounters counters)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits)
        { MemoryOwner = owner };
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }

    private sealed record ForwardingIndex(AwkArrayIndex Inner) : AwkArrayIndex
    {
        public override ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context) => Inner.ResolveKeyAsync(context);
    }

    private sealed class PendingIndex(Task<AwkValue> result) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => new(result);
    }
}
