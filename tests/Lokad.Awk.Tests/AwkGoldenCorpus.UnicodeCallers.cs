namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UnicodeCallerCases()
    {
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0000",
            ["-v", "v=\\u", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0002",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("start\n1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0003",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0004",
            ["BEGIN{ARGV[1]=\"\\\\u\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0005",
            ["-v", "v=\\ug", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0007",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("start\n2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0008",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0009",
            ["BEGIN{ARGV[1]=\"\\\\ug\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0010",
            ["-v", "v=\\u0", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0012",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0013",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0014",
            ["BEGIN{ARGV[1]=\"\\\\u0\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0015",
            ["-v", "v=\\u0000", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0017",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0018",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0019",
            ["BEGIN{ARGV[1]=\"\\\\u0000\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0020",
            ["-v", "v=\\u1", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0022",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0023",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0024",
            ["BEGIN{ARGV[1]=\"\\\\u1\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0025",
            ["-v", "v=\\u41", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0027",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0028",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0029",
            ["BEGIN{ARGV[1]=\"\\\\u41\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0030",
            ["-v", "v=\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0032",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0033",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0034",
            ["BEGIN{ARGV[1]=\"\\\\u0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0035",
            ["-v", "v=\\u00000041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0037",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0038",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0039",
            ["BEGIN{ARGV[1]=\"\\\\u00000041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0040",
            ["-v", "v=\\u000000041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0042",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("start\n2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0043",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0044",
            ["BEGIN{ARGV[1]=\"\\\\u000000041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0045",
            ["-v", "v=\\u0041a", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0047",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0048",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0049",
            ["BEGIN{ARGV[1]=\"\\\\u0041a\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0050",
            ["-v", "v=\\u03b1", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0052",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0053",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0054",
            ["BEGIN{ARGV[1]=\"\\\\u03b1\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0055",
            ["-v", "v=\\u1f600", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0057",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0058",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0059",
            ["BEGIN{ARGV[1]=\"\\\\u1f600\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0060",
            ["-v", "v=\\u10ffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0062",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0063",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0064",
            ["BEGIN{ARGV[1]=\"\\\\u10ffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0065",
            ["-v", "v=\\ud800", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0067",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0068",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0069",
            ["BEGIN{ARGV[1]=\"\\\\ud800\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0070",
            ["-v", "v=\\udfff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0072",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0073",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0074",
            ["BEGIN{ARGV[1]=\"\\\\udfff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0075",
            ["-v", "v=\\u110000", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0077",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0078",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0079",
            ["BEGIN{ARGV[1]=\"\\\\u110000\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0080",
            ["-v", "v=\\u7fffffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0082",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0083",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0084",
            ["BEGIN{ARGV[1]=\"\\\\u7fffffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0085",
            ["-v", "v=\\uffffffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0087",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0088",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0089",
            ["BEGIN{ARGV[1]=\"\\\\uffffffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0090",
            ["-v", "v=\\\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0092",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("start\n6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0093",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0094",
            ["BEGIN{ARGV[1]=\"\\\\\\\\u0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0095",
            ["-v", "v=\\u005cu0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0097",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("start\n6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0098",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0099",
            ["BEGIN{ARGV[1]=\"\\\\u005cu0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0103",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0108",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0110",
            ["-v", "v=\\x41", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0112",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0113",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0114",
            ["BEGIN{ARGV[1]=\"\\\\x41\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0118",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0120",
            ["-v", "v=\\101", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0122",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0123",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0124",
            ["BEGIN{ARGV[1]=\"\\\\101\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0125",
            ["-v", "v=\\n", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0127",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0128",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0129",
            ["BEGIN{ARGV[1]=\"\\\\n\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0130",
            ["-v", "v=\\t", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0132",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0133",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0134",
            ["BEGIN{ARGV[1]=\"\\\\t\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0135",
            ["-v", "v=\\\\n", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0137",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("start\n2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0138",
            ["BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0139",
            ["BEGIN{ARGV[1]=\"\\\\\\\\n\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0140",
            ["-F", "\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0142",
            ["-F", "\\u1f600", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0144",
            ["-F", "\\u", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0146",
            ["-F", "\\ud800", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0148",
            ["-F", "\\x41", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0152",
            ["-F", "\\\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0154",
            ["-F", "\\n", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0156",
            ["-F", "t", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0158",
            ["--posix", "-v", "v=\\u", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0159",
            ["-v", "v=\\u", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0160",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("start\n1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0161",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0162",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0163",
            ["--posix", "-v", "v=\\ug", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0164",
            ["-v", "v=\\ug", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0165",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("start\n2 [ug]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0166",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0167",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\ug\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0168",
            ["--posix", "-v", "v=\\u0", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [u0]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0169",
            ["-v", "v=\\u0", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [u0]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0170",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("start\n2 [u0]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0171",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0172",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u0\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("2 [u0]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0173",
            ["--posix", "-v", "v=\\u0000", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0174",
            ["-v", "v=\\u0000", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0175",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("start\n5 [u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0176",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0177",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u0000\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("5 [u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0178",
            ["--posix", "-v", "v=\\u1", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [u1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0179",
            ["-v", "v=\\u1", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [u1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0180",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("start\n2 [u1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0181",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0182",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u1\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("2 [u1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0183",
            ["--posix", "-v", "v=\\u41", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("3 [u41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0184",
            ["-v", "v=\\u41", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("3 [u41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0185",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("start\n3 [u41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0186",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0187",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u41\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("3 [u41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0188",
            ["--posix", "-v", "v=\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0189",
            ["-v", "v=\\u0041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0190",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("start\n5 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0191",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0192",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("5 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0193",
            ["--posix", "-v", "v=\\u00000041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("9 [u00000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0194",
            ["-v", "v=\\u00000041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("9 [u00000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0195",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("start\n9 [u00000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0196",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0197",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u00000041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("9 [u00000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0198",
            ["--posix", "-v", "v=\\u000000041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("10 [u000000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0199",
            ["-v", "v=\\u000000041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("10 [u000000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0200",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("start\n10 [u000000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0201",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0202",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u000000041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("10 [u000000041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0203",
            ["--posix", "-v", "v=\\u0041a", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [u0041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0204",
            ["-v", "v=\\u0041a", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [u0041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0205",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("start\n6 [u0041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0206",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0207",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u0041a\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("6 [u0041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0208",
            ["--posix", "-v", "v=\\u03b1", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0209",
            ["-v", "v=\\u03b1", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0210",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("start\n5 [u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0211",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0212",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u03b1\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("5 [u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0213",
            ["--posix", "-v", "v=\\u1f600", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0214",
            ["-v", "v=\\u1f600", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0215",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("start\n6 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0216",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0217",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u1f600\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("6 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0218",
            ["--posix", "-v", "v=\\u10ffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("7 [u10ffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0219",
            ["-v", "v=\\u10ffff", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("7 [u10ffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0220",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("start\n7 [u10ffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0221",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0222",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u10ffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("7 [u10ffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0223",
            ["--posix", "-v", "v=\\ud800", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0224",
            ["-v", "v=\\ud800", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0225",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("start\n5 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0226",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0227",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\ud800\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("5 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0228",
            ["--posix", "-v", "v=\\udfff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0229",
            ["-v", "v=\\udfff", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0230",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("start\n5 [udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0231",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0232",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\udfff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("5 [udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0233",
            ["--posix", "-v", "v=\\u110000", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("7 [u110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0234",
            ["-v", "v=\\u110000", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("7 [u110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0235",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("start\n7 [u110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0236",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0237",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u110000\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("7 [u110000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0238",
            ["--posix", "-v", "v=\\u7fffffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("9 [u7fffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0239",
            ["-v", "v=\\u7fffffff", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("9 [u7fffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0240",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("start\n9 [u7fffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0241",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0242",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u7fffffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("9 [u7fffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0243",
            ["--posix", "-v", "v=\\uffffffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("9 [uffffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0244",
            ["-v", "v=\\uffffffff", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("9 [uffffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0245",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("start\n9 [uffffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0246",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0247",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\uffffffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("9 [uffffffff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0248",
            ["--posix", "-v", "v=\\\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0249",
            ["-v", "v=\\\\u0041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0250",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("start\n6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0251",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0252",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\\\\\u0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0253",
            ["--posix", "-v", "v=\\u005cu0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("10 [u005cu0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0254",
            ["-v", "v=\\u005cu0041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("10 [u005cu0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0255",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("start\n10 [u005cu0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0256",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0257",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\u005cu0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("10 [u005cu0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0261",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0266",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0268",
            ["--posix", "-v", "v=\\x41", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("3 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0269",
            ["-v", "v=\\x41", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("3 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0270",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("start\n3 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0271",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0272",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\x41\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("3 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0273",
            ["--posix", "-v", "v=\\x", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0274",
            ["-v", "v=\\x", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0275",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("start\n1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0276",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0277",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\x\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0278",
            ["--posix", "-v", "v=\\101", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0279",
            ["-v", "v=\\101", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0280",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0281",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0282",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\101\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0283",
            ["--posix", "-v", "v=\\n", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0284",
            ["-v", "v=\\n", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0285",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0286",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0287",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\n\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0288",
            ["--posix", "-v", "v=\\t", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0289",
            ["-v", "v=\\t", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0290",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0291",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0292",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\t\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0293",
            ["--posix", "-v", "v=\\\\n", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0294",
            ["-v", "v=\\\\n", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0295",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("start\n2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0296",
            ["--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0297",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\\\\\n\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0298",
            ["--posix", "-F", "\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0299",
            ["-F", "\\u0041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0300",
            ["--posix", "-F", "\\u1f600", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0301",
            ["-F", "\\u1f600", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [u1f600]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0302",
            ["--posix", "-F", "\\u", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0303",
            ["-F", "\\u", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0304",
            ["--posix", "-F", "\\ud800", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0305",
            ["-F", "\\ud800", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0306",
            ["--posix", "-F", "\\x41", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("3 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0307",
            ["-F", "\\x41", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("3 [x41]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0308",
            ["--posix", "-F", "\\x", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0309",
            ["-F", "\\x", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0310",
            ["--posix", "-F", "\\\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0311",
            ["-F", "\\\\u0041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0312",
            ["--posix", "-F", "\\n", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0313",
            ["-F", "\\n", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0314",
            ["--posix", "-F", "t", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0315",
            ["-F", "t", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0316",
            ["--traditional", "-v", "v=\\u", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0317",
            ["-v", "v=\\u", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0318",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("start\n1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0319",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0320",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0321",
            ["--traditional", "-v", "v=\\ug", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0322",
            ["-v", "v=\\ug", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0323",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("start\n2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0324",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0325",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\ug\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ug"], [])
            .WithStandardInput("").WithStdOut("2 [ug]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0326",
            ["--traditional", "-v", "v=\\u0", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0327",
            ["-v", "v=\\u0", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0328",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0329",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0330",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u0\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0331",
            ["--traditional", "-v", "v=\\u0000", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0332",
            ["-v", "v=\\u0000", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0333",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0334",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0335",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u0000\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0000"], [])
            .WithStandardInput("").WithStdOut("1 [\u0000]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0336",
            ["--traditional", "-v", "v=\\u1", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0337",
            ["-v", "v=\\u1", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0338",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0339",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0340",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u1\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1"], [])
            .WithStandardInput("").WithStdOut("1 [\u0001]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0341",
            ["--traditional", "-v", "v=\\u41", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0342",
            ["-v", "v=\\u41", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0343",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0344",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0345",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u41\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u41"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0346",
            ["--traditional", "-v", "v=\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0347",
            ["-v", "v=\\u0041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0348",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0349",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0350",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0351",
            ["--traditional", "-v", "v=\\u00000041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0352",
            ["-v", "v=\\u00000041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0353",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0354",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0355",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u00000041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u00000041"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0356",
            ["--traditional", "-v", "v=\\u000000041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0357",
            ["-v", "v=\\u000000041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0358",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("start\n2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0359",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0360",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u000000041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u000000041"], [])
            .WithStandardInput("").WithStdOut("2 [\u00041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0361",
            ["--traditional", "-v", "v=\\u0041a", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0362",
            ["-v", "v=\\u0041a", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0363",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0364",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0365",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u0041a\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u0041a"], [])
            .WithStandardInput("").WithStdOut("1 [\u041a]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0366",
            ["--traditional", "-v", "v=\\u03b1", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0367",
            ["-v", "v=\\u03b1", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0368",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0369",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0370",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u03b1\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u03b1"], [])
            .WithStandardInput("").WithStdOut("1 [\u03b1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0371",
            ["--traditional", "-v", "v=\\u1f600", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0372",
            ["-v", "v=\\u1f600", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0373",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0374",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0375",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u1f600\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u1f600"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0376",
            ["--traditional", "-v", "v=\\u10ffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0377",
            ["-v", "v=\\u10ffff", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0378",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0379",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0380",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u10ffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u10ffff"], [])
            .WithStandardInput("").WithStdOut("1 [\udbff\udfff]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0381",
            ["--traditional", "-v", "v=\\ud800", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0382",
            ["-v", "v=\\ud800", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0383",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0384",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0385",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\ud800\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\ud800"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0386",
            ["--traditional", "-v", "v=\\udfff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0387",
            ["-v", "v=\\udfff", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0388",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0389",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0390",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\udfff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\udfff"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0391",
            ["--traditional", "-v", "v=\\u110000", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0392",
            ["-v", "v=\\u110000", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0393",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0394",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0395",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u110000\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u110000"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0396",
            ["--traditional", "-v", "v=\\u7fffffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0397",
            ["-v", "v=\\u7fffffff", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0398",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0399",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0400",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u7fffffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u7fffffff"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0401",
            ["--traditional", "-v", "v=\\uffffffff", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0402",
            ["-v", "v=\\uffffffff", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0403",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("start\n1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0404",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0405",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\uffffffff\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\uffffffff"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0406",
            ["--traditional", "-v", "v=\\\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0407",
            ["-v", "v=\\\\u0041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0408",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("start\n6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0409",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0410",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\\\\\u0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\u0041"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0411",
            ["--traditional", "-v", "v=\\u005cu0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0412",
            ["-v", "v=\\u005cu0041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0413",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("start\n6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0414",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0415",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\u005cu0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\u005cu0041"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0419",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0424",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0426",
            ["--traditional", "-v", "v=\\x41", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0427",
            ["-v", "v=\\x41", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0428",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0429",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0430",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\x41\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x41"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0434",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0436",
            ["--traditional", "-v", "v=\\101", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0437",
            ["-v", "v=\\101", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0438",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("start\n1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0439",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0440",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\101\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\101"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0441",
            ["--traditional", "-v", "v=\\n", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0442",
            ["-v", "v=\\n", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0443",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0444",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0445",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\n\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\n"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0446",
            ["--traditional", "-v", "v=\\t", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0447",
            ["-v", "v=\\t", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0448",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("start\n1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0449",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0450",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\t\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\t"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0451",
            ["--traditional", "-v", "v=\\\\n", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0452",
            ["-v", "v=\\\\n", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0453",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("start\n2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-after-BEGINEXIT-0454",
            ["--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("0 []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0455",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\\\\\n\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\\\n"], [])
            .WithStandardInput("").WithStdOut("2 [\\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0456",
            ["--traditional", "-F", "\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0457",
            ["-F", "\\u0041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0458",
            ["--traditional", "-F", "\\u1f600", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0459",
            ["-F", "\\u1f600", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\ud83d\ude00]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0460",
            ["--traditional", "-F", "\\u", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0461",
            ["-F", "\\u", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [u]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0462",
            ["--traditional", "-F", "\\ud800", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0463",
            ["-F", "\\ud800", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [?]\n").WithStdErr("awk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0464",
            ["--traditional", "-F", "\\x41", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0465",
            ["-F", "\\x41", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [A]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0468",
            ["--traditional", "-F", "\\\\u0041", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0469",
            ["-F", "\\\\u0041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("6 [\\u0041]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0470",
            ["--traditional", "-F", "\\n", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0471",
            ["-F", "\\n", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\n]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0472",
            ["--traditional", "-F", "t", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0473",
            ["-F", "t", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [\t]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0474",
            ["-v", "a=\\u", "-v", "b=\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0476",
            ["-v", "a=\\u", "BEGIN{print \"\\u\";exit}"], [])
            .WithStandardInput("").WithStdOut("u\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0478",
            ["BEGIN{print \"start\"}END{print \"ok\"}", "a=\\u", "b=\\u"], [])
            .WithStandardInput("").WithStdOut("start\nok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0480",
            ["BEGIN{print \"\\u\"}END{print \"ok\"}", "a=\\u"], [])
            .WithStandardInput("").WithStdOut("u\nok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0482",
            ["-v", "a=\\u", "BEGIN{print \"start\";print 1/z}"], [])
            .WithStandardInput("").WithStdOut("start\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0483",
            ["BEGIN{print \"start\";print 1/z}", "a=\\u"], [])
            .WithStandardInput("").WithStdOut("start\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0484",
            ["--posix", "-v", "a=\\u", "-v", "b=\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0486",
            ["--posix", "-v", "a=\\u", "BEGIN{print \"\\u\";exit}"], [])
            .WithStandardInput("").WithStdOut("u\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0488",
            ["--posix", "BEGIN{print \"start\"}END{print \"ok\"}", "a=\\u", "b=\\u"], [])
            .WithStandardInput("").WithStdOut("start\nok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0490",
            ["--posix", "BEGIN{print \"\\u\"}END{print \"ok\"}", "a=\\u"], [])
            .WithStandardInput("").WithStdOut("u\nok\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0492",
            ["--posix", "-v", "a=\\u", "BEGIN{print \"start\";print 1/z}"], [])
            .WithStandardInput("").WithStdOut("start\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0493",
            ["--posix", "BEGIN{print \"start\";print 1/z}", "a=\\u"], [])
            .WithStandardInput("").WithStdOut("start\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0494",
            ["--traditional", "-v", "a=\\u", "-v", "b=\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0496",
            ["--traditional", "-v", "a=\\u", "BEGIN{print \"\\u\";exit}"], [])
            .WithStandardInput("").WithStdOut("u\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0498",
            ["--traditional", "BEGIN{print \"start\"}END{print \"ok\"}", "a=\\u", "b=\\u"], [])
            .WithStandardInput("").WithStdOut("start\nok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0500",
            ["--traditional", "BEGIN{print \"\\u\"}END{print \"ok\"}", "a=\\u"], [])
            .WithStandardInput("").WithStdOut("u\nok\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0502",
            ["--traditional", "-v", "a=\\u", "BEGIN{print \"start\";print 1/z}"], [])
            .WithStandardInput("").WithStdOut("start\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0503",
            ["--traditional", "BEGIN{print \"start\";print 1/z}", "a=\\u"], [])
            .WithStandardInput("").WithStdOut("start\n").WithStdErr("awk: division by zero attempted\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0504",
            ["@include \"\\u0041.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0505",
            ["@include \"\\u1f600.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0506",
            ["@include \"\\u00e9.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0507",
            ["@include \"\\u.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0508",
            ["@include \"\\ud800.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0509",
            ["@include \"\\U0041.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0510",
            ["@include \"\\x41.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0511",
            ["@include \"\\x.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0512",
            ["@include \"\\101.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0513",
            ["--posix", "@include \"\\u0041.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0514",
            ["--posix", "@include \"\\u1f600.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0515",
            ["--posix", "@include \"\\u00e9.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0516",
            ["--posix", "@include \"\\u.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0517",
            ["--posix", "@include \"\\ud800.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0518",
            ["--posix", "@include \"\\U0041.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0519",
            ["--posix", "@include \"\\x41.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0520",
            ["--posix", "@include \"\\x.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0521",
            ["--posix", "@include \"\\101.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0522",
            ["--traditional", "@include \"\\u0041.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0523",
            ["--traditional", "@include \"\\u1f600.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0524",
            ["--traditional", "@include \"\\u00e9.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0525",
            ["--traditional", "@include \"\\u.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0526",
            ["--traditional", "@include \"\\ud800.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0527",
            ["--traditional", "@include \"\\U0041.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0528",
            ["--traditional", "@include \"\\x41.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0529",
            ["--traditional", "@include \"\\x.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-0530",
            ["--traditional", "@include \"\\101.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-option-0531",
            ["-f", "\\u0041.awk"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-0532",
            ["-i", "\\u0041.awk", "BEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-option-0533",
            ["-f", "\\u1f600.awk"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-0534",
            ["-i", "\\u1f600.awk", "BEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-option-0535",
            ["--posix", "-f", "\\u0041.awk"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-0536",
            ["--posix", "-i", "\\u0041.awk", "BEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-option-0537",
            ["--posix", "-f", "\\u1f600.awk"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-0538",
            ["--posix", "-i", "\\u1f600.awk", "BEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-option-0539",
            ["--traditional", "-f", "\\u0041.awk"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-0540",
            ["--traditional", "-i", "\\u0041.awk", "BEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-option-0541",
            ["--traditional", "-f", "\\u1f600.awk"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-0542",
            ["--traditional", "-i", "\\u1f600.awk", "BEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid path\n").ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0543",
            ["-F", ",", "-v", "FS=;", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[;] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0544",
            ["-v", "FS=;", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0545",
            ["-F", "\\u", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0546",
            ["-v", "FS=\\u", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0547",
            ["-F", "\\u", "-v", "FS=,", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0548",
            ["-F", "\\u", "-v", "v=\\ud800", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[u] [?]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0549",
            ["@include \"A.awk\"\nBEGIN{print \"main\";exit}"], [new("/A.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0550",
            ["@include \"\u03b1.awk\"\nBEGIN{print \"main\";exit}"], [new("/\u03b1.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0551",
            ["@include \"\ud83d\ude00.awk\"\nBEGIN{print \"main\";exit}"], [new("/\ud83d\ude00.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0552",
            ["--posix", "-F", ",", "-v", "FS=;", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[;] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0553",
            ["--posix", "-v", "FS=;", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0554",
            ["--posix", "-F", "\\u", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0555",
            ["--posix", "-v", "FS=\\u", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0556",
            ["--posix", "-F", "\\u", "-v", "FS=,", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0557",
            ["--posix", "-F", "\\u", "-v", "v=\\ud800", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[u] [ud800]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0558",
            ["--posix", "@include \"A.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0559",
            ["--posix", "@include \"\u03b1.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0560",
            ["--posix", "@include \"\ud83d\ude00.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0561",
            ["--traditional", "-F", ",", "-v", "FS=;", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[;] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0562",
            ["--traditional", "-v", "FS=;", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0563",
            ["--traditional", "-F", "\\u", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0564",
            ["--traditional", "-v", "FS=\\u", "-F", ",", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0565",
            ["--traditional", "-F", "\\u", "-v", "FS=,", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[,] []\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-startup-order-0566",
            ["--traditional", "-F", "\\u", "-v", "v=\\ud800", "BEGIN{printf \"[%s] [%s]\\n\",FS,v;exit}"], [])
            .WithStandardInput("").WithStdOut("[u] [?]\n").WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0567",
            ["--traditional", "@include \"A.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0568",
            ["--traditional", "@include \"\u03b1.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-control-0569",
            ["--traditional", "@include \"\ud83d\ude00.awk\"\nBEGIN{print \"main\";exit}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: @include is a GNU extension\n").ExitsWith(2).WithTags([AwkGoldenTag.CompilerDiagnostic]).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0570",
            ["-f", "A.awk"], [new("/A.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0571",
            ["-i", "A.awk", "BEGIN{print \"main\";exit}"], [new("/A.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0572",
            ["-f", "\u03b1.awk"], [new("/\u03b1.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0573",
            ["-i", "\u03b1.awk", "BEGIN{print \"main\";exit}"], [new("/\u03b1.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0574",
            ["-f", "\ud83d\ude00.awk"], [new("/\ud83d\ude00.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0575",
            ["-i", "\ud83d\ude00.awk", "BEGIN{print \"main\";exit}"], [new("/\ud83d\ude00.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0576",
            ["-F", "00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0577",
            ["-v", "FS=00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0578",
            ["-F", "00", "-v", "FS=00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0579",
            ["END{print (v==1), \"[\" v \"]\"}", "v=\\061"], [])
            .WithStandardInput("").WithStdOut("1 [1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0580",
            ["END{print (v==1), \"[\" v \"]\"}", "v=\\x31"], [])
            .WithStandardInput("").WithStdOut("1 [1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0581",
            ["END{print (v==1), \"[\" v \"]\"}", "v=\\u0031"], [])
            .WithStandardInput("").WithStdOut("1 [1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0582",
            ["--posix", "-f", "A.awk"], [new("/A.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0583",
            ["--posix", "-i", "A.awk", "BEGIN{print \"main\";exit}"], [new("/A.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0584",
            ["--posix", "-f", "\u03b1.awk"], [new("/\u03b1.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0585",
            ["--posix", "-i", "\u03b1.awk", "BEGIN{print \"main\";exit}"], [new("/\u03b1.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0586",
            ["--posix", "-f", "\ud83d\ude00.awk"], [new("/\ud83d\ude00.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0587",
            ["--posix", "-i", "\ud83d\ude00.awk", "BEGIN{print \"main\";exit}"], [new("/\ud83d\ude00.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0588",
            ["--posix", "-F", "00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0589",
            ["--posix", "-v", "FS=00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0590",
            ["--posix", "-F", "00", "-v", "FS=00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0591",
            ["--posix", "END{print (v==1), \"[\" v \"]\"}", "v=\\061"], [])
            .WithStandardInput("").WithStdOut("1 [1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0592",
            ["--posix", "END{print (v==1), \"[\" v \"]\"}", "v=\\x31"], [])
            .WithStandardInput("").WithStdOut("0 [x31]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0593",
            ["--posix", "END{print (v==1), \"[\" v \"]\"}", "v=\\u0031"], [])
            .WithStandardInput("").WithStdOut("0 [u0031]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0594",
            ["--traditional", "-f", "A.awk"], [new("/A.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0595",
            ["--traditional", "-i", "A.awk", "BEGIN{print \"main\";exit}"], [new("/A.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0596",
            ["--traditional", "-f", "\u03b1.awk"], [new("/\u03b1.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0597",
            ["--traditional", "-i", "\u03b1.awk", "BEGIN{print \"main\";exit}"], [new("/\u03b1.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-source-control-0598",
            ["--traditional", "-f", "\ud83d\ude00.awk"], [new("/\ud83d\ude00.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-include-option-control-0599",
            ["--traditional", "-i", "\ud83d\ude00.awk", "BEGIN{print \"main\";exit}"], [new("/\ud83d\ude00.awk", "BEGIN{print \"included\"}\n")])
            .WithStandardInput("").WithStdOut("included\nmain\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0600",
            ["--traditional", "-F", "00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0601",
            ["--traditional", "-v", "FS=00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-separator-value-kind-0602",
            ["--traditional", "-F", "00", "-v", "FS=00", "BEGIN{print (FS==0);exit}"], [])
            .WithStandardInput("").WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0603",
            ["--traditional", "END{print (v==1), \"[\" v \"]\"}", "v=\\061"], [])
            .WithStandardInput("").WithStdOut("1 [1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0604",
            ["--traditional", "END{print (v==1), \"[\" v \"]\"}", "v=\\x31"], [])
            .WithStandardInput("").WithStdOut("1 [1]\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-value-kind-0605",
            ["--traditional", "END{print (v==1), \"[\" v \"]\"}", "v=\\u0031"], [])
            .WithStandardInput("").WithStdOut("1 [1]\n").WithStdErr("").ExitsWith(0).Build();
    }
}
