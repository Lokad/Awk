namespace Lokad.Awk.Tests;

public sealed class AwkPredicateRegionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ComputedFieldPredicatesKeepNumericReadsAndCurrentRecords(bool length, bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(counters, governed);
        var state = memory.State;
        var region = Bind(length ? "result=(length($($1))>1)&&1" : "result=($($1)==\"needle\")&&1");
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("2 needle"));
        var before = counters[AwkCounter.TextAllocations];
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(before, counters[AwkCounter.TextAllocations]);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("1 needle"));
        before = counters[AwkCounter.TextAllocations];
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(before, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task OrdinaryLogicalOperandsKeepShortCircuitEffects(bool or, bool left)
    {
        using var memory = State(AwkRuntimeCounters.CreateEnabled(), true);
        var state = memory.State;
        var region = Bind("result=" + (left ? "1" : "0") + (or ? "||" : "&&") + "++effects");
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(or ? (left ? 0 : 1) : (left ? 1 : 0),
            state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
        Assert.Equal(or || left ? 1 : 0, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRegion Bind(string body)
    {
        var parsed = AwkParser.Parse("BEGIN{" + body + "}");
        Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(parsed.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
        return region;
    }

    private static CooperativeTestMemory State(AwkRuntimeCounters counters, bool governed)
    {
        var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault()))));
    }
}
