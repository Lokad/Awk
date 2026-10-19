using System;

namespace Lokad.Awk;

/// <summary>Controls capabilities and resource budgets for one awk execution.</summary>
public sealed record AwkExecutionPolicy
{
    /// <summary>Gets the borrowed application-supplied UTF-8 regex engine, or null for .NET compatibility.</summary>
    public IAwkRegexEngine? RegexEngine { get; private init; }

    /// <summary>Uses a borrowed regex engine while retaining Awk translation and match-selection rules.</summary>
    public AwkExecutionPolicy WithRegexEngine(IAwkRegexEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return this with { RegexEngine = engine };
    }

    /// <summary>Represents an effectively unlimited numeric budget.</summary>
    public const long Unlimited = long.MaxValue;

    /// <summary>Gets the borrowed cooperative host budget, if supplied.</summary>
    public IMemoryBudget? MemoryBudget { get; private init; }

    /// <summary>Uses a borrowed shared allowance; the library owns only its child scopes.</summary>
    /// <remarks>A budget carried by the command takes precedence, preserving nested command ownership.</remarks>
    public AwkExecutionPolicy WithMemoryBudget(IMemoryBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        return this with { MemoryBudget = budget };
    }

    internal AwkRuntimeLimits Limits { get; private init; }

    private AwkExecutionPolicy(AwkRuntimeLimits limits)
    {
        Limits = limits;
    }

    /// <summary>Gets whether awk file redirections are allowed.</summary>
    public bool AllowFileRedirections { get; private init; } = true;

    /// <summary>Gets whether awk input and output command pipes are allowed.</summary>
    public bool AllowCommandPipes { get; private init; } = true;

    /// <summary>Gets whether source text may load additional files through <c>@include</c>.</summary>
    public bool AllowSourceIncludes { get; private init; } = true;

    /// <summary>Gets whether awk may replace command-line main-input paths through <c>ARGV</c>.</summary>
    public bool AllowDynamicMainInputPaths { get; private init; } = true;

    /// <summary>Gets whether the mutable awk <c>ENVIRON</c> snapshot is forwarded to pipe commands.</summary>
    public bool ForwardCommandEnvironment { get; private init; } = true;

    /// <summary>Gets whether invocation environment variables are exposed through <c>ENVIRON</c> and <c>AWKPATH</c>.</summary>
    public bool ExposeEnvironment { get; private init; } = true;

    /// <summary>Gets whether structured host error details may be included in awk diagnostics.</summary>
    public bool ExposeHostErrorDetails { get; private init; } = true;

    /// <summary>Gets the optional lexical root for every host-opened path.</summary>
    public AwkPath? AccessiblePathRoot { get; private init; }

    /// <summary>Gets the time provider observed by time-related awk builtins.</summary>
    public TimeProvider TimeProvider { get; private init; } = TimeProvider.System;

    /// <summary>Gets the local time zone observed by time-related awk builtins.</summary>
    public TimeZoneInfo TimeZone { get; private init; } = TimeZoneInfo.Local;

    /// <summary>Gets the maximum active awk function-call depth.</summary>
    public int MaxFunctionCallDepth => Limits.MaxFunctionCallDepth;

    /// <summary>Gets the maximum frontend and expression-tree nesting depth.</summary>
    public int MaxSyntaxNestingDepth => Limits.MaxSyntaxNestingDepth;

    /// <summary>Gets the maximum UTF-8 byte length of a source or translated regex pattern.</summary>
    public long MaxRegexPatternBytes => Limits.MaxRegexPatternBytes;

    /// <summary>Gets the maximum number of group constructs in one regex pattern.</summary>
    public int MaxRegexGroups => Limits.MaxRegexGroups;

    /// <summary>Gets the maximum estimated bytes retained across regex caches.</summary>
    public long MaxRegexCacheBytes => Limits.MaxRegexCacheBytes;

    /// <summary>Gets the maximum concurrently open output redirections.</summary>
    public int MaxOutputFiles => Limits.MaxOutputFiles;

    /// <summary>Gets the maximum concurrently open input-file redirections.</summary>
    public int MaxInputFiles => Limits.MaxInputFiles;

    /// <summary>Gets the maximum concurrently open command pipes.</summary>
    public int MaxPipes => Limits.MaxPipes;

    /// <summary>Gets the maximum aggregate UTF-8 bytes loaded as awk source.</summary>
    public long MaxSourceBytes => Limits.MaxSourceBytes;

    /// <summary>Gets the maximum number of top-level source units in one invocation.</summary>
    public int MaxSourceUnits => Limits.MaxSourceUnits;

    /// <summary>Gets the maximum UTF-8 bytes in the fully expanded compilation source.</summary>
    public long MaxExpandedSourceBytes => Limits.MaxExpandedSourceBytes;

