using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkArrayStorageCacheTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("ARGV")]
    public void ReplacementAndClearRemainVisibleAfterCachedRead(string text)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var storage = new AwkArrayStorage(AwkArrayKeyComparer.Instance, counters);
        var name = AwkArrayName.FromText(text);
        var first = Values(7);
        storage.SetSlots(name, first);
        Assert.True(storage.TryGetSlots(name, out var original));
        Assert.Same(first, original);
        Assert.True(storage.Contains(name));
        Assert.Equal(1, counters.ArrayContainerCacheHits);

        var second = Values(9);
        storage.SetSlots(name, second);
        Assert.True(storage.TryGetSlots(name, out var replacement));
        Assert.Same(second, replacement);
        storage.Clear(name);
        Assert.True(storage.TryGetSlots(name, out var cleared));
        Assert.Empty(Assert.IsType<Dictionary<AwkArrayKey, AwkVariableSlot>>(cleared));
    }

    [Fact]
    public void LocalIdentitiesAndRemovalCannotReuseAnOldContainer()
    {
        var storage = new AwkArrayStorage();
        var global = AwkArrayName.FromText("a");
        var first = AwkArrayName.FunctionLocal(global.Identifier, 1);
        var second = AwkArrayName.FunctionLocal(global.Identifier, 2);
        var globalSlots = Values(1);
        var firstSlots = Values(2);
        var secondSlots = Values(3);
        storage.SetSlots(global, globalSlots);
        storage.SetSlots(first, firstSlots);
        storage.SetSlots(second, secondSlots);
        var special = AwkArrayName.FromText("ARGV");
        storage.SetSlots(special, Values(5));
        Assert.Equal(new[] { global, special }.OrderBy(name => name.ToString()),
            storage.Names.OrderBy(name => name.ToString()));
        Assert.True(storage.TryGetSlots(first, out var selected));
        Assert.Same(firstSlots, selected);
        Assert.True(storage.TryGetSlots(second, out selected));
        Assert.Same(secondSlots, selected);
        Assert.True(storage.Remove(first, out _));
        Assert.False(storage.Contains(first));
        Assert.True(storage.TryGetSlots(second, out selected));
        Assert.Same(secondSlots, selected);
        Assert.True(storage.Remove(second, out _));
        Assert.False(storage.Contains(second));
        Assert.False(storage.TryGetSlots(second, out _));
        storage.SetSlots(second, Values(4));
        Assert.True(storage.TryGetSlots(global, out selected));
        Assert.Same(globalSlots, selected);
        Assert.True(storage.TryGetSlots(second, out selected));
        Assert.NotSame(secondSlots, selected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacedOrRemovedContainersAreNotRetained(bool remove)
    {
        var storage = new AwkArrayStorage();
        var reference = RetireCachedContainer(storage, remove);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(reference.TryGetTarget(out _));
        GC.KeepAlive(storage);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Dictionary<AwkArrayKey, AwkVariableSlot>> RetireCachedContainer(
        AwkArrayStorage storage, bool remove)
    {
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("scratch"), 1);
        var slots = Values(7);
        storage.SetSlots(name, slots);
        Assert.True(storage.TryGetSlots(name, out _));
        var reference = new WeakReference<Dictionary<AwkArrayKey, AwkVariableSlot>>(slots);
        if (remove)
            Assert.True(storage.Remove(name, out _));
        else
            storage.SetSlots(name, Values(9));
        return reference;
    }

    private static Dictionary<AwkArrayKey, AwkVariableSlot> Values(int number) =>
        new(AwkArrayKeyComparer.Instance) { [AwkArrayKey.FromOrdinal(1)] = AwkVariableSlot.FromValue(AwkValue.FromNumber(number)) };
}
