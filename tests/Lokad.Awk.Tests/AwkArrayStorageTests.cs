using Lokad.Awk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkArrayStorageTests
{
    [Fact]
    public async Task MissingLookupsDoNotAllocateOrCreateAnArray()
    {
        // Keep the measured loop off the test runner's shared worker threads.
        var (storage, name, foundAny, allocatedBytes) = await Task.Factory.StartNew(static () =>
        {
            var storage = new AwkArrayStorage();
            var name = AwkArrayName.FromText("missing");
            var foundAny = storage.TryGetSlots(name, out _);
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 10_000; index++)
                foundAny |= storage.TryGetSlots(name, out _);

            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            return (storage, name, foundAny, allocatedBytes);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        Assert.False(foundAny);
        Assert.Equal(0, allocatedBytes);
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
