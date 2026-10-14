namespace Lokad.Awk.Tests;

public sealed class AwkIgnoreCaseSlotTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ReadsFollowAssignmentsAndLocalFrames(int modeValue)
    {
        var state=State((AwkCompatibilityMode)modeValue);
        var name=AwkSpecialVariables.VariableName(AwkSpecialVariableId.IgnoreCase);
        state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase,AwkValue.FromNumber(1));
        var global=state.GetOrCreateGlobalScalarSlot(name);
        var committed=state.MemoryAccounting.CurrentCommittedBytes;
        var allocations=state.Counters.AwkTextAllocations;
        for(var i=0;i<100;i++) Assert.True(state.ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).IsTruthy());
        Assert.Equal(committed,state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(allocations,state.Counters.AwkTextAllocations);
        Assert.Equal(0,state.Counters.StringDecodes);
        state.PushFunctionFrame(new AwkFunctionLocalLayout([name]),[AwkFunctionArgument.Scalar(AwkValue.FromNumber(0))]);
        try
        {
            Assert.False(state.ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).IsTruthy());
            state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase,AwkValue.FromNumber(2));
            Assert.True(state.ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).IsTruthy());
            Assert.Equal(1,state.GetGlobalScalarValue(global).AsNumber());
        }
        finally{state.PopFunctionFrame();}
        Assert.Equal(1,state.ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).AsNumber());
        state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase,AwkValue.FromNumber(0));
        Assert.False(state.ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).IsTruthy());
        Assert.Equal(0,state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void NonGnuConstructionDoesNotAdmitIgnoreCase(int modeValue)
    {
        var state=State((AwkCompatibilityMode)modeValue);
        Assert.False(state.HasScalar(AwkSpecialVariables.VariableName(AwkSpecialVariableId.IgnoreCase)));
    }

    private static AwkRuntimeState State(AwkCompatibilityMode mode)
    {
        var counters=AwkRuntimeCounters.CreateEnabled();
        var limits=AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1<<20);
        var options=new AwkRuntimeOptions(()=>new AwkUnixTimestampSeconds(0),TimeZoneInfo.Utc,new AwkRandomGenerator(),
            counters,limits,AwkRegexEngineKind.DotNetCompatibility,AwkExecutionEngineKind.Interpreter);
        return new AwkRuntimeState(AwkFieldSeparatorSpec.Default,CancellationToken.None,options,
            new AwkRuntimeResourceMonitor(limits,counters),AwkCompatibilityProfile.Default.WithMode(mode));
    }
}
