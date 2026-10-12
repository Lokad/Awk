namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCooperativeCancellationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultCheckpointsHaveNoImplicitFuelCutoff(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var governor = new AwkExecutionGovernor(AwkRuntimeLimits.CreateDefault().MaxExecutionSteps, counters);
        governor.Check(int.MaxValue, CancellationToken.None);
        governor.Check(int.MaxValue, CancellationToken.None);
        Assert.Equal(observed ? 2L * int.MaxValue : 0, counters.ExecutionSteps);
        Assert.Equal(observed ? 2 : 0, counters.CancellationChecks);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => governor.Check(1, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(observed ? 2L * int.MaxValue : 0, counters.ExecutionSteps);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitMaximumIntQuotaRemainsFinite(bool observed)
    {
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxExecutionSteps(int.MaxValue);
        Assert.Equal(int.MaxValue, policy.MaxExecutionSteps);
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var governor = new AwkExecutionGovernor(AwkRuntimeLimits.FromPolicy(policy).MaxExecutionSteps, counters);
        governor.Check(int.MaxValue, CancellationToken.None);
        var failure = Assert.Throws<AwkRuntimeException>(() => governor.Check(1, CancellationToken.None));
        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(observed ? int.MaxValue : 0, counters.ExecutionSteps);
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.WithMaxExecutionSteps(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.WithMaxExecutionSteps(-1));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task MidflightCancellationStopsLoopsAndDelayedReadsWithFiniteMemory(
        bool executable, bool observed, bool delayed)
    {
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "BEGIN{getline x<\"/input\";while(1){x++}}"),
            CancellationToken.None);
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
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        Assert.Equal(0, limits.MaxExecutionSteps);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility,
            executable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter);
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
    public async Task OwnedDeadlineKeepsFriendlyDiagnosticsWithoutDefaultFuel(bool restricted)
    {
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted() : AwkExecutionPolicy.CreateDefault())
            .WithMaxExecutionMemoryBytes(1 << 20).WithExecutionTimeout(TimeSpan.FromMilliseconds(50));
        Assert.Equal(0, policy.MaxExecutionSteps);
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
