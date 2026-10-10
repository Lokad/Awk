namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FunctionFlowCompatibilityRepairs()
    {
        yield return AwkGoldenCaseBuilder
            .Create("function-next-original-repro",
                ["function f(){next}{f();print \"bad\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-original-repro",
                ["function f(){nextfile}{print $0;f()}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("a\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-unused-input-control-is-valid",
                ["function f(){next} function g(){nextfile} BEGIN{print \"ok\"} END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("ok\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-unwinds-nested-print-and-skips-later-rules",
                ["function skip(){next} function outer(){skip();print \"bad\"}{x=9;print \"before\",NR;print \"discarded\",outer(),(++x);print \"bad\"}{print \"other\"}END{print \"end\",NR,x}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("before 1\nbefore 2\nend 2 9\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-unwinds-argument-evaluation",
                ["function skip(){next} function consume(x,y){calls++;return x+y}{x=1;consume(skip(),++x);print \"bad\"}END{print x,calls+0,NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("1 0 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-releases-local-array-between-records",
                ["function f(a){a[1]=NR;if(NR<3)next;return a[1]}{print f();print \"after\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\nc\n")
            .WithStdOut("3\nafter\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-unwinds-loop-body",
                ["function f(){for(i=0;i<5;i++){if(i==1)next;print i}print \"bad\"}{f();print \"bad\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("0\n0\n2\n")
            .Build();

        // GNU 5.2.1 and 5.4.0 fail internally when next unwinds this mutable argument.
        // Preserve the intended record flow; this is an explicit reference divergence.
        yield return AwkGoldenCaseBuilder
            .Create("function-next-unwinds-array-reference-and-substitution",
                ["function skip(){next}{sub(/a/,\"X\",a[skip()]);print \"bad\"}END{print NR,$0}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("2 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-unwinds-redirection-target",
                ["function path(){next}{print \"discarded\" > path();print \"bad\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-rule-pattern-executes-and-returns-value",
                ["function f(){return NR==1}f(){print $0}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("a\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-in-rule-pattern-skips-all-actions",
                ["function f(){print \"pattern\",NR;next}f(){print \"bad\"}{print \"other\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("pattern 1\npattern 2\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-in-range-end-retains-range-state",
                ["function start(){print \"start\",NR;return NR==1}function stop(){if(NR==3)next;return NR==4}start(),stop(){print \"range\",NR}{print \"tail\",NR}END{print NR}"],
                [])
            .WithStandardInput("1\n2\n3\n4\n5\n")
            .WithStdOut("start 1\nrange 1\ntail 1\nrange 2\ntail 2\nrange 4\ntail 4\nstart 5\ntail 5\n5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-cleans-each-file-and-runs-endfile",
                ["function f(){nextfile}BEGINFILE{print \"B\",ARGIND}{print $0;f();print \"bad\"}ENDFILE{print \"E\",ARGIND,FNR}END{print \"D\",NR}", "/a", "/b"],
                [File("/a", "a1\na2\n"), File("/b", "b1\nb2\n")])
            .WithStdOut("B 1\na1\nE 1 1\nB 2\nb1\nE 2 1\nD 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-from-beginfile-runs-endfile",
                ["function f(){nextfile}BEGINFILE{print \"B\",ARGIND;if(ARGIND==1)f()}{print $0}ENDFILE{print \"E\",ARGIND,FNR}END{print \"D\",NR}", "/a", "/b"],
                [File("/a", "a1\na2\n"), File("/b", "b1\nb2\n")])
            .WithStdOut("B 1\nE 1 0\nB 2\nb1\nb2\nE 2 2\nD 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-from-pattern-skips-file",
                ["function f(){print \"pattern\",ARGIND;nextfile}f(){print \"bad\"}{print \"other\"}ENDFILE{print \"E\",ARGIND,FNR}END{print NR}", "/a", "/b"],
                [File("/a", "a1\na2\n"), File("/b", "b1\nb2\n")])
            .WithStdOut("pattern 1\nE 1 1\npattern 2\nE 2 1\n2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-from-open-failure-beginfile-skips-failed-file",
                ["function f(){nextfile}BEGINFILE{print \"B\",ARGIND;if(ERRNO!=\"\")f()}{print $0}ENDFILE{print \"E\",ARGIND,FNR}END{print \"D\",NR}", "/missing", "/b"],
                [File("/b", "b1\nb2\n")])
            .WithStdOut("B 1\nB 2\nb1\nb2\nE 2 2\nD 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-after-getline-advances-record-counts",
                ["function f(){getline extra;print extra;next}{f();print \"bad\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\nc\nd\n")
            .WithStdOut("b\nd\n4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-after-redirected-getline",
                ["function f(){getline extra < \"/side\";print extra;nextfile}{f();print \"bad\"}END{print NR;print close(\"/side\")}"],
                [File("/side", "side\n")])
            .WithStandardInput("a\nb\n")
            .WithStdOut("side\n1\n0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-exit-unwinds-remaining-expression",
                ["function f(){exit 7}{x=9;print \"before\";x=1+f();print \"bad\"}END{print \"end\",NR,x}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(7)
            .WithStdOut("before\nend 1 9\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-exit-in-begin-runs-end",
                ["function f(){exit 7}BEGIN{print \"before\";f();print \"bad\"}END{print \"end\",NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(7)
            .WithStdOut("before\nend 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-exit-in-end-skips-later-end-actions",
                ["function f(){exit 4}END{print \"before\";f();print \"bad\"}END{print \"later\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(4)
            .WithStdOut("before\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-exit-nested-end-status-overrides-main-status",
                ["function stop(n){exit n}function f(n){stop(n);print \"bad\"}{stop(7);print \"bad\"}END{print \"end\";f(9);print \"bad\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(9)
            .WithStdOut("end\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-rejects-begin",
                ["function f(){next} function nested(){f()} BEGIN{print \"before\";nested();print \"bad\"} END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: next used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-rejects-end",
                ["function f(){next} function nested(){f()} END{print \"before\";nested();print \"bad\"} END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: next used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-rejects-beginfile",
                ["function f(){next} function nested(){f()} BEGINFILE{print \"before\";nested();print \"bad\"} END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: next used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-rejects-endfile",
                ["function f(){next} function nested(){f()} ENDFILE{print \"before\";nested();print \"bad\"} END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: next used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-rejects-begin",
                ["function f(){nextfile} function nested(){f()} BEGIN{print \"before\";nested();print \"bad\"} END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: nextfile used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-rejects-end",
                ["function f(){nextfile} function nested(){f()} END{print \"before\";nested();print \"bad\"} END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: nextfile used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-rejects-endfile",
                ["function f(){nextfile} function nested(){f()} ENDFILE{print \"before\";nested();print \"bad\"} END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: nextfile used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-rejects-sort-comparator",
                ["function leaf(){next} function cmp(i1,v1,i2,v2){print \"compare\";leaf()} {a[1]=2;a[2]=1;asort(a,b,\"cmp\");print \"bad\"}END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("compare\n")
            .WithStdErr("awk: next used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-rejects-sort-comparator",
                ["function leaf(){nextfile} function cmp(i1,v1,i2,v2){print \"compare\";leaf()} {a[1]=2;a[2]=1;asort(a,b,\"cmp\");print \"bad\"}END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("compare\n")
            .WithStdErr("awk: nextfile used outside input action\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-exit-rejects-sort-comparator",
                ["function leaf(){exit} function cmp(i1,v1,i2,v2){print \"compare\";leaf()} {a[1]=2;a[2]=1;asort(a,b,\"cmp\");print \"bad\"}END{print \"ending\"}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(2)
            .WithStdOut("compare\n")
            .WithStdErr("awk: exit used inside sort comparison function\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-restores-main-phase-after-file-action-reentry",
                ["function skipfile(){nextfile}function helper(){if((getline extra)>0)print \"got\",extra;next}BEGINFILE{if(ARGIND==2)skipfile()}{print \"record\",$0;helper();print \"bad\"}ENDFILE{print \"closed\",ARGIND}END{print NR}", "/a", "/b", "/c"],
                [File("/a", "a1\n"), File("/b", "b1\nb2\n"), File("/c", "c1\nc2\n")])
            .WithStdOut("record a1\nclosed 1\nclosed 2\ngot c1\nrecord c2\nclosed 3\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-next-unwinds-one-thousand-calls",
                ["function f(n){if(n==0)next;return f(n-1)}{f(999);print \"bad\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-nextfile-unwinds-one-thousand-calls",
                ["function f(n){if(n==0)nextfile;return f(n-1)}{f(999);print \"bad\"}END{print NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("function-exit-from-rule-pattern-runs-end",
                ["function f(){print \"pattern\";exit 6}f(){print \"bad\"}{print \"other\"}END{print \"end\",NR}"],
                [])
            .WithStandardInput("a\nb\n")
            .ExitsWith(6)
            .WithStdOut("pattern\nend 1\n")
            .Build();

    }
}
