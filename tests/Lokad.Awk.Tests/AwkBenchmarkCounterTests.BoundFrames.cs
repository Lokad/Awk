namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(1, false)] [InlineData(1, true)]
    [InlineData(2, false)] [InlineData(2, true)]
    [InlineData(5, false)] [InlineData(5, true)]
    [InlineData(65, false)] [InlineData(65, true)]
    public async Task BoundFrameNumbersKeepNativeReadsAndOwnedCleanup(int slots, bool governed)
    {
        const int records = 128;
        var parameters = string.Join(",", Enumerable.Range(0, slots).Select(index => "p" + index));
        var source = "function step(" + parameters + "){p0+=1;p0+=2;" +
            (slots > 1 ? "p1=1;p1+=2;return p0+(p1-3)}" : "return p0}") +
            "{total+=step($2)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1280\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.BoundFrameNumberWrites >= records);
        Assert.Equal(0, counters.FunctionActivationSlotVisits);
        Assert.Equal(0, counters.FunctionRetirementSlotVisits);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AlternatingPreparedAritiesReuseFundedLocalCapacity(bool governed)
    {
        const int records = 128;
        const string source = "function small(x){return x+1}function big(x,a,b,c,d,e,f,g){a=x+1;return a}" +
            "{if(NR%2)total+=big($2);else total+=small($2)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1024\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.FunctionFramesPrepared);
        Assert.Equal(1, counters.FunctionSlotBufferAllocations);
        Assert.Equal(0, counters.FunctionActivationSlotVisits);
        Assert.Equal(0, counters.FunctionRetirementSlotVisits);
        Assert.Equal(0, budget.Reserved);
    }
}
