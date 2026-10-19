using System;

namespace Lokad.Awk;

internal sealed record AwkRuntimeOptions
{
    private readonly Func<AwkRandomGenerator> _createRandomGenerator;

    public AwkRuntimeOptions(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        AwkRandomGenerator randomGenerator,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits,
        AwkRegexEngineKind regexEngineKind,
        bool allowFileRedirections = true,
        bool allowCommandPipes = true,
        AwkPath? accessiblePathRoot = null,
        TimeSpan? commandTimeout = null,
        TimeSpan? cleanupTimeout = null,
        bool forwardCommandEnvironment = true,
        bool allowSourceIncludes = true,
        bool allowDynamicMainInputPaths = true,
        TimeSpan? hostOperationTimeout = null,
        bool exposeEnvironment = true,
        bool exposeHostErrorDetails = true,
        int maxHostErrorBytes = 4 << 10,
        IMemoryBudget? memoryBudget = null,
        IAwkRegexEngine? regexEngine = null)
    {
        var randomGeneratorSnapshot = randomGenerator.Clone();
        CurrentUnixTimeSeconds = currentUnixTimeSeconds;
        TimeZone = timeZone;
        _createRandomGenerator = randomGeneratorSnapshot.Clone;
        Counters = counters;
        Limits = limits;
        RegexEngineKind = regexEngineKind;
        AllowFileRedirections = allowFileRedirections;
        AllowCommandPipes = allowCommandPipes;
        AccessiblePathRoot = accessiblePathRoot;
        CommandTimeout = commandTimeout ?? TimeSpan.FromMinutes(5);
        CleanupTimeout = cleanupTimeout ?? TimeSpan.FromSeconds(30);
        ForwardCommandEnvironment = forwardCommandEnvironment;
        AllowSourceIncludes = allowSourceIncludes;
        AllowDynamicMainInputPaths = allowDynamicMainInputPaths;
        HostOperationTimeout = hostOperationTimeout ?? TimeSpan.FromMinutes(5);
        ExposeEnvironment = exposeEnvironment;
        ExposeHostErrorDetails = exposeHostErrorDetails;
        MaxHostErrorBytes = maxHostErrorBytes;
        MemoryBudget = memoryBudget;
        RegexEngine = regexEngine;
    }

    public IMemoryBudget? MemoryBudget { get; init; }

    public IAwkRegexEngine? RegexEngine { get; init; }

    public AwkExecutionMemoryOwner MemoryOwner { get; init; } = AwkExecutionMemoryOwner.Disabled;

    public Func<AwkUnixTimestampSeconds> CurrentUnixTimeSeconds { get; }

    public TimeZoneInfo TimeZone { get; }

    public AwkRandomGenerator CreateRandomGenerator() => _createRandomGenerator();

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRegexEngineKind RegexEngineKind { get; }

    public bool AllowFileRedirections { get; }

    public bool AllowCommandPipes { get; }

    public AwkPath? AccessiblePathRoot { get; }

    public TimeSpan CommandTimeout { get; }

    public TimeSpan CleanupTimeout { get; }

    public bool ForwardCommandEnvironment { get; }

    public bool AllowSourceIncludes { get; }

    public bool AllowDynamicMainInputPaths { get; }

    public TimeSpan HostOperationTimeout { get; }

    public bool ExposeEnvironment { get; }

    public bool ExposeHostErrorDetails { get; }

    public int MaxHostErrorBytes { get; }

    public static AwkRuntimeOptions CreateSystemDefault() => Create(AwkExecutionPolicy.CreateDefault());

    public static AwkRuntimeOptions Create(AwkExecutionPolicy policy, IMemoryBudget? invocationBudget = null) =>
        new(
            () => new AwkUnixTimestampSeconds(policy.TimeProvider.GetUtcNow().ToUnixTimeSeconds()),
            policy.TimeZone,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.FromPolicy(policy),
            policy.RegexEngine is null ? AwkRegexEngineKind.DotNetCompatibility : AwkRegexEngineKind.Utf8Adapter,
            policy.AllowFileRedirections,
            policy.AllowCommandPipes,
            policy.AccessiblePathRoot,
            policy.CommandTimeout,
            policy.CleanupTimeout,
            policy.ForwardCommandEnvironment,
            policy.AllowSourceIncludes,
            policy.AllowDynamicMainInputPaths,
            policy.HostOperationTimeout,
            policy.ExposeEnvironment,
            policy.ExposeHostErrorDetails,
            policy.MaxHostErrorBytes,
            invocationBudget ?? policy.MemoryBudget,
            policy.RegexEngine);
}
