namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecycledCallCellsKeepBindingsWithoutNewSlotBuffersPerCall(bool governed)
    {
        const string source = "function id(x){return x}function touch(a,k,unused){a[k]++;return a[k]}" +
            "function relay(x){return touch(x,\"k\")}BEGIN{for(i=0;i<64;i++){s=id(\"0007\");v=relay(a);n=id(i+0)}print s,v,n,a[\"k\"]}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("0007 64 63 64\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(counters.BoundFunctionRuns, counters.FunctionFramesPrepared);
        Assert.True(counters.FunctionSlotBufferAllocations < counters.FunctionFramesPrepared);
        Assert.True(counters.IndexedLocalArrayArguments > 0);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task CallAssignmentsKeepUsedValuesAndBypassDiscardedNumericTransport(bool governed, bool local, bool used)
    {
        var loop = "x=0;for(i=0;i<64;i++)" + (used ? "total+=(x=step(x));" : "x=step(x);");
        var result = used ? "total" : "x";
        var source = "function step(x){return x+1}" + (local
            ? "function run(x,i,total){" + loop + "return " + result + "}BEGIN{print run()}"
            : "BEGIN{" + loop + "print " + result + "}");
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal(used ? "2080\n" : "64\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.CallAssignments > 0);
        if (used) Assert.Equal(0, counters.CallNumericScratchBypasses);
        else Assert.True(counters.CallNumericScratchBypasses > 0);
        if (local) Assert.True(counters.BoundFrameNumberWrites > 0);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, " ")] [InlineData(true, false, " ")]
    [InlineData(false, true, " ")] [InlineData(true, true, " ")]
    [InlineData(false, false, "|")] [InlineData(true, false, "|")]
    [InlineData(false, true, "|")] [InlineData(true, true, "|")]
    public async Task ArgumentSegmentsShareOnlyAdmittedFieldPrefixes(bool governed, bool earlierSegment, string separator)
    {
        var prepared = await Run("$1", "$2");
        var dynamic = await Run("$first", "$second");
        Assert.Equal("448\n", prepared.Output);
        Assert.Equal(dynamic.Output, prepared.Output);
        Assert.True(prepared.Counters.FieldPrefixExtensions < dynamic.Counters.FieldPrefixExtensions);
        Assert.Equal(dynamic.Counters.FieldsMaterialized, prepared.Counters.FieldsMaterialized);
        Assert.Equal(dynamic.Counters.NumericFieldByteParses, prepared.Counters.NumericFieldByteParses);
        Assert.Equal(dynamic.Counters.AwkTextAllocations, prepared.Counters.AwkTextAllocations);
        Assert.Equal(0, prepared.Counters.StringDecodes);
        Assert.Equal(0, prepared.Counters.Utf8Encodes);

        async Task<(string Output, AwkRuntimeCounters Counters)> Run(string key, string value)
        {
            var source = "function id(x){return x}function add(p,k,v,unused){p[k]+=v}" +
                "BEGIN{FS=\"" + separator + "\";first=1;second=2}{add(a," + key + "," + value +
                (earlierSegment ? ",id(0)" : "") + ")}END{print a[\"key\"]}";
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Repeat("key" + separator + "0007\n", 64)));
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(0, exit.Value);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            return (host.GetOutput(AwkFileDescriptor.StdOut), counters);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PreparedArgumentsForwardArrayAndScalarCellsWithoutTextConversion(bool governed)
    {
        const string source = "function add(p,k,v){p[k]+=v;return p[k]}function call(p,k,v){return add(p,k,v)}BEGIN{for(i=0;i<64;i++)t=call(a,\"key\",i);print t}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var result = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, result.Value);
        Assert.Equal("2016\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(128, counters.BoundFunctionRuns);
        Assert.Equal(64, counters.IndexedLocalArrayArguments);
        Assert.InRange(counters.NumericLocalReads, 64, 256);
        Assert.InRange(counters.AwkTextAllocations, 0, 16);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
