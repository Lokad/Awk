namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCooperativeCancellationTests
{
    [Theory]
    [InlineData("and")] [InlineData("or")] [InlineData("xor")]
    public void LongNumericPrimitiveObservesCancellationWithEvaluatedArguments(string name)
    {
        using var cancellation = new CancellationTokenSource();
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            cancellation.Token, AwkRuntimeOptions.CreateSystemDefault());
        Assert.True(AwkBuiltins.TryGetCallable(name, out var definition));
        Assert.True(definition.TryGetScalarComputation(out var implementation));
        var values = Enumerable.Repeat(AwkValue.FromNumber(1), 4096).ToArray();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            implementation.EvaluateBound(values, 0, values.Length, new(state)));
    }

    [Theory]
    [InlineData(false, false, "scalar")]
    [InlineData(false, true, "scalar")]
    [InlineData(true, false, "scalar")]
    [InlineData(true, true, "scalar")]
    [InlineData(false, false, "cached-field")]
    [InlineData(true, false, "cached-field")]
    [InlineData(false, false, "new-field")]
    [InlineData(true, false, "new-field")]
    [InlineData(false, false, "text")]
    [InlineData(true, false, "text")]
    [InlineData(false, false, "calls")]
    [InlineData(true, false, "calls")]
    [InlineData(false, false, "recursive-calls")]
    [InlineData(true, false, "recursive-calls")]
    public async Task MidflightCancellationStopsLoopsAndDelayedReadsWithFiniteMemory(
        bool observed, bool delayed, string values)
    {
        var source = values switch
        {
            "cached-field" => "BEGIN{$0=\"7\";seed=$1+0;getline x<\"/input\";while(1){x+=($1+0)}}",
            "new-field" => "BEGIN{getline x<\"/input\";while(1){$0=\"0007\";total+=$1+0}}",
            "text" => "BEGIN{getline x<\"/input\";while(1){x=\"0007\";total+=x+0}}",
            "calls" => "function f(x){return x+1}BEGIN{getline x<\"/input\";while(1){x=f(f(x))}}",
            "recursive-calls" => "function f(n){return n?f(n-1):1}BEGIN{getline x<\"/input\";while(1){x+=f(64)}}",
            _ => "BEGIN{getline x<\"/input\";while(1){x++}}"
        };
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
        var budget = new CooperativeTestBudget(1 << 20);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance) with { MemoryBudget = budget };
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
            Assert.Equal(0, budget.Reserved);
            if (observed)
            {
                Assert.True(counters.CancellationChecks > 0);
                if (values == "cached-field") Assert.Equal(1, counters.NumericFieldByteParses);
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
