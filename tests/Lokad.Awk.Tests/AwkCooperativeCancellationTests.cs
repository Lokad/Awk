namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCooperativeCancellationTests
{

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    public async Task MidflightCancellationStopsLoopsAndDelayedReadsWithFiniteMemory(
        bool observed, bool delayed, bool cachedField)
    {
        var source = cachedField
            ? "BEGIN{$0=\"7\";seed=$1+0;getline x<\"/input\";while(1){x+=($1+0)}}"
            : "BEGIN{getline x<\"/input\";while(1){x++}}";
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<AwkReadBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/input", "1\n");
        host.ReadAsyncOverride = (_, _, token) =>
        {
            entered.TrySetResult(true);
            return delayed ? new(pending.Task.WaitAsync(token))
                : ValueTask.FromResult(AwkReadBuffer.FromContent("1\n"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { }));
        };
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance) with { MemoryBudget = new CooperativeTestBudget(1 << 20) };
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
                if (cachedField) Assert.Equal(1, counters.NumericFieldByteParses);
                Assert.InRange(counters.MemoryPeakLiveBytes, 1, 1 << 20);
            }
        }
        finally
        {
            cancellation.Cancel();
            pending.TrySetResult(AwkReadBuffer.EndOfFile);
            try { await run.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (OperationCanceledException) { }
        }
    }
    [Theory]
    [InlineData("source", false)] [InlineData("source", true)]
    [InlineData("main", false)] [InlineData("main", true)]
    [InlineData("input", false)] [InlineData("input", true)]
    [InlineData("output", false)] [InlineData("output", true)]
    [InlineData("input-pipe", false)] [InlineData("input-pipe", true)]
    [InlineData("output-pipe", false)] [InlineData("output-pipe", true)]
    public async Task CancellationOnAcquisitionClosesEveryAcquiredDescriptor(string kind, bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(1 << 20);
        var closed = new List<AwkFileDescriptor>();
        var host = new TestOperationVisitor([])
        {
            AfterOpenFile = cancellation.Cancel,
            AfterCreatePipe = cancellation.Cancel,
            CloseDescriptorOverride = (descriptor, token) =>
            {
                Assert.False(token.CanBeCanceled);
                closed.Add(descriptor);
                return Task.FromResult(AwkDescriptorCloseResult.Closed());
            }
        };
        var pipe = kind.EndsWith("-pipe", StringComparison.Ordinal);
        if (pipe) host.EnqueueAwkPipeDescriptors(new(new(100), new(101)));
        else host.EnqueueOpenAwkFileDescriptor(100);
        string[] arguments = kind switch
        {
            "source" => ["-f", "/script.awk"],
            "main" => ["{print}", "/data"],
            "input" => ["BEGIN{getline x<\"/data\"}"],
            "output" => ["BEGIN{print \"x\">\"/out\"}"],
            "input-pipe" => ["BEGIN{\"worker a:b\"|getline x}"],
            _ => ["BEGIN{print \"x\"|\"worker a:b\"}"]
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tool.ExecuteAsync(host, policy, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(pipe ? 2 : 1, closed.Count);
        Assert.Equal(closed.Count, closed.Distinct().Count());
        Assert.Contains(new AwkFileDescriptor(100), closed);
        if (pipe) Assert.Contains(new AwkFileDescriptor(101), closed);
        Assert.Empty(host.CommandInvocations);
        Assert.Empty(host.ReadByteCounts);
        Assert.Empty(host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

}
