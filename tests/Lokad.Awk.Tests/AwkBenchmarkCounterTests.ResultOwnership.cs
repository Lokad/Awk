namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExternalDefinitionsRetainOrdinaryResultPublication(bool option)
    {
        const string source = "function copy(s){return substr(s,1)}{v=copy($1);total+=length(v)}END{print total}";
        string[] arguments = option ? ["-v", "v=previous", source] : [source, "v=previous", "-"];
        var host = new MockFileSystem();
        host.SetStandardInput("0007\nabc\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", arguments)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = budget });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("7\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, counters.CallAssignments);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ClosedCallResultsKeepFullValuesWithoutNamedPublication(bool governed)
    {
        const int records = 128;
        foreach (var observed in new[] { false, true })
        {
            var source = "function clip(s,n,t){t=tolower(s);if(length(t)>n)return substr(t,1,n);return t}" +
                "{v=clip($1,$2);x=3;if(NR%2)total+=length(v);else total+=length((v))}" +
                (observed ? "END{print total,typeof(v),v}" : "END{print total}");
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Range(0, records)
                .Select(index => index % 2 == 0 ? "CAFE 2\n" : "000700000000 32\n")));
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(observed ? "896 string 000700000000\n" : "896\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(observed ? records : 0, counters.CallAssignments);
            Assert.Equal(records, counters.BoundFunctionRuns);
            Assert.Equal(0, counters.StringDecodes);
            Assert.Equal(0, counters.Utf8Encodes);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
        }
    }
}
