using Lokad.Awk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkArrayKeyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("0007")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    [InlineData("0000000007")]
    [InlineData("é😀")]
    [InlineData("\0")]
    [InlineData("a\0b")]
    [InlineData("2147483647")]
    public void InputOriginPreservesByteIdentityAndFirstInsertedKey(string spelling)
    {
        using var projectionRuntime = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var ordinary = new AwkArrayKey(spelling);
        var input = ordinary.WithInputOrigin(true);
        var restored = AwkArrayKey.FromStorage(input.StoredText, input.StoredOrdinal, input.StorageTag);
        var projected = projectionRuntime.ProjectArrayKeyText(restored);
        var bytes = AwkUtf8Codec.Encode(spelling).ToArray();
        var comparer = AwkArrayKeyComparer.Instance;
        Assert.True(restored.HasInputOrigin);
        Assert.True(projected.HasInputOrigin);
        Assert.Equal(ordinary, restored);
        Assert.Equal(ordinary.GetHashCode(), restored.GetHashCode());
        Assert.Equal(comparer.GetHashCode(bytes), comparer.GetHashCode(restored));
        Assert.Equal(spelling, projected.ToString());
        var entries = new Dictionary<AwkArrayKey, int>(comparer) { [restored] = 1 };
        entries[ordinary] = 2;
        Assert.True(Assert.Single(entries).Key.HasInputOrigin);
        Assert.Equal(2, entries.GetAlternateLookup<ReadOnlySpan<byte>>()[bytes]);
        Assert.Equal(2, entries[projected]);
        Assert.False(input.WithInputOrigin(false).HasInputOrigin);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9999999)]
    [InlineData(int.MaxValue)]
    public void InputOrdinalOriginSurvivesOwnedBytePreparation(int ordinal)
    {
        using var projectionRuntime = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var coordinate = AwkArrayKey.FromOrdinal(ordinal).WithInputOrigin(true);
        var prepared = coordinate.PrepareByteLookup();
        var restored = AwkArrayKey.FromStorage(prepared.StoredText, prepared.StoredOrdinal, prepared.StorageTag);
        var spelling = ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var bytes = AwkUtf8Codec.Encode(spelling).ToArray();
        Assert.True(restored.HasInputOrigin);
        Assert.Equal(coordinate, restored);
        Assert.Equal(AwkArrayKeyComparer.Instance.GetHashCode(bytes), AwkArrayKeyComparer.Instance.GetHashCode(restored));
        Assert.Equal(spelling, restored.ToString());
        Assert.True(projectionRuntime.ProjectArrayKeyText(restored).HasInputOrigin);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(4095)]
    [InlineData(4096)]
    [InlineData(4097)]
    [InlineData(8191)]
    [InlineData(8192)]
    [InlineData(8193)]
    [InlineData(16387)]
    public void LongRuntimeKeyHashesAgreeAcrossOwnedBorrowedAndCheckpointedViews(int byteLength)
    {
        var spelling = new string('x', byteLength - 6) + "é😀";
        var bytes = AwkUtf8Codec.Encode(spelling).ToArray();
        var key = new AwkArrayKey(spelling);
        var reference = new HashCode();
        reference.AddBytes(bytes);
        var expected = reference.ToHashCode();
        var checkpointed = AwkArrayKeyComparer.ForRuntime(static () => { });

        Assert.Equal(expected, AwkArrayKeyComparer.Instance.GetHashCode(key));
        Assert.Equal(expected, AwkArrayKeyComparer.Instance.GetHashCode(bytes));
        Assert.Equal(expected, checkpointed.GetHashCode(key));
        Assert.Equal(expected, checkpointed.GetHashCode(bytes));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongRuntimeKeyHashObservesCancellationDuringScan(bool borrowed)
    {
        var spelling = new string('x', 4 * AwkRuntimeLimits.CancellationCheckBytes) + "é😀";
        var bytes = AwkUtf8Codec.Encode(spelling).ToArray();
        var key = new AwkArrayKey(spelling);
        using var cancellation = new CancellationTokenSource();
        var precedingBoundaries = 2;
        void Checkpoint()
        {
            // Let entry and another boundary pass, then cancel during continued
            // work. Assert the stopped operation, not an exact total poll count.
            if (precedingBoundaries-- == 0) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var comparer = AwkArrayKeyComparer.ForRuntime(Checkpoint);
        var seed = new AwkArrayKey("seed");
        var entries = new Dictionary<AwkArrayKey, int>(comparer) { [seed] = 7 };

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            if (borrowed) _ = entries.GetAlternateLookup<ReadOnlySpan<byte>>().ContainsKey(bytes);
            else entries.Add(key, 99);
        });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Single(entries);
        Assert.Equal(7, entries[seed]);
    }

    [Fact]
    public void ShortRuntimeByteKeysKeepAlternateLookupAcrossLengthsAndZeroBytes()
    {
        var comparer = AwkArrayKeyComparer.Instance;
        var expected = new Dictionary<string, int>(StringComparer.Ordinal);
        var entries = new Dictionary<AwkArrayKey, int>(comparer);
        for (var value = 0; value < 128; value++)
        foreach (var spelling in new[] { "", ((char)value).ToString(), new string([(char)value, '\0']),
            new string(['\0', (char)value]), new string([(char)value, '\0', '\0']),
            new string(['\0', (char)value, '\0']), new string(['\0', '\0', (char)value]),
            new string([(char)value, '\0', '\0', '\0']) })
        {
            if (!expected.TryAdd(spelling, expected.Count)) continue;
            var bytes = AwkUtf8Codec.Encode(spelling).ToArray();
            var key = new AwkArrayKey(spelling);
            var restored = AwkArrayKey.FromStorage(key.StoredText, key.StoredOrdinal, key.StorageTag);
            Assert.Equal(comparer.GetHashCode(bytes), comparer.GetHashCode(restored));
            entries.Add(restored, expected[spelling]);
        }
        var alternate = entries.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.Equal(expected.Count, entries.Count);
        foreach (var (spelling, value) in expected)
        {
            Assert.Equal(value, alternate[AwkUtf8Codec.Encode(spelling).Span]);
            Assert.Equal(value, entries[new AwkArrayKey(spelling)]);
        }
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(1024)]
    [InlineData(9999999)]
    [InlineData(int.MaxValue)]
    public void OrdinalKeysRetainIdentityThroughStorageAndByteProjection(int ordinal)
    {
        using var projectionRuntime = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var coordinate = AwkArrayKey.FromOrdinal(ordinal);
        var spelling = ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var owned = new AwkArrayKey(spelling);
        var prepared = coordinate.PrepareByteLookup();
        Assert.True(prepared.IsOrdinal);
        Assert.Equal(coordinate, prepared);
        Assert.Equal(prepared, owned);
        Assert.Equal(prepared, prepared.PrepareByteLookup());
        var restored = AwkArrayKey.FromStorage(coordinate.StoredText, coordinate.StoredOrdinal, coordinate.StorageTag);
        Assert.True(restored.TryGetOrdinalValue(out var restoredOrdinal));
        Assert.Equal(ordinal, restoredOrdinal);
        Assert.True(restored.IsOrdinal);
        Assert.Equal(0, restored.RetainedTextMemoryBytes);
        Assert.Equal(coordinate, restored);
        Assert.Equal(restored, coordinate);
        Assert.Equal(owned, restored);
        Assert.Equal(restored.GetHashCode(), coordinate.GetHashCode());
        var comparer = AwkArrayKeyComparer.Instance;
        Assert.Equal(comparer.GetHashCode(owned), comparer.GetHashCode(restored));
        Assert.Equal(comparer.GetHashCode(prepared), comparer.GetHashCode(restored));
        var entries = new Dictionary<AwkArrayKey, int>(comparer) { [restored] = 7 };
        Assert.Equal(7, entries[coordinate]);
        Assert.Equal(7, entries[prepared]);
        Assert.Equal(7, entries.GetAlternateLookup<ReadOnlySpan<byte>>()[AwkUtf8Codec.Encode(spelling).Span]);
        Assert.Equal(spelling, restored.ToString());
        Assert.Equal(spelling, projectionRuntime.MaterializeArrayKeyText(restored, AwkMemoryAccounting.Disabled).AsString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("abcd")]
    [InlineData("é")]
    [InlineData("€")]
    [InlineData("é\0")]
    [InlineData("a\0b")]
    [InlineData("\0\0\0")]
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
        using var projectionRuntime = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var bytes = AwkUtf8Codec.Encode(text).ToArray();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var key = runtime.CopyArrayKey(bytes);
        Assert.Equal(bytes.Length > 7 ? 1 : 0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(16, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArrayKey>());
        Assert.Equal(40, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArrayUpdateTarget>());
        var owned = new AwkArrayKey(text);
        var scalar = new AwkArrayKey(AwkTestHarness.CopyHostText(bytes));
        var literal = new AwkRegionLiteral(AwkTestHarness.CopyHostText(bytes), null);
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkRegionLiteral>());
        Assert.Equal(bytes.Length is > 0 and <= 7, literal.Key.RequiresTextMaterialization);
        Assert.Equal(owned, literal.Key);
        Assert.Equal(scalar, literal.Key);
        Assert.Equal(text, literal.Text.AsString());
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
        var owner = new AwkArrayIdentity(1, comparer) { [key] = AwkValue.FromNumber(9) };
        var view = owner.ByteLookup;
        Assert.True(view.ContainsKey(bytes));
        Assert.True(view.TryGetValue(bytes, out var observed));
        Assert.Equal(9, observed.AsNumber());
        Assert.True(view.TryGetValue(bytes, out var retained, out observed));
        Assert.Equal(key, retained);
        Assert.False(retained.IsOrdinal);
        Assert.Equal(9, observed.AsNumber());
        Assert.False(view.ContainsKey(AwkUtf8Codec.Encode(text + "x").Span));
        Assert.Single(owner);
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
        Assert.Equal(text, key.ToString());
        Assert.Equal(text, projectionRuntime.MaterializeArrayKeyText(key, AwkMemoryAccounting.Disabled).AsString());
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled);
        key.AppendTo(builder);
        Assert.Equal(AwkUtf8Codec.Encode(text).ToArray(), builder.AsSpan().ToArray());
    }

    [Fact]
    public void DictionaryLookupCapturesKeyBeforeLongTextCallback()
    {
        var lookupKey = new AwkArrayKey(new string('a', 4097));
        var original = lookupKey;
        var replacement = new AwkArrayKey("changed");
        var replace = false;
        var comparer = AwkArrayKeyComparer.ForRuntime(() =>
        {
            if (!replace) return;
            replace = false;
            lookupKey = replacement;
        });
        var owner = new AwkArrayIdentity(3, comparer) { [original] = AwkValue.FromNumber(7) };
        replace = true;
        Assert.True(owner.TryGetDictionaryCell(lookupKey, out var cell));
        Assert.Equal(7, cell.Slot.AsNumber());
        Assert.Equal(replacement, lookupKey);
    }

    [Fact]
    public void KeyAndTargetBackingArraysFitTheirAdmission()
    {
        const int count = 257;
        _ = AllocatedArrayBytes<AwkArrayKey>(1);
        _ = AllocatedArrayBytes<AwkArrayUpdateTarget>(1);
        Assert.InRange(AllocatedArrayBytes<AwkArrayKey>(count), 0,
            AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count));
        Assert.InRange(AllocatedArrayBytes<AwkArrayUpdateTarget>(count), 0,
            AwkRuntimeMemory.EstimateArrayUpdateTargetArrayBytes(count) -
                (long)AwkRuntimeMemory.ArrayUpdateTargetHoldingBytes * count);

        static long AllocatedArrayBytes<T>(int count)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var values = new T[count];
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            GC.KeepAlive(values);
            return bytes;
        }
    }

    [Fact]
    public void DictionaryBackingFitsItsOwnerAdmission()
    {
        var comparer = AwkArrayKeyComparer.Instance;
        GC.KeepAlive(new AwkArrayIdentity(257, comparer));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var owner = new AwkArrayIdentity(257, comparer);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(bytes, 0, AwkRuntimeMemory.ArrayIdentityBytes + owner.BackingBytes);
        GC.KeepAlive(owner);
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
        var comparer = AwkArrayKeyComparer.ForRuntime(static () => { });
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
        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
    }

    [Fact]
    public void RuntimeComparerMeasuresOwnedAndBorrowedWorkInBytes()
    {

        var comparer = AwkArrayKeyComparer.ForRuntime(null);
        var key = new AwkArrayKey("café");

        Assert.Equal(comparer.GetHashCode(key), comparer.GetHashCode("café"u8));
        Assert.True(comparer.Equals(key, new AwkArrayKey("café")));
        Assert.True(comparer.Equals("café"u8, key));

    }

    [Fact]
    public void RuntimeOrdinalHashesAgreeAcrossCanonicalSpellingsAndSignedBoundaries()
    {
        using var projectionRuntime = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var random = new Random(178);
        int[] boundaries = [int.MinValue, int.MaxValue, -1, 0, 1, -10, 10,
            -999999, 999999, -1000000, 1000000, -9999999, 9999999, -10000000, 10000000];
        foreach (var ordinal in boundaries.Concat(Enumerable.Range(0, 512)
            .Select(_ => (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1))))
        {
            var text = ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var bytes = AwkUtf8Codec.Encode(text).Span;
            var counters = AwkRuntimeCounters.CreateEnabled();
            var comparer = AwkArrayKeyComparer.ForRuntime(null);
            var key = AwkArrayKey.FromOrdinal(ordinal);
            var compactOrText = new AwkArrayKey(text);
            var projected = projectionRuntime.ProjectArrayKeyText(compactOrText);
            var hash = comparer.GetHashCode(key);
            Assert.Equal(hash, comparer.GetHashCode(compactOrText));
            Assert.Equal(hash, comparer.GetHashCode(projected));
            Assert.Equal(hash, comparer.GetHashCode(bytes));

            Assert.Equal(0, counters[AwkCounter.TextAllocations]);
            var values = new Dictionary<AwkArrayKey, int>(comparer) { [projected] = 1 };
            values[key] = 2;
            values[compactOrText] = 3;
            Assert.Single(values);
            Assert.Equal(3, values.GetAlternateLookup<ReadOnlySpan<byte>>()[bytes]);
        }
    }

    [Theory]
    [InlineData("007")]
    [InlineData("+7")]
    [InlineData("-0")]
    [InlineData("-07")]
    [InlineData("7.0")]
    [InlineData("7e0")]
    [InlineData("7x")]
    [InlineData("7\0")]
    [InlineData(" 7")]
    [InlineData("7 ")]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    [InlineData("７")]
    public void RuntimeHashesPreserveNoncanonicalKeyIdentity(string text)
    {
        using var projectionRuntime = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var comparer = AwkArrayKeyComparer.Instance;
        var key = new AwkArrayKey(text);
        var bytes = AwkUtf8Codec.Encode(text).Span;
        var projected = projectionRuntime.ProjectArrayKeyText(key);
        Assert.Equal(comparer.GetHashCode(key), comparer.GetHashCode(projected));
        Assert.Equal(comparer.GetHashCode(key), comparer.GetHashCode(bytes));
        var values = new Dictionary<AwkArrayKey, int>(comparer)
        {
            [AwkArrayKey.FromOrdinal(0)] = 0,
            [AwkArrayKey.FromOrdinal(7)] = 7,
            [AwkArrayKey.FromOrdinal(int.MaxValue)] = 8,
            [AwkArrayKey.FromOrdinal(int.MinValue)] = 9,
            [projected] = 10
        };
        Assert.Equal(5, values.Count);
        Assert.Equal(10, values[key]);
        Assert.Equal(10, values.GetAlternateLookup<ReadOnlySpan<byte>>()[bytes]);
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
        var text = AwkTestHarness.CopyHostText("café"u8.ToArray());
        var fromText = new AwkArrayKey(text);
        var values = new Dictionary<AwkArrayKey, string>
        {
            [fromString] = "value"
        };

        Assert.Equal(fromString, fromText);
        Assert.Equal(text.GetHashCode(), fromText.GetHashCode());
        Assert.Equal(fromString.GetHashCode(), fromText.GetHashCode());
        Assert.Equal("value", values[fromText]);
        Assert.Equal("café", fromText.ToString());
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(42, "42")]
    [InlineData(-7, "-7")]
    [InlineData(int.MinValue, "-2147483648")]
    [InlineData(int.MaxValue, "2147483647")]
    public void AwkArrayKey_OrdinalKeysMatchTextKeysWithoutRetainedText(int ordinal, string text)
    {
        using var projectionRuntime = AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var fromOrdinal = AwkArrayKey.FromOrdinal(ordinal);
        var fromText = new AwkArrayKey(text);
        var fromBytes = new AwkArrayKey(AwkTestHarness.CopyHostText(AwkUtf8Codec.Encode(text)));
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
        Assert.Equal(text, fromOrdinal.ToString());
        Assert.Equal(
            text,
            projectionRuntime.MaterializeArrayKeyText(fromOrdinal, AwkMemoryAccounting.Disabled).AsString());
        Assert.Equal(text.Length, fromOrdinal.ByteLength);
        Assert.Equal(0, fromOrdinal.RetainedTextMemoryBytes);
        Assert.Equal(
            fromText.ByteLength <= 7 ? 0 : AwkRuntimeMemory.EstimateTextBytes(fromText.ByteLength),
            fromText.RetainedTextMemoryBytes);

        var counters = AwkRuntimeCounters.CreateEnabled();
        var comparer = AwkArrayKeyComparer.ForRuntime(null);
        Assert.Equal(comparer.GetHashCode(fromOrdinal), comparer.GetHashCode(fromBytes));
        var borrowedValues = new Dictionary<AwkArrayKey, string>(comparer) { [fromOrdinal] = "value" };
        var lookup = borrowedValues.GetAlternateLookup<ReadOnlySpan<byte>>();
        Assert.True(lookup.TryGetValue(System.Text.Encoding.UTF8.GetBytes(text), out var borrowedValue));
        Assert.Equal("value", borrowedValue);
        Assert.False(lookup.ContainsKey(System.Text.Encoding.UTF8.GetBytes("0" + text)));
        Assert.False(comparer.Equals(System.Text.Encoding.UTF8.GetBytes(text + "\0"), fromOrdinal));
        Assert.Single(borrowedValues);

        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        var owner = new AwkArrayIdentity(1, comparer) { [fromOrdinal] = AwkValue.FromNumber(7) };
        Assert.True(owner.ByteLookup.TryGetValue(AwkUtf8Codec.Encode(text).Span,
            out var retained, out var observed));
        Assert.True(retained.IsOrdinal);
        Assert.Equal(7, observed.AsNumber());
        Assert.Single(owner);
    }

    [Fact]
    public void ShortSpellingAndOrdinalKeysNeedNoRetainedTextCharge()
    {
        var slot = AwkValue.ForStoredText("v");
        var ordinalBytes = AwkRuntimeMemory.EstimateArrayElementBytes(AwkArrayKey.FromOrdinal(12), slot);
        var textBytes = AwkRuntimeMemory.EstimateArrayElementBytes(new AwkArrayKey("12"), slot);

        Assert.Equal(ordinalBytes, textBytes);
        Assert.True(textBytes < AwkRuntimeMemory.EstimateArrayElementBytes(new AwkArrayKey("long-key"), slot));
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
        using var memoryForGovernor = new CooperativeTestMemory(1024);
        memoryForGovernor.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None,
            memoryForGovernor.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                null, counters, AwkRuntimeLimits.CreateDefault()))));
        var governor = memoryForGovernor.Accounting;
        var baselineAllocations = counters[AwkCounter.TextAllocations];
        var baselineBytes = counters[AwkCounter.TextBytes];

        var text = memoryForGovernor.State.MaterializeArrayKeyText(AwkArrayKey.FromOrdinal(123), governor);

        Assert.Equal("123", text.AsString());
        Assert.Equal(baselineAllocations + 1, counters[AwkCounter.TextAllocations]);
        Assert.Equal(baselineBytes + 3, counters[AwkCounter.TextBytes]);

        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void AwkArrayKey_RuntimeConversionPreservesNumericOrdinalShape()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var ordinal = state.ToArrayKey(AwkValue.FromNumber(42));
        var numericString = state.ToArrayKey(AwkTestHarness.NumericInput("042"));
        var text = state.ToArrayKey(AwkValue.FromText("42"));
        var fractional = state.ToArrayKey(AwkValue.FromNumber(1.5));

        Assert.True(ordinal.IsOrdinal);
        Assert.Equal("42", ordinal.ToString());
        Assert.False(numericString.IsOrdinal);
        Assert.Equal("042", numericString.ToString());
        Assert.False(text.IsOrdinal);
        Assert.Equal("42", text.ToString());
        Assert.False(fractional.IsOrdinal);
        Assert.Equal("1.5", fractional.ToString());
    }

    [Fact]
    public void AwkArrayKey_UnsortedSnapshotsKeepOrdinalShapeUntilObservation()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var arrayName = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(arrayName, AwkArrayKey.FromOrdinal(7), AwkValue.FromNumber(1));

        using var keys = AwkArrayStorageTestOperations.Snapshot(state, arrayName);
        var key = Assert.Single(keys);
        Assert.True(key.IsOrdinal);
        Assert.Equal("7", state.CreateArrayKeyValue(key).AsString());
    }
}
