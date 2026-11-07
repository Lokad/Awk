using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkArrayKeyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(31)]
    [InlineData(4095)]
    [InlineData(4096)]
    [InlineData(4097)]
    [InlineData(8193)]
    public void BorrowedKeyEqualityKeepsExactUtf8AndNulBoundariesWithoutCopies(int prefix)
    {
        var payload = prefix == 0 ? "" : new string('a', prefix) + "\0é😀z";
        var bytes = AwkUtf8Codec.Encode(payload).ToArray();
        var padded = new byte[bytes.Length + 2];
        padded[0] = padded[^1] = 0xff;
        bytes.CopyTo(padded.AsSpan(1));
        var key = new AwkArrayKey(payload);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var comparer = AwkArrayKeyComparer.ForRuntime(static () => { }, counters, AwkMemoryAccounting.Disabled);
        var values = new Dictionary<AwkArrayKey, int>(comparer) { [key] = 7 };
        var lookup = values.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.True(lookup.TryGetValue(padded.AsSpan(1, bytes.Length), out var value));
        Assert.Equal(7, value);
        Assert.True(comparer.Equals(key, new AwkArrayKey(payload)));
        Assert.False(comparer.Equals(padded.AsSpan(1, bytes.Length), new AwkArrayKey(payload + "\0")));
        Assert.False(lookup.ContainsKey(AwkUtf8Codec.Encode(payload + "x").ToArray()));
        Assert.Single(values);
        Assert.Equal(0, counters.ArrayAlternateKeyCopies);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public void RuntimeComparerMeasuresOwnedAndBorrowedWorkInBytes()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var comparer = AwkArrayKeyComparer.ForRuntime(null, counters, AwkMemoryAccounting.Disabled);
        var key = new AwkArrayKey("café");

        Assert.Equal(comparer.GetHashCode(key), comparer.GetHashCode("café"u8));
        Assert.True(comparer.Equals(key, new AwkArrayKey("café")));
        Assert.True(comparer.Equals("café"u8, key));
        var copy = ((IAlternateEqualityComparer<ReadOnlySpan<byte>, AwkArrayKey>)comparer).Create("café"u8);

        Assert.Equal(key, copy);
        Assert.Equal(1, counters.ArrayOwnedHashCalls);
        Assert.Equal(1, counters.ArrayBorrowedHashCalls);
        Assert.Equal(10, counters.ArrayHashBytes);
        Assert.Equal(2, counters.ArrayEqualityCalls);
        Assert.Equal(1, counters.ArrayAlternateKeyCopies);
        Assert.Equal(5, counters.ArrayAlternateKeyBytesCopied);
    }

    [Fact]
    public void AwkArrayKey_UsesAwkTextByteEquality()
    {
        var fromString = new AwkArrayKey("café");
        var text = AwkText.FromHostBytes("café"u8.ToArray());
        var fromText = new AwkArrayKey(text);
        var values = new Dictionary<AwkArrayKey, string>
        {
            [fromString] = "value"
        };

        Assert.Equal(fromString, fromText);
        Assert.Equal(text.GetHashCode(), fromText.GetHashCode());
        Assert.Equal(fromString.GetHashCode(), fromText.GetHashCode());
        Assert.Equal("value", values[fromText]);
        Assert.Equal("café", fromText.AsString(AwkRuntimeCounters.Disabled));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(42, "42")]
    [InlineData(-7, "-7")]
    [InlineData(int.MinValue, "-2147483648")]
    [InlineData(int.MaxValue, "2147483647")]
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
            fromOrdinal.ToByteText(AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled).AsString());
        Assert.Equal(text.Length, fromOrdinal.ByteLength);
        Assert.Equal(0, fromOrdinal.RetainedTextMemoryBytes);
        Assert.Equal(
            AwkRuntimeMemory.EstimateTextBytes(fromText.ByteLength),
            fromText.RetainedTextMemoryBytes);

        var counters = AwkRuntimeCounters.CreateEnabled();
        var comparer = AwkArrayKeyComparer.ForRuntime(null, counters, AwkMemoryAccounting.Disabled);
        var borrowedValues = new Dictionary<AwkArrayKey, string>(comparer) { [fromOrdinal] = "value" };
        var lookup = borrowedValues.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.True(lookup.TryGetValue(System.Text.Encoding.UTF8.GetBytes(text), out var borrowedValue));
        Assert.Equal("value", borrowedValue);
        Assert.False(lookup.ContainsKey(System.Text.Encoding.UTF8.GetBytes("0" + text)));
        Assert.False(comparer.Equals(System.Text.Encoding.UTF8.GetBytes(text + "\0"), fromOrdinal));
        Assert.Single(borrowedValues);
        Assert.True(counters.ArrayBorrowedHashCalls > 0);
        Assert.Equal(0, counters.ArrayAlternateKeyCopies);
        Assert.Equal(0, counters.AwkTextAllocations);
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
    public void AwkArrayKey_DefaultKeyMatchesEmptyTextAndRemainsDistinctFromZero()
    {
        var empty = new AwkArrayKey(AwkText.Empty);
        var zero = AwkArrayKey.FromOrdinal(0);
        var key = default(AwkArrayKey);
        foreach (var comparer in new IEqualityComparer<AwkArrayKey>[] { EqualityComparer<AwkArrayKey>.Default, AwkArrayKeyComparer.Instance })
        {
            Assert.True(comparer.Equals(key, empty));
            Assert.Equal(comparer.GetHashCode(empty), comparer.GetHashCode(key));
            Assert.False(comparer.Equals(key, zero));
            var dictionary = new Dictionary<AwkArrayKey, int>(comparer) { [key] = 1, [zero] = 2 };
            Assert.Equal(1, dictionary[empty]);
            Assert.Equal(2, dictionary[new AwkArrayKey("0")]);
        }
    }

    [Fact]
    public void AwkArrayKey_OrdinalMaterializationUsesRuntimeAccounting()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1024, counters);
        var governor = memoryForGovernor.Accounting;
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
    public void AwkArrayKey_UnsortedSnapshotsKeepOrdinalShapeUntilObservation()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var arrayName = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(arrayName, AwkArrayKey.FromOrdinal(7), AwkValue.FromNumber(1));

        using var keys = AwkArrayStorageTestOperations.Snapshot(state,arrayName);
        var key = Assert.Single(keys);
        Assert.True(key.IsOrdinal);
        Assert.Equal("7", state.CreateArrayKeyValue(key).AsString());
    }
}
