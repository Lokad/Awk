using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkFieldFrameTests
{
    [Fact]
    public void AwkFieldFrame_UsesItsBoundSplitter()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var context = CreateFrame(counters);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromString("alpha beta", counters, AwkMemoryAccounting.Disabled));
        var count = frame.GetFieldCount();

        Assert.Equal(2, count);
    }

    [Fact]
    public void AwkFieldFrame_CachesRecordSliceFieldValues()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Disabled;
        using var context = CreateFrame(counters);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromString("alpha beta", counters, governor));

        var first = frame.GetFieldValue(new AwkFieldIndex(1));
        var allocationsAfterFirstRead = counters.AwkTextAllocations;
        var second = frame.GetFieldValue(new AwkFieldIndex(1));

        Assert.Equal("alpha", first.AsString());
        Assert.Equal("alpha", second.AsString());
        Assert.Equal(allocationsAfterFirstRead, counters.AwkTextAllocations);

    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void AwkFieldFrame_RecordPlanMemoryFailurePreservesRecordAndPriorPlan(int mode, bool assignValue)
    {
        using var memoryForGovernor = new CooperativeTestMemory(500, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, governor, format: value => value.ByteText);
        var frame = context.Frame;
        var priorPlan = CreatePlan(0, ",");
        frame.SetRecordText(AwkText.FromSourceLiteralString("keep"), priorPlan);
        var committedBefore = governor.CurrentCommittedBytes;
        var replacementPlan = CreatePlan(mode, new string('x', 200));

        var exception = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (assignValue)
                frame.SetRecordValue(AwkValue.FromText("new"), replacementPlan);
            else
                frame.SetRecordText(AwkText.FromSourceLiteralString("new"), replacementPlan);
        });

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal("keep", frame.RecordAwkText.AsString());
        Assert.Equal(priorPlan, frame.SplitPlan);
        Assert.Equal(committedBefore, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AwkFieldFrame_MaterializationReleasesRetainedPlan(int mode)
    {
        using var memoryForGovernor = new CooperativeTestMemory(8 << 20, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using (var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, governor))
        {
            var frame = context.Frame;
            var plan = CreatePlan(mode, new string('x', 200));
            var record = AwkText.FromSourceLiteralString("keep");
            var expected = mode switch { 1 => 0, 2 => 4, _ => 1 };
            // Cache owners remain alive after the record's captured plan retires.
            var split = context.Splitter.SplitRecordFields(record, plan);
            Assert.Equal(expected, split.Count);
            var fieldBytes = AwkRuntimeMemory.EstimateFieldTextListBytes(split.Capacity);
            var serviceBytes = governor.CurrentCommittedBytes;
            frame.SetRecordText(record, plan);
            var committedBefore = governor.CurrentCommittedBytes;
            Assert.Equal(expected, frame.GetFieldCount());
            Assert.Equal(default, frame.SplitPlan);
            Assert.Equal(plan.RetainedMemoryBytes,
                committedBefore + fieldBytes - governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal("keep", frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(serviceBytes, governor.CurrentCommittedBytes);
            Assert.Equal(0, frame.GetFieldCount());
            Assert.Equal(string.Empty, frame.RecordAwkText.AsString());
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    private static AwkRecordSplitPlan CreatePlan(int mode, string pattern)
    {
        var text = AwkText.FromSourceLiteralString(pattern);
        return new AwkRecordSplitPlan(
            (AwkFieldSplittingMode)mode,
            mode == 0 ? AwkFieldSeparatorSpec.FromByteText(text) : default,
            mode == 1 ? text : AwkText.Empty,
            mode == 2 ? Enumerable.Repeat(new AwkFieldWidth(new(0), new(1)), 64).ToList() : [],
            AwkRegexCaseSensitivity.CaseSensitive,
            AwkRecordFieldSplittingMode.Ordinary);
    }

    private static AwkFieldTestContext CreateFrame(AwkRuntimeCounters counters) =>
        new(
            counters, AwkMemoryAccounting.Disabled, value => value.ByteText);

}
