namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> StrtonumConversionCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-0",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-0",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-0",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-0",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-0",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-0",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-0",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-0",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-0",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-0",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-1",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0\"))}"],
                [])
            .WithStdOut("[-0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-1",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[-0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-1",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[-0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-1",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0\"))}"],
                [])
            .WithStdOut("[-0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-1",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[-0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-1",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[-0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-1",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-1",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-1",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-1",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-2",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-2",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-2",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-2",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-2",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-2",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-2",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-2",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-2",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-2",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-3",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-3",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-3",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-3",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-3",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-3",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-3",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-3",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-3",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-3",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-4",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+012\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-4",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-4",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-4",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+012\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-4",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-4",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-4",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-4",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-4",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-4",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-5",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-012\"))}"],
                [])
            .WithStdOut("[-0x1.8p+3] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-5",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-5",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-5",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-012\"))}"],
                [])
            .WithStdOut("[-0x1.8p+3] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-5",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-5",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-5",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-5",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-5",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-5",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-012|")
            .WithStdOut("[-0x1.8p+3] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-6",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 012\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-6",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-6",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-6",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 012\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-6",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-6",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-6",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-6",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-6",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-6",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 012|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-7",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\\v012\\f\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-7",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-7",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-7",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\\v012\\f\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-7",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-7",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-7",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-7",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-7",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-7",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b012\f|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-8",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0109\"))}"],
                [])
            .WithStdOut("[0x1.b4p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-8",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-8",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-8",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0109\"))}"],
                [])
            .WithStdOut("[0x1.b4p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-8",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-8",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-8",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-8",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-8",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-8",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0109|")
            .WithStdOut("[0x1.b4p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-9",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"018\"))}"],
                [])
            .WithStdOut("[0x1.2p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-9",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-9",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-9",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"018\"))}"],
                [])
            .WithStdOut("[0x1.2p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-9",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-9",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-9",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-9",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-9",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-9",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("018|")
            .WithStdOut("[0x1.2p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-10",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"089\"))}"],
                [])
            .WithStdOut("[0x1.64p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-10",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-10",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-10",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"089\"))}"],
                [])
            .WithStdOut("[0x1.64p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-10",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-10",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-10",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-10",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-10",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-10",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("089|")
            .WithStdOut("[0x1.64p+6] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-11",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012e2\"))}"],
                [])
            .WithStdOut("[0x1.2cp+10] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-11",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-11",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-11",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012e2\"))}"],
                [])
            .WithStdOut("[0x1.2cp+10] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-11",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-11",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-11",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-11",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-11",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-11",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e2|")
            .WithStdOut("[0x1.2cp+10] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-12",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012e\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-12",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-12",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-12",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012e\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-12",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-12",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-12",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-12",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-12",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-12",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012e|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-13",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012.5\"))}"],
                [])
            .WithStdOut("[0x1.9p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-13",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-13",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-13",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012.5\"))}"],
                [])
            .WithStdOut("[0x1.9p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-13",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-13",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-13",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-13",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-13",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-13",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012.5|")
            .WithStdOut("[0x1.9p+3] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-14",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012z\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-14",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-14",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-14",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012z\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-14",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-14",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-14",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-14",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-14",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-14",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-15",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012z.5\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-15",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-15",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-15",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"012z.5\"))}"],
                [])
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-15",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-15",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-15",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-15",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.4p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-15",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-15",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("012z.5|")
            .WithStdOut("[0x1.8p+3] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-16",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x10\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-16",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-16",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-16",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x10\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-16",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-16",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-16",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-16",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-16",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-16",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-17",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0Xff\"))}"],
                [])
            .WithStdOut("[0x1.fep+7] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-17",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x1.fep+7] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-17",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x1.fep+7] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-17",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0Xff\"))}"],
                [])
            .WithStdOut("[0x1.fep+7] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-17",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x1.fep+7] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-17",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x1.fep+7] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-17",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-17",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x1.fep+7] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-17",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-17",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xff|")
            .WithStdOut("[0x1.fep+7] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-18",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0x10\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-18",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-18",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-18",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0x10\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-18",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-18",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-18",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-18",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-18",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-18",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-19",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x10\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-gnu-19",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-gnu-19",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-19",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x10\"))}"],
                [])
            .WithStdOut("[-0x1p+4] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-record-nondecimal-19",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum($0))}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[-0x1p+4] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-string-nondecimal-19",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit(strtonum(substr($0,1)))}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[-0x1p+4] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-19",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-19",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[-0x1p+4] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-19",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-19",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x10|")
            .WithStdOut("[-0x1p+4] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-20",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 0x10\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-20",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 0x10\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-20",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-20",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-20",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-20",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x10|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-21",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\\v0x10\\f\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-21",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\\v0x10\\f\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-21",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-21",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-21",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-21",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("\u000b0x10\f|")
            .WithStdOut("[0x1p+4] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-22",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x10z\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-22",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x10z\"))}"],
                [])
            .WithStdOut("[0x1p+4] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-22",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-22",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x1p+4] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-22",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-22",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x10z|")
            .WithStdOut("[0x1p+4] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-23",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-23",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-23",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-23",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-23",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-23",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-24",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0xz\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-24",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0xz\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-24",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-24",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-24",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-24",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-25",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x.\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-25",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x.\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-25",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-25",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-25",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-25",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-26",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x.p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-26",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x.p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-26",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-26",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-26",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-26",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-27",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0xp2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-27",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0xp2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-27",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-27",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-27",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-27",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0xp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-28",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1p2\"))}"],
                [])
            .WithStdOut("[0x1p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-28",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1p2\"))}"],
                [])
            .WithStdOut("[0x1p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-28",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-28",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x1p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-28",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-28",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p2|")
            .WithStdOut("[0x1p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-29",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-29",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-29",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-29",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-29",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-29",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-30",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-30",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-30",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-30",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-30",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-30",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-31",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-31",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-31",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-31",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-31",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-31",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-32",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p+\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-32",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p+\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-32",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-32",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-32",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-32",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p+|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-33",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p-\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-33",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p-\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-33",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-33",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-33",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-33",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p-|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-34",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p2tail\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-34",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8p2tail\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-34",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-34",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-34",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-34",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8p2tail|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-35",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8xyzp2\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-35",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8xyzp2\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-35",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-35",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-35",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-35",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyzp2|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-36",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8xyz\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-36",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.8xyz\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-36",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-36",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-36",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-36",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.8xyz|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-37",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0x1.8\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-37",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0x1.8\"))}"],
                [])
            .WithStdOut("[0x1.8p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-37",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-37",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-37",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-37",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-38",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x1.8\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-38",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x1.8\"))}"],
                [])
            .WithStdOut("[-0x1.8p+0] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-38",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-38",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[-0x1.8p+0] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-38",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-38",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8|")
            .WithStdOut("[-0x1.8p+0] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-39",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 0x1.8\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-39",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 0x1.8\"))}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-39",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-39",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-39",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-39",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput(" 0x1.8|")
            .WithStdOut("[0x1.8p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-40",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-40",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x1.8p+2] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-40",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-40",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-40",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-40",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("+0x1.8p2|")
            .WithStdOut("[0x1.8p+2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-41",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x1.8p2\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-41",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x1.8p2\"))}"],
                [])
            .WithStdOut("[-0x1.8p+2] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-41",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-41",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[-0x1.8p+2] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-41",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-41",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("-0x1.8p2|")
            .WithStdOut("[-0x1.8p+2] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-42",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x.8p1\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-42",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x.8p1\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-42",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-42",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x1p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-42",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-42",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x.8p1|")
            .WithStdOut("[0x1p+0] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-43",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0Xf.FP-2\"))}"],
                [])
            .WithStdOut("[0x1.fep+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-43",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0Xf.FP-2\"))}"],
                [])
            .WithStdOut("[0x1.fep+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-43",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-43",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x1.fep+1] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-43",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-43",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0Xf.FP-2|")
            .WithStdOut("[0x1.fep+1] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-44",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-44",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-44",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-44",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-44",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-44",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-45",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1p-1075\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-45",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1p-1075\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-45",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-45",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-45",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-45",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-46",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.0000000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-46",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.0000000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-46",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-46",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-46",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-46",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.0000000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-47",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffffp1023\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-47",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffffp1023\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-47",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-47",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-47",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-47",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffffp1023|")
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-48",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p1023\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-48",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p1023\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-48",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-48",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-48",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("[0x0p+0] 1 0 0\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-48",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-49",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1.2x\"))}"],
                [])
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-49",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1.2x\"))}"],
                [])
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-gnu-49",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-nondecimal-49",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-traditional-49",
                ["--traditional", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-conversion-posix-49",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{x=$0+0;emit(x);print($0==x)}"],
                [])
            .WithStandardInput("1.2x|")
            .WithStdOut("[0x1.3333333333333p+0] 0 0 1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-50",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-50",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-51",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-51",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-52",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e+\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-52",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e+\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-53",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e-\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-53",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e-\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-54",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e+2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-54",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"1e+2x\"))}"],
                [])
            .WithStdOut("[0x1.9p+6] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-55",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\".5x\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-55",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\".5x\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-56",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+.5z\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-56",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+.5z\"))}"],
                [])
            .WithStdOut("[0x1p-1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-57",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-2.5z\"))}"],
                [])
            .WithStdOut("[-0x1.4p+1] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-57",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-2.5z\"))}"],
                [])
            .WithStdOut("[-0x1.4p+1] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-58",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 12abc \"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-58",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" 12abc \"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-59",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\\t12abc\\r\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-59",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\\t12abc\\r\"))}"],
                [])
            .WithStdOut("[0x1.8p+3] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-60",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+inf\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-60",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+inf\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-61",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-inf\"))}"],
                [])
            .WithStdOut("-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-61",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-inf\"))}"],
                [])
            .WithStdOut("-inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-62",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"inf\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-62",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"inf\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-63",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"Infinity\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-63",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"Infinity\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-64",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+nan\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-64",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+nan\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-65",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-NaN\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-65",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-NaN\"))}"],
                [])
            .WithStdOut("nan\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-66",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"nan\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-66",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"nan\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-67",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+infx\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-67",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+infx\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-68",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+nan(payload)\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-68",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+nan(payload)\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-69",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-69",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-70",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" \"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-70",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\" \"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-71",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\".\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-71",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\".\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-72",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-72",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"+\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-73",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-73",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-gnu-74",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\u00a012\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-literal-nondecimal-74",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"\u00a012\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-0",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=1/3;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1.5555555555555p-2] 0 0 1\n[0x1.5555555555555p-2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-0",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=1/3;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1.5555555555555p-2] 0 0 1\n[0x1.5555555555555p-2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-1",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=-0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x0p+0] 1 0 0\n[-0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-1",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=-0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x0p+0] 1 0 0\n[-0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-2",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-2",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-3",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1p+60] 0 0 1\n[0x1p+60] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-3",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x1p+60] 0 0 1\n[0x1p+60] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-4",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=-2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x1p+60] 0 1 0\n[-0x1p+60] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-4",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=-2^60;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[-0x1p+60] 0 1 0\n[-0x1p+60] 0 1 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-5",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=4.9406564584124654e-324;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-5",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=4.9406564584124654e-324;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n[0x0.0000000000001p-1022] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-6",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"+nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-6",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"+nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-7",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"-nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-7",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"-nan\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("nan\nnan\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-8",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"+inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("+inf\n+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-8",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"+inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("+inf\n+inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-gnu-9",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"-inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("-inf\n-inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-typed-nondecimal-9",
                ["--non-decimal-data", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{CONVFMT=\"%.1f\";x=\"-inf\"+0;emit(x);emit(strtonum(x));print(x==strtonum(x))}"],
                [])
            .WithStdOut("-inf\n-inf\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-once",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} function f(){i++;return 1/3}BEGIN{emit(strtonum(f()));print i}"],
                [])
            .WithStdOut("[0x1.5555555555555p-2] 0 0 1\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-unassigned",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(x));print x==\"\"}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-fatal-after-output",
                ["BEGIN{print \"before\";x=strtonum(\"0x1.8\");print \"bad\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-0",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print s}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-1",
                ["--non-decimal-data", "BEGIN{print \"0x1.8\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-2",
                ["--non-decimal-data", "{print $0}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-3",
                ["--non-decimal-data", "{x=$0;print \"okay\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("okay\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-4",
                ["--non-decimal-data", "{print length($0)}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-5",
                ["--non-decimal-data", "{print typeof($0)}"],
                [])
            .WithStandardInput("0x1.8\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-6",
                ["--non-decimal-data", "{print $0==\"0x1.8\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-7",
                ["--non-decimal-data", "{if($0)print \"okay\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: hexadecimal floating point constants require an exponent\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-8",
                ["--non-decimal-data", "{printf \"%s\\n\",$0}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("0x1.8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-9",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print length(s)}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-10",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";print(s==\"0x1.8\")}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-11",
                ["--non-decimal-data", "BEGIN{s=\"0x1.8\";if(s)print \"okay\"}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("okay\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-lazy-12",
                ["--non-decimal-data", "BEGIN{print strtonum(\"+0x1.8\")}"],
                [])
            .WithStandardInput("0x1.8\n")
            .WithStdOut("1.5\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-0",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-0",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-1",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-1",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-2",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008p-1023\"))}"],
                [])
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-2",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1023|")
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-3",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008p-1022\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-3",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p-1022|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-4",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008p0\"))}"],
                [])
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-4",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p0|")
            .WithStdOut("[0x1p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-5",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008p1023\"))}"],
                [])
            .WithStdOut("[0x1p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-5",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p1023|")
            .WithStdOut("[0x1p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-6",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-6",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-7",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-7",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-8",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-8",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-9",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1023\"))}"],
                [])
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-9",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1023|")
            .WithStdOut("[0x0.8p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-10",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p-1022\"))}"],
                [])
            .WithStdOut("[0x1.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-10",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p-1022|")
            .WithStdOut("[0x1.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-11",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p0\"))}"],
                [])
            .WithStdOut("[0x1.0000000000001p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-11",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p0|")
            .WithStdOut("[0x1.0000000000001p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-12",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p1023\"))}"],
                [])
            .WithStdOut("[0x1.0000000000001p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-12",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p1023|")
            .WithStdOut("[0x1.0000000000001p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-13",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000008000000000001p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-13",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000008000000000001p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-14",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000018p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-14",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-15",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000018p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-15",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1074|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-16",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000018p-1023\"))}"],
                [])
            .WithStdOut("[0x0.8000000000001p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-16",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1023|")
            .WithStdOut("[0x0.8000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-17",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000018p-1022\"))}"],
                [])
            .WithStdOut("[0x1.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-17",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p-1022|")
            .WithStdOut("[0x1.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-18",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000018p0\"))}"],
                [])
            .WithStdOut("[0x1.0000000000002p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-18",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p0|")
            .WithStdOut("[0x1.0000000000002p+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-19",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000018p1023\"))}"],
                [])
            .WithStdOut("[0x1.0000000000002p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-19",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p1023|")
            .WithStdOut("[0x1.0000000000002p+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-20",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.00000000000018p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-20",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.00000000000018p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-21",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-21",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-22",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-22",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1074|")
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-23",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1023\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-23",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1023|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-24",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff7p-1022\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-24",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p-1022|")
            .WithStdOut("[0x1.fffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-25",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff7p0\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-25",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p0|")
            .WithStdOut("[0x1.fffffffffffffp+0] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-26",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff7p1023\"))}"],
                [])
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-26",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p1023|")
            .WithStdOut("[0x1.fffffffffffffp+1023] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-27",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff7p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-27",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff7p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-28",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-28",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-29",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-29",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1074|")
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-30",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1023\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-30",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1023|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-31",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p-1022\"))}"],
                [])
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-31",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p-1022|")
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-32",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p0\"))}"],
                [])
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-32",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p0|")
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-33",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1023|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-34",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff8p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-34",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff8p1024|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-35",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1075\"))}"],
                [])
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-35",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1075|")
            .WithStdOut("[0x0.0000000000001p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-36",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1074\"))}"],
                [])
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-36",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1074|")
            .WithStdOut("[0x0.0000000000002p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-37",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1023\"))}"],
                [])
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-37",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1023|")
            .WithStdOut("[0x1p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-38",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p-1022\"))}"],
                [])
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-38",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p-1022|")
            .WithStdOut("[0x1p-1021] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-39",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p0\"))}"],
                [])
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-39",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p0|")
            .WithStdOut("[0x1p+1] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-40",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p1023\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-40",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p1023|")
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-41",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x1.fffffffffffff800000000001p1024\"))}"],
                [])
            .WithStdOut("+inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-41",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x1.fffffffffffff800000000001p1024|")
            .WithStdOut("+inf\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-199",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x30000002222225p-1077\"))}"],
                [])
            .WithStdOut("[0x0.6000000444445p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-199",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x30000002222225p-1077|")
            .WithStdOut("[0x0.6000000444445p-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-200",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x0.7fffffffffffeap-1022\"))}"],
                [])
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-200",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x0.7fffffffffffeap-1022|")
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-201",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x0.7fffffffffffe9p-1022\"))}"],
                [])
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-201",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x0.7fffffffffffe9p-1022|")
            .WithStdOut("[0x0.7ffffffffffffp-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-202",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"0x0.70000000000002p-1022\"))}"],
                [])
            .WithStdOut("[0x0.7p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-202",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("0x0.70000000000002p-1022|")
            .WithStdOut("[0x0.7p-1022] 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-203",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x30000002222225p-1077\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-203",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x30000002222225p-1077|")
            .WithStdOut("[-0x0.6000000444445p-1022] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-204",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x0.7fffffffffffeap-1022\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        // Correct subnormal rounding; the pinned libc has upstream bug 30220.
        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-204",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x0.7fffffffffffeap-1022|")
            .WithStdOut("[-0x0.7ffffffffffffp-1022] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-205",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x0.7fffffffffffe9p-1022\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-205",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x0.7fffffffffffe9p-1022|")
            .WithStdOut("[-0x0.7ffffffffffffp-1022] 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-strtonum-206",
                ["function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{emit(strtonum(\"-0x0.70000000000002p-1022\"))}"],
                [])
            .WithStdOut("[0x0p+0] 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("conversion-hex-posix-206",
                ["--posix", "function emit(x){if(x!=x){print \"nan\";return}if(x>1.7976931348623157e308){print \"+inf\";return}if(x< -1.7976931348623157e308){print \"-inf\";return}printf \"[%a] %d %d %d\\n\",x,(x==0),(x<0),(x>0)} BEGIN{RS=\"|\"}{emit($0+0)}"],
                [])
            .WithStandardInput("-0x0.70000000000002p-1022|")
            .WithStdOut("[-0x0.7p-1022] 0 1 0\n")
            .Build();

    }
}
