namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void LocalReturnKeepsItsExistingFundingAtFullBudget(bool governed, bool input, bool overwrite)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("s");
        var spelling = "0007" + new string(' ', 32 << 10);
        var value = input ? AwkValue.FromNumericString(spelling) : AwkValue.FromText(spelling);
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(value)]);
        var frame = state.CurrentFunctionFrame;
        var captured = state.ResolveFunctionLocalValue(new(0), name);
        using var snapshot = new AwkRegionStorage(state);
        snapshot.Prepare(new(0, Values: 1));
        if (overwrite) snapshot.StoreValue(0, AwkValue.FromText("previous"));
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            Assert.True(snapshot.TryTakeRetiringLocal(0, frame, 0));
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, frame.GetSlot(0).Kind);
            state.PopFunctionFrame();
            Assert.Equal(spelling, snapshot.Values[0].ByteText.AsString());
            Assert.Equal(input ? AwkRuntimeValueKind.NumericString : AwkRuntimeValueKind.String,
                snapshot.Values[0].RuntimeKind);
            Assert.Equal(7, snapshot.Values[0].AsNumber());
            if (governed) Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= captured.RetainedTextMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        snapshot.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void LocalReturnCapturesTheCurrentCellAfterReplacementOrReuse(bool governed, bool recycle)
    {
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("s");
        var layout = new AwkFunctionLocalLayout([name]);
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("captured"))]);
        var frame = state.CurrentFunctionFrame;
        var captured = state.ResolveFunctionLocalValue(new(0), name);
        if (recycle)
        {
            state.PopFunctionFrame();
            state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("replacement"))]);
            Assert.Same(frame, state.CurrentFunctionFrame);
        }
        else state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("replacement"));
        using var snapshot = new AwkRegionStorage(state);
        snapshot.Prepare(new(0, Values: 1));
        snapshot.StoreValue(0, AwkValue.FromText("previous"));
        Assert.True(snapshot.TryTakeRetiringLocal(0, frame, 0));
        Assert.Equal("replacement", snapshot.Values[0].AsString());
        Assert.Equal("captured", captured.AsString());
        Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, frame.GetSlot(0).Kind);
        state.PopFunctionFrame();
        snapshot.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LocalReturnPollsLongInputBeforeRelinquishingItsSource(bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("s");
        var spelling = new string('0', 32 << 10) + "7";
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumericString(spelling))]);
        var frame = state.CurrentFunctionFrame;
        using var snapshot = new AwkRegionStorage(state);
        snapshot.Prepare(new(0, Values: 1));
        snapshot.StoreValue(0, AwkValue.FromText("previous"));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => snapshot.TryTakeRetiringLocal(0, frame, 0));
        Assert.Equal(spelling, frame.GetSlot(0).Value.ByteText.AsString());
        Assert.Equal("previous", snapshot.Values[0].AsString());
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        state.PopFunctionFrame();
        snapshot.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task LocalReturnKeepsFundingThroughPendingDiagnostics(bool cancel) =>
        CheckPublicTextReturnAsync(cancel, local: true);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task LocalReturnKeepsFundingThroughReadyAndPendingFlush(bool pending, bool cancel) =>
        CheckReturnedValueAsync(numeric: false, pending, cancel, local: true);
}
