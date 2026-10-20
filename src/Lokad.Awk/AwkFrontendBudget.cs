using System;

namespace Lokad.Awk;

internal sealed class AwkFrontendBudget
{

    public AwkFrontendBudget(
        int maxSyntaxNestingDepth,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null)
    {
        MaxSyntaxNestingDepth = maxSyntaxNestingDepth;
        StringEscapeWarnings = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        RegexEscapeWarnings = regexEscapeWarnings ?? new AwkRegexEscapeWarnings();
    }

    public AwkStringEscapeWarnings StringEscapeWarnings { get; }

    public AwkRegexEscapeWarnings RegexEscapeWarnings { get; }
    public AwkFrontendMemory Memory { get; init; } = AwkFrontendMemory.Disabled;

    public int MaxSyntaxNestingDepth { get; }

    public static AwkFrontendBudget FromResources(AwkRuntimeResourceMonitor resources, AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null,
        AwkFrontendMemory? memory = null) =>
        new(
            resources.Limits.MaxSyntaxNestingDepth, stringEscapeWarnings, regexEscapeWarnings)
        { Memory = memory ?? AwkFrontendMemory.Disabled };
}