    /// <summary>Gets the maximum UTF-16 characters admitted to the frontend.</summary>
    public int MaxFrontendCharacters => Limits.MaxFrontendCharacters;

    /// <summary>Gets the maximum token count produced by the frontend.</summary>
    public int MaxFrontendTokens => Limits.MaxFrontendTokens;

    /// <summary>Gets the maximum lexical diagnostics produced by the frontend.</summary>
    public int MaxFrontendDiagnostics => Limits.MaxFrontendDiagnostics;

    /// <summary>Gets the maximum command argument count admitted before invocation planning.</summary>
    public int MaxArguments { get; private init; } = 100_000;

    /// <summary>Gets the maximum aggregate UTF-8 bytes across command arguments.</summary>
    public long MaxArgumentBytes { get; private init; } = 64L << 20;

    /// <summary>Gets the maximum environment entry count admitted before invocation planning.</summary>
    public int MaxEnvironmentEntries { get; private init; } = 100_000;

    /// <summary>Gets the maximum aggregate UTF-8 bytes across environment names and values.</summary>
    public long MaxEnvironmentBytes { get; private init; } = 64L << 20;

    /// <summary>Gets the maximum UTF-8 byte length of a host error admitted to diagnostics.</summary>
    public int MaxHostErrorBytes { get; private init; } = 4 << 10;

    /// <summary>Gets the maximum nested <c>@include</c> depth.</summary>
    public int MaxIncludeDepth => Limits.MaxIncludeDepth;

    /// <summary>Gets the maximum number of distinct <c>@include</c> directives followed.</summary>
    public int MaxIncludeFiles => Limits.MaxIncludeFiles;

    /// <summary>Creates the compatibility-oriented default policy.</summary>
    public static AwkExecutionPolicy CreateDefault() => new(AwkRuntimeLimits.CreateDefault());

    /// <summary>Creates a finite deny-by-default policy suitable for untrusted awk.</summary>
    public static AwkExecutionPolicy CreateRestricted() =>
        new(AwkRuntimeLimits.CreateRestricted())
        {
            AllowFileRedirections = false,
            AllowCommandPipes = false,
            AllowSourceIncludes = false,
            AllowDynamicMainInputPaths = false,
            ForwardCommandEnvironment = false,
            ExposeEnvironment = false,
            ExposeHostErrorDetails = false,
            TimeProvider = AwkFixedTimeProvider.UnixEpoch,
            TimeZone = TimeZoneInfo.Utc,
            MaxArguments = 1_024,
            MaxArgumentBytes = 4L << 20,
            MaxEnvironmentEntries = 1_024,
            MaxEnvironmentBytes = 1L << 20,
            MaxHostErrorBytes = 1 << 10
        };

    /// <summary>Returns a copy with file-redirection capability configured.</summary>
    public AwkExecutionPolicy WithFileRedirections(bool allowed) =>
        this with { AllowFileRedirections = allowed };

    /// <summary>Returns a copy with command-pipe capability configured.</summary>
    public AwkExecutionPolicy WithCommandPipes(bool allowed) =>
        this with { AllowCommandPipes = allowed };

    /// <summary>Returns a copy with source-include capability configured.</summary>
    public AwkExecutionPolicy WithSourceIncludes(bool allowed) =>
        this with { AllowSourceIncludes = allowed };

    /// <summary>Returns a copy with dynamic main-input path replacement configured.</summary>
    public AwkExecutionPolicy WithDynamicMainInputPaths(bool allowed) =>
        this with { AllowDynamicMainInputPaths = allowed };

    /// <summary>Returns a copy with command-environment forwarding configured.</summary>
    public AwkExecutionPolicy WithCommandEnvironment(bool forward) =>
        this with { ForwardCommandEnvironment = forward };

    /// <summary>Returns a copy with invocation-environment exposure configured.</summary>
    public AwkExecutionPolicy WithEnvironmentExposure(bool expose) =>
        this with { ExposeEnvironment = expose };

    /// <summary>Returns a copy with structured host-error detail exposure configured.</summary>
    public AwkExecutionPolicy WithHostErrorDetails(bool expose) =>
        this with { ExposeHostErrorDetails = expose };

    /// <summary>Returns a copy that restricts host-opened paths to the supplied lexical root.</summary>
    public AwkExecutionPolicy WithAccessiblePathRoot(AwkPath root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return this with { AccessiblePathRoot = root };
    }

    /// <summary>Returns a copy with the time provider used by awk builtins.</summary>
    public AwkExecutionPolicy WithTimeProvider(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return this with { TimeProvider = timeProvider };
    }

    /// <summary>Returns a copy with the local time zone used by awk builtins.</summary>
    public AwkExecutionPolicy WithTimeZone(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        return this with { TimeZone = timeZone };
    }

