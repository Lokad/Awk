namespace Lokad.Awk.Tests;

public sealed class AwkIndexedFieldCacheTests
{
    [Theory]
    [InlineData("0007", false, false)]
    [InlineData("0007", true, false)]
    [InlineData("7tail", false, false)]
    [InlineData("7tail", true, false)]
    [InlineData("0007", false, true)]
    [InlineData("0007", true, true)]
    [InlineData("7tail", false, true)]
    [InlineData("7tail", true, true)]
    [InlineData("7e0", false, false)]
    [InlineData("7e0", true, false)]
    [InlineData("7e0", false, true)]
    [InlineData("7e0", true, true)]
    [InlineData("+007", false, false)]
    [InlineData("+007", true, false)]
    [InlineData("+007", false, true)]
    [InlineData("+007", true, true)]
    [InlineData("7\u00e9", false, false)]
    [InlineData("7\u00e9", true, false)]
    [InlineData("7\u00e9", false, true)]
    [InlineData("7\u00e9", true, true)]
    [InlineData(" 7 ", false, false)]
    [InlineData(" 7 ", true, false)]
    [InlineData(" 7 ", false, true)]
    [InlineData(" 7 ", true, true)]
    public void TextSnapshotsShareSpellingAndKeepEstablishedFactsThroughCompletion(
        string spelling, bool strict, bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);

