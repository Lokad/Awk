namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData("column", false)] [InlineData("column", true)]
    [InlineData("column+0", false)] [InlineData("column+0", true)]
    [InlineData("select(column)", false)] [InlineData("select(column)", true)]
    public async Task ComputedFieldsReuseNumericCachesAndBorrowedArrayKeys(string index, bool governed)
    {
        const int records = 1024;
        var source = "function select(n){return n}{column=1;total+=$((" + index + "))+$column;sum[$column]+=$2}END{print total,sum[\"0007\"],length(sum)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007 3 trailing fields\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("14336 3072 1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.BoundRegionRuns);
        Assert.Equal(2 * records, counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
