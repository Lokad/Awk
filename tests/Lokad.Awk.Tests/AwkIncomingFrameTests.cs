namespace Lokad.Awk.Tests;

public sealed class AwkIncomingFrameTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void IncomingLocalsStayHiddenDuringNestedCallerWork(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        var x = AwkVariableName.FromText("x");
        var layout = new AwkFunctionLocalLayout([x]);
        state.PushFunctionFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var outer = state.PrepareFunctionFrame(layout, 1);
        state.StageFunctionArgument(outer, 0, AwkFunctionArgument.Scalar(AwkValue.FromNumber(7)));
        Assert.Equal(42, state.ResolveFunctionLocalValue(new(0), x).AsNumber());
        var inner = state.PrepareFunctionFrame(layout, 1);
        state.StageFunctionArgument(inner, 0, AwkFunctionArgument.Scalar(AwkValue.FromNumber(9)));
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
        var frame = state.PrepareFunctionFrame(new([AwkVariableName.FromText("p")]), 1);
        state.StageFunctionArgument(frame, 0, AwkFunctionArgument.Array(child));
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
        var frame = state.PrepareFunctionFrame(new([AwkVariableName.FromText("p")]), 1);
        var before = fixture.Budget.Reserved;
        if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var argument = AwkFunctionArgument.Scalar(AwkValue.FromText(new string('x', 4096)));
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

    [Fact]
    public void ActiveLocalWriteCanceledByAdmissionPreservesPriorValue()
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        var state = fixture.State;
        var name = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([name]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(7))]);
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
