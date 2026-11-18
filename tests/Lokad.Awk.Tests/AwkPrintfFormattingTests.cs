using System.Runtime.CompilerServices;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkPrintfFormattingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiteralRunsUseAdmittedCapacityWithoutTextInterop(bool governed)
    {
        var prefix = new string('x', 4095) + "😀%%:" + new string('é', 4096);
        var format = prefix + "%04d:end";
        var expected = AwkUtf8Codec.Encode(prefix.Replace("%%", "%", StringComparison.Ordinal) + "0007:end");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(64 << 10, enabled: governed).InitializeRuntime(counters);
        var memoryBaseline = memory.Accounting.CurrentCommittedBytes;
        var memoryBudget = memory.Budget;
        using (memory)
        {
            using (var builder = new AwkUtf8Builder(memory.Accounting, capacity: 32 << 10))
            using (var pressure = memory.Accounting.ReserveTransient(memory.Budget.Limit - memory.Accounting.CurrentCommittedBytes))
            {
                var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromNumber(7));
                PrintfEvaluation.FormatToBuilder(builder, format, arguments, PrintfMissingArgumentPolicy.Throw,
                    memory.State);
                Assert.Equal(expected.ToArray(), builder.AsSpan().ToArray());
                Assert.Equal(0, counters[AwkCounter.StringDecodes]);
                Assert.Equal(0, counters[AwkCounter.TextAllocations]);

            }
            Assert.Equal(memoryBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void ZeroStringPrecisionDoesNotMaterializeConcatenatedText()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var text = state.ConcatText([AwkText.FromString(new string('q', 1_000_000)), AwkText.FromString("tail")]);
        var arguments = new AwkSingleValuePrintfArguments(
            AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal));
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled, capacity: 0);
        PrintfEvaluation.FormatToBuilder(builder, "%.0s", arguments, PrintfMissingArgumentPolicy.Throw,
            state);
        Assert.Equal(0, builder.Length);
        Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
    }

    [Theory]
    [InlineData(false, "a")]
    [InlineData(true, "a")]
    [InlineData(false, "é")]
    [InlineData(true, "é")]
    [InlineData(false, "😀")]
    [InlineData(true, "😀")]
    public void StringPrecisionDoesNotDecodeTheRemainingText(bool governed, string first)
    {
        foreach (var format in new[] { "%.0s", "%.1s", "%6.1s" })
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var memory = new CooperativeTestMemory(1 << 20, enabled: governed).InitializeRuntime(counters);
            var memoryBaseline = memory.Accounting.CurrentCommittedBytes;
            var memoryBudget = memory.Budget;
            using (memory)
            {
                using (var builder = new AwkUtf8Builder(memory.Accounting, capacity: 16))
                {
                    var warm = new AwkSingleValuePrintfArguments(AwkValue.FromText(first));
                    PrintfEvaluation.FormatToBuilder(builder, format, warm, PrintfMissingArgumentPolicy.Throw,
                        memory.State);
                    builder.Clear();
                    var text = AwkText.FromString(first + new string('q', 1_000_000));
                    var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(text));
                    var before = GC.GetAllocatedBytesForCurrentThread();
                    PrintfEvaluation.FormatToBuilder(builder, format, arguments, PrintfMissingArgumentPolicy.Throw,
                        memory.State);
                    Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 8192);
                    var expected = format == "%.0s" ? "" : (format == "%6.1s" ? "     " : "") + first;
                    Assert.Equal(System.Text.Encoding.UTF8.GetBytes(expected), builder.AsSpan().ToArray());
                    Assert.Equal(0, counters[AwkCounter.StringDecodes]);

                }
                Assert.Equal(memoryBaseline, memory.Budget.Reserved);
            }
            Assert.Equal(0, memoryBudget.Reserved);
        }
    }

    [Theory]
    [InlineData(false, "a")]
    [InlineData(true, "a")]
    [InlineData(false, "é")]
    [InlineData(true, "é")]
    [InlineData(false, "😀")]
    [InlineData(true, "😀")]
    public void CharacterConversionDoesNotDecodeTheRemainingText(bool governed, string first)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(1 << 20, enabled: governed).InitializeRuntime(counters);
        var memoryBaseline = memory.Accounting.CurrentCommittedBytes;
        var memoryBudget = memory.Budget;
        using (memory)
        {
            using (var builder = new AwkUtf8Builder(memory.Accounting, capacity: 16))
            {
                // Prime formatter code without warming the large input's decode cache.
                var warm = new AwkSingleValuePrintfArguments(AwkValue.FromText(first));
                PrintfEvaluation.FormatToBuilder(builder, "%c", warm, PrintfMissingArgumentPolicy.Throw,
                    memory.State);
                builder.Clear();
                var input = AwkText.FromString(first + new string('q', 1_000_000));
                var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(input));
                var before = GC.GetAllocatedBytesForCurrentThread();
                PrintfEvaluation.FormatToBuilder(builder, "%c", arguments, PrintfMissingArgumentPolicy.Throw,
                    memory.State);
                Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 8192);
                Assert.Equal(System.Text.Encoding.UTF8.GetBytes(first), builder.AsSpan().ToArray());
                Assert.Equal(0, counters[AwkCounter.StringDecodes]);

            }
            Assert.Equal(memoryBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void SignedDecimalDigitsMatchFixedFormatAcrossBinary64Boundaries()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var observed = 0;
        void Check(double number)
        {
            var integral = Math.Truncate(number);
            var expected = (integral < 0 ? "-" : "") +
                Math.Abs(integral).ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
            var actual = PrintfEvaluation.FormatByteText("%d", new AwkSingleValuePrintfArguments(AwkValue.FromNumber(number)), PrintfMissingArgumentPolicy.Throw, state);
            Assert.Equal(expected, AwkUtf8Codec.Decode(actual.AsSpan()));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            observed++;
        }
        foreach (var number in new[] { 0.0, -0.0, 7.75, -7.75, double.MaxValue, -double.MaxValue }) Check(number);
        for (var exponent = -1074; exponent <= 1023; exponent++)
        {
            var value = Math.ScaleB(1, exponent);
            foreach (var adjacent in new[] { Math.BitDecrement(value), value, Math.BitIncrement(value) })
            {
                Check(adjacent);
                Check(-adjacent);
            }
        }

    }

    [Theory]
    [InlineData("%d", double.MaxValue, 309)]
    [InlineData("%d", -double.MaxValue, 310)]
    [InlineData("%.308u", double.MaxValue, 314)]
    [InlineData("%.308u", -double.MaxValue, 315)]
    public void ExtremeIntegerFormattingHasBoundedExactSize(string format, double number, int byteCount)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        AwkValue[] values = [AwkValue.FromNumber(number)];
        var text = PrintfEvaluation.FormatByteText(format, new AwkValueArrayPrintfArguments(values, 1, state), PrintfMissingArgumentPolicy.Throw, state);
        Assert.Equal(byteCount, text.ByteLength);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task PositionalSelectionDoesNotBoxTheArgumentProvider()
    {
        var (sum, allocatedBytes) = await Task.Factory.StartNew(static () =>
        {
            using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
            AwkValue[] values = [AwkValue.FromNumber(8), AwkValue.FromNumber(1.5)];
            var arguments = new AwkValueArrayPrintfArguments(values, 2, state);
            // Measure this provider's warmed thread without process-wide GC controls.
            _ = ReadSum(arguments, state);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = ReadSum(arguments, state);
            return (sum, GC.GetAllocatedBytesForCurrentThread() - before);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        Assert.Equal(15000, sum);
        // Allow incidental runtime allocations while detecting a box on each of the 10,000 reads.
        Assert.InRange(allocatedBytes, 0, 16 * 1024);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double ReadSum(AwkValueArrayPrintfArguments arguments, AwkRuntimeState state)
        {
            var sum = 0.0;
            for (var index = 0; index < 10000; index++)
                sum += ReadValue(arguments, state);
            return sum;
        }

        static double ReadValue(AwkValueArrayPrintfArguments arguments, AwkRuntimeState state)
        {
            const string format = "%2$*1$a";
            var cursor = new PrintfArgumentCursor<AwkValueArrayPrintfArguments>(arguments,
                PrintfMissingArgumentPolicy.Throw, format, state);
            var directive = PrintfDirectiveParser.Read(format, 0, AwkCompatibilityMode.GnuDefault, ref cursor);
            if (!cursor.TryReadIndex(directive.ArgumentPosition, out var argumentIndex))
                throw new InvalidOperationException("Missing format value.");
            return arguments.GetNumber(argumentIndex);
        }
    }

    [Fact]
    public void HexadecimalFormattingKeepsDigitsInUtf8WithoutDecoding()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        AwkValue[] values = [AwkValue.FromNumber(1)];
        var text = PrintfEvaluation.FormatByteText("%a", new AwkValueArrayPrintfArguments(values, 1, state), PrintfMissingArgumentPolicy.Throw, state);
        Assert.Equal("0x1p+0", text.AsString());

        Assert.Equal(1, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
