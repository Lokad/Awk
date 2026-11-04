namespace Lokad.Awk.Tests;

public sealed partial class AwkArrayCompoundUpdateTests
{
    [Theory]
    [InlineData(0, false, false)] [InlineData(0, false, true)]
    [InlineData(0, true, false)] [InlineData(0, true, true)]
    [InlineData(1, false, false)] [InlineData(1, false, true)]
    [InlineData(1, true, false)] [InlineData(1, true, true)]
    [InlineData(2, false, false)] [InlineData(2, false, true)]
    [InlineData(2, true, false)] [InlineData(2, true, true)]
    public async Task BoundUpdateReacquiresReplacementBackingAfterPendingRhs(int replacement, bool refuse, bool composite)
    {
        var mutation = replacement switch
        {
            1 => "split(\"10 20\",values);",
            2 => "delete values;for(i=0;i<2000;i++)values[i]=i;",
            _ => "delete values;"
        };
        var source = "function rhs(v){" + mutation + "getline v<\"/rhs\";return v}" +
            "BEGIN{result=(values[" + (composite ? "\"left\",\"right\"" : "\"k\"") + "]+=rhs())}";
        await using var execution = new NativeArrayExecution(source, true, new InputGate("/rhs", "3\n", true));
        var state = execution.State;
        var name = AwkArrayName.FromText("values");
        var key = composite ? new AwkArrayKey("left" + (char)28 + "right") : Key;
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var pending = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(pending.IsCompleted);
        Assert.False(state.ContainsArrayValue(name, key));
        if (refuse) execution.Memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        execution.Inputs[0].Release.TrySetResult();
        if (refuse)
        {
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.False(state.ContainsArrayValue(name, key));
            Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
            Assert.Equal(1, execution.Memory.Budget.Refusals);
            Assert.Equal(0, execution.Memory.Budget.GrowthAttemptsAfterRefusal);
        }
        else
        {
            Assert.Equal(AwkStatementFlow.Continue, await pending.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
            Assert.Equal(10, state.GetArrayAwkValue(name, key).AsNumber());
        }
        if (replacement == 1) Assert.Equal("20", state.GetArrayAwkValue(name, AwkArrayKey.FromOrdinal(2)).AsString());
        if (replacement == 2) Assert.Equal(1999, state.GetArrayAwkValue(name, AwkArrayKey.FromOrdinal(1999)).AsNumber());
        Assert.Equal(1, execution.Reads);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
