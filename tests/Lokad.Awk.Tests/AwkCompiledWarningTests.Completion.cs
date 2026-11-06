namespace Lokad.Awk.Tests;

public sealed partial class AwkCompiledWarningTests
{
    [Theory]
    [InlineData(false, false, "success")] [InlineData(true, false, "success")]
    [InlineData(false, true, "success")] [InlineData(true, true, "success")]
    [InlineData(false, false, "cancel")] [InlineData(true, false, "cancel")]
    [InlineData(false, true, "cancel")] [InlineData(true, true, "cancel")]
    [InlineData(false, false, "failure")] [InlineData(true, false, "failure")]
    [InlineData(false, true, "failure")] [InlineData(true, true, "failure")]
    public async Task StatementCompletionWaitsForDiagnosticAcceptance(bool governed, bool inFunction, string outcome)
    {
        const string warning = "awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n";
        var source = inFunction
            ? "function f(x){n=1;print \"inside\";return 7}BEGIN{print f(FS=\":\");exit}"
            : "BEGIN{FS=\":\";print \"after\";exit}";
        var host = new TestOperationVisitor([]);
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = new List<(AwkFileDescriptor Descriptor, string Text)>();
        using var cancellation = new CancellationTokenSource();
        host.AppendOverride = async (descriptor, bytes, _) =>
        {
            writes.Add((descriptor, System.Text.Encoding.UTF8.GetString(bytes.Span)));
            if (writes.Count == 1)
            {
                entered.TrySetResult();
                // Cancellation does not retire a host that still owns the buffer.
                await release.Task.ConfigureAwait(false);
                if (outcome == "failure") return AwkAppendResult.Failure(default);
            }
            return AwkAppendResult.Accepted();
        };
        var execution = AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", "--csv", source)), cancellation.Token,
            Options(AwkRuntimeCounters.CreateEnabled()) with { MemoryBudget = governed ? budget : null });
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal((AwkFileDescriptor.StdErr, warning), Assert.Single(writes));
            Assert.False(execution.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(execution.IsCompleted);
        }
        finally { release.TrySetResult(); }

        if (outcome == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(outcome == "failure" ? 2 : 0, (await execution).Value);
        if (outcome == "success")
        {
            Assert.Equal(inFunction ? "inside\n7\n" : "after\n",
                string.Concat(writes.Where(w => w.Descriptor == AwkFileDescriptor.StdOut).Select(w => w.Text)));
            Assert.Equal(warning, string.Concat(writes.Where(w => w.Descriptor == AwkFileDescriptor.StdErr).Select(w => w.Text)));
        }
        else Assert.Equal((AwkFileDescriptor.StdErr, warning), Assert.Single(writes));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
