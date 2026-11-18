using System;
using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkTextTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(500)]
    public void ScalarTraversalPreservesBoundariesForOwnedAndBorrowedText(int repetitions)
    {
        const string boundaries = "\u007f\u0080\u07ff\u0800\ud7ff\ue000\uffff\U00010000\U0010ffff";
        var input = string.Concat(Enumerable.Repeat(boundaries, repetitions));
        var scalars = new List<string>();
        foreach (var scalar in input.EnumerateRunes()) scalars.Add(scalar.ToString());
        var text = AwkText.FromString(input);
        var bytes = Encoding.UTF8.GetBytes(input);
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);

        Assert.Equal(scalars.Count, text.RuneLength);
        Assert.Equal(scalars.Count, AwkText.CountRunes(bytes, checkpoint: null));
        foreach (var (start, length) in new[]
        {
            (0, 0), (1, 1), (scalars.Count / 2, 3),
            (scalars.Count - 1, int.MaxValue), (scalars.Count + 1, 1)
        })
        {
            var expected = Encoding.UTF8.GetBytes(string.Concat(scalars.Skip(start).Take(length)));
            var owned = runtime.CopyRuneSlice(text, start, length);
            AwkText.ResolveRuneByteRange(bytes, start, length, checkpoint: null, out var first, out var last);

            Assert.Equal(expected, owned.AsSpan().ToArray());
            Assert.Equal(expected, bytes.AsSpan(first, last - first).ToArray());
            Assert.Equal(start < scalars.Count, AwkText.HasRuneAt(bytes, start, checkpoint: null));
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(32, true)]
    [InlineData(33, false)]
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
        using var decodeRuntime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var managed = decodeRuntime.DecodeText(cached);
        Assert.Same(managed, cached.PreparedString);
        Assert.Same(managed, decodeRuntime.DecodeText(cached));
        Assert.Equal(1, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
    }

    [Fact]
    public void CompactTextKeepsNumericStringSpellingWhileManagedPreparationKeepsItsOwnCache()
    {
        var text = AwkText.FromHostBytes("0007"u8.ToArray());
        Assert.IsType<byte[]>(text.Backing);
        var value = AwkValue.FromNumericString(text);
        var copied = AwkValue.ForVariableStorage(value, AwkNumberParsingMode.Decimal).ReadVariableStorage(AwkNumberParsingMode.Decimal);
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
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);
        var copy = runtime.CopyRuneSlice(text, 0, int.MaxValue);
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
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);
        var error = Assert.Throws<AwkRuntimeException>(() => runtime.CopyHostText([0xC3, 0x28]));

        Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
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
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(4095)]
    [InlineData(4096)]
    [InlineData(4097)]
    [InlineData(8193)]
    public void EqualityKeepsUtf8NulAndLongPrefixBoundariesAcrossOwnedAndConcatenatedText(int prefix)
    {
        var start = new string('a', prefix);
        const string suffix = "\0é😀z";
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var text = runtime.ConcatText([AwkText.FromString(start), AwkText.FromString(suffix)]);
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
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
    }

    [Fact]
    public void AwkText_SupportsByteAndRuneOperations()
    {
        var source = AwkText.FromString("Alpha caf\u00e9");
        var suffix = AwkText.FromString("!");
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);

        Assert.Equal(
            "Alpha caf\u00e9!",
            runtime.ConcatText([source, suffix]).AsString());
        Assert.Equal(
            "ha",
            runtime.CopyByteSlice(source, 3, 2).AsString());
        Assert.Equal(
            "caf\u00e9",
            runtime.CopyRuneSlice(source, 6, 4).AsString());
        Assert.Equal(6, source.IndexOf(AwkText.FromString("caf\u00e9")));
        Assert.True(source.StartsWith(AwkText.FromString("Alpha")));
        Assert.True(source.EndsWith(AwkText.FromString("caf\u00e9")));
        Assert.True(source.Contains(AwkText.FromString("ha c")));
        var error = Assert.Throws<AwkRuntimeException>(() => runtime.CopyByteSlice(source, 10, 1));
        Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    [Fact]
    public void AwkText_ConcatenationDefersAndCachesPayloadMaterialization()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var concatenated = runtime.ConcatText([AwkText.FromString("ab"), AwkText.FromString("cd")]);

        Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal("abcd", concatenated.AsString());
        Assert.Equal(4, counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal("abcd"u8.ToArray(), concatenated.AsSpan().ToArray());
        Assert.Equal(4, counters[AwkCounter.ConcatenationBytesCopied]);
    }

    [Fact]
    public void AwkText_ConcatenationRetainsOwnedIngressParts()
    {
        var buffer = "ab"u8.ToArray();
        var owned = AwkText.FromHostBytes(buffer);
        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);
        var concatenated = runtime.ConcatText([owned, AwkText.FromString("cd")]);

        buffer[0] = (byte)'x';

        Assert.Equal("abcd", concatenated.AsString());
    }

    [Fact]
    public void AwkText_GovernedAllocationsCoverConcatAndSlices()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters);
        memoryForGovernor.Budget.SetAvailableBytes(64);
        var source = memoryForGovernor.State.EncodeText("café");
        var suffix = memoryForGovernor.State.EncodeText("!");

        Assert.Equal("café!", memoryForGovernor.State.ConcatText([source, suffix]).AsString());
        Assert.Equal("fé", memoryForGovernor.State.CopyRuneSlice(source, 2, 2).AsString());
        Assert.Equal("ca", memoryForGovernor.State.CopyByteSlice(source, 0, 2).AsString());

    }

    [Fact]
    public void AwkText_GovernedCopyFailsBeforeCreatingText()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters);
        memoryForGovernor.Budget.SetAvailableBytes(3);
        var governor = memoryForGovernor.Accounting;
        var committed = governor.CurrentCommittedBytes;

        var exception = Assert.Throws<AwkRuntimeException>(() =>
            memoryForGovernor.State.CopyText("abcd"u8));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void AwkText_GovernedRuneIndexFailsBeforeAllocation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters);
        memoryForGovernor.Budget.SetAvailableBytes(4);
        var source = AwkText.FromString(new string('\u00e9', 128));

        var exception = Assert.Throws<AwkRuntimeException>(
            () => memoryForGovernor.State.CopyRuneSlice(source, 65, 1));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));

        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
    }

    [Fact]
    public void AwkText_AllocationCountersTrackSmallPayloadDensity()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();

        using var runtime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        _ = runtime.EncodeText("a");
        _ = runtime.EncodeText("small");
        _ = runtime.EncodeText(new string('x', 32));

        Assert.Equal(3, counters[AwkCounter.TextAllocations]);
        Assert.Equal(3, counters[AwkCounter.Utf8Encodes]);

        Assert.Equal(38, counters[AwkCounter.TextBytes]);
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
        using var decodeRuntime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var text = AwkText.FromHostBytes("caf\u00e9"u8.ToArray());

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

        Assert.Equal("caf\u00e9", decodeRuntime.DecodeText(text));
        Assert.Equal("caf\u00e9", decodeRuntime.DecodeText(text));

        Assert.Equal(1, counters[AwkCounter.StringDecodes]);
    }

    [Fact]
    public void AwkText_RetainedEstimateAllowsForLazyCaches()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var decodeRuntime = AwkTestHarness.CreateRuntimeStateWithCounters(counters);
        var text = AwkText.FromString(new string('\u00e9', 128));
        var retainedBytes = text.EstimatedRetainedMemoryBytes;

        _ = decodeRuntime.DecodeText(text);
        _ = decodeRuntime.CopyRuneSlice(text, 65, 1);

        Assert.True(retainedBytes > text.ByteLength);
        Assert.Equal(retainedBytes, text.EstimatedRetainedMemoryBytes);

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
