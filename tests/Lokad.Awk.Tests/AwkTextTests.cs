using System;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkTextTests
{
    [Fact]
    public void AwkText_CountsBytesAndRunesForValidUtf8()
    {
        var text = AwkText.FromHostBytes("caf\u00e9"u8.ToArray());

        Assert.Equal(5, text.ByteLength);
        Assert.Equal(4, text.RuneLength);
        Assert.Equal("caf\u00e9", text.AsString());
    }

    [Theory]
    [InlineData("ascii", false)]
    [InlineData("aé", true)]
    public void AwkText_RuneLengthSharesTextFactsWithAsciiClassification(
        string value,
        bool expectedNonAscii)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromString(value);

        _ = text.RuneLength;

        Assert.Equal(expectedNonAscii, text.ContainsNonAscii(counters));
        Assert.Equal(0, counters.RegexPolicyInputBytesInspected);
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

    [Fact]
    public void AwkText_SupportsByteAndRuneOperations()
    {
        var source = AwkText.FromString("Alpha caf\u00e9");
        var suffix = AwkText.FromString("!");

        Assert.Equal(
            "Alpha caf\u00e9!",
            source.Concat(suffix, AwkRuntimeCounters.Disabled, AwkMemoryGovernor.Disabled).AsString());
        Assert.Equal(
            "Alpha caf\u00e9Alpha caf\u00e9",
            source.Repeat(2, AwkRuntimeCounters.Disabled, AwkMemoryGovernor.Disabled).AsString());
        Assert.Equal(
            "ha",
            source.CopyByteSlice(3, 2, AwkRuntimeCounters.Disabled, AwkMemoryGovernor.Disabled).AsString());
        Assert.Equal(
            "caf\u00e9",
            source.CopyRuneSlice(
                6,
                4,
                AwkRuntimeCounters.Disabled,
                AwkMemoryGovernor.Disabled).AsString());
        Assert.Equal(6, source.IndexOf(AwkText.FromString("caf\u00e9")));
        Assert.True(source.StartsWith(AwkText.FromString("Alpha")));
        Assert.True(source.EndsWith(AwkText.FromString("caf\u00e9")));
        Assert.True(source.Contains(AwkText.FromString("ha c")));
        Assert.Throws<AwkRuntimeException>(() => source.CopyByteSlice(
            10,
            1,
            AwkRuntimeCounters.Disabled,
            AwkMemoryGovernor.Disabled));
    }

    [Fact]
    public void AwkText_GovernedAllocationsCoverConcatRepeatAndSlices()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(64),
            counters);
        var source = AwkText.FromString("café", counters, governor);
        var suffix = AwkText.FromString("!", counters, governor);

        Assert.Equal("café!", source.Concat(suffix, counters, governor).AsString());
        Assert.Equal("cafécafé", source.Repeat(2, counters, governor).AsString());
        Assert.Equal("fé", source.CopyRuneSlice(2, 2, counters, governor).AsString());
        Assert.Equal("ca", source.CopyByteSlice(0, 2, counters, governor).AsString());

        Assert.True(counters.MemoryReserveCalls >= 6);
        Assert.True(counters.MemoryCommitCalls >= 6);
        Assert.True(counters.MemoryReleaseCommittedCalls >= 6);
        Assert.True(counters.MemoryPeakLiveBytes > 0);
    }

    [Fact]
    public void AwkText_GovernedAllocationFailsBeforeOversizedConcat()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(6),
            counters);
        var left = AwkText.FromString("abc", counters, governor);
        var right = AwkText.FromString("defg", counters, governor);

        var exception = Assert.Throws<AwkRuntimeException>(() => left.Concat(right, counters, governor));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
    }

    [Fact]
    public void AwkText_GovernedCopyFailsBeforeCreatingText()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(3),
            counters);

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
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4),
            counters);
        var source = AwkText.FromString(new string('\u00e9', 128));

        var exception = Assert.Throws<AwkRuntimeException>(
            () => source.CopyRuneSlice(1, 1, counters, governor));

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
    public void AwkText_ParsesNumbersFromUtf8Bytes()
    {
        var decimalText = AwkText.FromSourceLiteralString("  -12.5e2 tail");
        var hexText = AwkText.FromSourceLiteralString(" -0x10 ");
        var octalText = AwkText.FromSourceLiteralString("010");

        Assert.True(decimalText.TryParseNumber(AwkNumberParsingMode.Decimal, requireFullText: false, out var decimalNumber));
        Assert.Equal(-1250, decimalNumber);
        Assert.False(decimalText.TryParseNumber(AwkNumberParsingMode.Decimal, requireFullText: true, out _));
        Assert.True(hexText.TryParseNumber(AwkNumberParsingMode.HexadecimalInput, requireFullText: true, out var hexNumber));
        Assert.Equal(-16, hexNumber);
        Assert.True(octalText.TryParseNumber(AwkNumberParsingMode.NonDecimalData, requireFullText: true, out var octalNumber));
        Assert.Equal(8, octalNumber);
    }

    [Fact]
    public void AwkText_ParsesAwkDecimalPrefixesFromUtf8Bytes()
    {
        var incompleteExponent = AwkText.FromSourceLiteralString("1e");
        var leadingDot = AwkText.FromSourceLiteralString(".5 ");
        var overflow = AwkText.FromSourceLiteralString("1e999");

        Assert.True(incompleteExponent.TryParseNumber(AwkNumberParsingMode.Decimal, requireFullText: false, out var exponentNumber));
        Assert.Equal(1, exponentNumber);
        Assert.False(incompleteExponent.TryParseNumber(AwkNumberParsingMode.Decimal, requireFullText: true, out _));
        Assert.True(leadingDot.TryParseNumber(AwkNumberParsingMode.Decimal, requireFullText: true, out var leadingDotNumber));
        Assert.Equal(0.5, leadingDotNumber);
        Assert.True(overflow.TryParseNumber(AwkNumberParsingMode.Decimal, requireFullText: true, out var overflowNumber));
        Assert.Equal(double.PositiveInfinity, overflowNumber);
    }
}
