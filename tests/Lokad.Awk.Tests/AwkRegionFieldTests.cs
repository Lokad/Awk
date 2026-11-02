namespace Lokad.Awk.Tests;

public sealed class AwkRegionFieldTests
{
    public static IEnumerable<object[]> PendingFieldWrites()
    {
        foreach (var compound in new[] { false, true })
        foreach (var cancel in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [compound, cancel, governed];
    }

    [Theory]
    [MemberData(nameof(PendingFieldWrites))]
    public async Task SuspendedFieldWritesPreserveTargetOrderingAndActualPendingOwnership(bool compound, bool cancel, bool governed)
    {
        var source = "function read(x){getline x<\"/side\";i=2;$1=100;return x}" +
            "{i=1;$i" + (compound ? "+=" : "=") + "read();print $0,i;next}";
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
            Assert.Equal(1, counters.BoundRegionRuns);
            Assert.Equal(1, counters.BoundFunctionRuns);
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
            Assert.Equal(cancel ? "" : compound ? "18 old 2\n" : "100 11 2\n", host.GetOutput(AwkFileDescriptor.StdOut));
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
