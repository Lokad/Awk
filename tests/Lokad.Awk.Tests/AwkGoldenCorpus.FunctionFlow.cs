namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FunctionFlowCompatibilityRepairs()
    {
        yield return AwkGoldenCaseBuilder.Create("prepared-scalar-reference-keeps-prior-global-value-and-field-origin",
            ["function rhs(){g+=10;return 2}function f(x,t){t=x;t+=rhs();t++;g+=rhs();return t}BEGIN{g=7}{n+=f($1);last=$1}END{print n,g,last}"], [])
            .WithStandardInput("0007\n0007\n")
            .WithStdOut("20 31 0007\n")
            .WithNondeterminismNote("Lokad preserves its qualified prior-value-before-RHS compound assignment order; GNU awk 5.3.2 produced 20 51 0007.")
            .Build();

        yield return Passing("array-write-helper-keeps-rhs-before-key-and-origin",
            ["function key(){order=order*10+1;return k}function rhs(){k=2;order=order*10+2;return \"0007\"}function set(a){a[key()]=rhs();return a[2]}BEGIN{k=1;print \"[\"set(a)\"]\",order,(1 in a),(2 in a)}"],
            [], "[0007] 21 0 1\n");
        yield return Passing("array-write-helper-composite-key-keeps-numeric-string-spelling",
            ["function key(x){return x}function update(a,k){a[k,1]+=2;return a[k,1]}{a[$1,1]=$2;print update(a,key($1)),$1,((\"0007\",1) in a),((7,1) in a)}"],
            [], "7 0007 1 0\n", "0007 0005\n");
        yield return Passing("array-write-helper-increments-through-forwarded-alias",
            ["function tick(a){return a[1]++}function outer(a){return tick(a)}BEGIN{print outer(a),a[1],++a[1],a[1]--,a[1],length(a)}"],
            [], "0 1 2 2 1 1\n");

        yield return Passing("array-readonly-helper-keeps-composite-spelling-and-missing-reads",
            ["function key(n){return n}function value(a){return a[key(1),key(2)]}function has(a){return (key(1),key(2)) in a}BEGIN{print has(t),length(t);print \"[\"value(t)\"]\",length(t);t[1,2]=\"0007\";print \"[\"value(t)\"]\",value(t)==7,has(t)}"],
            [], "0 0\n[] 1\n[0007] 0 1\n");
        yield return Passing("array-readonly-helper-field-key-preserves-record-mutation",
            ["function key(){getline;return 1}function value(a){return a[$(key())]}BEGIN{$0=\"old\";t[\"new\"]=\"0007\";print \"[\"value(t)\"]\",$0;exit}"],
            [], "[0007] new\n", "new\n");

        yield return Passing("numeric-array-read-keeps-getline-field-key",
            ["function key(){getline;return 1}BEGIN{$0=\"old\";a[\"new\"]=7;print a[$(key())]*2,$0;exit}"],
            [], "14 new\n", "new\n");

        yield return Passing("array-parameter-survives-global-file-actions",
            ["function drain(a,line){while((getline line)>0)a[\"hits\"]++;return a[\"hits\"]}" +
             "BEGIN{g[\"hits\"]=7;print drain(g)}BEGINFILE{a[\"file\"]++;g[\"seen\"]++}" +
             "END{print a[\"file\"],g[\"hits\"],g[\"seen\"]}"],
            [], "9\n1 9 1\n", "one\ntwo\n");

        yield return Passing("prepared-numeric-parameter-keeps-pending-field-index-evaluation",
            ["function score(x){return x+1}function fieldno(){getline;return 2}BEGIN{$0=\"old 0002\";print score($(fieldno())),$0;exit}"],
            [], "6 new 0005\n", "new 0005\n");
        yield return Passing("prepared-numeric-parameter-keeps-mutating-field-index-evaluation",
            ["function score(x){return x+1}BEGIN{$0=\"0002 0005\";i=1;print score($(i++)),i}"],
            [], "3 2\n");
        yield return Passing("prepared-parameter-comparison-condition-keeps-string-origin",
            ["function score(x,c){if(c==0)return x+1;return x+0}{print score($1,$2),$1,$2}"],
            [], "2 0002 abc\n", "0002 abc\n");
        yield return Passing("prepared-parameter-unused-error-stays-deferred",
            ["--non-decimal-data", "function score(x,c){if(c)return x+1;return 0}function later(){print \"later\";return 0}{print score($1,later()),$1}"],
            [], "later\n0 0x1.5\n", "0x1.5\n");
        yield return Passing("prepared-parameter-contexts-preserve-spelling-comparison-and-truth",
            ["function number(x){return x+0} function identity(x){return x} function equal(x){return x==0} function truth(x){if(x)return 1;return 0} function branch(x,c){if(c)return x+1;return x*2}{print number($1),identity($1),equal($2),truth($2),branch($1,$2),$1,$2}"],
            [], "2 0002 0 1 3 0002 abc\n", "0002 abc\n");
        yield return Passing("prepared-parameter-mixed-numeric-and-text-uses",
            ["function score(n,s){return n+length(s)}{total+=score($1,$2)}END{print total}"],
            [], "6\n", "0002 0007\n");
        yield return Passing("prepared-parameter-omitted-and-untyped-arguments",
            ["function add(x){return x+1}BEGIN{print add(),add(z),z==0,z==\"\"}"], [], "1 1 1 1\n");
        yield return Passing("prepared-parameter-later-getline-keeps-earlier-value",
            ["function score(x,y){return x+y}function more(){getline;return $1}BEGIN{$0=\"0002 old\";print score($1,more()),$1;exit}"],
            [], "7 5\n", "5 new\n");
        yield return Passing("prepared-parameter-unused-array-alias-remains-an-array",
            ["function score(p,x){return x+1}BEGIN{a[1]=7}{total+=score(a,$1)}END{print total,a[1]}"],
            [], "7 7\n", "0002\n0003\n");
        yield return Passing("ordinary-function-nested-loops-propagate-return-and-local-flow",
            ["function score(x,a,t,i,j){t=x;for(i=0;i<4;i++){if(i==1)continue;for(j=0;j<3;j++){if(j==1)break;t+=i};if(i==2)return t};return t} BEGIN{i=99;a[\"x\"]=5}{print score($1,a),i,a[\"x\"]}"],
            [], "9 99 5\n", "0007\n");
        yield return Passing("ordinary-function-loops-preserve-returned-field-origin",
            ["function identity(x,t,i){t=x;i=0;do{i++;if(i<2)continue;return t}while(i<4);return} {v=identity($1)} END{print \"[\"v\"]\",v==7,length(v)}"],
            [], "[0007] 1 4\n", "0007\n");
        yield return Passing("prepared-general-arity-evaluates-arguments-in-caller-order",
            ["function score(a,b,c,d,e){return a*10000+b*1000+c*100+d*10+e} BEGIN{i=1}{n+=score(i++,i++,i++,i++,i++)} END{print n,i}"], [], "80245 11\n", "one\ntwo\n");
        yield return Passing("prepared-general-arity-keeps-field-spelling-and-aliases",
            ["function score(p,a,b,c,d){return a+b+c+d} BEGIN{t[\"0007\"]=9}{n+=score(t,$1,1,2,3)} END{print n,t[\"0007\"],length(t)}"], [], "26 9 1\n", "0007\n0007\n");
        yield return Passing("prepared-general-arity-returns-numeric-string-spelling",
            ["function identity(a,b,c,d,e){return a}{v=identity($1,1,2,3,4)} END{print \"[\"v\"]\",v==7,length(v)}"], [], "[0007] 1 4\n", "0007\n");
        yield return Passing("prepared-general-arity-keeps-local-prefix-and-missing-values",
            ["function score(a,b,c,d,e,t,u){t=a+b+c+d+e;return t+u}{n+=score(1,2,3,4,5)} END{print n}"], [], "30\n", "one\ntwo\n");
        yield return Passing("output-captures-values-and-format-before-later-mutation",
            ["""
                function wipe(){x="";print "held";return 0}
                BEGIN{
                    x="abc";print x,wipe()
                    x="abc";print x,wipe(),1,2,3
                    x="abc";printf "%s%s\n",x,wipe()
                    x="[%s]\n";printf x,wipe()
                }
                """], [], "held\nabc 0\nheld\nabc 0 1 2 3\nheld\nabc0\nheld\n[0]\n");
        yield return Passing("evaluation-captures-values-before-later-global-mutation",
            ["""
                function wipe(){x="";print "held";return 0}
                function f(a,b){print length(a)}
                BEGIN{
                    x="abc";f(x,wipe())
                    x="abc";print index(x,wipe())
                    x="abc";print x==wipe()
                    x="abc";print length(x wipe())
                }
                """], [], "held\n3\nheld\n0\nheld\n0\nheld\n4\n");
        yield return Passing("file-action-cold-scalar-uses-global-binding",
            ["function readNext(x){getline line;print \"function\",x} BEGIN{readNext(\"local\")} BEGINFILE{print \"file\",x;x=\"global\"}"],
            [], "file \nfunction local\n", "record\n");
        yield return Passing("file-action-warm-scalar-keeps-global-binding",
            ["function readNext(x){getline line;print \"function\",x} BEGIN{x=\"global\";readNext(\"local\")} BEGINFILE{print \"file\",x}"],
            [], "file global\nfunction local\n", "record\n");
        yield return Passing("file-action-array-write-does-not-change-caller-alias",
            ["function readNext(a){getline line;print \"function\",a[1]} BEGIN{source[1]=\"local\";readNext(source)} BEGINFILE{a[1]=\"global\";print \"file\",a[1]} END{print \"global\",a[1],source[1]}"],
            [], "file global\nfunction local\nglobal global local\n", "record\n");
        yield return Passing("file-action-nested-functions-see-their-own-arguments",
            ["function helper(x){print \"helper\",x;return x+1} function identity(y){return y==\"\"?\"empty\":y} function readNext(x){getline line;print \"function\",x} BEGIN{readNext(\"local\")} BEGINFILE{print \"nested\",helper(2);print \"argument\",identity(x)}"],
            [], "helper 2\nnested 3\nargument empty\nfunction local\n", "record\n");
        yield return Passing("file-action-endfile-hides-and-restores-caller-locals",
            ["function readAll(x){while((getline line)>0){};print \"function\",x} BEGIN{readAll(\"local\")} ENDFILE{print \"end\",x;x=\"global\"} END{print \"global\",x}"],
            [], "end \nfunction local\nglobal global\n", "record\n");
        yield return Passing("file-action-global-array-promotion-reaches-caller-alias",
            ["function readNext(a){getline line;a[2]=\"after\";print length(a),a[1],a[2]} BEGIN{readNext(source)} BEGINFILE{source[1]=\"global\"} END{print source[1],source[2]}"],
            [], "2 global after\nglobal after\n", "record\n");
        yield return Passing("file-action-bindings-restore-between-files-and-phases",
            ["function readAll(x){while((getline line)>0)print \"read\",x,line;print \"caller\",x} BEGIN{readAll(\"local\")} BEGINFILE{print \"begin\",FNR,x;x=\"global\"} ENDFILE{print \"end\",FNR,x} END{print \"global\",x}", "/a.txt", "/b.txt"],
            [File("/a.txt", "a\n"), File("/b.txt", "b\n")],
            "begin 0 \nread local a\nend 1 global\nbegin 0 global\nread local b\nend 1 global\ncaller local\nglobal global\n");

        yield return Passing("function-length-local-scalars-shadow-global-array",
            ["function size(a){return length(a)} BEGIN{a[1]=7;a[2]=8;print size(\"hello\"),size(12345),size(),length(a)}"],
            [], "5 5 0 2\n");

        yield return Passing("function-length-untyped-aliases-do-not-read-global-array",
            ["function size(a){return length(a)} function relay(x){return size(x)} BEGIN{a[1]=7;a[2]=8;print size(z),relay(z),size(),length(a)}"],
            [], "0 0 0 2\n");

        yield return Passing("function-length-keeps-nested-and-empty-array-aliases",
            ["function size(a){return length(a)} function relay(x){return size(x)} BEGIN{a[1]=7;a[2]=8;b[\"x\"]=1;print size(b),relay(b),length(a);delete b;print size(b),relay(b),length(a)}"],
            [], "1 1 2\n0 0 2\n");

        yield return Passing("prepared-return-keeps-scalar-snapshots-and-input-spelling",
            ["function id(x){return x} function missing(){return unused} BEGIN{s=id(\"007\");n=id(7);u=missing();unused=9;print s,s==7,n,n+1,u==\"\",u==0;print id(id(\"cafÃ©\"))}"],
            [], "007 0 7 8 1 1\ncafÃ©\n");

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
