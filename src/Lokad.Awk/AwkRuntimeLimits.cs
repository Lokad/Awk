using System;

namespace Lokad.Awk;

internal sealed record AwkRuntimeLimits
{
    public const long Unlimited = long.MaxValue;
    public const int DefaultMaxExecutionSteps = 10_000_000;
    public const int DefaultMaxFunctionCallDepth = 64;
    public const int DefaultMaxSyntaxNestingDepth = 128;
    public const int DefaultMaxArrayElements = 1_000_000;
    public const int DefaultMaxFields = 1_000_000;
    public const int DefaultMaxBuilderBytes = 64 << 20;
    public const long DefaultMaxRegexRecordSeparatorBytesScanned = 64L << 20;
    public const int DefaultMaxOutputFiles = 10_000;
    public const int DefaultMaxInputFiles = 10_000;
    public const int DefaultMaxPipes = 1_024;
    public const long DefaultMaxSourceBytes = 64L << 20;
    public const int DefaultMaxIncludeDepth = 128;
    public const int DefaultMaxIncludeFiles = 1_024;
    public const int DefaultInputReadBatchLineCount = 128;
    public const int DefaultUtf8BuilderInitialCapacity = 256;
    public const int DefaultRegexCacheSize = 128;
    public const int DefaultRegexMatchTimeoutSeconds = 2;

    private AwkRuntimeLimits()
    {
    }

    public int MaxExecutionSteps { get; private init; } = DefaultMaxExecutionSteps;

    public int MaxFunctionCallDepth { get; private init; } = DefaultMaxFunctionCallDepth;

    public int MaxSyntaxNestingDepth { get; private init; } = DefaultMaxSyntaxNestingDepth;

    public long MaxHostCalls { get; private init; } = Unlimited;

    public long MaxRecordBytes { get; private init; } = Unlimited;

    public long MaxValueBytes { get; private init; } = Unlimited;

    public long MaxOutputBytes { get; private init; } = Unlimited;

    public int MaxArrayElements { get; private init; } = DefaultMaxArrayElements;

    public int MaxFields { get; private init; } = DefaultMaxFields;

    public int MaxBuilderBytes { get; private init; } = DefaultMaxBuilderBytes;

    public long MaxRegexCaptureBytes { get; private init; } = Unlimited;

    public long MaxRegexRecordSeparatorBytesScanned { get; private init; } =
        DefaultMaxRegexRecordSeparatorBytesScanned;

    public long MaxVmStackBytes { get; private init; } = Unlimited;

    public long MaxExecutionMemoryBytes { get; private init; } = Unlimited;

    public int MaxOutputFiles { get; private init; } = DefaultMaxOutputFiles;

    public int MaxInputFiles { get; private init; } = DefaultMaxInputFiles;

    public int MaxPipes { get; private init; } = DefaultMaxPipes;

    public long MaxSourceBytes { get; private init; } = DefaultMaxSourceBytes;

    public int MaxIncludeDepth { get; private init; } = DefaultMaxIncludeDepth;

    public int MaxIncludeFiles { get; private init; } = DefaultMaxIncludeFiles;

    public static AwkRuntimeLimits CreateDefault() => new();

    public static AwkRuntimeLimits FromPolicy(AwkExecutionPolicy policy) =>
        new()
        {
            MaxExecutionSteps = policy.MaxExecutionSteps,
            MaxFunctionCallDepth = policy.MaxFunctionCallDepth,
            MaxSyntaxNestingDepth = policy.MaxSyntaxNestingDepth,
            MaxHostCalls = policy.MaxHostCalls,
            MaxRecordBytes = policy.MaxRecordBytes,
            MaxValueBytes = policy.MaxValueBytes,
            MaxOutputBytes = policy.MaxOutputBytes,
            MaxArrayElements = policy.MaxArrayElements,
            MaxFields = policy.MaxFields,
            MaxBuilderBytes = policy.MaxBuilderBytes,
            MaxRegexCaptureBytes = policy.MaxRegexCaptureBytes,
            MaxRegexRecordSeparatorBytesScanned = policy.MaxRegexRecordSeparatorBytesScanned,
            MaxVmStackBytes = policy.MaxVmStackBytes,
            MaxExecutionMemoryBytes = policy.MaxExecutionMemoryBytes,
            MaxOutputFiles = policy.MaxOutputFiles,
            MaxInputFiles = policy.MaxInputFiles,
            MaxPipes = policy.MaxPipes,
            MaxSourceBytes = policy.MaxSourceBytes,
            MaxIncludeDepth = policy.MaxIncludeDepth,
            MaxIncludeFiles = policy.MaxIncludeFiles
        };

    public AwkRuntimeLimits WithMaxExecutionSteps(int value) =>
        this with { MaxExecutionSteps = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxFunctionCallDepth(int value) =>
        this with { MaxFunctionCallDepth = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxSyntaxNestingDepth(int value) =>
        this with { MaxSyntaxNestingDepth = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxHostCalls(long value) =>
        this with { MaxHostCalls = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxRecordBytes(long value) =>
        this with { MaxRecordBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxValueBytes(long value) =>
        this with { MaxValueBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxOutputBytes(long value) =>
        this with { MaxOutputBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxArrayElements(int value) =>
        this with { MaxArrayElements = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxFields(int value) =>
        this with { MaxFields = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxBuilderBytes(int value) =>
        this with { MaxBuilderBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxRegexCaptureBytes(long value) =>
        this with { MaxRegexCaptureBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxRegexRecordSeparatorBytesScanned(long value) =>
        this with { MaxRegexRecordSeparatorBytesScanned = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxVmStackBytes(long value) =>
        this with { MaxVmStackBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxExecutionMemoryBytes(long value) =>
        this with { MaxExecutionMemoryBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxOutputFiles(int value) =>
        this with { MaxOutputFiles = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxInputFiles(int value) =>
        this with { MaxInputFiles = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxPipes(int value) =>
        this with { MaxPipes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxSourceBytes(long value) =>
        this with { MaxSourceBytes = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxIncludeDepth(int value) =>
        this with { MaxIncludeDepth = RequirePositive(value) };

    public AwkRuntimeLimits WithMaxIncludeFiles(int value) =>
        this with { MaxIncludeFiles = RequirePositive(value) };

    private static int RequirePositive(int value) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));

    private static long RequirePositive(long value) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
}
