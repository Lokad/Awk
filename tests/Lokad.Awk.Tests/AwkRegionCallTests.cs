namespace Lokad.Awk.Tests;

public sealed class AwkRegionCallTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task PendingRangeConditionOrdersItsActionAndLaterRules(bool governed, bool matches, bool cancel)
    {
        const string source = """
            function read(v){getline v<"/side";return v}
            read()=="yes",0 {print "match"}
            {print "tail";next}
            """;
        var host = new MockFileSystem();
        host.SetStandardInput("row\n");
        host.AddFile("/side", matches ? "yes\n" : "no\n");
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
            Assert.Equal(cancel ? "" : matches ? "match\ntail\n" : "tail\n", host.GetOutput(AwkFileDescriptor.StdOut));
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
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task NumericAndValueReturnsKeepPendingOwnersAndOriginalSpelling(bool governed, bool legacyBody, bool cancel)
    {
        // The splitting primitive is still an explicit migration boundary. This
        // covers its pending callee lifetime until that primitive joins the driver.
        var source = "function read(v,scratch){" + (legacyBody ? "split(\"\",scratch);" : "") +
            "getline v<\"/side\";return v}{sum+=read();saved=read();print sum,saved;next}";
        var host = new MockFileSystem();
        host.SetStandardInput("row\n");
        host.AddFile("/side", "0007\n0011\n");
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
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        var run = AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)),
            cancellation.Token, options);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
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
            Assert.Equal(cancel ? "" : "7 0011\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters.FunctionArgumentBufferRents);
            Assert.Equal(0, counters.Utf8Encodes);
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
    [InlineData(false)] [InlineData(true)]
    public async Task NumericGlobalArgumentsUseTheirBoundLocationWithoutIntermediateValues(bool governed)
    {
        const int records = 1024;
        const string source = "function inc(p){return p+1}BEGIN{x=7}{v=inc(x)}END{print v}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("row\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)
            { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)),
            CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("8\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.BoundFunctionRuns);
        Assert.True(counters.NumericGlobalReads >= records);
        // Factor out ordinary input/header value creation; the argument must not
        // introduce another value construction for every record.
        var control = new MockFileSystem();
        control.SetStandardInput(string.Concat(Enumerable.Repeat("row\n", records)));
        var controlCounters = AwkRuntimeCounters.CreateEnabled();
        var controlOptions = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            controlCounters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)
            { MemoryBudget = governed ? budget : null };
        var controlExit = await AwkExecutor.ExecuteAsync(control,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", "BEGIN{x=7}{v=8}END{print v}")),
            CancellationToken.None, controlOptions);
        Assert.Equal(AwkExitCode.Success, controlExit);
        Assert.Equal("8\n", control.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(control.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.ValuesCreated, 0, controlCounters.ValuesCreated + 16);
        Assert.InRange(counters.AwkTextAllocations, 0, 16);
        Assert.Equal(0, counters.FunctionArgumentBufferRents);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, true, false)]
    [InlineData(true, false, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(false, true, true)]
    [InlineData(true, false, true)] [InlineData(true, true, true)]
    public async Task PendingReadKeepsExplicitCallersUntilCompletionOrCancellation(bool governed, bool cancel, bool legacyRoot)
    {
        var source = "function leaf(x,y){y=(getline value<\"/side\");return x+value}" +
            "function outer(x){return and(x,leaf(x))+17}" + (legacyRoot
                ? "BEGIN{$0=\"0007 row\";result=outer($1);print result,$1;exit}"
                : "{result=outer($1);print result,$1;next}");
        var host = new MockFileSystem();
        host.SetStandardInput("0007 row\n");
        host.AddFile("/side", "11\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (descriptor != AwkFileDescriptor.StdIn)
            {
                entered.TrySetResult();
                await resume.Task.WaitAsync(token).ConfigureAwait(false);
            }
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false); }
            finally { host.ReadAsyncOverride = Read; }
        }
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)
            { MemoryBudget = governed ? budget : null };
        var run = AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)),
            cancellation.Token, options);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(2, counters.BoundFunctionRuns);
            Assert.Equal(0, counters.FunctionArgumentBufferRents);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            else resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? "" : "19 0007\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecursiveCallsUseExplicitContinuationsAndRefundFrames(bool governed)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("7\n");
        const string source = "function sum(n,x){if(n==0)return x;return sum(n-1,x)+1}{answer=sum(900,$1)}END{print answer}";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)
            { MemoryBudget = governed ? budget : null };
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)),
            CancellationToken.None, options);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("907\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(901, counters.BoundFunctionRuns);
        Assert.Equal(0, counters.FunctionStackSuspensions);
        Assert.Equal(0, counters.FunctionArgumentBufferRents);
        Assert.Equal(0, budget.Reserved);
    }
}
