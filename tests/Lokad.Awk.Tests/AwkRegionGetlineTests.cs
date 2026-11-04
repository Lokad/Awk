namespace Lokad.Awk.Tests;

public sealed class AwkRegionGetlineTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task ReadLoopKeepsStatusAndArgumentsNative(bool governed, bool redirected)
    {
        const int records = 1024;
        var host = new MockFileSystem();
        var input = string.Concat(Enumerable.Repeat("0007\n", records));
        if (redirected) host.AddFile("/side", input);
        else host.SetStandardInput(input);
        var source = "function add(x,y){return x+y}BEGIN{while((getline value" +
            (redirected ? "<\"/side\"" : "") + ")>0)sum=add(sum,value);print sum,value,NR}";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("7168 0007 " + (redirected ? "0\n" : "1024\n"), host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.Utf8Encodes);
        // The one native file name crosses the host boundary; records, numeric
        // status/arguments and original numeric-string spelling stay byte-native.
        Assert.InRange(counters.StringDecodes, 0, redirected ? 1 : 0);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
