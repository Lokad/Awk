namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, 0, false)] [InlineData(false, 0, true)]
    [InlineData(false, 1, false)] [InlineData(false, 1, true)]
    [InlineData(false, 2, false)] [InlineData(false, 2, true)]
    [InlineData(false, 3, false)] [InlineData(false, 3, true)]
    [InlineData(false, 4, false)] [InlineData(false, 4, true)]
    [InlineData(false, 5, false)] [InlineData(false, 5, true)]
    [InlineData(false, 6, false)] [InlineData(false, 6, true)]
    [InlineData(false, 7, false)] [InlineData(false, 7, true)]
    [InlineData(false, 8, false)] [InlineData(false, 8, true)]
    [InlineData(false, 9, false)] [InlineData(false, 9, true)]
    [InlineData(false, 10, false)] [InlineData(false, 10, true)]
    [InlineData(false, 11, false)] [InlineData(false, 11, true)]
    [InlineData(true, 0, false)] [InlineData(true, 0, true)]
    [InlineData(true, 1, false)] [InlineData(true, 1, true)]
    [InlineData(true, 2, false)] [InlineData(true, 2, true)]
    [InlineData(true, 3, false)] [InlineData(true, 3, true)]
    [InlineData(true, 4, false)] [InlineData(true, 4, true)]
    [InlineData(true, 5, false)] [InlineData(true, 5, true)]
    [InlineData(true, 6, false)] [InlineData(true, 6, true)]
    [InlineData(true, 7, false)] [InlineData(true, 7, true)]
    [InlineData(true, 8, false)] [InlineData(true, 8, true)]
    [InlineData(true, 9, false)] [InlineData(true, 9, true)]
    [InlineData(true, 10, false)] [InlineData(true, 10, true)]
    [InlineData(true, 11, false)] [InlineData(true, 11, true)]
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
            5 => ("function rhs(n){if(n==2){a[\"k\"]=100;print \"hold\"}return n}" +
                "BEGIN{a[\"k\"]=0;for(i=1;i<=3;i++)a[\"k\"]+=rhs(i);print a[\"k\"];exit}", "hold\n6\n"),
            6 => ("function rhs(n){if(n==2){a[\"k\"]=100;key=\"next\";print \"hold\"}return n}" +
                "BEGIN{a[\"k\"]=0;key=\"k\";for(i=1;i<=3;i++)a[key]+=rhs(i);print a[\"k\"],a[\"next\"];exit}", "hold\n3 3\n"),
            7 => ("function rhs(n){if(n==2){a[\"k:x\"]=100;SUBSEP=\"|\";print \"hold\"}return n}" +
                "BEGIN{SUBSEP=\":\";a[\"k:x\"]=0;for(i=1;i<=3;i++)a[\"k\",\"x\"]+=rhs(i);print a[\"k:x\"],a[\"k|x\"];exit}", "hold\n3 3\n"),
            8 => ("function rhs(p,n){if(n==2){delete p;p[\"k\"]=100;print \"hold\"}return n}" +
                "function bump(p,k,i){for(i=1;i<=3;i++)p[k]+=rhs(p,i)}" +
                "BEGIN{a[\"k\"]=0;bump(a,\"k\");print a[\"k\"];exit}", "hold\n6\n"),
            9 => ("function bump(p,k,i){for(i=1;i<=3;i++){p[k]+=i;" +
                "if(i==1){print \"hold\";delete p[k];p[k]=100;k=\"next\"}}}" +
                "BEGIN{a[\"k\"]=0;bump(a,\"k\");print a[\"k\"],a[\"next\"];exit}", "hold\n100 5\n"),
            10 => ("function rhs(){x=\"0009\";print \"hold\";return 3}BEGIN{x=7;print (x+=rhs()),x;exit}", "hold\n10 10\n"),
            11 => ("function rhs(){print \"hold\";return 3}function f(x){return x+=(x=\"0009\")+rhs()}" +
                "BEGIN{print f(7);exit}", "hold\n19\n"),
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
