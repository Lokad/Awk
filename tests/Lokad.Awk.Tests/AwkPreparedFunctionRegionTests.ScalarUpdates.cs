namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task CapturedScalarNumbersSurvivePendingRhsUsers(bool governed, bool local, bool cancel)
    {
        var source = local
            ? "function rhs(x){x=100;print \"hold\";return 3}function f(x){return x++ + rhs(x)}BEGIN{print f(7);exit}"
            : "function rhs(){x=100;print \"hold\";return 3}BEGIN{x=7;print (x+=rhs()),x;exit}";
        if (!Awk.TryParse(AwkTestHarness.BuildInvocation("awk", source), out var command))
            throw new InvalidOperationException("Expected an awk invocation.");
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var host = new TestOperationVisitor([]);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = new List<string>();
        using var cancellation = new CancellationTokenSource();
        host.AppendOverride = async (descriptor, bytes, _) =>
        {
            Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
            writes.Add(System.Text.Encoding.UTF8.GetString(bytes.Span));
            if (writes.Count == 1)
            {
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
            }
            return AwkAppendResult.Accepted();
        };
        var execution = command.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("hold\n", Assert.Single(writes));
            Assert.False(execution.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
        }
        finally { release.TrySetResult(); }
        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Equal("hold\n", Assert.Single(writes));
        }
        else
        {
            Assert.Equal(AwkExitCode.Success, await execution);
            Assert.Equal(local ? "hold\n10\n" : "hold\n10 10\n", string.Concat(writes));
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
