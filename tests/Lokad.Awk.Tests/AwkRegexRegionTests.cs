namespace Lokad.Awk.Tests;

public sealed class AwkRegexRegionTests
{
    [Theory]
    [InlineData("field", false)] [InlineData("field", true)]
    [InlineData("rule", false)] [InlineData("rule", true)]
    [InlineData("expression", false)] [InlineData("expression", true)]
    public async Task LiteralMatchesKeepNativeFieldAndRecordInputs(string shape, bool governed)
    {
        const int records = 128;
        var source = shape switch
        {
            "field" => "{$1~/^needle$/&&++matched}END{print matched}",
            "rule" => "/^needle/{matched++}END{print matched}",
            _ => "{matched+=(/^needle/)}END{print matched}"
        };
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("needle tail\n", records)));
        var budget = new CooperativeTestBudget(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)
            { MemoryBudget = governed ? budget : null };
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        Assert.Equal(AwkExitCode.Success, await program.ExecuteAsync(host, CancellationToken.None, options));
        Assert.Equal("128\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.InRange(counters.AwkTextAllocations, 0, 8);
        Assert.InRange(counters.RegexCompiles, 0, 1);
        Assert.Equal(records, counters.RegexMatches);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> DynamicInputs()
    {
        foreach (var shape in new[] { "field", "record", "number" })
        foreach (var governed in new[] { false, true })
        foreach (var cancel in new[] { false, true })
        foreach (var sortPrelude in new[] { false, true }) yield return [shape, governed, cancel, sortPrelude];
    }

    [Theory]
    [MemberData(nameof(DynamicInputs))]
    public async Task DynamicPatternsKeepTheInputValueAcrossPendingMutation(string shape, bool governed, bool cancel, bool sortPrelude)
    {
        var input = shape switch { "field" => "$1", "record" => "$0", _ => "0.125" };
        var source = "function pattern(v){$0=\"changed tail\";CONVFMT=\"%.1f\";getline v<\"/side\";return v}" +
            "function run(tmp){" + (sortPrelude ? "asort(tmp);" : "") +
            "print(" + input + "~pattern()),$0,CONVFMT;print \"tail\"}" +
            "BEGIN{CONVFMT=\"%.3f\";$0=\"old tail\";run()}";
        var host = new MockFileSystem();
        host.AddFile("/side", shape switch { "field" => "^old$\n", "record" => "^old tail$\n", _ => "^0[.]125$\n" });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            using var registration = token.Register(() => canceled.TrySetResult());
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
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
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
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
            Assert.Equal(cancel ? "" : (shape == "number" ? "0" : "1") + " changed tail %.1f\ntail\n",
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
