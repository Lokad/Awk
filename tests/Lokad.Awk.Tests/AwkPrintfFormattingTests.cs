using System.Runtime.CompilerServices;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkPrintfFormattingTests
{
    [Fact]
    public void ZeroStringPrecisionDoesNotMaterializeConcatenatedText()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromString(new string('q', 1_000_000))
            .Concat(AwkText.FromString("tail"), counters, AwkMemoryAccounting.Disabled);
        var arguments = new AwkSingleValuePrintfArguments(
            AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), counters);
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled, capacity: 0);
        PrintfEvaluation.FormatToBuilder(builder, "%.0s", arguments, PrintfMissingArgumentPolicy.Throw,
            counters, AwkMemoryAccounting.Disabled);
        Assert.Equal(0, builder.Length);
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData(false, "a")] [InlineData(true, "a")]
    [InlineData(false, "é")] [InlineData(true, "é")]
    [InlineData(false, "😀")] [InlineData(true, "😀")]
    public void StringPrecisionDoesNotDecodeTheRemainingText(bool governed, string first)
    {
        foreach (var format in new[] { "%.0s", "%.1s", "%6.1s" })
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            using var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
            using (var builder = new AwkUtf8Builder(memory.Accounting, capacity: 16))
            {
                var warm = new AwkSingleValuePrintfArguments(AwkValue.FromText(first), AwkRuntimeCounters.Disabled);
                PrintfEvaluation.FormatToBuilder(builder, format, warm, PrintfMissingArgumentPolicy.Throw,
                    AwkRuntimeCounters.Disabled, memory.Accounting);
                builder.Clear();
                var text = AwkText.FromString(first + new string('q', 1_000_000));
                var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(text), counters);
                var before = GC.GetAllocatedBytesForCurrentThread();
                PrintfEvaluation.FormatToBuilder(builder, format, arguments, PrintfMissingArgumentPolicy.Throw,
                    counters, memory.Accounting);
                Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 8192);
                var expected = format == "%.0s" ? "" : (format == "%6.1s" ? "     " : "") + first;
                Assert.Equal(System.Text.Encoding.UTF8.GetBytes(expected), builder.AsSpan().ToArray());
                Assert.Equal(0, counters.StringDecodes);
                Assert.Equal(0, counters.FormattingStringInteropInputs);
            }
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false, "a")] [InlineData(true, "a")]
    [InlineData(false, "é")] [InlineData(true, "é")]
    [InlineData(false, "😀")] [InlineData(true, "😀")]
    public void CharacterConversionDoesNotDecodeTheRemainingText(bool governed, string first)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        using (var builder = new AwkUtf8Builder(memory.Accounting, capacity: 16))
        {
            // Prime formatter code without warming the large input's decode cache.
            var warm = new AwkSingleValuePrintfArguments(AwkValue.FromText(first), AwkRuntimeCounters.Disabled);
            PrintfEvaluation.FormatToBuilder(builder, "%c", warm, PrintfMissingArgumentPolicy.Throw,
                AwkRuntimeCounters.Disabled, memory.Accounting);
            builder.Clear();
            var input = AwkText.FromString(first + new string('q', 1_000_000));
            var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(input), counters);
            var before = GC.GetAllocatedBytesForCurrentThread();
            PrintfEvaluation.FormatToBuilder(builder, "%c", arguments, PrintfMissingArgumentPolicy.Throw,
                counters, memory.Accounting);
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 8192);
            Assert.Equal(System.Text.Encoding.UTF8.GetBytes(first), builder.AsSpan().ToArray());
            Assert.Equal(0, counters.StringDecodes);
            Assert.Equal(0, counters.FormattingStringInteropInputs);
        }
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void SignedDecimalDigitsMatchFixedFormatAcrossBinary64Boundaries()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var observed = 0;
        void Check(double number)
        {
            var integral = Math.Truncate(number);
            var expected = (integral < 0 ? "-" : "") +
                Math.Abs(integral).ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
            var actual = AwkFormattingRuntime.FormatPrintfStrictByteText("%d", new AwkSingleValuePrintfArguments(AwkValue.FromNumber(number), counters), counters, state.MemoryAccounting);
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
        Assert.InRange(counters.PrintfSignedIntegerFastPaths, 1, observed - 1);
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
        var text = AwkFormattingRuntime.FormatPrintfStrictByteText(format, new AwkValueArrayPrintfArguments(values, 1, state), state.Counters, state.MemoryAccounting);
        Assert.Equal(byteCount, text.ByteLength);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task PositionalSelectionDoesNotBoxTheArgumentProvider()
    {
        var (sum, allocatedBytes) = await Task.Factory.StartNew(static () =>
        {
            var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
            AwkValue[] values = [AwkValue.FromNumber(8), AwkValue.FromNumber(1.5)];
            var arguments = new AwkValueArrayPrintfArguments(values, 2, state);
            // Warm the same loop that is measured, including its generated code.
            _ = ReadSum(arguments, state);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = ReadSum(arguments, state);
            return (sum, GC.GetAllocatedBytesForCurrentThread() - before);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        Assert.Equal(15000, sum);
        Assert.Equal(0, allocatedBytes);

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
                PrintfMissingArgumentPolicy.Throw, format, state.Counters, state.MemoryAccounting);
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
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        AwkValue[] values = [AwkValue.FromNumber(1)];
        var text = AwkFormattingRuntime.FormatPrintfStrictByteText("%a", new AwkValueArrayPrintfArguments(values, 1, state), counters, state.MemoryAccounting);
        Assert.Equal("0x1p+0", text.AsString());
        Assert.Equal(1, counters.FormattingFallbacks);
        Assert.Equal(1, counters.FormattingStringInteropInputs);
        Assert.Equal(1, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
