namespace Lokad.Awk.Tests;

public sealed class AwkIncomingFrameTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReusedFramesKeepMissingOriginsAcrossInlineAndBufferedLayouts(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        foreach (var width in new[] { 8, 1, 3, 12, 2, 7 })
        {
            var names = Enumerable.Range(0, width).Select(index => AwkVariableName.FromText("p" + index)).ToArray();
            var frame = state.PrepareFunctionFrame(new(names));
            for (var index = 0; index < width; index++)
            {
                ref readonly var slot = ref frame.GetSlot(index);
                Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, slot.Kind);
                Assert.Equal(AwkValueKind.Unassigned, slot.Value.StoredKind);
            }
            Assert.False(state.IsInsideFunction);
            state.StageFunctionArgument(frame, 0, AwkFunctionLocalSlot.Scalar(AwkValue.FromText("")));
            state.ActivateFunctionFrame(frame);
            try
            {
                Assert.Equal(AwkRuntimeValueKind.String, state.ResolveFunctionLocalValue(new(0), names[0]).RuntimeKind);
                for (var index = 1; index < width; index++)
                {
                    Assert.Equal(AwkRuntimeValueKind.Unassigned, state.ResolveFunctionLocalValue(new(index), names[index]).RuntimeKind);
                    state.AssignFunctionLocalValue(new(index), names[index], AwkValue.FromNumericString("0007"));
                }
            }
            finally { state.PopFunctionFrame(); }
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void IncomingLocalsStayHiddenDuringNestedCallerWork(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var x = AwkVariableName.FromText("x");
        var layout = new AwkFunctionLocalLayout([x]);
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))]);
        var outer = state.PrepareFunctionFrame(layout);
        state.StageFunctionArgument(outer, 0, AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7)));
        Assert.Equal(42, state.ResolveFunctionLocalValue(new(0), x).AsNumber());
        var inner = state.PrepareFunctionFrame(layout);
        state.StageFunctionArgument(inner, 0, AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(9)));
        state.ActivateFunctionFrame(inner);
        Assert.Equal(9, state.ResolveFunctionLocalValue(new(0), x).AsNumber());
        state.PopFunctionFrame();
        Assert.Equal(42, state.ResolveFunctionLocalValue(new(0), x).AsNumber());
        state.ActivateFunctionFrame(outer);
        Assert.Equal(7, state.ResolveFunctionLocalValue(new(0), x).AsNumber());
        state.PopFunctionFrame();
        Assert.Equal(42, state.ResolveFunctionLocalValue(new(0), x).AsNumber());
        state.PopFunctionFrame();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DetachedArrayArgumentIsRecheckedOnlyAtActivation(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var name = AwkArrayName.FromText("a");
        var parent = new AwkArrayKey("row");
        var child = state.GetSubarray(name, parent, default);
        state.SetArrayAwkValue(child, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        var frame = state.PrepareFunctionFrame(new([AwkVariableName.FromText("p")]));
        state.StageFunctionArgument(frame, 0, AwkFunctionLocalSlot.ArrayAlias(child));
        state.DeleteArrayValue(name, parent);
        state.ActivateFunctionFrame(frame);
        var local = AwkArrayName.FromText("p");
        Assert.Equal(0, state.GetArrayAwkValue(local, AwkArrayKey.FromOrdinal(1)).AsNumber());
        state.SetArrayAwkValue(local, AwkArrayKey.FromOrdinal(2), AwkValue.FromNumber(9));
        state.PopFunctionFrame();
        Assert.False(state.ContainsArrayValue(name, parent));
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RefusedOrCanceledArgumentPayloadLeavesIncomingFrameUnpublished(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var frame = state.PrepareFunctionFrame(new([AwkVariableName.FromText("p")]));
        var before = fixture.Budget.Reserved;
        if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var argument = AwkFunctionLocalSlot.Scalar(AwkValue.FromText(new string('x', 4096)));
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => state.StageFunctionArgument(frame, 0, argument));
        else Assert.Throws<AwkRuntimeException>(() => state.StageFunctionArgument(frame, 0, argument));
        Assert.False(state.IsInsideFunction);
        Assert.Equal(before, fixture.Budget.Reserved);
        state.DiscardIncomingFunctionFrame(frame);
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    private static CooperativeTestMemory State(CancellationToken token, bool governed)
    {
        var fixture = new CooperativeTestMemory(8 << 20, enabled: governed);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault());
        return fixture.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, fixture.Configure(options)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RefusedOrCanceledOwnedKeyLeavesTheCallerAndIncomingCellUntouched(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var spelling = new string('x', 4096);
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling))]);
        var caller = state.CurrentFunctionFrame;
        var incoming = state.PrepareFunctionFrame(new([name]));
        var before = fixture.Budget.Reserved;
        try
        {
            if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
            else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            void Stage() => state.StageFunctionLocalArgument(caller, new(AwkRegionArgumentKind.Local, 0, name, Consumption: AwkParameterInput.Key), incoming, 0);
            if (cancel) Assert.ThrowsAny<OperationCanceledException>(Stage);
            else Assert.Throws<AwkRuntimeException>(Stage);
            Assert.Same(caller, state.CurrentFunctionFrame);
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, incoming.GetSlot(0).Kind);
            Assert.Equal(AwkValueKind.Unassigned, incoming.GetSlot(0).Value.StoredKind);
            Assert.Equal(spelling, caller.GetSlot(0).Value.AsString());
            Assert.Equal(before, fixture.Budget.Reserved);
        }
        finally
        {
            fixture.Budget.OnGrowth = null;
            state.DiscardIncomingFunctionFrame(incoming);
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void ActiveLocalWriteCanceledByAdmissionPreservesPriorValue()
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var name = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7))]);
        var before = fixture.Budget.Reserved;
        var payload = AwkValue.FromText(new string('x', 4096));
        fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => state.AssignFunctionLocalValue(new(0), name, payload));
        Assert.Equal(7, state.ResolveFunctionLocalValue(new(0), name).AsNumber());
        Assert.Equal(before, fixture.Budget.Reserved);
        state.PopFunctionFrame();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }
}
