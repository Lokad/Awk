namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CapturedArrayValueSurvivesPendingKeyOutputAndCancellation(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(4 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stdout = "";
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (_, bytes, _) =>
            {
                var captured = bytes.ToArray();
                stdout += AwkUtf8Codec.Decode(bytes.Span);
                if (stdout == "held\n")
                {
                    entered.TrySetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual(captured));
                }
                return AwkAppendResult.Accepted();
            }
        };
        var script = "function key(){print \"held\";original=\"changed\";return \"k\"}" +
            "BEGIN{a[key()]=original;print length(a[\"k\"])}";
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["-v", "original=" + new string('x', 8192), script], []);
        var run = AwkTestHarness.ParseAwk(invocation).ExecuteAsync(host,
            AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(budget.Reserved > 0);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(0, (await run).Value);
            Assert.Equal(cancel ? "held\n" : "held\n8192\n", stdout);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
