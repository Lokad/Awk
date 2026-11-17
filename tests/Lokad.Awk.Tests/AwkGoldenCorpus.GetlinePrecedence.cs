namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> GetlinePrecedenceCases()
    {
        foreach (var test in GetlineBareLengthCases()) yield return test;
        yield return CompilerDiagnostic(Failing("getline-nested-source-invalid-function-before-effects",
            ["BEGIN{print \"unreached\";getline x < (getline x < (getline sin))}"], [], 2, "",
            "awk: function `sin' requires a call.\n"));
        yield return CompilerDiagnostic(Failing("getline-expression-nested-source-invalid-function-before-effects",
            ["BEGIN{print \"unreached\";print (getline x < (getline x < (getline sin)))}"], [], 2, "",
            "awk: function `sin' requires a call.\n"));
        yield return Passing("getline-statement-result-concatenation",
            ["BEGIN{getline x y}"], [], "");
        yield return Passing("getline-statement-numeric-result-concatenation",
            ["BEGIN{getline 1}"], [], "");
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

    private static IEnumerable<AwkGoldenCase> GetlineBareLengthCases()
    {
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-statement",
            ["BEGIN{getline length;print NR,NF,$0}"], [])
            .WithStandardInput("abc\n").WithStdOut("1 1 abc\n")
            .WithTags([AwkGoldenTag.Idiom]).Build();
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-expression",
            ["BEGIN{print (getline length),NR,NF,$0}"], [])
            .WithStandardInput("abc\n").WithStdOut("13 1 1 abc\n")
            .WithTags([AwkGoldenTag.Idiom]).Build();
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-statement-comparison",
            ["BEGIN{getline length < \"data\";print NR,NF,$0}"], [])
            .WithStandardInput("abc\n").WithStdOut("1 1 abc\n")
            .WithTags([AwkGoldenTag.Idiom]).Build();
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-expression-comparison",
            ["BEGIN{print (getline length < \"data\"),NR,NF,$0}"], [])
            .WithStandardInput("abc\n").WithStdOut("1 1 1 abc\n")
            .WithTags([AwkGoldenTag.Idiom]).Build();
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-explicit-source",
            ["BEGIN{print (getline < \"data\") length,NR,NF,$0}"], [File("/data", "longer record\n")])
            .WithStandardInput("abc\n").WithStdOut("113 0 2 longer record\n")
            .WithTags([AwkGoldenTag.Idiom, AwkGoldenTag.Io]).Build();
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-missing",
            ["BEGIN{print (getline < \"absent\") length,(ERRNO!=\"\")}"], [])
            .WithStandardInput("abc\n").WithStdOut("-10 1\n")
            .WithTags([AwkGoldenTag.Idiom]).Build();
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-nested",
            ["BEGIN{print \"unreached\";print (getline x < (getline x < (getline length)))}"], [])
            .WithStandardInput("abc\n").WithStdOut("unreached\n-1\n")
            .WithTags([AwkGoldenTag.Idiom]).Build();
        yield return AwkGoldenCaseBuilder.Create("getline-bare-length-empty",
            ["BEGIN{print (getline length),NR,NF,$0}"], [])
            .WithStandardInput("").WithStdOut("00 0 0 \n")
            .WithTags([AwkGoldenTag.Idiom]).Build();
    }
}
