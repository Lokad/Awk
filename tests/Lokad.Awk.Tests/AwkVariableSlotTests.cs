using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkVariableSlotTests
{
    [Fact]
    public void AwkVariableSlot_StoresTextAsAwkText()
    {
        var slot = AwkVariableSlot.FromExternalText("café");

        Assert.Equal(AwkText.FromString("café"), slot.ByteText);
        Assert.Equal("café", slot.StringValue);
    }

    [Fact]
    public void AwkVariableSlot_PreservesNumericStringKind()
    {
        var slot = AwkVariableSlot.FromExternalText("001");
        var value = slot.ToValue(
            AwkValue.FromText,
            AwkValue.FromNumericString,
            text => AwkValue.FromNumber(AwkValue.FromText(text).AsNumber()));

        Assert.True(value.IsNumericStringValue);
        Assert.Equal("001", value.AsString());
    }

    [Fact]
    public void AwkVariableSlot_ConvertsAwkTextWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var slot = AwkVariableSlot.FromExternalText(AwkText.FromHostBytes("001"u8.ToArray(), counters));
        var value = slot.ToValue(
            text => AwkValue.FromText(text, AwkNumberParsingMode.Decimal),
            text => AwkValue.FromNumericString(text, AwkNumberParsingMode.Decimal),
            text => AwkValue.FromNumber(AwkValue.FromText(text).AsNumber()));

        Assert.True(value.IsNumericStringValue);
        Assert.Equal(1, value.AsNumber());
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public void AwkVariableSlot_RetainsNumberWithoutTextProjection()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var slot = AwkVariableSlot.FromValue(AwkValue.FromNumber(42));
        var value = slot.ToValue(
            text => AwkValue.FromText(text, AwkNumberParsingMode.Decimal),
            text => AwkValue.FromNumericString(text, AwkNumberParsingMode.Decimal),
            text => AwkValue.FromNumber(AwkValue.FromText(text).AsNumber()));

        Assert.Equal(0, slot.RetainedTextByteLength);
        Assert.True(value.IsNumberValue);
        Assert.Equal(42, value.AsNumber());
        Assert.Equal("42", value.AsString(counters));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }
}
