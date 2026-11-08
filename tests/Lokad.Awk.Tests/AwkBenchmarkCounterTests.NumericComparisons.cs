namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ComparisonBranchesKeepUnorderedValuesAcrossPreparationBoundaries(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        for (var padding = 0; padding <= 128; padding++)
        {
            var source = "function choose(x){" + string.Concat(Enumerable.Repeat("work+=0;", padding)) +
                "if(x<0)return 9;return 7}BEGIN{print choose(\"+nan\"+0),choose(-1),choose(0)}";
            var host = new MockFileSystem();
            var counters = AwkRuntimeCounters.CreateEnabled();
            await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
            var exit = await program.ExecuteAsync(host, CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("7 9 7\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters.StringDecodes);
            Assert.Equal(0, counters.Utf8Encodes);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task NumericComparisonsConsumeBoundNumbers(bool local, bool governed)
    {
        const string comparisons = "print(a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b)";
        var source = local
            ? "function compare(a,b){" + comparisons + "}BEGIN{compare(7,9)}"
            : "BEGIN{a=7;b=9;" + comparisons + "}";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("0 1 1 1 0 0\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True((local ? counters.NumericLocalReads : counters.NumericGlobalReads) >= 12);
        Assert.Equal(0, counters.ByteTextComparisons);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MixedComparisonsKeepNumericStringOriginsAndAssignedNf(bool governed)
    {
        const string source = "BEGIN{a=\"20\";b=3;print(a<b),a,typeof(a);" +
            "NF=1.5;x=1.4;print(x<NF),(NF<x);" +
            "NF=\"02\";x=1;print(x<NF),(NF<x),NF,typeof(NF)}" +
            "{a=$1;print(a<b),a,typeof(a)}";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        host.SetStandardInput("0020\n");
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1 20 string\n1 0\n0 1 02 string\n0 0020 strnum\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }
}
