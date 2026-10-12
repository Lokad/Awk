using Xunit.Abstractions;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRecordFieldAllocationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false, 1_600_000)]
    [InlineData(true, 1_900_000)]
    public void RecordResetAndLazyFieldCountHaveBoundedAllocation(bool finiteMemory, long allocationLimit)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (finiteMemory)
            limits = limits.WithMaxExecutionMemoryBytes(4_096);
        var governor = AwkMemoryGovernor.Create(limits, counters);
        var frame = new AwkFieldFrame(counters, governor, 1_000, _ => { },
            value => value.ByteText, _ => { });
        var record = AwkText.FromString("alpha beta");
        List<AwkFieldText> fields =
        [AwkFieldText.FromRecordSlice(0, 5), AwkFieldText.FromRecordSlice(6, 4)];
        Func<AwkText, List<AwkFieldText>> splitFields = _ => fields;
        for (var index = 0; index < 100; index++)
        {
            frame.SetRecordText(record);
            _ = frame.GetFieldCount(splitFields);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = 0;
        for (var index = 0; index < 10_000; index++)
        {
            frame.SetRecordText(record);
            total += frame.GetFieldCount(splitFields);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine("10,000 record resets and lazy field counts: {0} bytes; finite memory: {1}.",
            allocatedBytes, finiteMemory);
        Assert.Equal(20_000, total);
        Assert.InRange(allocatedBytes, 0, allocationLimit);
        Assert.Equal(10_100, counters.FieldSplits);
        Assert.Equal(20_200, counters.FieldsMaterialized);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void CachedFieldsAndFieldCountDoNotAllocateOnRepeatedReads()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, options);
        state.SetCurrentRecord(AwkText.FromString("alpha beta"));
        var fieldCount = AwkSpecialVariables.VariableName(AwkSpecialVariableId.FieldCount);
        for (var index = 0; index < 100; index++)
        {
            _ = state.GetFieldValue(1);
            _ = state.FieldCount;
            _ = state.ResolveVariableValue(fieldCount);
        }
        var textAllocations = counters.AwkTextAllocations;
        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = 0;

        for (var index = 0; index < 10_000; index++)
        {
            total += state.GetFieldValue(1).ByteText.ByteLength;
            total += state.FieldCount;
            total += (int)state.ResolveVariableValue(fieldCount).AsNumber();
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(90_000, total);
        Assert.Equal(0, allocatedBytes);
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(2, counters.FieldsMaterialized);
        Assert.Equal(textAllocations, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }
}
