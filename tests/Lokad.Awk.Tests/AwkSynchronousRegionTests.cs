namespace Lokad.Awk.Tests;

public sealed partial class AwkSynchronousRegionTests
{
    [Fact]
    public async Task PreparedWhileMatchesLegacy()
    {
        {
            var prepared = await RunLoop(false, pendingOutput: false, whileLoop: true);
            var legacy = await RunLoop(true, pendingOutput: false, whileLoop: true);
            Assert.Equal(legacy, prepared);
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
    public async Task WhileBoundarySelectsLegacyBeforeEffects(bool lint, bool pendingOutput)
    {
        var prepared = await RunLoop(false, pendingOutput, whileLoop: true, lint: lint);
        var legacy = await RunLoop(true, pendingOutput, whileLoop: true, lint: lint);
        Assert.Equal(legacy, prepared);
        Assert.Equal(pendingOutput ? "prior" : "", prepared.Output);
    }

    [Fact]
    public async Task WhileMemoryFailurePreservesTargetAndStopsBeforeIncrement()
    {
        var prepared = await RunLoop(false, false, whileLoop: true, failMemory: true);
        var legacy = await RunLoop(true, false, whileLoop: true, failMemory: true);
        Assert.Equal(legacy, prepared);
        Assert.Equal(7, prepared.Value);
        Assert.Equal(0, prepared.Index);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
    }

    [Fact]
    public async Task PreparedLoopMatchesLegacy()
    {
        var prepared = await RunLoop(false, pendingOutput: false);
        var legacy = await RunLoop(true, pendingOutput: false);
        Assert.Equal(legacy, prepared);
        {
            Assert.Equal(20, prepared.Value);
            Assert.Equal(8, prepared.Index);
            Assert.Equal("", prepared.Error);
        }
    }

    [Fact]
    public async Task PendingOutputSelectsLegacyBeforeAnyLoopEffects()
    {
        var prepared = await RunLoop(false, pendingOutput: true);
        var legacy = await RunLoop(true, pendingOutput: true);
        Assert.Equal(legacy, prepared);
        Assert.Equal("prior", prepared.Output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingBodyIsNotReplayedOrPublishedEarly(bool whileLoop)
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
        var loop = Loop(body, iterations: 1, whileLoop: whileLoop);
        var pending = loop.ExecuteAsync(new AwkEvaluationContext(state, output, io)).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, counters.SynchronousLoopRuns);
        source.SetResult(AwkValue.FromNumber(9));
        Assert.Equal(AwkStatementFlow.Continue, await pending);
        Assert.Equal(9, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Fact]
    public void RegionsStopAtBoundedDepthAndExcludeSpecialVariables()
    {
        AwkExpression expression = AwkExpression.Number(1);
        for (var depth = 1; depth < 16; depth++)
            expression = AwkExpression.Add(expression, AwkExpression.Number(1));
        Assert.Equal(16, Assert.IsAssignableFrom<AwkSynchronousExpression>(expression).SynchronousDepth);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(AwkExpression.Add(expression, AwkExpression.Number(1)));
        var nf = AwkExpression.PreparedGlobalVariable(AwkVariableName.FromText("NF"), 0);
        Assert.Equal(0, Assert.IsAssignableFrom<AwkSynchronousExpression>(nf).SynchronousDepth);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(AwkExpression.Add(nf, AwkExpression.Number(1)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CancellationBeforeInitializationMutatesNothing(bool forceLegacy, bool whileLoop)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters, cancellation.Token);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), AwkExpression.Number(9));
        var loop = Loop(forceLegacy ? new ForwardingStatement(body) : body, whileLoop: whileLoop);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        cancellation.Cancel();
        var pending = loop.ExecuteAsync(new AwkEvaluationContext(state, output, io));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        Assert.True(pending.IsCanceled);
        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Fact]
    public async Task PreparedConditionalMatchesLegacy()
    {
        {
            var prepared = await RunConditional(false);
            var legacy = await RunConditional(true);
            Assert.Equal(legacy, prepared);
            {
                Assert.Equal(4, prepared.Value);
                Assert.Equal(23, prepared.Index);
                Assert.Equal("", prepared.Error);
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ConditionalPolicyBoundariesSelectLegacyBeforeEffects(int boundary)
    {
        var prepared = await RunConditional(false, boundary);
        var legacy = await RunConditional(true, boundary);
        Assert.Equal(legacy, prepared);
    }

    [Fact]
    public async Task ConditionalMemoryRefusalPreservesEarlierEffects()
    {
        var prepared = await RunConditional(false, failMemory: true);
        var legacy = await RunConditional(true, failMemory: true);
        Assert.Equal(legacy, prepared);
        Assert.Equal(0, prepared.Value);
        Assert.Equal(1, prepared.Index);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingConditionalBodyKeepsContextUntilItsActualCompletion(bool cancel)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var source = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new PendingOperand(source.Task);
        var statement = AwkStatement.If(new(AwkExpression.Number(1)), [AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), operand)]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var pending = new AwkAction([statement]).ExecuteAsync(state, io, output).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.True(state.TryGetExecutionContext(out _));
        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, counters.SynchronousConditionalRuns);
        if (cancel)
        {
            source.SetCanceled();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
            Assert.True(pending.IsCanceled);
        }
        else
        {
            source.SetResult(AwkValue.FromNumber(9));
            Assert.Equal(AwkStatementFlow.Continue, await pending);
        }
        Assert.False(state.TryGetExecutionContext(out _));
        Assert.Equal(cancel ? 7 : 9, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    private static async Task<ConditionalResult> RunConditional(bool forceLegacy, int boundary = 0, bool failMemory = false)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters, lint: boundary == 1);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        var indexName = AwkVariableName.FromText("i");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        if (boundary == 3)
        {
            state.AssignVariableValue(AwkVariableName.FromText("FS"), AwkValue.FromText("[ ,]"));
            state.SetCurrentRecord(AwkText.FromString("a b"));
        }
        var value = AwkExpression.PreparedGlobalVariable(name, 1);
        var indexValue = AwkExpression.PreparedGlobalVariable(indexName, 0);
        var target = AwkReference.PreparedGlobalScalar(name, 1);
        var indexTarget = AwkReference.PreparedGlobalScalar(indexName, 0);
        var condition = new AwkCondition(AwkExpression.LogicalOr(
            AwkExpression.Compare(value,AwkExpression.Number(5),AwkComparison.LessThan),            AwkExpression.Compare(indexValue,AwkExpression.Number(0),AwkComparison.Equal)));
        var inner = AwkStatement.If(new(AwkExpression.Compare(value, AwkExpression.Number(0), AwkComparison.Equal)), [AwkStatement.Assignment(indexTarget, AwkExpression.Add(indexValue, AwkExpression.Number(1)))], [AwkStatement.Assignment(indexTarget, AwkExpression.Add(indexValue, AwkExpression.Number(2)))]);
        var statement = AwkStatement.If(condition, [inner, AwkStatement.Assignment(target,failMemory ? AwkExpression.Literal(new string('x',2 << 20)) :
                AwkExpression.Add(value,AwkExpression.Number(3)))], [AwkStatement.Assignment(indexTarget,AwkExpression.Add(indexValue,AwkExpression.Number(10))),
                AwkStatement.Assignment(target,AwkExpression.Subtract(value,AwkExpression.Number(1)))]);
        var action = new AwkAction([forceLegacy ? new ForwardingStatement(statement) : statement]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var error = "";
        try
        {
            for (var run = 0; run < 4; run++)
            {
                if (boundary == 2) output.Append(AwkText.FromSourceLiteralString("prior"));
                await action.ExecuteAsync(state, io, output);
            }
        }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        Assert.False(state.TryGetExecutionContext(out _));
        var result = new ConditionalResult(state.ResolveVariableValue(name).AsNumber(), state.ResolveVariableValue(indexName).AsNumber(), state.MemoryAccounting.CurrentReservedBytes, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private readonly record struct ConditionalResult(double Value, double Index, long Reserved, string Output, string Error);

    private static async Task<Result> RunLoop(bool forceLegacy, bool pendingOutput, bool whileLoop = false, bool lint = false, bool failMemory = false, bool doWhileLoop = false)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters, lint: lint);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var value = AwkExpression.Modulo(AwkExpression.Add(AwkExpression.Multiply(
            AwkExpression.PreparedGlobalVariable(name, 1), AwkExpression.Number(3)), AwkExpression.Number(1)), AwkExpression.Number(101));
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), failMemory ? AwkExpression.Literal(new string('x', 2 << 20)) : value);
        var loop = Loop(forceLegacy ? new ForwardingStatement(body) : body, whileLoop: whileLoop, doWhileLoop: doWhileLoop);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (pendingOutput) output.Append(AwkText.FromSourceLiteralString("prior"));
        var error = "";
        try { await loop.ExecuteAsync(new AwkEvaluationContext(state, output, io)); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var result = new Result(state.ResolveVariableValue(name).AsNumber(), state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), state.MemoryAccounting.CurrentReservedBytes, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private static AwkStatement Loop(AwkStatement body, int iterations = 8, bool whileLoop = false, bool doWhileLoop = false)
    {
        var name = AwkVariableName.FromText("i");
        var reference = AwkReference.PreparedGlobalScalar(name, 0);
        var condition = new AwkCondition(AwkExpression.Compare(AwkExpression.PreparedGlobalVariable(name, 0),
            AwkExpression.Number(iterations), AwkComparison.LessThan));
        if (doWhileLoop)
            return AwkStatement.DoWhile([body, AwkStatement.PostIncrement(reference)], condition);
        if (whileLoop)
            return AwkStatement.While(condition, [body, AwkStatement.PostIncrement(reference)]);
        return AwkStatement.For(AwkStatement.Assignment(reference, AwkExpression.Number(0)), condition,
            AwkStatement.PostIncrement(reference), [body]);
    }

    private static CooperativeTestMemory State(AwkRuntimeCounters counters, CancellationToken token = default, bool lint = false)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, token, testMemory.Configure(options), new AwkRuntimeResourceMonitor(limits, counters, token), AwkCompatibilityProfile.Default.WithLintMode(lint ? AwkLintMode.Normal : AwkLintMode.Off));
        state.InitializeGlobalReadSlots(2);
        state.AssignVariableValue(AwkVariableName.FromText("i"), AwkValue.FromNumber(0));
        return testMemory.Initialize(() => state);
    }

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host, AwkRuntimeCounters counters) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, counters, state.Limits, state, static _ => []);

    private sealed class PendingOperand(Task<AwkValue> value) : AwkExpression
    {
        public int Entries { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            Entries++;
            return new(value);
        }
    }

    private sealed class ForwardingStatement(AwkStatement inner) : AwkStatement
    {
        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) => inner.ExecuteAsync(context);
    }

    private readonly record struct Result(double Value, double Index, long Reserved, string Output, string Error);
}
