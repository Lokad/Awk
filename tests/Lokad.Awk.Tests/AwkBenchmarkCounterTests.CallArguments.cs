namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PreparedArgumentsForwardArrayAndScalarCellsWithoutTextConversion(bool governed)
    {
        const string source = "function add(p,k,v){p[k]+=v;return p[k]}function call(p,k,v){return add(p,k,v)}BEGIN{for(i=0;i<64;i++)t=call(a,\"key\",i);print t}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var result = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, result.Value);
        Assert.Equal("2016\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(128, counters.BoundFunctionRuns);
        Assert.Equal(64, counters.IndexedLocalArrayArguments);
        Assert.InRange(counters.NumericLocalReads, 64, 256);
        Assert.InRange(counters.AwkTextAllocations, 0, 16);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
