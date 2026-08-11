using System;

namespace Lokad.Awk;

/// <summary>Controls capabilities and resource budgets for one awk execution.</summary>
public sealed record AwkExecutionPolicy
{
    /// <summary>Represents an effectively unlimited numeric budget.</summary>
    public const long Unlimited = long.MaxValue;

    private AwkExecutionPolicy()
    {
    }

    /// <summary>Gets whether awk file redirections are allowed.</summary>
    public bool AllowFileRedirections { get; private init; } = true;

    /// <summary>Gets whether awk input and output command pipes are allowed.</summary>
    public bool AllowCommandPipes { get; private init; } = true;

    /// <summary>Gets the optional lexical root for every host-opened path.</summary>
    public AwkPath? AccessiblePathRoot { get; private init; }

    /// <summary>Gets the maximum number of interpreted execution steps.</summary>
    public int MaxExecutionSteps { get; private init; } = 10_000_000;

    /// <summary>Gets the maximum active awk function-call depth.</summary>
    public int MaxFunctionCallDepth { get; private init; } = 64;

    /// <summary>Gets the maximum frontend and expression-tree nesting depth.</summary>
    public int MaxSyntaxNestingDepth { get; private init; } = 128;

    /// <summary>Gets the maximum number of host calls.</summary>
    public long MaxHostCalls { get; private init; } = Unlimited;

    /// <summary>Gets the maximum size of one input record in UTF-8 bytes.</summary>
    public long MaxRecordBytes { get; private init; } = Unlimited;

    /// <summary>Gets the maximum size of one runtime value in UTF-8 bytes.</summary>
    public long MaxValueBytes { get; private init; } = Unlimited;

    /// <summary>Gets the maximum total number of bytes appended to host descriptors.</summary>
    public long MaxOutputBytes { get; private init; } = Unlimited;

    /// <summary>Gets the maximum element count of each awk array.</summary>
    public int MaxArrayElements { get; private init; } = 1_000_000;

    /// <summary>Gets the maximum number of record fields.</summary>
    public int MaxFields { get; private init; } = 1_000_000;

    /// <summary>Gets the maximum size of a governed runtime builder.</summary>
    public int MaxBuilderBytes { get; private init; } = 64 << 20;

    /// <summary>Gets the maximum retained bytes in one regex capture result.</summary>
    public long MaxRegexCaptureBytes { get; private init; } = Unlimited;

    /// <summary>Gets the maximum bytes scanned by regex record separators.</summary>
    public long MaxRegexRecordSeparatorBytesScanned { get; private init; } = 64L << 20;

    /// <summary>Gets the maximum executable-engine stack size in bytes.</summary>
    public long MaxVmStackBytes { get; private init; } = Unlimited;

    /// <summary>Gets the maximum estimated aggregate retained execution memory.</summary>
    public long MaxExecutionMemoryBytes { get; private init; } = Unlimited;

    /// <summary>Gets the maximum concurrently open output redirections.</summary>
    public int MaxOutputFiles { get; private init; } = 10_000;

    /// <summary>Gets the maximum concurrently open input-file redirections.</summary>
    public int MaxInputFiles { get; private init; } = 10_000;

    /// <summary>Gets the maximum concurrently open command pipes.</summary>
    public int MaxPipes { get; private init; } = 1_024;

    /// <summary>Gets the maximum aggregate UTF-8 bytes loaded as awk source.</summary>
    public long MaxSourceBytes { get; private init; } = 64L << 20;

    /// <summary>Gets the maximum nested <c>@include</c> depth.</summary>
    public int MaxIncludeDepth { get; private init; } = 128;

    /// <summary>Gets the maximum number of distinct <c>@include</c> directives followed.</summary>
    public int MaxIncludeFiles { get; private init; } = 1_024;

    /// <summary>Creates the compatibility-oriented default policy.</summary>
    public static AwkExecutionPolicy CreateDefault() => new();

    /// <summary>Creates a finite deny-by-default policy suitable for untrusted awk.</summary>
    public static AwkExecutionPolicy CreateRestricted() =>
        new()
        {
            AllowFileRedirections = false,
            AllowCommandPipes = false,
            MaxExecutionSteps = 1_000_000,
            MaxFunctionCallDepth = 32,
            MaxSyntaxNestingDepth = 64,
            MaxHostCalls = 100_000,
            MaxRecordBytes = 8L << 20,
            MaxValueBytes = 8L << 20,
            MaxOutputBytes = 16L << 20,
            MaxArrayElements = 100_000,
            MaxFields = 100_000,
            MaxBuilderBytes = 8 << 20,
            MaxRegexCaptureBytes = 8L << 20,
            MaxRegexRecordSeparatorBytesScanned = 16L << 20,
            MaxVmStackBytes = 8L << 20,
            MaxExecutionMemoryBytes = 64L << 20,
            MaxOutputFiles = 64,
            MaxInputFiles = 64,
            MaxPipes = 8,
            MaxSourceBytes = 4L << 20,
            MaxIncludeDepth = 16,
            MaxIncludeFiles = 64
        };