            using var context = new AwkFieldTestContext(memory, separator: "|");
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(spelling + "|tail|old"));
            if (strict) _ = frame.GetFieldComparison(new(1), AwkNumberParsingMode.Decimal);
            else Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));

            Assert.True(frame.TryGetFieldTextSnapshot(new(1), out var first));
            Assert.True(frame.TryGetFieldTextSnapshot(new(1), out var repeated));
            Assert.True(first.HasSameStorage(repeated));
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));

            Assert.Equal(3, frame.GetFieldCount());
            var value = frame.GetFieldValue(new(1));

            Assert.True(first.HasSameStorage(value.ByteText));
            Assert.Equal(spelling, value.ByteText.AsString());
            Assert.Equal(spelling is not ("7tail" or "7\u00e9"), value.TryGetStoredComparisonNumber(out _, null));
            frame.SetRecordText(AwkText.FromSourceLiteralString("next"));
            Assert.Equal(spelling, first.AsString());
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(false, 8)]
    [InlineData(true, 8)]
    [InlineData(false, 16)]
    [InlineData(true, 16)]
    public void WarmRecordStorageIsReusedWithoutRetainingOldValues(bool governed, int fields)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(64L << 20, enabled: governed);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(options));
        memory.Initialize(state);
        var record = AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("001", fields)));
        state.SetCurrentRecord(record);
        var saved = state.RecordFields.GetFieldValue(new(1));
        ReadRecord();

        var committed = state.MemoryAccounting.LiveBytes;
        for (var row = 0; row < 100; row++) ReadRecord();

        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("next"));
        Assert.Equal("001", saved.ByteText.AsString());
        Assert.Equal("next", state.RecordFields.GetFieldValue(new(1)).ByteText.AsString());

        void ReadRecord()
        {
            state.SetCurrentRecord(record);
            for (var index = 1; index <= fields; index++)
                Assert.Equal(3, state.RecordFields.GetFieldValue(new(index)).ByteText.ByteLength);
        }
    }

    [Fact]
    public void DefaultScalarIsACachedValueRatherThanAnAbsentEntry()
    {

        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString("x"));
        frame.AssignFieldValue(new(1), default, AwkText.FromSourceLiteralString(" "));
        var first = frame.GetFieldValue(new(1));
        var second = frame.GetFieldValue(new(1));
        Assert.Equal(default(AwkValue), first);
        Assert.Equal(first, second);

        Assert.False(frame.TryGetUnmaterializedFieldBytes(new(1), out _));
    }

    [Fact]
    public void EarlyAndOutOfRangeReadsDoNotAllocateAFullWidthCacheAndRefusedGrowthIsAtomic()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8L << 20).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBaseline = memory.Accounting.LiveBytes;
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: "");
            memoryBaseline = context.BaselineBytes;
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var record = AwkText.FromSourceLiteralString(new string('x', 1024));
            frame.SetRecordText(record);
            Assert.Equal(1024, frame.GetFieldCount());
            var before = memory.Accounting.LiveBytes;
            Assert.Equal("x", frame.GetFieldValue(new(1)).AsString());
            for (var index = 2; index <= 4; index++)
                _ = frame.GetFieldValue(new(index));
            Assert.True(memory.Accounting.LiveBytes - before < 1024);
            var committed = memory.Accounting.LiveBytes;

            Assert.Equal(string.Empty, frame.GetFieldValue(new(int.MaxValue)).AsString());

            Assert.Equal(committed, memory.Accounting.LiveBytes);

            memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            var error = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldValue(new(1024)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));

            Assert.Equal(committed, memory.Accounting.LiveBytes);
            Assert.True(frame.TryGetUnmaterializedFieldBytes(new(1024), out _));
            Assert.Equal("x", frame.GetFieldValue(new(1)).AsString());
            frame.ResetRecord();
            Assert.Equal(memoryBaseline, memory.Accounting.LiveBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledCacheStagingPreservesExistingFieldsAndValues(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8L << 20, enabled: governed).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBaseline = memory.Accounting.LiveBytes;
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            var armed = false;
            memory.Budget.OnGrowth = _ => { if (armed) cancellation.Cancel(); };
            using var context = new AwkFieldTestContext(memory, separator: "|");
            memoryBaseline = context.BaselineBytes;
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var last = new string('y', 65_536);
            var record = AwkText.FromSourceLiteralString(string.Join('|', Enumerable.Repeat("x", 8191)) + "|" + last);
            frame.SetRecordText(record);
            Assert.Equal(8192, frame.GetFieldCount());
            var first = frame.GetFieldValue(new(1));
            for (var index = 2; index <= 4; index++)
                _ = frame.GetFieldValue(new(index));
            var committed = memory.Accounting.LiveBytes;
            armed = true;
            if (!governed) cancellation.Cancel();
            if (governed) memory.Budget.OnGrowth = _ => cancellation.Cancel();
            var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(8192)));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            armed = false;
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(8192, frame.GetFieldCount());
            Assert.Equal(first, frame.GetFieldValue(new(1)));
            Assert.True(frame.TryGetUnmaterializedFieldBytes(new(8192), out _));
            Assert.Equal(committed, memory.Accounting.LiveBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, separator: "|");
            frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(record);
            Assert.Equal(last, frame.GetFieldValue(new(8192)).AsString());
            frame.ResetRecord();
            Assert.Equal(memoryBaseline, memory.Accounting.LiveBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SparseLateReadsReuseOnlyCachedValues(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8L << 20, enabled: governed).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBaseline = memory.Accounting.LiveBytes;
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: "");
            memoryBaseline = context.BaselineBytes;
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var record = AwkText.FromSourceLiteralString(new string('x', 8192));
            for (var row = 0; row < 3; row++)
            {
                frame.SetRecordText(record);
                Assert.Equal(8192, frame.GetFieldCount());
                var before = memory.Accounting.LiveBytes;
                Assert.Equal("x", frame.GetFieldValue(new(8192)).AsString());
                Assert.InRange(memory.Accounting.LiveBytes - before, 0, 1024);
            }
            frame.ResetRecord();
            Assert.Equal(memoryBaseline, memory.Accounting.LiveBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompletingSplitPreservesCachedPrefixValues(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8L << 20, enabled: governed);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(options));
        memory.Initialize(state);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("007 " + new string('x', 512) + " 03"));
        var prefix = state.RecordFields.GetFieldValue(new(1));
        var allocations = counters[AwkCounter.TextAllocations];
        Assert.Equal(3, state.FieldCount);
        var again = state.RecordFields.GetFieldValue(new(1));
        Assert.True(prefix.ByteText.HasSameStorage(again.ByteText));
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal("007", again.ByteText.AsString());
        Assert.Equal(7, again.AsNumber());
    }

}
