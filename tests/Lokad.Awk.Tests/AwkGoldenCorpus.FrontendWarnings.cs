namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FrontendWarningCases()
    {
        const string warning = "awk: warning: escape sequence `\\q' treated as plain `q'\n";
        string[] profiles = ["", "--posix", "--traditional"];
        foreach (var profile in profiles)
        {
            var prefix = profile.Length == 0 ? Array.Empty<string>() : new[] { profile };
            yield return AwkGoldenCaseBuilder.Create("frontend-warning-unreachable-" + profile,
                [.. prefix, "BEGIN{if(0)print \"\\q\";print \"ok\"}"], [])
                .WithStdOut("ok\n").WithStdErr(warning).Build();
            yield return AwkGoldenCaseBuilder.Create("frontend-warning-unused-function-" + profile,
                [.. prefix, "function f(){return \"\\q\"} BEGIN{print \"ok\"}"], [])
                .WithStdOut("ok\n").WithStdErr(warning).Build();
        }
        yield return AwkGoldenCaseBuilder.Create("frontend-warning-before-runtime-failure",
            ["BEGIN{print \"before\";print \"\\q\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\nq\n")
            .WithStdErr(warning + "awk: division by zero attempted\n").ExitsWith(2).Build();
    }
}
