namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFunctionCleanupAllocationTests
{
    [Fact]
    public async Task RepeatedPreparedCallsDoNotAllocateArgumentArrays()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("function f(x){return x}BEGIN{total=0;for(i=0;i<10000;i++)total+=f(42)}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var region = Assert.Single(program.BeginActions);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        for (var index = 0; index < 4; index++) await region.ExecuteAsync(state, storage, regionOutput, regionIo);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated < 1024, "Allocated bytes: " + allocated);
        Assert.Equal(420000, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());

        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void RuntimeFrameCleanupDoesNotAllocateForEveryCall()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]);
        AwkFunctionLocalSlot[] arguments = [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))];
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
