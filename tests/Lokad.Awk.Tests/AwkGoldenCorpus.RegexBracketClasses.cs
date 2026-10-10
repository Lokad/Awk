namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RegexBracketClassCases()
    {
        var referenceClass = Benchmarking.ReferenceContracts.Cases.Single(contract =>
            contract.Name == "regex-reference-alpha-literal-union");
        yield return AwkGoldenCaseBuilder.Create(referenceClass.Name, [referenceClass.Program], [])
            .WithStdOut(referenceClass.ExpectedOutput).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-0-dynamic",
            ["BEGIN{print \"before\";p=\"[[:word:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-0-literal",
            ["BEGIN{if(0)print \"a\"~ /[[:word:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-1-dynamic",
            ["BEGIN{print \"before\";p=\"[[:ascii:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-1-literal",
            ["BEGIN{if(0)print \"a\"~ /[[:ascii:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-2-dynamic",
            ["BEGIN{print \"before\";p=\"[[:Alpha:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-2-literal",
            ["BEGIN{if(0)print \"a\"~ /[[:Alpha:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-3-dynamic",
            ["BEGIN{print \"before\";p=\"[[:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-3-literal",
            ["BEGIN{if(0)print \"a\"~ /[[:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-4-dynamic",
            ["BEGIN{print \"before\";p=\"[[:digit:][:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-4-literal",
            ["BEGIN{if(0)print \"a\"~ /[[:digit:][:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-5-dynamic",
            ["BEGIN{print \"before\";p=\"[^[:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-5-literal",
            ["BEGIN{if(0)print \"a\"~ /[^[:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-6-dynamic",
            ["BEGIN{print \"before\";p=\"[[:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-6-literal",
            ["BEGIN{if(0)print \"a\"~ /[[:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-7-dynamic",
            ["BEGIN{print \"before\";p=\"[[:digit]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-gnu-7-literal",
            ["BEGIN{if(0)print \"a\"~ /[[:digit]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-alnum",
            ["BEGIN{print \"9\"~/[[:alnum:]]/;print \"9\"~\"[[:alnum:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-alpha",
            ["BEGIN{print \"A\"~/[[:alpha:]]/;print \"A\"~\"[[:alpha:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-blank",
            ["BEGIN{print \"\\t\"~/[[:blank:]]/;print \"\\t\"~\"[[:blank:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-cntrl",
            ["BEGIN{print \"\\000\"~/[[:cntrl:]]/;print \"\\000\"~\"[[:cntrl:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-digit",
            ["BEGIN{print \"4\"~/[[:digit:]]/;print \"4\"~\"[[:digit:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-graph",
            ["BEGIN{print \"+\"~/[[:graph:]]/;print \"+\"~\"[[:graph:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-lower",
            ["BEGIN{print \"z\"~/[[:lower:]]/;print \"z\"~\"[[:lower:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-print",
            ["BEGIN{print \" \"~/[[:print:]]/;print \" \"~\"[[:print:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-punct",
            ["BEGIN{print \"$\"~/[[:punct:]]/;print \"$\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-space",
            ["BEGIN{print \"\\n\"~/[[:space:]]/;print \"\\n\"~\"[[:space:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-upper",
            ["BEGIN{print \"A\"~/[[:upper:]]/;print \"A\"~\"[[:upper:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-gnu-xdigit",
            ["BEGIN{print \"F\"~/[[:xdigit:]]/;print \"F\"~\"[[:xdigit:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-gnu-ordinary-name",
            ["BEGIN{p=\"[:word:]\";print \"w\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-gnu-escaped-opener",
            ["BEGIN{p=\"\\\\[[:word:]]\";print \"[w]\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-gnu-comment",
            ["BEGIN{p=\"(?# [[:word:]] )a\";print \"a\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-gnu-whitespace-comment",
            ["BEGIN{p=\"(?x)# [[:word:]]\\na\";print \"a\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-0-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[[:word:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-0-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[[:word:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-1-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[[:ascii:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-1-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[[:ascii:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-2-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[[:Alpha:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-2-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[[:Alpha:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-3-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[[:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-3-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[[:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-4-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[[:digit:][:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-4-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[[:digit:][:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-5-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[^[:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-5-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[^[:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-6-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[[:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-6-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[[:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-7-dynamic",
            ["--posix", "BEGIN{print \"before\";p=\"[[:digit]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-posix-7-literal",
            ["--posix", "BEGIN{if(0)print \"a\"~ /[[:digit]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-alnum",
            ["--posix", "BEGIN{print \"9\"~/[[:alnum:]]/;print \"9\"~\"[[:alnum:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-alpha",
            ["--posix", "BEGIN{print \"A\"~/[[:alpha:]]/;print \"A\"~\"[[:alpha:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-blank",
            ["--posix", "BEGIN{print \"\\t\"~/[[:blank:]]/;print \"\\t\"~\"[[:blank:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-cntrl",
            ["--posix", "BEGIN{print \"\\000\"~/[[:cntrl:]]/;print \"\\000\"~\"[[:cntrl:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-digit",
            ["--posix", "BEGIN{print \"4\"~/[[:digit:]]/;print \"4\"~\"[[:digit:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-graph",
            ["--posix", "BEGIN{print \"+\"~/[[:graph:]]/;print \"+\"~\"[[:graph:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-lower",
            ["--posix", "BEGIN{print \"z\"~/[[:lower:]]/;print \"z\"~\"[[:lower:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-print",
            ["--posix", "BEGIN{print \" \"~/[[:print:]]/;print \" \"~\"[[:print:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-punct",
            ["--posix", "BEGIN{print \"$\"~/[[:punct:]]/;print \"$\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-space",
            ["--posix", "BEGIN{print \"\\n\"~/[[:space:]]/;print \"\\n\"~\"[[:space:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-upper",
            ["--posix", "BEGIN{print \"A\"~/[[:upper:]]/;print \"A\"~\"[[:upper:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-posix-xdigit",
            ["--posix", "BEGIN{print \"F\"~/[[:xdigit:]]/;print \"F\"~\"[[:xdigit:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-posix-ordinary-name",
            ["--posix", "BEGIN{p=\"[:word:]\";print \"w\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-posix-escaped-opener",
            ["--posix", "BEGIN{p=\"\\\\[[:word:]]\";print \"[w]\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-posix-comment",
            ["--posix", "BEGIN{p=\"(?# [[:word:]] )a\";print \"a\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-posix-whitespace-comment",
            ["--posix", "BEGIN{p=\"(?x)# [[:word:]]\\na\";print \"a\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-0-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[[:word:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-0-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[[:word:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-1-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[[:ascii:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-1-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[[:ascii:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-2-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[[:Alpha:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-2-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[[:Alpha:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-3-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[[:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-3-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[[:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-4-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[[:digit:][:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-4-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[[:digit:][:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-5-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[^[:missing:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-5-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[^[:missing:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-6-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[[:]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-6-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[[:]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-7-dynamic",
            ["--traditional", "BEGIN{print \"before\";p=\"[[:digit]]\";print \"a\"~p}"], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build();
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create("regex-bracket-invalid-traditional-7-literal",
            ["--traditional", "BEGIN{if(0)print \"a\"~ /[[:digit]]/;print \"unexpected\"}"], [])
            .WithStdOut("").WithStdErr("awk: invalid regular expression\n").ExitsWith(2).Build());
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-alnum",
            ["--traditional", "BEGIN{print \"9\"~/[[:alnum:]]/;print \"9\"~\"[[:alnum:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-alpha",
            ["--traditional", "BEGIN{print \"A\"~/[[:alpha:]]/;print \"A\"~\"[[:alpha:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-blank",
            ["--traditional", "BEGIN{print \"\\t\"~/[[:blank:]]/;print \"\\t\"~\"[[:blank:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-cntrl",
            ["--traditional", "BEGIN{print \"\\000\"~/[[:cntrl:]]/;print \"\\000\"~\"[[:cntrl:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-digit",
            ["--traditional", "BEGIN{print \"4\"~/[[:digit:]]/;print \"4\"~\"[[:digit:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-graph",
            ["--traditional", "BEGIN{print \"+\"~/[[:graph:]]/;print \"+\"~\"[[:graph:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-lower",
            ["--traditional", "BEGIN{print \"z\"~/[[:lower:]]/;print \"z\"~\"[[:lower:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-print",
            ["--traditional", "BEGIN{print \" \"~/[[:print:]]/;print \" \"~\"[[:print:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-punct",
            ["--traditional", "BEGIN{print \"$\"~/[[:punct:]]/;print \"$\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-space",
            ["--traditional", "BEGIN{print \"\\n\"~/[[:space:]]/;print \"\\n\"~\"[[:space:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-upper",
            ["--traditional", "BEGIN{print \"A\"~/[[:upper:]]/;print \"A\"~\"[[:upper:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-valid-traditional-xdigit",
            ["--traditional", "BEGIN{print \"F\"~/[[:xdigit:]]/;print \"F\"~\"[[:xdigit:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-traditional-ordinary-name",
            ["--traditional", "BEGIN{p=\"[:word:]\";print \"w\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-traditional-escaped-opener",
            ["--traditional", "BEGIN{p=\"\\\\[[:word:]]\";print \"[w]\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-traditional-comment",
            ["--traditional", "BEGIN{p=\"(?# [[:word:]] )a\";print \"a\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-context-traditional-whitespace-comment",
            ["--traditional", "BEGIN{p=\"(?x)# [[:word:]]\\na\";print \"a\"~p}"], [])
            .WithStdOut("1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-0",
            ["BEGIN{print \"!\"~/[[:punct:]]/;print \"!\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-2",
            ["BEGIN{print \"+\"~/[[:punct:]]/;print \"+\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-3",
            ["BEGIN{print \"<\"~/[[:punct:]]/;print \"<\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-4",
            ["BEGIN{print \"=\"~/[[:punct:]]/;print \"=\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-5",
            ["BEGIN{print \"^\"~/[[:punct:]]/;print \"^\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-6",
            ["BEGIN{print \"|\"~/[[:punct:]]/;print \"|\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-7",
            ["BEGIN{print \"`\"~/[[:punct:]]/;print \"`\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-8",
            ["BEGIN{print \"~\"~/[[:punct:]]/;print \"~\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-9",
            ["BEGIN{print \"\u00a3\"~/[[:punct:]]/;print \"\u00a3\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-10",
            ["BEGIN{print \"\u00b1\"~/[[:punct:]]/;print \"\u00b1\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-gnu-11",
            ["BEGIN{print \"\u00a9\"~/[[:punct:]]/;print \"\u00a9\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-0",
            ["--posix", "BEGIN{print \"!\"~/[[:punct:]]/;print \"!\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-2",
            ["--posix", "BEGIN{print \"+\"~/[[:punct:]]/;print \"+\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-3",
            ["--posix", "BEGIN{print \"<\"~/[[:punct:]]/;print \"<\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-4",
            ["--posix", "BEGIN{print \"=\"~/[[:punct:]]/;print \"=\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-5",
            ["--posix", "BEGIN{print \"^\"~/[[:punct:]]/;print \"^\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-6",
            ["--posix", "BEGIN{print \"|\"~/[[:punct:]]/;print \"|\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-7",
            ["--posix", "BEGIN{print \"`\"~/[[:punct:]]/;print \"`\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-8",
            ["--posix", "BEGIN{print \"~\"~/[[:punct:]]/;print \"~\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-9",
            ["--posix", "BEGIN{print \"\u00a3\"~/[[:punct:]]/;print \"\u00a3\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-10",
            ["--posix", "BEGIN{print \"\u00b1\"~/[[:punct:]]/;print \"\u00b1\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-posix-11",
            ["--posix", "BEGIN{print \"\u00a9\"~/[[:punct:]]/;print \"\u00a9\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-0",
            ["--traditional", "BEGIN{print \"!\"~/[[:punct:]]/;print \"!\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-2",
            ["--traditional", "BEGIN{print \"+\"~/[[:punct:]]/;print \"+\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-3",
            ["--traditional", "BEGIN{print \"<\"~/[[:punct:]]/;print \"<\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-4",
            ["--traditional", "BEGIN{print \"=\"~/[[:punct:]]/;print \"=\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-5",
            ["--traditional", "BEGIN{print \"^\"~/[[:punct:]]/;print \"^\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-6",
            ["--traditional", "BEGIN{print \"|\"~/[[:punct:]]/;print \"|\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-7",
            ["--traditional", "BEGIN{print \"`\"~/[[:punct:]]/;print \"`\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-8",
            ["--traditional", "BEGIN{print \"~\"~/[[:punct:]]/;print \"~\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-9",
            ["--traditional", "BEGIN{print \"\u00a3\"~/[[:punct:]]/;print \"\u00a3\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-10",
            ["--traditional", "BEGIN{print \"\u00b1\"~/[[:punct:]]/;print \"\u00b1\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("regex-bracket-punct-symbol-traditional-11",
            ["--traditional", "BEGIN{print \"\u00a9\"~/[[:punct:]]/;print \"\u00a9\"~\"[[:punct:]]\"}"], [])
            .WithStdOut("1\n1\n").WithStdErr("").ExitsWith(0).Build();
    }
}
