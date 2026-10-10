using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkFieldFrameTests
{
    [Fact]
    public void AwkFieldFrame_AdoptsConcreteSplitterList()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var frame = CreateFrame(counters);
        frame.SetRecordText(AwkText.FromString("alpha beta", counters, CreateGovernor(counters)));
        List<AwkFieldText> fieldList =
        [
            AwkFieldText.FromRecordSlice(0, 5),
            AwkFieldText.FromRecordSlice(6, 4)
        ];

        var count = frame.GetFieldCount(_ => fieldList);

        Assert.Equal(2, count);
    }

    [Fact]
    public void AwkFieldFrame_CachesRecordSliceFieldValues()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = CreateGovernor(counters);
        var frame = CreateFrame(counters);
        frame.SetRecordText(AwkText.FromString("alpha beta", counters, governor));

        var first = frame.GetFieldValue(new AwkFieldIndex(1), SplitAlphaBeta, AwkValue.FromText);
        var allocationsAfterFirstRead = counters.AwkTextAllocations;
        var second = frame.GetFieldValue(new AwkFieldIndex(1), SplitAlphaBeta, AwkValue.FromText);

        Assert.Equal("alpha", first.AsString());
        Assert.Equal("alpha", second.AsString());
        Assert.Equal(allocationsAfterFirstRead, counters.AwkTextAllocations);

        static List<AwkFieldText> SplitAlphaBeta(AwkText _) =>
            [
                AwkFieldText.FromRecordSlice(0, 5),
            AwkFieldText.FromRecordSlice(6, 4)
            ];
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
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(500));
        var frame = new AwkFieldFrame(AwkRuntimeCounters.Disabled, governor, 1_000, _ => { });
        var priorPlan = CreatePlan(0, ",");
        frame.SetRecordText(AwkText.FromSourceLiteralString("keep"), priorPlan);
        var committedBefore = governor.CurrentCommittedBytes;
        var replacementPlan = CreatePlan(mode, new string('x', 200));

        var exception = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (assignValue)
                frame.SetRecordValue(
                    AwkValue.FromText("new"), value => AwkText.FromString(value.AsString()), replacementPlan);
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
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4_096));
        var frame = new AwkFieldFrame(AwkRuntimeCounters.Disabled, governor, 1_000, _ => { });
        var plan = CreatePlan(mode, new string('x', 200));
        frame.SetRecordText(AwkText.FromSourceLiteralString("keep"), plan);
        var committedBefore = governor.CurrentCommittedBytes;

        Assert.Equal(1, frame.GetFieldCount(_ => [AwkFieldText.FromRecordSlice(0, 4)]));

        Assert.Equal(default, frame.SplitPlan);
        Assert.True(governor.CurrentCommittedBytes < committedBefore);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal("keep", frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, frame.GetFieldCount(_ => throw new InvalidOperationException("Reset fields must be empty.")));
        Assert.Equal(string.Empty, frame.RecordAwkText.AsString());
    }

    [Fact]
    public void AwkFieldFrame_FieldLimitFailurePreservesLazyPlanForRetry()
    {
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4_096));
        var frame = new AwkFieldFrame(AwkRuntimeCounters.Disabled, governor, 1, _ => { });
        var plan = CreatePlan(0, ",");
        frame.SetRecordText(AwkText.FromSourceLiteralString("a,b"), plan);
        var committedBefore = governor.CurrentCommittedBytes;

        var exception = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldCount(_ =>
            [AwkFieldText.FromRecordSlice(0, 1), AwkFieldText.FromRecordSlice(2, 1)]));

        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(plan, frame.SplitPlan);
        Assert.Equal("a,b", frame.RecordAwkText.AsString());
        Assert.Equal(committedBefore, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(1, frame.GetFieldCount(_ => [AwkFieldText.FromRecordSlice(0, 3)]));
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

    private static AwkFieldFrame CreateFrame(AwkRuntimeCounters counters) =>
        new(
            counters,
            CreateGovernor(counters),
            maxFields: 1_000,
            _ => { });

    private static AwkMemoryGovernor CreateGovernor(AwkRuntimeCounters counters) =>
        AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters);

}
