namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UnaryPrefixCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-original",
                ["BEGIN{x=2;print !!x}"],
                [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-logical",
                ["BEGIN{x=2;print !!x,! !x,!!!x,!!!!x,!!0,!!!0}"],
                [])
            .WithStdOut("1 1 0 1 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-signs",
                ["BEGIN{x=2;print - -x,+ +x,- +x,+ -x,+ + - -x,!-x,-!x,!+x,+!x}"],
                [])
            .WithStdOut("2 2 -2 -2 2 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-powers",
                ["BEGIN{x=2;print - -x^2,- -x^2^2,2^- -2,2^! -0,- -2^3,(- -2)^3}"],
                [])
            .WithStdOut("4 16 4 2 8 8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-mutation-powers",
                ["BEGIN{x=2;print ++x^2,x;print -++x^2,x;print !--x^2,x}"],
                [])
            .WithStdOut("9 3\n-16 4\n0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-mutation-outer",
                ["BEGIN{x=2;print !++x,x;print -++x,x;print +--x,x;print !!++x,x}"],
                [])
            .WithStdOut("0 3\n-4 4\n3 3\n1 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-array-target",
                ["BEGIN{i=1;a[1]=2;print !!a[i++],i,a[1];i=1;print !++a[i++],i,a[1]}"],
                [])
            .WithStdOut("1 2 2\n0 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-array-target-powers",
                ["BEGIN{i=1;a[1]=2;print -++a[i++]^2,i,a[1]}"],
                [])
            .WithStdOut("-9 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-function-target",
                ["function p(){calls++;return 1}BEGIN{a[1]=2;print !++a[p()],calls,a[1];print -++a[p()]^2,calls,a[1]}"],
                [])
            .WithStdOut("0 1 3\n-16 2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-target",
                ["BEGIN{$0=\"2 3\";i=1;print !!$i,!++$i,$0;print -++$i^2,$0}"],
                [])
            .WithStdOut("1 0 3 3\n-16 4 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-index-mutation",
                ["BEGIN{$0=\"2 3\";i=0;print $++i,i,$0;print $--i,i,$0}"],
                [])
            .WithStdOut("2 1 2 3\n2 3 0 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-index-sign",
                ["BEGIN{$0=\"2 3\";i=-1;print $-i,$+1,$!0}"],
                [])
            .WithStdOut("2 2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-index-sign-powers",
                ["BEGIN{$0=\"2 3\";i=-1;print $-i^2,$- -1,$!0^2}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: negative field index\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-index-sign-valid-powers",
                ["BEGIN{$0=\"2 3\";i=1;print $- -i^2,($- -i)^2,$!0^2,($!0)^2}"],
                [])
            .WithStdOut("2 4 2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-index-sign-mutation",
                ["BEGIN{$0=\"2 3\";i=-1;print $-i++,i,$0;i=0;print $!++i^2,i,$0}"],
                [])
            .WithStdOut("2 0 2 3\n2 3 1 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-index-nested",
                ["BEGIN{$0=\"1 3\";i=1;print !!$$i,!!$++i,i,$0}"],
                [])
            .WithStdOut("1 1 2 1 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-field-postfix-binding",
                ["BEGIN{$0=\"2 3\";i=1;print !!$i++,i,$0;print !!$(i++),i,$0}"],
                [])
            .WithStdOut("1 1 3 3\n1 2 3 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-zero-sign",
                ["BEGIN{printf \"[%a][%a][%a][%a]\\n\",- -0,+ -0,- +0,- - -0}"],
                [])
            .WithStdOut("[0x0p+0][-0x0p+0][-0x0p+0][-0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-string-types",
                ["BEGIN{print !!\"0\",!!\"\",!+\"0\",!-\"0\",!!+\"2tail\",!!\"text\",- -\"2tail\"}"],
                [])
            .WithStdOut("1 0 1 1 1 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-numeric-fields",
                ["{print !!$1,!+$1,!!+$1,- -$2,!!$3}"],
                [])
            .WithStandardInput("0 2tail text\n")
            .WithStdOut("0 1 0 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-assignment-operand",
                ["BEGIN{x=0;print (0||x=!!2),x;(1?x=- -3:x=0);print x}"],
                [])
            .WithStdOut("1 1\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-concatenation",
                ["BEGIN{x=2;print !!x \"z\",- -x \"z\",!++x \"z\",x}"],
                [])
            .WithStdOut("1z 2z 0z 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-call-arguments",
                ["function f(x,y){return x+y}BEGIN{print f(!!2,- -3)}"],
                [])
            .WithStdOut("4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-subscript",
                ["BEGIN{a[1]=7;a[2]=9;print a[!!2],a[- -2]}"],
                [])
            .WithStdOut("7 9\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-control-flow",
                ["BEGIN{x=0;if(!!1)print \"if\";while(!!(x<2))x++;print x;for(i=0;!!(i<2);i++)print i}"],
                [])
            .WithStdOut("if\n2\n0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-deep",
                ["BEGIN{x=2;print !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!x,- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - x}"],
                [])
            .WithStdOut("1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-infinity-control",
                ["BEGIN{print !!(\"+inf\"+0),!- (\"+inf\"+0),! -(\"-inf\"+0)}"],
                [])
            .WithStdOut("1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-regex-control",
                ["{print !!/a/,!- /a/}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("1 0\n0 1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-0",
                ["BEGIN{x=2;a[1]=2;r=++-x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-1",
                ["BEGIN{x=2;a[1]=2;r=--+x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-2",
                ["BEGIN{x=2;a[1]=2;r=++!x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-3",
                ["BEGIN{x=2;a[1]=2;r=++ ++x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-4",
                ["BEGIN{x=2;a[1]=2;r=-- --x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-5",
                ["BEGIN{x=2;a[1]=2;r=!++(x+1);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-6",
                ["BEGIN{x=2;a[1]=2;r=++(x);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-7",
                ["BEGIN{x=2;a[1]=2;r=++(a[1]);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-8",
                ["BEGIN{x=2;a[1]=2;r=!++2;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-9",
                ["BEGIN{x=2;a[1]=2;r=!++sin(x);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-gnu-invalid-10",
                ["BEGIN{x=2;a[1]=2;r=!++x++;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("unary-gnu-power-alias",
                ["BEGIN{x=2;print - -x**2,2**- -2;print -++x**2,x}"],
                [])
            .WithStdOut("4 4\n-9 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-original",
                ["--traditional", "BEGIN{x=2;print !!x}"],
                [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-logical",
                ["--traditional", "BEGIN{x=2;print !!x,! !x,!!!x,!!!!x,!!0,!!!0}"],
                [])
            .WithStdOut("1 1 0 1 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-signs",
                ["--traditional", "BEGIN{x=2;print - -x,+ +x,- +x,+ -x,+ + - -x,!-x,-!x,!+x,+!x}"],
                [])
            .WithStdOut("2 2 -2 -2 2 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-powers",
                ["--traditional", "BEGIN{x=2;print - -x^2,- -x^2^2,2^- -2,2^! -0,- -2^3,(- -2)^3}"],
                [])
            .WithStdOut("4 16 4 2 8 8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-mutation-powers",
                ["--traditional", "BEGIN{x=2;print ++x^2,x;print -++x^2,x;print !--x^2,x}"],
                [])
            .WithStdOut("9 3\n-16 4\n0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-mutation-outer",
                ["--traditional", "BEGIN{x=2;print !++x,x;print -++x,x;print +--x,x;print !!++x,x}"],
                [])
            .WithStdOut("0 3\n-4 4\n3 3\n1 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-array-target",
                ["--traditional", "BEGIN{i=1;a[1]=2;print !!a[i++],i,a[1];i=1;print !++a[i++],i,a[1]}"],
                [])
            .WithStdOut("1 2 2\n0 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-array-target-powers",
                ["--traditional", "BEGIN{i=1;a[1]=2;print -++a[i++]^2,i,a[1]}"],
                [])
            .WithStdOut("-9 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-function-target",
                ["--traditional", "function p(){calls++;return 1}BEGIN{a[1]=2;print !++a[p()],calls,a[1];print -++a[p()]^2,calls,a[1]}"],
                [])
            .WithStdOut("0 1 3\n-16 2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-target",
                ["--traditional", "BEGIN{$0=\"2 3\";i=1;print !!$i,!++$i,$0;print -++$i^2,$0}"],
                [])
            .WithStdOut("1 0 3 3\n-16 4 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-index-mutation",
                ["--traditional", "BEGIN{$0=\"2 3\";i=0;print $++i,i,$0;print $--i,i,$0}"],
                [])
            .WithStdOut("2 1 2 3\n2 3 0 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-index-sign",
                ["--traditional", "BEGIN{$0=\"2 3\";i=-1;print $-i,$+1,$!0}"],
                [])
            .WithStdOut("2 2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-index-sign-powers",
                ["--traditional", "BEGIN{$0=\"2 3\";i=-1;print $-i^2,$- -1,$!0^2}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: negative field index\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-index-sign-valid-powers",
                ["--traditional", "BEGIN{$0=\"2 3\";i=1;print $- -i^2,($- -i)^2,$!0^2,($!0)^2}"],
                [])
            .WithStdOut("2 4 2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-index-sign-mutation",
                ["--traditional", "BEGIN{$0=\"2 3\";i=-1;print $-i++,i,$0;i=0;print $!++i^2,i,$0}"],
                [])
            .WithStdOut("2 0 2 3\n2 3 1 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-index-nested",
                ["--traditional", "BEGIN{$0=\"1 3\";i=1;print !!$$i,!!$++i,i,$0}"],
                [])
            .WithStdOut("1 1 2 1 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-field-postfix-binding",
                ["--traditional", "BEGIN{$0=\"2 3\";i=1;print !!$i++,i,$0;print !!$(i++),i,$0}"],
                [])
            .WithStdOut("1 1 3 3\n1 2 3 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-zero-sign",
                ["--traditional", "BEGIN{printf \"[%a][%a][%a][%a]\\n\",- -0,+ -0,- +0,- - -0}"],
                [])
            .WithStdOut("[0x0p+0][-0x0p+0][-0x0p+0][-0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-string-types",
                ["--traditional", "BEGIN{print !!\"0\",!!\"\",!+\"0\",!-\"0\",!!+\"2tail\",!!\"text\",- -\"2tail\"}"],
                [])
            .WithStdOut("1 0 1 1 1 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-numeric-fields",
                ["--traditional", "{print !!$1,!+$1,!!+$1,- -$2,!!$3}"],
                [])
            .WithStandardInput("0 2tail text\n")
            .WithStdOut("0 1 0 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-assignment-operand",
                ["--traditional", "BEGIN{x=0;print (0||x=!!2),x;(1?x=- -3:x=0);print x}"],
                [])
            .WithStdOut("1 1\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-concatenation",
                ["--traditional", "BEGIN{x=2;print !!x \"z\",- -x \"z\",!++x \"z\",x}"],
                [])
            .WithStdOut("1z 2z 0z 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-call-arguments",
                ["--traditional", "function f(x,y){return x+y}BEGIN{print f(!!2,- -3)}"],
                [])
            .WithStdOut("4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-subscript",
                ["--traditional", "BEGIN{a[1]=7;a[2]=9;print a[!!2],a[- -2]}"],
                [])
            .WithStdOut("7 9\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-control-flow",
                ["--traditional", "BEGIN{x=0;if(!!1)print \"if\";while(!!(x<2))x++;print x;for(i=0;!!(i<2);i++)print i}"],
                [])
            .WithStdOut("if\n2\n0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-deep",
                ["--traditional", "BEGIN{x=2;print !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!x,- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - x}"],
                [])
            .WithStdOut("1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-infinity-control",
                ["--traditional", "BEGIN{print !!(\"+inf\"+0),!- (\"+inf\"+0),! -(\"-inf\"+0)}"],
                [])
            .WithStdOut("1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-regex-control",
                ["--traditional", "{print !!/a/,!- /a/}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("1 0\n0 1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-0",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=++-x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-1",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=--+x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-2",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=++!x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-3",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=++ ++x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-4",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=-- --x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-5",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=!++(x+1);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-6",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=++(x);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-7",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=++(a[1]);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-8",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=!++2;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-9",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=!++sin(x);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-traditional-invalid-10",
                ["--traditional", "BEGIN{x=2;a[1]=2;r=!++x++;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("unary-traditional-power-alias",
                ["--traditional", "BEGIN{x=2;print - -x**2,2**- -2;print -++x**2,x}"],
                [])
            .WithStdOut("4 4\n-9 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-original",
                ["--posix", "BEGIN{x=2;print !!x}"],
                [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-logical",
                ["--posix", "BEGIN{x=2;print !!x,! !x,!!!x,!!!!x,!!0,!!!0}"],
                [])
            .WithStdOut("1 1 0 1 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-signs",
                ["--posix", "BEGIN{x=2;print - -x,+ +x,- +x,+ -x,+ + - -x,!-x,-!x,!+x,+!x}"],
                [])
            .WithStdOut("2 2 -2 -2 2 0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-powers",
                ["--posix", "BEGIN{x=2;print - -x^2,- -x^2^2,2^- -2,2^! -0,- -2^3,(- -2)^3}"],
                [])
            .WithStdOut("4 16 4 2 8 8\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-mutation-powers",
                ["--posix", "BEGIN{x=2;print ++x^2,x;print -++x^2,x;print !--x^2,x}"],
                [])
            .WithStdOut("9 3\n-16 4\n0 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-mutation-outer",
                ["--posix", "BEGIN{x=2;print !++x,x;print -++x,x;print +--x,x;print !!++x,x}"],
                [])
            .WithStdOut("0 3\n-4 4\n3 3\n1 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-array-target",
                ["--posix", "BEGIN{i=1;a[1]=2;print !!a[i++],i,a[1];i=1;print !++a[i++],i,a[1]}"],
                [])
            .WithStdOut("1 2 2\n0 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-array-target-powers",
                ["--posix", "BEGIN{i=1;a[1]=2;print -++a[i++]^2,i,a[1]}"],
                [])
            .WithStdOut("-9 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-function-target",
                ["--posix", "function p(){calls++;return 1}BEGIN{a[1]=2;print !++a[p()],calls,a[1];print -++a[p()]^2,calls,a[1]}"],
                [])
            .WithStdOut("0 1 3\n-16 2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-target",
                ["--posix", "BEGIN{$0=\"2 3\";i=1;print !!$i,!++$i,$0;print -++$i^2,$0}"],
                [])
            .WithStdOut("1 0 3 3\n-16 4 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-index-mutation",
                ["--posix", "BEGIN{$0=\"2 3\";i=0;print $++i,i,$0;print $--i,i,$0}"],
                [])
            .WithStdOut("2 1 2 3\n2 3 0 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-index-sign",
                ["--posix", "BEGIN{$0=\"2 3\";i=-1;print $-i,$+1,$!0}"],
                [])
            .WithStdOut("2 2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-index-sign-powers",
                ["--posix", "BEGIN{$0=\"2 3\";i=-1;print $-i^2,$- -1,$!0^2}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: negative field index\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-index-sign-valid-powers",
                ["--posix", "BEGIN{$0=\"2 3\";i=1;print $- -i^2,($- -i)^2,$!0^2,($!0)^2}"],
                [])
            .WithStdOut("2 4 2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-index-sign-mutation",
                ["--posix", "BEGIN{$0=\"2 3\";i=-1;print $-i++,i,$0;i=0;print $!++i^2,i,$0}"],
                [])
            .WithStdOut("2 0 2 3\n2 3 1 2 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-index-nested",
                ["--posix", "BEGIN{$0=\"1 3\";i=1;print !!$$i,!!$++i,i,$0}"],
                [])
            .WithStdOut("1 1 2 1 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-field-postfix-binding",
                ["--posix", "BEGIN{$0=\"2 3\";i=1;print !!$i++,i,$0;print !!$(i++),i,$0}"],
                [])
            .WithStdOut("1 1 3 3\n1 2 3 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-zero-sign",
                ["--posix", "BEGIN{printf \"[%a][%a][%a][%a]\\n\",- -0,+ -0,- +0,- - -0}"],
                [])
            .WithStdOut("[0x0p+0][-0x0p+0][-0x0p+0][-0x0p+0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-string-types",
                ["--posix", "BEGIN{print !!\"0\",!!\"\",!+\"0\",!-\"0\",!!+\"2tail\",!!\"text\",- -\"2tail\"}"],
                [])
            .WithStdOut("1 0 1 1 1 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-numeric-fields",
                ["--posix", "{print !!$1,!+$1,!!+$1,- -$2,!!$3}"],
                [])
            .WithStandardInput("0 2tail text\n")
            .WithStdOut("0 1 0 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-assignment-operand",
                ["--posix", "BEGIN{x=0;print (0||x=!!2),x;(1?x=- -3:x=0);print x}"],
                [])
            .WithStdOut("1 1\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-concatenation",
                ["--posix", "BEGIN{x=2;print !!x \"z\",- -x \"z\",!++x \"z\",x}"],
                [])
            .WithStdOut("1z 2z 0z 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-call-arguments",
                ["--posix", "function f(x,y){return x+y}BEGIN{print f(!!2,- -3)}"],
                [])
            .WithStdOut("4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-subscript",
                ["--posix", "BEGIN{a[1]=7;a[2]=9;print a[!!2],a[- -2]}"],
                [])
            .WithStdOut("7 9\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-control-flow",
                ["--posix", "BEGIN{x=0;if(!!1)print \"if\";while(!!(x<2))x++;print x;for(i=0;!!(i<2);i++)print i}"],
                [])
            .WithStdOut("if\n2\n0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-deep",
                ["--posix", "BEGIN{x=2;print !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!x,- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - x}"],
                [])
            .WithStdOut("1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-infinity-control",
                ["--posix", "BEGIN{print !!(\"+inf\"+0),!- (\"+inf\"+0),! -(\"-inf\"+0)}"],
                [])
            .WithStdOut("1 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("unary-posix-regex-control",
                ["--posix", "{print !!/a/,!- /a/}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("1 0\n0 1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-0",
                ["--posix", "BEGIN{x=2;a[1]=2;r=++-x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-1",
                ["--posix", "BEGIN{x=2;a[1]=2;r=--+x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-2",
                ["--posix", "BEGIN{x=2;a[1]=2;r=++!x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-3",
                ["--posix", "BEGIN{x=2;a[1]=2;r=++ ++x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-4",
                ["--posix", "BEGIN{x=2;a[1]=2;r=-- --x;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-5",
                ["--posix", "BEGIN{x=2;a[1]=2;r=!++(x+1);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-6",
                ["--posix", "BEGIN{x=2;a[1]=2;r=++(x);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-7",
                ["--posix", "BEGIN{x=2;a[1]=2;r=++(a[1]);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-8",
                ["--posix", "BEGIN{x=2;a[1]=2;r=!++2;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-9",
                ["--posix", "BEGIN{x=2;a[1]=2;r=!++sin(x);print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("unary-posix-invalid-10",
                ["--posix", "BEGIN{x=2;a[1]=2;r=!++x++;print r}"],
                [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid increment target.\n")
            .Build());

    }
}
