namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DirectNumericReturnsAvoidIntermediateValueTransport(bool governed)
    {
        const int records = 128;
        const string source = "function leaf(a,k,v){a[k][\"v\"]+=v;return a[k][\"v\"]}" +
            "function relay(a,k,v){return leaf(a,k,v)}{total+=relay(a,$1,$2)}END{print total,length(a)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Range(0, records).Select(index => "k" + index % 4 + " 1\n")));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("2112 4\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.NumericReturnForwards);
        Assert.Equal(2 * records, counters.FunctionFramesPrepared);
        Assert.Equal(2 * records, counters.BoundFunctionRuns);
        Assert.Equal(records, counters.ArrayTargetCommits);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task DirectReturnValueFallbackKeepsStringsAndInputOrigins()
    {
        const string source = "function leaf(x){return x}function relay(x){return leaf(x)}" +
            "BEGIN{print relay(7),relay(\"0007\");$0=\"0009\";v=relay($1);print v,typeof(v)}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters));
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("7 0007\n0009 strnum\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, counters.NumericReturnForwards);
        Assert.Equal(6, counters.BoundFunctionRuns);
        Assert.Equal(0, host.OpenFileCount);
    }
}
