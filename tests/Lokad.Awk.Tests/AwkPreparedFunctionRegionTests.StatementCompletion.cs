using System.Threading.Tasks.Sources;

namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StatementCompletionConsumesEachOperationOnceAndPreservesFlow(bool suspend)
    {
        foreach (var flow in Enum.GetValues<AwkStatementFlow>())
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var state = State(counters, AwkRuntimeLimits.CreateDefault());
            var host = new MockFileSystem();
            var io = Io(state, host, counters);
            using var output = new AwkOutputSink(state.MemoryAccounting, counters);
            var prefix = 0;
            var suffix = 0;
            var operation = new SingleConsumptionStatement(() =>
                output.Append(AwkText.FromSourceLiteralString("completed\n")));
            if (!suspend) operation.Complete(flow);
            var pending = AwkStatementBlockExecutor.ExecuteAsync(
                [new ObservedStatement(() => prefix++), operation, new ObservedStatement(() => suffix++)],
                new(state, output, io));
            if (suspend)
            {
                Assert.False(pending.IsCompleted);
                Assert.Equal(1, prefix);
                Assert.Equal(0, suffix);
                operation.Complete(flow);
            }
            Assert.Equal(flow, await pending);
            Assert.Equal(1, prefix);
            Assert.Equal(flow == AwkStatementFlow.Continue ? 1 : 0, suffix);
            Assert.Equal(1, operation.Entries);
            Assert.Equal(1, operation.Consumptions);
            Assert.Equal("completed\n", host.GetOutput(AwkFileDescriptor.StdOut));
            await io.CloseAsync();
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StatementFailureDoesNotReplayPrefixOrRunSuffix(bool suspend)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var prefix = 0;
        var suffix = 0;
        var operation = new SingleConsumptionStatement();
        var failure = new AwkRuntimeException(Lokad.Awk.AwkMessages.ErrorReturnOutsideFunction);
        if (!suspend) operation.Fail(failure);
        var pending = AwkStatementBlockExecutor.ExecuteAsync(
            [new ObservedStatement(() => prefix++), operation, new ObservedStatement(() => suffix++)],
            new(state, output, io));
        if (suspend) operation.Fail(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<AwkRuntimeException>(async () => await pending));
        Assert.Equal(1, prefix);
        Assert.Equal(0, suffix);
        Assert.Equal(1, operation.Entries);
        Assert.Equal(1, operation.Consumptions);
        await io.CloseAsync();
    }

    private sealed class ObservedStatement(Action execute) : AwkStatement
    {
        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            execute();
            return ValueTask.FromResult(AwkStatementFlow.Continue);
        }
    }

    private sealed class SingleConsumptionStatement(Action? consumed = null) : AwkStatement, IValueTaskSource<AwkStatementFlow>
    {
        private ManualResetValueTaskSourceCore<AwkStatementFlow> _completion = new()
        {
            RunContinuationsAsynchronously = true
        };
        public int Entries { get; private set; }
        public int Consumptions { get; private set; }
        public void Complete(AwkStatementFlow flow) => _completion.SetResult(flow);
        public void Fail(Exception failure) => _completion.SetException(failure);
        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            Entries++;
            return new(this, _completion.Version);
        }
        public AwkStatementFlow GetResult(short token)
        {
            Assert.Equal(0, Consumptions++);
            var flow = _completion.GetResult(token);
            consumed?.Invoke();
            return flow;
        }
        public ValueTaskSourceStatus GetStatus(short token) => _completion.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token,
            ValueTaskSourceOnCompletedFlags flags) => _completion.OnCompleted(continuation, state, token, flags);
    }
}
