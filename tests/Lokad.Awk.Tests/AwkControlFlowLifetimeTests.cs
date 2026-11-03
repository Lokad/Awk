namespace Lokad.Awk.Tests;

public sealed class AwkControlFlowLifetimeTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public Task CoprocessInputRejectsOnlyWhenReachedAfterAPendingCall(bool governed, bool reached, bool cancel) =>
        CheckPendingAsync("""
            function effect(){print "bad";return "cat"}
            function run(v){print "before";getline v<"/side";
                if(v){effect()|&getline a[effect()]}print "after"}
            BEGIN{run()}END{print "end"}
            """, reached ? "1\n" : "0\n", "before\n", reached ? "before\n" : "before\nafter\nend\n",
            reached ? 2 : 0, governed, cancel,
            reached ? "awk: coprocesses are unsupported in Lokad awk\n" : "");

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public Task SwitchKeepsFallthroughAndOuterIterationAcrossPendingSelectorOrBody(
        bool governed, bool pendingSelector, bool cancel)
    {
        var source = "function read(v){getline v<\"/side\";return v}function choose(a,k,v){for(k in a){switch(" +
            (pendingSelector ? "read()" : "\"0007\"") + ") {default:print \"bad\";break;" +
            "case /0007/:print \"first\";case \"fall\":delete a;a[\"fresh\"]=2;v=read();print k,v;continue}" +
            "print \"after\"}return 9}BEGIN{a[\"key\"]=1;print choose(a);print \"tail\",length(a)}END{print \"end\"}";
        return CheckPendingAsync(source, pendingSelector ? "0007\n0009\n" : "0009\n",
            pendingSelector ? "" : "first\n", "first\nkey 0009\n9\ntail 1\nend\n", 0, governed, cancel);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task FunctionExitWaitsForItsStatusAndUnwindsIterationBeforeEnd(bool governed, bool cancel) =>
        CheckPendingAsync("""
            function read(v){getline v<"/side";return v}
            function stop(){exit read()}
            BEGIN{a["key"]=1}
            {for(k in a){switch($1){case "stop":stop();default:print "bad"}}print "tail"}
            ENDFILE{print "file-end"}
            END{print "end";exit}
            """, "0007.9\n", "", "end\n", 7, governed, cancel);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SelectorAdmissionStopsBeforeSelectionEffects(bool cancel)
    {
        const string source = "BEGIN{x=\"payload\";print \"ready\";switch(x){case \"payload\":print \"body\";break;default:print \"bad\"};print \"tail\"}END{print \"end\"}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var reached = false;
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            budget.OnGrowth = _ => { reached = true; if (cancel) cancellation.Cancel(); };
            if (!cancel) budget.RefuseNextAdmissionMinimumGrowth = 1;
        };
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token);
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        else Assert.Equal(AwkExitCode.FatalError, await run);
        Assert.True(reached);
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(cancel ? "" : "awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    private static async Task CheckPendingAsync(string source, string side, string before, string expected,
        int status, bool governed, bool cancel, string error = "")
    {
        var host = new MockFileSystem();
        host.SetStandardInput("stop\n");
        host.AddFile("/side", side);
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
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(before, host.GetOutput(AwkFileDescriptor.StdOut));
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
            else Assert.Equal(new AwkExitCode(status), await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? before : expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(cancel ? "" : error, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Empty(host.AwkCommandInvocations);
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
