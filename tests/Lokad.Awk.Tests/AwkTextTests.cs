using System;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkTextTests
{
    [Theory]
    [InlineData(1, true)] [InlineData(32, true)] [InlineData(33, false)]
    public void OwnedAsciiByteViewsKeepIndependentBytesAndOptionalCacheOwnership(int length, bool compact)
    {
        var bytes = Enumerable.Repeat((byte)'a', length).ToArray();
        bytes[^1] = 0;
        var text = AwkText.FromHostBytes(bytes);
        Assert.Equal(compact, text.Backing is byte[]);
        Assert.Equal(length, text.RuneLength);
        Assert.Equal(length, text.ByteLength);
        Array.Fill(bytes, (byte)'x');
        Assert.Equal(0, text.AsSpan()[^1]);

        var cached = text.WithStringCache();
        Assert.True(text.AsMemory().Equals(cached.AsMemory()));
        Assert.Equal(text.EstimatedRetainedMemoryBytes, cached.EstimatedRetainedMemoryBytes);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var managed = cached.AsString(counters);
        Assert.Same(managed, cached.PreparedString);
        Assert.Same(managed, cached.AsString(counters));
        Assert.Equal(1, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Fact]
    public void CompactTextKeepsNumericStringSpellingWhileManagedPreparationKeepsItsOwnCache()
    {
        var text = AwkText.FromHostBytes("0007"u8.ToArray());
        Assert.IsType<byte[]>(text.Backing);
        var value = AwkValue.FromNumericString(text);
        var copied = AwkValue.ForVariableStorage(value).ReadVariableStorage(AwkNumberParsingMode.Decimal, AwkRuntimeCounters.Disabled);
        Assert.True(text.HasSameStorage(copied.ByteText));
        Assert.Equal(AwkRuntimeValueKind.NumericString, copied.RuntimeKind);
        Assert.Equal(7, copied.AsNumber());
        Assert.Equal("0007", copied.AsString());
        Assert.IsNotType<byte[]>(AwkText.FromSourceLiteralString("0007").Backing);
        Assert.IsNotType<byte[]>(AwkText.FromHostBytes("café"u8.ToArray()).Backing);
    }

    [Fact]
    public void AwkText_FullRuneSliceRetainsOwnedIngressBytes()
    {
        var bytes = "é😀q"u8.ToArray();
        var text = AwkText.FromHostBytes(bytes);
        var copy = text.CopyRuneSlice(0, int.MaxValue, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled);
        Array.Fill(bytes, (byte)'x');
        Assert.True(text.HasSameStorage(copy));
        Assert.Equal("é😀q", copy.AsString());
    }

    [Fact]
    public void AwkText_CountsBytesAndRunesForValidUtf8()
    {
        var text = AwkText.FromHostBytes("caf\u00e9"u8.ToArray());

        Assert.Equal(5, text.ByteLength);
        Assert.Equal(4, text.RuneLength);
        Assert.Equal("caf\u00e9", text.AsString());
    }

    [Theory]
    [InlineData("ascii", true)]
    [InlineData("aé", false)]
    public void AwkText_RuneLengthSharesTextFactsWithAsciiClassification(
        string value,
        bool expectedAscii)
    {
        var text = AwkText.FromString(value);

        _ = text.RuneLength;

        Assert.Equal(expectedAscii, text.IsAscii(out var inspectedBytes));
        Assert.Equal(0, inspectedBytes);
    }

    [Fact]
    public void AwkText_RejectsInvalidUtf8AtByteIngress()
    {
        var hostException = Assert.Throws<AwkRuntimeException>(
            () => AwkText.FromHostBytes(new byte[] { 0xC3, 0x28 }));
        var ownedException = Assert.Throws<AwkRuntimeException>(
            () => AwkText.FromOwnedBytes([0x61, 0x80], AwkRuntimeCounters.Disabled));

        Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(hostException.MessageBytes));
        Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(ownedException.MessageBytes));
    }

    [Fact]
    public void AwkText_UsesOrdinalByteEqualityAndHashing()
    {
        var first = AwkText.FromString("alpha");
        var second = AwkText.FromHostBytes("alpha"u8.ToArray());
        var later = AwkText.FromString("beta");

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.CompareBytes(later) < 0);
        Assert.False(first.Equals(later));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(7)] [InlineData(8)]
    [InlineData(4095)] [InlineData(4096)] [InlineData(4097)] [InlineData(8193)]
    public void EqualityKeepsUtf8NulAndLongPrefixBoundariesAcrossOwnedAndConcatenatedText(int prefix)
    {
        var start = new string('a', prefix);
        const string suffix = "\0é😀z";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromString(start).Concat(AwkText.FromString(suffix),
            counters, AwkMemoryAccounting.Disabled);
        var owned = AwkText.FromHostBytes(AwkUtf8Codec.Encode(start + suffix).ToArray());
        var different = AwkText.FromString(start + "\0é😀y");
        var longer = AwkText.FromString(start + suffix + "\0");
        foreach (var checkpoint in new Action?[] { null, static () => { } })
        {
            Assert.True(text.Equals(owned, checkpoint));
            Assert.True(owned.Equals(text, checkpoint));
            Assert.False(text.Equals(different, checkpoint));
            Assert.False(text.Equals(longer, checkpoint));
            Assert.False(text.Equals((object?)null));
        }
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Fact]
    public void AwkText_SupportsByteAndRuneOperations()
    {
        var source = AwkText.FromString("Alpha caf\u00e9");
        var suffix = AwkText.FromString("!");

        Assert.Equal(
            "Alpha caf\u00e9!",
            source.Concat(suffix, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled).AsString());
        Assert.Equal(
            "ha",
            source.CopyByteSlice(3, 2, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled).AsString());
        Assert.Equal(
            "caf\u00e9",
            source.CopyRuneSlice(
                6,
                4,
                AwkRuntimeCounters.Disabled,
                AwkMemoryAccounting.Disabled).AsString());
        Assert.Equal(6, source.IndexOf(AwkText.FromString("caf\u00e9")));
        Assert.True(source.StartsWith(AwkText.FromString("Alpha")));
        Assert.True(source.EndsWith(AwkText.FromString("caf\u00e9")));
        Assert.True(source.Contains(AwkText.FromString("ha c")));
        Assert.Throws<AwkRuntimeException>(() => source.CopyByteSlice(
            10,
            1,
            AwkRuntimeCounters.Disabled,
            AwkMemoryAccounting.Disabled));
    }

    [Fact]
    public void AwkText_ConcatenationDefersAndCachesPayloadMaterialization()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var concatenated = AwkText.FromString("ab", counters)
            .Concat(AwkText.FromString("cd", counters), counters, AwkMemoryAccounting.Disabled);

        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal("abcd", concatenated.AsString());
        Assert.Equal(4, counters.ConcatenationBytesCopied);
        Assert.Equal("abcd"u8.ToArray(), concatenated.AsSpan().ToArray());
        Assert.Equal(4, counters.ConcatenationBytesCopied);
    }

    [Fact]
    public void AwkText_ConcatenationRetainsOwnedIngressParts()
    {
        var buffer = "ab"u8.ToArray();
        var owned = AwkText.FromHostBytes(buffer);
        var concatenated = owned.Concat(
            AwkText.FromString("cd"),
            AwkRuntimeCounters.Disabled,
            AwkMemoryAccounting.Disabled);

        buffer[0] = (byte)'x';

        Assert.Equal("abcd", concatenated.AsString());
    }

    [Fact]
    public void AwkText_GovernedAllocationsCoverConcatAndSlices()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(64, counters);
        var governor = memoryForGovernor.Accounting;
        var source = AwkText.FromString("café", counters, governor);
        var suffix = AwkText.FromString("!", counters, governor);

        Assert.Equal("café!", source.Concat(suffix, counters, governor).AsString());
        Assert.Equal("fé", source.CopyRuneSlice(2, 2, counters, governor).AsString());
        Assert.Equal("ca", source.CopyByteSlice(0, 2, counters, governor).AsString());
        Assert.True(counters.MemoryPeakLiveBytes > 0);
    }

    [Fact]
    public void AwkText_GovernedCopyFailsBeforeCreatingText()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(3, counters);
        var governor = memoryForGovernor.Accounting;

        var exception = Assert.Throws<AwkRuntimeException>(() =>
            AwkText.CopyTrustedBytes("abcd"u8, counters, governor));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void AwkText_GovernedRuneIndexFailsBeforeAllocation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(4, counters);
        var governor = memoryForGovernor.Accounting;
        var source = AwkText.FromString(new string('\u00e9', 128));

        var exception = Assert.Throws<AwkRuntimeException>(
            () => source.CopyRuneSlice(65, 1, counters, governor));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(0, counters.RuneSliceIndexAllocations);
        Assert.Equal(0, counters.AwkTextAllocations);
    }

    [Fact]
    public void AwkText_AllocationCountersTrackSmallPayloadDensity()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();

        _ = AwkText.FromString("a", counters);
        _ = AwkText.FromString("small", counters);
        _ = AwkText.FromString(new string('x', 32), counters);

        Assert.Equal(3, counters.AwkTextAllocations);
        Assert.Equal(3, counters.Utf8Encodes);
        Assert.Equal(3, counters.ValuesCreated);
        Assert.Equal(2, counters.AwkTextSmallPayloadAllocations);
        Assert.Equal(6, counters.AwkTextSmallPayloadBytes);
        Assert.Equal(38, counters.AwkTextPayloadBytes);
    }

    [Fact]
    public void AwkText_ComparesAsciiTextWithoutDecoding()
    {
        Assert.True(AwkText.FromString("sku").EqualsAscii("sku"));
        Assert.False(AwkText.FromString("sku").EqualsAscii("sk\u00fa"));
    }

    [Fact]
    public void AwkText_AsStringIsCountedBoundaryDecode()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromHostBytes("caf\u00e9"u8.ToArray(), counters);

        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
        Assert.Equal("caf\u00e9", text.AsString(counters));
        Assert.Equal("caf\u00e9", text.AsString(counters));
        Assert.Equal(2, counters.AwkTextAsStringCalls);
        Assert.Equal(1, counters.AwkTextAsStringDecodeMisses);
        Assert.Equal(1, counters.StringDecodes);
    }

    [Fact]
    public void AwkText_RetainedEstimateAllowsForLazyCaches()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromString(new string('\u00e9', 128), counters);
        var retainedBytes = text.EstimatedRetainedMemoryBytes;

        _ = text.AsString(counters);
        _ = text.CopyRuneSlice(65, 1, counters, AwkMemoryAccounting.Disabled);

        Assert.True(retainedBytes > text.ByteLength);
        Assert.Equal(retainedBytes, text.EstimatedRetainedMemoryBytes);
        Assert.Equal(1, counters.AwkTextAsStringDecodeMisses);
        Assert.Equal(1, counters.RuneSliceIndexAllocations);
    }

    [Fact]
    public void AwkText_ParsesNumbersFromUtf8Bytes()
    {
        var decimalText = AwkText.FromSourceLiteralString("  -12.5e2 tail");
        var hexText = AwkText.FromSourceLiteralString(" -0x10 ");
        var octalText = AwkText.FromSourceLiteralString("010");

        Assert.True(decimalText.TryParseNumber(AwkNumberParsingMode.Decimal, AwkNumberTextExtent.Prefix, out var decimalNumber));
        Assert.Equal(-1250, decimalNumber);
        Assert.False(decimalText.TryParseNumber(AwkNumberParsingMode.Decimal, AwkNumberTextExtent.EntireText, out _));
        Assert.True(hexText.TryParseNumber(AwkNumberParsingMode.HexadecimalInput, AwkNumberTextExtent.EntireText, out var hexNumber));
        Assert.Equal(-16, hexNumber);
        Assert.True(octalText.TryParseNumber(AwkNumberParsingMode.NonDecimalData, AwkNumberTextExtent.EntireText, out var octalNumber));
        Assert.Equal(8, octalNumber);
    }

    [Fact]
    public void AwkText_ParsesAwkDecimalPrefixesFromUtf8Bytes()
    {
        var incompleteExponent = AwkText.FromSourceLiteralString("1e");
        var leadingDot = AwkText.FromSourceLiteralString(".5 ");
        var overflow = AwkText.FromSourceLiteralString("1e999");

        Assert.True(incompleteExponent.TryParseNumber(AwkNumberParsingMode.Decimal, AwkNumberTextExtent.Prefix, out var exponentNumber));
        Assert.Equal(1, exponentNumber);
        Assert.False(incompleteExponent.TryParseNumber(AwkNumberParsingMode.Decimal, AwkNumberTextExtent.EntireText, out _));
        Assert.True(leadingDot.TryParseNumber(AwkNumberParsingMode.Decimal, AwkNumberTextExtent.EntireText, out var leadingDotNumber));
        Assert.Equal(0.5, leadingDotNumber);
        Assert.True(overflow.TryParseNumber(AwkNumberParsingMode.Decimal, AwkNumberTextExtent.EntireText, out var overflowNumber));
        Assert.Equal(double.PositiveInfinity, overflowNumber);
    }
}
