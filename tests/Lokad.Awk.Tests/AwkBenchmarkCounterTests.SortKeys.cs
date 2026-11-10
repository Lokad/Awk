namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, "i")]
    [InlineData(true, "i")]
    [InlineData(false, "\"k\" i")]
    [InlineData(true, "\"k\" i")]
    public async Task NumericValueSortDoesNotMaterializeUnusedKeyText(bool governed, string key)
    {
        const int count = 128;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        var source = "BEGIN{for(i=1;i<=" + count + ";i++)a[" + key + "]=" + count +
            "-i;n=asort(a,b,\"@val_num_asc\");print n,b[1],b[n]}";
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("128 0 127\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        // Building string keys owns their formatting/concatenation; sorting distinct numbers
        // needs no additional language-key text. Allow bounded preparation/output.
        Assert.InRange(counters.AwkTextAllocations, 0, key == "i" ? 16 : 2 * count + 16);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
