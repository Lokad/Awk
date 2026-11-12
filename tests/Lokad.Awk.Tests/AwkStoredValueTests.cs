using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkStoredValueTests
{
    [Theory]
    [InlineData("Decimal", 10)]
    [InlineData("HexadecimalInput", 10)]
    [InlineData("NonDecimalData", 8)]
    public void ScalarAndArrayOwnersBindNumericReadingWithoutChangingOrigins(string readingMode, double expected)
    {
        var mode = Enum.Parse<AwkNumberParsingMode>(readingMode);
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, numberParsingMode: mode);
        variables.CaptureScalarMemoryBaseline();
        var name = AwkVariableName.FromText("saved");
        var array = AwkArrayName.FromText("a");
        var text = AwkText.FromSourceLiteralString("010");
        foreach (var sourceMode in Enum.GetValues<AwkNumberParsingMode>())
        {
            AwkValue[] inputs = [AwkValue.FromDeferredText(text, sourceMode), AwkValue.FromNumericString(text, sourceMode)];
            foreach (var input in inputs)
            {
                variables.SetScalarValue(name, input);
                variables.SetArrayValue(array, new("key"), input);
                AwkValue[] stored = [variables.GetOrCreateScalarValue(name), variables.GetArrayValue(array, new("key"))];
                foreach (var value in stored)
                {
                    Assert.Equal("010", value.AsString());
                    Assert.Equal(input.IsNumericStringValue, value.IsNumericStringValue);
                    Assert.Equal(expected, value.AsNumber());
                }
                Assert.Equal(sourceMode == AwkNumberParsingMode.NonDecimalData ? 8 : 10, input.AsNumber());
                Assert.Equal("010", input.AsString());
            }
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData("Decimal", false)]
    [InlineData("NonDecimalData", true)]
    public void OwnerWritesDeferConversionErrorsUntilNumericUse(string readingMode, bool fails)
    {
        var mode = Enum.Parse<AwkNumberParsingMode>(readingMode);
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, numberParsingMode: mode);
        variables.CaptureScalarMemoryBaseline();
        var text = AwkText.FromSourceLiteralString("0x1.2");
        AwkValue[] inputs = [AwkValue.FromDeferredText(text, AwkNumberParsingMode.NonDecimalData),
            AwkValue.FromNumericString(text, AwkNumberParsingMode.NonDecimalData)];
        foreach (var input in inputs)
        {
            var name = AwkVariableName.FromText("saved");
            var array = AwkArrayName.FromText("a");
            variables.SetScalarValue(name, input);
            variables.SetArrayValue(array, new("key"), input);
            AwkValue[] stored = [variables.GetOrCreateScalarValue(name), variables.GetArrayValue(array, new("key"))];
            foreach (var value in stored)
            {
                Assert.Equal("0x1.2", value.AsString());
                if (fails)
                {
                    var error = Assert.Throws<AwkRuntimeException>(() => value.AsNumber());
                    Assert.Equal("awk: hexadecimal floating point constants require an exponent\n", AwkUtf8Codec.Decode(error.MessageBytes));
                }
                else Assert.Equal(0, value.AsNumber());
            }
            Assert.Throws<AwkRuntimeException>(() => input.AsNumber());
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void StoredTextReadPreservesDeferredErrorsAndDoesNotPromoteUntypedOrigins()
    {
        var bytes = AwkText.FromSourceLiteralString("0x1.2");
        var slot = AwkValue.ForVariableStorage(AwkValue.FromDeferredText(bytes, AwkNumberParsingMode.NonDecimalData), AwkNumberParsingMode.NonDecimalData);
        Assert.True(slot.TryGetStoredScalarText(out var text));
        Assert.True(bytes.HasSameStorage(text));
        var value = slot.ReadVariableStorage(AwkNumberParsingMode.NonDecimalData, AwkRuntimeCounters.Disabled);
        Assert.Throws<AwkRuntimeException>(() => value.AsNumber());
        Assert.False(AwkValue.FromNumber(1.25).TryGetStoredScalarText(out _));
        var origin = new AwkUntypedScalar();
        var untyped = AwkValue.FromUntypedScalar(origin);
        Assert.False(untyped.TryGetStoredScalarText(out _));
        Assert.True(untyped.ByteText.AsSpan().IsEmpty);
        Assert.Equal(AwkRuntimeMemory.EstimateUntypedScalarBytes(), untyped.RetainedTextMemoryBytes);
        Assert.False(origin.IsScalar);
        Assert.True(AwkIdentifier.TryParse("a", out var identifier));
        var array = AwkValue.FromGlobalArrayReference(identifier);
        Assert.False(array.TryGetStoredScalarText(out _));
        Assert.Equal(0, array.RetainedTextMemoryBytes);
        Assert.Throws<AwkRuntimeException>(() => array.ByteText);
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
        var slot = AwkValue.ForVariableStorage(AwkValue.FromNumber(42), AwkNumberParsingMode.Decimal);
        var value = slot.ReadVariableStorage(AwkNumberParsingMode.Decimal, AwkRuntimeCounters.Disabled);

        Assert.Equal(0, slot.RetainedTextMemoryBytes);
        Assert.True(value.IsNumberValue);
        Assert.Equal(42, value.AsNumber());
        Assert.Equal("42", value.AsString(counters));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }
}
