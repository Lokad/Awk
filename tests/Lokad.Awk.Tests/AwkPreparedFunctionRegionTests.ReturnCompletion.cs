namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task CapturedReturnSurvivesActualFlushCompletion(bool governed, bool numeric, bool cancel)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        {
            await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, cancellation.Token);
            var counters = AwkRuntimeCounters.CreateEnabled();
            using var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, owner);
            var source = numeric ? "function f(x){return x+0}BEGIN{total+=f(7)}"
                : "function f(x){return x}BEGIN{saved=f(\"0007\")}";
            var program = Bind(source);
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            state.InitializeFunctions(program.Functions);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var host = new TestOperationVisitor([]);
            var writes = new List<byte[]>();
            host.AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                writes.Add(bytes.ToArray());
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
                // The host may use the borrow until actual completion.
                Assert.True(bytes.Span.SequenceEqual("before\n"u8));
                return AwkAppendResult.Accepted();
            };
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, cancellation.Token, counters, state.Limits, state, static _ => []);
            using var output = new AwkOutputSink(state.MemoryAccounting, counters);
            output.Append(AwkText.FromSourceLiteralString("before\n"));
            var pending = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(pending.IsCompleted);
                Assert.True(state.IsInsideFunction);
                // Return evaluation precedes the flush, including an owned text capture.
                state.AssignVariableValue(AwkVariableName.FromText("x"), AwkValue.FromText("changed"));
                if (governed) Assert.True(budget.Reserved > 0);
                if (cancel) cancellation.Cancel();
                Assert.False(pending.IsCompleted);
                Assert.True(state.IsInsideFunction);
                if (governed) Assert.True(budget.Reserved > 0);
                release.TrySetResult();
                if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
                else
                {
                    Assert.Equal(AwkStatementFlow.Continue, await pending);
                    if (numeric) Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
                    else Assert.True(state.ResolveVariableValue(AwkVariableName.FromText("saved")).ByteText.AsSpan().SequenceEqual("0007"u8));
                }
                Assert.True(Assert.Single(writes).AsSpan().SequenceEqual("before\n"u8));
                Assert.False(state.IsInsideFunction);
                Assert.Equal(1, counters.BoundFunctionRuns);
                Assert.Equal(0, counters.Utf8Encodes);
                Assert.Equal(0, counters.StringDecodes);
            }
            finally
            {
                release.TrySetResult();
                try { await pending; } catch (OperationCanceledException) when (cancel) { }
                await io.CloseForCleanupAsync();
            }
        }
        Assert.Equal(0, budget.Reserved);
    }
}
