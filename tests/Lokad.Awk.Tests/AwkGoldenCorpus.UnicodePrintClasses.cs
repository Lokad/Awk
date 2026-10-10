namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UnicodePrintClassCases()
    {
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Lu",
            ["BEGIN{v=\"A\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00c9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud801\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Ll",
            ["BEGIN{v=\"a\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00e9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud801\udc28\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Lt",
            ["BEGIN{v=\"\u01c5\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Lm",
            ["BEGIN{v=\"\u02b0\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Lo",
            ["BEGIN{v=\"\u4e2d\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud840\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Mn",
            ["BEGIN{v=\"\u0301\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud834\udd67\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Mc",
            ["BEGIN{v=\"\u0903\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud834\udd65\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Me",
            ["BEGIN{v=\"\u20dd\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Nd",
            ["BEGIN{v=\"7\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u0661\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud835\udfce\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Nl",
            ["BEGIN{v=\"\u2160\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd40\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-No",
            ["BEGIN{v=\"\u00b2\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd07\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Pc",
            ["BEGIN{v=\"_\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u203f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Pd",
            ["BEGIN{v=\"-\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2014\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Ps",
            ["BEGIN{v=\"(\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3008\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Pe",
            ["BEGIN{v=\")\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3009\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Pi",
            ["BEGIN{v=\"\u00ab\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2018\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Pf",
            ["BEGIN{v=\"\u00bb\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2019\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Po",
            ["BEGIN{v=\"!\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u060c\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Sm",
            ["BEGIN{v=\"+\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2211\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud835\udec1\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Sc",
            ["BEGIN{v=\"$\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u20ac\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Sk",
            ["BEGIN{v=\"^\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u02c2\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-So",
            ["BEGIN{v=\"\u00a9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2603\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud83d\ude00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Zs",
            ["BEGIN{v=\" \";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00a0\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u1680\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2007\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u202f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Zl",
            ["BEGIN{v=\"\u2028\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Zp",
            ["BEGIN{v=\"\u2029\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Cc",
            ["BEGIN{v=\"\\000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\011\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\012\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\037\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\177\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u0085\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Cf",
            ["BEGIN{v=\"\u00ad\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u200d\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2060\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ufeff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb40\udc01\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb40\udc7f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Co",
            ["BEGIN{v=\"\ue000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb80\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbc0\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbff\udffd\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-category-Cn",
            ["BEGIN{v=\"\u0378\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\uffff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud83f\udffe\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbff\udfff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-ascii",
            ["BEGIN{for(i=0;i<128;i++){v=sprintf(\"%c\",i);print v~/^[[:graph:]]$/,v~/^[[:print:]]$/}}"], [])
            .WithStdOut("0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-field-split",
            ["BEGIN{FS=\"[[:graph:]]+\"}{printf \"%d:[%s]:[%s]:[%s]\\n\",NF,$1,$2,$3}"], [])
            .WithStandardInput("a\u00e9b c\ud83d\ude00d\n")
            .WithStdOut("3:[]:[ ]:[]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-substitution",
            ["BEGIN{v=\"\u00e9\ud83d\ude00 \\t\";print gsub(/[[:graph:]]/,\"X\",v),v==\"XX \\t\";v=\"\u00e9\ud83d\ude00 \\t\";print gsub(/[[:print:]]/,\"X\",v),v==\"XXX\\t\"}"], [])
            .WithStdOut("2 1\n3 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-split",
            ["BEGIN{print split(\"\u00e9 \ud83d\ude00\",a,/[[:graph:]]+/);print \"[\"a[1]\"][\"a[2]\"][\"a[3]\"]\"}"], [])
            .WithStdOut("3\n[][ ][]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-union",
            ["BEGIN{print \"\u00e9 \"~/^[[:graph:][:blank:]]+$/, \"\u00e9 \"~/^[[:print:]]+$/;print \"\u00e9\\n\"~/^[[:graph:][:blank:]]+$/, \"\u00e9\\n\"~/^[[:print:]]+$/}"], [])
            .WithStdOut("1 1\n0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-negated-union",
            ["BEGIN{print \"\u00e9\"~/[^[:print:]0-9]/;print \"\\000\"~/[^[:print:]0-9]/;print \"6\"~/[^[:print:]0-9]/}"], [])
            .WithStdOut("0\n1\n0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-coordinate",
            ["BEGIN{print match(\"\u00e9\ud83d\ude00 \",/([[:graph:]]+)([[:print:]])/),RLENGTH}"], [])
            .WithStdOut("1 4\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-separator-option",
            ["-F", "[[:graph:]]+", "{printf \"%d:[%s]:[%s]:[%s]\\n\",NF,$1,$2,$3}"], [])
            .WithStandardInput("a\u00e9b c\ud83d\ude00d\n")
            .WithStdOut("3:[]:[ ]:[]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-record-separator",
            ["BEGIN{RS=\"[[:graph:]]+\"}{print \"[\"$0\"]\",RT}"], [])
            .WithStandardInput("a\u00e9b c\ud83d\ude00d")
            .WithStdOut("[] a\u00e9b\n[ ] c\ud83d\ude00d\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-array",
            ["BEGIN{print match(\"\u00e9\ud83d\ude00 \",/([[:graph:]]+)([[:print:]])/,a),RLENGTH;print a[1]==\"\u00e9\ud83d\ude00\",a[2]==\" \",a[1,\"start\"],a[1,\"length\"],a[2,\"start\"],a[2,\"length\"]}"], [])
            .WithStdOut("1 4\n1 1 1 3 4 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-byte-coordinate",
            ["--characters-as-bytes", "BEGIN{print match(\"\u00e9\ud83d\ude00 \",/([[:graph:]]+)([[:print:]])/),RLENGTH}"], [])
            .WithStdOut("1 7\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Lu",
            ["--posix", "BEGIN{v=\"A\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00c9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud801\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Ll",
            ["--posix", "BEGIN{v=\"a\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00e9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud801\udc28\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Lt",
            ["--posix", "BEGIN{v=\"\u01c5\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Lm",
            ["--posix", "BEGIN{v=\"\u02b0\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Lo",
            ["--posix", "BEGIN{v=\"\u4e2d\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud840\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Mn",
            ["--posix", "BEGIN{v=\"\u0301\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud834\udd67\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Mc",
            ["--posix", "BEGIN{v=\"\u0903\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud834\udd65\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Me",
            ["--posix", "BEGIN{v=\"\u20dd\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Nd",
            ["--posix", "BEGIN{v=\"7\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u0661\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud835\udfce\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Nl",
            ["--posix", "BEGIN{v=\"\u2160\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd40\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-No",
            ["--posix", "BEGIN{v=\"\u00b2\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd07\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Pc",
            ["--posix", "BEGIN{v=\"_\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u203f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Pd",
            ["--posix", "BEGIN{v=\"-\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2014\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Ps",
            ["--posix", "BEGIN{v=\"(\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3008\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Pe",
            ["--posix", "BEGIN{v=\")\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3009\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Pi",
            ["--posix", "BEGIN{v=\"\u00ab\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2018\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Pf",
            ["--posix", "BEGIN{v=\"\u00bb\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2019\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Po",
            ["--posix", "BEGIN{v=\"!\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u060c\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Sm",
            ["--posix", "BEGIN{v=\"+\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2211\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud835\udec1\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Sc",
            ["--posix", "BEGIN{v=\"$\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u20ac\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Sk",
            ["--posix", "BEGIN{v=\"^\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u02c2\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-So",
            ["--posix", "BEGIN{v=\"\u00a9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2603\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud83d\ude00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Zs",
            ["--posix", "BEGIN{v=\" \";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00a0\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u1680\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2007\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u202f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Zl",
            ["--posix", "BEGIN{v=\"\u2028\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Zp",
            ["--posix", "BEGIN{v=\"\u2029\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Cc",
            ["--posix", "BEGIN{v=\"\\000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\011\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\012\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\037\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\177\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u0085\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Cf",
            ["--posix", "BEGIN{v=\"\u00ad\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u200d\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2060\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ufeff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb40\udc01\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb40\udc7f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Co",
            ["--posix", "BEGIN{v=\"\ue000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb80\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbc0\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbff\udffd\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-category-Cn",
            ["--posix", "BEGIN{v=\"\u0378\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\uffff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud83f\udffe\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbff\udfff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-ascii",
            ["--posix", "BEGIN{for(i=0;i<128;i++){v=sprintf(\"%c\",i);print v~/^[[:graph:]]$/,v~/^[[:print:]]$/}}"], [])
            .WithStdOut("0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-field-split",
            ["--posix", "BEGIN{FS=\"[[:graph:]]+\"}{printf \"%d:[%s]:[%s]:[%s]\\n\",NF,$1,$2,$3}"], [])
            .WithStandardInput("a\u00e9b c\ud83d\ude00d\n")
            .WithStdOut("3:[]:[ ]:[]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-substitution",
            ["--posix", "BEGIN{v=\"\u00e9\ud83d\ude00 \\t\";print gsub(/[[:graph:]]/,\"X\",v),v==\"XX \\t\";v=\"\u00e9\ud83d\ude00 \\t\";print gsub(/[[:print:]]/,\"X\",v),v==\"XXX\\t\"}"], [])
            .WithStdOut("2 1\n3 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-split",
            ["--posix", "BEGIN{print split(\"\u00e9 \ud83d\ude00\",a,/[[:graph:]]+/);print \"[\"a[1]\"][\"a[2]\"][\"a[3]\"]\"}"], [])
            .WithStdOut("3\n[][ ][]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-union",
            ["--posix", "BEGIN{print \"\u00e9 \"~/^[[:graph:][:blank:]]+$/, \"\u00e9 \"~/^[[:print:]]+$/;print \"\u00e9\\n\"~/^[[:graph:][:blank:]]+$/, \"\u00e9\\n\"~/^[[:print:]]+$/}"], [])
            .WithStdOut("1 1\n0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-negated-union",
            ["--posix", "BEGIN{print \"\u00e9\"~/[^[:print:]0-9]/;print \"\\000\"~/[^[:print:]0-9]/;print \"6\"~/[^[:print:]0-9]/}"], [])
            .WithStdOut("0\n1\n0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-capture-coordinate",
            ["--posix", "BEGIN{print match(\"\u00e9\ud83d\ude00 \",/([[:graph:]]+)([[:print:]])/),RLENGTH}"], [])
            .WithStdOut("1 4\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-posix-separator-option",
            ["--posix", "-F", "[[:graph:]]+", "{printf \"%d:[%s]:[%s]:[%s]\\n\",NF,$1,$2,$3}"], [])
            .WithStandardInput("a\u00e9b c\ud83d\ude00d\n")
            .WithStdOut("3:[]:[ ]:[]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Lu",
            ["--traditional", "BEGIN{v=\"A\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00c9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud801\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Ll",
            ["--traditional", "BEGIN{v=\"a\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00e9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud801\udc28\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Lt",
            ["--traditional", "BEGIN{v=\"\u01c5\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Lm",
            ["--traditional", "BEGIN{v=\"\u02b0\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Lo",
            ["--traditional", "BEGIN{v=\"\u4e2d\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud840\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Mn",
            ["--traditional", "BEGIN{v=\"\u0301\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud834\udd67\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Mc",
            ["--traditional", "BEGIN{v=\"\u0903\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud834\udd65\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Me",
            ["--traditional", "BEGIN{v=\"\u20dd\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Nd",
            ["--traditional", "BEGIN{v=\"7\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u0661\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud835\udfce\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Nl",
            ["--traditional", "BEGIN{v=\"\u2160\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd40\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-No",
            ["--traditional", "BEGIN{v=\"\u00b2\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd07\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Pc",
            ["--traditional", "BEGIN{v=\"_\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u203f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Pd",
            ["--traditional", "BEGIN{v=\"-\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2014\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Ps",
            ["--traditional", "BEGIN{v=\"(\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3008\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Pe",
            ["--traditional", "BEGIN{v=\")\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3009\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Pi",
            ["--traditional", "BEGIN{v=\"\u00ab\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2018\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Pf",
            ["--traditional", "BEGIN{v=\"\u00bb\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2019\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Po",
            ["--traditional", "BEGIN{v=\"!\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u060c\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud800\udd00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Sm",
            ["--traditional", "BEGIN{v=\"+\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2211\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud835\udec1\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Sc",
            ["--traditional", "BEGIN{v=\"$\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u20ac\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Sk",
            ["--traditional", "BEGIN{v=\"^\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u02c2\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-So",
            ["--traditional", "BEGIN{v=\"\u00a9\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2603\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud83d\ude00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Zs",
            ["--traditional", "BEGIN{v=\" \";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u00a0\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u1680\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2007\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u202f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u3000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n0 1 0 1\n1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Zl",
            ["--traditional", "BEGIN{v=\"\u2028\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Zp",
            ["--traditional", "BEGIN{v=\"\u2029\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Cc",
            ["--traditional", "BEGIN{v=\"\\000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\011\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\012\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\037\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\\177\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u0085\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Cf",
            ["--traditional", "BEGIN{v=\"\u00ad\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u200d\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\u2060\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ufeff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb40\udc01\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb40\udc7f\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Co",
            ["--traditional", "BEGIN{v=\"\ue000\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udb80\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbc0\udc00\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbff\udffd\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-category-Cn",
            ["--traditional", "BEGIN{v=\"\u0378\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\uffff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\ud83f\udffe\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";v=\"\udbff\udfff\";print v~/^[[:graph:]]$/,v~/^[[:print:]]$/,v~\"^[[:graph:]]$\",v~\"^[[:print:]]$\";print v~/^[^[:graph:]]$/,v~/^[^[:print:]]$/,v~\"^[^[:graph:]]$\",v~\"^[^[:print:]]$\";}"], [])
            .WithStdOut("0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n0 0 0 0\n1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-ascii",
            ["--traditional", "BEGIN{for(i=0;i<128;i++){v=sprintf(\"%c\",i);print v~/^[[:graph:]]$/,v~/^[[:print:]]$/}}"], [])
            .WithStdOut("0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 0\n0 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n1 1\n0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-field-split",
            ["--traditional", "BEGIN{FS=\"[[:graph:]]+\"}{printf \"%d:[%s]:[%s]:[%s]\\n\",NF,$1,$2,$3}"], [])
            .WithStandardInput("a\u00e9b c\ud83d\ude00d\n")
            .WithStdOut("3:[]:[ ]:[]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-substitution",
            ["--traditional", "BEGIN{v=\"\u00e9\ud83d\ude00 \\t\";print gsub(/[[:graph:]]/,\"X\",v),v==\"XX \\t\";v=\"\u00e9\ud83d\ude00 \\t\";print gsub(/[[:print:]]/,\"X\",v),v==\"XXX\\t\"}"], [])
            .WithStdOut("2 1\n3 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-split",
            ["--traditional", "BEGIN{print split(\"\u00e9 \ud83d\ude00\",a,/[[:graph:]]+/);print \"[\"a[1]\"][\"a[2]\"][\"a[3]\"]\"}"], [])
            .WithStdOut("3\n[][ ][]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-union",
            ["--traditional", "BEGIN{print \"\u00e9 \"~/^[[:graph:][:blank:]]+$/, \"\u00e9 \"~/^[[:print:]]+$/;print \"\u00e9\\n\"~/^[[:graph:][:blank:]]+$/, \"\u00e9\\n\"~/^[[:print:]]+$/}"], [])
            .WithStdOut("1 1\n0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-negated-union",
            ["--traditional", "BEGIN{print \"\u00e9\"~/[^[:print:]0-9]/;print \"\\000\"~/[^[:print:]0-9]/;print \"6\"~/[^[:print:]0-9]/}"], [])
            .WithStdOut("0\n1\n0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-capture-coordinate",
            ["--traditional", "BEGIN{print match(\"\u00e9\ud83d\ude00 \",/([[:graph:]]+)([[:print:]])/),RLENGTH}"], [])
            .WithStdOut("1 4\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-traditional-separator-option",
            ["--traditional", "-F", "[[:graph:]]+", "{printf \"%d:[%s]:[%s]:[%s]\\n\",NF,$1,$2,$3}"], [])
            .WithStandardInput("a\u00e9b c\ud83d\ude00d\n")
            .WithStdOut("3:[]:[ ]:[]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-basic-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9 \",/([[:graph:]])([[:print:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-basic-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"([[:graph:]])([[:print:]])\";print match(\"\u00e9 \",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-nested-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9\ud83d\ude00\",/(([[:graph:]])([[:graph:]]))/,a),RLENGTH;print length(a);print a[0]==\"\u00e9\ud83d\ude00\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\ud83d\ude00\",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];print a[3]==\"\ud83d\ude00\",a[3,\"start\"],a[3,\"length\"];}"], [])
            .WithStdOut("1 3\n12\n1 1 3\n1 1 3\n1 1 1\n1 2 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-nested-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(([[:graph:]])([[:graph:]]))\";print match(\"\u00e9\ud83d\ude00\",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9\ud83d\ude00\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\ud83d\ude00\",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];print a[3]==\"\ud83d\ude00\",a[3,\"start\"],a[3,\"length\"];}"], [])
            .WithStdOut("1 3\n12\n1 1 3\n1 1 3\n1 1 1\n1 2 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-empty-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9\",/()([[:graph:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 1\n9\n1 1 1\n1 1 0\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-empty-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"()([[:graph:]])\";print match(\"\u00e9\",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 1\n9\n1 1 1\n1 1 0\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-absent-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9\",/(a)?([[:graph:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print (1 in a),((1 SUBSEP \"start\") in a),((1 SUBSEP \"length\") in a);print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 1\n6\n1 1 1\n0 0 0\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-absent-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(a)?([[:graph:]])\";print match(\"\u00e9\",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print (1 in a),((1 SUBSEP \"start\") in a),((1 SUBSEP \"length\") in a);print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 1\n6\n1 1 1\n0 0 0\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-repeated-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9\ud83d\ude00\",/([[:graph:]])+/,a),RLENGTH;print length(a);print a[0]==\"\u00e9\ud83d\ude00\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\ud83d\ude00\",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 3\n6\n1 1 3\n1 2 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-repeated-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"([[:graph:]])+\";print match(\"\u00e9\ud83d\ude00\",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9\ud83d\ude00\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\ud83d\ude00\",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 3\n6\n1 1 3\n1 2 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-literal-parenthesis-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"(\u00e9\",/[()]([[:graph:]])/,a),RLENGTH;print length(a);print a[0]==\"(\u00e9\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 2\n6\n1 1 2\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-literal-parenthesis-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"[()]([[:graph:]])\";print match(\"(\u00e9\",p,a),RLENGTH;print length(a);print a[0]==\"(\u00e9\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 2\n6\n1 1 2\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-escaped-parenthesis-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"(\u00e9)\",/\\(([[:graph:]])\\)/,a),RLENGTH;print length(a);print a[0]==\"(\u00e9)\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 3\n6\n1 1 3\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-escaped-parenthesis-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"\\\\(([[:graph:]])\\\\)\";print match(\"(\u00e9)\",p,a),RLENGTH;print length(a);print a[0]==\"(\u00e9)\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 3\n6\n1 1 3\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-noncapturing-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9 \",/(?:([[:graph:]]))([[:print:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-noncapturing-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(?:([[:graph:]]))([[:print:]])\";print match(\"\u00e9 \",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-lookahead-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9\",/(?=([[:graph:]]))([[:graph:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 1\n9\n1 1 1\n1 1 1\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-lookahead-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(?=([[:graph:]]))([[:graph:]])\";print match(\"\u00e9\",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 1\n9\n1 1 1\n1 1 1\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-lookbehind-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9 \",/(?<=([[:graph:]]))([[:print:]])/,a),RLENGTH;print length(a);print a[0]==\" \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("2 1\n9\n1 2 1\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-lookbehind-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(?<=([[:graph:]]))([[:print:]])\";print match(\"\u00e9 \",p,a),RLENGTH;print length(a);print a[0]==\" \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("2 1\n9\n1 2 1\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-named-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9 \",/(?<word>[[:graph:]])([[:print:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\" \",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 2 1\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-named-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(?<word>[[:graph:]])([[:print:]])\";print match(\"\u00e9 \",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\" \",a[1,\"start\"],a[1,\"length\"];print a[2]==\"\u00e9\",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 2 1\n1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-comment-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9 \",/(?x:(?# [:graph:] )([[:graph:]]))([[:print:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-comment-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(?x:(?# [:graph:] )([[:graph:]]))([[:print:]])\";print match(\"\u00e9 \",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\"\u00e9\",a[1,\"start\"],a[1,\"length\"];print a[2]==\" \",a[2,\"start\"],a[2,\"length\"];}"], [])
            .WithStdOut("1 2\n9\n1 1 2\n1 1 1\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-conditional-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9\",/(a)?(?(1)b|[[:graph:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print (1 in a),((1 SUBSEP \"start\") in a),((1 SUBSEP \"length\") in a);}"], [])
            .WithStdOut("1 1\n3\n1 1 1\n0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-conditional-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(a)?(?(1)b|[[:graph:]])\";print match(\"\u00e9\",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9\",a[0,\"start\"],a[0,\"length\"];print (1 in a),((1 SUBSEP \"start\") in a),((1 SUBSEP \"length\") in a);}"], [])
            .WithStdOut("1 1\n3\n1 1 1\n0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-atomic-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9 \",/(?>[[:graph:]]+)([[:print:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\" \",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 2\n6\n1 1 2\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-atomic-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(?>[[:graph:]]+)([[:print:]])\";print match(\"\u00e9 \",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print a[1]==\" \",a[1,\"start\"],a[1,\"length\"];}"], [])
            .WithStdOut("1 2\n6\n1 1 2\n1 2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-balancing-literal",
            ["BEGIN{a[\"old\"]=\"keep\";print match(\"\u00e9 \",/(?<n>[[:graph:]])(?<-n>[[:print:]])/,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print (1 in a),((1 SUBSEP \"start\") in a),((1 SUBSEP \"length\") in a);}"], [])
            .WithStdOut("1 2\n3\n1 1 2\n0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-print-gnu-capture-balancing-dynamic",
            ["BEGIN{a[\"old\"]=\"keep\";p=\"(?<n>[[:graph:]])(?<-n>[[:print:]])\";print match(\"\u00e9 \",p,a),RLENGTH;print length(a);print a[0]==\"\u00e9 \",a[0,\"start\"],a[0,\"length\"];print (1 in a),((1 SUBSEP \"start\") in a),((1 SUBSEP \"length\") in a);}"], [])
            .WithStdOut("1 2\n3\n1 1 2\n0 0 0\n").WithStdErr("").ExitsWith(0).Build();
    }
}
