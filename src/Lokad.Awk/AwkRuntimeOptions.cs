using System;

namespace Lokad.Awk;

internal sealed class AwkRuntimeOptions
{
    public const int DefaultMaxExecutionSteps = AwkRuntimeLimits.DefaultMaxExecutionSteps;

    public AwkRuntimeOptions(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        AwkRandomGenerator randomGenerator,
        int maxExecutionSteps = DefaultMaxExecutionSteps)
        : this(
            currentUnixTimeSeconds,
            timeZone,
            randomGenerator,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(maxExecutionSteps))
    {
    }

    public AwkRuntimeOptions(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        AwkRandomGenerator randomGenerator,
        AwkRuntimeCounters counters,
        int maxExecutionSteps = DefaultMaxExecutionSteps)
        : this(
            currentUnixTimeSeconds,
            timeZone,
            randomGenerator,
            counters,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(maxExecutionSteps))
    {
    }

    public AwkRuntimeOptions(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        AwkRandomGenerator randomGenerator,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits,
        AwkRegexEngineKind regexEngineKind = AwkRegexEngineKind.DotNetCompatibility,
        AwkExecutionEngineKind executionEngineKind = AwkExecutionEngineKind.Interpreter)
    {
        CurrentUnixTimeSeconds = currentUnixTimeSeconds ?? throw new ArgumentNullException(nameof(currentUnixTimeSeconds));
        TimeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
        RandomGenerator = randomGenerator ?? throw new ArgumentNullException(nameof(randomGenerator));
        ArgumentNullException.ThrowIfNull(counters);
        ArgumentNullException.ThrowIfNull(limits);
        Counters = counters;
        Limits = limits;
        RegexEngineKind = regexEngineKind;
        ExecutionEngineKind = executionEngineKind;
    }

    public Func<AwkUnixTimestampSeconds> CurrentUnixTimeSeconds { get; }

    public TimeZoneInfo TimeZone { get; }

    public AwkRandomGenerator RandomGenerator { get; }

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRegexEngineKind RegexEngineKind { get; }

    public AwkExecutionEngineKind ExecutionEngineKind { get; }

    public static AwkRuntimeOptions CreateSystemDefault() =>
        new(
            AwkUnixTimestampSeconds.FromSystemClock,
            TimeZoneInfo.Local,
            new AwkRandomGenerator());
}
