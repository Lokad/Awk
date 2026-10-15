using System.Globalization;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkNumberParserTests
{
    [Theory]
    [InlineData("0x1.00000000000008p-1075", "0000000000000001")]
    [InlineData("0x1p-1075", "0000000000000000")]
    [InlineData("0x1.00000000000018p-1023", "0008000000000001")]
    [InlineData("0x1.fffffffffffff8p1023", "7ff0000000000000")]
    [InlineData("0x1.fffffffffffffp1023", "7fefffffffffffff")]
    [InlineData("-0x0p0", "8000000000000000")]
    [InlineData("0x0000.0001p-1058", "0000000000000001")]
    [InlineData("0x1p-99999", "0000000000000000")]
    public void HexConversionRoundsDirectlyToBinary64WithoutTextDecoding(string source, string expectedBits)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromHostBytes(AwkUtf8Codec.Encode(source).ToArray(), counters);
        var allocations = counters.AwkTextAllocations;

        Assert.True(text.TryParseNumber(AwkNumberParsingMode.HexadecimalInput, AwkNumberTextExtent.EntireText, out var value));

        Assert.Equal(ulong.Parse(expectedBits, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            BitConverter.DoubleToUInt64Bits(value));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(allocations, counters.AwkTextAllocations);
    }

    [Fact]
    public void MissingHexExponentRetainsInputIdentityAcrossStorageUntilConversion()
    {
        var text = AwkText.FromSourceLiteralString("0x1.8");
        var input = AwkValue.FromNumericString(text, AwkNumberParsingMode.NonDecimalData);
        var slot = AwkVariableSlot.FromValue(input);
        var restored = slot.ToValue(
            static text => AwkValue.FromText(text, AwkNumberParsingMode.NonDecimalData),
            static text => AwkValue.FromNumericString(text, AwkNumberParsingMode.NonDecimalData),
            static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.NonDecimalData));
        var literal = AwkValue.FromText(text, AwkNumberParsingMode.NonDecimalData);

        Assert.Equal(text, restored.ByteText);
        Assert.Equal(AwkVariableSlotKind.NumericString, slot.Kind);
        Assert.True(literal.IsTruthy());
        Assert.False(literal.TryGetNumber(out _));
        Assert.Equal(AwkRuntimeValueKind.String, literal.RuntimeKind);
        Assert.Equal("0x1.8", restored.AsString());
        AssertFriendly(Assert.Throws<AwkRuntimeException>(() => restored.AsNumber()));
        AssertFriendly(Assert.Throws<AwkRuntimeException>(() => restored.IsTruthy()));
        AssertFriendly(Assert.Throws<AwkRuntimeException>(() => restored.TryGetNumber(out _)));
        AssertFriendly(Assert.Throws<AwkRuntimeException>(() => _ = restored.RuntimeKind));
        AssertFriendly(Assert.Throws<AwkRuntimeException>(() => literal.AsNumber()));

        static void AssertFriendly(AwkRuntimeException error) =>
            Assert.Equal("awk: hexadecimal floating point constants require an exponent\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    [Theory]
    [InlineData("0x10z", 16)]
    [InlineData("012z", 10)]
    [InlineData("0109", 109)]
    [InlineData("1.2x", 1.2)]
    public async Task StrtonumByteConversionDoesNotDecodeOrFormatItsArgument(string source, double expected)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault());
        Assert.True(AwkBuiltins.TryGetCallable("strtonum", out var builtin));
        var context = new AwkEvaluationContext(state);
        AwkExpression[] arguments = [AwkExpression.Literal(source)];

        var result = await builtin.EvaluateAsync(arguments, context);

        Assert.True(result.IsNumberValue);
        Assert.Equal(expected, result.AsNumber());
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.AwkTextAllocations);
    }

    [Fact]
    public async Task StrtonumNumericArgumentRetainsEveryBitAndAvoidsByteWork()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = CreateState(counters, AwkRuntimeLimits.CreateDefault());
        Assert.True(AwkBuiltins.TryGetCallable("strtonum", out var builtin));
        foreach (var number in new double[] { 1d / 3, -0d, double.Epsilon, Math.CopySign(double.NaN, -1) })
        {
            var before = counters.ExecutionSteps;
            var context = new AwkEvaluationContext(state);
            AwkExpression[] arguments = [AwkExpression.Number("value", number)];
            var result = await builtin.EvaluateAsync(arguments, context);
            Assert.Equal(BitConverter.DoubleToUInt64Bits(number), BitConverter.DoubleToUInt64Bits(result.AsNumber()));
            Assert.Equal(1, counters.ExecutionSteps - before);
        }
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.AwkTextAllocations);
    }

    [Theory]
    [InlineData(4096, 1)]
    [InlineData(4097, 2)]
    [InlineData(8193, 3)]
    public async Task StrtonumPreflightsOfferedByteWorkAtomically(int byteCount, int chunks)
    {
        Assert.True(AwkBuiltins.TryGetCallable("strtonum", out var builtin));
        var expression = AwkExpression.Literal("0x" + new string('0', byteCount - 3) + "1");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var admitted = CreateState(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(chunks + 1));
        var value = await builtin.EvaluateAsync([expression], new AwkEvaluationContext(admitted));
        Assert.Equal(1, value.AsNumber());
        Assert.Equal(chunks + 1, counters.ExecutionSteps);

        var rejectedCounters = AwkRuntimeCounters.CreateEnabled();
        var rejected = CreateState(rejectedCounters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(chunks));
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await builtin.EvaluateAsync([expression], new AwkEvaluationContext(rejected)));
        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(1, rejectedCounters.ExecutionSteps);
        Assert.Equal(0, rejectedCounters.StringDecodes);
    }

    [Fact]
    public void HexStickyBitsAndExponentScanningNeedNoMagnitudeSizedStorage()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromHostBytes(AwkUtf8Codec.Encode("0x1." + new string('0', 20_000) + "1p-1075").ToArray(), counters);
        var allocations = counters.AwkTextAllocations;
        Assert.True(text.TryParseNumber(AwkNumberParsingMode.HexadecimalInput, AwkNumberTextExtent.EntireText, out var value));
        Assert.Equal(double.Epsilon, value);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
    }

    private static AwkRuntimeState CreateState(AwkRuntimeCounters counters, AwkRuntimeLimits limits) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter));
}
