using System;

namespace Lokad.Awk;

internal sealed class AwkRuntimeOptions
{
    private readonly Func<AwkRandomGenerator> _createRandomGenerator;

    public AwkRuntimeOptions(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        AwkRandomGenerator randomGenerator,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits,
        AwkRegexEngineKind regexEngineKind,
        AwkExecutionEngineKind executionEngineKind)
        : this(
            currentUnixTimeSeconds,
            timeZone,
            CreateSnapshotFactory(randomGenerator),
            counters,
            limits,
            regexEngineKind,
            executionEngineKind)
    {
    }

    private AwkRuntimeOptions(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        Func<AwkRandomGenerator> createRandomGenerator,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits,
        AwkRegexEngineKind regexEngineKind,
        AwkExecutionEngineKind executionEngineKind)
    {
        CurrentUnixTimeSeconds = currentUnixTimeSeconds;
        TimeZone = timeZone;
        _createRandomGenerator = createRandomGenerator;
        Counters = counters;
        Limits = limits;
        RegexEngineKind = regexEngineKind;
        ExecutionEngineKind = executionEngineKind;
    }

    public Func<AwkUnixTimestampSeconds> CurrentUnixTimeSeconds { get; }

    public TimeZoneInfo TimeZone { get; }

    public AwkRandomGenerator CreateRandomGenerator() => _createRandomGenerator();

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRegexEngineKind RegexEngineKind { get; }

    public AwkExecutionEngineKind ExecutionEngineKind { get; }

    public static AwkRuntimeOptions CreateSystemDefault() =>
        new(
            AwkUnixTimestampSeconds.FromSystemClock,
            TimeZoneInfo.Local,
            static () => new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);

    private static Func<AwkRandomGenerator> CreateSnapshotFactory(AwkRandomGenerator randomGenerator)
    {
        var snapshot = randomGenerator.Clone();
        return snapshot.Clone;
    }
}
