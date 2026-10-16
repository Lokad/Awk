namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Small canonical controls for the generated, scaled reference benchmark fixtures.
    private static IEnumerable<AwkGoldenCase> BenchmarkCoreCases()
    {
        yield return Passing("field-list-print-keeps-unicode-cached-modified-and-missing-values",
            ["BEGIN{OFS=\"|\";ORS=\"!\";FS=\",\"}{print $1,$3,$2;x=$1;$2=1.25;OFMT=\"%.2f\";print $1,$2,$4;NF=1;print $3,$1}"],
            [], "café|😀|007!café|1.25|!|café!", "café,007,😀\n");
        yield return Passing("field-list-print-keeps-duplicate-fields-and-captured-separator",
            ["{x=$1;FS=\",\";print $1,$1,$3,$2;print NF,$0}"],
            [], "007 007 tail café\n3 007 café tail\n", "007 café tail\n");
        yield return Passing("small-print-keeps-side-effects-operand-and-separator-order",
            ["function first(){OFS=\"|\";return \"café\"} function last(){ORS=\"!\";OFMT=\"%.2f\";return 2.5} BEGIN{print first(),last();print 1,2,3;print 1,2,3,4;print 1,2,3,4,5}"],
            [], "café|2.50!1|2|3!1|2|3|4!1|2|3|4|5!");
        yield return Passing("small-print-keeps-field-values-before-later-record-mutation",
            ["function change(){$0=\"after second third\";return 2}{print $1,$(change()),$3;print $0}"],
            [], "before second third\nafter second third\n", "before old tail\n");
        yield return Passing("small-print-keeps-numeric-separator-and-field-formatting",
            ["BEGIN{OFS=1.5;ORS=2.5;CONVFMT=\"%.2f\";OFMT=\"%.3f\";$0=\"07 008\";print $1,$2;$1=3.25;print $1,$2}"],
            [], "071.500082.503.2501.500082.50");
        yield return Passing("benchmark-core-multi-column-print",
            ["{print $1,$3,$2}"], [], "sku tail value\nother done 7\n", "sku\tvalue\ttail\nother\t7\tdone\n");
        yield return Passing("single-print-keeps-operand-ors-format-and-utf8-order",
            ["function value(){ORS=\"|\";OFMT=\"%.2f\";return 1.25} BEGIN{print value();print \"café\";print 2.5;print $0}"],
            [], "1.25|café|2.50||");
        yield return Passing("prepared-multi-arguments-evaluate-in-caller-order",
            ["function pack(x,y){return x*10+y} function inc(x){return x+1} BEGIN{x=3;total=pack(inc(x),pack(x,inc(x)))} END{print total,x}"],
            [], "74 3\n");
        yield return Passing("prepared-multi-arguments-forward-array-aliases",
            ["function second(a,x){return x+1} function forward(a,x){return second(a,x)} BEGIN{a[1]=7} BEGIN{total=forward(a,41)} END{print total,length(a)}"],
            [], "42 1\n");
        yield return Passing("prepared-multi-arguments-keep-zero-and-four-argument-calls",
            ["function zero(){return 1} function pack(a,b,c,d){return 1000*a+100*b+10*c+d} BEGIN{total=pack(zero(),2,3,4)} END{print total}"],
            [], "1234\n");
        yield return Passing("benchmark-core-two-argument-call-loop",
            ["function step(x,a){return (x*3+a)%101} {x=$1;for(i=0;i<8;i++)x=step(x,i);total+=x} END{print total}"],
            [], "186\n", "7\n7\n");
        yield return Passing("scalar-call-actions-preserve-statement-and-caller-order",
            ["function inc(x){return x+1}{x=$1;total+=inc(x);x=inc(x);total+=inc(x)} END{print total,x}"],
            [], "36 9\n", "7\n8\n");

        yield return Passing("benchmark-core-arithmetic-loop-calls",
            ["function step(x){return (x*3+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}"],
            [], "40\n", "7\n7\n");

        yield return Passing("field-array-predicates-keep-forwarded-alias-and-short-circuit",
            ["function diff(a){return ($1 in a)&&a[$1]!=$2} function forward(a){return diff(a)} BEGIN{a[\"A\"]=1;$0=\"A 2\";print forward(a),length(a);$0=\"B 2\";print forward(a),length(a);$0=\"A 1\";print forward(a);$1=\"A\";$2=9;print forward(a)}"],
            [], "1 1\n0 1\n0\n1\n");

        yield return Passing("field-loops-keep-nf-length-comparisons-and-unicode",
            ["BEGIN{FS=\"|\"}{for(i=1;i<=NF;i++){nonempty+=($i!=\"\");chars+=length($i)}} END{print nonempty,chars}"],
            [], "4 7\n", "aa||b\né|xyz|\n");
        yield return Passing("field-loop-functions-keep-array-aliases-and-record-reads",
            ["function count(a){return NF} function forward(a){return count(a)} function size(i){return length($i)} BEGIN{$0=\"é xyz\";a[1]=1;print forward(a),size(1),size(2),length(a)}"],
            [], "2 1 3 1\n");

        yield return Passing("printf-field-arguments-keep-input-spelling-numbers-and-missing-fields",
            ["{printf \"%s:%04d:%s\\n\",$1,$2,$5}"], [], "café:0007:\nother:-003:\n", "café 007\nother -3\n");
        yield return Passing("printf-field-arguments-keep-modified-values-and-positional-conversions",
            ["BEGIN{$0=\"old 8\";$1=1.25;CONVFMT=\"%.3f\";printf \"%s:%d\\n\",$1,$2;$0=\"65 café\";printf \"%2$s:%1$c:%1$d\\n\",$1,$2}"], [], "1.250:8\ncafé:A:65\n");
        yield return Passing("printf-field-arguments-freeze-values-before-later-mutations",
            ["function change(){$0=\"after 9\";return 2} {printf \"%s:%d\\n\",$1,$(change());print $0}"], [], "before:9\nafter 9\n", "before 7\n");

        yield return Passing("prepared-call-chains-keep-outer-effects-and-loop-results",
            ["function add1(x){return x+1} function combine(x){return add1(add1(x))+x} BEGIN{x=7;print combine(x++),x;total=0;for(i=0;i<5;i++)total+=combine(i);print total;a[1]=1;print(1 in a)}"],
            [], "16 8\n30\n1\n");
        yield return Passing("single-argument-calls-keep-scalar-snapshots-and-forwarded-array-aliases",
            ["function inner(a){a[\"k\"]++;return a[\"k\"]} function forward(a){return inner(a)} function twice(x){return x+x} BEGIN{x=7;print twice(x++),x;print forward(a),a[\"k\"];print forward(a),a[\"k\"];print twice(twice(3))}"],
            [], "14 8\n1 1\n2 2\n12\n");
        yield return Passing("string-builtins-evaluate-values-before-conversions",
            ["function fmt(){CONVFMT=\"%.2f\";return 1} BEGIN{CONVFMT=\"%.0f\";x=1.25;print substr(x,fmt(),3),CONVFMT;s=\"abc\";print index(s,(s=\"b\")),s;n=0;print substr((n++ \"abcdef\"),++n,++n),n}"],
            [], "1.2 %.2f\n2 b\nabc 3\n");
        yield return Passing("string-builtins-nest-through-function-effects-and-unicode",
            ["function val(x){n++;return x} BEGIN{print length(tolower(toupper(val(\"Café\")))),sqrt(int(val(9))),n}"],
            [], "4 3 2\n");
        yield return Passing("synchronous-numeric-local-loop-and-returns",
            ["function run(x,i){for(i=0;i<8;i++)x=(x*3+1)%101;return x} BEGIN{print run(7),run(8)}"], [], "20 16\n");
        yield return Passing("synchronous-numeric-loop-with-call-boundaries",
            ["function inc(x){return x+1} BEGIN{x=0;for(i=0;i<3;i++)x=inc(x);print x}"], [], "3\n");
        yield return Passing("synchronous-numeric-loop-keeps-text-coercion",
            ["BEGIN{x=\"007\";for(i=0;i<2;i++)x=x+1;print x;x=\"é\";for(i=0;i<2;i++)x=x+1;print x}"], [], "9\n2\n");
        yield return Passing("field-array-write-spelling-hits-misses-and-empty",
            ["BEGIN{FS=\"|\";a[7]=1;a[\"007\"]=2;a[\"café\"]=3;a[\"\"]=4;$0=\"7|007|café\";a[$1]++;a[$2]+=5;a[$3]=9;print a[7],a[\"007\"],a[\"café\"];$0=\"missing|\";a[$1]++;a[$2]+=2;print a[\"missing\"],a[\"\"],length(a)}"], [], "2 7 9\n1 6 5\n");
        yield return Passing("field-array-write-alias-and-index-record-effects",
            ["function idx(){$0=\"hit data\";return 1} function add(a){a[$(idx())]+=3} BEGIN{a[\"hit\"]=2;$0=\"old rest\";add(a);print a[\"hit\"],$0}"], [], "5 hit data\n");
        yield return Passing("field-array-write-cached-numeric-and-record-zero",
            ["BEGIN{a[\"007\"]=1;a[7]=2;$0=\"007 rest\";v=$1;a[$1]++;$1=7;a[$1]+=4;$0=7;a[$0]++;print a[\"007\"],a[7]}"], [], "2 7\n");
        yield return Passing("field-array-write-symbol-table-boundary",
            ["BEGIN{v=4;$0=\"v tail\";SYMTAB[$1]+=3;print v}"], [], "7\n");
        yield return Passing("field-array-write-increment-results-and-insertion",
            ["BEGIN{a[\"hit\"]=7;$0=\"hit old\";print a[$1]++,++a[$1],a[$1];$0=\"new later\";a[$1]=5;print a[\"hit\"],a[\"new\"]}"], [], "7 9 9\n9 5\n");
        yield return Passing("field-array-read-spelling-misses-and-empty-key",
            ["BEGIN{a[7]=\"plain\";a[\"007\"]=\"padded\";a[\"café\"]=\"unicode\";FS=\"|\";$0=\"7|007|café\";print a[$1],a[$2],a[$3];$0=\"missing|\";print \"[\"a[$1]\"]\",\"[\"a[$2]\"]\",length(a);print (\"missing\" in a),(\"\" in a)}"],
            [], "plain padded unicode\n[] [] 5\n1 1\n");
        yield return Passing("field-array-read-alias-and-index-record-effects",
            ["function idx(){$0=\"new data\";return 1} function read(a){return a[$(idx())]} BEGIN{a[\"new\"]=\"kept\";$0=\"old before\";print read(a),$0;print a[\"new\"],length(a)}"],
            [], "kept new data\nkept 1\n");
        yield return Passing("field-array-read-cached-numeric-and-record-zero",
            ["BEGIN{a[\"007\"]=\"padded\";a[7]=\"numeric\";$0=\"007 tail\";v=$1;print a[$1];$1=7;print a[$1];$0=7;print a[$0]}"],
            [], "padded\nnumeric\nnumeric\n");
        yield return Passing("field-array-read-symbol-table-boundary",
            ["BEGIN{v=42;$0=\"v tail\";print SYMTAB[$1]}"], [], "42\n");
        yield return Passing("field-array-read-snapshot-before-later-effects",
            ["function change(){a[\"hit\"]=\"after\";$0=\"other rest\";return \"tail\"} BEGIN{a[\"hit\"]=\"before\";$0=\"hit rest\";print a[$1],change(),a[\"hit\"],$0}"],
            [], "before tail after other rest\n");
        yield return Passing("benchmark-core-membership-hit-miss",
            ["BEGIN{lookup[\"hit\"]=1} ($1 in lookup){n++} END{print n+0,length(lookup)}"],
            [], "2 1\n", "hit payload\nmiss payload\nhit other\n");
        yield return Passing("field-membership-original-key-spelling-and-empty",
            ["BEGIN{a[7]=1;a[\"007\"]=2;a[-2147483648]=3;a[\"café\"]=4;a[\"\"]=5}{print ($1 in a),NF}"],
            [], "1 1\n1 1\n1 1\n1 1\n0 1\n1 0\n", "7\n007\n-2147483648\ncafé\nmissing\n\n");
        yield return Passing("field-membership-index-effects-alias-and-delete",
            ["function idx(){$0=\"hit new\";return 1} function has(a){return ($(idx()) in a)} BEGIN{a[\"hit\"]=1;$0=\"miss old\";print has(a),$0;delete a[\"hit\"];$0=\"hit after\";print ($1 in a),length(a)}"],
            [], "1 hit new\n0 0\n");
        yield return Passing("field-membership-cached-modified-numeric-and-record-zero",
            ["BEGIN{a[\"007\"]=1;a[7]=2;a[\"\"]=3;$0=\"007 tail\";v=$1;print ($1 in a);$1=7;print ($1 in a);$0=7;print ($0 in a);print ($9 in a),NF}"],
            [], "1\n1\n1\n1 1\n");
        yield return Passing("field-membership-symbol-table-boundary",
            ["BEGIN{v=3;$0=\"v tail\";print ($1 in SYMTAB);$0=\"absent tail\";print ($1 in SYMTAB)}"],
            [], "1\n0\n");
        yield return Passing("array-split-ordinal-key-text-delete-reuse",
            ["BEGIN{for(i=0;i<32;i++){split(\"old tail\",a);a[\"1\"]=\"new\";delete a[\"1\"];delete a[2];n+=length(a)} print n;split(\"last\",a);print a[\"1\"],length(a)}"],
            [], "0\nlast 1\n");
        var ignoredTail = string.Join(" ", Enumerable.Repeat("ERROR", 61));
        var keptTail = string.Join(" ", Enumerable.Repeat("tail", 61));
        yield return Passing("benchmark-core-early-fields-wide",
            ["$1==\"ERROR\"{print $3}"], [], "payload\n",
            "INFO worker ignored " + ignoredTail + "\nERROR worker payload " + keptTail + "\n");
        yield return Passing("benchmark-core-early-fields-wide-literal",
            ["BEGIN{FS=\"|\"} $1==\"ERROR\"{print $3}"], [], "payload\n",
            "INFO|worker|ignored|" + ignoredTail.Replace(' ', '|') +
            "\nERROR|worker|payload|" + keptTail.Replace(' ', '|') + "\n");
        yield return Passing("benchmark-core-arithmetic-loops",
            ["{x=$1;for(i=0;i<8;i++)x=(x*3+1)%101;total+=x} END{print total}"],
            [], "40\n", "7\n7\n");
        yield return Passing("benchmark-core-user-functions",
            ["function inc(x){return x+1} function twice(x){return inc(inc(x))} {total+=twice($1)} END{print total}"],
            [], "12\n", "7\n1\n");
        yield return Passing("benchmark-core-string-functions",
            ["{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))} END{print total}"],
            [], "24\n", "id:beta:tail\nid:beta:tail\n");
        yield return Passing("benchmark-core-concatenation",
            ["{s=\"[\" $0 \":\" NR \"]\";total+=length(s)} END{print total}"],
            [], "16\n", "item\nitem\n");
        yield return Passing("benchmark-core-regex-match",
            ["$0~/^[A-Z]{3}[0-9]{4}$/{n++} END{print n+0}"],
            [], "1\n", "ABC0123\nabc0123\n");
        yield return Passing("benchmark-core-regex-captures",
            ["match($0,/^([[:alpha:]]+)([0-9]+)$/,m){total+=length(m[1])+m[2]} END{print total}"],
            [], "24\n", "sku007\nitem010\n");
    }
}
