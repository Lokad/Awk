namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false, 0)] [InlineData(true, false, 0)]
    [InlineData(false, false, 1)] [InlineData(true, false, 1)]
    [InlineData(false, false, 2)] [InlineData(true, false, 2)]
    [InlineData(false, true, 0)] [InlineData(true, true, 0)]
    [InlineData(false, true, 1)] [InlineData(true, true, 1)]
    [InlineData(false, true, 2)] [InlineData(true, true, 2)]
    public async Task InputDerivedFormatsKeepDecodeCachesWithTheirActualScalarOwner(bool governed, bool output, int scope)
    {
        foreach (var count in new[] { 1, 128 })
        {
            var name = output ? "OFMT" : "CONVFMT";
            var body = output ? "print 1.25" : "total+=length(1.25 \"\")";
            var loop = "for(j=0;j<" + count + ";j++){" + body + "}";
            var finish = output ? "" : "print total;";
            var outside = output ? "print 1.25" : "print 1.25 \"\"";
            var fromArgument = scope == 2;
            var definitions = scope == 0 ? "" : "function work(" + (fromArgument ? "fmt" : "") + "){" +
                (fromArgument ? name + "=fmt;" : "") + loop + "}";
            var source = definitions + "{" + (fromArgument ? "" : name + "=$1;") +
                (scope == 0 ? loop : "work(" + (fromArgument ? "$1" : "") + ")") + ";" + finish + outside + "}";
            var host = new MockFileSystem();
            host.SetStandardInput("%.3f\n");
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(AwkExitCode.Success, exit);
            var inside = output ? string.Concat(Enumerable.Repeat("1.250\n", count)) : (5 * count) + "\n";
            Assert.Equal(inside + "1.250\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(1, counters.StringDecodes);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FormatCachesRespectGlobalFilePhasesWhileCallerLocalsAreHidden(bool governed)
    {
        var host = new MockFileSystem();
        host.AddFile("/left", "%.3f %.2f\n");
        host.AddFile("/right", "next\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        const string source = "function work(fmt,line){first=1.25 \"\";getline line;print 1.25 \"\"}" +
            "BEGINFILE{if(ARGIND==2)CONVFMT=fmt;print 1.25 \"\"}" +
            "{fmt=$1;CONVFMT=$1;work($2);exit}END{print 1.25 \"\"}";
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source, "left", "right")),
            CancellationToken.None, CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1.25\n1.250\n1.250\n1.250\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.StringDecodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

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
