using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFieldCacheReuseTests
{
    [Fact]
    public void WarmSmallRecordsAvoidRepeatedCacheContainerAllocations()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var record = AwkText.FromString("text\ttext\ttext");
        for (var row = 0; row < 100; row++) ReadRecord();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var row = 0; row < 10000; row++) ReadRecord();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated < 6_500_000, "Allocated bytes: " + allocated);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);

        void ReadRecord()
        {
            state.SetCurrentRecord(record);
            for (var index = 1; index <= 3; index++)
                _ = state.GetFieldValue(index).ByteText.ByteLength;
        }
    }

    [Fact]
    public void ResetDoesNotRetainPreviousFieldText()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var previous = ReadPreviousField(state);
        state.SetCurrentRecord(AwkText.FromString("next"));
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(previous.TryGetTarget(out _));
        Assert.Equal("next", state.GetFieldValue(1).ByteText.AsString());
    }

    [Theory]
    [InlineData(false, 1024)]
    [InlineData(true, 850000)]
    public void UnsplitRecordsAvoidRepeatedEmptyListAllocations(bool finite, long ceiling)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (finite) limits = limits.WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var record = AwkText.FromString("text\ttext\ttext");
        for (var row = 0; row < 100; row++) state.SetCurrentRecord(record);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var row = 0; row < 10000; row++) state.SetCurrentRecord(record);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated < ceiling, "Allocated bytes: " + allocated);
        Assert.Equal(0, counters.FieldSplits);
        Assert.Equal(0, counters.FieldsMaterialized);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void LazyFieldCountsAvoidTemporaryEstimateLists()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var record = AwkText.FromString("text\ttext\ttext");
        for (var row = 0; row < 100; row++) { state.SetCurrentRecord(record); _ = state.FieldCount; }
        var total = 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var row = 0; row < 10000; row++)
        {
            state.SetCurrentRecord(record);
            total += state.FieldCount;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(30000, total);
        Assert.True(allocated < 2000000, "Allocated bytes: " + allocated);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkText> ReadPreviousField(AwkRuntimeState state)
    {
        state.SetCurrentRecord(AwkText.FromString("old café\tsecond"));
        return new(state.GetFieldValue(1).ByteText);
    }
}
