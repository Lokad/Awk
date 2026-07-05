namespace Lokad.Awk;

internal sealed class AwkRandomRuntime(AwkRandomGenerator generator, AwkTimeRuntime timeRuntime)
{
    public double NextNumber() => generator.NextDouble();

    public AwkRandomSeed Seed(AwkRandomSeedRequest seed)
    {
        var resolvedSeed = seed.Resolve(timeRuntime.CurrentUnixTimeSeconds());
        return generator.Seed(resolvedSeed);
    }
}
