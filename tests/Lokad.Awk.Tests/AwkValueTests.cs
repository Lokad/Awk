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
        var value = AwkValue.FromNumericString("001");

        Assert.True(value.IsNumericStringValue);
        Assert.Equal(1, value.AsNumber());
        Assert.Equal("001", value.AsString());
    }

    [Fact]
    public void AwkValue_SeparatesArithmeticNumericTextFromComparisonNumericValues()
    {
        var text = AwkValue.FromText("001");
        var numericString = AwkValue.FromNumericString("001");
        var number = AwkValue.FromNumber(1);

        Assert.Equal(1, text.AsNumber());
        Assert.False(text.TryGetNumber(out _));
        Assert.True(text.IsTruthy());
        Assert.NotEqual(0, AwkValue.Compare(text, number));
        Assert.Equal(0, AwkValue.Compare(numericString, number));
    }

    [Fact]
    public void AwkValue_ParsesNumericAwkTextWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var value = AwkValue.FromNumericString(AwkText.FromHostBytes("+inf"u8.ToArray(), counters));

        Assert.True(value.IsNumericStringValue);
        Assert.Equal(double.PositiveInfinity, value.AsNumber());
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public void AwkValue_ParsesIncompleteExponentFromAwkTextWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var value = AwkValue.FromNumericString(AwkText.FromHostBytes("1e"u8.ToArray(), counters));

        Assert.False(value.IsNumericStringValue);
        Assert.Equal(1, value.AsNumber());
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public void AwkValue_NumberStringifiesWithoutAwkTextDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var value = AwkValue.FromNumber(123);

        Assert.True(value.IsNumberValue);
        Assert.Equal(123, value.AsNumber());
        Assert.Equal("123", value.AsString(counters));
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public void AwkValue_IntegerConversionFormattingAvoidsStringProjection()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters);
        var formatter = new AwkFormattingRuntime(
            static _ => new AwkFormatString(AwkText.FromSourceLiteralString("%.6g")),
            counters,
            governor);

        var text = formatter.FormatStringByteText(AwkValue.FromNumber(123));

        Assert.Equal("123", text.AsString());
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(1, counters.AwkTextAllocations);
        Assert.Equal(3, counters.AwkTextPayloadBytes);
    }

    [Fact]
    public void AwkValue_DefaultNumericFormattingAvoidsPrintfFallback()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters);
        var defaultFormat = new AwkFormatString(AwkText.FromSourceLiteralString("%.6g"));
        var formatter = new AwkFormattingRuntime(
            _ => defaultFormat,
            counters,
            governor);

        var oneThird = formatter.FormatPrintByteText(AwkValue.FromNumber(1.0 / 3));
        var exponent = formatter.FormatPrintByteText(AwkValue.FromNumber(1_234_567));
        var conversion = formatter.FormatStringByteText(AwkValue.FromNumber(1.0 / 3));

        Assert.Equal("0.333333", oneThird.AsString());
        Assert.Equal("1.23457e+06", exponent.AsString());
        Assert.Equal("0.333333", conversion.AsString());
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(3, counters.AwkTextAllocations);
    }

    [Fact]
    public void AwkValue_UsesByteComparisonWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var left = AwkValue.FromText(AwkText.FromHostBytes("café"u8.ToArray(), counters));
        var right = AwkValue.FromText(AwkText.FromHostBytes("caff"u8.ToArray(), counters));

        var result = AwkValue.Compare(left, right, counters);

        Assert.True(result > 0);
        Assert.Equal(1, counters.ByteTextComparisons);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }
}
