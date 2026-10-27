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

    public IAwkRegexEngine? RegexEngine { get; init; }
    public CancellationToken RegexCancellationToken { get; init; }

    public int MaxSyntaxNestingDepth { get; }

}
