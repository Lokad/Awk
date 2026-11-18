namespace Lokad.Awk.Tests;

public sealed class AwkScalarMemoryModeTests
{
    [Theory]
    [InlineData("123.5", true)]
    [InlineData("0012", true)]
    [InlineData("12tail", true)]
    [InlineData("0012", false)]
    public void ScalarCachePromotionUsesFundedCapacityAndKeepsOriginsAndEarlierCopies(string spelling, bool input)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = memory.InitializeRuntime(AwkRuntimeCounters.Disabled).State.Variables;
        var name = AwkSpecialVariables.VariableName(AwkSpecialVariableId.ConversionFormat);
        var bytes = AwkUtf8Codec.Encode(spelling).ToArray();
        var text = AwkText.FromHostBytes(bytes);
        var value = input ? AwkTestHarness.NumericInput(text) :
            AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal);
        variables.SetScalarValue(name, value);
        var cell = variables.GetSpecialScalarCell(AwkSpecialVariableId.ConversionFormat) ??
            throw new InvalidOperationException("Expected retained format scalar.");
        var previous = cell.State.Slot;
        var before = variables.GetScalarValue(cell);
        var funded = memory.Budget.Reserved;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        var prepared = AwkVariables.PrepareScalarStringCache(cell, text);

        Assert.Equal(funded, memory.Budget.Reserved);
        Assert.Equal(0, memory.Budget.Refusals);
        Assert.Equal(previous.StoredKind, cell.State.Slot.StoredKind);
        Assert.True(text.HasSameStorage(previous.ByteText));
        Assert.True(prepared.AsMemory().Equals(text.AsMemory()));
        Assert.False(prepared.HasSameStorage(text));
        var after = variables.GetScalarValue(cell);
        Assert.Equal(before.RuntimeKind, after.RuntimeKind);
        Assert.Equal(before.AsNumber(), after.AsNumber());
        Assert.Equal(spelling, after.AsString());
        Assert.Equal(spelling, previous.ByteText.AsString());
        Assert.Throws<AwkRuntimeException>(() => variables.SetScalarText(name, new string('x', 4096)));
        Assert.Equal(spelling, variables.GetScalarValue(cell).AsString());
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NumericUpdatesKeepTheirOwnerFundedAndRealGrowthStillFails(bool beforeBaseline, bool indexed)
    {
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var variables = memory.State.Variables;
        var name = beforeBaseline ? AwkVariableName.FromText("RSTART") : AwkVariableName.FromText("total");
        variables.SetScalarValue(name, AwkValue.FromNumber(7));
        var id = variables.GetOrCreateScalarSlot(name);
        var committed = memory.Accounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        for (var value = 8; value <= 100; value++)
        {
            if (indexed) variables.SetScalarValue(id, AwkValue.FromNumber(value));
            else variables.SetScalarValue(name, AwkValue.FromNumber(value));
        }
        Assert.Equal(100, variables.GetScalarValue(variables.GetScalarCell(id)).AsNumber());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Throws<AwkRuntimeException>(() => variables.SetScalarText(name, new string('x', 4096)));
        Assert.Equal(100, variables.GetOrCreateScalarValue(name).AsNumber());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NumericOfsAssignmentFreezesTheDirtyRecordBeforePublication(bool fail, bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed)
            .InitializeRuntime(AwkRuntimeCounters.Disabled, cancellation.Token);
        var state = memory.State;
        var variables = state.Variables;
        var ofs = AwkVariableName.FromText("OFS");
        variables.SetScalarValue(ofs, AwkValue.FromNumber(42));
        state.SetCurrentRecord(AwkText.FromString("left right"));
        state.AssignFieldValue(1, AwkValue.FromNumber(12.5));
        if (fail)
        {
            if (governed) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            else cancellation.Cancel();
            if (governed)
                Assert.Throws<AwkRuntimeException>(() => variables.SetScalarValue(ofs, AwkValue.FromNumber(7)));
            else
                Assert.Throws<OperationCanceledException>(() => variables.SetScalarValue(ofs, AwkValue.FromNumber(7)));
        }
        else variables.SetScalarValue(ofs, AwkValue.FromNumber(7));
        Assert.Equal(fail ? 42 : 7, variables.GetOrCreateScalarValue(ofs).AsNumber());
        if (!fail)
        {
            Assert.Equal("12.542right", state.CurrentRecordByteText.AsString());
            state.AssignFieldValue(1, AwkValue.FromNumber(8.5));
            Assert.Equal("8.57right", state.CurrentRecordByteText.AsString());
        }
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpdatesPreserveComputedNfOfsPreparationAndRefunds(bool governed)
    {
        using var memory = new CooperativeTestMemory(20_000, enabled: governed)
            .InitializeRuntime(AwkRuntimeCounters.CreateEnabled());
        var budget = memory.Budget;
        var accounting = memory.Accounting;
        Assert.Equal(governed, accounting.IsEnabled);
        var state = memory.State;
        var variables = state.Variables;
        var name = AwkVariableName.FromText("x");
        var ofs = AwkVariableName.FromText("OFS");
        state.SetCurrentRecord(AwkText.FromString("a b c"));
        variables.SetScalarValue(name, AwkValue.FromNumber(42));
        var nf = variables.GetOrCreateScalarSlot(AwkVariableName.FromText("NF"));
        Assert.Equal(3, variables.GetScalarValue(variables.GetScalarCell(nf)).AsNumber());
        variables.SetScalarText(ofs, "|");
        variables.SetScalarValue(AwkVariableName.FromText("NF"), AwkValue.FromNumber(2));
        Assert.Equal(2, variables.GetOrCreateScalarValue(AwkVariableName.FromText("NF")).AsNumber());
        Assert.Equal(2, variables.GetScalarValue(variables.GetScalarCell(nf)).AsNumber());
        Assert.Equal("a|b", state.CurrentRecordByteText.AsString());
        var baselineBytes = accounting.CurrentCommittedBytes;
        var baselineReservation = budget.Reserved;
        for (var iteration = 0; iteration < 3; iteration++)
        {
            variables.SetScalarText(name, new string('a', 4096));
            Assert.Equal(new string('a', 4096), variables.GetOrCreateScalarValue(name).AsString());
            variables.SetScalarValue(name, AwkValue.FromNumber(7));
            Assert.Equal(7, variables.GetOrCreateScalarValue(name).AsNumber());
            Assert.Equal(baselineBytes, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(baselineReservation, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedOfsPreparationPreservesValueAndAdmission(bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(20_000, enabled: governed)
            .InitializeRuntime(AwkRuntimeCounters.Disabled, cancellation.Token);
        var variables = memory.State.Variables;
        var ofs = AwkVariableName.FromText("OFS");
        memory.State.SetCurrentRecord(AwkText.FromString("left right"));
        memory.State.AssignFieldValue(1, AwkValue.FromNumber(1.25));
        var committed = memory.Accounting.CurrentCommittedBytes;
        var reserved = memory.Budget.Reserved;
        if (governed)
        {
            memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            Assert.Throws<AwkRuntimeException>(() => variables.SetScalarText(ofs, "|"));
        }
        else
        {
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => variables.SetScalarText(ofs, "|"));
        }
        Assert.Equal(" ", variables.GetOrCreateScalarValue(ofs).AsString());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Equal(reserved, memory.Budget.Reserved);
    }
    [Fact]
    public void SharedAdmissionRefusalPrecedesOfsPreparation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters);
        memory.State.SetCurrentRecord(AwkText.FromString("left right"));
        memory.State.AssignFieldValue(1, AwkValue.FromNumber(1.25));
        var variables = memory.State.Variables;
        var ofs = AwkVariableName.FromText("OFS");
        var committed = memory.Accounting.CurrentCommittedBytes;
        var reserved = memory.Budget.Reserved;
        var allocations = counters[AwkCounter.TextAllocations];
        memory.Budget.SetAvailableBytes(0);
        Assert.Throws<AwkRuntimeException>(() => variables.SetScalarText(ofs, new string('|', 4096)));
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(" ", variables.GetOrCreateScalarValue(ofs).AsString());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Equal(reserved, memory.Budget.Reserved);
        memory.Budget.SetAvailableBytes(20_000);
        Assert.Equal("1.25 right", memory.State.CurrentRecordByteText.AsString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationDuringScalarAdmissionPreventsPublication(bool existing)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = memory.InitializeRuntime(AwkRuntimeCounters.Disabled, cancellation.Token).State.Variables;
        var name = AwkVariableName.FromText("target");
        if (existing) variables.SetScalarText(name, "kept");
        var committed = memory.Accounting.CurrentCommittedBytes;
        var reserved = memory.Budget.Reserved;
        memory.Budget.OnGrowth = _ => cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => variables.SetScalarText(name, new string('x', 4096)));

        Assert.Equal(existing, variables.ContainsScalar(name));
        if (existing) Assert.Equal("kept", variables.GetOrCreateScalarValue(name).AsString());
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Equal(reserved, memory.Budget.Reserved);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

}
