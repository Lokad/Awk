namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FormattingSyntaxCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-value-order",
                ["BEGIN{print \"before\";printf \"[%2$s][%1$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .WithStdOut("before\n[b][a]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-reuse",
                ["BEGIN{print \"before\";printf \"[%2$s][%2$s][%1$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .WithStdOut("before\n[b][b][a]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-width",
                ["BEGIN{print \"before\";printf \"[%2$*1$s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .WithStdOut("before\n[     x]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-precision",
                ["BEGIN{print \"before\";printf \"[%2$.*1$s]\\n\",2,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[ab]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-width-precision",
                ["BEGIN{print \"before\";printf \"[%3$*1$.*2$f]\\n\",9,3,1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[    1.250]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-mix-values",
                ["BEGIN{print \"before\";printf \"[%1$s][%s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional and sequential printf arguments cannot be mixed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-mix-values-reverse",
                ["BEGIN{print \"before\";printf \"[%s][%2$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional and sequential printf arguments cannot be mixed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-mix-width",
                ["BEGIN{print \"before\";printf \"[%2$*s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional and sequential printf arguments cannot be mixed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-mix-width-reverse",
                ["BEGIN{print \"before\";printf \"[%*2$s]\\n\",\"x\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n[     x]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-mix-precision",
                ["BEGIN{print \"before\";printf \"[%2$.*s]\\n\",2,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional and sequential printf arguments cannot be mixed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-zero-position",
                ["BEGIN{print \"before\";printf \"[%0$s]\\n\",\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: printf argument positions must be positive\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-zero-width-position",
                ["BEGIN{print \"before\";printf \"[%2$*0$s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .WithStdOut("before\n[x]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-zero-precision-position",
                ["BEGIN{print \"before\";printf \"[%2$.*0$s]\\n\",2,\"x\";print \"after\"}"],
                [])
            .WithStdOut("before\n[]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-missing-value",
                ["BEGIN{print \"before\";printf \"[%3$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-missing-width",
                ["BEGIN{print \"before\";printf \"[%1$*3$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-missing-precision",
                ["BEGIN{print \"before\";printf \"[%1$.*3$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-percent-position",
                ["BEGIN{print \"before\";printf \"[%2$%][%1$s]\\n\",\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-percent-width",
                ["BEGIN{print \"before\";printf \"[%2$*1$%][%1$s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%][6]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-unknown-position",
                ["BEGIN{print \"before\";printf \"[%2$q][%s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional and sequential printf arguments cannot be mixed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-sequential-unknown",
                ["BEGIN{print \"before\";printf \"[%q][%s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%q][a]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-length-single",
                ["BEGIN{print \"before\";printf \"[%ld][%hu][%Lf][%ls][%hc]\\n\",2,3,1.5,\"x\",\"a\";print \"after\"}"],
                [])
            .WithStdOut("before\n[2][3][1.500000][x][a]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-length-double",
                ["BEGIN{print \"before\";printf \"[%lld][%hhd][%LLf]\\n\",2,3,1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[%lld][%hhd][%LLf]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-length-extra",
                ["BEGIN{print \"before\";printf \"[%zd][%td][%jd][%qd][%I64d]\\n\",1,2,3,4,5;print \"after\"}"],
                [])
            .WithStdOut("before\n[1][2][3][%qd][%I64d]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-length-mixed",
                ["BEGIN{print \"before\";printf \"[%hld][%lhd][%hhhhd]\\n\",1,2,3;print \"after\"}"],
                [])
            .WithStdOut("before\n[1][2][%hhhhd]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-length-with-position",
                ["BEGIN{print \"before\";printf \"[%2$*1$ld][%3$.*4$Lf]\\n\",6,2,1.5,3;print \"after\"}"],
                [])
            .WithStdOut("before\n[     2][1.500]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-default",
                ["BEGIN{print \"before\";printf \"[%a][%A][%a][%a]\\n\",1.5,1.5,0,-0;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0][0X1.8P+0][0x0p+0][-0x0p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-rounding",
                ["BEGIN{print \"before\";printf \"[%.0a][%.1a][%.2a][%#.0a]\\n\",1.5,1.96875,1.998046875,1;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x2p+0][0x2.0p+0][0x2.00p+0][0x1.p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-sign-padding",
                ["BEGIN{print \"before\";printf \"[%+020a][%-20a][%020a]\\n\",1.5,1.5,-1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[+0x000000000001.8p+0][0x1.8p+0            ][-0x000000000001.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-value-order",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$s][%1$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-reuse",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$s][%2$s][%1$s]\\n\",\"a\",\"b\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-width",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$*1$s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-precision",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$.*1$s]\\n\",2,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-width-precision",
                ["--posix", "BEGIN{print \"before\";printf \"[%3$*1$.*2$f]\\n\",9,3,1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-length-single",
                ["--posix", "BEGIN{print \"before\";printf \"[%ld][%hu][%Lf][%ls][%hc]\\n\",2,3,1.5,\"x\",\"a\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-length-double",
                ["--posix", "BEGIN{print \"before\";printf \"[%lld][%hhd][%LLf]\\n\",2,3,1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-hex-default",
                ["--posix", "BEGIN{print \"before\";printf \"[%a][%A][%a][%a]\\n\",1.5,1.5,0,-0;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0][0X1.8P+0][0x0p+0][-0x0p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-hex-rounding",
                ["--posix", "BEGIN{print \"before\";printf \"[%.0a][%.1a][%.2a][%#.0a]\\n\",1.5,1.96875,1.998046875,1;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x2p+0][0x2.0p+0][0x2.00p+0][0x1.p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-hex-sign-padding",
                ["--posix", "BEGIN{print \"before\";printf \"[%+020a][%-20a][%020a]\\n\",1.5,1.5,-1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[+0x000000000001.8p+0][0x1.8p+0            ][-0x000000000001.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-special-678",
                ["BEGIN{printf \"[%a]\\n\",(-1)^0.5}"],
                [])
            .WithStdOut("[-nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-special-679",
                ["BEGIN{printf \"[%A]\\n\",(-1)^0.5}"],
                [])
            .WithStdOut("[-NAN]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-special-680",
                ["BEGIN{printf \"[%.0a]\\n\",(-1)^0.5}"],
                [])
            .WithStdOut("[-nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-special-681",
                ["BEGIN{printf \"[%020a]\\n\",(-1)^0.5}"],
                [])
            .WithStdOut("[                -nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-special-682",
                ["BEGIN{printf \"[%+020a]\\n\",(-1)^0.5}"],
                [])
            .WithStdOut("[                -nan]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-hex-special-683",
                ["BEGIN{printf \"[%-20a]\\n\",(-1)^0.5}"],
                [])
            .WithStdOut("[-nan                ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-position-sign",
                ["BEGIN{print \"before\";printf \"[%+2$d]\\n\",11,22;print \"after\"}"],
                [])
            .WithStdOut("before\n[+22]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-position-sign",
                ["--traditional", "BEGIN{print \"before\";printf \"[%+2$d]\\n\",11,22;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-position-sign",
                ["--posix", "BEGIN{print \"before\";printf \"[%+2$d]\\n\",11,22;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-position-zero-pad",
                ["BEGIN{print \"before\";printf \"[%02$d]\\n\",11,22;print \"after\"}"],
                [])
            .WithStdOut("before\n[22]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-position-zero-pad",
                ["--traditional", "BEGIN{print \"before\";printf \"[%02$d]\\n\",11,22;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-position-zero-pad",
                ["--posix", "BEGIN{print \"before\";printf \"[%02$d]\\n\",11,22;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-position-modifier-first",
                ["BEGIN{print \"before\";printf \"[%l2$d]\\n\",11,22;print \"after\"}"],
                [])
            .WithStdOut("before\n[22]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-position-modifier-first",
                ["--traditional", "BEGIN{print \"before\";printf \"[%l2$d]\\n\",11,22;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-position-modifier-first",
                ["--posix", "BEGIN{print \"before\";printf \"[%l2$d]\\n\",11,22;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-width",
                ["BEGIN{print \"before\";printf \"[%l8d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[      11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-width",
                ["--traditional", "BEGIN{print \"before\";printf \"[%l8d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[      11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-width",
                ["--posix", "BEGIN{print \"before\";printf \"[%l8d]\\n\",11;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-precision",
                ["BEGIN{print \"before\";printf \"[%l.2f]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.25]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-precision",
                ["--traditional", "BEGIN{print \"before\";printf \"[%l.2f]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.25]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-precision",
                ["--posix", "BEGIN{print \"before\";printf \"[%l.2f]\\n\",1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-flags",
                ["BEGIN{print \"before\";printf \"[%l+8d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[     +11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-flags",
                ["--traditional", "BEGIN{print \"before\";printf \"[%l+8d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[     +11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-flags",
                ["--posix", "BEGIN{print \"before\";printf \"[%l+8d]\\n\",11;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-width-flags",
                ["BEGIN{print \"before\";printf \"[%8+d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[     +11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-width-flags",
                ["--traditional", "BEGIN{print \"before\";printf \"[%8+d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[     +11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-width-flags",
                ["--posix", "BEGIN{print \"before\";printf \"[%8+d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[     +11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-width-modifier-flags",
                ["BEGIN{print \"before\";printf \"[%8l+d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[     +11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-width-modifier-flags",
                ["--traditional", "BEGIN{print \"before\";printf \"[%8l+d]\\n\",11;print \"after\"}"],
                [])
            .WithStdOut("before\n[     +11]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-width-modifier-flags",
                ["--posix", "BEGIN{print \"before\";printf \"[%8l+d]\\n\",11;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-precision-position",
                ["BEGIN{print \"before\";printf \"[%.2$s]\\n\",\"abc\",\"xyz\";print \"after\"}"],
                [])
            .WithStdOut("before\n[]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-precision-position",
                ["--traditional", "BEGIN{print \"before\";printf \"[%.2$s]\\n\",\"abc\",\"xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-precision-position",
                ["--posix", "BEGIN{print \"before\";printf \"[%.2$s]\\n\",\"abc\",\"xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-negative-literal-precision",
                ["BEGIN{print \"before\";printf \"[%.-3s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-negative-literal-precision",
                ["--traditional", "BEGIN{print \"before\";printf \"[%.-3s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-negative-literal-precision",
                ["--posix", "BEGIN{print \"before\";printf \"[%.-3s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-repeated-dot",
                ["BEGIN{print \"before\";printf \"[%..s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%..s]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-repeated-dot",
                ["--traditional", "BEGIN{print \"before\";printf \"[%..s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%..s]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-repeated-dot",
                ["--posix", "BEGIN{print \"before\";printf \"[%..s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%..s]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-precision-flag",
                ["BEGIN{print \"before\";printf \"[%.2+s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%.2+s]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-precision-flag",
                ["--traditional", "BEGIN{print \"before\";printf \"[%.2+s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%.2+s]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-precision-flag",
                ["--posix", "BEGIN{print \"before\";printf \"[%.2+s]\\n\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%.2+s]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-repeated-position",
                ["BEGIN{print \"before\";printf \"[%1$2$s]\\n\",\"abc\",\"xyz\";print \"after\"}"],
                [])
            .WithStdOut("before\n[xyz]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-repeated-position",
                ["--traditional", "BEGIN{print \"before\";printf \"[%1$2$s]\\n\",\"abc\",\"xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-repeated-position",
                ["--posix", "BEGIN{print \"before\";printf \"[%1$2$s]\\n\",\"abc\",\"xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-double-width",
                ["BEGIN{print \"before\";printf \"[%2$*1$*1$s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .WithStdOut("before\n[     x]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-double-width",
                ["--traditional", "BEGIN{print \"before\";printf \"[%2$*1$*1$s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-double-width",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$*1$*1$s]\\n\",6,\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-missing-tail-position",
                ["BEGIN{print \"before\";printf \"%2$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-missing-tail-position",
                ["--traditional", "BEGIN{print \"before\";printf \"%2$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-missing-tail-position",
                ["--posix", "BEGIN{print \"before\";printf \"%2$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-missing-tail-width",
                ["BEGIN{print \"before\";printf \"%*2$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-missing-tail-width",
                ["--traditional", "BEGIN{print \"before\";printf \"%*2$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-missing-tail-width",
                ["--posix", "BEGIN{print \"before\";printf \"%*2$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-missing-tail-precision",
                ["BEGIN{print \"before\";printf \"%.*2$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-missing-tail-precision",
                ["--traditional", "BEGIN{print \"before\";printf \"%.*2$\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n%.*2$after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-missing-tail-precision",
                ["--posix", "BEGIN{print \"before\";printf \"%.*2$\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n%.*2$after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-incomplete-position",
                ["BEGIN{print \"before\";printf \"%1$\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n%1$after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-incomplete-position",
                ["--traditional", "BEGIN{print \"before\";printf \"%1$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-incomplete-position",
                ["--posix", "BEGIN{print \"before\";printf \"%1$\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-incomplete-modifier",
                ["BEGIN{print \"before\";printf \"%l\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n%lafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-incomplete-modifier",
                ["--traditional", "BEGIN{print \"before\";printf \"%l\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n%lafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-incomplete-modifier",
                ["--posix", "BEGIN{print \"before\";printf \"%l\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-incomplete-width",
                ["BEGIN{print \"before\";printf \"%*\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n%*after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-incomplete-width",
                ["--traditional", "BEGIN{print \"before\";printf \"%*\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n%*after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-incomplete-width",
                ["--posix", "BEGIN{print \"before\";printf \"%*\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n%*after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-incomplete-precision",
                ["BEGIN{print \"before\";printf \"%.*\",2;print \"after\"}"],
                [])
            .WithStdOut("before\n%.*after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-incomplete-precision",
                ["--traditional", "BEGIN{print \"before\";printf \"%.*\",2;print \"after\"}"],
                [])
            .WithStdOut("before\n%.*after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-incomplete-precision",
                ["--posix", "BEGIN{print \"before\";printf \"%.*\",2;print \"after\"}"],
                [])
            .WithStdOut("before\n%.*after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-bare-dollar",
                ["BEGIN{print \"before\";printf \"%$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: printf argument positions must be positive\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-bare-dollar",
                ["--traditional", "BEGIN{print \"before\";printf \"%$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-bare-dollar",
                ["--posix", "BEGIN{print \"before\";printf \"%$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-negative-position",
                ["BEGIN{print \"before\";printf \"%-1$s\",\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\nabcafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-negative-position",
                ["--traditional", "BEGIN{print \"before\";printf \"%-1$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-negative-position",
                ["--posix", "BEGIN{print \"before\";printf \"%-1$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-precision-zero-position",
                ["BEGIN{print \"before\";printf \"%.0$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: printf argument positions must be positive\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-precision-zero-position",
                ["--traditional", "BEGIN{print \"before\";printf \"%.0$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-precision-zero-position",
                ["--posix", "BEGIN{print \"before\";printf \"%.0$s\",\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-unknown-dynamic",
                ["BEGIN{print \"before\";printf \"[%*2$q][%s]\\n\",\"abc\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n[%*2$q][abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-unknown-dynamic",
                ["--traditional", "BEGIN{print \"before\";printf \"[%*2$q][%s]\\n\",\"abc\",6;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-unknown-dynamic",
                ["--posix", "BEGIN{print \"before\";printf \"[%*2$q][%s]\\n\",\"abc\",6;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-percent-sequential-width",
                ["BEGIN{print \"before\";printf \"[%*%][%s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%][abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-percent-sequential-width",
                ["--traditional", "BEGIN{print \"before\";printf \"[%*%][%s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%][abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-percent-sequential-width",
                ["--posix", "BEGIN{print \"before\";printf \"[%*%][%s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[%][abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-percent-mixed-width",
                ["BEGIN{print \"before\";printf \"[%1$*%]\\n\",6;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional and sequential printf arguments cannot be mixed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-percent-mixed-width",
                ["--traditional", "BEGIN{print \"before\";printf \"[%1$*%]\\n\",6;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-percent-mixed-width",
                ["--posix", "BEGIN{print \"before\";printf \"[%1$*%]\\n\",6;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-mixed-missing-value",
                ["BEGIN{print \"before\";printf \"[%s][%4$s]\\n\",\"abc\",\"def\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-mixed-missing-value",
                ["--traditional", "BEGIN{print \"before\";printf \"[%s][%4$s]\\n\",\"abc\",\"def\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-mixed-missing-value",
                ["--posix", "BEGIN{print \"before\";printf \"[%s][%4$s]\\n\",\"abc\",\"def\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-star-missing-suffix",
                ["BEGIN{print \"before\";printf \"%*2s\",\"abc\",6;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional printf width or precision requires `$'\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-star-missing-suffix",
                ["--traditional", "BEGIN{print \"before\";printf \"%*2s\",\"abc\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n 6after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-star-missing-suffix",
                ["--posix", "BEGIN{print \"before\";printf \"%*2s\",\"abc\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n 6after\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-star-precision-missing-suffix",
                ["BEGIN{print \"before\";printf \"%.*2s\",\"abc\",6;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional printf width or precision requires `$'\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-star-precision-missing-suffix",
                ["--traditional", "BEGIN{print \"before\";printf \"%.*2s\",\"abc\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n%.*2safter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-star-precision-missing-suffix",
                ["--posix", "BEGIN{print \"before\";printf \"%.*2s\",\"abc\",6;print \"after\"}"],
                [])
            .WithStdOut("before\n%.*2safter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-positional-negative-width",
                ["BEGIN{print \"before\";printf \"[%2$*1$s]\\n\",-6,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[abc   ]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-positional-negative-width",
                ["--traditional", "BEGIN{print \"before\";printf \"[%2$*1$s]\\n\",-6,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-positional-negative-width",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$*1$s]\\n\",-6,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-positional-negative-precision",
                ["BEGIN{print \"before\";printf \"[%2$.*1$s]\\n\",-2,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n[abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-positional-negative-precision",
                ["--traditional", "BEGIN{print \"before\";printf \"[%2$.*1$s]\\n\",-2,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-positional-negative-precision",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$.*1$s]\\n\",-2,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-positional-negative-float-precision",
                ["BEGIN{print \"before\";printf \"[%2$.*1$a]\\n\",-2,1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-positional-negative-float-precision",
                ["--traditional", "BEGIN{print \"before\";printf \"[%2$.*1$a]\\n\",-2,1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-positional-negative-float-precision",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$.*1$a]\\n\",-2,1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-unicode-width-precision",
                ["BEGIN{print \"before\";printf \"[%3$*1$.*2$s]\\n\",7,2,\"é😀xyz\";print \"after\"}"],
                [])
            .WithStdOut("before\n[     é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-unicode-width-precision",
                ["--traditional", "BEGIN{print \"before\";printf \"[%3$*1$.*2$s]\\n\",7,2,\"é😀xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-unicode-width-precision",
                ["--posix", "BEGIN{print \"before\";printf \"[%3$*1$.*2$s]\\n\",7,2,\"é😀xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-unicode-character",
                ["BEGIN{print \"before\";printf \"[%2$*1$c]\\n\",4,\"😀xyz\";print \"after\"}"],
                [])
            .WithStdOut("before\n[   😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-unicode-character",
                ["--traditional", "BEGIN{print \"before\";printf \"[%2$*1$c]\\n\",4,\"😀xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-unicode-character",
                ["--posix", "BEGIN{print \"before\";printf \"[%2$*1$c]\\n\",4,\"😀xyz\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-sequential-positional-controls",
                ["BEGIN{print \"before\";printf \"[%*2$.*3$s][%s]\\n\",\"abc\",6,2;print \"after\"}"],
                [])
            .WithStdOut("before\n[    ab][6]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-sequential-positional-controls",
                ["--traditional", "BEGIN{print \"before\";printf \"[%*2$.*3$s][%s]\\n\",\"abc\",6,2;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-sequential-positional-controls",
                ["--posix", "BEGIN{print \"before\";printf \"[%*2$.*3$s][%s]\\n\",\"abc\",6,2;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-precision-positional-control",
                ["BEGIN{print \"before\";printf \"[%.*2$s][%s]\\n\",\"abcd\",2;print \"after\"}"],
                [])
            .WithStdOut("before\n[ab][2]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-precision-positional-control",
                ["--traditional", "BEGIN{print \"before\";printf \"[%.*2$s][%s]\\n\",\"abcd\",2;print \"after\"}"],
                [])
            .WithStdOut("before\n[%.*2$s][2]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-precision-positional-control",
                ["--posix", "BEGIN{print \"before\";printf \"[%.*2$s][%s]\\n\",\"abcd\",2;print \"after\"}"],
                [])
            .WithStdOut("before\n[%.*2$s][2]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-zero-control-numeric-format",
                ["BEGIN{print \"before\";printf \"12[%2$*0$s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n12[         abc]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-zero-control-numeric-format",
                ["--traditional", "BEGIN{print \"before\";printf \"12[%2$*0$s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-zero-control-numeric-format",
                ["--posix", "BEGIN{print \"before\";printf \"12[%2$*0$s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-zero-precision-numeric-format",
                ["BEGIN{print \"before\";printf \"2[%2$.*0$s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .WithStdOut("before\n2[ab]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-zero-precision-numeric-format",
                ["--traditional", "BEGIN{print \"before\";printf \"2[%2$.*0$s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-zero-precision-numeric-format",
                ["--posix", "BEGIN{print \"before\";printf \"2[%2$.*0$s]\\n\",6,\"abc\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-position-takes-zero-flag",
                ["BEGIN{print \"before\";printf \"[%02$s]\\n\",\"abc\",\"x\";print \"after\"}"],
                [])
            .WithStdOut("before\n[x]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-position-takes-zero-flag",
                ["--traditional", "BEGIN{print \"before\";printf \"[%02$s]\\n\",\"abc\",\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-position-takes-zero-flag",
                ["--posix", "BEGIN{print \"before\";printf \"[%02$s]\\n\",\"abc\",\"x\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-h-s",
                ["BEGIN{print \"before\";printf \"[%hs]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-h-s",
                ["--traditional", "BEGIN{print \"before\";printf \"[%hs]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-h-s",
                ["--posix", "BEGIN{print \"before\";printf \"[%hs]\\n\",\"é😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `h' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-h-c",
                ["BEGIN{print \"before\";printf \"[%hc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-h-c",
                ["--traditional", "BEGIN{print \"before\";printf \"[%hc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-h-c",
                ["--posix", "BEGIN{print \"before\";printf \"[%hc]\\n\",\"😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `h' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-h-d",
                ["BEGIN{print \"before\";printf \"[%hd]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-h-d",
                ["--traditional", "BEGIN{print \"before\";printf \"[%hd]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-h-d",
                ["--posix", "BEGIN{print \"before\";printf \"[%hd]\\n\",12;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `h' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-h-f",
                ["BEGIN{print \"before\";printf \"[%hf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-h-f",
                ["--traditional", "BEGIN{print \"before\";printf \"[%hf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-h-f",
                ["--posix", "BEGIN{print \"before\";printf \"[%hf]\\n\",1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `h' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-h-a",
                ["BEGIN{print \"before\";printf \"[%ha]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-h-a",
                ["--traditional", "BEGIN{print \"before\";printf \"[%ha]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-h-a",
                ["--posix", "BEGIN{print \"before\";printf \"[%ha]\\n\",1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `h' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-j-s",
                ["BEGIN{print \"before\";printf \"[%js]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-j-s",
                ["--traditional", "BEGIN{print \"before\";printf \"[%js]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-j-s",
                ["--posix", "BEGIN{print \"before\";printf \"[%js]\\n\",\"é😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `j' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-j-c",
                ["BEGIN{print \"before\";printf \"[%jc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-j-c",
                ["--traditional", "BEGIN{print \"before\";printf \"[%jc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-j-c",
                ["--posix", "BEGIN{print \"before\";printf \"[%jc]\\n\",\"😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `j' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-j-d",
                ["BEGIN{print \"before\";printf \"[%jd]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-j-d",
                ["--traditional", "BEGIN{print \"before\";printf \"[%jd]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-j-d",
                ["--posix", "BEGIN{print \"before\";printf \"[%jd]\\n\",12;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `j' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-j-f",
                ["BEGIN{print \"before\";printf \"[%jf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-j-f",
                ["--traditional", "BEGIN{print \"before\";printf \"[%jf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-j-f",
                ["--posix", "BEGIN{print \"before\";printf \"[%jf]\\n\",1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `j' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-j-a",
                ["BEGIN{print \"before\";printf \"[%ja]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-j-a",
                ["--traditional", "BEGIN{print \"before\";printf \"[%ja]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-j-a",
                ["--posix", "BEGIN{print \"before\";printf \"[%ja]\\n\",1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `j' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-l-s",
                ["BEGIN{print \"before\";printf \"[%ls]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-l-s",
                ["--traditional", "BEGIN{print \"before\";printf \"[%ls]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-l-s",
                ["--posix", "BEGIN{print \"before\";printf \"[%ls]\\n\",\"é😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-l-c",
                ["BEGIN{print \"before\";printf \"[%lc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-l-c",
                ["--traditional", "BEGIN{print \"before\";printf \"[%lc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-l-c",
                ["--posix", "BEGIN{print \"before\";printf \"[%lc]\\n\",\"😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-l-d",
                ["BEGIN{print \"before\";printf \"[%ld]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-l-d",
                ["--traditional", "BEGIN{print \"before\";printf \"[%ld]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-l-d",
                ["--posix", "BEGIN{print \"before\";printf \"[%ld]\\n\",12;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-l-f",
                ["BEGIN{print \"before\";printf \"[%lf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-l-f",
                ["--traditional", "BEGIN{print \"before\";printf \"[%lf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-l-f",
                ["--posix", "BEGIN{print \"before\";printf \"[%lf]\\n\",1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-l-a",
                ["BEGIN{print \"before\";printf \"[%la]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-l-a",
                ["--traditional", "BEGIN{print \"before\";printf \"[%la]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-l-a",
                ["--posix", "BEGIN{print \"before\";printf \"[%la]\\n\",1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-L-s",
                ["BEGIN{print \"before\";printf \"[%Ls]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-L-s",
                ["--traditional", "BEGIN{print \"before\";printf \"[%Ls]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-L-s",
                ["--posix", "BEGIN{print \"before\";printf \"[%Ls]\\n\",\"é😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `L' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-L-c",
                ["BEGIN{print \"before\";printf \"[%Lc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-L-c",
                ["--traditional", "BEGIN{print \"before\";printf \"[%Lc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-L-c",
                ["--posix", "BEGIN{print \"before\";printf \"[%Lc]\\n\",\"😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `L' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-L-d",
                ["BEGIN{print \"before\";printf \"[%Ld]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-L-d",
                ["--traditional", "BEGIN{print \"before\";printf \"[%Ld]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-L-d",
                ["--posix", "BEGIN{print \"before\";printf \"[%Ld]\\n\",12;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `L' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-L-f",
                ["BEGIN{print \"before\";printf \"[%Lf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-L-f",
                ["--traditional", "BEGIN{print \"before\";printf \"[%Lf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-L-f",
                ["--posix", "BEGIN{print \"before\";printf \"[%Lf]\\n\",1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `L' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-L-a",
                ["BEGIN{print \"before\";printf \"[%La]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-L-a",
                ["--traditional", "BEGIN{print \"before\";printf \"[%La]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-L-a",
                ["--posix", "BEGIN{print \"before\";printf \"[%La]\\n\",1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `L' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-t-s",
                ["BEGIN{print \"before\";printf \"[%ts]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-t-s",
                ["--traditional", "BEGIN{print \"before\";printf \"[%ts]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-t-s",
                ["--posix", "BEGIN{print \"before\";printf \"[%ts]\\n\",\"é😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `t' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-t-c",
                ["BEGIN{print \"before\";printf \"[%tc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-t-c",
                ["--traditional", "BEGIN{print \"before\";printf \"[%tc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-t-c",
                ["--posix", "BEGIN{print \"before\";printf \"[%tc]\\n\",\"😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `t' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-t-d",
                ["BEGIN{print \"before\";printf \"[%td]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-t-d",
                ["--traditional", "BEGIN{print \"before\";printf \"[%td]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-t-d",
                ["--posix", "BEGIN{print \"before\";printf \"[%td]\\n\",12;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `t' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-t-f",
                ["BEGIN{print \"before\";printf \"[%tf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-t-f",
                ["--traditional", "BEGIN{print \"before\";printf \"[%tf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-t-f",
                ["--posix", "BEGIN{print \"before\";printf \"[%tf]\\n\",1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `t' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-t-a",
                ["BEGIN{print \"before\";printf \"[%ta]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-t-a",
                ["--traditional", "BEGIN{print \"before\";printf \"[%ta]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-t-a",
                ["--posix", "BEGIN{print \"before\";printf \"[%ta]\\n\",1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `t' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-z-s",
                ["BEGIN{print \"before\";printf \"[%zs]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-z-s",
                ["--traditional", "BEGIN{print \"before\";printf \"[%zs]\\n\",\"é😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[é😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-z-s",
                ["--posix", "BEGIN{print \"before\";printf \"[%zs]\\n\",\"é😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `z' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-z-c",
                ["BEGIN{print \"before\";printf \"[%zc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-z-c",
                ["--traditional", "BEGIN{print \"before\";printf \"[%zc]\\n\",\"😀\";print \"after\"}"],
                [])
            .WithStdOut("before\n[😀]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-z-c",
                ["--posix", "BEGIN{print \"before\";printf \"[%zc]\\n\",\"😀\";print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `z' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-z-d",
                ["BEGIN{print \"before\";printf \"[%zd]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-z-d",
                ["--traditional", "BEGIN{print \"before\";printf \"[%zd]\\n\",12;print \"after\"}"],
                [])
            .WithStdOut("before\n[12]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-z-d",
                ["--posix", "BEGIN{print \"before\";printf \"[%zd]\\n\",12;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `z' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-z-f",
                ["BEGIN{print \"before\";printf \"[%zf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-z-f",
                ["--traditional", "BEGIN{print \"before\";printf \"[%zf]\\n\",1.25;print \"after\"}"],
                [])
            .WithStdOut("before\n[1.250000]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-z-f",
                ["--posix", "BEGIN{print \"before\";printf \"[%zf]\\n\",1.25;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `z' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-modifier-z-a",
                ["BEGIN{print \"before\";printf \"[%za]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-modifier-z-a",
                ["--traditional", "BEGIN{print \"before\";printf \"[%za]\\n\",1.5;print \"after\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-modifier-z-a",
                ["--posix", "BEGIN{print \"before\";printf \"[%za]\\n\",1.5;print \"after\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `z' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-side-effects-210",
                ["function w(){print \"width\";return 7}function p(){print \"precision\";return 2}function v(){print \"value\";return 1.25}BEGIN{i=1;print sprintf(\"%2$s %1$s\",i++,i++);print i}"],
                [])
            .WithStdOut("2 1\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-side-effects-211",
                ["function w(){print \"width\";return 7}function p(){print \"precision\";return 2}function v(){print \"value\";return 1.25}BEGIN{i=1;print sprintf(\"%1$s %1$s\",i++,i++);print i}"],
                [])
            .WithStdOut("1 1\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-side-effects-212",
                ["function w(){print \"width\";return 7}function p(){print \"precision\";return 2}function v(){print \"value\";return 1.25}BEGIN{i=1;print sprintf(\"%3$*1$.*2$f\",w(),p(),v());print i}"],
                [])
            .WithStdOut("width\nprecision\nvalue\n   1.25\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-ofmt-hex",
                ["BEGIN{OFMT=\"%a\";print 1.5}"],
                [])
            .WithStdOut("0x1.8p+0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-ofmt-hex",
                ["--traditional", "BEGIN{OFMT=\"%a\";print 1.5}"],
                [])
            .WithStdOut("0x1.8p+0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-ofmt-hex",
                ["--posix", "BEGIN{OFMT=\"%a\";print 1.5}"],
                [])
            .WithStdOut("0x1.8p+0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-convfmt-hex",
                ["BEGIN{CONVFMT=\"%a\";print \"[\" 1.5 \"]\"}"],
                [])
            .WithStdOut("[0x1.8p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-convfmt-hex",
                ["--traditional", "BEGIN{CONVFMT=\"%a\";print \"[\" 1.5 \"]\"}"],
                [])
            .WithStdOut("[0x1.8p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-convfmt-hex",
                ["--posix", "BEGIN{CONVFMT=\"%a\";print \"[\" 1.5 \"]\"}"],
                [])
            .WithStdOut("[0x1.8p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-ofmt-position",
                ["BEGIN{print \"before\";OFMT=\"%1$a\";print 1.5}"],
                [])
            .WithStdOut("before\n0x1.8p+0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-ofmt-position",
                ["--traditional", "BEGIN{print \"before\";OFMT=\"%1$a\";print 1.5}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-ofmt-position",
                ["--posix", "BEGIN{print \"before\";OFMT=\"%1$a\";print 1.5}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-convfmt-position",
                ["BEGIN{print \"before\";CONVFMT=\"%1$a\";print \"[\" 1.5 \"]\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-convfmt-position",
                ["--traditional", "BEGIN{print \"before\";CONVFMT=\"%1$a\";print \"[\" 1.5 \"]\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-convfmt-position",
                ["--posix", "BEGIN{print \"before\";CONVFMT=\"%1$a\";print \"[\" 1.5 \"]\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-ofmt-modifier",
                ["BEGIN{print \"before\";OFMT=\"%la\";print 1.5}"],
                [])
            .WithStdOut("before\n0x1.8p+0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-ofmt-modifier",
                ["--traditional", "BEGIN{print \"before\";OFMT=\"%la\";print 1.5}"],
                [])
            .WithStdOut("before\n0x1.8p+0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-ofmt-modifier",
                ["--posix", "BEGIN{print \"before\";OFMT=\"%la\";print 1.5}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-convfmt-modifier",
                ["BEGIN{print \"before\";CONVFMT=\"%la\";print \"[\" 1.5 \"]\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-convfmt-modifier",
                ["--traditional", "BEGIN{print \"before\";CONVFMT=\"%la\";print \"[\" 1.5 \"]\"}"],
                [])
            .WithStdOut("before\n[0x1.8p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-convfmt-modifier",
                ["--posix", "BEGIN{print \"before\";CONVFMT=\"%la\";print \"[\" 1.5 \"]\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: `l' is not permitted in POSIX awk formats\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-sprintf-position",
                ["BEGIN{print sprintf(\"[%2$s][%1$s]\",\"a\",\"b\")}"],
                [])
            .WithStdOut("[b][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-sprintf-position",
                ["--traditional", "BEGIN{print sprintf(\"[%2$s][%1$s]\",\"a\",\"b\")}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-sprintf-position",
                ["--posix", "BEGIN{print sprintf(\"[%2$s][%1$s]\",\"a\",\"b\")}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-sprintf-hex",
                ["BEGIN{print sprintf(\"[%2$*1$a]\",15,1.5)}"],
                [])
            .WithStdOut("[       0x1.8p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-sprintf-hex",
                ["--traditional", "BEGIN{print sprintf(\"[%2$*1$a]\",15,1.5)}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-sprintf-hex",
                ["--posix", "BEGIN{print sprintf(\"[%2$*1$a]\",15,1.5)}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-sprintf-error-eager-arguments",
                ["function f(){print \"argument\";return \"x\"}BEGIN{print \"before\";print sprintf(\"%2$s\",f())}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\nargument\n")
            .WithStdErr("awk: not enough arguments to satisfy format string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-sprintf-error-eager-arguments",
                ["--traditional", "function f(){print \"argument\";return \"x\"}BEGIN{print \"before\";print sprintf(\"%2$s\",f())}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\nargument\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-sprintf-error-eager-arguments",
                ["--posix", "function f(){print \"argument\";return \"x\"}BEGIN{print \"before\";print sprintf(\"%2$s\",f())}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\nargument\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-gnu-printf-unused-eager-arguments",
                ["function f(x){print x;return x}BEGIN{printf \"%2$s\\n\",f(\"a\"),f(\"b\"),f(\"c\")}"],
                [])
            .WithStdOut("a\nb\nc\nb\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-traditional-printf-unused-eager-arguments",
                ["--traditional", "function f(x){print x;return x}BEGIN{printf \"%2$s\\n\",f(\"a\"),f(\"b\"),f(\"c\")}"],
                [])
            .ExitsWith(2)
            .WithStdOut("a\nb\nc\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("format-syntax-posix-printf-unused-eager-arguments",
                ["--posix", "function f(x){print x;return x}BEGIN{printf \"%2$s\\n\",f(\"a\"),f(\"b\"),f(\"c\")}"],
                [])
            .ExitsWith(2)
            .WithStdOut("a\nb\nc\n")
            .WithStdErr("awk: positional formats are not permitted in this awk profile\n")
            .Build();

    }
}
