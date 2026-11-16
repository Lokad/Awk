namespace Lokad.Awk.Tests;

public sealed class AwkBoundedCacheTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void ReplacementPublishesBeforeReleaseAndKeepsTheSameOwnerAlive(int capacity)
    {
        var previous = new object();
        var replacement = new object();
        var other = new object();
        var released = new List<object>();
        AwkBoundedCache<string, object>? cache = null;
        cache = new(capacity, value =>
        {
            released.Add(value);
            if (ReferenceEquals(value, previous))
            {
                Assert.NotNull(cache);
                Assert.True(cache.TryGetValue("key", out var current));
                Assert.Same(replacement, current);
            }
        });
        if (capacity > 1) cache.Set("other", other);
        cache.Set("key", previous);
        cache.Set("key", previous);
        Assert.Empty(released);
        cache.Set("key", replacement);
        Assert.Same(previous, Assert.Single(released));
        Assert.True(cache.TryGetValue("key", out var actual));
        Assert.Same(replacement, actual);
        cache.Clear();
        Assert.Equal(capacity + 1, released.Count);
        Assert.Equal(released.Count, released.Distinct().Count());
        Assert.Contains(replacement, released);
        if (capacity > 1) Assert.Contains(other, released);
        Assert.False(cache.TryGetValue("key", out _));
    }

    [Fact]
    public void ClearAndReinsertCannotReturnOrReleaseRetiredEntriesAgain()
    {
        var released = new List<object>();
        var cache = new AwkBoundedCache<string, object>(1, released.Add);
        var first = new object();
        var second = new object();
        var third = new object();
        cache.Set("key", first);
        Assert.True(cache.TryGetValue("key", out var actual));
        Assert.Same(first, actual);
        cache.Clear();
        Assert.False(cache.TryGetValue("key", out _));
        cache.Set("key", second);
        cache.Set("third", third);
        Assert.False(cache.TryGetValue("key", out _));
        Assert.True(cache.TryGetValue("third", out actual));
        Assert.Same(third, actual);
        cache.Clear();
        cache.Clear();
        Assert.Equal(new[] { first, second, third }, released);
        Assert.False(cache.TryGetValue("third", out _));
    }

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
