namespace Lokad.Awk.Tests;

public sealed class AwkMemoryBudgetExecutionTests
{
    [Fact]
    public async Task RestrictedArrayGrowthFailsCleanlyAndTheBudgetCanBeReused()
    {
        var budget = AwkMemoryBudget.CreateFixed(1 << 20);
        var failures = new List<Exception>();
        var policy = AwkExecutionPolicy.CreateRestricted(budget).WithInternalFailureObserver(failures.Add);
        var host = new MockFileSystem();
        var growing = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "BEGIN{for(i=0;i<1000000;i++)a[i]=i;print \"after\"}"));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal(AwkExitCode.FatalError, await growing.ExecuteAsync(host, policy, deadline.Token));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(failures);
        Assert.True(budget.PeakBytes > 0);
        Assert.InRange(budget.PeakBytes, 1, budget.LimitBytes);
        Assert.Equal(0, budget.ReservedBytes);

        var nextHost = new MockFileSystem();
        var next = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"reused\"}"));
        Assert.Equal(AwkExitCode.Success, await next.ExecuteAsync(nextHost, policy, deadline.Token));
        Assert.Equal("reused\n", nextHost.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", nextHost.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.ReservedBytes);
        Assert.Empty(failures);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvocationBudgetTakesPrecedenceOverTheRestrictedPolicy(bool refuseInvocation)
    {
        var invocationBudget = AwkMemoryBudget.CreateFixed(refuseInvocation ? 0 : 1 << 20);
        var policyBudget = AwkMemoryBudget.CreateFixed(refuseInvocation ? 1 << 20 : 0);
        var host = new MockFileSystem();
        var invocation = AwkTestHarness.BuildInvocation("awk", "BEGIN{print 7}").WithMemoryBudget(invocationBudget);
        var result = await AwkTestHarness.ParseAwk(invocation).ExecuteAsync(host,
            AwkExecutionPolicy.CreateRestricted(policyBudget), CancellationToken.None);
        Assert.Equal(refuseInvocation ? AwkExitCode.FatalError : AwkExitCode.Success, result);
        Assert.Equal(refuseInvocation ? "" : "7\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(refuseInvocation ? "awk: memory limit exceeded\n" : "", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, invocationBudget.ReservedBytes);
        Assert.Equal(0, policyBudget.PeakBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingOutputRetainsAdmissionUntilActualCompletion(bool cancel)
    {
        var budget = AwkMemoryBudget.CreateFixed(1 << 20);
        var host = new TestOperationVisitor([]);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<AwkAppendResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ReadOnlyMemory<byte> borrowed = default;
        var outputs = new List<string>();
        host.AppendOverride = (descriptor, bytes, _) =>
        {
            Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
            outputs.Add(System.Text.Encoding.UTF8.GetString(bytes.Span));
            if (outputs.Count != 1) return Task.FromResult(AwkAppendResult.Accepted());
            borrowed = bytes;
            entered.SetResult();
            return release.Task;
        };
        using var cancellation = new CancellationTokenSource();
        var policy = AwkExecutionPolicy.CreateRestricted(budget)
            .WithInternalFailureObserver(AwkInternalFailuresAttribute.CreateObserver());
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"held\";print \"after\"}"));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.ReservedBytes > 0);
            Assert.False(budget.TryReserve(budget.LimitBytes, out _));
            Assert.Equal("held\n", System.Text.Encoding.UTF8.GetString(borrowed.Span));
        }
        finally { release.TrySetResult(AwkAppendResult.Accepted()); }
        if (cancel)
        {
            var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(new[] { "held\n" }, outputs);
        }
        else
        {
            Assert.Equal(AwkExitCode.Success, await execution);
            Assert.Equal(new[] { "held\n", "after\n" }, outputs);
        }
        Assert.Equal(0, budget.ReservedBytes);
    }
}
