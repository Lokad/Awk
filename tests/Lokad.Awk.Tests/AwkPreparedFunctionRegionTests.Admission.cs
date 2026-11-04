namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("function inc(x){return x+1}function step(x){print \"before\",x;return inc(inc(x))}", "step(7)", 7)]
    [InlineData("function inc(x,y){return x+y}function step(x,y){print \"before\",x;return inc(inc(x,y),y)}", "step(7,1)", 7)]
    [InlineData("function inc(x){return x+1}function step(x){print \"before\",x;if(x)return inc(x);return inc(x)+1}", "step(1)", 1)]
    [InlineData("function inc(x){return x+1}function step(x){print \"before\",x;if(x)return inc(x);return inc(x)+1}", "step(0)", 0)]
    [InlineData("function inc(x){return x+1}function step(x){print \"before\",x;x=inc(x);return x}", "step(7)", 7)]
    [InlineData("function inc(x){return x+1}function step(x){print \"before\",x;x=inc(x);if(x>0)return x;return 0}", "step(7)", 7)]
    public async Task IncomingCallRefusalPreservesActiveCallerAndEarlierEffects(string definitions, string call, int expected)
    {
        var budget = new CooperativeTestBudget(8L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var program = Bind(definitions + "function caller(x){total=" + call + ";print \"after\",x}BEGIN{caller(42)}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.AssignVariableValue(AwkVariableName.FromText("total"), AwkValue.FromNumber(5));
        var host = new MockFileSystem();
        var observedRefusal = false;
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before " + expected + "\n") return;
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            budget.OnRefusal = () =>
            {
                observedRefusal = true;
                Assert.Equal(expected, state.ResolveVariableValue(AwkVariableName.FromText("x")).AsNumber());
                Assert.True(state.IsInsideFunction);
            };
        };
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        try
        {
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.True(observedRefusal);
            Assert.Equal(5, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
            Assert.Equal("before " + expected + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        }
        finally
        {
            await io.CloseForCleanupAsync();
            output.Dispose();
            state.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LocalValueRefusalPreservesAssignmentPrefixWithoutRetry(bool conditional)
    {
        var budget = new CooperativeTestBudget(8L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var program = Bind("function step(x,t){t=\"keep\";print \"before\",x,t;t=\"" + new string('a', 4096) +
            "\";" + (conditional ? "if(x)return t;return \"\"" : "return t") + "}BEGIN{total=step(7);print \"after\"}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.AssignVariableValue(AwkVariableName.FromText("total"), AwkValue.FromNumber(5));
        var observedRefusal = false;
        var host = new MockFileSystem();
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before 7 keep\n") return;
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            budget.OnRefusal = () =>
            {
                observedRefusal = true;
                Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("x")).AsNumber());
                Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("t")).AsString());
            };
        };
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        try
        {
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.True(observedRefusal);
            Assert.Equal("before 7 keep\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(5, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        }
        finally
        {
            await io.CloseForCleanupAsync();
            output.Dispose();
            state.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, budget.Reserved);
        }
    }
}
