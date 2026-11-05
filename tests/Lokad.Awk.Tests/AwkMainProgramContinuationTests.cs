namespace Lokad.Awk.Tests;

public sealed class AwkMainProgramContinuationTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task RulesRetainCapturedUpdatesThroughPendingCalls(bool governed, bool cancel)
    {
        const string source = """
            function read(t){getline t<"/side";return t}
            {a[$1]+=$2;keep=$0;print "ready",$1}
            {a[$1]+=read()}
            {print keep,a[$1]}
            END{print "done"}
            """;
        var host = new MockFileSystem();
        host.SetStandardInput("a 0007\nb 8\n");
        host.AddFile("/side", "3\n4\n");
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
            Assert.Equal("ready a\n", host.GetOutput(AwkFileDescriptor.StdOut));
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
            Assert.Equal("ready a\n" + (cancel ? "" : "a 0007 10\nready b\nb 8 12\ndone\n"),
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
