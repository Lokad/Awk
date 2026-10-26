namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NumericSemanticsCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("numeric-default-general-exponents",
                ["BEGIN{x=1.23456789e-9;tiny=4.9406564584124654e-324;print x,-x,tiny;" +
                 "print \"[\" (x \"\") \"]\",\"[\" (tiny \"\") \"]\";" +
                 "OFMT=\"%.2E\";CONVFMT=\"%.3E\";print x,\"[\" (x \"\") \"]\"}"], [])
            .WithStdOut("1.23457e-09 -1.23457e-09 4.94066e-324\n" +
                "[1.23457e-09] [4.94066e-324]\n1.23E-09 [1.235E-09]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-0",
                ["BEGIN{x=(-1)^0.5;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-1",
                ["BEGIN{x=sqrt(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-2",
                ["BEGIN{x=log(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-3",
                ["BEGIN{x=sqrt(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-4",
                ["BEGIN{x=log(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-5",
                ["BEGIN{x=sin(\"+inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-6",
                ["BEGIN{x=cos(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-7",
                ["BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-8",
                ["BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-9",
                ["BEGIN{x=0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-10",
                ["BEGIN{x=-0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-11",
                ["BEGIN{x=4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-12",
                ["BEGIN{x=-4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-13",
                ["BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-14",
                ["BEGIN{x=\"-inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-15",
                ["BEGIN{x=1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-16",
                ["BEGIN{x=-1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-17",
                ["BEGIN{x=sqrt(-0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-18",
                ["BEGIN{x=log(0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-19",
                ["BEGIN{x=sqrt(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-20",
                ["BEGIN{x=log(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-21",
                ["BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-22",
                ["BEGIN{x=sin(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-gnu-23",
                ["BEGIN{x=cos(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-0",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-0",
                ["BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-1",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-NaN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-1",
                ["BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-2",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+NAN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-2",
                ["BEGIN{x=\"+NAN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-3",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-3",
                ["BEGIN{x=\"nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-4",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("NaN\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-4",
                ["BEGIN{x=\"NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-5",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nanx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-5",
                ["BEGIN{x=\"+nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-6",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nanx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-6",
                ["BEGIN{x=\"-nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-7",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan(payload)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-7",
                ["BEGIN{x=\"+nan(payload)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-8",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-8",
                ["BEGIN{x=\"nan(123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-9",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan()\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-9",
                ["BEGIN{x=\"nan()\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-10",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(_abc123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-10",
                ["BEGIN{x=\"nan(_abc123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-11",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(a-b)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-11",
                ["BEGIN{x=\"nan(a-b)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-12",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-12",
                ["BEGIN{x=\"nan(\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-13",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(abc)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-13",
                ["BEGIN{x=\"nan(abc)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-14",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nan(0x123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-14",
                ["BEGIN{x=\"-nan(0x123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-15",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-15",
                ["BEGIN{x=\"+nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-16",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("\f-nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-16",
                ["BEGIN{x=\"\\f-nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-17",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+inf\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-17",
                ["BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-18",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-Inf\n")
            .WithStdOut("1 0 1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-18",
                ["BEGIN{x=\"-Inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-19",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("inf\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-19",
                ["BEGIN{x=\"inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-20",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("Infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-20",
                ["BEGIN{x=\"Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-21",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+Infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-21",
                ["BEGIN{x=\"+Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-22",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-22",
                ["BEGIN{x=\"-infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-23",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+infx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-23",
                ["BEGIN{x=\"+infx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-24",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infxyz\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-24",
                ["BEGIN{x=\"-infxyz\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-25",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("InfINiTy\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-25",
                ["BEGIN{x=\"InfINiTy\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-26",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinit\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-26",
                ["BEGIN{x=\"infinit\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-gnu-27",
                ["{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinitytail\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-gnu-27",
                ["BEGIN{x=\"infinitytail\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-0",
                ["BEGIN{x=exp(-1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-1",
                ["BEGIN{x=exp(-800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-2",
                ["BEGIN{x=exp(-750);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -750 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-3",
                ["BEGIN{x=exp(-745.1332191019411);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-4",
                ["BEGIN{x=exp(-745.1332191019412);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -745.133 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-5",
                ["BEGIN{x=exp(-745);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-6",
                ["BEGIN{x=exp(-744);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-7",
                ["BEGIN{x=exp(-710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-8",
                ["BEGIN{x=exp(-709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-9",
                ["BEGIN{x=exp(-708.3964185322642);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-10",
                ["BEGIN{x=exp(-708.3964185322641);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-11",
                ["BEGIN{x=exp(-708);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-12",
                ["BEGIN{x=exp(-700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-13",
                ["BEGIN{x=exp(-1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-14",
                ["BEGIN{x=exp(-0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-15",
                ["BEGIN{x=exp(0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-16",
                ["BEGIN{x=exp(1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-17",
                ["BEGIN{x=exp(700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-18",
                ["BEGIN{x=exp(709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-19",
                ["BEGIN{x=exp(709.7827128933839);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-20",
                ["BEGIN{x=exp(709.782712893384);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-21",
                ["BEGIN{x=exp(709.7827128933841);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 709.783 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-22",
                ["BEGIN{x=exp(710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 710 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-23",
                ["BEGIN{x=exp(800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-24",
                ["BEGIN{x=exp(1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-25",
                ["BEGIN{x=exp(1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-26",
                ["BEGIN{x=exp(-1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-27",
                ["BEGIN{x=exp(\"+inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-28",
                ["BEGIN{x=exp(\"-inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-gnu-29",
                ["BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-gnu-0",
                ["BEGIN{s=\" 2 \";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-gnu-1",
                ["BEGIN{s=\"\\t2\\t\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-gnu-2",
                ["BEGIN{s=\"\\n2\\n\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-gnu-3",
                ["BEGIN{s=\"\\r2\\r\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-gnu-4",
                ["BEGIN{s=\"\\v2\\v\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-gnu-5",
                ["BEGIN{s=\"\\f2\\f\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-0-leading",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-0-trailing",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u000b|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-0-both",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2\u000b|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-1-leading",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-1-trailing",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\f|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-1-both",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2\f|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-2-leading",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-2-trailing",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\r|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-2-both",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2\r|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-3-leading",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-3-trailing",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u00a0|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-3-both",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02\u00a0|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-4-leading",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-4-trailing",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u2003|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-4-both",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032\u2003|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-5-leading",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-5-trailing",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u0085|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-gnu-5-both",
                ["BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852\u0085|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-discard-gnu",
                ["BEGIN{sqrt(-1);log(-1);exp(1000);exp(-1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: warning: log: received negative argument -1\nawk: warning: exp: argument 1000 is out of range\nawk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-configuration-gnu",
                ["BEGIN{CONVFMT=\"%.1f\";OFMT=\"%.1f\";sqrt(-123456789);log(-0.00000123);exp(1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1.23457e+08\nawk: warning: log: received negative argument -1.23e-06\nawk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-before-fatal-gnu",
                ["BEGIN{print \"before\";x=sqrt(-1);z=0;y=1/z;print \"bad\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: division by zero attempted\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-0",
                ["--non-decimal-data", "BEGIN{x=(-1)^0.5;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-1",
                ["--non-decimal-data", "BEGIN{x=sqrt(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-2",
                ["--non-decimal-data", "BEGIN{x=log(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-3",
                ["--non-decimal-data", "BEGIN{x=sqrt(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-4",
                ["--non-decimal-data", "BEGIN{x=log(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-5",
                ["--non-decimal-data", "BEGIN{x=sin(\"+inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-6",
                ["--non-decimal-data", "BEGIN{x=cos(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-7",
                ["--non-decimal-data", "BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-8",
                ["--non-decimal-data", "BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-9",
                ["--non-decimal-data", "BEGIN{x=0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-10",
                ["--non-decimal-data", "BEGIN{x=-0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-11",
                ["--non-decimal-data", "BEGIN{x=4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-12",
                ["--non-decimal-data", "BEGIN{x=-4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-13",
                ["--non-decimal-data", "BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-14",
                ["--non-decimal-data", "BEGIN{x=\"-inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-15",
                ["--non-decimal-data", "BEGIN{x=1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-16",
                ["--non-decimal-data", "BEGIN{x=-1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-17",
                ["--non-decimal-data", "BEGIN{x=sqrt(-0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-18",
                ["--non-decimal-data", "BEGIN{x=log(0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-19",
                ["--non-decimal-data", "BEGIN{x=sqrt(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-20",
                ["--non-decimal-data", "BEGIN{x=log(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-21",
                ["--non-decimal-data", "BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-22",
                ["--non-decimal-data", "BEGIN{x=sin(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-nondecimal-23",
                ["--non-decimal-data", "BEGIN{x=cos(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-0",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-0",
                ["--non-decimal-data", "BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-1",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-NaN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-1",
                ["--non-decimal-data", "BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-2",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+NAN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-2",
                ["--non-decimal-data", "BEGIN{x=\"+NAN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-3",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-3",
                ["--non-decimal-data", "BEGIN{x=\"nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-4",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("NaN\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-4",
                ["--non-decimal-data", "BEGIN{x=\"NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-5",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nanx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-5",
                ["--non-decimal-data", "BEGIN{x=\"+nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-6",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nanx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-6",
                ["--non-decimal-data", "BEGIN{x=\"-nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-7",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan(payload)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-7",
                ["--non-decimal-data", "BEGIN{x=\"+nan(payload)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-8",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-8",
                ["--non-decimal-data", "BEGIN{x=\"nan(123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-9",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan()\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-9",
                ["--non-decimal-data", "BEGIN{x=\"nan()\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-10",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(_abc123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-10",
                ["--non-decimal-data", "BEGIN{x=\"nan(_abc123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-11",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(a-b)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-11",
                ["--non-decimal-data", "BEGIN{x=\"nan(a-b)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-12",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-12",
                ["--non-decimal-data", "BEGIN{x=\"nan(\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-13",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(abc)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-13",
                ["--non-decimal-data", "BEGIN{x=\"nan(abc)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-14",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nan(0x123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-14",
                ["--non-decimal-data", "BEGIN{x=\"-nan(0x123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-15",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-15",
                ["--non-decimal-data", "BEGIN{x=\"+nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-16",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("\f-nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-16",
                ["--non-decimal-data", "BEGIN{x=\"\\f-nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-17",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+inf\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-17",
                ["--non-decimal-data", "BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-18",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-Inf\n")
            .WithStdOut("1 0 1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-18",
                ["--non-decimal-data", "BEGIN{x=\"-Inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-19",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("inf\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-19",
                ["--non-decimal-data", "BEGIN{x=\"inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-20",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("Infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-20",
                ["--non-decimal-data", "BEGIN{x=\"Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-21",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+Infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-21",
                ["--non-decimal-data", "BEGIN{x=\"+Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-22",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-22",
                ["--non-decimal-data", "BEGIN{x=\"-infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-23",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+infx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-23",
                ["--non-decimal-data", "BEGIN{x=\"+infx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-24",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infxyz\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-24",
                ["--non-decimal-data", "BEGIN{x=\"-infxyz\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-25",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("InfINiTy\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-25",
                ["--non-decimal-data", "BEGIN{x=\"InfINiTy\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-26",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinit\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-26",
                ["--non-decimal-data", "BEGIN{x=\"infinit\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-nondecimal-27",
                ["--non-decimal-data", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinitytail\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-nondecimal-27",
                ["--non-decimal-data", "BEGIN{x=\"infinitytail\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-0",
                ["--non-decimal-data", "BEGIN{x=exp(-1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-1",
                ["--non-decimal-data", "BEGIN{x=exp(-800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-2",
                ["--non-decimal-data", "BEGIN{x=exp(-750);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -750 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-3",
                ["--non-decimal-data", "BEGIN{x=exp(-745.1332191019411);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-4",
                ["--non-decimal-data", "BEGIN{x=exp(-745.1332191019412);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -745.133 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-5",
                ["--non-decimal-data", "BEGIN{x=exp(-745);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-6",
                ["--non-decimal-data", "BEGIN{x=exp(-744);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-7",
                ["--non-decimal-data", "BEGIN{x=exp(-710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-8",
                ["--non-decimal-data", "BEGIN{x=exp(-709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-9",
                ["--non-decimal-data", "BEGIN{x=exp(-708.3964185322642);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-10",
                ["--non-decimal-data", "BEGIN{x=exp(-708.3964185322641);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-11",
                ["--non-decimal-data", "BEGIN{x=exp(-708);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-12",
                ["--non-decimal-data", "BEGIN{x=exp(-700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-13",
                ["--non-decimal-data", "BEGIN{x=exp(-1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-14",
                ["--non-decimal-data", "BEGIN{x=exp(-0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-15",
                ["--non-decimal-data", "BEGIN{x=exp(0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-16",
                ["--non-decimal-data", "BEGIN{x=exp(1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-17",
                ["--non-decimal-data", "BEGIN{x=exp(700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-18",
                ["--non-decimal-data", "BEGIN{x=exp(709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-19",
                ["--non-decimal-data", "BEGIN{x=exp(709.7827128933839);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-20",
                ["--non-decimal-data", "BEGIN{x=exp(709.782712893384);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-21",
                ["--non-decimal-data", "BEGIN{x=exp(709.7827128933841);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 709.783 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-22",
                ["--non-decimal-data", "BEGIN{x=exp(710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 710 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-23",
                ["--non-decimal-data", "BEGIN{x=exp(800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-24",
                ["--non-decimal-data", "BEGIN{x=exp(1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-25",
                ["--non-decimal-data", "BEGIN{x=exp(1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-26",
                ["--non-decimal-data", "BEGIN{x=exp(-1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-27",
                ["--non-decimal-data", "BEGIN{x=exp(\"+inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-28",
                ["--non-decimal-data", "BEGIN{x=exp(\"-inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-nondecimal-29",
                ["--non-decimal-data", "BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-nondecimal-0",
                ["--non-decimal-data", "BEGIN{s=\" 2 \";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-nondecimal-1",
                ["--non-decimal-data", "BEGIN{s=\"\\t2\\t\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-nondecimal-2",
                ["--non-decimal-data", "BEGIN{s=\"\\n2\\n\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-nondecimal-3",
                ["--non-decimal-data", "BEGIN{s=\"\\r2\\r\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-nondecimal-4",
                ["--non-decimal-data", "BEGIN{s=\"\\v2\\v\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-nondecimal-5",
                ["--non-decimal-data", "BEGIN{s=\"\\f2\\f\";print s+0,!(s+0),(s==2);print strtonum(s)}"],
                [])
            .WithStdOut("2 0 0\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-0-leading",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-0-trailing",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u000b|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-0-both",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2\u000b|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-1-leading",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-1-trailing",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\f|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-1-both",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2\f|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-2-leading",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-2-trailing",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\r|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-2-both",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2\r|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-3-leading",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-3-trailing",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u00a0|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-3-both",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02\u00a0|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-4-leading",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-4-trailing",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u2003|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-4-both",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032\u2003|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-5-leading",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-5-trailing",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u0085|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-nondecimal-5-both",
                ["--non-decimal-data", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852\u0085|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-discard-nondecimal",
                ["--non-decimal-data", "BEGIN{sqrt(-1);log(-1);exp(1000);exp(-1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: warning: log: received negative argument -1\nawk: warning: exp: argument 1000 is out of range\nawk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-configuration-nondecimal",
                ["--non-decimal-data", "BEGIN{CONVFMT=\"%.1f\";OFMT=\"%.1f\";sqrt(-123456789);log(-0.00000123);exp(1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1.23457e+08\nawk: warning: log: received negative argument -1.23e-06\nawk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-before-fatal-nondecimal",
                ["--non-decimal-data", "BEGIN{print \"before\";x=sqrt(-1);z=0;y=1/z;print \"bad\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: division by zero attempted\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-0",
                ["--traditional", "BEGIN{x=(-1)^0.5;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-1",
                ["--traditional", "BEGIN{x=sqrt(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-2",
                ["--traditional", "BEGIN{x=log(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-3",
                ["--traditional", "BEGIN{x=sqrt(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-4",
                ["--traditional", "BEGIN{x=log(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-5",
                ["--traditional", "BEGIN{x=sin(\"+inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-6",
                ["--traditional", "BEGIN{x=cos(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-7",
                ["--traditional", "BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-8",
                ["--traditional", "BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-9",
                ["--traditional", "BEGIN{x=0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-10",
                ["--traditional", "BEGIN{x=-0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-11",
                ["--traditional", "BEGIN{x=4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-12",
                ["--traditional", "BEGIN{x=-4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-13",
                ["--traditional", "BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-14",
                ["--traditional", "BEGIN{x=\"-inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-15",
                ["--traditional", "BEGIN{x=1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-16",
                ["--traditional", "BEGIN{x=-1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-17",
                ["--traditional", "BEGIN{x=sqrt(-0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-18",
                ["--traditional", "BEGIN{x=log(0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-19",
                ["--traditional", "BEGIN{x=sqrt(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-20",
                ["--traditional", "BEGIN{x=log(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-21",
                ["--traditional", "BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-22",
                ["--traditional", "BEGIN{x=sin(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-traditional-23",
                ["--traditional", "BEGIN{x=cos(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-0",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-0",
                ["--traditional", "BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-1",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-NaN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-1",
                ["--traditional", "BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-2",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+NAN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-2",
                ["--traditional", "BEGIN{x=\"+NAN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-3",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-3",
                ["--traditional", "BEGIN{x=\"nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-4",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("NaN\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-4",
                ["--traditional", "BEGIN{x=\"NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-5",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nanx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-5",
                ["--traditional", "BEGIN{x=\"+nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-6",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nanx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-6",
                ["--traditional", "BEGIN{x=\"-nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-7",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan(payload)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-7",
                ["--traditional", "BEGIN{x=\"+nan(payload)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-8",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-8",
                ["--traditional", "BEGIN{x=\"nan(123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-9",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan()\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-9",
                ["--traditional", "BEGIN{x=\"nan()\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-10",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(_abc123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-10",
                ["--traditional", "BEGIN{x=\"nan(_abc123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-11",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(a-b)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-11",
                ["--traditional", "BEGIN{x=\"nan(a-b)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-12",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-12",
                ["--traditional", "BEGIN{x=\"nan(\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-13",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(abc)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-13",
                ["--traditional", "BEGIN{x=\"nan(abc)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-14",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nan(0x123)\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-14",
                ["--traditional", "BEGIN{x=\"-nan(0x123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-15",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-15",
                ["--traditional", "BEGIN{x=\"+nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-16",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("\f-nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-16",
                ["--traditional", "BEGIN{x=\"\\f-nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-17",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+inf\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-17",
                ["--traditional", "BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-18",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-Inf\n")
            .WithStdOut("1 0 1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-18",
                ["--traditional", "BEGIN{x=\"-Inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-19",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("inf\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-19",
                ["--traditional", "BEGIN{x=\"inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-20",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("Infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-20",
                ["--traditional", "BEGIN{x=\"Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-21",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+Infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-21",
                ["--traditional", "BEGIN{x=\"+Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-22",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infinity\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-22",
                ["--traditional", "BEGIN{x=\"-infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-23",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+infx\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-23",
                ["--traditional", "BEGIN{x=\"+infx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-24",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infxyz\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-24",
                ["--traditional", "BEGIN{x=\"-infxyz\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-25",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("InfINiTy\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-25",
                ["--traditional", "BEGIN{x=\"InfINiTy\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-26",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinit\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-26",
                ["--traditional", "BEGIN{x=\"infinit\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-traditional-27",
                ["--traditional", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinitytail\n")
            .WithStdOut("1 0 0 0 1 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-traditional-27",
                ["--traditional", "BEGIN{x=\"infinitytail\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-0",
                ["--traditional", "BEGIN{x=exp(-1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-1",
                ["--traditional", "BEGIN{x=exp(-800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-2",
                ["--traditional", "BEGIN{x=exp(-750);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -750 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-3",
                ["--traditional", "BEGIN{x=exp(-745.1332191019411);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-4",
                ["--traditional", "BEGIN{x=exp(-745.1332191019412);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -745.133 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-5",
                ["--traditional", "BEGIN{x=exp(-745);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-6",
                ["--traditional", "BEGIN{x=exp(-744);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-7",
                ["--traditional", "BEGIN{x=exp(-710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-8",
                ["--traditional", "BEGIN{x=exp(-709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-9",
                ["--traditional", "BEGIN{x=exp(-708.3964185322642);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-10",
                ["--traditional", "BEGIN{x=exp(-708.3964185322641);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-11",
                ["--traditional", "BEGIN{x=exp(-708);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-12",
                ["--traditional", "BEGIN{x=exp(-700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-13",
                ["--traditional", "BEGIN{x=exp(-1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-14",
                ["--traditional", "BEGIN{x=exp(-0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-15",
                ["--traditional", "BEGIN{x=exp(0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-16",
                ["--traditional", "BEGIN{x=exp(1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-17",
                ["--traditional", "BEGIN{x=exp(700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-18",
                ["--traditional", "BEGIN{x=exp(709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-19",
                ["--traditional", "BEGIN{x=exp(709.7827128933839);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-20",
                ["--traditional", "BEGIN{x=exp(709.782712893384);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-21",
                ["--traditional", "BEGIN{x=exp(709.7827128933841);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 709.783 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-22",
                ["--traditional", "BEGIN{x=exp(710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 710 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-23",
                ["--traditional", "BEGIN{x=exp(800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-24",
                ["--traditional", "BEGIN{x=exp(1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-25",
                ["--traditional", "BEGIN{x=exp(1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-26",
                ["--traditional", "BEGIN{x=exp(-1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-27",
                ["--traditional", "BEGIN{x=exp(\"+inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-28",
                ["--traditional", "BEGIN{x=exp(\"-inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-traditional-29",
                ["--traditional", "BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-traditional-0",
                ["--traditional", "BEGIN{s=\" 2 \";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-traditional-1",
                ["--traditional", "BEGIN{s=\"\\t2\\t\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-traditional-2",
                ["--traditional", "BEGIN{s=\"\\n2\\n\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-traditional-3",
                ["--traditional", "BEGIN{s=\"\\r2\\r\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-traditional-4",
                ["--traditional", "BEGIN{s=\"\\v2\\v\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-traditional-5",
                ["--traditional", "BEGIN{s=\"\\f2\\f\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-0-leading",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-0-trailing",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u000b|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-0-both",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2\u000b|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-1-leading",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-1-trailing",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\f|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-1-both",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2\f|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-2-leading",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-2-trailing",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\r|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-2-both",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2\r|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-3-leading",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-3-trailing",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u00a0|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-3-both",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02\u00a0|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-4-leading",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-4-trailing",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u2003|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-4-both",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032\u2003|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-5-leading",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-5-trailing",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u0085|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-traditional-5-both",
                ["--traditional", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852\u0085|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-discard-traditional",
                ["--traditional", "BEGIN{sqrt(-1);log(-1);exp(1000);exp(-1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: warning: log: received negative argument -1\nawk: warning: exp: argument 1000 is out of range\nawk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-configuration-traditional",
                ["--traditional", "BEGIN{CONVFMT=\"%.1f\";OFMT=\"%.1f\";sqrt(-123456789);log(-0.00000123);exp(1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1.23457e+08\nawk: warning: log: received negative argument -1.23e-06\nawk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-before-fatal-traditional",
                ["--traditional", "BEGIN{print \"before\";x=sqrt(-1);z=0;y=1/z;print \"bad\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: division by zero attempted\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-0",
                ["--posix", "BEGIN{x=(-1)^0.5;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-1",
                ["--posix", "BEGIN{x=sqrt(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-2",
                ["--posix", "BEGIN{x=log(-1);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-3",
                ["--posix", "BEGIN{x=sqrt(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-4",
                ["--posix", "BEGIN{x=log(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .WithStdErr("awk: warning: log: received negative argument -inf\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-5",
                ["--posix", "BEGIN{x=sin(\"+inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-6",
                ["--posix", "BEGIN{x=cos(\"-inf\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-7",
                ["--posix", "BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-8",
                ["--posix", "BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-9",
                ["--posix", "BEGIN{x=0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-10",
                ["--posix", "BEGIN{x=-0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-11",
                ["--posix", "BEGIN{x=4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-12",
                ["--posix", "BEGIN{x=-4.9406564584124654e-324;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-13",
                ["--posix", "BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-14",
                ["--posix", "BEGIN{x=\"-inf\"+0;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-15",
                ["--posix", "BEGIN{x=1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 0 1 1 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-16",
                ["--posix", "BEGIN{x=-1;print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-17",
                ["--posix", "BEGIN{x=sqrt(-0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 0 1 0 1 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-18",
                ["--posix", "BEGIN{x=log(0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("1 0 1 1 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-19",
                ["--posix", "BEGIN{x=sqrt(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-20",
                ["--posix", "BEGIN{x=log(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-21",
                ["--posix", "BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-22",
                ["--posix", "BEGIN{x=sin(\"+nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-math-posix-23",
                ["--posix", "BEGIN{x=cos(\"-nan\"+0);print (x==x),(x!=x),(x<0),(x<=0),(x>0),(x>=0),!x,!!x,(x&&1),(x||0),(x?1:0)}"],
                [])
            .WithStdOut("0 1 0 0 0 0 0 1 1 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-0",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-0",
                ["--posix", "BEGIN{x=\"+nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-1",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-NaN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-1",
                ["--posix", "BEGIN{x=\"-NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-2",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+NAN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-2",
                ["--posix", "BEGIN{x=\"+NAN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-3",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-3",
                ["--posix", "BEGIN{x=\"nan\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-4",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("NaN\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-4",
                ["--posix", "BEGIN{x=\"NaN\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-5",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nanx\n")
            .WithStdOut("0 1 0 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-5",
                ["--posix", "BEGIN{x=\"+nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-6",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nanx\n")
            .WithStdOut("0 1 0 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-6",
                ["--posix", "BEGIN{x=\"-nanx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-7",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan(payload)\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-7",
                ["--posix", "BEGIN{x=\"+nan(payload)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-8",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(123)\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-8",
                ["--posix", "BEGIN{x=\"nan(123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-9",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan()\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-9",
                ["--posix", "BEGIN{x=\"nan()\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-10",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(_abc123)\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-10",
                ["--posix", "BEGIN{x=\"nan(_abc123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-11",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(a-b)\n")
            .WithStdOut("0 1 0 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-11",
                ["--posix", "BEGIN{x=\"nan(a-b)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-12",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(\n")
            .WithStdOut("0 1 0 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-12",
                ["--posix", "BEGIN{x=\"nan(\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-13",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("nan(abc)\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-13",
                ["--posix", "BEGIN{x=\"nan(abc)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-14",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-nan(0x123)\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-14",
                ["--posix", "BEGIN{x=\"-nan(0x123)\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-15",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-15",
                ["--posix", "BEGIN{x=\"+nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-16",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("\f-nan\u000b\n")
            .WithStdOut("0 1 0 0 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-16",
                ["--posix", "BEGIN{x=\"\\f-nan\\v\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-17",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+inf\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-17",
                ["--posix", "BEGIN{x=\"+inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-18",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-Inf\n")
            .WithStdOut("1 0 1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-18",
                ["--posix", "BEGIN{x=\"-Inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-19",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("inf\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-19",
                ["--posix", "BEGIN{x=\"inf\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-20",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("Infinity\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-20",
                ["--posix", "BEGIN{x=\"Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-21",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+Infinity\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-21",
                ["--posix", "BEGIN{x=\"+Infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-22",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infinity\n")
            .WithStdOut("1 0 1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-22",
                ["--posix", "BEGIN{x=\"-infinity\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-23",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("+infx\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-23",
                ["--posix", "BEGIN{x=\"+infx\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-24",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("-infxyz\n")
            .WithStdOut("1 0 1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-24",
                ["--posix", "BEGIN{x=\"-infxyz\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-25",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("InfINiTy\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-25",
                ["--posix", "BEGIN{x=\"InfINiTy\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-26",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinit\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-26",
                ["--posix", "BEGIN{x=\"infinit\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-special-posix-27",
                ["--posix", "{x=$0+0;print (x==x),(x!=x),(x<0),(x>0),!x,($0==$0),($0!=$0),!$0}"],
                [])
            .WithStandardInput("infinitytail\n")
            .WithStdOut("1 0 0 1 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-literal-special-posix-27",
                ["--posix", "BEGIN{x=\"infinitytail\"+0;print (x==x),(x!=x),(x<0),(x>0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-0",
                ["--posix", "BEGIN{x=exp(-1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-1",
                ["--posix", "BEGIN{x=exp(-800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-2",
                ["--posix", "BEGIN{x=exp(-750);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -750 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-3",
                ["--posix", "BEGIN{x=exp(-745.1332191019411);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-4",
                ["--posix", "BEGIN{x=exp(-745.1332191019412);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -745.133 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-5",
                ["--posix", "BEGIN{x=exp(-745);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-6",
                ["--posix", "BEGIN{x=exp(-744);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-7",
                ["--posix", "BEGIN{x=exp(-710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-8",
                ["--posix", "BEGIN{x=exp(-709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-9",
                ["--posix", "BEGIN{x=exp(-708.3964185322642);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-10",
                ["--posix", "BEGIN{x=exp(-708.3964185322641);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-11",
                ["--posix", "BEGIN{x=exp(-708);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-12",
                ["--posix", "BEGIN{x=exp(-700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-13",
                ["--posix", "BEGIN{x=exp(-1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-14",
                ["--posix", "BEGIN{x=exp(-0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-15",
                ["--posix", "BEGIN{x=exp(0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-16",
                ["--posix", "BEGIN{x=exp(1);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-17",
                ["--posix", "BEGIN{x=exp(700);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-18",
                ["--posix", "BEGIN{x=exp(709);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-19",
                ["--posix", "BEGIN{x=exp(709.7827128933839);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-20",
                ["--posix", "BEGIN{x=exp(709.782712893384);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-21",
                ["--posix", "BEGIN{x=exp(709.7827128933841);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 709.783 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-22",
                ["--posix", "BEGIN{x=exp(710);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 710 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-23",
                ["--posix", "BEGIN{x=exp(800);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 800 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-24",
                ["--posix", "BEGIN{x=exp(1000);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-25",
                ["--posix", "BEGIN{x=exp(1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .WithStdErr("awk: warning: exp: argument 1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-26",
                ["--posix", "BEGIN{x=exp(-1e308);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .WithStdErr("awk: warning: exp: argument -1e+308 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-27",
                ["--posix", "BEGIN{x=exp(\"+inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 0 1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-28",
                ["--posix", "BEGIN{x=exp(\"-inf\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("1 0 1 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-exp-posix-29",
                ["--posix", "BEGIN{x=exp(\"+nan\"+0);print (x==x),(x!=x),(x==0),(x>0),(x<0),!x}"],
                [])
            .WithStdOut("0 1 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-posix-0",
                ["--posix", "BEGIN{s=\" 2 \";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-posix-1",
                ["--posix", "BEGIN{s=\"\\t2\\t\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-posix-2",
                ["--posix", "BEGIN{s=\"\\n2\\n\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-posix-3",
                ["--posix", "BEGIN{s=\"\\r2\\r\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-posix-4",
                ["--posix", "BEGIN{s=\"\\v2\\v\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-whitespace-posix-5",
                ["--posix", "BEGIN{s=\"\\f2\\f\";print s+0,!(s+0),(s==2)}"],
                [])
            .WithStdOut("2 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-0-leading",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-0-trailing",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u000b|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-0-both",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u000b2\u000b|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-1-leading",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-1-trailing",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\f|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-1-both",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\f2\f|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-2-leading",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-2-trailing",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\r|")
            .WithStdOut("1 2 1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-2-both",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\r2\r|")
            .WithStdOut("1 2 1 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-3-leading",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-3-trailing",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u00a0|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-3-both",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00a02\u00a0|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-4-leading",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-4-trailing",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u2003|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-4-both",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u20032\u2003|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-5-leading",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852|")
            .WithStdOut("1 0 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-5-trailing",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("2\u0085|")
            .WithStdOut("1 2 0 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-space-posix-5-both",
                ["--posix", "BEGIN{RS=\"|\"}{print NF,$1+0,($1==2),!$1,length($1)}"],
                [])
            .WithStandardInput("\u00852\u0085|")
            .WithStdOut("1 0 0 0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-discard-posix",
                ["--posix", "BEGIN{sqrt(-1);log(-1);exp(1000);exp(-1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: warning: log: received negative argument -1\nawk: warning: exp: argument 1000 is out of range\nawk: warning: exp: argument -1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-configuration-posix",
                ["--posix", "BEGIN{CONVFMT=\"%.1f\";OFMT=\"%.1f\";sqrt(-123456789);log(-0.00000123);exp(1000);print \"after\"}"],
                [])
            .WithStdOut("after\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1.23457e+08\nawk: warning: log: received negative argument -1.23e-06\nawk: warning: exp: argument 1000 is out of range\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-warning-before-fatal-posix",
                ["--posix", "BEGIN{print \"before\";x=sqrt(-1);z=0;y=1/z;print \"bad\"}"],
                [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: warning: sqrt: received negative argument -1\nawk: division by zero attempted\n")
            .Build();

    }
}
