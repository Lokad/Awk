namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task EqualPureIntegerSortValuesNeedNoFormattedTieText(bool governed, bool signedZero)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        var value = signedZero ? "(i%2?-0:0)" : "i%8";
        var source = "BEGIN{for(i=1;i<=128;i++)a[i]=" + value +
            ";n=asort(a,b,\"@val_num_asc\");print n,b[1],b[n]}";
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal(signedZero ? "128 0 0\n" : "128 0 7\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.AwkTextAllocations, 0, 16);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
