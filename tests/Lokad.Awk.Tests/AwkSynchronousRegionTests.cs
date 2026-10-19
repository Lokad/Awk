namespace Lokad.Awk.Tests;

public sealed partial class AwkSynchronousRegionTests
{
    [Fact]
    public async Task PreparedWhileMatchesLegacy()
    {
        {
            var prepared = await RunLoop(false, pendingOutput: false, whileLoop: true);
            var legacy = await RunLoop(true, pendingOutput: false, whileLoop: true);
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
    public async Task WhileBoundarySelectsLegacyBeforeEffects(bool lint, bool pendingOutput)
    {
        var prepared = await RunLoop(false, pendingOutput, whileLoop: true, lint: lint);
        var legacy = await RunLoop(true, pendingOutput, whileLoop: true, lint: lint);
        Assert.Equal(legacy, prepared);
        Assert.Equal(0, prepared.LoopRuns);
        Assert.Equal(pendingOutput ? "prior" : "", prepared.Output);
    }

    [Fact]
    public async Task WhileMemoryFailurePreservesTargetAndStopsBeforeIncrement()
    {
        var prepared = await RunLoop(false, false, whileLoop: true, failMemory: true);
        var legacy = await RunLoop(true, false, whileLoop: true, failMemory: true);
        Assert.Equal(legacy with { LoopRuns = prepared.LoopRuns }, prepared);
        Assert.Equal(1, prepared.LoopRuns);
        Assert.Equal(0, legacy.LoopRuns);
        Assert.Equal(7, prepared.Value);
        Assert.Equal(0, prepared.Index);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
    }

    [Fact]
    public async Task PreparedLoopMatchesLegacy()
    {
        var prepared = await RunLoop(false, pendingOutput: false);
        var legacy = await RunLoop(true, pendingOutput: false);
        Assert.Equal(legacy with { LoopRuns = prepared.LoopRuns }, prepared);
        Assert.Equal(1, prepared.LoopRuns);
        Assert.Equal(0, legacy.LoopRuns);
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
        Assert.Equal(0, prepared.LoopRuns);
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
        AwkExpression expression = AwkExpression.Number("1", 1);
        for (var depth = 1; depth < 16; depth++)
            expression = AwkExpression.Add(expression, AwkExpression.Number("1", 1));
        Assert.Equal(16, Assert.IsAssignableFrom<AwkSynchronousExpression>(expression).SynchronousDepth);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(AwkExpression.Add(expression, AwkExpression.Number("1", 1)));
        var nf = AwkExpression.PreparedGlobalVariable(AwkVariableName.FromText("NF"), 0);
        Assert.Equal(0, Assert.IsAssignableFrom<AwkSynchronousExpression>(nf).SynchronousDepth);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(AwkExpression.Add(nf, AwkExpression.Number("1", 1)));
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
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), AwkExpression.Number("9", 9));
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
            Assert.Equal(legacy with { Runs = prepared.Runs }, prepared);
            Assert.Equal(0, legacy.Runs);
            {
                Assert.Equal(4, prepared.Value);
                Assert.Equal(23, prepared.Index);
                Assert.Equal(6, prepared.Runs);
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
        Assert.Equal(0, prepared.Runs);
    }

    [Fact]
    public async Task ConditionalMemoryRefusalPreservesEarlierEffects()
    {
        var prepared = await RunConditional(false, failMemory: true);
        var legacy = await RunConditional(true, failMemory: true);
        Assert.Equal(legacy with { Runs = prepared.Runs }, prepared);
        Assert.Equal(0, prepared.Value);
        Assert.Equal(1, prepared.Index);
        Assert.Equal(2, prepared.Runs);
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
        var statement = AwkStatement.If(new([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]), [AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), operand)]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var pending = new AwkAction(null, [statement]).ExecuteAsync(state, io, output).AsTask();
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

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void ConditionalPreparationBoundsNesting(int depth, bool eligible)
    {
        var condition = new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]);
        var statement = AwkStatement.NoOp();
        for (var index = 0; index < depth; index++) statement = AwkStatement.If(condition, [statement]);
        Assert.Equal(eligible, AwkStatementBlockExecutor.TryPrepareActions([statement], null) is not null);
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(16, false)]
    public void ConditionalPreparationBoundsStatementsBeforeInspectingBody(int count, bool eligible)
    {
        var probe = new PreparationProbe();
        var condition = new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]);
        var statement = AwkStatement.If(condition, Enumerable.Repeat<AwkStatement>(probe, count).ToArray());
        Assert.Equal(eligible, AwkStatementBlockExecutor.TryPrepareActions([statement], null) is not null);
        Assert.Equal(eligible ? count : 0, probe.Preparations);
    }

    [Fact]
    public void ConditionalPreparationSharesStatementBudgetWithFollowingStatements()
    {
        var condition = new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]);
        var statements = Enumerable.Repeat(AwkStatement.NoOp(), 16).ToArray();
        statements[0] = AwkStatement.If(condition, [AwkStatement.NoOp()]);
        Assert.Null(AwkStatementBlockExecutor.TryPrepareActions(statements, null));
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
        var condition = new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.LogicalOr(
            AwkExpression.Compare(value,AwkExpression.Number("5",5),AwkComparison.LessThan),            AwkExpression.Compare(indexValue,AwkExpression.Number("0",0),AwkComparison.Equal)))]]);
        var inner = AwkStatement.If(new([[AwkConditionClause.Comparison(value, AwkExpression.Number("0", 0), AwkComparison.Equal)]]), [AwkStatement.Assignment(indexTarget, AwkExpression.Add(indexValue, AwkExpression.Number("1", 1)))], [AwkStatement.Assignment(indexTarget, AwkExpression.Add(indexValue, AwkExpression.Number("2", 2)))]);
        var statement = AwkStatement.If(condition, [inner, AwkStatement.Assignment(target,failMemory ? AwkExpression.Literal(new string('x',2 << 20)) :
                AwkExpression.Add(value,AwkExpression.Number("3",3)))], [AwkStatement.Assignment(indexTarget,AwkExpression.Add(indexValue,AwkExpression.Number("10",10))),
                AwkStatement.Assignment(target,AwkExpression.Subtract(value,AwkExpression.Number("1",1)))]);
        var action = new AwkAction(null, [forceLegacy ? new ForwardingStatement(statement) : statement]);
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
        var result = new ConditionalResult(state.ResolveVariableValue(name).AsNumber(), state.ResolveVariableValue(indexName).AsNumber(), counters.SynchronousConditionalRuns, state.MemoryAccounting.CurrentReservedBytes, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private readonly record struct ConditionalResult(double Value, double Index, long Runs, long Reserved, string Output, string Error);

    private static async Task<Result> RunLoop(bool forceLegacy, bool pendingOutput, bool whileLoop = false, bool lint = false, bool failMemory = false, bool doWhileLoop = false)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters, lint: lint);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var value = AwkExpression.Modulo(AwkExpression.Add(AwkExpression.Multiply(
            AwkExpression.PreparedGlobalVariable(name, 1), AwkExpression.Number("3", 3)), AwkExpression.Number("1", 1)), AwkExpression.Number("101", 101));
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), failMemory ? AwkExpression.Literal(new string('x', 2 << 20)) : value);
        var loop = Loop(forceLegacy ? new ForwardingStatement(body) : body, whileLoop: whileLoop, doWhileLoop: doWhileLoop);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (pendingOutput) output.Append(AwkText.FromSourceLiteralString("prior"));
        var error = "";
        try { await loop.ExecuteAsync(new AwkEvaluationContext(state, output, io)); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var result = new Result(state.ResolveVariableValue(name).AsNumber(), state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), counters.SynchronousLoopRuns, state.MemoryAccounting.CurrentReservedBytes, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    [Theory]
    [InlineData(16, true)]
    [InlineData(17, false)]
    public void PreparationBoundsBodyWorkBeforeInspectingStatements(int count, bool eligible)
    {
        var header = new PreparationProbe();
        var condition = new AwkCondition([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]);
        var statement = AwkStatement.Assignment(
            AwkReference.PreparedGlobalScalar(AwkVariableName.FromText("x"), 0), AwkExpression.Number("1", 1));
        var prepared = AwkSynchronousFor.TryPrepare(header, condition, header,
            Enumerable.Repeat(statement, count).ToArray());
        Assert.Equal(eligible, prepared is not null);
        Assert.Equal(eligible ? 2 : 0, header.Preparations);
    }

    private sealed class PreparationProbe : AwkStatement
    {
        public int Preparations { get; private set; }
        internal override bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action)
        {
            Preparations++;
            action = static _ => { };
            return true;
        }
        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            throw new InvalidOperationException("Preparation must not execute a statement.");
    }

    private static AwkStatement Loop(AwkStatement body, int iterations = 8, bool whileLoop = false, bool doWhileLoop = false)
    {
        var name = AwkVariableName.FromText("i");
        var reference = AwkReference.PreparedGlobalScalar(name, 0);
        var condition = new AwkCondition([[AwkConditionClause.Comparison(AwkExpression.PreparedGlobalVariable(name, 0),
            AwkExpression.Number(iterations.ToString(System.Globalization.CultureInfo.InvariantCulture), iterations), AwkComparison.LessThan)]]);
        if (doWhileLoop)
            return AwkStatement.DoWhile([body, AwkStatement.PostIncrement(reference)], condition);
        if (whileLoop)
            return AwkStatement.While(condition, [body, AwkStatement.PostIncrement(reference)]);
        return AwkStatement.For(AwkStatement.Assignment(reference, AwkExpression.Number("0", 0)), condition,
            AwkStatement.PostIncrement(reference), [body]);
    }

    private static CooperativeTestMemory State(AwkRuntimeCounters counters, CancellationToken token = default, bool lint = false)
    {

        var testMemory = new CooperativeTestMemory(1 << 20, enabled: true && (long)(1 << 20) != long.MaxValue);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility);
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, token, testMemory.Configure(options), new AwkRuntimeResourceMonitor(limits, counters, token), AwkCompatibilityProfile.Default.WithLintMode(lint ? AwkLintMode.Normal : AwkLintMode.Off));
        state.InitializeGlobalReadSlots(2);
        state.AssignVariableValue(AwkVariableName.FromText("i"), AwkValue.FromNumber(0));
        return testMemory.Initialize(() => state);
    }

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host, AwkRuntimeCounters counters) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, counters, state.Limits, state, static () => []);

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

    private readonly record struct Result(double Value, double Index, long LoopRuns, long Reserved, string Output, string Error);
}
