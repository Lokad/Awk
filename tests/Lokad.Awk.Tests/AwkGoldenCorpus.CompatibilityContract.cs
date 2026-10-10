namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> CompatibilityContractCases() =>
    [
        AwkGoldenCaseBuilder.Create(
                "main-directory-getline-preserves-output",
                ["BEGIN{print \"before\";code=getline;print \"after\",code}", "/directory", "/data"],
                [File("/data", "later\n")])
            .WithInputDirectories(["/directory"])
            .WithStdOut("before\n")
            .WithStdErr("awk: /directory: Is a directory\n")
            .ExitsWith(2)
            .WithNondeterminismNote("Intentional host-contract difference: default GNU awk skips main-input directories; Lokad treats host open failures as fatal.")
            .Build(),
        AwkGoldenCaseBuilder.Create(
                "main-directory-does-not-fall-back-to-stdin",
                ["BEGIN{print \"before\"}{print \"record\",$0}", "/directory"], [])
            .WithInputDirectories(["/directory"])
            .WithStandardInput("stdin record\n")
            .WithStdOut("before\n")
            .WithStdErr("awk: /directory: Is a directory\n")
            .ExitsWith(2)
            .WithNondeterminismNote("Intentional host-contract difference: a failed named main input does not cause stdin fallback.")
            .Build(),
        AwkGoldenCaseBuilder.Create("csv-short-option-rejected", ["-k", "BEGIN{print \"unreachable\"}"], [])
            .WithStdErr("awk: invalid option -- -k\n")
            .ExitsWith(2)
            .WithNondeterminismNote("CSV mode is deferred; this case asserts option rejection, not CSV parsing.")
            .Build(),
        AwkGoldenCaseBuilder.Create("csv-long-option-rejected", ["--csv", "BEGIN{print \"unreachable\"}"], [])
            .WithStdErr("awk: invalid option -- --csv\n")
            .ExitsWith(2)
            .WithNondeterminismNote("CSV mode is deferred; this case asserts option rejection, not CSV parsing.")
            .Build()
    ];
}
