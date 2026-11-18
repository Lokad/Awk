namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task StandaloneKeyReadsKeepVisibleBindingsAtFullBudget(bool governed, bool hidden)
    {
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var program = Bind("function f(k){x=a[k]}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var function = Assert.Single(program.Functions).Value;
        var region = Assert.IsType<AwkRegion>(function.Region);
        var name = AwkArrayName.FromText("a");
        var x = AwkVariableName.FromText("x");
        state.Arrays.SetAwkValue(name, new("local"), AwkValue.FromNumber(7));
        state.Arrays.SetAwkValue(name, new("global"), AwkValue.FromNumber(9));
        state.AssignVariableValue(AwkVariableName.FromText("k"), AwkValue.FromText("global"));
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        state.PushFunctionFrame(function.LocalLayout, [AwkFunctionLocalSlot.PreparedKey(new("local"))]);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            try
            {
                if (hidden)
                {
                    using var bindings = state.EnterGlobalBindings();
                    await region.ExecuteAsync(state, storage, regionOutput, regionIo);
                }
                else await region.ExecuteAsync(state, storage, regionOutput, regionIo);
                Assert.Equal(hidden ? 9 : 7, state.ResolveVariableValue(x).AsNumber());
                Assert.Equal(0, memory.Budget.Refusals);
            }
            finally { state.PopFunctionFrame(); }
        }
        storage.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }
}
