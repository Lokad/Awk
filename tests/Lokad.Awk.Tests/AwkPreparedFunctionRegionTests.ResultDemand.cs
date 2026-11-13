namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CountedReturnKeepsItsCalleeFundedThroughPendingDiagnostics(bool cancel)
    {
        const string warning = "awk: warning: sqrt: received negative argument -1\n";
        const string source = "function part(s){return substr(s,1,sqrt(-1)?4:4)}" +
            "BEGIN{total=0;print \"ready\"}{total+=length(part($1));print total}";
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = "";
        var error = "";
        var released = false;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdOut) output += AwkUtf8Codec.Decode(bytes.Span);
                else
                {
                    error += AwkUtf8Codec.Decode(bytes.Span);
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual(System.Text.Encoding.UTF8.GetBytes(warning)));
                }
                return AwkAppendResult.Accepted();
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("0007\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, _ => released = true));
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("ready\n", output);
            Assert.False(run.IsCompleted);
            var funded = budget.Reserved;
            Assert.True(funded > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Equal(funded, budget.Reserved);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(AwkExitCode.Success, await run);
            Assert.Equal(cancel ? "ready\n" : "ready\n4\n", output);
            Assert.Equal(warning, error);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.True(released);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
