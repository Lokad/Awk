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
    public void Awk_ArrayKeyComparerBreaksKnownStableHashCollision()
    {
        var first = new AwkArrayKey("k32728");
        var second = new AwkArrayKey("k261234");
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        var comparer = AwkArrayKeyComparer.Instance;

        Assert.NotEqual(comparer.GetHashCode(first), comparer.GetHashCode(second));
        var values = new Dictionary<AwkArrayKey, int>(comparer)
        {
            [first] = 1,
            [second] = 2
        };
        Assert.Equal(2, values.Count);
    }
}
