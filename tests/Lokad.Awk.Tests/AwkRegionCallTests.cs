namespace Lokad.Awk.Tests;

public sealed class AwkRegionCallTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task PendingReadKeepsExplicitCallersUntilCompletionOrCancellation(bool governed, bool cancel)
    {
        const string source = "function leaf(x,y){y=(getline value<\"/side\");return x+value}" +
            "function outer(x){return leaf(x)+1}{result=outer($1);print result,$1;next}";
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
