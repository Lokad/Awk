using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkArrayKeyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("abcde")]
    [InlineData("abcdef")]
    [InlineData("\0")]
    [InlineData("a\0")]
    [InlineData("abc\0\0")]
    [InlineData("abcdef\0")]
    [InlineData("😀é")]
    [InlineData("0007")]
    [InlineData("+0")]
    [InlineData("-0")]
    [InlineData(" 7 ")]
    [InlineData("abcdefg")]
    [InlineData("abcdefgh")]
    [InlineData("a\0b\0c\0d")]
    [InlineData("abc😀")]
    [InlineData("abc😀x")]
    [InlineData("ééabc")]
    [InlineData("ééabcd")]
    public void RetainedByteKeysKeepSpellingHashAndOwnershipAcrossRepresentations(string text)
    {
        var bytes = AwkUtf8Codec.Encode(text).ToArray();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var key = AwkArrayKey.CopyTrustedBytes(bytes, counters, AwkMemoryAccounting.Disabled, null);
        Assert.Equal(bytes.Length > 7 ? 1 : 0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(16, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArrayKey>());
        Assert.Equal(40, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArrayUpdateTarget>());
        var owned = new AwkArrayKey(text);
        var scalar = new AwkArrayKey(AwkText.FromHostBytes(bytes)).CompactForCapture();
        var literal = new AwkRegionLiteral(AwkText.FromHostBytes(bytes), null);
        var capturedLiteral = literal.ForCapture();
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkRegionLiteral>());
        Assert.False(literal.Key.RequiresTextMaterialization);
        Assert.Equal(owned, literal.Key);
        Assert.Equal(scalar, capturedLiteral.Key);
        Assert.Equal(text, capturedLiteral.Text.AsString());
        var comparer = AwkArrayKeyComparer.Instance;
        var values = new Dictionary<AwkArrayKey, int>(comparer) { [key] = 9 };
        Assert.Equal(owned, key);
        Assert.Equal(key, owned);
        Assert.Equal(key, scalar);
        Assert.Equal(scalar, key);
        Assert.Equal(key.RetainedTextMemoryBytes, scalar.RetainedTextMemoryBytes);
        Assert.Equal(key.GetHashCode(), scalar.GetHashCode());
        Assert.Equal(comparer.GetHashCode(key), comparer.GetHashCode(scalar));
        Assert.Equal(9, values[scalar]);
        Assert.Equal(owned.GetHashCode(), key.GetHashCode());
        Assert.Equal(comparer.GetHashCode(bytes), comparer.GetHashCode(key));
        Assert.Equal(9, values[owned]);
        var borrowed = values.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.True(borrowed.TryGetValue(bytes, out var found));
        Assert.Equal(9, found);
        for (var i = 0; i < bytes.Length; i++)
        {
            // Exact byte identity includes every compact word and trailing NULs.
            var different = bytes.ToArray();
            different[i] ^= 1;
            Assert.False(comparer.Equals(different, key));
            Assert.False(borrowed.ContainsKey(different));
        }
        var extended = new byte[bytes.Length + 1];
        bytes.CopyTo(extended, 0);
        Assert.False(comparer.Equals(extended, key));
        Assert.False(borrowed.ContainsKey(extended));
        Assert.Equal(bytes.Length, key.ByteLength);
        Assert.Equal(bytes.Length > 7 ? AwkRuntimeMemory.EstimateTextBytes(bytes.Length) : 0,
            key.RetainedTextMemoryBytes);
        Array.Fill(bytes, (byte)'x');
        Assert.Equal(text, key.AsString(AwkRuntimeCounters.Disabled));
        Assert.Equal(text, key.ToByteText(AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled).AsString());
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled);
        key.AppendTo(builder);
        Assert.Equal(AwkUtf8Codec.Encode(text).ToArray(), builder.AsSpan().ToArray());
    }

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
        var comparer = AwkArrayKeyComparer.ForRuntime(static () => { }, counters);
        var values = new Dictionary<AwkArrayKey, int>(comparer) { [key] = 7 };
        var lookup = values.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.True(lookup.TryGetValue(padded.AsSpan(1, bytes.Length), out var value));
        Assert.Equal(7, value);
        Assert.True(comparer.Equals(key, new AwkArrayKey(payload)));
        Assert.False(comparer.Equals(padded.AsSpan(1, bytes.Length), new AwkArrayKey(payload + "\0")));
        Assert.False(lookup.ContainsKey(AwkUtf8Codec.Encode(payload + "x").ToArray()));
        if (payload.Length > 0)
        {
            var different = payload[..^1] + "y";
            Assert.False(comparer.Equals(AwkUtf8Codec.Encode(different).Span, key));
            Assert.False(comparer.Equals(key, new AwkArrayKey(different)));
            Assert.False(lookup.ContainsKey(AwkUtf8Codec.Encode(different).Span));
        }
        Assert.Single(values);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public void RuntimeComparerMeasuresOwnedAndBorrowedWorkInBytes()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var comparer = AwkArrayKeyComparer.ForRuntime(null, counters);
        var key = new AwkArrayKey("café");

        Assert.Equal(comparer.GetHashCode(key), comparer.GetHashCode("café"u8));
        Assert.True(comparer.Equals(key, new AwkArrayKey("café")));
        Assert.True(comparer.Equals("café"u8, key));

        Assert.Equal(1, counters.ArrayOwnedHashCalls);
        Assert.Equal(1, counters.ArrayBorrowedHashCalls);
        Assert.Equal(10, counters.ArrayHashBytes);
        Assert.Equal(2, counters.ArrayEqualityCalls);
    }

    [Theory]
    [InlineData(4097)]
    [InlineData(16384)]
    public void LongByteEqualityObservesCancellationDuringComparison(int prefix)
    {
        var text = new string('a', prefix) + "é😀";
        var bytes = AwkUtf8Codec.Encode(text).ToArray();
        var other = bytes.ToArray();
        using var cancellation = new CancellationTokenSource();
        var entered = false;
        void Checkpoint()
        {
            // Inject cancellation after entry, without requiring an exact poll count.
            if (entered) cancellation.Cancel();
            entered = true;
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var error = Assert.Throws<OperationCanceledException>(() => AwkText.EqualBytes(bytes, other, Checkpoint));
        Assert.Equal(cancellation.Token, error.CancellationToken);
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
        var fromBytes = AwkArrayKey.CopyTrustedBytes(AwkUtf8Codec.Encode(text).Span,
            AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled, null);
        var values = new Dictionary<AwkArrayKey, string>
        {
            [fromOrdinal] = "value"
        };

        Assert.True(fromOrdinal.IsOrdinal);
        Assert.False(fromText.IsOrdinal);
        Assert.Equal(fromText, fromOrdinal);
        Assert.Equal(fromOrdinal, fromBytes);
        Assert.Equal(fromBytes, fromOrdinal);
        Assert.Equal(fromOrdinal.GetHashCode(), fromBytes.GetHashCode());
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
        var comparer = AwkArrayKeyComparer.ForRuntime(null, counters);
        Assert.Equal(comparer.GetHashCode(fromOrdinal), comparer.GetHashCode(fromBytes));
        var borrowedValues = new Dictionary<AwkArrayKey, string>(comparer) { [fromOrdinal] = "value" };
        var lookup = borrowedValues.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.True(lookup.TryGetValue(System.Text.Encoding.UTF8.GetBytes(text), out var borrowedValue));
        Assert.Equal("value", borrowedValue);
        Assert.False(lookup.ContainsKey(System.Text.Encoding.UTF8.GetBytes("0" + text)));
        Assert.False(comparer.Equals(System.Text.Encoding.UTF8.GetBytes(text + "\0"), fromOrdinal));
        Assert.Single(borrowedValues);
        Assert.True(counters.ArrayBorrowedHashCalls > 0);
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
