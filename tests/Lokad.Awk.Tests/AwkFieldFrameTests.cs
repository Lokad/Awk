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

    private static AwkFieldFrame CreateFrame(AwkRuntimeCounters counters) =>
        new(
            counters,
            CreateGovernor(counters),
            maxFields: 1_000,
            _ => { });

    private static AwkMemoryGovernor CreateGovernor(AwkRuntimeCounters counters) =>
        AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters);

}
