namespace Lokad.Awk.Tests;

public sealed class AwkRegionFieldTests
{
    public static IEnumerable<object[]> PendingFieldWrites()
    {
        foreach (var operation in new[] { "simple", "compound", "concat", "condition", "multi", "printf", "substr", "index", "sprintf", "membership", "nested-read", "nested-simple", "nested-compound", "nested-membership", "nested-delete" })
        foreach (var cancel in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [operation, cancel, governed];
    }

    [Theory]
    [MemberData(nameof(PendingFieldWrites))]
    public async Task SuspendedFieldsPreserveTargetOrderingTextAndActualPendingOwnership(string operation, bool cancel, bool governed)
    {
        var action = operation switch
        {
            "concat" => "{i=1;text=($i \":\" read());print text,$0,i;next}",
            "condition" => "(i=0)||(read()>0&&!(0||0)){print $0,i;next}",
            "multi" => "{i=1;print $i,read(),$i;next}",
            "printf" => "{i=1;fmt=\"[%s][%d][%s]\\n\";printf fmt,$i,read(),$i;next}",
            "substr" => "{i=1;text=substr($i,read()-10);print text,$0,i;next}",
            "index" => "{i=1;found=index($i,(read()==11?\"0\":\"x\"));print found,$0,i;next}",
            "sprintf" => "{i=1;text=sprintf($i,read());print text,$0,i;next}",
            "membership" => "{i=1;seen[\"0007\",11]=1;found=(($i,read()) in seen);print found,$0,i;next}",
            "nested-read" => "{i=1;a[\"0007\",\"tag\"][11]=\"kept\";text=a[$i,\"tag\"][read()];print text,$0,i;next}",
            "nested-simple" => "{i=1;a[$i,\"tag\"][read()]=\"saved\";print a[\"0007\",\"tag\"][11],$0,i;next}",
            "nested-compound" => "{i=1;a[\"0007\"][\"leaf\"]=7;a[$i][\"leaf\"]+=read();print a[\"0007\"][\"leaf\"],$0,i;next}",
            "nested-membership" => "{i=1;SUBSEP=\":\";a[\"r\"][\"0007:tag\"]=1;found=(($i,\"tag\") in a[(read()>0?\"r\":\"x\")]);print found,$0,i;next}",
            "nested-delete" => "{i=1;a[\"0007\",\"tag\"][11]=1;delete a[$i,\"tag\"][read()];print (11 in a[\"0007\",\"tag\"]),$0,i;next}",
            _ => "{i=1;$i" + (operation == "compound" ? "+=" : "=") + "read();print $0,i;next}"
        };
        var source = "function read(x){getline x<\"/side\";i=2;$1=100;return x}" + action;
        var host = new MockFileSystem();
        host.SetStandardInput("0007 old\n");
        host.AddFile("/side", "11\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (descriptor != AwkFileDescriptor.StdIn)
            {
                using var registration = token.Register(() => canceled.TrySetResult());
                entered.TrySetResult();
                await resume.Task.ConfigureAwait(false);
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
            resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            var expected = operation switch
            {
                "compound" => "18 old 2\n",
                "concat" => "0007:11 100 old 2\n",
                "condition" => "100 old 2\n",
                "multi" => "0007 11 old\n",
                "printf" => "[0007][11][old]\n",
                "substr" or "sprintf" => "0007 100 old 2\n",
                "index" or "membership" or "nested-membership" => "1 100 old 2\n",
                "nested-delete" => "0 100 old 2\n",
                "nested-read" => "kept 100 old 2\n",
                "nested-simple" => "saved 100 old 2\n",
                "nested-compound" => "18 100 old 2\n",
                _ => "100 11 2\n"
            };
            Assert.Equal(cancel ? "" : expected, host.GetOutput(AwkFileDescriptor.StdOut));
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
}
