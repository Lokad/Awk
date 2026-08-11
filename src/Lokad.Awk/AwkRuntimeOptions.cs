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
        AwkExecutionEngineKind executionEngineKind,
        bool allowFileRedirections = true,
        bool allowCommandPipes = true,
        AwkPath? accessiblePathRoot = null,
        TimeSpan? commandTimeout = null,
        TimeSpan? cleanupTimeout = null,
        bool forwardCommandEnvironment = true,
        bool allowSourceIncludes = true,
        bool allowDynamicMainInputPaths = true)
    {
        var randomGeneratorSnapshot = randomGenerator.Clone();
        CurrentUnixTimeSeconds = currentUnixTimeSeconds;
        TimeZone = timeZone;
        _createRandomGenerator = randomGeneratorSnapshot.Clone;
        Counters = counters;
        Limits = limits;
        RegexEngineKind = regexEngineKind;
        ExecutionEngineKind = executionEngineKind;
        AllowFileRedirections = allowFileRedirections;
        AllowCommandPipes = allowCommandPipes;
        AccessiblePathRoot = accessiblePathRoot;
        CommandTimeout = commandTimeout ?? TimeSpan.FromMinutes(5);
        CleanupTimeout = cleanupTimeout ?? TimeSpan.FromSeconds(30);
        ForwardCommandEnvironment = forwardCommandEnvironment;
        AllowSourceIncludes = allowSourceIncludes;
        AllowDynamicMainInputPaths = allowDynamicMainInputPaths;
    }

    public Func<AwkUnixTimestampSeconds> CurrentUnixTimeSeconds { get; }

    public TimeZoneInfo TimeZone { get; }

    public AwkRandomGenerator CreateRandomGenerator() => _createRandomGenerator();

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRegexEngineKind RegexEngineKind { get; }

    public AwkExecutionEngineKind ExecutionEngineKind { get; }

    public bool AllowFileRedirections { get; }

    public bool AllowCommandPipes { get; }

    public AwkPath? AccessiblePathRoot { get; }

    public TimeSpan CommandTimeout { get; }

    public TimeSpan CleanupTimeout { get; }

    public bool ForwardCommandEnvironment { get; }

    public bool AllowSourceIncludes { get; }

    public bool AllowDynamicMainInputPaths { get; }

    public static AwkRuntimeOptions CreateSystemDefault() => Create(AwkExecutionPolicy.CreateDefault());

    public static AwkRuntimeOptions Create(AwkExecutionPolicy policy) =>
        new(
            () => new AwkUnixTimestampSeconds(policy.TimeProvider.GetUtcNow().ToUnixTimeSeconds()),
            policy.TimeZone,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.FromPolicy(policy),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter,
            policy.AllowFileRedirections,
            policy.AllowCommandPipes,
            policy.AccessiblePathRoot,
            policy.CommandTimeout,
            policy.CleanupTimeout,
            policy.ForwardCommandEnvironment,
            policy.AllowSourceIncludes,
            policy.AllowDynamicMainInputPaths);
}
