namespace Lokad.Awk.Tests;

public sealed class AwkIndexedFieldCacheTests
{
    [Theory]
    [InlineData(false, 3)] [InlineData(true, 3)]
    [InlineData(false, 8)] [InlineData(true, 8)]
    [InlineData(false, 16)] [InlineData(true, 16)]
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
        var saved = state.GetFieldValue(1);
        ReadRecord();
        var allocations = counters.FieldValueCacheBufferAllocations;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        for (var row = 0; row < 100; row++) ReadRecord();

        Assert.Equal(allocations, counters.FieldValueCacheBufferAllocations);
        Assert.True(counters.FieldListBufferReuses >= 100);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("next"));
        Assert.Equal("001", saved.ByteText.AsString());
        Assert.Equal("next", state.GetFieldValue(1).ByteText.AsString());

        void ReadRecord()
        {
            state.SetCurrentRecord(record);
            for (var index = 1; index <= fields; index++)
                Assert.Equal(3, state.GetFieldValue(index).ByteText.ByteLength);
        }
    }

    [Fact]
    public void DefaultScalarIsACachedValueRatherThanAnAbsentEntry()
    {
        var frame = new AwkFieldFrame(AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled,
            value => value.ByteText);
        frame.SetRecordText(AwkText.FromSourceLiteralString("x"));
        var calls = 0;
        var first = frame.GetFieldValue(new(1), _ => [AwkFieldText.FromRecordSlice(0, 1)], _ =>
        {
            calls++;
            return default;
        });
        var second = frame.GetFieldValue(new(1), NoSplit, _ => throw new InvalidOperationException("Cached."));
        Assert.Equal(default(AwkValue), first);
        Assert.Equal(first, second);
        Assert.Equal(1, calls);
        Assert.False(frame.TryGetUnmaterializedFieldBytes(new(1), NoSplit, out _));
    }

    [Fact]
    public void EarlyAndOutOfRangeReadsDoNotAllocateAFullWidthCacheAndRefusedGrowthIsAtomic()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8L << 20, counters);
        var frame = new AwkFieldFrame(counters, memory.Accounting, value => value.ByteText);
        var record = AwkText.FromSourceLiteralString(new string('x', 1024));
        frame.SetRecordText(record);
        Assert.Equal(1024, frame.GetFieldCount(_ => Enumerable.Range(0, 1024)
            .Select(index => AwkFieldText.FromRecordSlice(index, 1)).ToList()));
        var before = memory.Accounting.CurrentCommittedBytes;
        Assert.Equal("x", frame.GetFieldValue(new(1), NoSplit, AwkValue.FromText).AsString());
        for (var index = 2; index <= 4; index++)
            _ = frame.GetFieldValue(new(index), NoSplit, AwkValue.FromText);
        Assert.True(memory.Accounting.CurrentCommittedBytes - before < 1024);
        var committed = memory.Accounting.CurrentCommittedBytes;
        var allocations = counters.FieldValueCacheBufferAllocations;
        Assert.Equal(string.Empty, frame.GetFieldValue(new(int.MaxValue), NoSplit, AwkValue.FromText).AsString());
        Assert.Equal(allocations, counters.FieldValueCacheBufferAllocations);
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);

        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldValue(new(1024), NoSplit, AwkValue.FromText));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(allocations, counters.FieldValueCacheBufferAllocations);
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.True(frame.TryGetUnmaterializedFieldBytes(new(1024), NoSplit, out _));
        Assert.Equal("x", frame.GetFieldValue(new(1), NoSplit, _ => throw new InvalidOperationException("Cached.")).AsString());
        frame.ResetRecord();
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CanceledCacheStagingPreservesExistingFieldsAndValues(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8L << 20, counters, enabled: governed);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        long allocations = 0;
        var frame = new AwkFieldFrame(counters, memory.Accounting, value => value.ByteText, () =>
        {
            if (armed && counters.FieldValueCacheBufferAllocations > allocations) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        var record = AwkText.FromSourceLiteralString(new string('x', 8192));
        frame.SetRecordText(record);
        Assert.Equal(8192, frame.GetFieldCount(_ => Enumerable.Range(0, 8192)
            .Select(index => AwkFieldText.FromRecordSlice(index, 1)).ToList()));
        var first = frame.GetFieldValue(new(1), NoSplit, AwkValue.FromText);
        for (var index = 2; index <= 4; index++)
            _ = frame.GetFieldValue(new(index), NoSplit, AwkValue.FromText);
        allocations = counters.FieldValueCacheBufferAllocations;
        var committed = memory.Accounting.CurrentCommittedBytes;
        armed = true;
        var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(8192), NoSplit, AwkValue.FromText));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        armed = false;
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(8192, frame.GetFieldCount(NoSplit));
        Assert.Equal(first, frame.GetFieldValue(new(1), NoSplit, _ => throw new InvalidOperationException("Cached.")));
        Assert.True(frame.TryGetUnmaterializedFieldBytes(new(8192), NoSplit, out _));
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Equal("x", frame.GetFieldValue(new(8192), NoSplit, AwkValue.FromText).AsString());
        frame.ResetRecord();
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SparseLateReadsReuseOnlyCachedValues(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8L << 20, counters, enabled: governed);
        var frame = new AwkFieldFrame(counters, memory.Accounting, value => value.ByteText);
        List<AwkFieldText> fields = Enumerable.Range(0, 8192)
            .Select(index => AwkFieldText.FromRecordSlice(index, 1)).ToList();
        var record = AwkText.FromSourceLiteralString(new string('x', 8192));
        long allocations = 0;
        for (var row = 0; row < 3; row++)
        {
            frame.SetRecordText(record);
            Assert.Equal(8192, frame.GetFieldCount(_ => fields));
            var before = memory.Accounting.CurrentCommittedBytes;
            Assert.Equal("x", frame.GetFieldValue(new(8192), NoSplit, AwkValue.FromText).AsString());
            Assert.InRange(memory.Accounting.CurrentCommittedBytes - before, 0, 1024);
            if (row == 0) allocations = counters.FieldValueCacheBufferAllocations;
            else Assert.Equal(allocations, counters.FieldValueCacheBufferAllocations);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        }
        frame.ResetRecord();
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
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
        var prefix = state.GetFieldValue(1);
        var allocations = counters.AwkTextAllocations;
        Assert.Equal(3, state.FieldCount);
        var again = state.GetFieldValue(1);
        Assert.Same(prefix.ByteText, again.ByteText);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal("007", again.ByteText.AsString());
        Assert.Equal(7, again.AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static List<AwkFieldText> NoSplit(AwkText _) =>
        throw new InvalidOperationException("Fields must already be split.");
}
