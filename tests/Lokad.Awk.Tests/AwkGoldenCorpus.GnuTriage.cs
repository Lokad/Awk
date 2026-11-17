namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> GnuTriageCases()
    {
        yield return Failing("printf-no-arguments-fails-at-execution",
            ["BEGIN{print \"before\";printf;print \"after\"}END{print \"end\"}"], [], 2,
            "before\n", "awk: printf: no arguments\n");
        yield return Passing("printf-no-arguments-in-untaken-branch",
            ["BEGIN{if(0)printf;print \"after\"}"], [], "after\n");
        yield return Failing("printf-no-arguments-captures-target-before-failure",
            ["function path(){print \"target\";return \"out\"}BEGIN{print \"before\";printf > path();print \"after\"}"], [], 2,
            "before\ntarget\n", "awk: printf: no arguments\n");
        foreach (var option in new[] { "--posix", "--traditional" })
        {
            var mode = option[2..];
            yield return Passing("printf-no-arguments-is-empty-" + mode,
                [option, "BEGIN{print \"before\";printf;print \"after\"}"], [], "before\nafter\n");
            yield return Passing("printf-no-arguments-comment-is-empty-" + mode,
                [option, "BEGIN{printf # no format\nprint \"after\"}"], [], "after\n");
            yield return PassingWithExpectedFiles("printf-no-arguments-opens-empty-output-" + mode,
                [option, "function path(){print \"target\";return \"out\"}BEGIN{print \"before\";printf > path();print \"after\",close(\"out\");print (getline x < \"out\")}"], [],
                "before\ntarget\nafter 0\n0\n", [File("/out", "")]);
            yield return PassingWithExpectedFiles("printf-no-arguments-truncates-output-" + mode,
                [option, "BEGIN{printf > \"out\";print close(\"out\")}"], [File("/out", "kept\n")], "0\n", [File("/out", "")]);
        }
    }
}
