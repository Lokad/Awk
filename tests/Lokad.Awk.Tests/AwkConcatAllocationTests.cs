namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkConcatAllocationTests
{
    [Theory]
    [InlineData(false, 2)]
    [InlineData(false, 5)]
    [InlineData(false, 8)]
    [InlineData(false, 9)]
    [InlineData(true, 2)]
    [InlineData(true, 5)]
    [InlineData(true, 8)]
    [InlineData(true, 9)]
    public void RepeatedConcatenationDoesNotAllocateArgumentStorage(bool finiteMemory, int arity)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (finiteMemory) limits = limits.WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits,
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var context = new AwkEvaluationContext(state);
        var parts = Enumerable.Range(0, arity).Select(index => AwkExpression.Literal(index == arity / 2 ? "café" : "")).ToArray();
        var expression = AwkExpression.Concat(parts);
        for (var index = 0; index < 100; index++)
            _ = EvaluateCompleted(expression, context);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var bytes = 0;
        for (var index = 0; index < 1_000; index++)
            bytes += EvaluateCompleted(expression, context).ByteText.ByteLength;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(5_000, bytes);
        Assert.InRange(allocated, 0, 4_096);
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkValue EvaluateCompleted(AwkExpression expression, AwkEvaluationContext context)
    {
        var pending = expression.EvaluateAsync(context);
        if (!pending.IsCompletedSuccessfully)
            throw new InvalidOperationException("Allocation measurement requires completed operands.");
        return pending.Result;
    }
}
