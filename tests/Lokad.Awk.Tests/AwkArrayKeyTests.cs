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
        Assert.Equal("café", fromText.AsString(AwkRuntimeCounters.Disabled));
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
        Assert.Equal(text, fromOrdinal.AsString(AwkRuntimeCounters.Disabled));
        Assert.Equal(
            text,
            fromOrdinal.ToByteText(AwkRuntimeCounters.Disabled, AwkMemoryGovernor.Disabled).AsString());
        Assert.Equal(text.Length, fromOrdinal.ByteLength);
        Assert.Equal(0, fromOrdinal.RetainedTextMemoryBytes);
        Assert.Equal(
            AwkRuntimeMemory.EstimateTextBytes(fromText.ByteLength),
            fromText.RetainedTextMemoryBytes);
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
    public void AwkArrayKey_OrdinalMaterializationUsesRuntimeAccounting()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1024),
            counters);
        var baselineAllocations = counters.AwkTextAllocations;
        var baselineBytes = counters.AwkTextPayloadBytes;

        var text = AwkArrayKey.FromOrdinal(123).ToByteText(counters, governor);

        Assert.Equal("123", text.AsString());
        Assert.Equal(baselineAllocations + 1, counters.AwkTextAllocations);
        Assert.Equal(baselineBytes + 3, counters.AwkTextPayloadBytes);
        Assert.True(counters.MemoryPeakLiveBytes >= 3);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void AwkArrayKey_RuntimeConversionPreservesNumericOrdinalShape()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var ordinal = state.ToArrayKey(AwkValue.FromNumber(42));
        var numericString = state.ToArrayKey(AwkValue.FromNumericString("042"));
        var text = state.ToArrayKey(AwkValue.FromText("42"));
        var fractional = state.ToArrayKey(AwkValue.FromNumber(1.5));

        Assert.True(ordinal.IsOrdinal);
        Assert.Equal("42", ordinal.AsString(AwkRuntimeCounters.Disabled));
        Assert.False(numericString.IsOrdinal);
        Assert.Equal("042", numericString.AsString(AwkRuntimeCounters.Disabled));
        Assert.False(text.IsOrdinal);
        Assert.Equal("42", text.AsString(AwkRuntimeCounters.Disabled));
        Assert.False(fractional.IsOrdinal);
        Assert.Equal("1.5", fractional.AsString(AwkRuntimeCounters.Disabled));
    }

    [Fact]
    public async Task AwkArrayKey_UnsortedSnapshotsKeepOrdinalShapeUntilObservation()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var arrayName = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(arrayName, AwkArrayKey.FromOrdinal(7), AwkValue.FromNumber(1));

        using var keys = await state.SnapshotArrayKeyValuesAsync(arrayName, new AwkEvaluationContext(state));
        var key = Assert.Single(keys);
        Assert.True(key.IsOrdinal);
        Assert.Equal("7", state.CreateArrayKeyValue(key).AsString());
    }
}
