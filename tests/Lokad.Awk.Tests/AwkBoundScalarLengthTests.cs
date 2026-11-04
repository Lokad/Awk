namespace Lokad.Awk.Tests;

public sealed class AwkBoundScalarLengthTests
{
    [Fact]
    public void NativeLengthDistinguishesCurrentScalarsAndArrays()
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "BEGIN{result=length(s);count=length(a)}");
        state.AssignVariableValue(AwkVariableName.FromText("s"), AwkValue.FromText("abc"));
        state.SetArrayAwkValue(AwkArrayName.FromText("a"), AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(3, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("count")).AsNumber());
        var lengths = state.Counters.BoundScalarLengths;
        state.AssignVariableValue(AwkVariableName.FromText("s"), AwkValue.FromText("updated"));
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("count")).AsNumber());
        Assert.Equal(lengths + 1, state.Counters.BoundScalarLengths);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
    }

    [Fact]
    public void FirstMissingReadKeepsUnassignedSemanticsBeforeBinding()
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "BEGIN{result=length(missing)}");
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        var lengths = state.Counters.BoundScalarLengths;
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(lengths + 1, state.Counters.BoundScalarLengths);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRegion Bind(AwkRuntimeState state, string source)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        return Assert.Single(program.BeginActions);
    }

    private static CooperativeTestMemory State()
    {
        var memory = new CooperativeTestMemory(1 << 20);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault()))));
    }
}
