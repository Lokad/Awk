using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkStoredValueTests
{
    [Fact]
    public void StoredTextReadPreservesDeferredErrorsAndDoesNotPromoteUntypedOrigins()
    {
        var bytes = AwkText.FromSourceLiteralString("0x1.2");
        var slot = AwkValue.ForVariableStorage(AwkValue.FromDeferredText(bytes, AwkNumberParsingMode.NonDecimalData));
        Assert.True(slot.TryGetStoredScalarText(out var text));
        Assert.True(bytes.HasSameStorage(text));
        var value = slot.ReadVariableStorage(AwkNumberParsingMode.NonDecimalData, AwkRuntimeCounters.Disabled);
        Assert.Throws<AwkRuntimeException>(() => value.AsNumber());
        Assert.False(AwkValue.FromNumber(1.25).TryGetStoredScalarText(out _));
        var origin = new AwkUntypedScalar();
        Assert.False(AwkValue.FromUntypedScalar(origin).TryGetStoredScalarText(out _));
        Assert.False(origin.IsScalar);
    }

    [Fact]
    public void AwkValue_StoresTextAsAwkText()
    {
        var slot = AwkValue.ForStoredInputText("café");

        Assert.Equal(AwkText.FromString("café"), slot.ByteText);
    }

    [Fact]
    public void AwkValue_PreservesNumericStringKind()
    {
        var slot = AwkValue.ForStoredInputText("001");
        var value = slot.ReadVariableStorage(AwkNumberParsingMode.Decimal, AwkRuntimeCounters.Disabled);

        Assert.True(value.IsNumericStringValue);
        Assert.Equal("001", value.AsString());
    }

    [Fact]
    public void AwkValue_ConvertsAwkTextWithoutStringDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var slot = AwkValue.ForStoredInputText(AwkText.FromHostBytes("001"u8.ToArray(), counters));
        var value = slot.ReadVariableStorage(AwkNumberParsingMode.Decimal, AwkRuntimeCounters.Disabled);

        Assert.True(value.IsNumericStringValue);
        Assert.Equal(1, value.AsNumber());
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public void AwkValue_RetainsNumberWithoutTextProjection()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var slot = AwkValue.ForVariableStorage(AwkValue.FromNumber(42));
        var value = slot.ReadVariableStorage(AwkNumberParsingMode.Decimal, AwkRuntimeCounters.Disabled);

        Assert.Equal(0, slot.RetainedTextMemoryBytes);
        Assert.True(value.IsNumberValue);
        Assert.Equal(42, value.AsNumber());
        Assert.Equal("42", value.AsString(counters));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }
}
