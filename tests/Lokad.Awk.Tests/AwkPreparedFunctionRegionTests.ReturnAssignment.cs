namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void ConsumedReturnsTransferFundingToTheActualScalar(bool governed, bool local, bool numericString)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("saved");
        var variable = new AwkRegionOperand(local ? AwkRegionOperandKind.LocalVariable : AwkRegionOperandKind.GlobalVariable, 0, name);
        if (local) state.PushFunctionFrame(new([name]), []);
        else state.InitializeGlobalReadSlots(1);
        if (local) state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("previous"));
        else state.AssignPreparedGlobalValue(name, 0, AwkValue.FromText("previous"));
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Values: 1));
        var spelling = "0007" + new string(' ', 32 << 10);
        var text = AwkText.FromSourceLiteralString(spelling);
        var value = numericString ? AwkValue.FromNumericString(text) : state.CreateStringValue(text);
        source.StoreValue(0, value);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            state.AssignConsumedReturn(in variable, source, 0);
            Assert.Equal(0, source.Values[0].RetainedTextMemoryBytes);
            source.Dispose();
            var stored = local ? state.ResolveFunctionLocalValue(new(0), name) : state.ResolveVariableValue(name);
            Assert.Equal(spelling, stored.ByteText.AsString());
            Assert.Equal(numericString ? AwkRuntimeValueKind.NumericString : AwkRuntimeValueKind.String, stored.RuntimeKind);
            Assert.Equal(7, stored.AsNumber());
            if (governed) Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= stored.RetainedTextMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        if (local) state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CancelledReturnPublicationKeepsBothOwnersIntact(bool local)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("saved");
        var variable = new AwkRegionOperand(local ? AwkRegionOperandKind.LocalVariable : AwkRegionOperandKind.GlobalVariable, 0, name);
        if (local)
        {
            state.PushFunctionFrame(new([name]), []);
            state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("previous"));
        }
        else
        {
            state.InitializeGlobalReadSlots(1);
            state.AssignPreparedGlobalValue(name, 0, AwkValue.FromText("previous"));
        }
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Values: 1));
        source.StoreValue(0, AwkValue.FromText("returned"));
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => state.AssignConsumedReturn(in variable, source, 0));
        Assert.Equal("returned", source.Values[0].ByteText.AsString());
        // Inspect existing owner snapshots without entering another cancelled read.
        if (local) Assert.Equal("previous", state.CurrentFunctionFrame.GetSlot(0).Value.ByteText.AsString());
        else Assert.Equal("previous", state.ResolveVariableValue(name).ByteText.AsString());
        source.Dispose();
        if (local) state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData("cold")] [InlineData("hidden")] [InlineData("array")]
    public void ReturnFallbackKeepsTheSourceUntilItsOwnPreflightsSucceed(string target)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("saved");
        var variable = new AwkRegionOperand(target == "cold" ? AwkRegionOperandKind.GlobalVariable : AwkRegionOperandKind.LocalVariable, 0, name);
        state.InitializeGlobalReadSlots(1);
        var array = AwkArrayName.FromText("a");
        if (target == "array")
        {
            state.SetArrayAwkValue(array, new("kept"), AwkValue.FromNumber(9));
            state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.ArrayAlias(array)]);
        }
        else if (target == "hidden") state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("caller"))]);
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Values: 1));
        source.StoreValue(0, AwkValue.FromText("returned"));
        using (target == "hidden" ? (IDisposable)state.EnterGlobalBindings() : null)
        {
            if (target != "array") memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            var failure = Assert.Throws<AwkRuntimeException>(() => state.AssignConsumedReturn(in variable, source, 0));
            Assert.Equal(target == "array" ? "awk: attempt to use array `saved' in a scalar context\n" : "awk: memory limit exceeded\n",
                AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal("returned", source.Values[0].ByteText.AsString());
            Assert.False(state.HasScalar(name));
        }
        if (target == "array") Assert.Equal(9, state.GetArrayAwkValue(array, new("kept")).AsNumber());
        else if (target == "hidden") Assert.Equal("caller", state.ResolveFunctionLocalValue(new(0), name).AsString());
        source.Dispose();
        if (target != "cold") state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }
}
