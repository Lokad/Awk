namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FunctionEntryPreparesAvailableFieldPrefixes(bool governed)
    {
        const int records = 128;
        var prepared = await Run("$1+$2+$3");
        var dynamic = await Run("$first+$second+$third");
        Assert.Equal("768\n", prepared.Output);
        Assert.Equal(prepared.Output, dynamic.Output);
        Assert.True(prepared.Counters.FieldPrefixExtensions < dynamic.Counters.FieldPrefixExtensions);
        Assert.Equal(3 * records, prepared.Counters.NumericFieldByteParses);
        Assert.Equal(dynamic.Counters.NumericFieldByteParses, prepared.Counters.NumericFieldByteParses);
        Assert.Equal(0, prepared.Counters.StringDecodes);
        Assert.Equal(0, prepared.Counters.Utf8Encodes);

        async Task<(string Output, AwkRuntimeCounters Counters)> Run(string expression)
        {
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Repeat("0001 0002 0003\n", records)));
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var source = "function f(){return +" + expression +
                "}BEGIN{first=1;second=2;third=3}{total+=f()}END{print total}";
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
        foreach (var (pattern, prefix) in new[] { ("", ""), ("", "marker=1;"), ("", "if(NR>0)"), ("NR>0", "") })
        {
            var prepared = await Run(literalKey, "$2", pattern, prefix);
            var dynamic = await Run(dynamicKey, "$second", pattern, prefix);
            Assert.Equal(dynamic.Output, prepared.Output);
            Assert.Equal("256 1\n", prepared.Output);
            Assert.True(prepared.Counters.FieldPrefixExtensions < dynamic.Counters.FieldPrefixExtensions);
            Assert.Equal(dynamic.Counters.FieldsMaterialized, prepared.Counters.FieldsMaterialized);
            Assert.Equal(records, prepared.Counters.NumericFieldByteParses);
            Assert.Equal(dynamic.Counters.NumericFieldByteParses, prepared.Counters.NumericFieldByteParses);
            Assert.Equal(0, prepared.Counters.StringDecodes);
            Assert.Equal(0, prepared.Counters.Utf8Encodes);
            Assert.InRange(prepared.Counters.AwkTextAllocations, 0, 32);
        }

        async Task<(string Output, AwkRuntimeCounters Counters)> Run(string key, string value, string pattern, string prefix)
        {
            var host = new MockFileSystem();
            host.SetStandardInput(input);
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            var source = "BEGIN{first=1;second=2;third=3;FS=\"" + separator + "\"}" + pattern + "{" + prefix + "sum[" + key + "]+=" + value +
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
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task FieldPreparationWaitsForActualAppendCompletion(bool governed, bool cancel)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("key 0002\nkey 0002\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, static _ => { }));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new List<string>();
        var writes = 0;
        host.AppendOverride = async (descriptor, bytes, _) =>
        {
            Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
            output.Add(System.Text.Encoding.UTF8.GetString(bytes.Span));
            if (++writes == 2)
            {
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
            }
            return AwkAppendResult.Accepted();
        };
        var source = "{print \"before\";sum[$1]+=$2}END{print sum[\"key\"]}";
        var execution = AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), cancellation.Token,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.Equal(2, counters.FieldsMaterialized);
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.Equal(2, counters.FieldsMaterialized);
        }
        finally { release.TrySetResult(); }
        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Equal("before\nbefore\n", string.Concat(output));
            Assert.Equal(2, counters.FieldsMaterialized);
        }
        else
        {
            Assert.Equal(AwkExitCode.Success, await execution);
            Assert.Equal("before\nbefore\n4\n", string.Concat(output));
            Assert.Equal(4, counters.FieldsMaterialized);
            Assert.True(counters.FieldPrefixExtensions < counters.FieldsMaterialized);
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

}
