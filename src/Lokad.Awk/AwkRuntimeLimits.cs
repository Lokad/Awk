using System;

namespace Lokad.Awk;

internal sealed record AwkRuntimeLimits
{
    public const long Unlimited = long.MaxValue;
    public const int CancellationCheckBytes = 4_096;
    public const int DefaultMaxFunctionCallDepth = 1_000;
    public const int DefaultMaxSyntaxNestingDepth = 128;
    public const int DefaultMaxArrayElements = 1_000_000;
    public const int DefaultMaxFields = 1_000_000;
    public const int DefaultMaxOutputFiles = 10_000;
    public const int DefaultMaxInputFiles = 10_000;
    public const int DefaultMaxPipes = 1_024;
    public const long DefaultMaxSourceBytes = 64L << 20;
    public const int DefaultMaxSourceUnits = 10_000;
    public const long DefaultMaxExpandedSourceBytes = 64L << 20;
    public const int DefaultMaxIncludeDepth = 128;
    public const int DefaultMaxIncludeFiles = 1_024;
    public const int DefaultInputReadBatchLineCount = 128;
    public const int DefaultUtf8BuilderInitialCapacity = 256;
    public const int DefaultRegexCacheSize = 128;
    public const int DefaultRegexMatchTimeoutSeconds = 2;

    private AwkRuntimeLimits()
    {
    }

    public int MaxFunctionCallDepth { get; private init; } = DefaultMaxFunctionCallDepth;

    public int MaxSyntaxNestingDepth { get; private init; } = DefaultMaxSyntaxNestingDepth;

    public int MaxArrayElements { get; private init; } = DefaultMaxArrayElements;

    public int MaxFields { get; private init; } = DefaultMaxFields;

    public int MaxSymbols { get; private init; } = 100_000;

    public int MaxIdentifierBytes { get; private init; } = 64 << 10;

    public long MaxRegexPatternBytes { get; private init; } = 8L << 20;

    public int MaxRegexGroups { get; private init; } = 10_000;

    public long MaxRegexCacheBytes { get; private init; } = 64L << 20;

    public int MaxOutputFiles { get; private init; } = DefaultMaxOutputFiles;

    public int MaxInputFiles { get; private init; } = DefaultMaxInputFiles;

    public int MaxPipes { get; private init; } = DefaultMaxPipes;

    public long MaxSourceBytes { get; private init; } = DefaultMaxSourceBytes;

    public int MaxSourceUnits { get; private init; } = DefaultMaxSourceUnits;

    public long MaxExpandedSourceBytes { get; private init; } = DefaultMaxExpandedSourceBytes;

    public int MaxFrontendCharacters { get; private init; } = int.MaxValue;

    public int MaxFrontendTokens { get; private init; } = int.MaxValue;

    public int MaxFrontendDiagnostics { get; private init; } = int.MaxValue;

    public int MaxIncludeDepth { get; private init; } = DefaultMaxIncludeDepth;

    public int MaxIncludeFiles { get; private init; } = DefaultMaxIncludeFiles;

    public static AwkRuntimeLimits CreateDefault() => new();

    public static AwkRuntimeLimits CreateRestricted() =>
        new()
        {
            MaxFunctionCallDepth = 32,
            MaxSyntaxNestingDepth = 64,
            MaxArrayElements = 100_000,
            MaxFields = 100_000,
            MaxSymbols = 10_000,
            MaxIdentifierBytes = 1 << 10,
            MaxRegexPatternBytes = 64L << 10,
            MaxRegexGroups = 256,
            MaxRegexCacheBytes = 8L << 20,
            MaxOutputFiles = 64,
            MaxInputFiles = 64,
            MaxPipes = 8,
            MaxSourceBytes = 4L << 20,
            MaxSourceUnits = 256,
            MaxExpandedSourceBytes = 4L << 20,
            MaxFrontendCharacters = 512 << 10,
            MaxFrontendTokens = 250_000,
            MaxFrontendDiagnostics = 256,
            MaxIncludeDepth = 16,
            MaxIncludeFiles = 64
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

    public AwkRuntimeLimits WithMaxArrayElements(int value) =>
        this with { MaxArrayElements = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxFields(int value) =>
        this with { MaxFields = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxSymbols(int value) =>
        this with { MaxSymbols = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxIdentifierBytes(int value) =>
        this with { MaxIdentifierBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxRegexPatternBytes(long value) =>
        this with { MaxRegexPatternBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxRegexGroups(int value) =>
        this with { MaxRegexGroups = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxRegexCacheBytes(long value) =>
        this with { MaxRegexCacheBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxOutputFiles(int value) =>
        this with { MaxOutputFiles = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxInputFiles(int value) =>
        this with { MaxInputFiles = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxPipes(int value) =>
        this with { MaxPipes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxSourceBytes(long value) =>
        this with { MaxSourceBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxSourceUnits(int value) =>
        this with { MaxSourceUnits = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxExpandedSourceBytes(long value) =>
        this with { MaxExpandedSourceBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxFrontendCharacters(int value) =>
        this with { MaxFrontendCharacters = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxFrontendTokens(int value) =>
        this with { MaxFrontendTokens = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxFrontendDiagnostics(int value) =>
        this with { MaxFrontendDiagnostics = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxIncludeDepth(int value) =>
        this with { MaxIncludeDepth = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxIncludeFiles(int value) =>
        this with { MaxIncludeFiles = RequirePositive(value) };

    internal static int RequirePositive(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        return value;
    }

    internal static long RequirePositive(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        return value;
    }
}
