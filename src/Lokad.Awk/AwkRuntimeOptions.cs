using System;

namespace Lokad.Awk;

internal sealed class AwkRuntimeOptions
{
    private readonly AwkRandomGenerator _randomGenerator;

    public const int DefaultMaxExecutionSteps = AwkRuntimeLimits.DefaultMaxExecutionSteps;

    public AwkRuntimeOptions(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        AwkRandomGenerator randomGenerator,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits,
        AwkRegexEngineKind regexEngineKind,
        AwkExecutionEngineKind executionEngineKind)
    {
        CurrentUnixTimeSeconds = currentUnixTimeSeconds;
        TimeZone = timeZone;
        _randomGenerator = randomGenerator.Clone();
        Counters = counters;
        Limits = limits;
        RegexEngineKind = regexEngineKind;
        ExecutionEngineKind = executionEngineKind;
    }

    public Func<AwkUnixTimestampSeconds> CurrentUnixTimeSeconds { get; }

    public TimeZoneInfo TimeZone { get; }

    public AwkRandomGenerator CreateRandomGenerator() => _randomGenerator.Clone();

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRegexEngineKind RegexEngineKind { get; }

    public AwkExecutionEngineKind ExecutionEngineKind { get; }

    public static AwkRuntimeOptions CreateSystemDefault() =>
        new(
            AwkUnixTimestampSeconds.FromSystemClock,
            TimeZoneInfo.Local,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
}
