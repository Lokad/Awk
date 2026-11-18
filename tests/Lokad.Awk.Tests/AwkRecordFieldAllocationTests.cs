using Xunit.Abstractions;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRecordFieldAllocationTests(ITestOutputHelper output)
{
    [Fact]
    public void OwnedRecordHandoffsRetainTextWithoutWrapperAllocations()
    {
        var text = AwkText.FromTrustedOwnedBytes("row 0007"u8.ToArray());
        var records = new AwkInputRecordResult[4096];
        for (var index = 0; index < records.Length; index++)
            records[index] = AwkInputRecordResult.Record(text, AwkRecordTerminator.NewLine,
                AwkRecordFieldSplittingMode.Ordinary);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < records.Length; index++)
            records[index] = AwkInputRecordResult.Record(text, AwkRecordTerminator.NewLine,
                AwkRecordFieldSplittingMode.Ordinary);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        foreach (var record in records)
        {
            Assert.True(text.HasSameStorage(record.RecordText));
            Assert.Equal("row 0007", record.RecordText.AsString());
        }
    }

    [Fact]
    public void OrdinarySmallRecordsReuseDescriptorStorageWithinBoundedAllocation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, options);
        var record = AwkText.FromSourceLiteralString("alpha beta");
        for (var index = 0; index < 100; index++)
        {
            state.SetCurrentRecord(record);
            _ = state.FieldCount;
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = 0;
        for (var index = 0; index < 1_000; index++)
        {
            state.SetCurrentRecord(record);
            total += state.FieldCount;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine("1,000 ordinary small-record field splits: {0} allocated bytes.", allocated);
        Assert.Equal(2_000, total);

        Assert.InRange(allocated, 0, 96_000);
        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
    }

    [Theory]
    [InlineData(false, 64_000)]
    [InlineData(true, 64_000)]
    public void RecordResetAndLazyFieldCountHaveBoundedAllocation(bool finiteMemory, long allocationLimit)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memory = new CooperativeTestMemory(1 << 20, enabled: finiteMemory);
        var governor = memory.Accounting;
        using var context = new AwkFieldTestContext(counters, governor, format: value => value.ByteText);
        var frame = context.Frame;
        var record = AwkText.FromString("alpha beta");
        for (var index = 0; index < 100; index++)
        {
            frame.SetRecordText(record);
            _ = frame.GetFieldCount();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = 0;
        for (var index = 0; index < 10_000; index++)
        {
            frame.SetRecordText(record);
            total += frame.GetFieldCount();
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine("10,000 record resets and lazy field counts: {0} bytes; finite memory: {1}.", allocatedBytes, finiteMemory);
        Assert.Equal(20_000, total);
        Assert.InRange(allocatedBytes, 0, allocationLimit);
        Assert.Equal(10_100, counters[AwkCounter.FieldSplits]);
        Assert.Equal(20_200, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
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
            counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
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
        var textAllocations = counters[AwkCounter.TextAllocations];
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
        Assert.Equal(1, counters[AwkCounter.FieldSplits]);
        Assert.Equal(2, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal(textAllocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void GovernedWholeRecordFieldCacheRemainsChargedAndReusesStorage()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForAccounting = new CooperativeTestMemory(4_096);
        var accounting = memoryForAccounting.Accounting;
        using var context = new AwkFieldTestContext(counters, accounting, format: value => value.ByteText);
        var frame = context.Frame;
        var record = AwkText.FromSourceLiteralString("row");
        for (var index = 0; index < 100; index++)
        {
            frame.SetRecordText(record);
            _ = frame.GetFieldValue(new AwkFieldIndex(1));
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            frame.SetRecordText(record);
            _ = frame.GetFieldValue(new AwkFieldIndex(1));
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine("10,000 governed whole-record field cache reads: {0} allocated bytes.", allocated);
        Assert.InRange(allocated, 0, 800_000);
        Assert.Equal(10_100, counters[AwkCounter.FieldSplits]);
        Assert.Equal(10_100, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal(0, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
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
        var accounting = AwkMemoryAccounting.Create(owner);
        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, accounting, format: value => value.ByteText);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString("one"));
        Assert.Equal("one", frame.GetFieldValue(new AwkFieldIndex(1)).AsString());
        frame.SetRecordText(AwkText.FromSourceLiteralString("two"));
        Assert.Equal("two", frame.GetFieldValue(new AwkFieldIndex(1)).AsString());
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedWideRecordsReuseFundedDescriptorsAndReleaseThemForNarrowInput(bool governed)
    {
        const int width = 512;
        using var memory = new CooperativeTestMemory(8 << 20, enabled: governed);
        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, memory.Accounting);
        var frame = context.Frame;
        var record = AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("1", width)));
        for (var row = 0; row < 100; row++) _ = ReadRecord();
        var retained = memory.Accounting.CurrentCommittedBytes;
        var total = 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var row = 0; row < 200; row++) total += ReadRecord();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        output.WriteLine("200 wide-record splits: {0} bytes; governed: {1}.", allocated, governed);
        Assert.Equal(width * 200, total);
        Assert.InRange(allocated, 0, 64_000);
        Assert.Equal(retained, memory.Accounting.CurrentCommittedBytes);
        if (governed)
        {
            Assert.True(retained >= AwkRuntimeMemory.EstimateRecordFieldListBytes(width));
            memory.Budget.SetAvailableBytes(0);
        }
        Assert.Equal(width, ReadRecord());
        frame.SetRecordText(AwkText.FromSourceLiteralString("7"));
        Assert.Equal(1, frame.GetFieldCount());
        if (governed) Assert.True(memory.Accounting.CurrentCommittedBytes < retained);
        frame.ResetRecord();
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);

        int ReadRecord()
        {
            frame.SetRecordText(record);
            return frame.GetFieldCount();
        }
    }

    [Fact]
    public void WideCacheRetainsOnlyItsFundedCapacityAcrossReplacement()
    {
        const int width = 256;
        using var memory = new CooperativeTestMemory(8 << 20);
        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, memory.Accounting);
        var frame = context.Frame;
        var record = AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("0007", width)));
        frame.SetRecordText(record);
        for (var field = 1; field <= width; field++) Assert.Equal("0007", frame.GetFieldValue(new(field)).AsString());
        var captured = frame.GetFieldValue(new(width));
        var full = memory.Accounting.CurrentCommittedBytes;

        frame.SetRecordText(record);
        var empty = memory.Accounting.CurrentCommittedBytes;
        Assert.InRange(empty, AwkRuntimeMemory.EstimateRecordFieldListBytes(width) +
            AwkRuntimeMemory.EstimateFieldValueListBytes(width), full);
        Assert.Equal("0007", captured.AsString());
        Assert.Equal("0007", frame.GetFieldValue(new(1)).AsString());
        frame.SetRecordText(AwkText.Empty);
        Assert.True(memory.Accounting.CurrentCommittedBytes < empty);
        Assert.Equal("0007", captured.AsString());
        frame.ResetRecord();
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
