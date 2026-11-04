namespace Lokad.Awk.Tests;

public sealed class AwkSortingContinuationTests
{
    [Theory]
    [InlineData(false, false, false, false)] [InlineData(false, false, false, true)]
    [InlineData(false, false, true, false)] [InlineData(false, false, true, true)]
    [InlineData(false, true, false, false)] [InlineData(false, true, false, true)]
    [InlineData(false, true, true, false)] [InlineData(false, true, true, true)]
    [InlineData(true, false, false, false)] [InlineData(true, false, false, true)]
    [InlineData(true, false, true, false)] [InlineData(true, false, true, true)]
    [InlineData(true, true, false, false)] [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)] [InlineData(true, true, true, true)]
    public async Task PendingComparatorKeepsSnapshotsFramesAndNestedSorts(
        bool governed, bool keys, bool nested, bool cancel)
    {
        var source = """
            function inner(a,av,b,bv){return av-bv}
            function compare(a,av,b,bv,local,t){
                print "compare",result[1];delete data;data["fresh"]=9;
            """ + (nested ? "local[1]=2;local[2]=1;asort(local,local,\"inner\");" : "") +
            "getline t<\"/side\";return av-bv}" +
            "BEGIN{data[\"01\"]=2;data[\"2\"]=1;result[1]=\"keep\";n=" +
            (keys ? "asorti" : "asort") + "(data,result,\"compare\");print n,result[1],result[2],data[\"fresh\"];print \"tail\"}";
        var host = new MockFileSystem();
        host.AddFile("/side", "0007\n");
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
            Assert.Equal("compare keep\n", host.GetOutput(AwkFileDescriptor.StdOut));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
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
            var completed = keys ? "2 2 01 9\ntail\n" : "2 1 2 9\ntail\n";
            Assert.Equal("compare keep\n" + (cancel ? "" : completed), host.GetOutput(AwkFileDescriptor.StdOut));
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

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task AdmissionStopsBeforeComparatorAndPublication(bool keys, bool cancel)
    {
        var source = "function compare(a,av,b,bv){print \"bad\";return av-bv}" +
            "BEGIN{data[1]=2;data[2]=1;result[1]=\"keep\";print \"ready\";" +
            (keys ? "asorti" : "asort") + "(data,result,\"compare\");print \"tail\"}END{print \"end\"}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var admitted = false;
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            budget.OnGrowth = _ => { admitted = true; if (cancel) cancellation.Cancel(); };
            if (!cancel) budget.RefuseNextAdmissionMinimumGrowth = 1;
        };
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token);
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        else Assert.Equal(AwkExitCode.FatalError, await run);
        Assert.True(admitted);
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(cancel ? "" : "awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TraversalComparatorsUseBoundFramesWithoutArgumentBuffers(bool governed)
    {
        const string source = """
            function compare(a,av,b,bv){return av-bv}
            BEGIN{for(i=65;i>=1;i--)data[i]=i;PROCINFO["sorted_in"]="compare";
                for(k in data){if(!n++)first=k;sum+=data[k];last=k}print n,first,last,sum}
            """;
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("65 1 65 2145\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, counters.FunctionNameLookups);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task ComparatorsUseBoundFramesWithoutArgumentBuffers(bool governed, bool keys)
    {
        var source = "function compare(a,av,b,bv){return av-bv}" +
            "BEGIN{for(i=65;i>=1;i--)data[i]=i;" +
            (keys ? "asorti" : "asort") + "(data,result,\"compare\");print length(result),result[1],result[65]}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("65 1 65\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, counters.FunctionNameLookups);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
    }
}
