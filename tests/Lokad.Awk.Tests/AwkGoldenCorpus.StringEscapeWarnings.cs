namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> StringEscapeWarningCases()
    {
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0100",
            ["-v", "v=\\U0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0102",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("start\n5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0104",
            ["BEGIN{ARGV[1]=\"\\\\U0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0105",
            ["-v", "v=\\q", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0107",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("start\n1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0109",
            ["BEGIN{ARGV[1]=\"\\\\q\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0115",
            ["-v", "v=\\x", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0117",
            ["BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("start\n1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0119",
            ["BEGIN{ARGV[1]=\"\\\\x\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0150",
            ["-F", "\\x", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0258",
            ["--posix", "-v", "v=\\U0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0259",
            ["-v", "v=\\U0041", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0260",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("start\n5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0262",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\U0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0263",
            ["--posix", "-v", "v=\\q", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0264",
            ["-v", "v=\\q", "--posix", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0265",
            ["--posix", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("start\n1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0267",
            ["--posix", "BEGIN{ARGV[1]=\"\\\\q\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0416",
            ["--traditional", "-v", "v=\\U0041", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0417",
            ["-v", "v=\\U0041", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0418",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("start\n5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0420",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\U0041\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\U0041"], [])
            .WithStandardInput("").WithStdOut("5 [U0041]\n")
            .WithStdErr("awk: warning: escape sequence `\\U' treated as plain `U'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0421",
            ["--traditional", "-v", "v=\\q", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0422",
            ["-v", "v=\\q", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0423",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("start\n1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0425",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\q\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\q"], [])
            .WithStandardInput("").WithStdOut("1 [q]\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-0431",
            ["--traditional", "-v", "v=\\x", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-option-before-profile-0432",
            ["-v", "v=\\x", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(v),v;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-0433",
            ["--traditional", "BEGIN{print \"start\"}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("start\n1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-operand-reassigned-0435",
            ["--traditional", "BEGIN{ARGV[1]=\"\\\\x\";ARGV[1]=\"v=\" ARGV[1]}END{printf \"%d [%s]\\n\",length(v),v}", "v=\\x"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-0466",
            ["--traditional", "-F", "\\x", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-field-option-before-0467",
            ["-F", "\\x", "--traditional", "BEGIN{printf \"%d [%s]\\n\",length(FS),FS;exit}"], [])
            .WithStandardInput("").WithStdOut("1 [x]\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0475",
            ["-v", "a=\\q", "-v", "b=\\q", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0477",
            ["-v", "a=\\q", "BEGIN{print \"\\q\";exit}"], [])
            .WithStandardInput("").WithStdOut("q\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0479",
            ["BEGIN{print \"start\"}END{print \"ok\"}", "a=\\q", "b=\\q"], [])
            .WithStandardInput("").WithStdOut("start\nok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0481",
            ["BEGIN{print \"\\q\"}END{print \"ok\"}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("q\nok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0485",
            ["--posix", "-v", "a=\\q", "-v", "b=\\q", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0487",
            ["--posix", "-v", "a=\\q", "BEGIN{print \"\\q\";exit}"], [])
            .WithStandardInput("").WithStdOut("q\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0489",
            ["--posix", "BEGIN{print \"start\"}END{print \"ok\"}", "a=\\q", "b=\\q"], [])
            .WithStandardInput("").WithStdOut("start\nok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0491",
            ["--posix", "BEGIN{print \"\\q\"}END{print \"ok\"}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("q\nok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0495",
            ["--traditional", "-v", "a=\\q", "-v", "b=\\q", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0497",
            ["--traditional", "-v", "a=\\q", "BEGIN{print \"\\q\";exit}"], [])
            .WithStandardInput("").WithStdOut("q\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0499",
            ["--traditional", "BEGIN{print \"start\"}END{print \"ok\"}", "a=\\q", "b=\\q"], [])
            .WithStandardInput("").WithStdOut("start\nok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-callers-warning-order-0501",
            ["--traditional", "BEGIN{print \"\\q\"}END{print \"ok\"}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("q\nok\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-repeated-source-gnu",
            ["BEGIN{print \"\\q\\q\\j\\j\";exit}"], [])
            .WithStandardInput("").WithStdOut("qqjj\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: escape sequence `\\j' treated as plain `j'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-shared-callers-gnu",
            ["-v", "a=\\q", "BEGIN{print a,\"\\q\\j\"}END{print b,c}", "b=\\q", "c=\\j"], [])
            .WithStandardInput("").WithStdOut("q qj\nq j\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: escape sequence `\\j' treated as plain `j'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-per-occurrence-x-gnu",
            ["-v", "a=\\x", "-v", "b=\\x", "BEGIN{print a,b,\"\\x\"}END{print c,d}", "c=\\x", "d=\\x"], [])
            .WithStandardInput("").WithStdOut("x x x\nx x\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-per-occurrence-u-gnu",
            ["-v", "a=\\u", "-v", "b=\\u", "BEGIN{print a,b,\"\\u\"}END{print c,d}", "c=\\u", "d=\\u"], [])
            .WithStandardInput("").WithStdOut("u u u\nu u\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-unconsumed-gnu",
            ["BEGIN{print \"ok\";exit}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-deleted-operand-gnu",
            ["BEGIN{delete ARGV[1];print \"ok\"}END{print \"done\"}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("ok\ndone\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-quoted-backslash-gnu",
            ["BEGIN{printf \"%s\",\"\\\\q\";exit}"], [])
            .WithStandardInput("").WithStdOut("\\q")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-source-owns-origin-gnu",
            ["-f", "/warning.awk", "a=\\q"], [new("/warning.awk", "# source\nBEGIN{print \"\\q\"}END{print a}\n")])
            .WithStandardInput("").WithStdOut("q\nq\n")
            .WithStdErr("awk: /warning.awk:2:13: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-option-owns-origin-gnu",
            ["-v", "a=\\q", "-f", "/warning.awk", "a=\\q"], [new("/warning.awk", "# source\nBEGIN{print \"\\q\"}END{print a}\n")])
            .WithStandardInput("").WithStdOut("q\nq\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-repeated-source-posix",
            ["--posix", "BEGIN{print \"\\q\\q\\j\\j\";exit}"], [])
            .WithStandardInput("").WithStdOut("qqjj\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: escape sequence `\\j' treated as plain `j'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-shared-callers-posix",
            ["--posix", "-v", "a=\\q", "BEGIN{print a,\"\\q\\j\"}END{print b,c}", "b=\\q", "c=\\j"], [])
            .WithStandardInput("").WithStdOut("q qj\nq j\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: escape sequence `\\j' treated as plain `j'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-per-occurrence-x-posix",
            ["--posix", "-v", "a=\\x", "-v", "b=\\x", "BEGIN{print a,b,\"\\x\"}END{print c,d}", "c=\\x", "d=\\x"], [])
            .WithStandardInput("").WithStdOut("x x x\nx x\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-per-occurrence-u-posix",
            ["--posix", "-v", "a=\\u", "-v", "b=\\u", "BEGIN{print a,b,\"\\u\"}END{print c,d}", "c=\\u", "d=\\u"], [])
            .WithStandardInput("").WithStdOut("u u u\nu u\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-unconsumed-posix",
            ["--posix", "BEGIN{print \"ok\";exit}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-deleted-operand-posix",
            ["--posix", "BEGIN{delete ARGV[1];print \"ok\"}END{print \"done\"}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("ok\ndone\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-quoted-backslash-posix",
            ["--posix", "BEGIN{printf \"%s\",\"\\\\q\";exit}"], [])
            .WithStandardInput("").WithStdOut("\\q")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-source-owns-origin-posix",
            ["--posix", "-f", "/warning.awk", "a=\\q"], [new("/warning.awk", "# source\nBEGIN{print \"\\q\"}END{print a}\n")])
            .WithStandardInput("").WithStdOut("q\nq\n")
            .WithStdErr("awk: /warning.awk:2:13: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-option-owns-origin-posix",
            ["--posix", "-v", "a=\\q", "-f", "/warning.awk", "a=\\q"], [new("/warning.awk", "# source\nBEGIN{print \"\\q\"}END{print a}\n")])
            .WithStandardInput("").WithStdOut("q\nq\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-repeated-source-traditional",
            ["--traditional", "BEGIN{print \"\\q\\q\\j\\j\";exit}"], [])
            .WithStandardInput("").WithStdOut("qqjj\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: escape sequence `\\j' treated as plain `j'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-shared-callers-traditional",
            ["--traditional", "-v", "a=\\q", "BEGIN{print a,\"\\q\\j\"}END{print b,c}", "b=\\q", "c=\\j"], [])
            .WithStandardInput("").WithStdOut("q qj\nq j\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\nawk: warning: escape sequence `\\j' treated as plain `j'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-per-occurrence-x-traditional",
            ["--traditional", "-v", "a=\\x", "-v", "b=\\x", "BEGIN{print a,b,\"\\x\"}END{print c,d}", "c=\\x", "d=\\x"], [])
            .WithStandardInput("").WithStdOut("x x x\nx x\n")
            .WithStdErr("awk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\nawk: warning: no hex digits in `\\x' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-per-occurrence-u-traditional",
            ["--traditional", "-v", "a=\\u", "-v", "b=\\u", "BEGIN{print a,b,\"\\u\"}END{print c,d}", "c=\\u", "d=\\u"], [])
            .WithStandardInput("").WithStdOut("u u u\nu u\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-unconsumed-traditional",
            ["--traditional", "BEGIN{print \"ok\";exit}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-deleted-operand-traditional",
            ["--traditional", "BEGIN{delete ARGV[1];print \"ok\"}END{print \"done\"}", "a=\\q"], [])
            .WithStandardInput("").WithStdOut("ok\ndone\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-quoted-backslash-traditional",
            ["--traditional", "BEGIN{printf \"%s\",\"\\\\q\";exit}"], [])
            .WithStandardInput("").WithStdOut("\\q")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-source-owns-origin-traditional",
            ["--traditional", "-f", "/warning.awk", "a=\\q"], [new("/warning.awk", "# source\nBEGIN{print \"\\q\"}END{print a}\n")])
            .WithStandardInput("").WithStdOut("q\nq\n")
            .WithStdErr("awk: /warning.awk:2:13: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-option-owns-origin-traditional",
            ["--traditional", "-v", "a=\\q", "-f", "/warning.awk", "a=\\q"], [new("/warning.awk", "# source\nBEGIN{print \"\\q\"}END{print a}\n")])
            .WithStandardInput("").WithStdOut("q\nq\n")
            .WithStdErr("awk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-utf8-diagnostic",
            ["BEGIN{print \"\\é\\ê\\𐐀\\q\";exit}"], [])
            .WithStandardInput("").WithStdOut("éê𐐀q\n")
            .WithStdErr("awk: warning: escape sequence `\\é' treated as plain `é'\nawk: warning: escape sequence `\\𐐀' treated as plain `𐐀'\nawk: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("string-escape-included-source-history",
            ["-f", "/main.awk", "a=\\q"], [new("/main.awk", "@include \"/included.awk\"\nBEGIN{print \"\\q\"}END{print a}"), new("/included.awk", "function f(){return \"\\q\"}")])
            .WithStandardInput("").WithStdOut("q\nq\n")
            .WithStdErr("awk: /included.awk:1:21: warning: escape sequence `\\q' treated as plain `q'\n").ExitsWith(0).Build();
    }
}
