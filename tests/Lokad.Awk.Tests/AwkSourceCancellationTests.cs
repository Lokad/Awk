using System.Text;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkSourceCancellationTests
{
    public static IEnumerable<object[]> CanceledBatches()
    {
        foreach (var include in new[] { false, true })
        foreach (var governed in new[] { false, true })
        foreach (var completion in new[] { AwkReadCompletion.MoreData, AwkReadCompletion.EndOfFile })
        foreach (var multiline in new[] { false, true })
            yield return [include, governed, completion, multiline];
    }

    [Theory]
    [MemberData(nameof(CanceledBatches))]
    public async Task ReturnedCanceledSourceDoesNotCopyItsPayload(
        bool include, bool governed, AwkReadCompletion completion, bool multiline)
    {
        var content = Encoding.UTF8.GetBytes("#" + new string('a', (1 << 20) - 2) + "\n");
        var offsets = multiline ? Enumerable.Range(1, content.Length / 128 - 1).Select(i => i * 128).ToArray() : [];
        if (multiline)
            for (var i = 0; i < content.Length; i += 128) { content[i] = (byte)'#'; content[i + 127] = (byte)'\n'; }
        var batch = AwkReadResult.FromContent(content, offsets, completion);
        // Warm the same cancellation boundary before bounding allocations.
        await RunAsync();
        await RunAsync();

        async Task RunAsync()
        {
            using var cancellation = new CancellationTokenSource();
            var budget = new CooperativeTestBudget(32 << 20);
            long start = 0, afterCancelGrowth = 0;
            budget.OnGrowth = growth => { if (cancellation.IsCancellationRequested) afterCancelGrowth += growth; };
            var host = new MockFileSystem
            {
                ReadLinesOverride = (_, _) =>
                {
                    cancellation.Cancel();
                    start = GC.GetAllocatedBytesForCurrentThread();
                    return batch;
                }
            };
            host.AddFile("/source", "placeholder");
            var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
            if (governed) policy = policy.WithMemoryBudget(budget);
            var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                Tool(include).ExecuteAsync(host, policy, cancellation.Token));
            var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.InRange(allocated, 0, 32 << 10);
            Assert.Equal(0, afterCancelGrowth);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancellationDuringSourceAdmissionStopsBeforeDecoding(bool include)
    {
        using var cancellation = new CancellationTokenSource();
        var payload = Encoding.UTF8.GetBytes("#" + new string('a', 1 << 20) + "\n");
        var batch = AwkReadResult.FromContent(payload, [], AwkReadCompletion.EndOfFile);
        var armed = false;
        long start = 0;
        var budget = new CooperativeTestBudget(32 << 20)
        {
            OnGrowth = _ =>
            {
                if (!armed || cancellation.IsCancellationRequested) return;
                cancellation.Cancel();
                start = GC.GetAllocatedBytesForCurrentThread();
            }
        };
        var host = new MockFileSystem { ReadLinesOverride = (_, _) => { armed = true; return batch; } };
        host.AddFile("/source", "placeholder");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Tool(include).ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), cancellation.Token));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - start, 0, 32 << 10);
        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task CanceledPendingSourceAwaitsTheActualReadBeforeClosing(bool include, bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { entered.TrySetResult(); return pending.Task; }
        };
        host.AddFile("/source", "placeholder");
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = Tool(include).ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Equal(1, host.OpenFileCount);
            Assert.Equal(governed, budget.Reserved > 0);
            pending.SetResult(AwkReadResult.FromContent("BEGIN{print \"unreached\"}"u8.ToArray(), [], AwkReadCompletion.EndOfFile));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            pending.TrySetResult(AwkReadResult.EndOfFile);
            try { await run; } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }

    private static Awk Tool(bool include)
    {
        string[] arguments = include ? ["@include \"source\"\nBEGIN{print \"unreached\"}"] : ["-f", "/source"];
        Assert.True(Awk.TryParse(AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []), out var awk));
        return awk;
    }
}
