namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Fact]
    public async Task TerminalLocalReturnsAvoidDuplicateAdmissionWithoutExtraTextWork()
    {
        const int records = 128;
        var observations = new List<(long Resizes, long Decodes, long Encodes, long Texts)>();
        foreach (var conditional in new[] { false, true })
        {
            var source = "function copy(s){return " + (conditional ? "0?s:s" : "s") +
                "}{v=copy($1);total+=length(v)}END{print total,typeof(v),v}";
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Range(0, records)
                .Select(index => index % 2 == 0 ? "0007\n" : "000700000000\n")));
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = budget });
            Assert.Equal(0, exit.Value);
            Assert.Equal("1024 strnum 000700000000\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(records, counters.BoundFunctionRuns);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            observations.Add((budget.ResizeCalls, counters.StringDecodes, counters.Utf8Encodes, counters.AwkTextAllocations));
        }
        Assert.True(observations[0].Resizes < observations[1].Resizes);
        Assert.Equal(observations[1].Decodes, observations[0].Decodes);
        Assert.Equal(observations[1].Encodes, observations[0].Encodes);
        Assert.Equal(observations[1].Texts, observations[0].Texts);
    }

    [Fact]
    public async Task DiscardedTextReturnsReuseFundingWithoutExtraConversions()
    {
        const int records = 128;
        var observations = new List<(long Resizes, long Decodes, long Encodes, long Texts)>();
        foreach (var used in new[] { false, true })
        {
            var source = "function copy(s){return substr(s,1)}" +
                (used ? "{total+=length(v=copy($1))}" : "{v=copy($1);total+=length(v)}") +
                "END{print total,typeof(v),v}";
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Range(0, records)
                .Select(index => index % 2 == 0 ? "0007\n" : "000700000000\n")));
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = budget });
            Assert.Equal(0, exit.Value);
            Assert.Equal("1024 string 000700000000\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(records, counters.BoundFunctionRuns);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            observations.Add((budget.ResizeCalls, counters.StringDecodes, counters.Utf8Encodes, counters.AwkTextAllocations));
        }
        Assert.True(observations[0].Resizes < observations[1].Resizes,
            $"Discarded/used admission callbacks: {observations[0].Resizes}/{observations[1].Resizes}");
        Assert.Equal(observations[1].Decodes, observations[0].Decodes);
        Assert.Equal(observations[1].Encodes, observations[0].Encodes);
        Assert.Equal(observations[1].Texts, observations[0].Texts);
    }
}
