using Lokad.Awk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkPrintfFormattingTests
{
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
            var actual = AwkFormattingRuntime.FormatPrintfStrictByteText("%d",
                new AwkSingleValuePrintfArguments(AwkValue.FromNumber(number), counters),
                state.Limits, counters, state.MemoryAccounting);
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
        var text = AwkFormattingRuntime.FormatPrintfStrictByteText(format,
            new AwkValueArrayPrintfArguments(values, 1, state),
            AwkRuntimeLimits.CreateDefault(), state.Counters, state.MemoryAccounting);
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
            for (var index = 0; index < 1000; index++)
                ReadValue(arguments, state);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = 0.0;
            for (var index = 0; index < 10000; index++)
                sum += ReadValue(arguments, state);
            return (sum, GC.GetAllocatedBytesForCurrentThread() - before);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        Assert.Equal(15000, sum);
        Assert.Equal(0, allocatedBytes);

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
    public void HexadecimalUsesItsExactOutputSizeAtTheValueLimit()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        AwkValue[] values = [AwkValue.FromNumber(1)];
        var text = AwkFormattingRuntime.FormatPrintfStrictByteText("%a",
            new AwkValueArrayPrintfArguments(values, 1, state),
            AwkRuntimeLimits.CreateDefault(), counters, state.MemoryAccounting);
        Assert.Equal("0x1p+0", text.AsString());
        Assert.Equal(1, counters.FormattingFallbacks);
        Assert.Equal(1, counters.FormattingStringInteropInputs);
        Assert.Equal(1, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
