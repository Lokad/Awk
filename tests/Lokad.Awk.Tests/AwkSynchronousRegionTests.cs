namespace Lokad.Awk.Tests;

public sealed class AwkSynchronousRegionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(40)]
    [InlineData(80)]
    [InlineData(160)]
    [InlineData(1000)]
    public async Task PreparedLoopMatchesLegacyAtExactFuelAndFailureBoundaries(int fuel)
    {
        var prepared = await RunLoop(false, fuel, pendingOutput: false);
        var legacy = await RunLoop(true, fuel, pendingOutput: false);
        Assert.Equal(legacy with { LoopRuns = prepared.LoopRuns }, prepared);
        Assert.Equal(1, prepared.LoopRuns);
        Assert.Equal(0, legacy.LoopRuns);
        if (fuel == 0 || fuel >= 160)
        {
            Assert.Equal(20, prepared.Value);
            Assert.Equal(8, prepared.Index);
            Assert.Equal(117, prepared.Steps);
            Assert.Equal("", prepared.Error);
        }
        else
            Assert.Equal("awk: execution step limit exceeded\n", prepared.Error);
    }

    [Fact]
    public async Task PendingOutputSelectsLegacyBeforeAnyLoopEffects()
    {
        var prepared = await RunLoop(false, 0, pendingOutput: true);
        var legacy = await RunLoop(true, 0, pendingOutput: true);
        Assert.Equal(legacy, prepared);
        Assert.Equal(0, prepared.LoopRuns);
        Assert.Equal("prior", prepared.Output);
    }

    [Fact]
    public async Task PendingBodyIsNotReplayedOrPublishedEarly()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, 0);
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var source = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new PendingOperand(source.Task);
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), operand);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var loop = Loop(body, iterations: 1);
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforeInitializationMutatesNothing(bool forceLegacy)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, 0, cancellation.Token);
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), AwkExpression.Number("9", 9));
        var loop = Loop(forceLegacy ? new ForwardingStatement(body) : body);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        cancellation.Cancel();
        var pending = loop.ExecuteAsync(new AwkEvaluationContext(state, output, io));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        Assert.True(pending.IsCanceled);
        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    private static async Task<Result> RunLoop(bool forceLegacy, int fuel, bool pendingOutput)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, fuel);
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var value = AwkExpression.Modulo(AwkExpression.Add(AwkExpression.Multiply(
            AwkExpression.PreparedGlobalVariable(name, 1), AwkExpression.Number("3", 3)),
            AwkExpression.Number("1", 1)), AwkExpression.Number("101", 101));
        var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(name, 1), value);
        var loop = Loop(forceLegacy ? new ForwardingStatement(body) : body);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        if (pendingOutput) output.Append(AwkText.FromSourceLiteralString("prior"));
        var error = "";
        try { await loop.ExecuteAsync(new AwkEvaluationContext(state, output, io)); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var result = new Result(state.ResolveVariableValue(name).AsNumber(),
            state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(),
            counters.ExecutionSteps, counters.SynchronousLoopRuns, state.MemoryAccounting.CurrentReservedBytes,
            host.GetOutput(AwkFileDescriptor.StdOut), error);
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

    private static AwkStatement Loop(AwkStatement body, int iterations = 8)
    {
        var name = AwkVariableName.FromText("i");
        var reference = AwkReference.PreparedGlobalScalar(name, 0);
        return AwkStatement.For(AwkStatement.Assignment(reference, AwkExpression.Number("0", 0)),
            new AwkCondition([[AwkConditionClause.Comparison(AwkExpression.PreparedGlobalVariable(name, 0),
                AwkExpression.Number(iterations.ToString(System.Globalization.CultureInfo.InvariantCulture), iterations), AwkComparison.LessThan)]]),
            AwkStatement.PostIncrement(reference), [body]);
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters, int fuel, CancellationToken token = default)
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        if (fuel > 0) limits = limits.WithMaxExecutionSteps(fuel);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, limits, AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, options);
        state.InitializeGlobalReadSlots(2);
        state.AssignVariableValue(AwkVariableName.FromText("i"), AwkValue.FromNumber(0));
        return state;
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

    private readonly record struct Result(double Value, double Index, long Steps, long LoopRuns, long Reserved,
        string Output, string Error);
}
