namespace Lokad.Awk.Tests;

public sealed class AwkBinaryNumericTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NumericSubtreesConsumeFieldBytesWithoutCachingAndKeepWork(bool finite)
    {
        var (state, counters) = State(finite);
        var bytes = "007 3 tail"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        _ = state.FieldCount;
        var allocations = counters.AwkTextAllocations;
        var expression = AwkExpression.Add(
            AwkExpression.Multiply(Field(1), AwkExpression.Number("2", 2)), Field(2));
        Assert.Equal(17, (await expression.EvaluateAsync(new AwkEvaluationContext(state))).AsNumber());
        Assert.Equal(allocations, counters.AwkTextAllocations);
        bytes[2] = (byte)'8';
        Assert.Equal(19, await expression.ResolveNumberAsync(new AwkEvaluationContext(state)));
        state.AssignFieldValue(1, AwkValue.FromNumber(1.25));
        Assert.Equal(5.5, await expression.ResolveNumberAsync(new AwkEvaluationContext(state)));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LeftNumericFailureStopsTheRightOperand(bool numeric)
    {
        var (state, _) = State(true);
        var right = new EffectExpression();
        var expression = AwkExpression.Add(
            AwkExpression.Divide(AwkExpression.Number("1", 1), AwkExpression.Number("0", 0)), right);
        var error = numeric
            ? await Record.ExceptionAsync(async () => { _ = await expression.ResolveNumberAsync(new AwkEvaluationContext(state)); })
            : await Record.ExceptionAsync(async () => { _ = await expression.EvaluateAsync(new AwkEvaluationContext(state)); });
        Assert.IsType<AwkRuntimeException>(error);
        Assert.Equal(0, right.Effects);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronousConversionFailureStopsTheRightOperand(bool numeric)
    {
        var (state, counters) = State(true);
        var left = new SynchronousOperand(AwkValue.FromNumericString("0x1.5", AwkNumberParsingMode.NonDecimalData));
        var right = new SynchronousOperand(AwkValue.FromNumber(3));
        var expression = AwkExpression.Add(left, right);
        var context = new AwkEvaluationContext(state);
        var error = numeric
            ? await Record.ExceptionAsync(async () => { _ = await expression.ResolveNumberAsync(context); })
            : await Record.ExceptionAsync(async () => { _ = await expression.EvaluateAsync(context); });
        Assert.IsType<AwkRuntimeException>(error);
        Assert.Equal(1, left.Entries);
        Assert.Equal(0, right.Entries);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private sealed class SynchronousOperand(AwkValue value) : AwkSynchronousExpression
    {
        public int Entries { get; private set; }
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            Entries++;
            return value;
        }
    }

    private static AwkExpression Field(int index) =>
        AwkExpression.FieldIndex(AwkExpression.Number(index.ToString(System.Globalization.CultureInfo.InvariantCulture), index));

    private sealed class EffectExpression : AwkExpression
    {
        public int Effects { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            Effects++;
            return ValueTask.FromResult(AwkValue.FromNumber(3));
        }
    }

    private static (AwkRuntimeState, AwkRuntimeCounters) State(bool finite)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits);
        return (AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options), counters);
    }
}
