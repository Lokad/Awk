namespace Lokad.Awk.Tests;

public sealed class AwkSynchronousStringTests
{

    [Fact]
    public async Task CompleteStringActionPreservesState()
    {
        var prepared = await RunAction(false);
        var legacy = await RunAction(true);
        Assert.Equal(legacy with { Synchronous = prepared.Synchronous }, prepared);
        Assert.True(prepared.Synchronous > legacy.Synchronous);
        {
            Assert.Equal("beta", prepared.Text);
            Assert.Equal(12, prepared.Total);
            Assert.Equal(2, prepared.Synchronous);
            Assert.Equal("", prepared.Error);
        }
    }

    [Fact]
    public void StringPreparationStopsAtSixteenAndExcludesPendingArguments()
    {
        AwkExpression value = AwkExpression.Literal("text");
        for (var depth = 1; depth < 16; depth++) value = Builtin("tolower", value);
        Assert.Equal(16, Assert.IsAssignableFrom<AwkSynchronousExpression>(value).SynchronousDepth);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(Builtin("tolower", value));
        var pending = new PendingOperand(new TaskCompletionSource<AwkValue>().Task);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(Builtin("substr", pending, AwkExpression.Number("1", 1)));
        Assert.Equal(0, Assert.IsAssignableFrom<AwkSynchronousExpression>(AwkExpression.FieldIndex(pending)).SynchronousDepth);
    }

    [Fact]
    public async Task PendingBuiltinArgumentIsEvaluatedOnceBeforeAssignment()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new PendingOperand(completion.Task);
        var expression = Builtin("substr", source, AwkExpression.Number("2", 2), AwkExpression.Number("2", 2));
        var context = new AwkEvaluationContext(state);
        var pending = expression.EvaluateAsync(context).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(1, source.Entries);
        completion.SetResult(AwkValue.FromText("abcd"));
        Assert.Equal("bc", (await pending).ByteText.AsString());
        Assert.Equal(1, source.Entries);
    }

    private static AwkExpression Builtin(string name, params AwkExpression[] arguments)
    {
        Assert.True(AwkBuiltins.TryGetCallable(name, out var definition));
        return AwkExpression.BuiltinCall(definition, arguments);
    }

    private static async Task<Result> RunAction(bool legacy)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        var s = AwkVariableName.FromText("s"); var total = AwkVariableName.FromText("total");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id:beta:tail"));
        AwkExpression Number(string text, int value) => legacy ? new LegacyNumber(text, value) : AwkExpression.Number(text, value);
        AwkExpression Variable() => legacy ? new LegacyVariable(s, 0) : AwkExpression.PreparedGlobalVariable(s, 0);
        var record = AwkExpression.FieldIndex(Number("0", 0));
        var first = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(s, 0), Builtin("substr", record, Number("4", 4), Number("4", 4)));
        var length = AwkExpression.Length(Variable(), AwkOptionalIdentifier.Some(s.Identifier));
        var index = Builtin("index", record, legacy ? new LegacyLiteral("beta") : AwkExpression.Literal("beta"));
        var lower = AwkExpression.Length(Builtin("tolower", Variable()), AwkOptionalIdentifier.None);
        var second = AwkStatement.Expression(AwkExpression.CompoundAssignment(AwkReference.PreparedGlobalScalar(total, 1), AwkExpression.Add(AwkExpression.Add(length, index), lower), AwkCompoundAssignmentOperation.Add));
        var host = new MockFileSystem();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None, counters, state.Limits, state, static () => []);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var error = "";
        using (state.PushExecutionContext(output, io))
        {
            try { await AwkStatementBlockExecutor.ExecuteAsync([first, second], new AwkEvaluationContext(state, output, io)); }
            catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        }
        var result = new Result(state.HasScalar(s) ? state.ResolveVariableValue(s).ByteText.AsString() : "", state.HasScalar(total) ? state.ResolveVariableValue(total).AsNumber() : 0, counters.SynchronousExpressionRuns, state.MemoryAccounting.CurrentReservedBytes, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private static CooperativeTestMemory State(AwkRuntimeCounters counters)
    {

        var testMemory = new CooperativeTestMemory(1 << 20, enabled: true && (long)(1 << 20) != long.MaxValue);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options));
        state.InitializeGlobalReadSlots(2);
        return testMemory.Initialize(() => state);
    }

    private sealed class LegacyNumber(string text, int value) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkValue.FromNumber(context.State.ResolveSourceNumberLiteral(text, value)));
    }
    private sealed class LegacyLiteral(string text) : AwkExpression
    {
        private readonly AwkText _text = AwkText.FromSourceLiteralString(text);
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.CreateByteTextValue(_text));
    }
    private sealed class LegacyVariable(AwkVariableName name, int index) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolvePreparedGlobalValue(name, index));
    }
    private sealed class PendingOperand(Task<AwkValue> pending) : AwkExpression
    {
        public int Entries { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        { Entries++; return new(pending); }
    }
    private readonly record struct Result(string Text, double Total, long Synchronous, long Reserved, string Output, string Error);
}
