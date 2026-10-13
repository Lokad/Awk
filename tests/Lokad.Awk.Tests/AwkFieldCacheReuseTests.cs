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
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkText> ReadPreviousField(AwkRuntimeState state)
    {
        state.SetCurrentRecord(AwkText.FromString("old café\tsecond"));
        return new(state.GetFieldValue(1).ByteText);
    }
}
