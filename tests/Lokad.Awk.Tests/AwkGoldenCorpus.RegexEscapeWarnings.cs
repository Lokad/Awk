namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU re.c has separate fixed once-per-escape histories for string and regexp warnings.
    private static IEnumerable<AwkGoldenCase> RegexEscapeWarningCases()
    {
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-gnu-unreachable",
            ["function dead(){return /\\q\\q/}BEGIN{print \"ok\"}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-gnu-dynamic-history",
            ["BEGIN{p=\"\\\\q\";for(i=0;i<3;i++)print \"q\"~p;p=\"x\";print \"x\"~p;p=\"\\\\q\";IGNORECASE=1;print \"Qq\"~p}"], [])
            .WithStdOut("1\n1\n1\n1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-gnu-source-runtime-history",
            ["BEGIN{if(0)print /\\q/;print \"q\"~\"\\\\q\";print \"q\"~\"\\\\d\";print \"d\"~ /\\d/}"], [])
            .WithStdOut("1\n0\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\nawk: warning: regexp escape sequence `\\d' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-gnu-string-independent",
            ["BEGIN{print \"\\q\";print \"q\"~/\\q/;print \"q\"~\"\\\\q\"}"], [])
            .WithStdOut("q\n1\n1\n").WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-gnu-digits",
            ["BEGIN{print \"89\"~/\\8\\9/;print \"89\"~\"\\\\8\\\\9\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\8' treated as plain `8'\nawk: warning: regexp escape sequence `\\9' treated as plain `9'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-gnu-bracket",
            ["BEGIN{print \"q\"~/[\\q]/;print \"q\"~\"[\\\\q]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-gnu-profile-word",
            ["BEGIN{print \"a\"~/\\w/;print \"w\"~\"\\\\w\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-posix-unreachable",
            ["--posix", "function dead(){return /\\q\\q/}BEGIN{print \"ok\"}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-posix-dynamic-history",
            ["--posix", "BEGIN{p=\"\\\\q\";for(i=0;i<3;i++)print \"q\"~p;p=\"x\";print \"x\"~p;p=\"\\\\q\";IGNORECASE=1;print \"Qq\"~p}"], [])
            .WithStdOut("1\n1\n1\n1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-posix-source-runtime-history",
            ["--posix", "BEGIN{if(0)print /\\q/;print \"q\"~\"\\\\q\";print \"q\"~\"\\\\d\";print \"d\"~ /\\d/}"], [])
            .WithStdOut("1\n0\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\nawk: warning: regexp escape sequence `\\d' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-posix-string-independent",
            ["--posix", "BEGIN{print \"\\q\";print \"q\"~/\\q/;print \"q\"~\"\\\\q\"}"], [])
            .WithStdOut("q\n1\n1\n").WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-posix-digits",
            ["--posix", "BEGIN{print \"89\"~/\\8\\9/;print \"89\"~\"\\\\8\\\\9\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\8' treated as plain `8'\nawk: warning: regexp escape sequence `\\9' treated as plain `9'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-posix-bracket",
            ["--posix", "BEGIN{print \"q\"~/[\\q]/;print \"q\"~\"[\\\\q]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-posix-profile-word",
            ["--posix", "BEGIN{print \"a\"~/\\w/;print \"w\"~\"\\\\w\"}"], [])
            .WithStdOut("0\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\w' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-traditional-unreachable",
            ["--traditional", "function dead(){return /\\q\\q/}BEGIN{print \"ok\"}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-traditional-dynamic-history",
            ["--traditional", "BEGIN{p=\"\\\\q\";for(i=0;i<3;i++)print \"q\"~p;p=\"x\";print \"x\"~p;p=\"\\\\q\";IGNORECASE=1;print \"Qq\"~p}"], [])
            .WithStdOut("1\n1\n1\n1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-traditional-source-runtime-history",
            ["--traditional", "BEGIN{if(0)print /\\q/;print \"q\"~\"\\\\q\";print \"q\"~\"\\\\d\";print \"d\"~ /\\d/}"], [])
            .WithStdOut("1\n0\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\nawk: warning: regexp escape sequence `\\d' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-traditional-string-independent",
            ["--traditional", "BEGIN{print \"\\q\";print \"q\"~/\\q/;print \"q\"~\"\\\\q\"}"], [])
            .WithStdOut("q\n1\n1\n").WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-traditional-digits",
            ["--traditional", "BEGIN{print \"89\"~/\\8\\9/;print \"89\"~\"\\\\8\\\\9\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\8' treated as plain `8'\nawk: warning: regexp escape sequence `\\9' treated as plain `9'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-traditional-bracket",
            ["--traditional", "BEGIN{print \"q\"~/[\\q]/;print \"q\"~\"[\\\\q]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\q' is not a known regexp operator\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-escape-warning-traditional-profile-word",
            ["--traditional", "BEGIN{print \"a\"~/\\w/;print \"w\"~\"\\\\w\"}"], [])
            .WithStdOut("0\n1\n").WithStdErr("awk: warning: regexp escape sequence `\\w' is not a known regexp operator\n").ExitsWith(0).Build();
    }
}
