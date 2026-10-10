namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> SpecialRegexAssignmentCases()
    {
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0475",
            ["BEGIN{FS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0476",
            ["BEGIN{FPAT=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0477",
            ["BEGIN{RS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0478",
            ["BEGIN{FS=\"\\\\u\";$0=\"u?\";print NF;FS=\"A\";FS=\"\\\\u\";$0=\"u?\";print NF;exit}"], [])
            .WithStandardInput("").WithStdOut("2\n2\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0479",
            ["BEGIN{FPAT=\"\\\\u\";$0=\"u?\";print NF;FPAT=\"A\";FPAT=\"\\\\u\";$0=\"u?\";print NF;exit}"], [])
            .WithStandardInput("").WithStdOut("1\n1\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-FS-0480",
            ["-v", "FS=\\\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-FPAT-0481",
            ["-v", "FPAT=\\\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-RS-0482",
            ["-v", "RS=\\\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0493",
            ["BEGIN{FS=\"\\\\ud800\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0494",
            ["BEGIN{FPAT=\"\\\\ud800\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0495",
            ["BEGIN{RS=\"\\\\ud800\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0496",
            ["BEGIN{FS=\"\\\\ud800\";$0=\"u?\";print NF;FS=\"A\";FS=\"\\\\ud800\";$0=\"u?\";print NF;exit}"], [])
            .WithStandardInput("").WithStdOut("2\n2\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0497",
            ["BEGIN{FPAT=\"\\\\ud800\";$0=\"u?\";print NF;FPAT=\"A\";FPAT=\"\\\\ud800\";$0=\"u?\";print NF;exit}"], [])
            .WithStandardInput("").WithStdOut("1\n1\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-FS-0498",
            ["-v", "FS=\\\\ud800", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-FPAT-0499",
            ["-v", "FPAT=\\\\ud800", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-RS-0500",
            ["-v", "RS=\\\\ud800", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0547",
            ["--traditional", "BEGIN{FS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0549",
            ["--traditional", "BEGIN{RS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0550",
            ["--traditional", "BEGIN{FS=\"\\\\u\";$0=\"u?\";print NF;FS=\"A\";FS=\"\\\\u\";$0=\"u?\";print NF;exit}"], [])
            .WithStandardInput("").WithStdOut("2\n2\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-FS-0552",
            ["--traditional", "-v", "FS=\\\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-RS-0554",
            ["--traditional", "-v", "RS=\\\\u", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0565",
            ["--traditional", "BEGIN{FS=\"\\\\ud800\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0567",
            ["--traditional", "BEGIN{RS=\"\\\\ud800\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-warning-history-0568",
            ["--traditional", "BEGIN{FS=\"\\\\ud800\";$0=\"u?\";print NF;FS=\"A\";FS=\"\\\\ud800\";$0=\"u?\";print NF;exit}"], [])
            .WithStandardInput("").WithStdOut("2\n2\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-FS-0570",
            ["--traditional", "-v", "FS=\\\\ud800", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("unicode-regex-startup-RS-0572",
            ["--traditional", "-v", "RS=\\\\ud800", "BEGIN{print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: invalid `\\u' escape sequence\nawk: warning: invalid `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-gnu-FS",
            ["BEGIN{FS=\"\\\\u\";FS=\"\\\\u\";IGNORECASE=1;FS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-gnu-FPAT",
            ["BEGIN{FPAT=\"\\\\u\";FPAT=\"\\\\u\";IGNORECASE=1;FPAT=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-gnu-RS",
            ["BEGIN{RS=\"\\\\u\";RS=\"\\\\u\";IGNORECASE=1;RS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-posix-FS",
            ["--posix", "BEGIN{FS=\"\\\\u\";FS=\"\\\\u\";IGNORECASE=1;FS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-posix-FPAT",
            ["--posix", "BEGIN{FPAT=\"\\\\u\";FPAT=\"\\\\u\";IGNORECASE=1;FPAT=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-posix-RS",
            ["--posix", "BEGIN{RS=\"\\\\u\";RS=\"\\\\u\";IGNORECASE=1;RS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-traditional-FS",
            ["--traditional", "BEGIN{FS=\"\\\\u\";FS=\"\\\\u\";IGNORECASE=1;FS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-traditional-FPAT",
            ["--traditional", "BEGIN{FPAT=\"\\\\u\";FPAT=\"\\\\u\";IGNORECASE=1;FPAT=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-same-value-traditional-RS",
            ["--traditional", "BEGIN{RS=\"\\\\u\";RS=\"\\\\u\";IGNORECASE=1;RS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-reset-FS",
            ["BEGIN{FS=\"\\\\u\";FS=\"x\";FS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-reset-FPAT",
            ["BEGIN{FPAT=\"\\\\u\";FPAT=\"x\";FPAT=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-reset-RS",
            ["BEGIN{RS=\"\\\\u\";RS=\"x\";RS=\"\\\\u\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-rs-reprepares-fs",
            ["BEGIN{FS=\"\\\\u\";RS=\":\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-rs-input-history",
            ["BEGIN{RS=\"\\\\u\"}{print \"[\"$0\"]\"}"], [])
            .WithStandardInput("aubuc").WithStdOut("[a]\n[b]\n[c]\n")
            .WithStdErr("awk: warning: no hex digits in `\\u' escape sequence\nawk: warning: no hex digits in `\\u' escape sequence\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-invalid-rs",
            ["BEGIN{print \"before\";RS=\"[a\";print \"after\"}"], [])
            .WithStandardInput("").WithStdOut("before\n")
            .WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-invalid-rs-fpat",
            ["BEGIN{FPAT=\".\";print \"before\";RS=\"[a\";print \"after\"}"], [])
            .WithStandardInput("").WithStdOut("before\n")
            .WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-invalid-rs-traditional",
            ["--traditional", "BEGIN{print \"before\";RS=\"[a\";print \"after\"}"], [])
            .WithStandardInput("").WithStdOut("before\n")
            .WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-invalid-rs-posix",
            ["--posix", "BEGIN{RS=\"[a\";print \"ok\";exit}"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("special-regex-invalid-rs-startup",
            ["-v", "RS=[a", "BEGIN{print \"after\"}"], [])
            .WithStandardInput("").WithStdOut("")
            .WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
    }
}
