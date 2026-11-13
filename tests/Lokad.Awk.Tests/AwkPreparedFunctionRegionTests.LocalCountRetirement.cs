namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UnobservedLocalRetirementPreservesFundingAndUntypedOrigin(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(4 << 20);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("t");
        var layout = new AwkFunctionLocalLayout([name]);
        var spelling = new string('x', 32 << 10);
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling))]);
        var before = memory.Budget.Reserved;
        var observed = false;
        memory.Budget.OnRefund = () =>
        {
            memory.Budget.OnRefund = null;
            observed = true;
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, state.CurrentFunctionFrame.GetSlot(0).Kind);
            if (cancel) cancellation.Cancel();
        };
        var operand = new AwkRegionOperand(AwkRegionOperandKind.LocalVariable, 0, name);
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => state.RetireFunctionLocalScalar(in operand));
        else state.RetireFunctionLocalScalar(in operand);
        Assert.True(observed);
        Assert.True(memory.Budget.Reserved < before);
        state.PopFunctionFrame();
        var origin = new AwkUntypedScalar();
        // Use a fresh uncanceled state to isolate the shared-origin contract.
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        using var origins = new CooperativeTestMemory(4 << 20);
        origins.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: origins.Owner));
        origins.State.PushFunctionFrame(layout, [AwkFunctionLocalSlot.UntypedScalar(AwkValue.FromUntypedScalar(origin))]);
        Assert.False(origin.IsScalar);
        origins.State.RetireFunctionLocalScalar(in operand);
        Assert.True(origin.IsScalar);
        origins.State.PopFunctionFrame();
        origins.Dispose();
        Assert.Equal(0, origins.Budget.Reserved);
    }
}