    /// <summary>Returns a copy with the awk function-call depth configured.</summary>
    public AwkExecutionPolicy WithMaxFunctionCallDepth(int value) =>
        this with { Limits = Limits.WithMaxFunctionCallDepth(value) };

    /// <summary>Returns a copy with the frontend nesting depth configured.</summary>
    public AwkExecutionPolicy WithMaxSyntaxNestingDepth(int value) =>
        this with { Limits = Limits.WithMaxSyntaxNestingDepth(value) };

    /// <summary>Returns a copy with the regex pattern-byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxRegexPatternBytes(long value) =>
        this with { Limits = Limits.WithMaxRegexPatternBytes(value) };

    /// <summary>Returns a copy with the per-pattern regex group budget configured.</summary>
    public AwkExecutionPolicy WithMaxRegexGroups(int value) =>
        this with { Limits = Limits.WithMaxRegexGroups(value) };

    /// <summary>Returns a copy with the retained regex-cache byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxRegexCacheBytes(long value) =>
        this with { Limits = Limits.WithMaxRegexCacheBytes(value) };

    /// <summary>Returns a copy with the open output-file budget configured.</summary>
    public AwkExecutionPolicy WithMaxOutputFiles(int value) =>
        this with { Limits = Limits.WithMaxOutputFiles(value) };

    /// <summary>Returns a copy with the open input-file budget configured.</summary>
    public AwkExecutionPolicy WithMaxInputFiles(int value) =>
        this with { Limits = Limits.WithMaxInputFiles(value) };

    /// <summary>Returns a copy with the open command-pipe budget configured.</summary>
    public AwkExecutionPolicy WithMaxPipes(int value) =>
        this with { Limits = Limits.WithMaxPipes(value) };

    /// <summary>Returns a copy with the aggregate source-byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxSourceBytes(long value) =>
        this with { Limits = Limits.WithMaxSourceBytes(value) };

    /// <summary>Returns a copy with the top-level source-unit budget configured.</summary>
    public AwkExecutionPolicy WithMaxSourceUnits(int value) =>
        this with { Limits = Limits.WithMaxSourceUnits(value) };

    /// <summary>Returns a copy with the expanded compilation-source byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxExpandedSourceBytes(long value) =>
        this with { Limits = Limits.WithMaxExpandedSourceBytes(value) };

    /// <summary>Returns a copy with the frontend character budget configured.</summary>
    public AwkExecutionPolicy WithMaxFrontendCharacters(int value) =>
        this with { Limits = Limits.WithMaxFrontendCharacters(value) };

    /// <summary>Returns a copy with the frontend token budget configured.</summary>
    public AwkExecutionPolicy WithMaxFrontendTokens(int value) =>
        this with { Limits = Limits.WithMaxFrontendTokens(value) };

    /// <summary>Returns a copy with the frontend diagnostic budget configured.</summary>
    public AwkExecutionPolicy WithMaxFrontendDiagnostics(int value) =>
        this with { Limits = Limits.WithMaxFrontendDiagnostics(value) };

    /// <summary>Returns a copy with the command argument-count budget configured.</summary>
    public AwkExecutionPolicy WithMaxArguments(int value) =>
        this with { MaxArguments = AwkRuntimeLimits.RequirePositive(value) };

    /// <summary>Returns a copy with the aggregate command argument-byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxArgumentBytes(long value) =>
        this with { MaxArgumentBytes = AwkRuntimeLimits.RequirePositive(value) };

    /// <summary>Returns a copy with the environment entry-count budget configured.</summary>
    public AwkExecutionPolicy WithMaxEnvironmentEntries(int value) =>
        this with { MaxEnvironmentEntries = AwkRuntimeLimits.RequirePositive(value) };

    /// <summary>Returns a copy with the aggregate environment-byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxEnvironmentBytes(long value) =>
        this with { MaxEnvironmentBytes = AwkRuntimeLimits.RequirePositive(value) };

    /// <summary>Returns a copy with the host-error byte budget configured.</summary>
    public AwkExecutionPolicy WithMaxHostErrorBytes(int value) =>
        this with { MaxHostErrorBytes = AwkRuntimeLimits.RequirePositive(value) };

    /// <summary>Returns a copy with the include-depth budget configured.</summary>
    public AwkExecutionPolicy WithMaxIncludeDepth(int value) =>
        this with { Limits = Limits.WithMaxIncludeDepth(value) };

    /// <summary>Returns a copy with the included-file budget configured.</summary>
    public AwkExecutionPolicy WithMaxIncludeFiles(int value) =>
        this with { Limits = Limits.WithMaxIncludeFiles(value) };

}

internal sealed class AwkFixedTimeProvider : TimeProvider
{
    private AwkFixedTimeProvider()
    {
    }

    public static AwkFixedTimeProvider UnixEpoch { get; } = new();

    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
