namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData("direct", false)] [InlineData("direct", true)]
    [InlineData("assigned", false)] [InlineData("assigned", true)]
    [InlineData("forwarded", false)] [InlineData("forwarded", true)]
    public async Task ProvedLengthReturnsAvoidUnusedTextWithoutChangingCallerValues(string shape, bool governed)
    {
        const int records = 128;
        var observations = new List<(long Texts, long Payload)>();
        foreach (var generalObserver in new[] { false, true })
        {
            var functions = "function clip(s,n,t){t=tolower(s);if(length(t)>n)return substr(t,1,n);return t}" +
                (shape == "forwarded" ? "function forward(s,n){return clip(s,n)}" : "");
            var callee = shape == "forwarded" ? "forward" : "clip";
            var source = functions + (generalObserver ? "BEGIN{if(0)print " + callee + "(\"ignored\",0)}" : "") +
                (shape == "direct" ? "{total+=length(clip($1,$2))}" :
                    "{v=" + callee + "($1,$2);x=NR+1;if(NR%2)total+=length(v);else total+=length((v))}") +
                "END{print total}";
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Range(0, records)
                .Select(index => index % 2 == 0 ? "ID:beta:tail 4\n" : "ID:beta:tail 32\n")));
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("1024\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters.CallAssignments);
            Assert.Equal(0, counters.StringDecodes);
            Assert.Equal(0, counters.Utf8Encodes);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            observations.Add((counters.AwkTextAllocations, counters.AwkTextPayloadBytes));
        }
        Assert.True(observations[0].Texts < observations[1].Texts);
        Assert.True(observations[0].Payload < observations[1].Payload);
    }
}
