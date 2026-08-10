using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkArrayStorageTests
{
    [Fact]
    public void MissingLookupsDoNotAllocateOrCreateAnArray()
    {
        var storage = new AwkArrayStorage();
        var name = AwkArrayName.FromText("missing");

        Assert.False(storage.TryGetSlots(name, out _));
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
            Assert.False(storage.TryGetSlots(name, out _));

        var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(allocatedBefore, allocatedAfter);
        Assert.False(storage.Contains(name));
    }

    [Fact]
    public void ResolvingAMissingArrayCreatesOneContainer()
    {
        var storage = new AwkArrayStorage();
        var name = AwkArrayName.FromText("values");

        var resolved = storage.ResolveSlots(name);

        Assert.True(storage.TryGetSlots(name, out var found));
        Assert.Same(resolved, found);
    }
}
