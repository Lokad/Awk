namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCooperativeCancellationTests
{

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MidflightCancellationStopsLoopsAndDelayedReadsWithFiniteMemory(
bool observed, bool delayed)
    {
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), AwkTestHarness.BuildInvocation("awk", "BEGIN{getline x<\"/input\";while(1){x++}}"), CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/input", "1\n");
        host.ReadLinesAsyncOverride = (_, _, token) =>
        {
            entered.TrySetResult(true);
            return delayed ? pending.Task.WaitAsync(token)
                : Task.FromResult(AwkReadResult.FromContent("1\n"u8.ToArray(), [], AwkReadCompletion.EndOfFile));
        };
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility) with { MemoryBudget = new CooperativeTestBudget(1 << 20) };
        var run = Task.Run(() => compiled.ExecuteAsync(host, cancellation.Token, options));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (!delayed) await Task.Delay(30);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            if (observed)
            {
                Assert.True(counters.CancellationChecks > 0);
                Assert.InRange(counters.MemoryPeakLiveBytes, 1, 1 << 20);
            }
        }
        finally
        {
            cancellation.Cancel();
            pending.TrySetResult(AwkReadResult.EndOfFile);
            try { await run.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedDeadlineKeepsFriendlyDiagnostics(bool restricted)
    {
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted() : AwkExecutionPolicy.CreateDefault()).WithExecutionTimeout(TimeSpan.FromMilliseconds(50));
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{while(1){x++}}"));
        var host = new MockFileSystem();
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exit = await Task.Run(() => tool.ExecuteAsync(host, policy, watchdog.Token)).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: execution timed out\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }
}
