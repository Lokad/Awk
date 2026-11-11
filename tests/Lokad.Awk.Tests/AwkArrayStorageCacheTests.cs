using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkArrayStorageCacheTests
{
    [Fact]
    public void BoundIdentityObservesReplacementAndClearButNotScopeRecreation()
    {
        var storage = new AwkArrayStorage();
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("scratch"), 1);
        var identity = storage.ResolveArray(name);
        var replacement = Values(9);
        storage.SetSlots(name, replacement);
        Assert.Same(identity, storage.ResolveArray(name));
        Assert.Equal(9, identity[AwkArrayKey.FromOrdinal(1)].AsNumber());
        storage.Clear(name);
        Assert.Same(identity, storage.ResolveArray(name));
        Assert.Empty(identity);
        Assert.True(storage.Remove(name, out _));
        Assert.False(storage.TryGetArray(name, out _));
        Assert.NotSame(identity, storage.ResolveArray(name));
    }

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
        Assert.True(storage.TryGetArray(name, out var original));
        Assert.Same(first, original);
        Assert.True(storage.Contains(name));
        Assert.Equal(1, counters.ArrayContainerCacheHits);

        var second = Values(9);
        storage.SetSlots(name, second);
        Assert.True(storage.TryGetArray(name, out var replacement));
        Assert.Same(original, replacement);
        Assert.Equal(9, replacement[AwkArrayKey.FromOrdinal(1)].AsNumber());
        storage.Clear(name);
        Assert.True(storage.TryGetArray(name, out var cleared));
        Assert.Same(original, cleared);
        Assert.Empty(cleared);
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
        Assert.True(storage.TryGetArray(first, out var selected));
        Assert.Same(firstSlots, selected);
        Assert.True(storage.TryGetArray(second, out selected));
        Assert.Same(secondSlots, selected);
        Assert.True(storage.Remove(first, out _));
        Assert.False(storage.Contains(first));
        Assert.True(storage.TryGetArray(second, out selected));
        Assert.Same(secondSlots, selected);
        Assert.True(storage.Remove(second, out _));
        Assert.False(storage.Contains(second));
        Assert.False(storage.TryGetArray(second, out _));
        storage.SetSlots(second, Values(4));
        Assert.True(storage.TryGetArray(global, out selected));
        Assert.Same(globalSlots, selected);
        Assert.True(storage.TryGetArray(second, out selected));
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
    private static WeakReference<AwkArrayCell> RetireCachedContainer(
        AwkArrayStorage storage, bool remove)
    {
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("scratch"), 1);
        var slots = Values(7);
        storage.SetSlots(name, slots);
        Assert.True(storage.TryGetArray(name, out _));
        var reference = new WeakReference<AwkArrayCell>(DictionaryCell(slots, AwkArrayKey.FromOrdinal(1)));
        if (remove)
            Assert.True(storage.Remove(name, out _));
        else
            storage.SetSlots(name, Values(9));
        return reference;
    }

    private static AwkArrayCell DictionaryCell(AwkArrayIdentity array, AwkArrayKey key)
    {
        Assert.True(array.TryGetEntry(key, out _, out _, out var cell));
        return Assert.IsType<AwkArrayCell>(cell);
    }

    private static AwkArrayIdentity Values(int number) =>
        new(0, AwkArrayKeyComparer.Instance) { [AwkArrayKey.FromOrdinal(1)] = AwkValue.ForVariableStorage(AwkValue.FromNumber(number)) };
}
