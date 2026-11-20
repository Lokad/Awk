namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecycledCallCellsKeepBindingsWithoutNewSlotBuffersPerCall(bool governed)
    {
        const string source = "function id(x){return x}function touch(a,k,unused){a[k]++;return a[k]}" +
            "function relay(x){return touch(x,\"k\")}BEGIN{for(i=0;i<64;i++){s=id(\"0007\");v=relay(a);n=id(i+0)}print s,v,n,a[\"k\"]}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("0007 64 63 64\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
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
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal(used ? "2080\n" : "64\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, " ")]
    [InlineData(true, false, " ")]
    [InlineData(false, true, " ")]
    [InlineData(true, true, " ")]
    [InlineData(false, false, "|")]
    [InlineData(true, false, "|")]
    [InlineData(false, true, "|")]
    [InlineData(true, true, "|")]
    public async Task ArgumentSegmentsShareOnlyAdmittedFieldPrefixes(bool governed, bool earlierSegment, string separator)
    {
        var prepared = await Run("$1", "$2");
        var dynamic = await Run("$first", "$second");
        Assert.Equal("448\n", prepared.Output);
        Assert.Equal(dynamic.Output, prepared.Output);

        Assert.Equal(dynamic.Counters[AwkCounter.FieldsPrepared], prepared.Counters[AwkCounter.FieldsPrepared]);

        Assert.Equal(dynamic.Counters[AwkCounter.TextAllocations], prepared.Counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, prepared.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, prepared.Counters[AwkCounter.Utf8Encodes]);

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
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(0, exit.Value);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            return (host.GetOutput(AwkFileDescriptor.StdOut), counters);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparedArgumentsForwardArrayAndScalarCellsWithoutTextConversion(bool governed)
    {
        const string source = "function add(p,k,v){p[k]+=v;return p[k]}function call(p,k,v){return add(p,k,v)}BEGIN{for(i=0;i<64;i++)t=call(a,\"key\",i);print t}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var result = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, result.Value);
        Assert.Equal("2016\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.InRange(counters[AwkCounter.TextAllocations], 0, 16);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    public static IEnumerable<object[]> SharedArgumentBindings()
    {
        foreach (var shape in new[] { "global", "local", "array", "omitted-array" })
            foreach (var withSwitch in new[] { false, true })
                foreach (var governed in new[] { false, true }) yield return [shape, withSwitch, governed];
    }

    [Theory]
    [MemberData(nameof(SharedArgumentBindings))]
    public async Task CallsAcrossSwitchBranchesShareGuardedReferenceBindings(string shape, bool withSwitch, bool governed)
    {
        const int records = 128;
        var boundary = withSwitch ? "switch(0){default:break}" : "";
        var source = shape switch
        {
            "global" => "function leaf(x,y){" + boundary + "return x+y}{x=$1+0;total+=leaf(x,1)}END{print total}",
            "local" => "function leaf(x,y){" + boundary + "return x+y}function outer(x){return leaf(x,1)}{total+=outer($1)}END{print total}",
            "array" => "function leaf(a,k,v){" + boundary + "a[k]+=v;return a[k]}function outer(a,k,v){return leaf(a,k,v)}{total+=outer(values,$1,$2)}END{print total,values[\"key\"]}",
            _ => "function leaf(a,k,v){" + boundary + "a[k]+=v;return a[k]}function outer(k,v,a){return leaf(a,k,v)}{total+=outer($1,$2)}END{print total,(\"a\" in SYMTAB)}"
        };
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat(shape is "global" or "local" ? "0007\n" : "key 2\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var result = await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal(shape switch { "array" => "16512 256\n", "omitted-array" => "256 0\n", _ => "1024\n" },
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(65, false)]
    [InlineData(65, true)]
    public async Task BoundFrameNumbersKeepNativeReadsAndOwnedCleanup(int slots, bool governed)
    {
        const int records = 128;
        var parameters = string.Join(",", Enumerable.Range(0, slots).Select(index => "p" + index));
        var source = "function step(" + parameters + "){p0+=1;p0+=2;" +
            (slots > 1 ? "p1=1;p1+=2;return p0+(p1-3)}" : "return p0}") +
            "{total+=step($2)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault())
        { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1280\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AlternatingPreparedAritiesReuseFundedLocalCapacity(bool governed)
    {
        const int records = 128;
        const string source = "function small(x){return x+1}function big(x,a,b,c,d,e,f,g){a=x+1;return a}" +
            "{if(NR%2)total+=big($2);else total+=small($2)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault())
        { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1024\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComputedArgumentValuesKeepNumbersSpellingAndAdmission(bool governed)
    {
        const string source = "function inner(x){return x+1}function capture(x,y,z){return x+y+z}" +
            "function preserve(x){return x}BEGIN{for(i=0;i<128;i++)total+=capture(inner(i),int(i),NR);" +
            "print total;print preserve(\"0007\")}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var exit = await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("16384\n0007\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.InRange(counters[AwkCounter.TextAllocations], 0, 4);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false, false, 5)]
    [InlineData(false, true, 5)]
    [InlineData(true, false, 5)]
    [InlineData(true, true, 5)]
    [InlineData(false, false, 9)]
    [InlineData(false, true, 9)]
    [InlineData(true, false, 9)]
    [InlineData(true, true, 9)]
    [InlineData(false, false, 65)]
    [InlineData(false, true, 65)]
    [InlineData(true, false, 65)]
    [InlineData(true, true, 65)]
    public async Task WideCallsKeepNativeArgumentsAcrossSwitchBranches(bool governed, bool withSwitch, int arity)
    {
        const int records = 128;
        var parameters = string.Join(',', Enumerable.Range(0, arity).Select(i => "p" + i));
        var statements = string.Concat(Enumerable.Range(0, arity).Select(i => "s+=p" + i + ";"));
        var source = "function sum(" + parameters + ",s){"
            + (withSwitch ? "switch(0){default:break}" : "")
            + statements + "return s}{total+=sum("
            + string.Join(',', Enumerable.Repeat("$1", arity)) + ")}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var exit = await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((records * arity * 7) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
