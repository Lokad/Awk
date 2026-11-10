namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ArgumentStorageCases()
    {
        foreach (var (mode, options) in new (string, string[])[]
        {
            ("default", []), ("posix", ["--posix"]), ("traditional", ["--traditional"]),
            ("nondecimal", ["--non-decimal-data"])
        })
        {
            yield return Passing("argument-numeric-comparison-retains-ordinary-string-" + mode,
                [.. options, "function clip(s,n){if(length(s)>n)return substr(s,1,n);return s}function relay(s,n){return clip(s,n)}BEGIN{print relay(\"ABCDEFGHIJ\",\"2\"),relay(\"ABCDEFGHIJ\",2);print relay(\"ABCDEFGHIJ\",\"2tail\")}{print relay($1,$2),$2}"],
                [], "ABCDEFGHIJ AB\nABCDEFGHIJ\nAB 0002\n", "ABCDEFGHIJ 0002\n");
            yield return Passing("argument-numeric-comparison-input-number-mode-" + mode,
                [.. options, "function clip(s,n){if(length(s)>n)return substr(s,1,n);return s}{print clip($1,$2),$2}"],
                [], (mode == "nondecimal" ? "ABCDEFGH" : "ABCDEFGHIJ") + " 0010\n", "ABCDEFGHIJKL 0010\n");
        }
        yield return Passing("argument-numeric-comparison-spelling-observer-prevents-normalization",
            ["function clip(s,n){if(n!=\"0002\")return \"MISMATCH\";if(length(s)>n)return substr(s,1,n);return s}function relay(s,n){return clip(s,n)}{print relay($1,$2),$2,typeof($2)}"],
            [], "AB 0002 strnum\n", "ABCDEFGHIJ 0002\n");
        yield return Passing("argument-numeric-comparison-captures-before-later-record-mutation",
            ["function change(){$0=\"new 0009\";return 1}function clip(s,n,ignored){if(length(s)>n)return substr(s,1,n);return s}{print clip($1,$2,change()),$0}"],
            [], "AB new 0009\n", "ABCDEFGHIJ 0002\n");
        yield return Passing("argument-numeric-comparison-keeps-scalar-reassignment-and-array-alias",
            ["function clip(s,n,a){n=\"2\";a[\"seen\"]=n;return length(s)>n?substr(s,1,n):s}{print clip($1,$2,a),a[\"seen\"],$2}"],
            [], "ABCDEFGHIJ 2 0002\n", "ABCDEFGHIJ 0002\n");
        yield return Passing("argument-numeric-comparison-does-not-use-arithmetic-prefix-cache",
            ["function choose(n){return 10>n?\"less\":\"more\"}{prior=$1+0;print choose($1),$1}"],
            [], "more 2tail\nless 0002\n", "2tail\n0002\n");
        yield return Passing("argument-numeric-comparison-keeps-untaken-conversion-error-deferred",
            ["--non-decimal-data", "function first(n){if(0)return n<1;return \"ok\"}BEGIN{getline;print first($1);print $1}"],
            [], "ok\n0x1.5\n", "0x1.5\n");
        yield return Passing("argument-numeric-comparison-keeps-signed-zero-and-nonfinite-facts",
            ["function probe(n){return (n==0)\":\"(n!=0)\":\"(n>0)\":\"(n<0)}{print probe($1),$1}"],
            [], "1:0:0:0 -0\n0:1:0:0 +nan\n0:1:1:0 +inf\n0:1:0:1 -inf\n", "-0\n+nan\n+inf\n-inf\n");
        yield return Passing("argument-numeric-builtin-results-format-at-use",
            ["BEGIN{CONVFMT=\"%.2f\";OFMT=\"%.1f\";n=sqrt(2);print typeof(n),sprintf(\"%s\",n),n;CONVFMT=\"%.3f\";print n \"\",int(-2.9),sqrt(9)+int(2.9),typeof(strtonum(\"0x10\"))}"],
            [], "number 1.41 1.4\n1.414 -2 5 number\n");
        yield return Passing("argument-numeric-builtin-results-key-kind-and-random-state",
            ["BEGIN{a[int(\"0007\")]=\"seven\";print a[7],a[\"0007\"],length(a);srand(7);x=rand();srand(7);print x==rand(),typeof(srand(8))}"],
            [], "seven  2\n1 number\n");
        yield return Passing("argument-numeric-builtin-results-nesting-and-effect-order",
            ["function step(x){order=order x;return x+0}function total(x){return int(x)+sqrt(int(x+2))}BEGIN{print int(step(\"0007\"))+sqrt(step(\"0009\")),order;print total(7),and(7,3),or(1,2),xor(7,3),lshift(1,3),rshift(8,2);if(0)print int(1/zero)}"],
            [], "10 00070009\n10 3 3 4 8 2\n");
        yield return Passing("argument-numeric-builtin-results-keep-field-spelling",
            ["{n=int(int($1)+sqrt($2));a[n]++;print n,typeof(n),$1,$2}END{print a[10]}"],
            [], "10 number 0007 0009\n1\n", "0007 0009\n");
        foreach (var (mode, options, expected) in new (string, string[], string)[]
        {
            ("default", [], "jkl"), ("posix", ["--posix"], "jkl"),
            ("traditional", ["--traditional"], "jkl"),
            ("nondecimal", ["--non-decimal-data"], "hij")
        })
            yield return Passing("argument-mixed-builtin-numeric-positions-" + mode,
                [.. options, "function part(s,p,n){return substr(s,p,n)}function relay(s,p,n){return part(s,p,n)}{print relay($1,$2,$3),$2,$3}"],
                [], expected + " 0010 0003\n", "abcdefghijklmnop 0010 0003\n");
        yield return Passing("argument-mixed-builtin-captures-before-later-record-change",
            ["function change(){$0=\"new 0009 0001\";return 0}function part(s,p,n,ignored){return substr(s,p,n)}{print part($1,$2,$3,change()),$0}"],
            [], "bcd new 0009 0001\n", "abcdef 0002 0003\n");
        yield return Passing("argument-mixed-builtin-observed-numeric-spelling",
            ["function part(s,p,n){if(p!=\"0002\"||n!=\"0003\")return \"MISMATCH\";return substr(s,p,n)}function relay(s,p,n){return part(s,p,n)}{print relay($1,$2,$3),$2,$3}"],
            [], "bcd 0002 0003\n", "abcdef 0002 0003\n");
        yield return Passing("argument-mixed-builtin-ordinary-prefix-strings",
            ["function part(s,p,n){return substr(s,p,n)}BEGIN{print part(\"abcdef\",\"2tail\",\"3tail\");print part(\"abcdef\",\"2\",\"3\")}"],
            [], "bcd\nbcd\n");
        yield return Passing("argument-mixed-builtin-keeps-forwarded-array-aliases",
            ["function part(s,p,n,a){a[\"seen\"]=substr(s,p,n);return a[\"seen\"]}function relay(s,p,n,a){return part(s,p,n,a)}{print relay($1,$2,$3,a),a[\"seen\"],length(a)}"],
            [], "bcd bcd 1\n", "abcdef 0002 0003\n");
        yield return Passing("argument-field-demands-keep-captures-before-record-and-separator-changes",
            ["function show(a,b,c,d){print a,b,c,d;print $0}function change(){FS=\":\";$0=\"new:0011\";return \"done\"}BEGIN{for(i=0;i<3;i++){FS=\" \";$0=\"key 0007\";show($1,$2,change(),$2)}}"], [],
            "key 0007 done 0011\nnew:0011\nkey 0007 done 0011\nnew:0011\nkey 0007 done 0011\nnew:0011\n");
        yield return Passing("argument-field-demands-keep-high-missing-and-whole-record-values",
            ["function show(a,b,c,d){print \"[\"a\"]\",\"[\"b\"]\",\"[\"c\"]\",\"[\"d\"]\"}BEGIN{for(i=0;i<2;i++){$0=\"0001 0002\";show($2,$1,$9,$0);show($1,7,$2,$9)}}"], [],
            "[0002] [0001] [] [0001 0002]\n[0001] [7] [0002] []\n[0002] [0001] [] [0001 0002]\n[0001] [7] [0002] []\n");
        yield return Passing("argument-binding-ordinary-global-and-local-scalars",
            ["function leaf(x){switch(0){default:break}return x+0}function wrap(x){return leaf(x)}BEGIN{x=\"0007\";print wrap(x),x;for(i=0;i<3;i++)print leaf(++x);print x}"],
            [], "7 0007\n8\n9\n10\n10\n");
        yield return Passing("argument-binding-ordinary-actual-and-omitted-local-arrays",
            ["function leaf(a,k,v){switch(0){default:break}a[k]+=v;return a[k]}function forward(a,k,v){return leaf(a,k,v)}function local(k,v,a){return leaf(a,k,v)}BEGIN{for(i=1;i<=3;i++)print forward(a,\"key\",i);print length(a),a[\"key\"];print local(\"key\",7),local(\"key\",9);print a[\"key\"]}"],
            [], "1\n3\n6\n1 6\n7 9\n6\n");
        yield return Passing("argument-binding-global-actions-hide-scalar-caller",
            ["function leaf(a){switch(0){default:break}return a[\"k\"]}function scan(x,line){getline line;return x}BEGINFILE{print leaf(x)}BEGIN{x[\"k\"]=\"global\";print scan(\"local\"),x[\"k\"]}"],
            [], "global\nlocal global\n", "payload\n");
        yield return Passing("argument-binding-ordinary-calls-see-symbol-table-assignment",
            ["function leaf(x){switch(0){default:break}return x+0}BEGIN{x=7;print leaf(x);SYMTAB[\"x\"]=\"0009\";print leaf(x),x}"],
            [], "7\n9 0009\n");
        yield return Passing("argument-storage-large-calls-evaluate-in-caller-order",
            ["function total(a,b,c,d,e,t){t=a+b+c+d+e;return t}BEGIN{for(i=0;i<3;i++)print total(++x,++x,++x,++x,++x),x}"],
            [], "15 5\n40 10\n65 15\n");
        yield return Passing("argument-storage-large-calls-preserve-aliases-and-spelling",
            ["function use(x,a,b,y,z,missing){a[\"sum\"]+=x;b[\"seen\"]=y;return x \":\" z \":\" length(missing)}BEGIN{a[\"sum\"]=0;for(i=0;i<2;i++)print use(\"0007\",a,a,++n,\"0009\"),a[\"sum\"],a[\"seen\"],n}"],
            [], "0007:0009:0 7 1 1\n0007:0009:0 14 2 2\n");
        yield return Passing("argument-storage-nesting-spelling-and-output",
            ["function value(x){return int(x)+int(int(x))} {print int($1),atan2(0,1),value($1),sprintf(\"%s\",$1),length($1);printf \"%s:%d:%d\\n\",$1,int($1),value($1)}"],
            [], "7 0 14 0007 4\n0007:7:14\n9 0 18 0009 4\n0009:9:18\n", "0007\n0009\n");
        yield return Passing("argument-storage-changing-arity-preserves-effect-order",
            ["function take(){return ++n}BEGIN{for(i=0;i<2;i++){print int(take()),atan2(0,take()),length(\"spelling\"),int(int(take())+take()),n;printf \"%d/%d/%s\\n\",int(take()),int(take()),\"done\"}print n}"],
            [], "1 0 8 7 4\n5/6/done\n7 0 8 19 10\n11/12/done\n12\n");
    }
}
