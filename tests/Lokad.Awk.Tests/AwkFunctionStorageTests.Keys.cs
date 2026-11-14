namespace Lokad.Awk.Tests;

public sealed partial class AwkFunctionStorageTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ForwardedKeyCellsRetainTheirSpellingOriginAndAdjacentArguments(bool governed)
    {
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        var stack = Stack(memory);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("key"), AwkVariableName.FromText("number")]);
        AwkArrayKey[] keys = [default, AwkArrayKey.FromOrdinal(int.MinValue), AwkArrayKey.FromOrdinal(-7),
            AwkArrayKey.FromOrdinal(7), AwkArrayKey.FromOrdinal(7).PrepareByteLookup(),
            AwkArrayKey.FromOrdinal(int.MaxValue), new("0007"), new("a\0b"), new("é😀"),
            new("abc😀"), new("long owned key spelling"), new(new string('a', 4097))];
        foreach (var key in keys)
        {
            var caller = stack.PrepareFrame(layout);
            stack.StageKey(caller, 0, key);
            caller.StageNumber(1, 9);
            stack.ActivateFrame(caller);
            var incoming = stack.PrepareFrame(layout);
            incoming.StageNumber(1, 42);
            Assert.True(stack.TryStageLocalArgument(null, new(0), incoming, 0, allowArray: false, prepareKey: true));
            var actual = incoming.GetSlot(0).Key;
            Assert.Equal(AwkFunctionLocalSlotKind.Key, incoming.GetSlot(0).Kind);
            Assert.Equal(default, incoming.GetSlot(0).ArrayName);
            Assert.Equal(key, actual);
            Assert.Equal(key.IsOrdinal, actual.IsOrdinal);
            Assert.Equal(key.AsString(AwkRuntimeCounters.Disabled), actual.AsString(AwkRuntimeCounters.Disabled));
            Assert.Equal(AwkArrayKeyComparer.Instance.GetHashCode(key), AwkArrayKeyComparer.Instance.GetHashCode(actual));
            Assert.True(key.StoredText.HasSameStorage(actual.StoredText));
            Assert.Equal(9, caller.GetSlot(1).Value.AsNumber());
            Assert.Equal(42, incoming.GetSlot(1).Value.AsNumber());
            stack.DiscardIncomingFrame(incoming);
            stack.PopFrame(static _ => { });
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void CompactKeyForwardingUsesAlreadyFundedCellsAtFullBudget()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("key")]);
        var caller = stack.PrepareFrame(layout);
        stack.StageKey(caller, 0, new("é😀"));
        stack.ActivateFrame(caller);
        var incoming = stack.PrepareFrame(layout);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            Assert.True(stack.TryStageLocalArgument(null, new(0), incoming, 0, allowArray: false, prepareKey: true));
            Assert.Equal("é😀", incoming.GetSlot(0).Key.AsString(AwkRuntimeCounters.Disabled));
            Assert.Equal(0, incoming.RetainedMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        stack.DiscardIncomingFrame(incoming);
        stack.PopFrame(static _ => { });
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void KeyForwardingCapturesTheCallerCellBeforeAdmission()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("key")]);
        var key = new AwkArrayKey(new string('a', 4097));
        var caller = stack.PrepareFrame(layout);
        stack.StageKey(caller, 0, key);
        stack.ActivateFrame(caller);
        var incoming = stack.PrepareFrame(layout);
        memory.Budget.OnGrowth = _ =>
        {
            memory.Budget.OnGrowth = null;
            caller.GetSlot(0) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(9));
        };
        Assert.True(stack.TryStageLocalArgument(null, new(0), incoming, 0, allowArray: false, prepareKey: true));
        Assert.Equal(9, caller.GetSlot(0).Value.AsNumber());
        Assert.Equal(key, incoming.GetSlot(0).Key);
        Assert.True(key.StoredText.HasSameStorage(incoming.GetSlot(0).Key.StoredText));
        Assert.True(incoming.RetainedMemoryBytes > 0);
        stack.DiscardIncomingFrame(incoming);
        stack.PopFrame(static _ => { });
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FailedKeyForwardingPreservesTheCallerAndUnpublishedIncomingCell(bool cancel)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var cancellation = new CancellationTokenSource();
        var stack = Stack(memory, cancellation.Token.ThrowIfCancellationRequested);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("key")]);
        var key = new AwkArrayKey(new string('a', 4097));
        var caller = stack.PrepareFrame(layout);
        stack.StageKey(caller, 0, key);
        stack.ActivateFrame(caller);
        var incoming = stack.PrepareFrame(layout);
        var before = memory.Budget.Reserved;
        if (cancel)
        {
            memory.Budget.OnGrowth = _ => cancellation.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(() =>
                stack.TryStageLocalArgument(null, new(0), incoming, 0, allowArray: false, prepareKey: true));
        }
        else
        {
            memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            Assert.Throws<AwkRuntimeException>(() =>
                stack.TryStageLocalArgument(null, new(0), incoming, 0, allowArray: false, prepareKey: true));
            Assert.Equal(1, memory.Budget.Refusals);
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        }
        Assert.Equal(key, caller.GetSlot(0).Key);
        Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, incoming.GetSlot(0).Kind);
        Assert.Equal(0, incoming.RetainedMemoryBytes);
        Assert.Equal(before, memory.Budget.Reserved);
        memory.Budget.OnGrowth = null;
        stack.DiscardIncomingFrame(incoming);
        stack.PopFrame(static _ => { });
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
