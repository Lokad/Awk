namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> SourceSearchCases()
    {
        AwkGoldenFile[] files =
        [
            File("/common.awk", "BEGIN{print \"LOCAL\"}"),
            File("/lib/common.awk", "BEGIN{print \"LIB\"}"),
            File("/other/common.awk", "BEGIN{print \"OTHER\"}"),
            File("/lib/sub/common.awk", "BEGIN{print \"WRONG\"}"),
            File("/work/common.awk", "BEGIN{print \"WORK\"}")
        ];
        (string Name, string Path, string? Search, string Output, string Error)[] cases =
        [
            ("configured-before-local", "common.awk", "lib", "LIB\n", ""),
            ("ordered", "common.awk", "other:lib", "OTHER\n", ""),
            ("missing-first", "common.awk", "missing:lib", "LIB\n", ""),
            ("no-implicit-local", "common.awk", "missing", "", "awk: common.awk: No such file or directory\n"),
            ("dot-first", "common.awk", ".:lib", "LOCAL\n", ""),
            ("dot-last", "common.awk", "lib:.", "LIB\n", ""),
            ("empty-first", "common.awk", ":lib", "LOCAL\n", ""),
            ("empty-last", "common.awk", "lib:", "LIB\n", ""),
            ("trailing-slashes", "common.awk", "lib///", "LIB\n", ""),
            ("dot-qualified", "./common.awk", "lib", "LOCAL\n", ""),
            ("slash-qualified-missing", "sub/common.awk", "lib", "", "awk: sub/common.awk: No such file or directory\n"),
            ("absolute", "/common.awk", "lib", "LOCAL\n", ""),
            ("unset", "common.awk", null, "LOCAL\n", ""),
            ("empty", "common.awk", "", "LOCAL\n", "")
        ];
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        {
            foreach (var test in cases)
            {
                string[] arguments = profile.Length == 0 ? ["-f", test.Path] : [profile, "-f", test.Path];
                yield return AwkGoldenCaseBuilder.Create("source-search-" + test.Name + "-" + (profile.Length == 0 ? "default" : profile[2..]), arguments, files)
                    .WithEnvironment(test.Search is { } search ? [new("AWKPATH", search)] : [])
                    .ExitsWith(test.Error.Length == 0 ? 0 : 2).WithStdOut(test.Output).WithStdErr(test.Error).Build();
            }
            string[] rootArguments = profile.Length == 0 ? ["-f", "common.awk"] : [profile, "-f", "common.awk"];
            yield return AwkGoldenCaseBuilder.Create("source-search-root-entry-" + (profile.Length == 0 ? "default" : profile[2..]), rootArguments, files)
                .WithEnvironment([new("PWD", "/work"), new("AWKPATH", "/")]).WithStdOut("LOCAL\n").Build();
        }
        yield return AwkGoldenCaseBuilder.Create("source-search-include-precedence", ["@include \"common.awk\"\nBEGIN{print \"MAIN\"}"], files)
            .WithEnvironment([new("AWKPATH", "lib")]).WithStdOut("LIB\nMAIN\n").Build();
        yield return AwkGoldenCaseBuilder.Create("source-search-include-suffix", ["@include \"common\"\nBEGIN{print \"MAIN\"}"], files)
            .WithEnvironment([new("AWKPATH", "lib")]).WithStdOut("LIB\nMAIN\n").Build();
        yield return AwkGoldenCaseBuilder.Create("source-search-include-slash", ["@include \"sub/common.awk\"\nBEGIN{print \"unreachable\"}"], files)
            .WithEnvironment([new("AWKPATH", "lib")]).ExitsWith(2).WithStdErr("awk: sub/common.awk: No such file or directory\n").Build();
    }
}
