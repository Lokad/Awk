namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task StringKeysKeepSpellingAndAdmissionAcrossSortCallbacks(bool governed, bool traversal)
    {
        async Task<AwkRuntimeCounters> Run(int count)
        {
            var source = "function compare(i,av,j,bv){return av-bv}BEGIN{for(i=" + count +
                ";i>0;i--)a[i]=i;" + (traversal
                    ? "PROCINFO[\"sorted_in\"]=\"compare\";for(k in a)total+=a[k];print total}"
                    : "print asort(a,b,\"compare\"),b[1],b[" + count + "]}");
            var host = new MockFileSystem();
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
            var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(AwkExitCode.Success, exit);
            var expected = traversal ? (count * (count + 1) / 2) + "\n" : count + " 1 " + count + "\n";
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(1, counters.StringDecodes); // Resolve the callback's source name once.
            Assert.Equal(0, counters.Utf8Encodes);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            return counters;
        }

        var single = await Run(1);
        var repeated = await Run(128);
        Assert.True(repeated.BoundFunctionRuns > 0);
        Assert.Equal(single.TextValueClassifications, repeated.TextValueClassifications);
    }
}
