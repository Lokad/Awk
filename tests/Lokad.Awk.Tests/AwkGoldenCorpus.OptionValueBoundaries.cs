namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> OptionValueBoundaryCases()
    {
        const string inputProgram = "BEGIN{print \"argc\",ARGC;for(i=1;i<ARGC;i++)print i,\"[\"ARGV[i]\"]\"}{print FILENAME,\"[\"$0\"]\"}";
        const string lintProgram = "BEGIN{print \"LINT\"}";
        AwkGoldenFile[] files =
        [
            File("/--", "DASH\n"), File("/normal", "NORMAL\n"), File("/-data.txt", "LEADING\n"),
            File("/program.awk", inputProgram),
            File("/selected.awk", "BEGIN{print \"INTENDED\";exit}"),
            File("/data", "BEGIN{print \"DATA_EXECUTED\";exit}"),
            File("/include.awk", "function extra(){return \"INCLUDED\"}")
        ];
        (string Name, string[] Arguments, string Output)[] cases =
        [
            ("single-after", [inputProgram, "--", "normal"], "argc 3\n1 [--]\n2 [normal]\n-- [DASH]\nnormal [NORMAL]\n"),
            ("double-after", [inputProgram, "--", "--"], "argc 3\n1 [--]\n2 [--]\n-- [DASH]\n-- [DASH]\n"),
            ("after-file-double", [inputProgram, "normal", "--", "--"], "argc 4\n1 [normal]\n2 [--]\n3 [--]\nnormal [NORMAL]\n-- [DASH]\n-- [DASH]\n"),
            ("before-script-literal", ["--", inputProgram, "--"], "argc 2\n1 [--]\n-- [DASH]\n"),
            ("before-script-two-literals", ["--", inputProgram, "--", "--"], "argc 3\n1 [--]\n2 [--]\n-- [DASH]\n-- [DASH]\n"),
            ("dot-literal", [inputProgram, "./--"], "argc 2\n1 [./--]\n./-- [DASH]\n"),
            ("leading-dash", [inputProgram, "--", "-data.txt"], "argc 3\n1 [--]\n2 [-data.txt]\n-- [DASH]\n-data.txt [LEADING]\n"),
            ("e-double", ["-e", inputProgram, "--", "--"], "argc 2\n1 [--]\n-- [DASH]\n"),
            ("f-double", ["-f", "program.awk", "--", "--"], "argc 2\n1 [--]\n-- [DASH]\n"),
            ("f-attached-no-data", ["-fselected.awk"], "INTENDED\n"),
            ("f-attached-data", ["-fselected.awk", "data"], "INTENDED\n"),
            ("lint-short", ["-L", lintProgram], "LINT\n"),
            ("lint-attached", ["-Linvalid", lintProgram], "LINT\n"),
            ("lint-long", ["--lint", lintProgram], "LINT\n"),
            ("grouped-f", ["-sfselected.awk", "data"], "INTENDED\n"),
            ("include-attached", ["-iinclude.awk", "BEGIN{print extra()}"], "INCLUDED\n"),
            ("include-separated", ["-i", "include.awk", "BEGIN{print extra()}"], "INCLUDED\n"),
            ("exec-separated", ["-E", "program.awk", "--"], "argc 2\n1 [--]\n-- [DASH]\n"),
            ("exec-attached", ["-Eprogram.awk", "--", "normal"], "argc 3\n1 [--]\n2 [normal]\n-- [DASH]\nnormal [NORMAL]\n"),
            ("assignment-order", ["BEGIN{print ARGC}{print FILENAME,$0,v}END{print \"v\",v}", "--", "v=7"], "3\n-- DASH \nv 7\n"),
            ("exit-unopened", ["BEGIN{print ARGC;exit}", "--", "missing"], "3\n"),
        ];
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        foreach (var test in cases)
        {
            string[] arguments = profile.Length == 0 ? test.Arguments : [profile, .. test.Arguments];
            yield return AwkGoldenCaseBuilder.Create("option-boundary-" + test.Name + "-" + (profile.Length == 0 ? "default" : profile[2..]), arguments, files)
                .WithStandardInput("STDIN\n").WithStdOut(test.Output).Build();
        }
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        {
            string[] arguments = profile.Length == 0 ? ["--", "{print}", "--"] : [profile, "--", "{print}", "--"];
            yield return AwkGoldenCaseBuilder.Create("option-boundary-missing-literal-" + (profile.Length == 0 ? "default" : profile[2..]), arguments, [])
                .WithStandardInput("STDIN\n").ExitsWith(2).WithStdErr("awk: --: No such file or directory\n").Build();
        }
    }
}
