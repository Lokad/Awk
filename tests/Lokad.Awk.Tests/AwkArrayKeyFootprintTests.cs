namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkArrayKeyFootprintTests
{
    [Fact]
    public void ThousandNumericCellsKeepCompactKeysWithoutTextAllocation()
    {
        var slot = AwkVariableSlot.FromValue(AwkValue.FromNumber(7));
        var comparer = AwkArrayKeyComparer.Instance;
        _ = new Dictionary<AwkArrayKey, AwkArrayCell>(1, comparer) { [AwkArrayKey.FromOrdinal(1)] = new(slot) };
        var before = GC.GetAllocatedBytesForCurrentThread();
        var entries = new Dictionary<AwkArrayKey, AwkArrayCell>(1_024, comparer);
        for (var i = 0; i < 1_024; i++)
            entries[AwkArrayKey.FromOrdinal(i)] = new(slot);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.InRange(allocated, 1, 128 * 1_024);
        Assert.Equal(1_024, entries.Count);
        for (var i = 0; i < 1_024; i++)
            Assert.Equal(slot, entries[new AwkArrayKey(i.ToString(System.Globalization.CultureInfo.InvariantCulture))].Slot);
    }
}
