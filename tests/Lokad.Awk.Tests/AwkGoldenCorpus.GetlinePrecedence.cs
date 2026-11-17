namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> GetlinePrecedenceCases()
    {
        yield return Passing("getline-statement-result-concatenation",
            ["BEGIN{getline x y}"], [], "");
        var files = new[] { File("/a", "A\n"), File("/ab", "AB\n") };
        yield return Passing("getline-statement-concatenates-after-file-read",
            ["BEGIN{getline x < \"/a\" \"b\";print x}"], files, "A\n");
        yield return Passing("getline-statement-parenthesized-source-concatenation",
            ["BEGIN{getline x < (\"/a\" \"b\");print x}"], files, "AB\n");
        yield return Passing("getline-explicit-result-concatenation-control",
            ["BEGIN{(getline x < \"/a\") \"b\";print x}"], files, "A\n");
        yield return Passing("getline-statement-array-target-before-concatenation",
            ["BEGIN{getline a[1] < \"/a\" \"b\";print a[1]}"], files, "A\n");
        yield return Passing("getline-statement-field-target-before-concatenation",
            ["BEGIN{$0=\"seed\";getline $1 < \"/a\" \"b\";print $1,$0}"], files, "A A\n");
        yield return Passing("getline-statement-record-before-concatenation",
            ["BEGIN{getline < \"/a\" \"b\";print $0}"], files, "A\n");
        yield return Passing("getline-statement-completes-before-suffix-effects",
            ["function suffix(){print \"seen\",x;return \"b\"}BEGIN{getline x < \"/a\" suffix();print \"read\",x}"], files, "seen A\nread A\n");
        yield return Passing("getline-statement-logical-suffix",
            ["BEGIN{getline x < \"/a\" && x==\"A\";print x}"], files, "A\n");
        yield return Passing("getline-statement-concatenation-keeps-recoverable-failure",
            ["BEGIN{x=\"keep\";getline x < \"/missing\" \"b\";print x,(ERRNO!=\"\")}"],
            [File("/missingb", "wrong\n")], "keep 1\n");
    }
}