    /// <summary>Returns a copy with file-redirection capability configured.</summary>
    public AwkExecutionPolicy WithFileRedirections(bool allowed) =>
        this with { AllowFileRedirections = allowed };

    /// <summary>Returns a copy with command-pipe capability configured.</summary>
    public AwkExecutionPolicy WithCommandPipes(bool allowed) =>
        this with { AllowCommandPipes = allowed };

    /// <summary>Returns a copy that restricts host-opened paths to the supplied lexical root.</summary>
    public AwkExecutionPolicy WithAccessiblePathRoot(AwkPath root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return this with { AccessiblePathRoot = root };
    }

    /// <summary>Returns a copy with the execution-step budget configured.</summary>
    public AwkExecutionPolicy WithMaxExecutionSteps(int value) =>
        this with { MaxExecutionSteps = RequirePositive(value) };

    /// <summary>Returns a copy with the awk function-call depth configured.</summary>
    public AwkExecutionPolicy WithMaxFunctionCallDepth(int value) =>
        this with { MaxFunctionCallDepth = RequirePositive(value) };

    /// <summary>Returns a copy with the frontend nesting depth configured.</summary>
    public AwkExecutionPolicy WithMaxSyntaxNestingDepth(int value) =>
        this with { MaxSyntaxNestingDepth = RequirePositive(value) };

    /// <summary>Returns a copy with the host-call budget configured.</summary>
    public AwkExecutionPolicy WithMaxHostCalls(long value) =>
        this with { MaxHostCalls = RequirePositive(value) };

    /// <summary>Returns a copy with the per-record byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxRecordBytes(long value) =>
        this with { MaxRecordBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the per-value byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxValueBytes(long value) =>
        this with { MaxValueBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the total output-byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxOutputBytes(long value) =>
        this with { MaxOutputBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the per-array element budget configured.</summary>
    public AwkExecutionPolicy WithMaxArrayElements(int value) =>
        this with { MaxArrayElements = RequirePositive(value) };

    /// <summary>Returns a copy with the field-count budget configured.</summary>
    public AwkExecutionPolicy WithMaxFields(int value) =>
        this with { MaxFields = RequirePositive(value) };

    /// <summary>Returns a copy with the governed-builder byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxBuilderBytes(int value) =>
        this with { MaxBuilderBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the regex-capture byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxRegexCaptureBytes(long value) =>
        this with { MaxRegexCaptureBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the regex record-separator scan budget configured.</summary>
    public AwkExecutionPolicy WithMaxRegexRecordSeparatorBytesScanned(long value) =>
        this with { MaxRegexRecordSeparatorBytesScanned = RequirePositive(value) };

    /// <summary>Returns a copy with the executable stack-byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxVmStackBytes(long value) =>
        this with { MaxVmStackBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the aggregate retained-memory budget configured.</summary>
    public AwkExecutionPolicy WithMaxExecutionMemoryBytes(long value) =>
        this with { MaxExecutionMemoryBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the open output-file budget configured.</summary>
    public AwkExecutionPolicy WithMaxOutputFiles(int value) =>
        this with { MaxOutputFiles = RequirePositive(value) };

    /// <summary>Returns a copy with the open input-file budget configured.</summary>
    public AwkExecutionPolicy WithMaxInputFiles(int value) =>
        this with { MaxInputFiles = RequirePositive(value) };

    /// <summary>Returns a copy with the open command-pipe budget configured.</summary>
    public AwkExecutionPolicy WithMaxPipes(int value) =>
        this with { MaxPipes = RequirePositive(value) };

    /// <summary>Returns a copy with the aggregate source-byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxSourceBytes(long value) =>
        this with { MaxSourceBytes = RequirePositive(value) };

    /// <summary>Returns a copy with the include-depth budget configured.</summary>
    public AwkExecutionPolicy WithMaxIncludeDepth(int value) =>
        this with { MaxIncludeDepth = RequirePositive(value) };

    /// <summary>Returns a copy with the included-file budget configured.</summary>
    public AwkExecutionPolicy WithMaxIncludeFiles(int value) =>
        this with { MaxIncludeFiles = RequirePositive(value) };

    private static int RequirePositive(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        return value;
    }

    private static long RequirePositive(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        return value;
    }
}
