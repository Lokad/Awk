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
        bool allowFileRedirections = true,
        bool allowCommandPipes = true,
        AwkPath? accessiblePathRoot = null,
        bool forwardCommandEnvironment = true,
        bool allowSourceIncludes = true,
        bool allowDynamicMainInputPaths = true,
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
        AllowFileRedirections = allowFileRedirections;
        AllowCommandPipes = allowCommandPipes;
        AccessiblePathRoot = accessiblePathRoot;

        ForwardCommandEnvironment = forwardCommandEnvironment;
        AllowSourceIncludes = allowSourceIncludes;
        AllowDynamicMainInputPaths = allowDynamicMainInputPaths;

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

    public bool AllowFileRedirections { get; }

    public bool AllowCommandPipes { get; }

    public AwkPath? AccessiblePathRoot { get; }

    public bool ForwardCommandEnvironment { get; }

    public bool AllowSourceIncludes { get; }

    public bool AllowDynamicMainInputPaths { get; }

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
            policy.AllowFileRedirections,
            policy.AllowCommandPipes,
            policy.AccessiblePathRoot,
            policy.ForwardCommandEnvironment,
            policy.AllowSourceIncludes,
            policy.AllowDynamicMainInputPaths,
            policy.ExposeEnvironment,
            policy.ExposeHostErrorDetails,
            policy.MaxHostErrorBytes,
            invocationBudget ?? policy.MemoryBudget,
            policy.RegexEngine);
}
