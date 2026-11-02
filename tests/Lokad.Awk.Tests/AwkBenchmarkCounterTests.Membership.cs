namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task CompositeMembershipBorrowsKeysWithoutInsertingMisses(bool governed, bool emptySeparator)
    {
        const int records = 128;
        var source = "BEGIN{" + (emptySeparator ? "SUBSEP=\"\";" : "") + "seen[\"0007\",\"tag\"]=1}" +
            "{hit+=(($1,$2) in seen);miss+=(($1,\"missing\") in seen)}END{print hit,miss,length(seen)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007 tag\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("128 0 1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.FunctionArgumentBufferRents);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
