namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> AutomaticInputCases()
    {
        AwkGoldenFile[] files =
        [
            File("/data.txt", "a\nb\n"),
            File("/program.awk", "BEGIN{print \"B\"}")
        ];
        (string Name, string[] Arguments, string Output, string Error)[] cases =
        [
            ("begin", ["BEGIN{print \"B\"}", "missing.txt"], "B\n", ""),
            ("unused-invalid-path", ["BEGIN{print \"B\"}", "../../secret"], "B\n", ""),
            ("begin-empty", ["BEGIN{}", "missing.txt"], "", ""),
            ("begin-function", ["function f(){return \"B\"}BEGIN{print f()}", "missing.txt"], "B\n", ""),
            ("functions", ["function f(){}", "missing.txt"], "", ""),
            ("comments", ["# comment", "missing.txt"], "", ""),
            ("empty", ["", "missing.txt"], "", ""),
            ("argv-data", ["BEGIN{for(i=1;i<ARGC;i++)print ARGV[i]}", "payload"], "payload\n", ""),
            ("source-file", ["-f", "program.awk", "missing.txt"], "B\n", ""),
            ("getline", ["BEGIN{getline x;print x,NR}", "data.txt", "missing.txt"], "a 1\n", ""),
            ("exit-end", ["BEGIN{exit}END{print NR}", "missing.txt"], "0\n", ""),
            ("end", ["END{print NR}", "data.txt"], "2\n", ""),
            ("begin-end", ["BEGIN{print \"B\"}END{print NR}", "data.txt"], "B\n2\n", ""),
            ("end-empty", ["END{}", "missing.txt"], "", "awk: missing.txt: No such file or directory\n"),
            ("main-empty", ["{}", "missing.txt"], "", "awk: missing.txt: No such file or directory\n"),
            ("main-false", ["0", "missing.txt"], "", "awk: missing.txt: No such file or directory\n")
        ];
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        foreach (var test in cases)
            yield return AwkGoldenCaseBuilder.Create(
                    "automatic-input-" + test.Name + "-" + (profile.Length == 0 ? "default" : profile[2..]),
                    profile.Length == 0 ? test.Arguments : [profile, .. test.Arguments], files)
                .WithStandardInput("unused\n").WithStdOut(test.Output).WithStdErr(test.Error)
                .ExitsWith(test.Error.Length == 0 ? 0 : 2).Build();

        foreach (var test in new[]
        {
            (Name: "beginfile", Program: "BEGINFILE{print FILENAME}", Output: "data.txt\n"),
            (Name: "endfile", Program: "ENDFILE{print FNR}", Output: "2\n"),
            (Name: "file-hooks", Program: "BEGINFILE{}ENDFILE{print FNR}", Output: "2\n")
        })
            yield return AwkGoldenCaseBuilder.Create("automatic-input-" + test.Name,
                    [test.Program, "data.txt"], files).WithStdOut(test.Output).Build();
        yield return AwkGoldenCaseBuilder.Create("automatic-input-include",
                ["@include \"program.awk\"", "missing.txt"], files).WithStdOut("B\n").Build();
    }
}
