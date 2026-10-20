namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFunctionCleanupAllocationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleArgumentInvocationDoesNotAllocateAnInlineArray(bool evaluatedValues)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault());
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var function = new AwkUserFunction(AwkFunctionName.FromText("f"), new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]), []);
        AwkExpression[] expressions = [AwkExpression.Number("42", 42)];
        AwkValue[] values = [AwkValue.FromNumber(42)];
        var context = new AwkEvaluationContext(state);
        void Invoke()
        {
            var pending = evaluatedValues ? function.InvokeValuesAsync(values, context) : function.InvokeAsync(expressions, context);
            if (!pending.IsCompletedSuccessfully) throw new InvalidOperationException("Ordinary call suspended unexpectedly.");
            _ = pending.Result;
        }
        for (var index = 0; index < 100; index++) Invoke();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++) Invoke();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated < 1024, "Allocated bytes: " + allocated);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.False(state.IsInsideFunction);
    }

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
