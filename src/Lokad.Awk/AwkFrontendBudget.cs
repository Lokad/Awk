using System;

namespace Lokad.Awk;

internal sealed class AwkFrontendBudget
{

    public AwkFrontendBudget(
        int maxSyntaxNestingDepth,
        int maxTokens,
        int maxDiagnostics,
        long maxRegexPatternBytes,
        int maxRegexGroups,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null)
    {
        MaxSyntaxNestingDepth = maxSyntaxNestingDepth;
        MaxTokens = maxTokens;
        MaxDiagnostics = maxDiagnostics;
        MaxRegexPatternBytes = maxRegexPatternBytes;
        MaxRegexGroups = maxRegexGroups;
        StringEscapeWarnings = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        RegexEscapeWarnings = regexEscapeWarnings ?? new AwkRegexEscapeWarnings();
    }

    public AwkStringEscapeWarnings StringEscapeWarnings { get; }

    public AwkRegexEscapeWarnings RegexEscapeWarnings { get; }
    public AwkFrontendMemory Memory { get; init; } = AwkFrontendMemory.Disabled;

    public int MaxSyntaxNestingDepth { get; }

    public int MaxTokens { get; }

    public int MaxDiagnostics { get; }

    public long MaxRegexPatternBytes { get; }

    public int MaxRegexGroups { get; }

    public static AwkFrontendBudget Unlimited(int maxSyntaxNestingDepth) =>
        new(
            maxSyntaxNestingDepth, int.MaxValue, int.MaxValue, long.MaxValue, int.MaxValue);

    public static AwkFrontendBudget FromResources(AwkRuntimeResourceMonitor resources, int previousDiagnostics = 0,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null,
        AwkFrontendMemory? memory = null) =>
        new(
            resources.Limits.MaxSyntaxNestingDepth, resources.Limits.MaxFrontendTokens, Math.Max(0, resources.Limits.MaxFrontendDiagnostics - previousDiagnostics), resources.Limits.MaxRegexPatternBytes, resources.Limits.MaxRegexGroups, stringEscapeWarnings, regexEscapeWarnings)
        { Memory = memory ?? AwkFrontendMemory.Disabled };
}
