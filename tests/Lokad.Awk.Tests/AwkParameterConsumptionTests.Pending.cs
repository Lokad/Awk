namespace Lokad.Awk.Tests;

public sealed partial class AwkParameterConsumptionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DeferredErrorTextStaysFundedThroughRecordReplacementAndActualPendingRead(bool cancel)
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
            var source = "function replace(){$0=\"new:9\";return 0}" +
                "function choose(x,ignored,line){getline line;if(line)return x+1;return 0}" +
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
                else Assert.Equal(AwkExitCode.Success, await execution);
                Assert.Equal(cancel ? "ready\n" : "ready\n0\nend\n", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
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
