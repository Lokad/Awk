namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false, 5)] [InlineData(false, true, 5)]
    [InlineData(true, false, 5)] [InlineData(true, true, 5)]
    [InlineData(false, false, 9)] [InlineData(false, true, 9)]
    [InlineData(true, false, 9)] [InlineData(true, true, 9)]
    [InlineData(false, false, 65)] [InlineData(false, true, 65)]
    [InlineData(true, false, 65)] [InlineData(true, true, 65)]
    public async Task UserCallsReuseGovernedStagingAcrossOrdinaryAndPreparedBodies(bool governed, bool ordinary, int arity)
    {
        const int records = 128;
        var parameters = string.Join(',', Enumerable.Range(0, arity).Select(i => "p" + i));
        var statements = string.Concat(Enumerable.Range(0, arity).Select(i => "s+=p" + i + ";"));
        var source = "function sum(" + parameters + ",s){"
            + (ordinary ? "switch(0){default:break}" : "")
            + statements + "return s}{total+=sum("
            + string.Join(',', Enumerable.Repeat("$1", arity)) + ")}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((records * arity * 7) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.FunctionArgumentBufferRents);
        Assert.Equal(governed ? 1 : 0, counters.GovernedFunctionArgumentBufferAllocations);
        Assert.Equal(governed ? records - 1 : 0, counters.GovernedFunctionArgumentBufferReuses);
        Assert.Equal(ordinary ? 0 : records, counters.SynchronousFunctionRuns);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
