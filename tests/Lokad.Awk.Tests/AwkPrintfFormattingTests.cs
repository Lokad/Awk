using Lokad.Awk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkPrintfFormattingTests
{
    [Theory]
    [InlineData("%.5d", 2, "00002")]
    [InlineData("%+.5d", -2, "-00002")]
    [InlineData("%#.5o", 7, "00007")]
    [InlineData("%#.5x", 7, "0x00007")]
    [InlineData("%.0d", 0, "")]
    [InlineData("%u", 9223372036854775808.0, "9223372036854775808")]
    [InlineData("%d", 1e20, "100000000000000000000")]
    [InlineData("%u", 1e20, "1e+20")]
    [InlineData("%.1000000u", 1e20, "100000000000000000000")]
    public void IntegerFormattingFitsItsExactValueLimit(string format, double number, string expected)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        AwkValue[] values = [AwkValue.FromNumber(number)];
        var text = AwkFormattingRuntime.FormatPrintfStrictByteText(format,
            new AwkValueArrayPrintfArguments(values, 1, state),
            AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(Math.Max(1, expected.Length)), counters, state.MemoryAccounting);
        Assert.Equal(expected, text.AsString());
        Assert.Equal(1, counters.FormattingFallbacks);
        Assert.Equal(1, counters.FormattingStringInteropInputs);
        Assert.Equal(expected.Length == 0 ? 0 : 1, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
            AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(byteCount), state.Counters, state.MemoryAccounting);
        Assert.Equal(byteCount, text.ByteLength);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("%.10000d", 1, 32, false)]
    [InlineData("%+.2147483647d", 1, 32, false)]
    [InlineData("%1$.*2$x", 1, 32, false)]
    [InlineData("%#.10000x", 1, 32, false)]
    [InlineData("%d", double.MaxValue, 308, false)]
    [InlineData("%.308u", double.MaxValue, 313, false)]
    [InlineData("%.308u", -double.MaxValue, 314, false)]
    [InlineData("%.10000d", 1, 32, true)]
    [InlineData("é%1$.100d", 1, 32, false)]
    public void IntegerOutputFailureReleasesTheBuilder(string format, double number, int maximum, bool builderLimit)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        AwkValue[] values = [AwkValue.FromNumber(number), AwkValue.FromNumber(10000)];
        var limits = builderLimit ? AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(maximum)
            : AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(maximum);
        var error = Assert.Throws<AwkRuntimeException>(() => AwkFormattingRuntime.FormatPrintfStrictByteText(format,
            new AwkValueArrayPrintfArguments(values, 2, state), limits, counters, state.MemoryAccounting));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IntegerFormattingPreservesEarlierOutputAtTheOutputLimit(bool useExecutable)
    {
        var host = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(host,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"before\";printf \"%.5d\",2}"), CancellationToken.None);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
            AwkRuntimeLimits.CreateDefault().WithMaxOutputBytes(7), AwkRegexEngineKind.DotNetCompatibility,
            useExecutable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
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
            AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(6), counters, state.MemoryAccounting);
        Assert.Equal("0x1p+0", text.AsString());
        Assert.Equal(1, counters.FormattingFallbacks);
        Assert.Equal(1, counters.FormattingStringInteropInputs);
        Assert.Equal(1, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("%a", 5)]
    [InlineData("%1$.*2$a", 32)]
    [InlineData("%1$*2$a", 32)]
    public void HexadecimalRejectsOversizedOutputBeforeBuilderReservation(string format, int maximum)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        AwkValue[] values = [AwkValue.FromNumber(1), AwkValue.FromNumber(10000)];
        var error = Assert.Throws<AwkRuntimeException>(() => AwkFormattingRuntime.FormatPrintfStrictByteText(format,
            new AwkValueArrayPrintfArguments(values, 2, state),
            AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(maximum), counters, state.MemoryAccounting));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
