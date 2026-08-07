using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkUtf8BuilderTests
{
    [Fact]
    public void AwkUtf8Builder_AppendsAndMaterializesExactAwkText()
    {
        using var builder = new AwkUtf8Builder(
            AwkRuntimeLimits.CreateDefault(),
            AwkMemoryGovernor.Disabled,
            capacity: 2);

        builder.Append("caf"u8.ToArray());
        builder.Append(AwkText.FromString("\u00e9"));
        var text = builder.CopyToAwkText();

        Assert.Equal(5, builder.Length);
        Assert.Equal(5, text.ByteLength);
        Assert.Equal("caf\u00e9", text.AsString());
        Assert.Equal("caf\u00e9"u8.ToArray(), builder.ToArray());
    }

    [Fact]
    public void AwkUtf8Builder_MaxLengthFailureDoesNotMutateContent()
    {
        using var builder = new AwkUtf8Builder(
            AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(4),
            AwkMemoryGovernor.Disabled,
            capacity: 4);

        builder.Append("abcd"u8.ToArray());
        var exception = Assert.Throws<AwkRuntimeException>(() => builder.Append((byte)'e'));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(4, builder.Length);
        Assert.Equal("abcd", builder.CopyToAwkText().AsString());
    }

    [Fact]
    public void AwkUtf8Builder_ReservedPrefixSupportsBorrowedRegexViews()
    {
        using var builder = AwkUtf8Builder.CreateWithReservedPrefix(
            AwkRuntimeLimits.CreateDefault(),
            AwkMemoryGovernor.Disabled,
            capacity: 2,
            prefix: 0x01);
        builder.Append("abc"u8);

        var inputStart = builder.BorrowAsAwkText(includeReservedPrefix: false);
        var continuedInput = builder.BorrowAsAwkText(includeReservedPrefix: true);
        var ownedCopy = continuedInput.CopyByteSlice(0, continuedInput.ByteLength);

        Assert.Equal("abc"u8.ToArray(), inputStart.AsSpan().ToArray());
        Assert.Equal([0x01, (byte)'a', (byte)'b', (byte)'c'], continuedInput.AsSpan().ToArray());
        builder.RemovePrefix(3);
        builder.Append("replacement"u8);
        Assert.Equal([0x01, (byte)'a', (byte)'b', (byte)'c'], ownedCopy.AsSpan().ToArray());
    }

    [Fact]
    public void AwkUtf8Builder_UsesGovernorReserveCommitRelease()
    {
        var limits = AwkRuntimeLimits.CreateDefault()
            .WithMaxBuilderBytes(64)
            .WithMaxExecutionMemoryBytes(64);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(limits, counters);

        using (var builder = new AwkUtf8Builder(limits, governor, capacity: 4))
        {
            Assert.Equal(4, governor.CurrentCommittedBytes);
            builder.Append("abcde"u8.ToArray());
            Assert.Equal(8, governor.CurrentCommittedBytes);
            Assert.Equal(12, governor.PeakLiveBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }

        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(2, counters.MemoryReserveCalls);
        Assert.Equal(2, counters.MemoryCommitCalls);
        Assert.Equal(0, counters.MemoryReleaseReservedCalls);
        Assert.Equal(2, counters.MemoryReleaseCommittedCalls);
        Assert.Equal(12, counters.MemoryReservedBytes);
        Assert.Equal(12, counters.MemoryCommittedBytes);
        Assert.Equal(0, counters.MemoryReleasedReservedBytes);
        Assert.Equal(12, counters.MemoryReleasedCommittedBytes);
        Assert.Equal(12, counters.MemoryPeakLiveBytes);
    }

    [Fact]
    public void AwkUtf8Builder_GovernedAllocationFailureDoesNotMutateContent()
    {
        var limits = AwkRuntimeLimits.CreateDefault()
            .WithMaxBuilderBytes(64)
            .WithMaxExecutionMemoryBytes(10);
        var governor = AwkMemoryGovernor.Create(limits);
        using var builder = new AwkUtf8Builder(limits, governor, capacity: 4);
        builder.Append("abcd"u8.ToArray());

        var exception = Assert.Throws<AwkRuntimeException>(() => builder.Append((byte)'e'));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(4, builder.Length);
        Assert.Equal("abcd", builder.CopyToAwkText().AsString());
        Assert.Equal(4, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void AwkUtf8Builder_MaterializationPreflightsBeforeCreatingOwnedText()
    {
        var limits = AwkRuntimeLimits.CreateDefault()
            .WithMaxBuilderBytes(64)
            .WithMaxExecutionMemoryBytes(7);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(limits, counters);
        using var builder = new AwkUtf8Builder(limits, governor, capacity: 4);
        builder.Append("abcd"u8);
        var baselineTextAllocations = counters.AwkTextAllocations;

        var exception = Assert.Throws<AwkRuntimeException>(() => builder.CopyToAwkText(counters));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(baselineTextAllocations, counters.AwkTextAllocations);
        Assert.Equal("abcd"u8.ToArray(), builder.AsSpan().ToArray());
        Assert.Equal(4, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }
}
