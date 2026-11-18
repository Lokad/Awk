namespace Lokad.Awk.Tests;

public sealed partial class AwkBoundScalarLengthTests
{
    [Fact]
    public async Task NativeLengthDistinguishesCurrentScalarsAndArrays()
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "BEGIN{result=length(s);count=length(a)}");
        state.AssignVariableValue(AwkVariableName.FromText("s"), AwkValue.FromText("abc"));
        state.SetArrayAwkValue(AwkArrayName.FromText("a"), AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        using var storage = new AwkRegionStorage(state);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var io = AwkTestHarness.CreateRegionIo(state, new MockFileSystem());
        using var context = state.PushExecutionContext(output, io);
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, output, io));
        Assert.Equal(3, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("count")).AsNumber());

        state.AssignVariableValue(AwkVariableName.FromText("s"), AwkValue.FromText("updated"));
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, output, io));
        Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("count")).AsNumber());

        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
        await io.CloseForCleanupAsync();
    }

    [Fact]
    public async Task FirstMissingReadKeepsUnassignedSemanticsBeforeBinding()
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "BEGIN{result=length(missing)}");
        using var storage = new AwkRegionStorage(state);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var io = AwkTestHarness.CreateRegionIo(state, new MockFileSystem());
        using var context = state.PushExecutionContext(output, io);
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, output, io));
        Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());

        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, output, io));
        Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());

        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseForCleanupAsync();
    }

    private static AwkRegion Bind(AwkRuntimeState state, string source)
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource(source));
        Assert.True(binding.Success);
        var program = binding.Value.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        return Assert.Single(program.BeginActions);
    }

    private static CooperativeTestMemory State(bool governed = true, CancellationToken cancellationToken = default)
    {
        var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            cancellationToken, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault()))));
    }
}
