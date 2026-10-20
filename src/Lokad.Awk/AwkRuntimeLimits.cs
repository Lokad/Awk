using System;

namespace Lokad.Awk;

internal sealed record AwkRuntimeLimits
{
    public const int CancellationCheckBytes = 4_096;
    public const int DefaultMaxFunctionCallDepth = 1_000;
    public const int DefaultMaxSyntaxNestingDepth = 128;
    public const int DefaultMaxOutputFiles = 10_000;
    public const int DefaultMaxInputFiles = 10_000;
    public const int DefaultMaxPipes = 1_024;
    public const int DefaultMaxIncludeDepth = 128;
    public const int DefaultInputReadBatchLineCount = 128;
    public const int DefaultUtf8BuilderInitialCapacity = 256;
    public const int DefaultRegexCacheSize = 128;

    private AwkRuntimeLimits()
    {
    }

    public int MaxFunctionCallDepth { get; private init; } = DefaultMaxFunctionCallDepth;

    public int MaxSyntaxNestingDepth { get; private init; } = DefaultMaxSyntaxNestingDepth;

    public int MaxOutputFiles { get; private init; } = DefaultMaxOutputFiles;

    public int MaxInputFiles { get; private init; } = DefaultMaxInputFiles;

    public int MaxPipes { get; private init; } = DefaultMaxPipes;

    public int MaxIncludeDepth { get; private init; } = DefaultMaxIncludeDepth;

    public static AwkRuntimeLimits CreateDefault() => new();

    public static AwkRuntimeLimits CreateRestricted() =>
        new()
        {
            MaxFunctionCallDepth = 32,
            MaxSyntaxNestingDepth = 64,
            MaxOutputFiles = 64,
            MaxInputFiles = 64,
            MaxPipes = 8,
            MaxIncludeDepth = 16
        };

    public static AwkRuntimeLimits FromPolicy(AwkExecutionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return policy.Limits;
    }

    public AwkRuntimeLimits WithMaxFunctionCallDepth(int value) =>
        this with { MaxFunctionCallDepth = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxSyntaxNestingDepth(int value) =>
        this with { MaxSyntaxNestingDepth = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxOutputFiles(int value) =>
        this with { MaxOutputFiles = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxInputFiles(int value) =>
        this with { MaxInputFiles = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxPipes(int value) =>
        this with { MaxPipes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxIncludeDepth(int value) =>
        this with { MaxIncludeDepth = RequirePositive(value) };

    internal static int RequirePositive(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        return value;
    }

}
