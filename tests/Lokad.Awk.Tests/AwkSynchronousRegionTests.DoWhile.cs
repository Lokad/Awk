namespace Lokad.Awk.Tests;

public sealed partial class AwkSynchronousRegionTests
{
    [Fact]
    public async Task PreparedDoWhileMatchesLegacy()
    {
        {
            var prepared = await RunLoop(false, false, doWhileLoop: true);
            var legacy = await RunLoop(true, false, doWhileLoop: true);
            Assert.Equal(legacy with { LoopRuns = prepared.LoopRuns }, prepared);
            Assert.Equal(1, prepared.LoopRuns);
            Assert.Equal(0, legacy.LoopRuns);
            {
                Assert.Equal(20, prepared.Value);
                Assert.Equal(8, prepared.Index);
                Assert.Equal("", prepared.Error);
            }
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DoWhileBoundarySelectsLegacyBeforeEffects(bool lint, bool pendingOutput)
    {
        var prepared = await RunLoop(false, pendingOutput, lint: lint, doWhileLoop: true);
        var legacy = await RunLoop(true, pendingOutput, lint: lint, doWhileLoop: true);
        Assert.Equal(legacy, prepared);
        Assert.Equal(0, prepared.LoopRuns);
    }

    [Fact]
    public async Task DoWhileMemoryFailurePreservesTargetAndStopsBeforeIncrement()
    {
        var prepared = await RunLoop(false, false, failMemory: true, doWhileLoop: true);
        var legacy = await RunLoop(true, false, failMemory: true, doWhileLoop: true);
        Assert.Equal(legacy with { LoopRuns = prepared.LoopRuns }, prepared);
        Assert.Equal(1, prepared.LoopRuns);
        Assert.Equal(7, prepared.Value);
        Assert.Equal(0, prepared.Index);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DoWhilePendingBodyIsNotReplayedOrPublishedEarly(bool cancel)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var source = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new PendingOperand(source.Task);
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), operand);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var pending = Loop(body, iterations: 1, doWhileLoop: true)
            .ExecuteAsync(new AwkEvaluationContext(state, output, io)).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, counters.SynchronousDoWhileLoopRuns);
        if (cancel)
        {
            source.SetCanceled();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        }
        else
        {
            source.SetResult(AwkValue.FromNumber(9));
            Assert.Equal(AwkStatementFlow.Continue, await pending);
            Assert.Equal(9, state.ResolveVariableValue(name).AsNumber());
        }
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DoWhilePendingConditionObservesFirstBodyOnce(bool cancel)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var source = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new PendingOperand(source.Task);
        var condition = new AwkCondition([[AwkConditionClause.Truthy(operand)]]);
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), AwkExpression.Number("9", 9));
        var io = Io(state, new MockFileSystem(), counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var pending = AwkStatement.DoWhile([body], condition)
            .ExecuteAsync(new AwkEvaluationContext(state, output, io)).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(9, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, counters.SynchronousDoWhileLoopRuns);
        if (cancel)
        {
            source.SetCanceled();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
        else
        {
            source.SetResult(AwkValue.FromNumber(0));
            Assert.Equal(AwkStatementFlow.Continue, await pending);
        }
        Assert.Equal(9, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DoWhileCancellationBeforeFirstBodyMutatesNothing(bool forceLegacy)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters, cancellation.Token);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), AwkExpression.Number("9", 9));
        var loop = Loop(forceLegacy ? new ForwardingStatement(body) : body, doWhileLoop: true);
        var io = Io(state, new MockFileSystem(), counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        cancellation.Cancel();
        var pending = loop.ExecuteAsync(new AwkEvaluationContext(state, output, io));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        Assert.True(pending.IsCanceled);
        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(16, 1)]
    [InlineData(17, 0)]
    public async Task DoWhileBodyBoundKeepsMandatoryFirstIteration(int count, int expectedPrepared)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        var io = Io(state, new MockFileSystem(), counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var condition = new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.Number("0", 0))]]);
        var loop = AwkStatement.DoWhile(Enumerable.Repeat(AwkStatement.NoOp(), count).ToArray(), condition);
        Assert.Equal(AwkStatementFlow.Continue, await loop.ExecuteAsync(new AwkEvaluationContext(state, output, io)));
        Assert.Equal(expectedPrepared, counters.SynchronousDoWhileLoopRuns);
        await io.CloseAsync();
    }
}
