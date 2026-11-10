namespace Lokad.Awk.Tests;

public sealed partial class AwkParameterConsumptionTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public Task OwnedKeysStayFundedUntilActualPendingCallCompletion(bool cancel, bool forward) =>
        PendingOwnedArgument(cancel, forward, text: false);

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public Task OwnedTextStaysFundedUntilActualPendingCallCompletion(bool cancel, bool forward) =>
        PendingOwnedArgument(cancel, forward, text: true);

    private static async Task PendingOwnedArgument(bool cancel, bool forward, bool text)
    {
        const int payload = 32_768;
        var character = text ? 'T' : 'k';
        var shortHeld = await Run(character.ToString());
        var longHeld = await Run(new string(character, payload));
        Assert.True(longHeld - shortHeld >= payload);

        async Task<long> Run(string key)
        {
            var firstRecord = key + ":seed\n";
            var host = new MockFileSystem();
            host.SetStandardInput(firstRecord + "0\n");
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var reads = 0;
            host.ReadAsyncOverride = Read;
            async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
            {
                if (++reads > 1)
                {
                    using var registration = token.Register(() => canceled.TrySetResult());
                    entered.TrySetResult();
                    await resume.Task.ConfigureAwait(false);
                }
                host.ReadAsyncOverride = null;
                var result = await host.ReadAsync(descriptor,
                    reads == 1 ? Math.Min(maximum, firstRecord.Length) : maximum, token).ConfigureAwait(false);
                if (reads == 1) host.ReadAsyncOverride = Read;
                return result;
            }
            var source = text ? "function save(s,line){$0=\"new:9\";getline line;return substr(s,1,1)}" +
                (forward ? "function relay(s){return save(s)}" : "") +
                "BEGIN{FS=\":\";getline;print \"ready\";print " + (forward ? "relay" : "save") +
                "($1);exit}END{print \"end\"}" :
                "function save(a,k,line){$0=\"new:9\";getline line;a[k]++;return a[k]}" +
                (forward ? "function relay(a,k){return save(a,k)}" : "") +
                "BEGIN{FS=\":\";getline;print \"ready\";print " + (forward ? "relay" : "save") +
                "(a,$1);exit}END{print \"end\"}";
            var budget = new CooperativeTestBudget(8 << 20);
            using var cancellation = new CancellationTokenSource();
            var execution = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
                .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.False(execution.IsCompleted);
                var held = budget.Reserved;
                if (cancel)
                {
                    cancellation.Cancel();
                    await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    Assert.False(execution.IsCompleted);
                    Assert.Equal(held, budget.Reserved);
                }
                resume.TrySetResult();
                if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
                else Assert.Equal(AwkExitCode.Success, await execution);
                Assert.Equal(cancel ? "ready\n" : text ? "ready\nT\nend\n" : "ready\n1\nend\n", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
                Assert.Equal(0, host.OpenFileCount);
                Assert.Equal(0, budget.Reserved);
                return held;
            }
            finally
            {
                cancellation.Cancel();
                resume.TrySetResult();
                try { await execution; } catch (OperationCanceledException) { }
            }
        }
    }

    [Fact]
    public async Task RefusedKeyConstructionStopsEffectsAndLeavesTheSharedBudgetReusable()
    {
        const int payload = 32_768;
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        host.SetStandardInput(new string('k', payload) + "\n");
        budget.OnGrowth = _ =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) == "ready\n")
                budget.RefuseNextAdmissionMinimumGrowth = payload;
        };
        const string source = "function save(a,k){a[k]++;print \"late\" > \"result\";return a[k]}" +
            "BEGIN{getline;print \"ready\";print save(a,$1)}END{print \"end\"}";
        var exit = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.False(host.FileExists("/result"));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);

        budget.OnGrowth = null;
        budget.RefuseNextAdmissionMinimumGrowth = null;
        var nextHost = new MockFileSystem();
        var next = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"next\"}"))
            .ExecuteAsync(nextHost, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, next);
        Assert.Equal("next\n", nextHost.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(nextHost.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public Task RefusedMixedInputTextStopsTheCallBeforeEffectsAndRefundsItsOwners() => RefusedText(false);

    [Fact]
    public Task RefusedComparisonInputTextStopsTheCallBeforeEffectsAndRefundsItsOwners() => RefusedText(true);

    private static async Task RefusedText(bool comparison)
    {
        const int payload = 32_768;
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        host.SetStandardInput(new string('s', payload) + " 0002 0003\n");
        budget.OnGrowth = _ =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) == "ready\n")
                budget.RefuseNextAdmissionMinimumGrowth = payload;
        };
        var source = "function part(s,p,n){print \"late\" > \"result\";" +
            (comparison ? "if(length(s)>p)" : "") + "return substr(s,p,n)}" +
            "BEGIN{getline;print \"ready\";print part($1,$2,$3)}END{print \"end\"}";
        var exit = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.False(host.FileExists("/result"));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    [InlineData(false, 3)] [InlineData(true, 3)]
    [InlineData(false, 4)] [InlineData(true, 4)]
    public async Task DeferredErrorTextStaysFundedThroughRecordReplacementAndActualPendingRead(bool cancel, int consumption)
    {
        const int payload = 32_768;
        var numeric = await Run("0007");
        var deferred = await Run("0x1.5");
        Assert.True(deferred - numeric >= payload / 2);

        async Task<long> Run(string suffix)
        {
            var firstRecord = new string(' ', payload) + suffix + ":seed\n";
            var host = new MockFileSystem();
            host.SetStandardInput(firstRecord + "0\n");
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var reads = 0;
            host.ReadAsyncOverride = Read;
            async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
            {
                if (++reads > 1)
                {
                    using var registration = token.Register(() => canceled.TrySetResult());
                    entered.TrySetResult();
                    await resume.Task.ConfigureAwait(false);
                }
                host.ReadAsyncOverride = null;
                var result = await host.ReadAsync(descriptor,
                    reads == 1 ? Math.Min(maximum, firstRecord.Length) : maximum, token).ConfigureAwait(false);
                if (reads == 1) host.ReadAsyncOverride = Read;
                return result;
            }
            var source = consumption == 2
                ? "function later(line){$0=\"new:9\";getline line;return 0}" +
                  "BEGIN{FS=\":\";getline;print \"ready\";print and($1,later());exit}END{print \"end\"}"
                : "function replace(){$0=\"new:9\";return 0}" +
                "function choose(x,ignored,line){getline line;if(line)return " +
                (consumption == 4 ? "x<8" : consumption == 3 ? "substr(\"abcdef\",x,1)" : consumption == 1 ? "int(x)" : "x+1") + ";return 0}" +
                "BEGIN{FS=\":\";getline;print \"ready\";print choose($1,replace());exit}END{print \"end\"}";
            var budget = new CooperativeTestBudget(8 << 20);
            using var cancellation = new CancellationTokenSource();
            var command = AwkTestHarness.BuildInvocation("awk", "--non-decimal-data", source);
            var execution = AwkTestHarness.ParseAwk(command).ExecuteAsync(host,
                AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.False(execution.IsCompleted);
                var retained = budget.Reserved;
                Assert.True(retained > 0);
                if (cancel)
                {
                    cancellation.Cancel();
                    await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    Assert.False(execution.IsCompleted);
                    Assert.Equal(retained, budget.Reserved);
                }
                resume.TrySetResult();
                if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
                var error = consumption == 2 && suffix == "0x1.5";
                if (!cancel) Assert.Equal(error ? 2 : 0, (await execution).Value);
                Assert.Equal(cancel || error ? "ready\n" : "ready\n0\nend\n", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.Equal(!cancel && error ? "awk: hexadecimal floating point constants require an exponent\n" : "",
                    host.GetOutput(AwkFileDescriptor.StdErr));
                Assert.Equal(0, host.OpenFileCount);
                Assert.Equal(0, budget.Reserved);
                return retained;
            }
            finally
            {
                cancellation.Cancel();
                resume.TrySetResult();
                try { await execution; } catch (OperationCanceledException) { }
            }
        }
    }
}
