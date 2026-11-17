namespace Lokad.Awk.Tests;

public sealed class AwkNumericFormatAdmissionTests
{
    [Theory]
    [InlineData("CONVFMT", false)]
    [InlineData("CONVFMT", true)]
    [InlineData("OFMT", false)]
    [InlineData("OFMT", true)]
    public async Task NumericFormatAdmissionStopsBeforeFollowingEffects(string variable, bool cancel)
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(16 << 20);
        using var cancellation = new CancellationTokenSource();
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            if (cancel) budget.OnGrowth = _ => cancellation.Cancel();
            else budget.RefuseNextAdmissionMinimumGrowth = 1;
        };
        var source = "BEGIN{CONVFMT=\"%.1000f\";print \"ready\";" + variable + "=0.25;print \"tail\"}";
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = command.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        else
        {
            Assert.Equal(2, (await execution).Value);
            Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        }
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("CONVFMT")]
    [InlineData("OFMT")]
    public async Task ReplacingNumericFormatHonorsRefundCancellation(string variable)
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(16 << 20);
        using var cancellation = new CancellationTokenSource();
        var refunded = false;
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            budget.OnRefund = () => { refunded = true; cancellation.Cancel(); };
        };
        var source = "BEGIN{" + variable + "=0.25;print \"ready\";" + variable + "=\"%.6g\";print \"tail\"}";
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            command.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token));
        Assert.True(refunded);
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
        Assert.Equal(0, budget.Reserved);
    }
}
