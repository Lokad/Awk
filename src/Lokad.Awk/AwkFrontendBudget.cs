using System;

namespace Lokad.Awk;

internal sealed class AwkFrontendBudget
{
    private readonly Action _reserveRegexLiteralCompilations;

    public AwkFrontendBudget(
        int maxSyntaxNestingDepth,
        int maxTokens,
        int maxDiagnostics,
        long maxRegexPatternBytes,
        int maxRegexGroups,
        Action reserveRegexLiteralCompilations)
    {
        MaxSyntaxNestingDepth = maxSyntaxNestingDepth;
        MaxTokens = maxTokens;
        MaxDiagnostics = maxDiagnostics;
        MaxRegexPatternBytes = maxRegexPatternBytes;
        MaxRegexGroups = maxRegexGroups;
        _reserveRegexLiteralCompilations = reserveRegexLiteralCompilations;
    }

    public int MaxSyntaxNestingDepth { get; }

    public int MaxTokens { get; }

    public int MaxDiagnostics { get; }

    public long MaxRegexPatternBytes { get; }

    public int MaxRegexGroups { get; }

    public void ReserveRegexLiteralCompilations() => _reserveRegexLiteralCompilations();

    public static AwkFrontendBudget Unlimited(int maxSyntaxNestingDepth) =>
        new(
            maxSyntaxNestingDepth,
            int.MaxValue,
            int.MaxValue,
            long.MaxValue,
            int.MaxValue,
            static () => { });

    public static AwkFrontendBudget FromResources(AwkRuntimeResourceMonitor resources) =>
        new(
            resources.Limits.MaxSyntaxNestingDepth,
            resources.Limits.MaxFrontendTokens,
            resources.Limits.MaxFrontendDiagnostics,
            resources.Limits.MaxRegexPatternBytes,
            resources.Limits.MaxRegexGroups,
            () => resources.ReserveRegexCompilations(2));
}
