namespace Lokad.Awk.Tests;

public sealed partial class AwkArrayCompoundUpdateTests
{
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(0, true, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, true, true)]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    public async Task BoundUpdateReacquiresReplacementBackingAfterPendingRhs(int replacement, bool refuse, bool composite)
    {
        using var memory = State();
        var state = memory.State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkArrayName.FromText("values");
        var key = composite ? new AwkArrayKey("left" + (char)28 + "right") : Key;
        var index = composite ? AwkArrayIndex.Composite([
            AwkExpression.Literal("left"), AwkExpression.Literal("right")]) : Index;
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7), 0);
        var source = new ValueSource(3, pending: true);
        var right = new EffectExpression(source, () =>
        {
            if (replacement == 1)
                state.SplitIntoArray(AwkText.FromString("10 20"), name, AwkFieldSeparatorSpec.Default, AwkOptionalIdentifier.None);
            else
            {
                state.ClearArray(name);
                if (replacement == 2)
                    for (var index = 0; index < 2000; index++)
                        state.SetArrayAwkValue(name, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));
            }
        });
        var target = AwkReference.ArrayElement(name, index, 0);
        var pending = target.ApplyCompoundAssignmentAsync(new(state), right, AwkCompoundAssignmentOperation.Add);
        Assert.False(pending.IsCompleted);
        Assert.False(state.ContainsArrayValue(name, key));
        var heldBytes = state.MemoryAccounting.CurrentCommittedBytes;
        if (refuse) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        source.Complete();

        if (refuse)
        {
            await Assert.ThrowsAsync<AwkRuntimeException>(() => pending.AsTask());
            Assert.False(state.ContainsArrayValue(name, key));
            Assert.Equal(1, memory.Budget.Refusals);
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes < heldBytes);
        }
        else
        {
            Assert.Equal(10, (await pending).AsNumber());
            Assert.Equal(10, state.GetArrayAwkValue(name, key, 0).AsNumber());
            Assert.Equal(1, state.Counters.BoundArrayUpdates);
        }
        if (replacement == 1) Assert.Equal("20", state.GetArrayAwkValue(name, AwkArrayKey.FromOrdinal(2)).AsString());
        if (replacement == 2) Assert.Equal(1999, state.GetArrayAwkValue(name, AwkArrayKey.FromOrdinal(1999)).AsNumber());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.StringDecodes);
    }
}
