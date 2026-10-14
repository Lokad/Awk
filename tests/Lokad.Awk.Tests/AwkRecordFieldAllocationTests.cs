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
        var governor = AwkMemoryAccounting.Create(limits, counters);
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

    [Fact]
    public void GovernedSmallFieldCacheRemainsChargedAndAvoidsPerRecordDictionaryAllocation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4_096), counters);
        var frame = new AwkFieldFrame(counters, accounting, 1_000, _ => { }, value => value.ByteText, _ => { });
        var record = AwkText.FromSourceLiteralString("row");
        List<AwkFieldText> fields = [AwkFieldText.Empty];
        Func<AwkText,List<AwkFieldText>> split = _ => fields;
        for (var index = 0; index < 100; index++)
        {
            frame.SetRecordText(record);
            _ = frame.GetFieldValue(new AwkFieldIndex(1), split, AwkValue.FromText);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            frame.SetRecordText(record);
            _ = frame.GetFieldValue(new AwkFieldIndex(1), split, AwkValue.FromText);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine("10,000 governed empty-field cache reads: {0} allocated bytes.", allocated);
        Assert.InRange(allocated, 0, 800_000);
        Assert.Equal(10_100, counters.FieldSplits);
        Assert.Equal(10_100, counters.FieldsMaterialized);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        frame.SetRecordText(AwkText.Empty);
        Assert.True(accounting.CurrentCommittedBytes > 0);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
    }

    [Fact]
    public async Task SharedSmallCacheKeepsCapacityChargedAndClearsCachedValues()
    {
        var budget = new CooperativeTestBudget(4_096);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled, owner);
        var frame = new AwkFieldFrame(AwkRuntimeCounters.Disabled, accounting, 1_000, _ => { }, value => value.ByteText, _ => { });
        List<AwkFieldText> fields = [AwkFieldText.FromRecordSlice(0, 3)];
        Func<AwkText,List<AwkFieldText>> split = _ => fields;
        frame.SetRecordText(AwkText.FromSourceLiteralString("one"));
        Assert.Equal("one", frame.GetFieldValue(new AwkFieldIndex(1), split, AwkValue.FromText).AsString());
        frame.SetRecordText(AwkText.FromSourceLiteralString("two"));
        Assert.Equal("two", frame.GetFieldValue(new AwkFieldIndex(1), split, AwkValue.FromText).AsString());
        frame.SetRecordText(AwkText.Empty);
        var retained = budget.Reserved;
        Assert.True(retained > 0);
        Assert.False(budget.TryReserve(4_096, out _));
        Assert.True(budget.TryReserve(4_096 - retained, out var remainder));
        remainder.Dispose();
        frame.ResetRecord();
        Assert.Equal(0, budget.Reserved);
        Assert.True(budget.TryReserve(4_096, out var reusable));
        reusable.Dispose();
    }
}
