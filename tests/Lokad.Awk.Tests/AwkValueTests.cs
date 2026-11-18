using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkValueTests
{
    [Fact]
    public void AwkValue_StoresTextAsAwkText()
    {
        var value = AwkValue.FromText("café");

        Assert.Equal(AwkText.FromString("café"), value.ByteText);
        Assert.Equal("café", value.AsString());
        Assert.True(value.IsTruthy());
    }

    [Fact]
    public void AwkValue_PreservesNumericStringState()
    {
        var value = AwkTestHarness.NumericInput("001");

        Assert.True(value.IsNumericStringValue);
        Assert.Equal(1, value.AsNumber());
        Assert.Equal("001", value.AsString());
    }

    [Fact]
    public void AwkValue_SeparatesArithmeticNumericTextFromComparisonNumericValues()
    {
        var text = AwkValue.FromText("001");
        var numericString = AwkTestHarness.NumericInput("001");
        var number = AwkValue.FromNumber(1);

        Assert.Equal(1, text.AsNumber());
        Assert.False(text.TryGetNumber(out _));
        Assert.True(text.IsTruthy());
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);
        Assert.False(AwkComparisonOperations.Evaluate(text, number, AwkComparison.Equal, state));
        Assert.True(AwkComparisonOperations.Evaluate(numericString, number, AwkComparison.Equal, state));
    }

    [Fact]
    public void AwkValue_ParsesNumericAwkTextWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var value = AwkTestHarness.NumericInput(AwkTestHarness.CopyHostText("+inf"u8.ToArray()));

        Assert.True(value.IsNumericStringValue);
        Assert.Equal(double.PositiveInfinity, value.AsNumber());
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

    }

    [Fact]
    public void AwkValue_ParsesIncompleteExponentFromAwkTextWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var value = AwkTestHarness.NumericInput(AwkTestHarness.CopyHostText("1e"u8.ToArray()));

        Assert.False(value.IsNumericStringValue);
        Assert.Equal(1, value.AsNumber());
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

    }

    [Fact]
    public void AwkValue_NumberStringifiesWithoutAwkTextDecode()
    {
        var value = AwkValue.FromNumber(123);

        Assert.True(value.IsNumberValue);
        Assert.Equal(123, value.AsNumber());
        Assert.Equal("123", value.AsString());
    }

    [Fact]
    public void AwkValue_IntegerConversionFormattingAvoidsStringProjection()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var formatter = new AwkFormattingRuntime(
            static _ => AwkText.FromSourceLiteralString("%.6g"), state);

        var text = formatter.FormatStringByteText(AwkValue.FromNumber(123));

        Assert.Equal("123", text.AsString());
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);

        Assert.Equal(1, counters[AwkCounter.TextAllocations]);
        Assert.Equal(3, counters[AwkCounter.TextBytes]);
    }

    [Fact]
    public void AwkValue_DefaultNumericFormattingAvoidsPrintfFallback()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var defaultFormat = AwkText.FromSourceLiteralString("%.6g");
        var formatter = new AwkFormattingRuntime(
            _ => defaultFormat, state);

        var oneThird = formatter.FormatPrintByteText(AwkValue.FromNumber(1.0 / 3));
        var exponent = formatter.FormatPrintByteText(AwkValue.FromNumber(1_234_567.5));
        var conversion = formatter.FormatStringByteText(AwkValue.FromNumber(1.0 / 3));

        Assert.Equal("0.333333", oneThird.AsString());
        Assert.Equal("1.23457e+06", exponent.AsString());
        Assert.Equal("0.333333", conversion.AsString());

        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(3, counters[AwkCounter.TextAllocations]);
    }

    [Fact]
    public void AwkValue_UsesByteComparisonWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var left = AwkValue.FromText(AwkTestHarness.CopyHostText("café"u8.ToArray()));
        var right = AwkValue.FromText(AwkTestHarness.CopyHostText("caff"u8.ToArray()));

        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var result = AwkComparisonOperations.Evaluate(left, right, AwkComparison.GreaterThan, state);

        Assert.True(result);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

    }
}
