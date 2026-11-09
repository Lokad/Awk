namespace Lokad.Awk.Tests;

public sealed class AwkIterationLifetimeTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task SnapshotAdmissionStopsBeforeBodyAndLaterEffects(bool nested, bool cancel)
    {
        var child = nested ? "[\"child\"]" : "";
        var source = "BEGIN{a" + child + "[\"key\"]=1;print \"ready\";for(k in a" + child +
            ")print \"body\";print \"tail\"}END{print \"end\"}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var reachedAdmission = false;
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            budget.OnGrowth = _ => { reachedAdmission = true; if (cancel) cancellation.Cancel(); };
            if (!cancel) budget.RefuseNextAdmissionMinimumGrowth = 1;
        };
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token);
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        else Assert.Equal(AwkExitCode.FatalError, await run);
        Assert.True(reachedAdmission);
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(cancel ? "" : "awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(16, false, false)] [InlineData(16, false, true)]
    [InlineData(16, true, false)] [InlineData(16, true, true)]
    [InlineData(1024, false, false)] [InlineData(1024, false, true)]
    [InlineData(1024, true, false)] [InlineData(1024, true, true)]
    public async Task AggregationAndAliasedTraversalKeepNativeValuesAndBoundCalls(int keys, bool nested, bool governed)
    {
        var child = nested ? "[\"child\"]" : "";
        var source = "function total(a, k,s){for(k in a" + child + ")s+=a" + child +
            "[k];return s}{data" + child + "[$1]+=$2}END{print total(data)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Range(1, keys).Select(index =>
            index.ToString("D4", System.Globalization.CultureInfo.InvariantCulture) + " 3\n")));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault())
            { MemoryBudget = governed ? budget : null };
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((keys * 3).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.FunctionNameLookups);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        // Cold field spelling and escaped traversal text are bounded by key count.
        Assert.InRange(counters.AwkTextAllocations, 0, 2 * keys + 8);
        Assert.Equal(keys, counters.NumericFieldByteParses);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false, false)] [InlineData(false, false, false, true)]
    [InlineData(false, false, true, false)] [InlineData(false, false, true, true)]
    [InlineData(false, true, false, false)] [InlineData(false, true, false, true)]
    [InlineData(false, true, true, false)] [InlineData(false, true, true, true)]
    [InlineData(true, false, false, false)] [InlineData(true, false, false, true)]
    [InlineData(true, false, true, false)] [InlineData(true, false, true, true)]
    [InlineData(true, true, false, false)] [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)] [InlineData(true, true, true, true)]
    public async Task SnapshotAndCallerSurvivePendingComparerOrBody(
        bool governed, bool nested, bool comparer, bool cancel)
    {
        var child = nested ? "[\"child\"]" : "";
        var source = """
            function compare(a,av,b,bv,t){getline t<"/side";return a<b?-1:a>b}
            function walk(a, k,t){for(k in a
            """ + child + ") {if(!done++){delete data;data[\"fresh\"]=9};" +
            (comparer ? "print k" : "getline t<\"/side\";print k,t") + "}return 7}" +
            "BEGIN{data" + child + "[\"01\"]=1;data" + child + "[\"2\"]=2;" +
            "PROCINFO[\"sorted_in\"]=\"" + (comparer ? "compare" : "@ind_str_asc") +
            "\";print walk(data);print \"tail\"}";
        var host = new MockFileSystem();
        host.AddFile("/side", "0007\n0008\n0009\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (descriptor != AwkFileDescriptor.StdIn)
            {
                using var registration = token.Register(() => canceled.TrySetResult());
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
            }
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false); }
            finally { host.ReadAsyncOverride = Read; }
        }
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            if (cancel)
            {
                cancellation.Cancel();
                await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(run.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? "" : comparer ? "01\n2\n7\ntail\n" : "01 0007\n2 0008\n7\ntail\n",
                host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            release.TrySetResult();
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }
}
