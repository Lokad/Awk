using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkVariableSlotTests
{
    [Fact]
    public void StoredTextReadPreservesDeferredErrorsAndDoesNotPromoteUntypedOrigins()
    {
        var bytes = AwkText.FromSourceLiteralString("0x1.2");
        var slot = AwkVariableSlot.FromValue(AwkValue.FromDeferredText(bytes, AwkNumberParsingMode.NonDecimalData));
        Assert.True(slot.TryGetStoredScalarText(out var text));
        Assert.Same(bytes, text);
        var value = slot.ToValue(AwkValue.FromNumericString,
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.NonDecimalData));
        Assert.Throws<AwkRuntimeException>(() => value.AsNumber());
        Assert.False(AwkVariableSlot.FromNumber(1.25).TryGetStoredScalarText(out _));
        var origin = new AwkUntypedScalar();
        Assert.False(AwkVariableSlot.FromUntypedScalar(origin).TryGetStoredScalarText(out _));
        Assert.False(origin.IsScalar);
    }

    [Fact]
    public void AwkVariableSlot_StoresTextAsAwkText()
    {
        var slot = AwkVariableSlot.FromExternalText("café");

        Assert.Equal(AwkText.FromString("café"), slot.ByteText);
    }

    [Fact]
    public void AwkVariableSlot_PreservesNumericStringKind()
    {
        var slot = AwkVariableSlot.FromExternalText("001");
        var value = slot.ToValue(AwkValue.FromNumericString,
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal));

        Assert.True(value.IsNumericStringValue);
        Assert.Equal("001", value.AsString());
    }

    [Fact]
    public void AwkVariableSlot_ConvertsAwkTextWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var slot = AwkVariableSlot.FromExternalText(AwkText.FromHostBytes("001"u8.ToArray(), counters));
        var value = slot.ToValue(text => AwkValue.FromNumericString(text, AwkNumberParsingMode.Decimal),
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal));

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
        var value = slot.ToValue(text => AwkValue.FromNumericString(text, AwkNumberParsingMode.Decimal),
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal));

        Assert.Equal(0, slot.RetainedTextMemoryBytes);
        Assert.True(value.IsNumberValue);
        Assert.Equal(42, value.AsNumber());
        Assert.Equal("42", value.AsString(counters));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }
}
