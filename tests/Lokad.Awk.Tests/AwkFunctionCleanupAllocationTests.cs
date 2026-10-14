namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFunctionCleanupAllocationTests
{
    [Fact]
    public void RuntimeFrameCleanupDoesNotAllocateForEveryCall()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]);
        AwkFunctionArgument[] arguments = [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))];
        for (var i = 0; i < 100; i++)
        {
            state.PushFunctionFrame(layout, arguments);
            state.PopFunctionFrame();
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++)
        {
            state.PushFunctionFrame(layout, arguments);
            state.PopFunctionFrame();
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated < 1024, "Allocated bytes: " + allocated);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
