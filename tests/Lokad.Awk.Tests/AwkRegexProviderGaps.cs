namespace Lokad.Awk.Tests;

// Exact, reviewed integration gaps in the pinned published provider, not a runtime capability
// detector. New failures stay blocking. Preserve every expectation and remove entries only
// after qualifying a provider upgrade. Set SkipReason to null locally for full qualification.
internal static class AwkRegexProviderGaps
{
    public const string? SkipReason =
        "Published Lokad.Utf8Regex lacks required POSIX longest/subcapture selection. See tests/README.md.";

    public static bool AffectsExecution(AwkGoldenCase testCase) => ExecutionCases.Contains(testCase.Name);

    // POSIX whole-match and subcapture selection, including repeated/nullable groups.
    private static readonly HashSet<string> ExecutionCases = new(StringComparer.Ordinal)
    {
        "regex-longest-alternation-and-earliest-start",
        "regex-longest-byte-coordinate-fallback",
        "regex-longest-capture-priority-is-left-to-right",
        "regex-longest-competing-repetitions-without-alternation",
        "regex-longest-distinguishes-empty-and-absent-captures",
        "regex-longest-dynamic-ignorecase-is-not-stale",
        "regex-longest-field-pattern",
        "regex-longest-field-separator",
        "regex-longest-gensub-captures-and-occurrences",
        "regex-longest-intervals-and-nullable-alternatives",
        "regex-longest-patsplit-and-separator-array",
        "regex-longest-record-separator-and-rt",
        "regex-longest-repeated-captures-retain-last-participation",
        "regex-longest-split-and-separator-array",
        "regex-longest-sub-and-gsub",
        "regex-longest-supplementary-text-and-captures",
        "regex-posix-capture-selection-dynamic-case",
        "regex-posix-descendant-captures-reset-per-parent",
        "regex-posix-empty-capture-remains-distinct-from-absent",
        "regex-posix-finite-and-nested-capture-priority",
        "regex-posix-gensub-capture-selection",
        "regex-posix-nullable-capture-participation",
    };
}
