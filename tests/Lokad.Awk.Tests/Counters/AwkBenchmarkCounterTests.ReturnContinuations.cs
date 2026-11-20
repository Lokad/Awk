namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NumericLocalReturnsKeepCanonicalReadsAndFundedFrameReuse(bool governed)
    {
        const int records = 128;
        const string source = "function step(x,a,t){t=(x*3+a)%101;return t}" +
            "BEGIN{for(i=0;i<128;i++)total+=step(i,7);print total}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        var total = Enumerable.Range(0, records).Sum(index => (index * 3 + 7) % 101);
        Assert.Equal(total + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectNumericReturnsAvoidIntermediateValueTransport(bool governed)
    {
        const int records = 128;
        const string source = "function leaf(a,k,v){a[k][\"v\"]+=v;return a[k][\"v\"]}" +
            "function relay(a,k,v){return leaf(a,k,v)}{total+=relay(a,$1,$2)}END{print total,length(a)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Range(0, records).Select(index => "k" + index % 4 + " 1\n")));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("2112 4\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task DirectReturnValueFallbackKeepsStringsAndInputOrigins()
    {
        const string source = "function leaf(x){return x}function relay(x){return leaf(x)}" +
            "BEGIN{print relay(7),relay(\"0007\");$0=\"0009\";v=relay($1);print v,typeof(v)}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters));
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("7 0007\n0009 strnum\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, host.OpenFileCount);
    }

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
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = budget });
            Assert.Equal(0, exit.Value);
            Assert.Equal("1024 strnum 000700000000\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));

            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            observations.Add((budget.ResizeCalls, counters[AwkCounter.StringDecodes], counters[AwkCounter.Utf8Encodes], counters[AwkCounter.TextAllocations]));
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
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = budget });
            Assert.Equal(0, exit.Value);
            Assert.Equal("1024 string 000700000000\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));

            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            observations.Add((budget.ResizeCalls, counters[AwkCounter.StringDecodes], counters[AwkCounter.Utf8Encodes], counters[AwkCounter.TextAllocations]));
        }
        Assert.True(observations[0].Resizes < observations[1].Resizes,
            $"Discarded/used admission callbacks: {observations[0].Resizes}/{observations[1].Resizes}");
        Assert.Equal(observations[1].Decodes, observations[0].Decodes);
        Assert.Equal(observations[1].Encodes, observations[0].Encodes);
        Assert.Equal(observations[1].Texts, observations[0].Texts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalTextResultsPreserveLengthAndAdmission(bool governed)
    {
        const int records = 128;
        const string source = "function clip(s,n,t){t=tolower(s);if(length(t)>n)return substr(t,1,n);return t}" +
            "{v=clip($1,$2);total+=length(v)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Range(0, records)
            .Select(index => index % 2 == 0 ? "ID:beta:tail 4\n" : "ID:beta:tail 32\n")));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1024\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.InRange(counters[AwkCounter.TextAllocations], 0, records * 2 + 32);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData("direct", false)]
    [InlineData("direct", true)]
    [InlineData("assigned", false)]
    [InlineData("assigned", true)]
    [InlineData("forwarded", false)]
    [InlineData("forwarded", true)]
    public async Task TextReturnsPreserveCallerValuesAcrossObservations(string shape, bool governed)
    {
        const int records = 128;
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
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal("1024\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters[AwkCounter.StringDecodes]);
            Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExternalDefinitionsRetainOrdinaryResultPublication(bool option)
    {
        const string source = "function copy(s){return substr(s,1)}{v=copy($1);total+=length(v)}END{print total}";
        string[] arguments = option ? ["-v", "v=previous", source] : [source, "v=previous", "-"];
        var host = new MockFileSystem();
        host.SetStandardInput("0007\nabc\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", arguments), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = budget });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("7\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallResultsKeepFullValuesAcrossObservations(bool governed)
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
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(observed ? "896 string 000700000000\n" : "896\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));

            Assert.Equal(0, counters[AwkCounter.StringDecodes]);
            Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
        }
    }
}
