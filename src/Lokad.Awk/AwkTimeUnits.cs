using System;

namespace Lokad.Awk;

internal readonly record struct AwkUnixTimestampSeconds(long Value)
{
    public static AwkUnixTimestampSeconds FromSystemClock() =>
        new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
}

internal readonly record struct AwkRandomSeed(long Value)
{
    public static AwkRandomSeed FromTimestamp(AwkUnixTimestampSeconds timestamp) =>
        new(timestamp.Value);
}

internal readonly record struct AwkRandomSeedRequest
{
    private readonly AwkRandomSeed _seed;

    private AwkRandomSeedRequest(bool hasExplicitSeed, AwkRandomSeed seed)
    {
        HasExplicitSeed = hasExplicitSeed;
        _seed = seed;
    }

    public static AwkRandomSeedRequest CurrentTime { get; } = new(false, default);

    public static AwkRandomSeedRequest Explicit(AwkRandomSeed seed) => new(true, seed);

    public bool HasExplicitSeed { get; }

    public AwkRandomSeed Resolve(AwkUnixTimestampSeconds currentTime) =>
        HasExplicitSeed ? _seed : AwkRandomSeed.FromTimestamp(currentTime);
}
