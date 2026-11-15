namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void StandaloneArrayRootsKeepVisibleBindingsAtFullBudget(bool governed, bool hidden)
    {
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var program = Bind("function f(a,k){x=a[k][\"v\"]}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var function = Assert.Single(program.Functions).Value;
        var region = Assert.IsType<AwkRegion>(function.Region);
        var global = AwkArrayName.FromText("a");
        var local = AwkArrayName.FromText("b");
        var x = AwkVariableName.FromText("x");
        state.SetArrayAwkValue(state.GetSubarray(global, new("key"), default), new("v"), AwkValue.FromNumber(9));
        state.SetArrayAwkValue(state.GetSubarray(local, new("key"), default), new("v"), AwkValue.FromNumber(7));
        state.AssignVariableValue(AwkVariableName.FromText("k"), AwkValue.FromText("key"));
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        state.PushFunctionFrame(function.LocalLayout,
            [AwkFunctionLocalSlot.ArrayAlias(local), AwkFunctionLocalSlot.PreparedKey(new("key"))]);
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            try
            {
                if (hidden)
                {
                    using var bindings = state.EnterGlobalBindings();
                    region.Execute(state, storage);
                }
                else region.Execute(state, storage);
                Assert.Equal(hidden ? 9 : 7, state.ResolveVariableValue(x).AsNumber());
                Assert.Equal(0, memory.Budget.Refusals);
            }
            finally { state.PopFunctionFrame(); }
        }
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }
}
