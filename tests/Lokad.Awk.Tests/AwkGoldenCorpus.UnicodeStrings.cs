namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UnicodeStringCases()
    {
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0000",
            ["BEGIN{v=\"\\u\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0001",
            ["BEGIN{v=\"\\ug\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0002",
            ["BEGIN{v=\"\\u0\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0003",
            ["BEGIN{v=\"\\u0000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0004",
            ["BEGIN{v=\"\\u0g\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [\u0000g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0005",
            ["BEGIN{v=\"\\u1\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0006",
            ["BEGIN{v=\"\\u000b\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u000b]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0007",
            ["BEGIN{v=\"\\u41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0008",
            ["BEGIN{v=\"\\u041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0009",
            ["BEGIN{v=\"\\u0041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0010",
            ["BEGIN{v=\"\\u00041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0011",
            ["BEGIN{v=\"\\u000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0012",
            ["BEGIN{v=\"\\u0000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0013",
            ["BEGIN{v=\"\\u00000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0014",
            ["BEGIN{v=\"\\u000000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0015",
            ["BEGIN{v=\"\\u00000041g\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [Ag]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0016",
            ["BEGIN{v=\"\\u0041a\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0017",
            ["BEGIN{v=\"\\u03B1\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0018",
            ["BEGIN{v=\"\\u00e9\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u00e9]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0019",
            ["BEGIN{v=\"\\u1f600\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0020",
            ["BEGIN{v=\"\\u10ffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0021",
            ["BEGIN{v=\"\\ud7ff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\ud7ff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0022",
            ["BEGIN{v=\"\\ud800\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0023",
            ["BEGIN{v=\"\\udfff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0024",
            ["BEGIN{v=\"\\ud800\\udc00\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [??]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0025",
            ["BEGIN{v=\"\\ue000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\ue000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0026",
            ["BEGIN{v=\"\\u110000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0027",
            ["BEGIN{v=\"\\u00110000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0028",
            ["BEGIN{v=\"\\u7fffffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0029",
            ["BEGIN{v=\"\\u80000000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0030",
            ["BEGIN{v=\"\\uffffffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0031",
            ["BEGIN{v=\"\\U0001f600\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0032",
            ["BEGIN{v=\"\\u-41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("4 [u-41]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0033",
            ["BEGIN{v=\"\\u{41}\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u{41}]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0034",
            ["BEGIN{v=\"\\u005c\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\\]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0035",
            ["BEGIN{v=\"\\u005cq\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [\\q]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0036",
            ["BEGIN{v=\"\\u0041\\n\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [A\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0037",
            ["BEGIN{v=\"\\x\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [x]\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0038",
            ["BEGIN{v=\"\\x41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0039",
            ["BEGIN{v=\"\\x4142\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("3 [A42]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0040",
            ["BEGIN{v=\"\\000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0041",
            ["BEGIN{v=\"\\101\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0042",
            ["BEGIN{v=\"\\n\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0043",
            ["BEGIN{v=\"\\\\u0041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0044",
            ["BEGIN{v=\"\\u\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 u\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0045",
            ["BEGIN{v=\"\\ug\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 ug\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0046",
            ["BEGIN{v=\"\\u0\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0047",
            ["BEGIN{v=\"\\u0000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0048",
            ["BEGIN{v=\"\\u0g\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 \u0000g\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0049",
            ["BEGIN{v=\"\\u1\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0001\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0050",
            ["BEGIN{v=\"\\u000b\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u000b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0051",
            ["BEGIN{v=\"\\u41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0052",
            ["BEGIN{v=\"\\u041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0053",
            ["BEGIN{v=\"\\u0041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0054",
            ["BEGIN{v=\"\\u00041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0055",
            ["BEGIN{v=\"\\u000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0056",
            ["BEGIN{v=\"\\u0000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0057",
            ["BEGIN{v=\"\\u00000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0058",
            ["BEGIN{v=\"\\u000000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 \u00041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0059",
            ["BEGIN{v=\"\\u00000041g\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 Ag\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0060",
            ["BEGIN{v=\"\\u0041a\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u041a\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0061",
            ["BEGIN{v=\"\\u03B1\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u03b1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0062",
            ["BEGIN{v=\"\\u00e9\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u00e9\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0063",
            ["BEGIN{v=\"\\u1f600\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \ud83d\ude00\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0064",
            ["BEGIN{v=\"\\u10ffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \udbff\udfff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0065",
            ["BEGIN{v=\"\\ud7ff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \ud7ff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0066",
            ["BEGIN{v=\"\\ud800\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0067",
            ["BEGIN{v=\"\\udfff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0068",
            ["BEGIN{v=\"\\ud800\\udc00\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 ??\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0069",
            ["BEGIN{v=\"\\ue000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \ue000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0070",
            ["BEGIN{v=\"\\u110000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0071",
            ["BEGIN{v=\"\\u00110000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0072",
            ["BEGIN{v=\"\\u7fffffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0073",
            ["BEGIN{v=\"\\u80000000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0074",
            ["BEGIN{v=\"\\uffffffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0075",
            ["BEGIN{v=\"\\U0001f600\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 U0001f600\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0076",
            ["BEGIN{v=\"\\u-41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("4 1 1 1 u-41\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0077",
            ["BEGIN{v=\"\\u{41}\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u{41}\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0078",
            ["BEGIN{v=\"\\u005c\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \\\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0079",
            ["BEGIN{v=\"\\u005cq\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 \\q\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0080",
            ["BEGIN{v=\"\\u0041\\n\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 A\n\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0081",
            ["BEGIN{v=\"\\x\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 x\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0082",
            ["BEGIN{v=\"\\x41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0083",
            ["BEGIN{v=\"\\x4142\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("3 1 1 1 A42\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0084",
            ["BEGIN{v=\"\\000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0085",
            ["BEGIN{v=\"\\101\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0086",
            ["BEGIN{v=\"\\n\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \n\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0087",
            ["BEGIN{v=\"\\\\u0041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 \\u0041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0088",
            ["BEGIN{if(0)print \"\\u\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0089",
            ["BEGIN{if(0)print \"\\ug\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0090",
            ["BEGIN{if(0)print \"\\u0\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0091",
            ["BEGIN{if(0)print \"\\u0000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0092",
            ["BEGIN{if(0)print \"\\u0g\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0093",
            ["BEGIN{if(0)print \"\\u1\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0094",
            ["BEGIN{if(0)print \"\\u000b\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0095",
            ["BEGIN{if(0)print \"\\u41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0096",
            ["BEGIN{if(0)print \"\\u041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0097",
            ["BEGIN{if(0)print \"\\u0041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0098",
            ["BEGIN{if(0)print \"\\u00041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0099",
            ["BEGIN{if(0)print \"\\u000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0100",
            ["BEGIN{if(0)print \"\\u0000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0101",
            ["BEGIN{if(0)print \"\\u00000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0102",
            ["BEGIN{if(0)print \"\\u000000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0103",
            ["BEGIN{if(0)print \"\\u00000041g\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0104",
            ["BEGIN{if(0)print \"\\u0041a\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0105",
            ["BEGIN{if(0)print \"\\u03B1\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0106",
            ["BEGIN{if(0)print \"\\u00e9\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0107",
            ["BEGIN{if(0)print \"\\u1f600\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0108",
            ["BEGIN{if(0)print \"\\u10ffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0109",
            ["BEGIN{if(0)print \"\\ud7ff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0110",
            ["BEGIN{if(0)print \"\\ud800\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0111",
            ["BEGIN{if(0)print \"\\udfff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0112",
            ["BEGIN{if(0)print \"\\ud800\\udc00\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0113",
            ["BEGIN{if(0)print \"\\ue000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0114",
            ["BEGIN{if(0)print \"\\u110000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0115",
            ["BEGIN{if(0)print \"\\u00110000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0116",
            ["BEGIN{if(0)print \"\\u7fffffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0117",
            ["BEGIN{if(0)print \"\\u80000000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0118",
            ["BEGIN{if(0)print \"\\uffffffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0119",
            ["BEGIN{if(0)print \"\\U0001f600\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0120",
            ["BEGIN{if(0)print \"\\u-41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0121",
            ["BEGIN{if(0)print \"\\u{41}\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0122",
            ["BEGIN{if(0)print \"\\u005c\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0123",
            ["BEGIN{if(0)print \"\\u005cq\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0124",
            ["BEGIN{if(0)print \"\\u0041\\n\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0125",
            ["BEGIN{if(0)print \"\\x\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0126",
            ["BEGIN{if(0)print \"\\x41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0127",
            ["BEGIN{if(0)print \"\\x4142\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0128",
            ["BEGIN{if(0)print \"\\000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0129",
            ["BEGIN{if(0)print \"\\101\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0130",
            ["BEGIN{if(0)print \"\\n\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0131",
            ["BEGIN{if(0)print \"\\\\u0041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0132",
            ["function f(){return \"\\u\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0133",
            ["function f(){return \"\\ug\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0134",
            ["function f(){return \"\\u0\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0135",
            ["function f(){return \"\\u0000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0136",
            ["function f(){return \"\\u0g\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0137",
            ["function f(){return \"\\u1\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0138",
            ["function f(){return \"\\u000b\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0139",
            ["function f(){return \"\\u41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0140",
            ["function f(){return \"\\u041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0141",
            ["function f(){return \"\\u0041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0142",
            ["function f(){return \"\\u00041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0143",
            ["function f(){return \"\\u000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0144",
            ["function f(){return \"\\u0000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0145",
            ["function f(){return \"\\u00000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0146",
            ["function f(){return \"\\u000000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0147",
            ["function f(){return \"\\u00000041g\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0148",
            ["function f(){return \"\\u0041a\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0149",
            ["function f(){return \"\\u03B1\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0150",
            ["function f(){return \"\\u00e9\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0151",
            ["function f(){return \"\\u1f600\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0152",
            ["function f(){return \"\\u10ffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0153",
            ["function f(){return \"\\ud7ff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0154",
            ["function f(){return \"\\ud800\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0155",
            ["function f(){return \"\\udfff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0156",
            ["function f(){return \"\\ud800\\udc00\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0157",
            ["function f(){return \"\\ue000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0158",
            ["function f(){return \"\\u110000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0159",
            ["function f(){return \"\\u00110000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0160",
            ["function f(){return \"\\u7fffffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0161",
            ["function f(){return \"\\u80000000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0162",
            ["function f(){return \"\\uffffffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0163",
            ["function f(){return \"\\U0001f600\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0164",
            ["function f(){return \"\\u-41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0165",
            ["function f(){return \"\\u{41}\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0166",
            ["function f(){return \"\\u005c\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0167",
            ["function f(){return \"\\u005cq\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0168",
            ["function f(){return \"\\u0041\\n\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0169",
            ["function f(){return \"\\x\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0170",
            ["function f(){return \"\\x41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0171",
            ["function f(){return \"\\x4142\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0172",
            ["function f(){return \"\\000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0173",
            ["function f(){return \"\\101\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0174",
            ["function f(){return \"\\n\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0175",
            ["function f(){return \"\\\\u0041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0176",
            ["function id(x){return x} BEGIN{v=id(\"\\u\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0177",
            ["function id(x){return x} BEGIN{v=id(\"\\ug\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0178",
            ["function id(x){return x} BEGIN{v=id(\"\\u0\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0179",
            ["function id(x){return x} BEGIN{v=id(\"\\u0000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0180",
            ["function id(x){return x} BEGIN{v=id(\"\\u0g\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0181",
            ["function id(x){return x} BEGIN{v=id(\"\\u1\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0182",
            ["function id(x){return x} BEGIN{v=id(\"\\u000b\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u000b]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0183",
            ["function id(x){return x} BEGIN{v=id(\"\\u41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0184",
            ["function id(x){return x} BEGIN{v=id(\"\\u041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0185",
            ["function id(x){return x} BEGIN{v=id(\"\\u0041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0186",
            ["function id(x){return x} BEGIN{v=id(\"\\u00041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0187",
            ["function id(x){return x} BEGIN{v=id(\"\\u000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0188",
            ["function id(x){return x} BEGIN{v=id(\"\\u0000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0189",
            ["function id(x){return x} BEGIN{v=id(\"\\u00000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0190",
            ["function id(x){return x} BEGIN{v=id(\"\\u000000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0191",
            ["function id(x){return x} BEGIN{v=id(\"\\u00000041g\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [Ag]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0192",
            ["function id(x){return x} BEGIN{v=id(\"\\u0041a\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0193",
            ["function id(x){return x} BEGIN{v=id(\"\\u03B1\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0194",
            ["function id(x){return x} BEGIN{v=id(\"\\u00e9\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u00e9]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0195",
            ["function id(x){return x} BEGIN{v=id(\"\\u1f600\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0196",
            ["function id(x){return x} BEGIN{v=id(\"\\u10ffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0197",
            ["function id(x){return x} BEGIN{v=id(\"\\ud7ff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\ud7ff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0198",
            ["function id(x){return x} BEGIN{v=id(\"\\ud800\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0199",
            ["function id(x){return x} BEGIN{v=id(\"\\udfff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0200",
            ["function id(x){return x} BEGIN{v=id(\"\\ud800\\udc00\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [??]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0201",
            ["function id(x){return x} BEGIN{v=id(\"\\ue000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\ue000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0202",
            ["function id(x){return x} BEGIN{v=id(\"\\u110000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0203",
            ["function id(x){return x} BEGIN{v=id(\"\\u00110000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0204",
            ["function id(x){return x} BEGIN{v=id(\"\\u7fffffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0205",
            ["function id(x){return x} BEGIN{v=id(\"\\u80000000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0206",
            ["function id(x){return x} BEGIN{v=id(\"\\uffffffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0207",
            ["function id(x){return x} BEGIN{v=id(\"\\U0001f600\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0208",
            ["function id(x){return x} BEGIN{v=id(\"\\u-41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u-41]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0209",
            ["function id(x){return x} BEGIN{v=id(\"\\u{41}\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u{41}]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0210",
            ["function id(x){return x} BEGIN{v=id(\"\\u005c\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\\]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0211",
            ["function id(x){return x} BEGIN{v=id(\"\\u005cq\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\\q]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0212",
            ["function id(x){return x} BEGIN{v=id(\"\\u0041\\n\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0213",
            ["function id(x){return x} BEGIN{v=id(\"\\x\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [x]\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0214",
            ["function id(x){return x} BEGIN{v=id(\"\\x41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0215",
            ["function id(x){return x} BEGIN{v=id(\"\\x4142\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A42]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0216",
            ["function id(x){return x} BEGIN{v=id(\"\\000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0217",
            ["function id(x){return x} BEGIN{v=id(\"\\101\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0218",
            ["function id(x){return x} BEGIN{v=id(\"\\n\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0219",
            ["function id(x){return x} BEGIN{v=id(\"\\\\u0041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0220",
            ["BEGIN{print \"before\";v=\"\\u\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0221",
            ["BEGIN{print \"before\";v=\"\\ug\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0222",
            ["BEGIN{print \"before\";v=\"\\u0\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0223",
            ["BEGIN{print \"before\";v=\"\\u0000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0224",
            ["BEGIN{print \"before\";v=\"\\u0g\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000g]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0225",
            ["BEGIN{print \"before\";v=\"\\u1\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0001]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0226",
            ["BEGIN{print \"before\";v=\"\\u000b\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u000b]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0227",
            ["BEGIN{print \"before\";v=\"\\u41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0228",
            ["BEGIN{print \"before\";v=\"\\u041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0229",
            ["BEGIN{print \"before\";v=\"\\u0041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0230",
            ["BEGIN{print \"before\";v=\"\\u00041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0231",
            ["BEGIN{print \"before\";v=\"\\u000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0232",
            ["BEGIN{print \"before\";v=\"\\u0000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0233",
            ["BEGIN{print \"before\";v=\"\\u00000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0234",
            ["BEGIN{print \"before\";v=\"\\u000000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u00041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0235",
            ["BEGIN{print \"before\";v=\"\\u00000041g\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[Ag]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0236",
            ["BEGIN{print \"before\";v=\"\\u0041a\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u041a]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0237",
            ["BEGIN{print \"before\";v=\"\\u03B1\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u03b1]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0238",
            ["BEGIN{print \"before\";v=\"\\u00e9\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u00e9]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0239",
            ["BEGIN{print \"before\";v=\"\\u1f600\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\ud83d\ude00]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0240",
            ["BEGIN{print \"before\";v=\"\\u10ffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\udbff\udfff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0241",
            ["BEGIN{print \"before\";v=\"\\ud7ff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\ud7ff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0242",
            ["BEGIN{print \"before\";v=\"\\ud800\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0243",
            ["BEGIN{print \"before\";v=\"\\udfff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0244",
            ["BEGIN{print \"before\";v=\"\\ud800\\udc00\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[??]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0245",
            ["BEGIN{print \"before\";v=\"\\ue000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\ue000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0246",
            ["BEGIN{print \"before\";v=\"\\u110000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0247",
            ["BEGIN{print \"before\";v=\"\\u00110000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0248",
            ["BEGIN{print \"before\";v=\"\\u7fffffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0249",
            ["BEGIN{print \"before\";v=\"\\u80000000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0250",
            ["BEGIN{print \"before\";v=\"\\uffffffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0251",
            ["BEGIN{print \"before\";v=\"\\U0001f600\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0252",
            ["BEGIN{print \"before\";v=\"\\u-41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u-41]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0253",
            ["BEGIN{print \"before\";v=\"\\u{41}\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u{41}]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0254",
            ["BEGIN{print \"before\";v=\"\\u005c\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\\]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0255",
            ["BEGIN{print \"before\";v=\"\\u005cq\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\\q]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0256",
            ["BEGIN{print \"before\";v=\"\\u0041\\n\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A\n]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0257",
            ["BEGIN{print \"before\";v=\"\\x\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[x]\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0258",
            ["BEGIN{print \"before\";v=\"\\x41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0259",
            ["BEGIN{print \"before\";v=\"\\x4142\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A42]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0260",
            ["BEGIN{print \"before\";v=\"\\000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0261",
            ["BEGIN{print \"before\";v=\"\\101\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0262",
            ["BEGIN{print \"before\";v=\"\\n\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\n]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0263",
            ["BEGIN{print \"before\";v=\"\\\\u0041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\\u0041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0264",
            ["--posix", "BEGIN{v=\"\\u\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0265",
            ["--posix", "BEGIN{v=\"\\ug\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [ug]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0266",
            ["--posix", "BEGIN{v=\"\\u0\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [u0]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0267",
            ["--posix", "BEGIN{v=\"\\u0000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0268",
            ["--posix", "BEGIN{v=\"\\u0g\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("3 [u0g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0269",
            ["--posix", "BEGIN{v=\"\\u1\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [u1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0270",
            ["--posix", "BEGIN{v=\"\\u000b\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u000b]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0271",
            ["--posix", "BEGIN{v=\"\\u41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("3 [u41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0272",
            ["--posix", "BEGIN{v=\"\\u041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("4 [u041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0273",
            ["--posix", "BEGIN{v=\"\\u0041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0274",
            ["--posix", "BEGIN{v=\"\\u00041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0275",
            ["--posix", "BEGIN{v=\"\\u000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("7 [u000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0276",
            ["--posix", "BEGIN{v=\"\\u0000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("8 [u0000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0277",
            ["--posix", "BEGIN{v=\"\\u00000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [u00000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0278",
            ["--posix", "BEGIN{v=\"\\u000000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("10 [u000000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0279",
            ["--posix", "BEGIN{v=\"\\u00000041g\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("10 [u00000041g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0280",
            ["--posix", "BEGIN{v=\"\\u0041a\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [u0041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0281",
            ["--posix", "BEGIN{v=\"\\u03B1\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u03B1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0282",
            ["--posix", "BEGIN{v=\"\\u00e9\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u00e9]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0283",
            ["--posix", "BEGIN{v=\"\\u1f600\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0284",
            ["--posix", "BEGIN{v=\"\\u10ffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("7 [u10ffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0285",
            ["--posix", "BEGIN{v=\"\\ud7ff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [ud7ff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0286",
            ["--posix", "BEGIN{v=\"\\ud800\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0287",
            ["--posix", "BEGIN{v=\"\\udfff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0288",
            ["--posix", "BEGIN{v=\"\\ud800\\udc00\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("10 [ud800udc00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0289",
            ["--posix", "BEGIN{v=\"\\ue000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [ue000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0290",
            ["--posix", "BEGIN{v=\"\\u110000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("7 [u110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0291",
            ["--posix", "BEGIN{v=\"\\u00110000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [u00110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0292",
            ["--posix", "BEGIN{v=\"\\u7fffffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [u7fffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0293",
            ["--posix", "BEGIN{v=\"\\u80000000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [u80000000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0294",
            ["--posix", "BEGIN{v=\"\\uffffffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [uffffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0295",
            ["--posix", "BEGIN{v=\"\\U0001f600\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0296",
            ["--posix", "BEGIN{v=\"\\u-41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("4 [u-41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0297",
            ["--posix", "BEGIN{v=\"\\u{41}\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u{41}]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0298",
            ["--posix", "BEGIN{v=\"\\u005c\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u005c]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0299",
            ["--posix", "BEGIN{v=\"\\u005cq\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [u005cq]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0300",
            ["--posix", "BEGIN{v=\"\\u0041\\n\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [u0041\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0301",
            ["--posix", "BEGIN{v=\"\\x\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0302",
            ["--posix", "BEGIN{v=\"\\x41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("3 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0303",
            ["--posix", "BEGIN{v=\"\\x4142\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [x4142]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0304",
            ["--posix", "BEGIN{v=\"\\000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0305",
            ["--posix", "BEGIN{v=\"\\101\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0306",
            ["--posix", "BEGIN{v=\"\\n\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0307",
            ["--posix", "BEGIN{v=\"\\\\u0041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0308",
            ["--posix", "BEGIN{v=\"\\u\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 u\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0309",
            ["--posix", "BEGIN{v=\"\\ug\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 ug\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0310",
            ["--posix", "BEGIN{v=\"\\u0\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 u0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0311",
            ["--posix", "BEGIN{v=\"\\u0000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0312",
            ["--posix", "BEGIN{v=\"\\u0g\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("3 1 1 1 u0g\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0313",
            ["--posix", "BEGIN{v=\"\\u1\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 u1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0314",
            ["--posix", "BEGIN{v=\"\\u000b\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u000b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0315",
            ["--posix", "BEGIN{v=\"\\u41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("3 1 1 1 u41\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0316",
            ["--posix", "BEGIN{v=\"\\u041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("4 1 1 1 u041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0317",
            ["--posix", "BEGIN{v=\"\\u0041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u0041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0318",
            ["--posix", "BEGIN{v=\"\\u00041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 u00041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0319",
            ["--posix", "BEGIN{v=\"\\u000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("7 1 1 1 u000041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0320",
            ["--posix", "BEGIN{v=\"\\u0000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("8 1 1 1 u0000041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0321",
            ["--posix", "BEGIN{v=\"\\u00000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 u00000041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0322",
            ["--posix", "BEGIN{v=\"\\u000000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("10 1 1 1 u000000041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0323",
            ["--posix", "BEGIN{v=\"\\u00000041g\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("10 1 1 1 u00000041g\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0324",
            ["--posix", "BEGIN{v=\"\\u0041a\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 u0041a\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0325",
            ["--posix", "BEGIN{v=\"\\u03B1\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u03B1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0326",
            ["--posix", "BEGIN{v=\"\\u00e9\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u00e9\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0327",
            ["--posix", "BEGIN{v=\"\\u1f600\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 u1f600\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0328",
            ["--posix", "BEGIN{v=\"\\u10ffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("7 1 1 1 u10ffff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0329",
            ["--posix", "BEGIN{v=\"\\ud7ff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 ud7ff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0330",
            ["--posix", "BEGIN{v=\"\\ud800\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 ud800\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0331",
            ["--posix", "BEGIN{v=\"\\udfff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 udfff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0332",
            ["--posix", "BEGIN{v=\"\\ud800\\udc00\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("10 1 1 1 ud800udc00\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0333",
            ["--posix", "BEGIN{v=\"\\ue000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 ue000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0334",
            ["--posix", "BEGIN{v=\"\\u110000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("7 1 1 1 u110000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0335",
            ["--posix", "BEGIN{v=\"\\u00110000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 u00110000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0336",
            ["--posix", "BEGIN{v=\"\\u7fffffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 u7fffffff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0337",
            ["--posix", "BEGIN{v=\"\\u80000000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 u80000000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0338",
            ["--posix", "BEGIN{v=\"\\uffffffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 uffffffff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0339",
            ["--posix", "BEGIN{v=\"\\U0001f600\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 U0001f600\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0340",
            ["--posix", "BEGIN{v=\"\\u-41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("4 1 1 1 u-41\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0341",
            ["--posix", "BEGIN{v=\"\\u{41}\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u{41}\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0342",
            ["--posix", "BEGIN{v=\"\\u005c\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u005c\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0343",
            ["--posix", "BEGIN{v=\"\\u005cq\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 u005cq\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0344",
            ["--posix", "BEGIN{v=\"\\u0041\\n\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 u0041\n\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0345",
            ["--posix", "BEGIN{v=\"\\x\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 x\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0346",
            ["--posix", "BEGIN{v=\"\\x41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("3 1 1 1 x41\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0347",
            ["--posix", "BEGIN{v=\"\\x4142\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 x4142\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0348",
            ["--posix", "BEGIN{v=\"\\000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0349",
            ["--posix", "BEGIN{v=\"\\101\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0350",
            ["--posix", "BEGIN{v=\"\\n\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \n\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0351",
            ["--posix", "BEGIN{v=\"\\\\u0041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 \\u0041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0352",
            ["--posix", "BEGIN{if(0)print \"\\u\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0353",
            ["--posix", "BEGIN{if(0)print \"\\ug\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0354",
            ["--posix", "BEGIN{if(0)print \"\\u0\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0355",
            ["--posix", "BEGIN{if(0)print \"\\u0000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0356",
            ["--posix", "BEGIN{if(0)print \"\\u0g\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0357",
            ["--posix", "BEGIN{if(0)print \"\\u1\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0358",
            ["--posix", "BEGIN{if(0)print \"\\u000b\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0359",
            ["--posix", "BEGIN{if(0)print \"\\u41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0360",
            ["--posix", "BEGIN{if(0)print \"\\u041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0361",
            ["--posix", "BEGIN{if(0)print \"\\u0041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0362",
            ["--posix", "BEGIN{if(0)print \"\\u00041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0363",
            ["--posix", "BEGIN{if(0)print \"\\u000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0364",
            ["--posix", "BEGIN{if(0)print \"\\u0000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0365",
            ["--posix", "BEGIN{if(0)print \"\\u00000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0366",
            ["--posix", "BEGIN{if(0)print \"\\u000000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0367",
            ["--posix", "BEGIN{if(0)print \"\\u00000041g\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0368",
            ["--posix", "BEGIN{if(0)print \"\\u0041a\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0369",
            ["--posix", "BEGIN{if(0)print \"\\u03B1\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0370",
            ["--posix", "BEGIN{if(0)print \"\\u00e9\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0371",
            ["--posix", "BEGIN{if(0)print \"\\u1f600\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0372",
            ["--posix", "BEGIN{if(0)print \"\\u10ffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0373",
            ["--posix", "BEGIN{if(0)print \"\\ud7ff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0374",
            ["--posix", "BEGIN{if(0)print \"\\ud800\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0375",
            ["--posix", "BEGIN{if(0)print \"\\udfff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0376",
            ["--posix", "BEGIN{if(0)print \"\\ud800\\udc00\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0377",
            ["--posix", "BEGIN{if(0)print \"\\ue000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0378",
            ["--posix", "BEGIN{if(0)print \"\\u110000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0379",
            ["--posix", "BEGIN{if(0)print \"\\u00110000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0380",
            ["--posix", "BEGIN{if(0)print \"\\u7fffffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0381",
            ["--posix", "BEGIN{if(0)print \"\\u80000000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0382",
            ["--posix", "BEGIN{if(0)print \"\\uffffffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0383",
            ["--posix", "BEGIN{if(0)print \"\\U0001f600\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0384",
            ["--posix", "BEGIN{if(0)print \"\\u-41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0385",
            ["--posix", "BEGIN{if(0)print \"\\u{41}\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0386",
            ["--posix", "BEGIN{if(0)print \"\\u005c\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0387",
            ["--posix", "BEGIN{if(0)print \"\\u005cq\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0388",
            ["--posix", "BEGIN{if(0)print \"\\u0041\\n\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0389",
            ["--posix", "BEGIN{if(0)print \"\\x\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0390",
            ["--posix", "BEGIN{if(0)print \"\\x41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0391",
            ["--posix", "BEGIN{if(0)print \"\\x4142\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0392",
            ["--posix", "BEGIN{if(0)print \"\\000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0393",
            ["--posix", "BEGIN{if(0)print \"\\101\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0394",
            ["--posix", "BEGIN{if(0)print \"\\n\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0395",
            ["--posix", "BEGIN{if(0)print \"\\\\u0041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0396",
            ["--posix", "function f(){return \"\\u\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0397",
            ["--posix", "function f(){return \"\\ug\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0398",
            ["--posix", "function f(){return \"\\u0\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0399",
            ["--posix", "function f(){return \"\\u0000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0400",
            ["--posix", "function f(){return \"\\u0g\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0401",
            ["--posix", "function f(){return \"\\u1\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0402",
            ["--posix", "function f(){return \"\\u000b\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0403",
            ["--posix", "function f(){return \"\\u41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0404",
            ["--posix", "function f(){return \"\\u041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0405",
            ["--posix", "function f(){return \"\\u0041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0406",
            ["--posix", "function f(){return \"\\u00041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0407",
            ["--posix", "function f(){return \"\\u000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0408",
            ["--posix", "function f(){return \"\\u0000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0409",
            ["--posix", "function f(){return \"\\u00000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0410",
            ["--posix", "function f(){return \"\\u000000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0411",
            ["--posix", "function f(){return \"\\u00000041g\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0412",
            ["--posix", "function f(){return \"\\u0041a\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0413",
            ["--posix", "function f(){return \"\\u03B1\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0414",
            ["--posix", "function f(){return \"\\u00e9\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0415",
            ["--posix", "function f(){return \"\\u1f600\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0416",
            ["--posix", "function f(){return \"\\u10ffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0417",
            ["--posix", "function f(){return \"\\ud7ff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0418",
            ["--posix", "function f(){return \"\\ud800\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0419",
            ["--posix", "function f(){return \"\\udfff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0420",
            ["--posix", "function f(){return \"\\ud800\\udc00\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0421",
            ["--posix", "function f(){return \"\\ue000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0422",
            ["--posix", "function f(){return \"\\u110000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0423",
            ["--posix", "function f(){return \"\\u00110000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0424",
            ["--posix", "function f(){return \"\\u7fffffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0425",
            ["--posix", "function f(){return \"\\u80000000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0426",
            ["--posix", "function f(){return \"\\uffffffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0427",
            ["--posix", "function f(){return \"\\U0001f600\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0428",
            ["--posix", "function f(){return \"\\u-41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0429",
            ["--posix", "function f(){return \"\\u{41}\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0430",
            ["--posix", "function f(){return \"\\u005c\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0431",
            ["--posix", "function f(){return \"\\u005cq\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0432",
            ["--posix", "function f(){return \"\\u0041\\n\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0433",
            ["--posix", "function f(){return \"\\x\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0434",
            ["--posix", "function f(){return \"\\x41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0435",
            ["--posix", "function f(){return \"\\x4142\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0436",
            ["--posix", "function f(){return \"\\000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0437",
            ["--posix", "function f(){return \"\\101\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0438",
            ["--posix", "function f(){return \"\\n\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0439",
            ["--posix", "function f(){return \"\\\\u0041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0440",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0441",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\ug\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [ug]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0442",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u0\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u0]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0443",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u0000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0444",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u0g\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u0g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0445",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u1\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0446",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u000b\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u000b]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0447",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0448",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0449",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u0041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0450",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u00041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0451",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0452",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u0000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u0000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0453",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u00000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u00000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0454",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u000000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u000000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0455",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u00000041g\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u00000041g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0456",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u0041a\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u0041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0457",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u03B1\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u03B1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0458",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u00e9\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u00e9]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0459",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u1f600\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0460",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u10ffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u10ffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0461",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\ud7ff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [ud7ff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0462",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\ud800\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0463",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\udfff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0464",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\ud800\\udc00\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [ud800udc00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0465",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\ue000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [ue000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0466",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u110000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0467",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u00110000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u00110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0468",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u7fffffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u7fffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0469",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u80000000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u80000000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0470",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\uffffffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [uffffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0471",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\U0001f600\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0472",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u-41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u-41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0473",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u{41}\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u{41}]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0474",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u005c\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u005c]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0475",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u005cq\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u005cq]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0476",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\u0041\\n\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u0041\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0477",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\x\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0478",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\x41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0479",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\x4142\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [x4142]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0480",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0481",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\101\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0482",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\n\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0483",
            ["--posix", "function id(x){return x} BEGIN{v=id(\"\\\\u0041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0484",
            ["--posix", "BEGIN{print \"before\";v=\"\\u\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0485",
            ["--posix", "BEGIN{print \"before\";v=\"\\ug\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[ug]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0486",
            ["--posix", "BEGIN{print \"before\";v=\"\\u0\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u0]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0487",
            ["--posix", "BEGIN{print \"before\";v=\"\\u0000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0488",
            ["--posix", "BEGIN{print \"before\";v=\"\\u0g\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u0g]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0489",
            ["--posix", "BEGIN{print \"before\";v=\"\\u1\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u1]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0490",
            ["--posix", "BEGIN{print \"before\";v=\"\\u000b\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u000b]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0491",
            ["--posix", "BEGIN{print \"before\";v=\"\\u41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u41]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0492",
            ["--posix", "BEGIN{print \"before\";v=\"\\u041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0493",
            ["--posix", "BEGIN{print \"before\";v=\"\\u0041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u0041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0494",
            ["--posix", "BEGIN{print \"before\";v=\"\\u00041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u00041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0495",
            ["--posix", "BEGIN{print \"before\";v=\"\\u000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u000041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0496",
            ["--posix", "BEGIN{print \"before\";v=\"\\u0000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u0000041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0497",
            ["--posix", "BEGIN{print \"before\";v=\"\\u00000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u00000041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0498",
            ["--posix", "BEGIN{print \"before\";v=\"\\u000000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u000000041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0499",
            ["--posix", "BEGIN{print \"before\";v=\"\\u00000041g\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u00000041g]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0500",
            ["--posix", "BEGIN{print \"before\";v=\"\\u0041a\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u0041a]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0501",
            ["--posix", "BEGIN{print \"before\";v=\"\\u03B1\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u03B1]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0502",
            ["--posix", "BEGIN{print \"before\";v=\"\\u00e9\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u00e9]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0503",
            ["--posix", "BEGIN{print \"before\";v=\"\\u1f600\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u1f600]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0504",
            ["--posix", "BEGIN{print \"before\";v=\"\\u10ffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u10ffff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0505",
            ["--posix", "BEGIN{print \"before\";v=\"\\ud7ff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[ud7ff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0506",
            ["--posix", "BEGIN{print \"before\";v=\"\\ud800\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[ud800]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0507",
            ["--posix", "BEGIN{print \"before\";v=\"\\udfff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[udfff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0508",
            ["--posix", "BEGIN{print \"before\";v=\"\\ud800\\udc00\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[ud800udc00]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0509",
            ["--posix", "BEGIN{print \"before\";v=\"\\ue000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[ue000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0510",
            ["--posix", "BEGIN{print \"before\";v=\"\\u110000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u110000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0511",
            ["--posix", "BEGIN{print \"before\";v=\"\\u00110000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u00110000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0512",
            ["--posix", "BEGIN{print \"before\";v=\"\\u7fffffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u7fffffff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0513",
            ["--posix", "BEGIN{print \"before\";v=\"\\u80000000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u80000000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0514",
            ["--posix", "BEGIN{print \"before\";v=\"\\uffffffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[uffffffff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0515",
            ["--posix", "BEGIN{print \"before\";v=\"\\U0001f600\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0516",
            ["--posix", "BEGIN{print \"before\";v=\"\\u-41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u-41]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0517",
            ["--posix", "BEGIN{print \"before\";v=\"\\u{41}\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u{41}]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0518",
            ["--posix", "BEGIN{print \"before\";v=\"\\u005c\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u005c]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0519",
            ["--posix", "BEGIN{print \"before\";v=\"\\u005cq\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u005cq]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0520",
            ["--posix", "BEGIN{print \"before\";v=\"\\u0041\\n\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u0041\n]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0521",
            ["--posix", "BEGIN{print \"before\";v=\"\\x\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[x]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0522",
            ["--posix", "BEGIN{print \"before\";v=\"\\x41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[x41]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0523",
            ["--posix", "BEGIN{print \"before\";v=\"\\x4142\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[x4142]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0524",
            ["--posix", "BEGIN{print \"before\";v=\"\\000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0525",
            ["--posix", "BEGIN{print \"before\";v=\"\\101\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0526",
            ["--posix", "BEGIN{print \"before\";v=\"\\n\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\n]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0527",
            ["--posix", "BEGIN{print \"before\";v=\"\\\\u0041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\\u0041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0528",
            ["--traditional", "BEGIN{v=\"\\u\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0529",
            ["--traditional", "BEGIN{v=\"\\ug\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0530",
            ["--traditional", "BEGIN{v=\"\\u0\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0531",
            ["--traditional", "BEGIN{v=\"\\u0000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0532",
            ["--traditional", "BEGIN{v=\"\\u0g\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [\u0000g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0533",
            ["--traditional", "BEGIN{v=\"\\u1\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0534",
            ["--traditional", "BEGIN{v=\"\\u000b\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u000b]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0535",
            ["--traditional", "BEGIN{v=\"\\u41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0536",
            ["--traditional", "BEGIN{v=\"\\u041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0537",
            ["--traditional", "BEGIN{v=\"\\u0041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0538",
            ["--traditional", "BEGIN{v=\"\\u00041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0539",
            ["--traditional", "BEGIN{v=\"\\u000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0540",
            ["--traditional", "BEGIN{v=\"\\u0000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0541",
            ["--traditional", "BEGIN{v=\"\\u00000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0542",
            ["--traditional", "BEGIN{v=\"\\u000000041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0543",
            ["--traditional", "BEGIN{v=\"\\u00000041g\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [Ag]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0544",
            ["--traditional", "BEGIN{v=\"\\u0041a\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0545",
            ["--traditional", "BEGIN{v=\"\\u03B1\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0546",
            ["--traditional", "BEGIN{v=\"\\u00e9\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u00e9]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0547",
            ["--traditional", "BEGIN{v=\"\\u1f600\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0548",
            ["--traditional", "BEGIN{v=\"\\u10ffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0549",
            ["--traditional", "BEGIN{v=\"\\ud7ff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\ud7ff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0550",
            ["--traditional", "BEGIN{v=\"\\ud800\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0551",
            ["--traditional", "BEGIN{v=\"\\udfff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0552",
            ["--traditional", "BEGIN{v=\"\\ud800\\udc00\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [??]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0553",
            ["--traditional", "BEGIN{v=\"\\ue000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\ue000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0554",
            ["--traditional", "BEGIN{v=\"\\u110000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0555",
            ["--traditional", "BEGIN{v=\"\\u00110000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0556",
            ["--traditional", "BEGIN{v=\"\\u7fffffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0557",
            ["--traditional", "BEGIN{v=\"\\u80000000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0558",
            ["--traditional", "BEGIN{v=\"\\uffffffff\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0559",
            ["--traditional", "BEGIN{v=\"\\U0001f600\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("9 [U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0560",
            ["--traditional", "BEGIN{v=\"\\u-41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("4 [u-41]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0561",
            ["--traditional", "BEGIN{v=\"\\u{41}\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("5 [u{41}]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0562",
            ["--traditional", "BEGIN{v=\"\\u005c\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\\]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0563",
            ["--traditional", "BEGIN{v=\"\\u005cq\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [\\q]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0564",
            ["--traditional", "BEGIN{v=\"\\u0041\\n\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("2 [A\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0565",
            ["--traditional", "BEGIN{v=\"\\x\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [x]\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0566",
            ["--traditional", "BEGIN{v=\"\\x41\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0567",
            ["--traditional", "BEGIN{v=\"\\x4142\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("3 [A42]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0568",
            ["--traditional", "BEGIN{v=\"\\000\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0569",
            ["--traditional", "BEGIN{v=\"\\101\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0570",
            ["--traditional", "BEGIN{v=\"\\n\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-value-0571",
            ["--traditional", "BEGIN{v=\"\\\\u0041\";print length(v),\"[\"v\"]\";exit}"], [])
            .WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0572",
            ["--traditional", "BEGIN{v=\"\\u\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 u\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0573",
            ["--traditional", "BEGIN{v=\"\\ug\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 ug\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0574",
            ["--traditional", "BEGIN{v=\"\\u0\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0575",
            ["--traditional", "BEGIN{v=\"\\u0000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0576",
            ["--traditional", "BEGIN{v=\"\\u0g\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 \u0000g\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0577",
            ["--traditional", "BEGIN{v=\"\\u1\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0001\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0578",
            ["--traditional", "BEGIN{v=\"\\u000b\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u000b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0579",
            ["--traditional", "BEGIN{v=\"\\u41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0580",
            ["--traditional", "BEGIN{v=\"\\u041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0581",
            ["--traditional", "BEGIN{v=\"\\u0041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0582",
            ["--traditional", "BEGIN{v=\"\\u00041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0583",
            ["--traditional", "BEGIN{v=\"\\u000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0584",
            ["--traditional", "BEGIN{v=\"\\u0000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0585",
            ["--traditional", "BEGIN{v=\"\\u00000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0586",
            ["--traditional", "BEGIN{v=\"\\u000000041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 \u00041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0587",
            ["--traditional", "BEGIN{v=\"\\u00000041g\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 Ag\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0588",
            ["--traditional", "BEGIN{v=\"\\u0041a\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u041a\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0589",
            ["--traditional", "BEGIN{v=\"\\u03B1\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u03b1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0590",
            ["--traditional", "BEGIN{v=\"\\u00e9\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u00e9\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0591",
            ["--traditional", "BEGIN{v=\"\\u1f600\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \ud83d\ude00\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0592",
            ["--traditional", "BEGIN{v=\"\\u10ffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \udbff\udfff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0593",
            ["--traditional", "BEGIN{v=\"\\ud7ff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \ud7ff\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0594",
            ["--traditional", "BEGIN{v=\"\\ud800\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0595",
            ["--traditional", "BEGIN{v=\"\\udfff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0596",
            ["--traditional", "BEGIN{v=\"\\ud800\\udc00\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 ??\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0597",
            ["--traditional", "BEGIN{v=\"\\ue000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \ue000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0598",
            ["--traditional", "BEGIN{v=\"\\u110000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0599",
            ["--traditional", "BEGIN{v=\"\\u00110000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0600",
            ["--traditional", "BEGIN{v=\"\\u7fffffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0601",
            ["--traditional", "BEGIN{v=\"\\u80000000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0602",
            ["--traditional", "BEGIN{v=\"\\uffffffff\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 ?\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0603",
            ["--traditional", "BEGIN{v=\"\\U0001f600\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("9 1 1 1 U0001f600\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0604",
            ["--traditional", "BEGIN{v=\"\\u-41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("4 1 1 1 u-41\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0605",
            ["--traditional", "BEGIN{v=\"\\u{41}\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("5 1 1 1 u{41}\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0606",
            ["--traditional", "BEGIN{v=\"\\u005c\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \\\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0607",
            ["--traditional", "BEGIN{v=\"\\u005cq\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 \\q\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0608",
            ["--traditional", "BEGIN{v=\"\\u0041\\n\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("2 1 1 1 A\n\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0609",
            ["--traditional", "BEGIN{v=\"\\x\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 x\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0610",
            ["--traditional", "BEGIN{v=\"\\x41\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0611",
            ["--traditional", "BEGIN{v=\"\\x4142\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("3 1 1 1 A42\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0612",
            ["--traditional", "BEGIN{v=\"\\000\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \u0000\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0613",
            ["--traditional", "BEGIN{v=\"\\101\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 A\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0614",
            ["--traditional", "BEGIN{v=\"\\n\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("1 1 1 1 \n\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-operations-0615",
            ["--traditional", "BEGIN{v=\"\\\\u0041\";print length(v),index(v,v),length(substr(v,1,1)),(v==v),sprintf(\"%s\",v);exit}"], [])
            .WithStdOut("6 1 1 1 \\u0041\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0616",
            ["--traditional", "BEGIN{if(0)print \"\\u\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0617",
            ["--traditional", "BEGIN{if(0)print \"\\ug\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0618",
            ["--traditional", "BEGIN{if(0)print \"\\u0\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0619",
            ["--traditional", "BEGIN{if(0)print \"\\u0000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0620",
            ["--traditional", "BEGIN{if(0)print \"\\u0g\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0621",
            ["--traditional", "BEGIN{if(0)print \"\\u1\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0622",
            ["--traditional", "BEGIN{if(0)print \"\\u000b\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0623",
            ["--traditional", "BEGIN{if(0)print \"\\u41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0624",
            ["--traditional", "BEGIN{if(0)print \"\\u041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0625",
            ["--traditional", "BEGIN{if(0)print \"\\u0041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0626",
            ["--traditional", "BEGIN{if(0)print \"\\u00041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0627",
            ["--traditional", "BEGIN{if(0)print \"\\u000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0628",
            ["--traditional", "BEGIN{if(0)print \"\\u0000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0629",
            ["--traditional", "BEGIN{if(0)print \"\\u00000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0630",
            ["--traditional", "BEGIN{if(0)print \"\\u000000041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0631",
            ["--traditional", "BEGIN{if(0)print \"\\u00000041g\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0632",
            ["--traditional", "BEGIN{if(0)print \"\\u0041a\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0633",
            ["--traditional", "BEGIN{if(0)print \"\\u03B1\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0634",
            ["--traditional", "BEGIN{if(0)print \"\\u00e9\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0635",
            ["--traditional", "BEGIN{if(0)print \"\\u1f600\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0636",
            ["--traditional", "BEGIN{if(0)print \"\\u10ffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0637",
            ["--traditional", "BEGIN{if(0)print \"\\ud7ff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0638",
            ["--traditional", "BEGIN{if(0)print \"\\ud800\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0639",
            ["--traditional", "BEGIN{if(0)print \"\\udfff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0640",
            ["--traditional", "BEGIN{if(0)print \"\\ud800\\udc00\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0641",
            ["--traditional", "BEGIN{if(0)print \"\\ue000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0642",
            ["--traditional", "BEGIN{if(0)print \"\\u110000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0643",
            ["--traditional", "BEGIN{if(0)print \"\\u00110000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0644",
            ["--traditional", "BEGIN{if(0)print \"\\u7fffffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0645",
            ["--traditional", "BEGIN{if(0)print \"\\u80000000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0646",
            ["--traditional", "BEGIN{if(0)print \"\\uffffffff\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0647",
            ["--traditional", "BEGIN{if(0)print \"\\U0001f600\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0648",
            ["--traditional", "BEGIN{if(0)print \"\\u-41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0649",
            ["--traditional", "BEGIN{if(0)print \"\\u{41}\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0650",
            ["--traditional", "BEGIN{if(0)print \"\\u005c\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0651",
            ["--traditional", "BEGIN{if(0)print \"\\u005cq\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0652",
            ["--traditional", "BEGIN{if(0)print \"\\u0041\\n\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0653",
            ["--traditional", "BEGIN{if(0)print \"\\x\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0654",
            ["--traditional", "BEGIN{if(0)print \"\\x41\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0655",
            ["--traditional", "BEGIN{if(0)print \"\\x4142\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0656",
            ["--traditional", "BEGIN{if(0)print \"\\000\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0657",
            ["--traditional", "BEGIN{if(0)print \"\\101\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0658",
            ["--traditional", "BEGIN{if(0)print \"\\n\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unreachable-0659",
            ["--traditional", "BEGIN{if(0)print \"\\\\u0041\";print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0660",
            ["--traditional", "function f(){return \"\\u\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0661",
            ["--traditional", "function f(){return \"\\ug\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0662",
            ["--traditional", "function f(){return \"\\u0\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0663",
            ["--traditional", "function f(){return \"\\u0000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0664",
            ["--traditional", "function f(){return \"\\u0g\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0665",
            ["--traditional", "function f(){return \"\\u1\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0666",
            ["--traditional", "function f(){return \"\\u000b\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0667",
            ["--traditional", "function f(){return \"\\u41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0668",
            ["--traditional", "function f(){return \"\\u041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0669",
            ["--traditional", "function f(){return \"\\u0041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0670",
            ["--traditional", "function f(){return \"\\u00041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0671",
            ["--traditional", "function f(){return \"\\u000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0672",
            ["--traditional", "function f(){return \"\\u0000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0673",
            ["--traditional", "function f(){return \"\\u00000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0674",
            ["--traditional", "function f(){return \"\\u000000041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0675",
            ["--traditional", "function f(){return \"\\u00000041g\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0676",
            ["--traditional", "function f(){return \"\\u0041a\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0677",
            ["--traditional", "function f(){return \"\\u03B1\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0678",
            ["--traditional", "function f(){return \"\\u00e9\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0679",
            ["--traditional", "function f(){return \"\\u1f600\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0680",
            ["--traditional", "function f(){return \"\\u10ffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0681",
            ["--traditional", "function f(){return \"\\ud7ff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0682",
            ["--traditional", "function f(){return \"\\ud800\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0683",
            ["--traditional", "function f(){return \"\\udfff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0684",
            ["--traditional", "function f(){return \"\\ud800\\udc00\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0685",
            ["--traditional", "function f(){return \"\\ue000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0686",
            ["--traditional", "function f(){return \"\\u110000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0687",
            ["--traditional", "function f(){return \"\\u00110000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0688",
            ["--traditional", "function f(){return \"\\u7fffffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0689",
            ["--traditional", "function f(){return \"\\u80000000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0690",
            ["--traditional", "function f(){return \"\\uffffffff\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0691",
            ["--traditional", "function f(){return \"\\U0001f600\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0692",
            ["--traditional", "function f(){return \"\\u-41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0693",
            ["--traditional", "function f(){return \"\\u{41}\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0694",
            ["--traditional", "function f(){return \"\\u005c\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0695",
            ["--traditional", "function f(){return \"\\u005cq\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0696",
            ["--traditional", "function f(){return \"\\u0041\\n\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0697",
            ["--traditional", "function f(){return \"\\x\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0698",
            ["--traditional", "function f(){return \"\\x41\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0699",
            ["--traditional", "function f(){return \"\\x4142\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0700",
            ["--traditional", "function f(){return \"\\000\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0701",
            ["--traditional", "function f(){return \"\\101\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0702",
            ["--traditional", "function f(){return \"\\n\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-unused-0703",
            ["--traditional", "function f(){return \"\\\\u0041\"} BEGIN{print \"ok\";exit}"], [])
            .WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0704",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0705",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\ug\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0706",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u0\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0707",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u0000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0708",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u0g\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000g]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0709",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u1\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0710",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u000b\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u000b]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0711",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0712",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0713",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u0041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0714",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u00041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0715",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0716",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u0000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0717",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u00000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0718",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u000000041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0719",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u00000041g\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [Ag]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0720",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u0041a\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0721",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u03B1\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0722",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u00e9\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u00e9]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0723",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u1f600\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0724",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u10ffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0725",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\ud7ff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\ud7ff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0726",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\ud800\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0727",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\udfff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0728",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\ud800\\udc00\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [??]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0729",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\ue000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\ue000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0730",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u110000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0731",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u00110000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0732",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u7fffffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0733",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u80000000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0734",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\uffffffff\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0735",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\U0001f600\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0736",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u-41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u-41]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0737",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u{41}\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [u{41}]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0738",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u005c\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\\]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0739",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u005cq\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\\q]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0740",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\u0041\\n\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0741",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\x\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [x]\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0742",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\x41\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0743",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\x4142\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A42]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0744",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\000\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0745",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\101\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0746",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\n\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-array-0747",
            ["--traditional", "function id(x){return x} BEGIN{v=id(\"\\\\u0041\");a[v]=v;n=0;for(k in a)n++;print n,\"[\"a[v]\"]\";exit}"], [])
            .WithStdOut("1 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0748",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0749",
            ["--traditional", "BEGIN{print \"before\";v=\"\\ug\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0750",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u0\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0751",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u0000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0752",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u0g\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000g]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0753",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u1\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0001]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0754",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u000b\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u000b]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0755",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0756",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0757",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u0041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0758",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u00041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0759",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0760",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u0000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0761",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u00000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0762",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u000000041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u00041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0763",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u00000041g\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[Ag]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0764",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u0041a\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u041a]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0765",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u03B1\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u03b1]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0766",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u00e9\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u00e9]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0767",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u1f600\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\ud83d\ude00]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0768",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u10ffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\udbff\udfff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0769",
            ["--traditional", "BEGIN{print \"before\";v=\"\\ud7ff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\ud7ff]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0770",
            ["--traditional", "BEGIN{print \"before\";v=\"\\ud800\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0771",
            ["--traditional", "BEGIN{print \"before\";v=\"\\udfff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0772",
            ["--traditional", "BEGIN{print \"before\";v=\"\\ud800\\udc00\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[??]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0773",
            ["--traditional", "BEGIN{print \"before\";v=\"\\ue000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\ue000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0774",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u110000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0775",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u00110000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0776",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u7fffffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0777",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u80000000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0778",
            ["--traditional", "BEGIN{print \"before\";v=\"\\uffffffff\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0779",
            ["--traditional", "BEGIN{print \"before\";v=\"\\U0001f600\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[U0001f600]\n").WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0780",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u-41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u-41]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0781",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u{41}\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[u{41}]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0782",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u005c\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\\]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0783",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u005cq\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\\q]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0784",
            ["--traditional", "BEGIN{print \"before\";v=\"\\u0041\\n\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A\n]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0785",
            ["--traditional", "BEGIN{print \"before\";v=\"\\x\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[x]\n").WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0786",
            ["--traditional", "BEGIN{print \"before\";v=\"\\x41\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0787",
            ["--traditional", "BEGIN{print \"before\";v=\"\\x4142\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A42]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0788",
            ["--traditional", "BEGIN{print \"before\";v=\"\\000\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\u0000]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0789",
            ["--traditional", "BEGIN{print \"before\";v=\"\\101\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[A]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0790",
            ["--traditional", "BEGIN{print \"before\";v=\"\\n\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\n]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-failure-0791",
            ["--traditional", "BEGIN{print \"before\";v=\"\\\\u0041\";print \"[\"v\"]\";z=0;print 1/z;print \"unreachable\"}"], [])
            .WithStdOut("before\n[\\u0041]\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0792",
            ["BEGIN{v=\"\\u0041\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0793",
            ["BEGIN{v=\"\\u0031\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0794",
            ["BEGIN{v=\"\\u00e9\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0795",
            ["BEGIN{v=\"\\u03b1\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0796",
            ["BEGIN{v=\"\\u1f600\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 1 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0797",
            ["BEGIN{v=\"\\u002e\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0798",
            ["BEGIN{v=\"\\u005c\\u002e\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 1 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0799",
            ["BEGIN{v=\"\\u005bA\\u005d\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0800",
            ["BEGIN{v=\"\\u005eA\\u0024\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0801",
            ["BEGIN{v=\"\\u0028A\\u0029\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0802",
            ["BEGIN{v=\"\\u0041\\u002a\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0803",
            ["BEGIN{v=\"\\u0041\\u007c\\u0042\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0804",
            ["BEGIN{v=\"\\u005c\\u002a\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0805",
            ["BEGIN{v=\"\\u005c\\u005c\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0806",
            ["--posix", "BEGIN{v=\"\\u0041\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0807",
            ["--posix", "BEGIN{v=\"\\u0031\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0808",
            ["--posix", "BEGIN{v=\"\\u00e9\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0809",
            ["--posix", "BEGIN{v=\"\\u03b1\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0810",
            ["--posix", "BEGIN{v=\"\\u1f600\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0811",
            ["--posix", "BEGIN{v=\"\\u002e\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0812",
            ["--posix", "BEGIN{v=\"\\u005c\\u002e\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0813",
            ["--posix", "BEGIN{v=\"\\u005bA\\u005d\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0814",
            ["--posix", "BEGIN{v=\"\\u005eA\\u0024\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0815",
            ["--posix", "BEGIN{v=\"\\u0028A\\u0029\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0816",
            ["--posix", "BEGIN{v=\"\\u0041\\u002a\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0817",
            ["--posix", "BEGIN{v=\"\\u0041\\u007c\\u0042\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0818",
            ["--posix", "BEGIN{v=\"\\u005c\\u002a\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0819",
            ["--posix", "BEGIN{v=\"\\u005c\\u005c\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0820",
            ["--traditional", "BEGIN{v=\"\\u0041\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0821",
            ["--traditional", "BEGIN{v=\"\\u0031\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0822",
            ["--traditional", "BEGIN{v=\"\\u00e9\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0823",
            ["--traditional", "BEGIN{v=\"\\u03b1\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0824",
            ["--traditional", "BEGIN{v=\"\\u1f600\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 1 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0825",
            ["--traditional", "BEGIN{v=\"\\u002e\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0826",
            ["--traditional", "BEGIN{v=\"\\u005c\\u002e\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 1 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0827",
            ["--traditional", "BEGIN{v=\"\\u005bA\\u005d\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0828",
            ["--traditional", "BEGIN{v=\"\\u005eA\\u0024\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0829",
            ["--traditional", "BEGIN{v=\"\\u0028A\\u0029\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0830",
            ["--traditional", "BEGIN{v=\"\\u0041\\u002a\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 1 1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0831",
            ["--traditional", "BEGIN{v=\"\\u0041\\u007c\\u0042\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 0 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0832",
            ["--traditional", "BEGIN{v=\"\\u005c\\u002a\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0833",
            ["--traditional", "BEGIN{v=\"\\u005c\\u005c\";print (\"A\"~v),(\"1\"~v),(\"\u00e9\"~v),(\"\u03b1\"~v),(\"\ud83d\ude00\"~v),(\".\"~v),(\"*\"~v),(\"\\\\\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0834",
            ["BEGIN{print (\"a)\"~/)/),(\"(a)\"~/)/),(\"a))b\"~/)/),(\"A\"~/)/),(\"]\"~/)/),(\"(\"~/)/),(\")\"~/)/),(\"\"~/)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0835",
            ["BEGIN{print (\"a)\"~/a)/),(\"(a)\"~/a)/),(\"a))b\"~/a)/),(\"A\"~/a)/),(\"]\"~/a)/),(\"(\"~/a)/),(\")\"~/a)/),(\"\"~/a)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0836",
            ["BEGIN{print (\"a)\"~/(a))/),(\"(a)\"~/(a))/),(\"a))b\"~/(a))/),(\"A\"~/(a))/),(\"]\"~/(a))/),(\"(\"~/(a))/),(\")\"~/(a))/),(\"\"~/(a))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0837",
            ["BEGIN{print (\"a)\"~/((a)))/),(\"(a)\"~/((a)))/),(\"a))b\"~/((a)))/),(\"A\"~/((a)))/),(\"]\"~/((a)))/),(\"(\"~/((a)))/),(\")\"~/((a)))/),(\"\"~/((a)))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0838",
            ["BEGIN{print (\"a)\"~/[)]/),(\"(a)\"~/[)]/),(\"a))b\"~/[)]/),(\"A\"~/[)]/),(\"]\"~/[)]/),(\"(\"~/[)]/),(\")\"~/[)]/),(\"\"~/[)]/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0839",
            ["BEGIN{print (\"a)\"~/[(]/),(\"(a)\"~/[(]/),(\"a))b\"~/[(]/),(\"A\"~/[(]/),(\"]\"~/[(]/),(\"(\"~/[(]/),(\")\"~/[(]/),(\"\"~/[(]/);exit}"], [])
            .WithStdOut("0 1 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0840",
            ["BEGIN{print (\"a)\"~/[])]/),(\"(a)\"~/[])]/),(\"a))b\"~/[])]/),(\"A\"~/[])]/),(\"]\"~/[])]/),(\"(\"~/[])]/),(\")\"~/[])]/),(\"\"~/[])]/);exit}"], [])
            .WithStdOut("1 1 1 0 1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0841",
            ["BEGIN{print (\"a)\"~/[()]/),(\"(a)\"~/[()]/),(\"a))b\"~/[()]/),(\"A\"~/[()]/),(\"]\"~/[()]/),(\"(\"~/[()]/),(\")\"~/[()]/),(\"\"~/[()]/);exit}"], [])
            .WithStdOut("1 1 1 0 0 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0842",
            ["BEGIN{print (\"a)\"~/[[:punct:])]/),(\"(a)\"~/[[:punct:])]/),(\"a))b\"~/[[:punct:])]/),(\"A\"~/[[:punct:])]/),(\"]\"~/[[:punct:])]/),(\"(\"~/[[:punct:])]/),(\")\"~/[[:punct:])]/),(\"\"~/[[:punct:])]/);exit}"], [])
            .WithStdOut("1 1 1 0 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0843",
            ["BEGIN{print (\"a)\"~/(a)?)+/),(\"(a)\"~/(a)?)+/),(\"a))b\"~/(a)?)+/),(\"A\"~/(a)?)+/),(\"]\"~/(a)?)+/),(\"(\"~/(a)?)+/),(\")\"~/(a)?)+/),(\"\"~/(a)?)+/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0844",
            ["BEGIN{print (\"a)\"~/(a|b))/),(\"(a)\"~/(a|b))/),(\"a))b\"~/(a|b))/),(\"A\"~/(a|b))/),(\"]\"~/(a|b))/),(\"(\"~/(a|b))/),(\")\"~/(a|b))/),(\"\"~/(a|b))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0845",
            ["BEGIN{print (\"a)\"~/\\(a)/),(\"(a)\"~/\\(a)/),(\"a))b\"~/\\(a)/),(\"A\"~/\\(a)/),(\"]\"~/\\(a)/),(\"(\"~/\\(a)/),(\")\"~/\\(a)/),(\"\"~/\\(a)/);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0846",
            ["BEGIN{print (\"a)\"~/\\050a)/),(\"(a)\"~/\\050a)/),(\"a))b\"~/\\050a)/),(\"A\"~/\\050a)/),(\"]\"~/\\050a)/),(\"(\"~/\\050a)/),(\")\"~/\\050a)/),(\"\"~/\\050a)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0847",
            ["BEGIN{print (\"a)\"~/\\051+/),(\"(a)\"~/\\051+/),(\"a))b\"~/\\051+/),(\"A\"~/\\051+/),(\"]\"~/\\051+/),(\"(\"~/\\051+/),(\")\"~/\\051+/),(\"\"~/\\051+/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0848",
            ["BEGIN{print (\"a)\"~/([)])/),(\"(a)\"~/([)])/),(\"a))b\"~/([)])/),(\"A\"~/([)])/),(\"]\"~/([)])/),(\"(\"~/([)])/),(\")\"~/([)])/),(\"\"~/([)])/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0849",
            ["BEGIN{print (\"a)\"~/a))b/),(\"(a)\"~/a))b/),(\"a))b\"~/a))b/),(\"A\"~/a))b/),(\"]\"~/a))b/),(\"(\"~/a))b/),(\")\"~/a))b/),(\"\"~/a))b/);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0850",
            ["BEGIN{print (\"a)\"~/[^)]/),(\"(a)\"~/[^)]/),(\"a))b\"~/[^)]/),(\"A\"~/[^)]/),(\"]\"~/[^)]/),(\"(\"~/[^)]/),(\")\"~/[^)]/),(\"\"~/[^)]/);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0851",
            ["BEGIN{print (\"a)\"~/\\)/),(\"(a)\"~/\\)/),(\"a))b\"~/\\)/),(\"A\"~/\\)/),(\"]\"~/\\)/),(\"(\"~/\\)/),(\")\"~/\\)/),(\"\"~/\\)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0852",
            ["BEGIN{v=\")\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0853",
            ["BEGIN{v=\"a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0854",
            ["BEGIN{v=\"(a))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0855",
            ["BEGIN{v=\"((a)))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0856",
            ["BEGIN{v=\"[)]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0857",
            ["BEGIN{v=\"[(]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0858",
            ["BEGIN{v=\"[])]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0859",
            ["BEGIN{v=\"[()]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0860",
            ["BEGIN{v=\"[[:punct:])]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0861",
            ["BEGIN{v=\"(a)?)+\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0862",
            ["BEGIN{v=\"(a|b))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0863",
            ["BEGIN{v=\"\\\\(a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0864",
            ["BEGIN{v=\"\\\\050a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0865",
            ["BEGIN{v=\"\\\\051+\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0866",
            ["BEGIN{v=\"([)])\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0867",
            ["BEGIN{v=\"a))b\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0868",
            ["BEGIN{v=\"[^)]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0869",
            ["BEGIN{v=\"\\\\)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0870",
            ["--posix", "BEGIN{print (\"a)\"~/)/),(\"(a)\"~/)/),(\"a))b\"~/)/),(\"A\"~/)/),(\"]\"~/)/),(\"(\"~/)/),(\")\"~/)/),(\"\"~/)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0871",
            ["--posix", "BEGIN{print (\"a)\"~/a)/),(\"(a)\"~/a)/),(\"a))b\"~/a)/),(\"A\"~/a)/),(\"]\"~/a)/),(\"(\"~/a)/),(\")\"~/a)/),(\"\"~/a)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0872",
            ["--posix", "BEGIN{print (\"a)\"~/(a))/),(\"(a)\"~/(a))/),(\"a))b\"~/(a))/),(\"A\"~/(a))/),(\"]\"~/(a))/),(\"(\"~/(a))/),(\")\"~/(a))/),(\"\"~/(a))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0873",
            ["--posix", "BEGIN{print (\"a)\"~/((a)))/),(\"(a)\"~/((a)))/),(\"a))b\"~/((a)))/),(\"A\"~/((a)))/),(\"]\"~/((a)))/),(\"(\"~/((a)))/),(\")\"~/((a)))/),(\"\"~/((a)))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0874",
            ["--posix", "BEGIN{print (\"a)\"~/[)]/),(\"(a)\"~/[)]/),(\"a))b\"~/[)]/),(\"A\"~/[)]/),(\"]\"~/[)]/),(\"(\"~/[)]/),(\")\"~/[)]/),(\"\"~/[)]/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0875",
            ["--posix", "BEGIN{print (\"a)\"~/[(]/),(\"(a)\"~/[(]/),(\"a))b\"~/[(]/),(\"A\"~/[(]/),(\"]\"~/[(]/),(\"(\"~/[(]/),(\")\"~/[(]/),(\"\"~/[(]/);exit}"], [])
            .WithStdOut("0 1 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0876",
            ["--posix", "BEGIN{print (\"a)\"~/[])]/),(\"(a)\"~/[])]/),(\"a))b\"~/[])]/),(\"A\"~/[])]/),(\"]\"~/[])]/),(\"(\"~/[])]/),(\")\"~/[])]/),(\"\"~/[])]/);exit}"], [])
            .WithStdOut("1 1 1 0 1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0877",
            ["--posix", "BEGIN{print (\"a)\"~/[()]/),(\"(a)\"~/[()]/),(\"a))b\"~/[()]/),(\"A\"~/[()]/),(\"]\"~/[()]/),(\"(\"~/[()]/),(\")\"~/[()]/),(\"\"~/[()]/);exit}"], [])
            .WithStdOut("1 1 1 0 0 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0878",
            ["--posix", "BEGIN{print (\"a)\"~/[[:punct:])]/),(\"(a)\"~/[[:punct:])]/),(\"a))b\"~/[[:punct:])]/),(\"A\"~/[[:punct:])]/),(\"]\"~/[[:punct:])]/),(\"(\"~/[[:punct:])]/),(\")\"~/[[:punct:])]/),(\"\"~/[[:punct:])]/);exit}"], [])
            .WithStdOut("1 1 1 0 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0879",
            ["--posix", "BEGIN{print (\"a)\"~/(a)?)+/),(\"(a)\"~/(a)?)+/),(\"a))b\"~/(a)?)+/),(\"A\"~/(a)?)+/),(\"]\"~/(a)?)+/),(\"(\"~/(a)?)+/),(\")\"~/(a)?)+/),(\"\"~/(a)?)+/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0880",
            ["--posix", "BEGIN{print (\"a)\"~/(a|b))/),(\"(a)\"~/(a|b))/),(\"a))b\"~/(a|b))/),(\"A\"~/(a|b))/),(\"]\"~/(a|b))/),(\"(\"~/(a|b))/),(\")\"~/(a|b))/),(\"\"~/(a|b))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0881",
            ["--posix", "BEGIN{print (\"a)\"~/\\(a)/),(\"(a)\"~/\\(a)/),(\"a))b\"~/\\(a)/),(\"A\"~/\\(a)/),(\"]\"~/\\(a)/),(\"(\"~/\\(a)/),(\")\"~/\\(a)/),(\"\"~/\\(a)/);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0882",
            ["--posix", "BEGIN{print (\"a)\"~/\\050a)/),(\"(a)\"~/\\050a)/),(\"a))b\"~/\\050a)/),(\"A\"~/\\050a)/),(\"]\"~/\\050a)/),(\"(\"~/\\050a)/),(\")\"~/\\050a)/),(\"\"~/\\050a)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0883",
            ["--posix", "BEGIN{print (\"a)\"~/\\051+/),(\"(a)\"~/\\051+/),(\"a))b\"~/\\051+/),(\"A\"~/\\051+/),(\"]\"~/\\051+/),(\"(\"~/\\051+/),(\")\"~/\\051+/),(\"\"~/\\051+/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0884",
            ["--posix", "BEGIN{print (\"a)\"~/([)])/),(\"(a)\"~/([)])/),(\"a))b\"~/([)])/),(\"A\"~/([)])/),(\"]\"~/([)])/),(\"(\"~/([)])/),(\")\"~/([)])/),(\"\"~/([)])/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0885",
            ["--posix", "BEGIN{print (\"a)\"~/a))b/),(\"(a)\"~/a))b/),(\"a))b\"~/a))b/),(\"A\"~/a))b/),(\"]\"~/a))b/),(\"(\"~/a))b/),(\")\"~/a))b/),(\"\"~/a))b/);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0886",
            ["--posix", "BEGIN{print (\"a)\"~/[^)]/),(\"(a)\"~/[^)]/),(\"a))b\"~/[^)]/),(\"A\"~/[^)]/),(\"]\"~/[^)]/),(\"(\"~/[^)]/),(\")\"~/[^)]/),(\"\"~/[^)]/);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0887",
            ["--posix", "BEGIN{print (\"a)\"~/\\)/),(\"(a)\"~/\\)/),(\"a))b\"~/\\)/),(\"A\"~/\\)/),(\"]\"~/\\)/),(\"(\"~/\\)/),(\")\"~/\\)/),(\"\"~/\\)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0888",
            ["--posix", "BEGIN{v=\")\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0889",
            ["--posix", "BEGIN{v=\"a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0890",
            ["--posix", "BEGIN{v=\"(a))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0891",
            ["--posix", "BEGIN{v=\"((a)))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0892",
            ["--posix", "BEGIN{v=\"[)]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0893",
            ["--posix", "BEGIN{v=\"[(]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0894",
            ["--posix", "BEGIN{v=\"[])]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0895",
            ["--posix", "BEGIN{v=\"[()]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0896",
            ["--posix", "BEGIN{v=\"[[:punct:])]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0897",
            ["--posix", "BEGIN{v=\"(a)?)+\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0898",
            ["--posix", "BEGIN{v=\"(a|b))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0899",
            ["--posix", "BEGIN{v=\"\\\\(a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0900",
            ["--posix", "BEGIN{v=\"\\\\050a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0901",
            ["--posix", "BEGIN{v=\"\\\\051+\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0902",
            ["--posix", "BEGIN{v=\"([)])\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0903",
            ["--posix", "BEGIN{v=\"a))b\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0904",
            ["--posix", "BEGIN{v=\"[^)]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0905",
            ["--posix", "BEGIN{v=\"\\\\)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0906",
            ["--traditional", "BEGIN{print (\"a)\"~/)/),(\"(a)\"~/)/),(\"a))b\"~/)/),(\"A\"~/)/),(\"]\"~/)/),(\"(\"~/)/),(\")\"~/)/),(\"\"~/)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0907",
            ["--traditional", "BEGIN{print (\"a)\"~/a)/),(\"(a)\"~/a)/),(\"a))b\"~/a)/),(\"A\"~/a)/),(\"]\"~/a)/),(\"(\"~/a)/),(\")\"~/a)/),(\"\"~/a)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0908",
            ["--traditional", "BEGIN{print (\"a)\"~/(a))/),(\"(a)\"~/(a))/),(\"a))b\"~/(a))/),(\"A\"~/(a))/),(\"]\"~/(a))/),(\"(\"~/(a))/),(\")\"~/(a))/),(\"\"~/(a))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0909",
            ["--traditional", "BEGIN{print (\"a)\"~/((a)))/),(\"(a)\"~/((a)))/),(\"a))b\"~/((a)))/),(\"A\"~/((a)))/),(\"]\"~/((a)))/),(\"(\"~/((a)))/),(\")\"~/((a)))/),(\"\"~/((a)))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0910",
            ["--traditional", "BEGIN{print (\"a)\"~/[)]/),(\"(a)\"~/[)]/),(\"a))b\"~/[)]/),(\"A\"~/[)]/),(\"]\"~/[)]/),(\"(\"~/[)]/),(\")\"~/[)]/),(\"\"~/[)]/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0911",
            ["--traditional", "BEGIN{print (\"a)\"~/[(]/),(\"(a)\"~/[(]/),(\"a))b\"~/[(]/),(\"A\"~/[(]/),(\"]\"~/[(]/),(\"(\"~/[(]/),(\")\"~/[(]/),(\"\"~/[(]/);exit}"], [])
            .WithStdOut("0 1 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0912",
            ["--traditional", "BEGIN{print (\"a)\"~/[])]/),(\"(a)\"~/[])]/),(\"a))b\"~/[])]/),(\"A\"~/[])]/),(\"]\"~/[])]/),(\"(\"~/[])]/),(\")\"~/[])]/),(\"\"~/[])]/);exit}"], [])
            .WithStdOut("1 1 1 0 1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0913",
            ["--traditional", "BEGIN{print (\"a)\"~/[()]/),(\"(a)\"~/[()]/),(\"a))b\"~/[()]/),(\"A\"~/[()]/),(\"]\"~/[()]/),(\"(\"~/[()]/),(\")\"~/[()]/),(\"\"~/[()]/);exit}"], [])
            .WithStdOut("1 1 1 0 0 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0914",
            ["--traditional", "BEGIN{print (\"a)\"~/[[:punct:])]/),(\"(a)\"~/[[:punct:])]/),(\"a))b\"~/[[:punct:])]/),(\"A\"~/[[:punct:])]/),(\"]\"~/[[:punct:])]/),(\"(\"~/[[:punct:])]/),(\")\"~/[[:punct:])]/),(\"\"~/[[:punct:])]/);exit}"], [])
            .WithStdOut("1 1 1 0 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0915",
            ["--traditional", "BEGIN{print (\"a)\"~/(a)?)+/),(\"(a)\"~/(a)?)+/),(\"a))b\"~/(a)?)+/),(\"A\"~/(a)?)+/),(\"]\"~/(a)?)+/),(\"(\"~/(a)?)+/),(\")\"~/(a)?)+/),(\"\"~/(a)?)+/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0916",
            ["--traditional", "BEGIN{print (\"a)\"~/(a|b))/),(\"(a)\"~/(a|b))/),(\"a))b\"~/(a|b))/),(\"A\"~/(a|b))/),(\"]\"~/(a|b))/),(\"(\"~/(a|b))/),(\")\"~/(a|b))/),(\"\"~/(a|b))/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0917",
            ["--traditional", "BEGIN{print (\"a)\"~/\\(a)/),(\"(a)\"~/\\(a)/),(\"a))b\"~/\\(a)/),(\"A\"~/\\(a)/),(\"]\"~/\\(a)/),(\"(\"~/\\(a)/),(\")\"~/\\(a)/),(\"\"~/\\(a)/);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0918",
            ["--traditional", "BEGIN{print (\"a)\"~/\\050a)/),(\"(a)\"~/\\050a)/),(\"a))b\"~/\\050a)/),(\"A\"~/\\050a)/),(\"]\"~/\\050a)/),(\"(\"~/\\050a)/),(\")\"~/\\050a)/),(\"\"~/\\050a)/);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0919",
            ["--traditional", "BEGIN{print (\"a)\"~/\\051+/),(\"(a)\"~/\\051+/),(\"a))b\"~/\\051+/),(\"A\"~/\\051+/),(\"]\"~/\\051+/),(\"(\"~/\\051+/),(\")\"~/\\051+/),(\"\"~/\\051+/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0920",
            ["--traditional", "BEGIN{print (\"a)\"~/([)])/),(\"(a)\"~/([)])/),(\"a))b\"~/([)])/),(\"A\"~/([)])/),(\"]\"~/([)])/),(\"(\"~/([)])/),(\")\"~/([)])/),(\"\"~/([)])/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0921",
            ["--traditional", "BEGIN{print (\"a)\"~/a))b/),(\"(a)\"~/a))b/),(\"a))b\"~/a))b/),(\"A\"~/a))b/),(\"]\"~/a))b/),(\"(\"~/a))b/),(\")\"~/a))b/),(\"\"~/a))b/);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0922",
            ["--traditional", "BEGIN{print (\"a)\"~/[^)]/),(\"(a)\"~/[^)]/),(\"a))b\"~/[^)]/),(\"A\"~/[^)]/),(\"]\"~/[^)]/),(\"(\"~/[^)]/),(\")\"~/[^)]/),(\"\"~/[^)]/);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-literal-0923",
            ["--traditional", "BEGIN{print (\"a)\"~/\\)/),(\"(a)\"~/\\)/),(\"a))b\"~/\\)/),(\"A\"~/\\)/),(\"]\"~/\\)/),(\"(\"~/\\)/),(\")\"~/\\)/),(\"\"~/\\)/);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0924",
            ["--traditional", "BEGIN{v=\")\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0925",
            ["--traditional", "BEGIN{v=\"a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0926",
            ["--traditional", "BEGIN{v=\"(a))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0927",
            ["--traditional", "BEGIN{v=\"((a)))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0928",
            ["--traditional", "BEGIN{v=\"[)]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0929",
            ["--traditional", "BEGIN{v=\"[(]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0930",
            ["--traditional", "BEGIN{v=\"[])]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0931",
            ["--traditional", "BEGIN{v=\"[()]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0932",
            ["--traditional", "BEGIN{v=\"[[:punct:])]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 1 1 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0933",
            ["--traditional", "BEGIN{v=\"(a)?)+\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0934",
            ["--traditional", "BEGIN{v=\"(a|b))\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0935",
            ["--traditional", "BEGIN{v=\"\\\\(a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0936",
            ["--traditional", "BEGIN{v=\"\\\\050a)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 1 0 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0937",
            ["--traditional", "BEGIN{v=\"\\\\051+\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0938",
            ["--traditional", "BEGIN{v=\"([)])\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0939",
            ["--traditional", "BEGIN{v=\"a))b\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 1 0 0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0940",
            ["--traditional", "BEGIN{v=\"[^)]\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 1 1 1 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-parentheses-dynamic-0941",
            ["--traditional", "BEGIN{v=\"\\\\)\";print (\"a)\"~v),(\"(a)\"~v),(\"a))b\"~v),(\"A\"~v),(\"]\"~v),(\"(\"~v),(\")\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 1 1 0 0 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0942",
            ["BEGIN{v=\"\\u0000005bA\\u005d\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0943",
            ["BEGIN{v=\"\\u0000005eA\\u0024\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0944",
            ["BEGIN{v=\"\\u00000028A\\u0029\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0945",
            ["--posix", "BEGIN{v=\"\\u0000005bA\\u005d\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0946",
            ["--posix", "BEGIN{v=\"\\u0000005eA\\u0024\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0947",
            ["--posix", "BEGIN{v=\"\\u00000028A\\u0029\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("0 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0948",
            ["--traditional", "BEGIN{v=\"\\u0000005bA\\u005d\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0949",
            ["--traditional", "BEGIN{v=\"\\u0000005eA\\u0024\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 0 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-string-pattern-0950",
            ["--traditional", "BEGIN{v=\"\\u00000028A\\u0029\";print (\"A\"~v),(\"B\"~v),(\"AA\"~v),(\"\"~v);exit}"], [])
            .WithStdOut("1 0 1 0\n").WithStdErr("").ExitsWith(0).Build();
    }
}
