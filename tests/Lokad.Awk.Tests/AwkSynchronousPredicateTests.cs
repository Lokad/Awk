namespace Lokad.Awk.Tests;

public sealed class AwkSynchronousPredicateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LogicalFieldIndicesKeepBorrowedNumericReads(bool length)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, 0);
        var bytes = "2 needle"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        var before = counters.AwkTextAllocations;
        var field = AwkExpression.FieldIndex(AwkExpression.FieldIndex(AwkExpression.Number("1", 1)));
        var test = length
            ? AwkExpression.Compare(AwkExpression.Length(field, AwkOptionalIdentifier.None), AwkExpression.Number("1", 1), AwkComparison.GreaterThan)
            : AwkExpression.Compare(field, AwkExpression.Literal("needle"), AwkComparison.Equal);
        var predicate = AwkExpression.LogicalAnd(test, AwkExpression.Number("1", 1));
        var context = new AwkEvaluationContext(state);
        Assert.Equal(1, (await predicate.EvaluateAsync(context)).AsNumber());
        Assert.Equal(before, counters.AwkTextAllocations);
        bytes[0] = (byte)'1';
        Assert.Equal(0, (await predicate.EvaluateAsync(context)).AsNumber());
        Assert.Equal(before, counters.AwkTextAllocations);
    }

    public static IEnumerable<object[]> FuelBoundaries =>
        from fuel in Enumerable.Range(0, 14).Append(100)
        from found in new[] { false, true }
        select new object[] { fuel, found };

    [Theory]
    [MemberData(nameof(FuelBoundaries))]
    public async Task FieldPredicatePreservesFuelAndPartialState(int fuel, bool found)
    {
        var prepared = await Run(false, fuel, found);
        var legacy = await Run(true, fuel, found);
        Assert.Equal(legacy with { Synchronous = prepared.Synchronous }, prepared);
        if (fuel is 0 or 100)
        {
            Assert.Equal(found ? 1 : 0, prepared.Value);
            Assert.Equal(1, prepared.Synchronous);
            Assert.Equal(0, legacy.Synchronous);
            Assert.Equal("", prepared.Error);
        }
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 0)]
    public async Task SynchronousLogicalOperandsKeepShortCircuitOrder(bool or, bool left, int expectedEntries)
    {
        var state = State(AwkRuntimeCounters.CreateEnabled(), 0);
        var right = new EffectOperand();
        var first = AwkExpression.Number(left ? "1" : "0", left ? 1 : 0);
        var expression = or ? AwkExpression.LogicalOr(first, right) : AwkExpression.LogicalAnd(first, right);
        Assert.IsAssignableFrom<AwkSynchronousExpression>(expression);
        _ = await expression.EvaluateAsync(new AwkEvaluationContext(state));
        Assert.Equal(expectedEntries, right.Entries);
    }

    [Fact]
    public async Task PendingLogicalOperandUsesTheOriginalSingleEvaluationPath()
    {
        var state = State(AwkRuntimeCounters.CreateEnabled(), 0);
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var right = new PendingOperand(completion.Task);
        var expression = AwkExpression.LogicalAnd(AwkExpression.Number("1", 1), right);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(expression);
        var pending = expression.EvaluateAsync(new AwkEvaluationContext(state)).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(1, right.Entries);
        completion.SetResult(AwkValue.FromNumber(7));
        Assert.Equal(1, (await pending).AsNumber());
        Assert.Equal(1, right.Entries);
    }

    [Fact]
    public void LogicalPreparationKeepsTheSixteenLevelBound()
    {
        AwkExpression value = AwkExpression.Number("1", 1);
        for (var depth = 1; depth < 16; depth++) value = AwkExpression.LogicalAnd(value, AwkExpression.Number("1", 1));
        Assert.Equal(16, Assert.IsAssignableFrom<AwkSynchronousExpression>(value).SynchronousDepth);
        Assert.IsNotAssignableFrom<AwkSynchronousExpression>(AwkExpression.LogicalOr(value, AwkExpression.Number("1", 1)));
    }

    private static async Task<Result> Run(bool legacy, int fuel, bool found)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, fuel);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(found ? "A 9" : "B 9"));
        AwkExpression Field(int index) => AwkExpression.FieldIndex(legacy ? new LegacyNumber(index) :
            AwkExpression.Number(index.ToString(System.Globalization.CultureInfo.InvariantCulture), index));
        var first = AwkExpression.Compare(Field(1), AwkExpression.Literal("A"), AwkComparison.Equal);
        var second = AwkExpression.Compare(Field(2), AwkExpression.Literal("9"), AwkComparison.Equal);
        var expression = AwkExpression.LogicalAnd(first, second);
        if (!legacy) Assert.IsAssignableFrom<AwkSynchronousExpression>(expression);
        var result = -1.0; var error = "";
        try { result = (await expression.EvaluateAsync(new AwkEvaluationContext(state))).AsNumber(); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        return new(result, error, counters.ExecutionSteps, counters.SynchronousExpressionRuns,
            counters.FieldsMaterialized, counters.AwkTextAllocations, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters, int fuel)
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        if (fuel > 0) limits = limits.WithMaxExecutionSteps(fuel);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits,
                AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter));
    }
    private sealed class LegacyNumber(int index) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkValue.FromNumber(context.State.ResolveSourceNumberLiteral(
                index.ToString(System.Globalization.CultureInfo.InvariantCulture), index)));
    }
    private sealed class EffectOperand : AwkSynchronousExpression
    {
        public int Entries { get; private set; }
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        { Entries++; return AwkValue.FromNumber(1); }
    }
    private sealed class PendingOperand(Task<AwkValue> value) : AwkExpression
    {
        public int Entries { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        { Entries++; return new(value); }
    }
    private readonly record struct Result(double Value, string Error, long Steps, long Synchronous,
        long FieldsMaterialized, long TextAllocations, long Reserved);
}
