using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkArrayKeyTests
{
    [Fact]
    public void AwkArrayKey_UsesAwkTextByteEquality()
    {
        var fromString = new AwkArrayKey("café");
        var fromText = new AwkArrayKey(AwkText.FromHostBytes("café"u8.ToArray()));
        var values = new Dictionary<AwkArrayKey, string>
        {
            [fromString] = "value"
        };

        Assert.Equal(fromString, fromText);
        Assert.Equal(fromString.GetHashCode(), fromText.GetHashCode());
        Assert.Equal("value", values[fromText]);
        Assert.Equal("café", fromText.StringValue);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(42, "42")]
    [InlineData(-7, "-7")]
    public void AwkArrayKey_OrdinalKeysMatchTextKeysWithoutRetainedText(int ordinal, string text)
    {
        var fromOrdinal = AwkArrayKey.FromOrdinal(ordinal);
        var fromText = new AwkArrayKey(text);
        var values = new Dictionary<AwkArrayKey, string>
        {
            [fromOrdinal] = "value"
        };

        Assert.True(fromOrdinal.IsOrdinal);
        Assert.False(fromText.IsOrdinal);
        Assert.Equal(fromText, fromOrdinal);
        Assert.Equal(fromText.GetHashCode(), fromOrdinal.GetHashCode());
        Assert.Equal("value", values[fromText]);
        Assert.Equal(text, fromOrdinal.StringValue);
        Assert.Equal(text, fromOrdinal.ByteText.AsString());
        Assert.Equal(text.Length, fromOrdinal.ByteLength);
        Assert.Equal(0, fromOrdinal.RetainedTextByteLength);
        Assert.Equal(fromText.ByteLength, fromText.RetainedTextByteLength);
    }

    [Fact]
    public void AwkArrayKey_OrdinalKeyAccountingAvoidsRetainedTextCharge()
    {
        var slot = AwkVariableSlot.FromText("v");
        var ordinalBytes = AwkRuntimeMemory.EstimateArrayElementBytes(AwkArrayKey.FromOrdinal(12), slot);
        var textBytes = AwkRuntimeMemory.EstimateArrayElementBytes(new AwkArrayKey("12"), slot);

        Assert.True(ordinalBytes < textBytes);
    }

    [Fact]
    public void AwkArrayKey_RuntimeConversionPreservesNumericOrdinalShape()
    {
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var ordinal = state.ToArrayKey(AwkValue.FromNumber(42));
        var numericString = state.ToArrayKey(AwkValue.FromNumericString("042"));
        var text = state.ToArrayKey(AwkValue.FromText("42"));
        var fractional = state.ToArrayKey(AwkValue.FromNumber(1.5));

        Assert.True(ordinal.IsOrdinal);
        Assert.Equal("42", ordinal.StringValue);
        Assert.False(numericString.IsOrdinal);
        Assert.Equal("042", numericString.StringValue);
        Assert.False(text.IsOrdinal);
        Assert.Equal("42", text.StringValue);
        Assert.False(fractional.IsOrdinal);
        Assert.Equal("1.5", fractional.StringValue);
    }

    [Fact]
    public async Task AwkArrayKey_UnsortedSnapshotsKeepOrdinalShapeUntilObservation()
    {
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var arrayName = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(arrayName, AwkArrayKey.FromOrdinal(7), AwkValue.FromNumber(1));

        using var keys = await state.SnapshotArrayKeyValuesAsync(arrayName, new AwkEvaluationContext(0, state));
        var key = Assert.Single(keys);
        Assert.True(key.IsOrdinal);
        Assert.Equal("7", state.CreateArrayKeyValue(key).AsString());
    }
}
