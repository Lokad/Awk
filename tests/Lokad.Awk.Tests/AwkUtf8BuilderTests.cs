using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkUtf8BuilderTests
{
    [Fact]
    public void AwkUtf8Builder_AppendsAndMaterializesExactAwkText()
    {
        using var builder = new AwkUtf8Builder(
            AwkMemoryAccounting.Disabled, capacity: 2);

        builder.Append("caf"u8);
        builder.Append(AwkText.FromString("\u00e9"));
        var text = builder.CopyToAwkText(AwkRuntimeCounters.Disabled);

        Assert.Equal(5, builder.Length);
        Assert.Equal(5, text.ByteLength);
        Assert.Equal("caf\u00e9", text.AsString());
        Assert.Equal("caf\u00e9"u8.ToArray(), builder.AsSpan().ToArray());
    }

    [Fact]
    public void AwkUtf8Builder_RefusedGrowthDoesNotMutateContent()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var builder = new AwkUtf8Builder(
            memory.Accounting, capacity: 4);

        builder.Append("abcd"u8);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(() => builder.Append((byte)'e'));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(4, builder.Length);
        Assert.Equal("abcd", builder.CopyToAwkText(AwkRuntimeCounters.Disabled).AsString());
    }

    [Fact]
    public void AwkUtf8Builder_TextSequencePreflightsBeforeMutatingContent()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var builder = new AwkUtf8Builder(
            memory.Accounting, capacity: 4);
        builder.Append("a"u8);
        AwkText[] values = [AwkText.FromString("bc"), AwkText.FromString("de")];

        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var exception = Assert.Throws<AwkRuntimeException>(() => builder.Append(values));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(1, builder.Length);
        Assert.Equal("a", builder.CopyToAwkText(AwkRuntimeCounters.Disabled).AsString());
    }

    [Fact]
    public void AwkUtf8Builder_OwnedCopySurvivesBorrowedBufferReuse()
    {
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled, capacity: 2);
        builder.Append("abc"u8);

        var input = builder.BorrowAsAwkText();
        var ownedCopy = input.CopyByteSlice(
            0,
            input.ByteLength,
            AwkRuntimeCounters.Disabled,
            AwkMemoryAccounting.Disabled);

        Assert.Equal("abc"u8.ToArray(), input.AsSpan().ToArray());
        builder.RemovePrefix(3);
        builder.Append("replacement"u8);
        Assert.Equal("abc"u8.ToArray(), ownedCopy.AsSpan().ToArray());
    }

    [Fact]
    public void AwkUtf8Builder_UsesGovernorReserveCommitRelease()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(64, counters);
        var governor = memoryForGovernor.Accounting;

        using (var builder = new AwkUtf8Builder(governor, capacity: 4))
        {
            Assert.Equal(4, governor.CurrentCommittedBytes);
            builder.Append("abcde"u8);
            Assert.Equal(8, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }

        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(12, counters.MemoryPeakLiveBytes);
    }

    [Fact]
    public void AwkUtf8Builder_GovernedAllocationFailureDoesNotMutateContent()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(10, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using var builder = new AwkUtf8Builder(governor, capacity: 4);
        builder.Append("abcd"u8);

        var exception = Assert.Throws<AwkRuntimeException>(() => builder.Append((byte)'e'));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(4, builder.Length);
        Assert.Equal("abcd", builder.CopyToAwkText(AwkRuntimeCounters.Disabled).AsString());
        Assert.Equal(4, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void AwkUtf8Builder_MaterializationPreflightsBeforeCreatingOwnedText()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(7, counters);
        var governor = memoryForGovernor.Accounting;
        using var builder = new AwkUtf8Builder(governor, capacity: 4);
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
