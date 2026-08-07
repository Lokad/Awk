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
}
