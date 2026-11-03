namespace Lokad.Awk.Tests;

public sealed class AwkRangeLifetimeTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RepeatedRangesKeepFieldPredicatesByteNative(bool governed)
    {
        const int groups = 128;
        const string source = "$1==\"start\",$1==\"end\"{matched++}END{print matched}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("ignored tail\nstart tail\nbody tail\nend tail\n", groups)));
        var budget = new CooperativeTestBudget(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, await program.ExecuteAsync(host, CancellationToken.None, options));
        Assert.Equal("384\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(groups * 4, counters.RecordsRead);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.InRange(counters.AwkTextAllocations, 0, 8);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> PendingRanges()
    {
        foreach (var kind in new[] { "start", "end", "end-next" })
        foreach (var governed in new[] { false, true })
        foreach (var cancel in new[] { false, true }) yield return [kind, governed, cancel];
    }

    [Theory]
    [MemberData(nameof(PendingRanges))]
    public async Task PendingConditionsPreserveInclusiveFlowAndActualUsers(string kind, bool governed, bool cancel)
    {
        var next = kind == "end-next";
        var source = "function probe(v){getline v<\"/side\";" + (next ? "if(v==\"skip\")next;" : "") +
            "return v==\"yes\"}" + (kind == "start" ? "probe(),1" : next ? "NR==1,probe()" : "1,probe()") +
            "{print \"range\",NR}{print \"tail\",NR}";
        var host = new MockFileSystem();
        host.SetStandardInput(next ? "one\ntwo\nthree\n" : "one\ntwo\n");
        host.AddFile("/side", next ? "no\nskip\nyes\n" : "yes\nno\n");
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
            var expected = kind switch
            {
                "start" => "range 1\ntail 1\ntail 2\n",
                "end" => "range 1\ntail 1\nrange 2\ntail 2\n",
                _ => "range 1\ntail 1\nrange 3\ntail 3\n"
            };
            Assert.Equal(cancel ? "" : expected, host.GetOutput(AwkFileDescriptor.StdOut));
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

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReusedArtifactKeepsConcurrentRangesIndependent(bool governed)
    {
        const string source = """
            $1=="A",$1=="Z"{print "one",$1}
            $1=="B",$1=="Y"{split($0,parts);print "two",$1;getline wait<"/gate"}
            """;
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var first = new MockFileSystem();
        first.SetStandardInput("A\nB\n");
        first.AddFile("/gate", "\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (descriptor != AwkFileDescriptor.StdIn)
            {
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
            }
            first.ReadAsyncOverride = null;
            try { return await first.ReadAsync(descriptor, maximum, token).ConfigureAwait(false); }
            finally { first.ReadAsyncOverride = Read; }
        }
        var budget = new CooperativeTestBudget(8 << 20);
        var options = AwkRuntimeOptions.CreateSystemDefault() with { MemoryBudget = governed ? budget : null };
        using var cancellation = new CancellationTokenSource();
        var run = program.ExecuteAsync(first, cancellation.Token, options);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("one A\none B\ntwo B\n", first.GetOutput(AwkFileDescriptor.StdOut));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            var second = new MockFileSystem();
            second.SetStandardInput("plain\n");
            Assert.Equal(AwkExitCode.Success, await program.ExecuteAsync(second, CancellationToken.None, options));
            Assert.Empty(second.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(second.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(held, budget.Reserved);
            release.TrySetResult();
            Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            var third = new MockFileSystem();
            third.SetStandardInput("plain\n");
            Assert.Equal(AwkExitCode.Success, await program.ExecuteAsync(third, CancellationToken.None, options));
            Assert.Empty(third.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(third.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Empty(first.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, first.OpenFileCount);
            Assert.Equal(0, second.OpenFileCount);
            Assert.Equal(0, third.OpenFileCount);
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
