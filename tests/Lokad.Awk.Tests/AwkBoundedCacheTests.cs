namespace Lokad.Awk.Tests;

public sealed class AwkBoundedCacheTests
{
    [Fact]
    public void Awk_BoundedCacheEvictsLeastRecentEntry()
    {
        var cache = new AwkBoundedCache<string, int>(capacity: 2);
        cache.Set("a", 1);
        cache.Set("b", 2);

        Assert.True(cache.TryGetValue("a", out var a));
        cache.Set("c", 3);

        Assert.Equal(1, a);
        Assert.False(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out var c));
        Assert.Equal(3, c);
    }

    [Fact]
    public void Awk_BoundedCacheEvictsEntriesToRespectWeightBudget()
    {
        var cache = new AwkBoundedCache<string, int>(capacity: 4, maxWeight: 5);
        Assert.True(cache.Set("a", 1, weight: 3));
        Assert.True(cache.Set("b", 2, weight: 2));

        Assert.True(cache.Set("c", 3, weight: 4));

        Assert.False(cache.TryGetValue("a", out _));
        Assert.False(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out var c));
        Assert.Equal(3, c);
        Assert.False(cache.Set("oversized", 4, weight: 6));
    }
}
