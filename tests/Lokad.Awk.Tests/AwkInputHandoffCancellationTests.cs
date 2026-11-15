namespace Lokad.Awk.Tests;

public sealed class AwkInputHandoffCancellationTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task CanceledInputCannotPublishEffectsAndKeepsPendingHostLifetime(bool getline, bool pending, bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(4 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<AwkReadBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/input", "row\n");
        host.ReadAsyncOverride = (_, _, _) =>
        {
            entered.TrySetResult();
            if (pending) return new(completion.Task);
            cancellation.Cancel();
            return ValueTask.FromResult(Buffer());
        };
        var source = getline ? "BEGIN{getline x;print \"after\"}END{print \"end\"}" : "{print \"after\"}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source, "/input"));
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = tool.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (pending)
            {
                cancellation.Cancel();
                Assert.False(run.IsCompleted);
                Assert.True(host.OpenFileCount > 0);
                if (governed) Assert.True(budget.Reserved > 0);
                completion.SetResult(Buffer());
            }
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally { completion.TrySetResult(AwkReadBuffer.EndOfFile); }

        static AwkReadBuffer Buffer() => AwkReadBuffer.FromContent("row\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, static _ => { });
    }
}
