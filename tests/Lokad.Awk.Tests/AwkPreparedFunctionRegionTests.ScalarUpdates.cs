namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, 0, false)] [InlineData(false, 0, true)]
    [InlineData(false, 1, false)] [InlineData(false, 1, true)]
    [InlineData(false, 2, false)] [InlineData(false, 2, true)]
    [InlineData(false, 3, false)] [InlineData(false, 3, true)]
    [InlineData(false, 4, false)] [InlineData(false, 4, true)]
    [InlineData(true, 0, false)] [InlineData(true, 0, true)]
    [InlineData(true, 1, false)] [InlineData(true, 1, true)]
    [InlineData(true, 2, false)] [InlineData(true, 2, true)]
    [InlineData(true, 3, false)] [InlineData(true, 3, true)]
    [InlineData(true, 4, false)] [InlineData(true, 4, true)]
    public async Task CapturedPriorNumbersSurvivePendingRhsUsers(bool governed, int shape, bool cancel)
    {
        var (source, expected) = shape switch
        {
            0 => ("function rhs(){x=100;print \"hold\";return 3}BEGIN{x=7;print (x+=rhs()),x;exit}", "hold\n10 10\n"),
            1 => ("function rhs(x){x=100;print \"hold\";return 3}function f(x){return x++ + rhs(x)}BEGIN{print f(7);exit}", "hold\n10\n"),
            2 => ("function rhs(){x=100;print \"hold\";return 3}BEGIN{x=\"0007\";print x+rhs(),x;exit}", "hold\n10 100\n"),
            3 => ("function rhs(){$0=\"new 100\";print \"hold\";return 3}BEGIN{$0=\"old 0007\";print $2+rhs(),$2;exit}", "hold\n10 100\n"),
            4 => ("function rhs(n){if(n==2){a[\"k\"]=100;print \"hold\"}return n}" +
                "BEGIN{a[\"k\"]=0;$0=\"k\";for(i=1;i<=3;i++)a[$1]+=rhs(i);print a[\"k\"];exit}", "hold\n6\n"),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
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
            Assert.Equal(expected, string.Concat(writes));
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
