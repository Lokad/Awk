using System;

namespace Lokad.Awk;

internal sealed class AwkFrontendBudget
{

    public AwkFrontendBudget(
        int maxSyntaxNestingDepth,
        long maxRegexPatternBytes,
        int maxRegexGroups,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null)
    {
        MaxSyntaxNestingDepth = maxSyntaxNestingDepth;
        MaxRegexPatternBytes = maxRegexPatternBytes;
        MaxRegexGroups = maxRegexGroups;
        StringEscapeWarnings = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        RegexEscapeWarnings = regexEscapeWarnings ?? new AwkRegexEscapeWarnings();
    }

    public AwkStringEscapeWarnings StringEscapeWarnings { get; }

    public AwkRegexEscapeWarnings RegexEscapeWarnings { get; }
    public AwkFrontendMemory Memory { get; init; } = AwkFrontendMemory.Disabled;

    public int MaxSyntaxNestingDepth { get; }

    public long MaxRegexPatternBytes { get; }

    public int MaxRegexGroups { get; }

    public static AwkFrontendBudget Unlimited(int maxSyntaxNestingDepth) =>
        new(
            maxSyntaxNestingDepth, long.MaxValue, int.MaxValue);

    public static AwkFrontendBudget FromResources(AwkRuntimeResourceMonitor resources, AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null,
        AwkFrontendMemory? memory = null) =>
        new(
            resources.Limits.MaxSyntaxNestingDepth, resources.Limits.MaxRegexPatternBytes, resources.Limits.MaxRegexGroups, stringEscapeWarnings, regexEscapeWarnings)
        { Memory = memory ?? AwkFrontendMemory.Disabled };
}
