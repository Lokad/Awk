namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false, " ")] [InlineData(true, false, " ")]
    [InlineData(false, true, " ")] [InlineData(true, true, " ")]
    [InlineData(false, false, "|")] [InlineData(true, false, "|")]
    [InlineData(false, true, "|")] [InlineData(true, true, "|")]
    public async Task PreparedDemandsSharePrefixWorkWithoutChangingArrayPrograms(bool governed, bool composite, string separator)
    {
        const int records = 128;
        var input = string.Concat(Enumerable.Repeat("key" + separator + "0002" + separator + "part\n", records));
        var literalKey = composite ? "$1,$3" : "$1";
        var dynamicKey = composite ? "$first,$third" : "$first";
        var prepared = await Run(literalKey, "$2");
        var dynamic = await Run(dynamicKey, "$second");
        Assert.Equal(dynamic.Output, prepared.Output);
        Assert.Equal("256 1\n", prepared.Output);
        Assert.True(prepared.Counters.FieldPrefixExtensions < dynamic.Counters.FieldPrefixExtensions);
        Assert.Equal(dynamic.Counters.FieldsMaterialized, prepared.Counters.FieldsMaterialized);
        Assert.Equal(records, prepared.Counters.NumericFieldByteParses);
        Assert.Equal(dynamic.Counters.NumericFieldByteParses, prepared.Counters.NumericFieldByteParses);
        Assert.Equal(0, prepared.Counters.StringDecodes);
        Assert.Equal(0, prepared.Counters.Utf8Encodes);
        Assert.InRange(prepared.Counters.AwkTextAllocations, 0, 32);

        async Task<(string Output, AwkRuntimeCounters Counters)> Run(string key, string value)
        {
            var host = new MockFileSystem();
            host.SetStandardInput(input);
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var source = "BEGIN{first=1;second=2;third=3;FS=\"" + separator + "\"}{sum[" + key + "]+=" + value +
                "}END{for(k in sum)total+=sum[k];print total,length(sum)}";
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(0, exit.Value);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
            return (host.GetOutput(AwkFileDescriptor.StdOut), counters);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PreparedDemandsLeaveRejectedWideRecordsLazy(bool governed)
    {
        const int records = 64;
        var tail = string.Concat(Enumerable.Repeat(" 0007", 63));
        var host = new MockFileSystem();
        host.SetStandardInput("warm" + tail + "\n" + string.Concat(Enumerable.Repeat("skip" + tail + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var source = "NR==1{warm=$64;next}$1==\"keep\"{sum[$1]+=$64}END{print warm}";
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("0007\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.FieldsMaterialized, records, records + 64);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
    }
}
