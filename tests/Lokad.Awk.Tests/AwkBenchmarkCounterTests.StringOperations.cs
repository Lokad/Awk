namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StringOperationsPreserveOwnedFieldsThroughRecordReconstruction(bool governed)
    {
        const int records = 128;
        const string source = """
            {old=$2;a["kept"]=$2;$1="0007"
             found=index($2,$0);from_array=index(a["kept"],$0)
             slice=substr($2,1,2);lower=tolower($2);upper=toupper($2)}
            END{print found,from_array,slice,lower,upper,old,$0}
            """;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("left ABC\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("0 0 AB abc ABC ABC 0007 ABC\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.AwkTextAllocations, 0, records * 5 + 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.GovernedArgumentBufferAllocations);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
