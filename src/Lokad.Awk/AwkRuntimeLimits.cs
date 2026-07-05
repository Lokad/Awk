using System;

namespace Lokad.Awk;

internal sealed class AwkRuntimeLimits
{
    public const long Unlimited = long.MaxValue;
    public const int DefaultMaxExecutionSteps = 1_000_000;
    public const int DefaultMaxFunctionCallDepth = 64;
    public const int DefaultMaxArrayElements = 1_000_000;
    public const int DefaultMaxFields = 1_000_000;
    public const int DefaultMaxBuilderBytes = 64 << 20;
    public const int DefaultMaxOutputFiles = 10_000;
    public const int DefaultInputReadBatchLineCount = 128;
    public const int DefaultUtf8BuilderInitialCapacity = 256;
    public const int DefaultRegexCacheSize = 128;
    public const int DefaultRegexMatchTimeoutSeconds = 2;

    private AwkRuntimeLimits(
        int maxExecutionSteps,
        int maxFunctionCallDepth,
        long maxHostCalls,
        long maxRecordBytes,
        long maxValueBytes,
        long maxOutputBytes,
        int maxArrayElements,
        int maxFields,
        int maxBuilderBytes,
        long maxRegexCaptureBytes,
        long maxVmStackBytes,
        long maxExecutionMemoryBytes,
        int maxOutputFiles)
    {
        ThrowIfNonPositive(maxExecutionSteps, nameof(maxExecutionSteps));
        ThrowIfNonPositive(maxFunctionCallDepth, nameof(maxFunctionCallDepth));
        ThrowIfNonPositive(maxHostCalls, nameof(maxHostCalls));
        ThrowIfNonPositive(maxRecordBytes, nameof(maxRecordBytes));
        ThrowIfNonPositive(maxValueBytes, nameof(maxValueBytes));
        ThrowIfNonPositive(maxOutputBytes, nameof(maxOutputBytes));
        ThrowIfNonPositive(maxArrayElements, nameof(maxArrayElements));
        ThrowIfNonPositive(maxFields, nameof(maxFields));
        ThrowIfNonPositive(maxBuilderBytes, nameof(maxBuilderBytes));
        ThrowIfNonPositive(maxRegexCaptureBytes, nameof(maxRegexCaptureBytes));
        ThrowIfNonPositive(maxVmStackBytes, nameof(maxVmStackBytes));
        ThrowIfNonPositive(maxExecutionMemoryBytes, nameof(maxExecutionMemoryBytes));
        ThrowIfNonPositive(maxOutputFiles, nameof(maxOutputFiles));

        MaxExecutionSteps = maxExecutionSteps;
        MaxFunctionCallDepth = maxFunctionCallDepth;
        MaxHostCalls = maxHostCalls;
        MaxRecordBytes = maxRecordBytes;
        MaxValueBytes = maxValueBytes;
        MaxOutputBytes = maxOutputBytes;
        MaxArrayElements = maxArrayElements;
        MaxFields = maxFields;
        MaxBuilderBytes = maxBuilderBytes;
        MaxRegexCaptureBytes = maxRegexCaptureBytes;
        MaxVmStackBytes = maxVmStackBytes;
        MaxExecutionMemoryBytes = maxExecutionMemoryBytes;
        MaxOutputFiles = maxOutputFiles;
    }

    public int MaxExecutionSteps { get; }

    public int MaxFunctionCallDepth { get; }

    public long MaxHostCalls { get; }

    public long MaxRecordBytes { get; }

    public long MaxValueBytes { get; }

    public long MaxOutputBytes { get; }

    public int MaxArrayElements { get; }

    public int MaxFields { get; }

    public int MaxBuilderBytes { get; }

    public long MaxRegexCaptureBytes { get; }

    public long MaxVmStackBytes { get; }

    public long MaxExecutionMemoryBytes { get; }

    public int MaxOutputFiles { get; }

    public static AwkRuntimeLimits CreateDefault() =>
        new(
            DefaultMaxExecutionSteps,
            DefaultMaxFunctionCallDepth,
            Unlimited,
            Unlimited,
            Unlimited,
            Unlimited,
            DefaultMaxArrayElements,
            DefaultMaxFields,
            DefaultMaxBuilderBytes,
            Unlimited,
            Unlimited,
            Unlimited,
            DefaultMaxOutputFiles);

    public AwkRuntimeLimits WithMaxExecutionSteps(int value) =>
        new(
            value,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxFunctionCallDepth(int value) =>
        new(
            MaxExecutionSteps,
            value,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxHostCalls(long value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            value,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxRecordBytes(long value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            value,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxValueBytes(long value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            value,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxOutputBytes(long value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            value,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxArrayElements(int value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            value,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxFields(int value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            value,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxBuilderBytes(int value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            value,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxRegexCaptureBytes(long value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            value,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxVmStackBytes(long value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            value,
            MaxExecutionMemoryBytes,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxExecutionMemoryBytes(long value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            value,
            MaxOutputFiles);

    public AwkRuntimeLimits WithMaxOutputFiles(int value) =>
        new(
            MaxExecutionSteps,
            MaxFunctionCallDepth,
            MaxHostCalls,
            MaxRecordBytes,
            MaxValueBytes,
            MaxOutputBytes,
            MaxArrayElements,
            MaxFields,
            MaxBuilderBytes,
            MaxRegexCaptureBytes,
            MaxVmStackBytes,
            MaxExecutionMemoryBytes,
            value);

    private static void ThrowIfNonPositive(long value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
