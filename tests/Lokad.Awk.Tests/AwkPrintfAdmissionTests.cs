namespace Lokad.Awk.Tests;

public sealed class AwkPrintfAdmissionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task AdmittedCapacityNeedsNoAdditionalMemoryToFormat(int mode)
    {
        const int limit = 1024;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(mode == 0 ? null : budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = mode == 1 ? AwkRuntimeLimits.CreateDefault()
            : AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(limit);
        var accounting = AwkMemoryAccounting.Create(limits, counters, owner);
        using (var builder = new AwkUtf8Builder(limits, accounting, capacity: 16))
        using (var pressure = accounting.ReserveTransient(limit - accounting.CurrentCommittedBytes))
        {
            var committed = accounting.CurrentCommittedBytes;
            var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.FromSourceLiteralString("payload")), counters);
            PrintfEvaluation.FormatToBuilder(builder, "%8s", arguments, PrintfMissingArgumentPolicy.Throw, limits, counters, accounting);
            Assert.Equal(" payload"u8.ToArray(), builder.AsSpan().ToArray());
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(1, counters.Utf8Encodes);
            Assert.Equal(1, counters.StringDecodes);
        }
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ActualGrowthStillRefusesBeforeMutationAndRefundsForRetry(int mode)
    {
        const int limit = 1024;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(mode == 0 ? null : budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = mode == 1 ? AwkRuntimeLimits.CreateDefault()
            : AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(limit);
        var accounting = AwkMemoryAccounting.Create(limits, counters, owner);
        using (var builder = new AwkUtf8Builder(limits, accounting, capacity: 16))
        {
            builder.Append("kept"u8);
            var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.FromSourceLiteralString("payload")), counters);
            using (var pressure = accounting.ReserveTransient(limit - accounting.CurrentCommittedBytes))
            {
                var committed = accounting.CurrentCommittedBytes;
                var error = Assert.Throws<AwkRuntimeException>(() => PrintfEvaluation.FormatToBuilder(builder,
                    "%32s", arguments, PrintfMissingArgumentPolicy.Throw, limits, counters, accounting));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal("kept"u8.ToArray(), builder.AsSpan().ToArray());
                Assert.Equal(committed, accounting.CurrentCommittedBytes);
                Assert.Equal(0, accounting.CurrentReservedBytes);
            }
            builder.Clear();
            PrintfEvaluation.FormatToBuilder(builder, "%32s", arguments, PrintfMissingArgumentPolicy.Throw,
                limits, counters, accounting);
            Assert.Equal(AwkUtf8Codec.Encode(new string(' ', 25) + "payload").ToArray(), builder.AsSpan().ToArray());
        }
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValueAndBuilderLimitsPrecedeArgumentConversion(bool valueLimit)
    {
        var limits = valueLimit ? AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(8)
            : AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(8);
        using var builder = new AwkUtf8Builder(limits, AwkMemoryAccounting.Disabled, capacity: 0);
        var arguments = new UnreachableArguments();
        var error = Assert.Throws<AwkRuntimeException>(() => PrintfEvaluation.FormatToBuilder(builder, "%9s", arguments,
            PrintfMissingArgumentPolicy.Throw, limits, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, builder.Length);
    }

    private sealed class UnreachableArguments : IPrintfArguments
    {
        public int Count => 1;
        public AwkText GetByteText(int index) => throw new InvalidOperationException("Conversion happened before bounds validation.");
        public double GetNumber(int index) => throw new InvalidOperationException("Unexpected numeric conversion.");
        public bool IsNumeric(int index) => false;
    }
}
