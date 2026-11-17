namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ForSimpleStatementCases()
    {
        yield return Passing("for-empty-whitespace-clauses",
            ["BEGIN{for( \t ; \t ; \t ){print \"once\";break}print \"done\"}"], [], "once\ndone\n");
        yield return CompilerDiagnostic(Failing("for-invalid-initializer-if",
            ["BEGIN{print \"unreached\";for(if(1) x=1;;)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-initializer-while",
            ["BEGIN{print \"unreached\";for(while(1) break;;)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-initializer-block",
            ["BEGIN{print \"unreached\";for({x=1};;)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-condition-if",
            ["BEGIN{print \"unreached\";for(;if(1) x=1;)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-condition-while",
            ["BEGIN{print \"unreached\";for(;while(1) break;)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-condition-block",
            ["BEGIN{print \"unreached\";for(;{x=1};)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-step-if",
            ["BEGIN{print \"unreached\";for(;;if(1) x=1)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-step-while",
            ["BEGIN{print \"unreached\";for(;;while(1) break)break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-invalid-step-block",
            ["BEGIN{print \"unreached\";for(;;{x=1})break;print \"after\"}"], [], 2, "", "awk: invalid `for' statement.\n"));
        yield return CompilerDiagnostic(Failing("for-simple-validates-print-calls-before-effects",
            ["BEGIN{print \"before\";for(print substr(\"a\");0;);}"], [], 2, "",
            "awk: invalid number of arguments for substr\n"));
        yield return CompilerDiagnostic(Failing("for-simple-validates-delete-target-before-effects",
            ["BEGIN{print \"before\";for(;0;delete 1);}"], [], 2, "",
            "awk: invalid delete target.\n"));
        yield return Passing("for-simple-print-initializer",
            ["BEGIN{for(print 9;0;);print \"done\"}"], [], "9\ndone\n");
        yield return Passing("for-simple-print-step-continue",
            ["BEGIN{i=0;for(print \"init\";i<3 && turns++<4;print ++i){if(i==1)continue;print \"body\",i}}"], [], "init\nbody 0\n1\n2\nbody 2\n3\n");
        yield return Passing("for-simple-printf-initializer-step",
            ["BEGIN{for(printf \"%s\",\"init\";i<2 && turns++<3;printf \"%d\",++i)printf \"B\";print \"\"}"], [], "initB1B2\n");
        yield return Passing("for-simple-delete-initializer-step",
            ["BEGIN{a[1]=1;for(delete a[1];i++<2;delete a[2]){a[2]=2;print(1 in a),(2 in a)}print length(a)}"], [], "0 1\n0 1\n0\n");
        yield return Passing("for-simple-plain-record-print",
            ["BEGIN{$0=\"record\";for(print;0;);}"], [], "record\n");
        yield return Passing("for-simple-parameter-spelling",
            ["function f(x){for(print x;0;);return x}BEGIN{print length(f(\"0007\"))}"], [], "0007\n4\n");
        yield return Passing("for-simple-header-observations",
            ["{for(print NR,FNR,RT;0;);}"], [], "1 1 \n\n", "a\n");
        yield return PassingWithExpectedFiles("for-simple-redirected-output",
            ["BEGIN{for(print \"ready\">\"/out\";0;);close(\"/out\");print \"done\"}"], [], "done\n",
            [File("/out", "ready\n")]);
        yield return Passing("for-simple-newlines-comments",
            ["BEGIN{for(print \"init\"; # condition\ni<1 && turns++<2; # step\nprintf \"%d\\n\",++i)print \"body\"}"], [], "init\nbody\n1\n");
    }
}
