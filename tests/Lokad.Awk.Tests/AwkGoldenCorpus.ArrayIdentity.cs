namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Native reference: GNU awk 5.3.2, source 027ebff5. Interfering operand order
    // intentionally retains the qualified Lokad contract; differences are explicit.
    private static IEnumerable<AwkGoldenCase> ArrayIdentityCases() =>
    [
        Passing("composite-field-key-formats-mutated-numeric-value",
            ["BEGIN{$0=\"a x\";CONVFMT=\"%.2f\";$1=2.5;a[$1,$2]=8;CONVFMT=\"%.3f\";print a[\"2.50\",\"x\"]}"], [], "8\n"),
        Passing("composite-field-key-keeps-empty-and-record-components",
            ["BEGIN{$0=\"a\";a[$9,$0]=4;print a[\"\",\"a\"],NF}"], [], "4 1\n"),
        Passing("composite-field-key-keeps-original-numeric-spelling",
            ["{a[$1,$2]+=$3;print a[\"k\",\"0007\"],a[\"k\",7]+0,$2}"], [], "2 0 0007\n", "k 0007 2\n"),
        Passing("composite-field-key-copies-before-record-mutation",
            ["function edit(){$1=\"changed\";return \"tail\"} {a[$1,edit(),$1]=7;print a[\"original\",\"tail\",\"changed\"],length(a)}"],
            [], "7 1\n", "original rest\n"),
        Passing("composite-field-key-copies-before-main-getline",
            ["function readnext(){getline;return $1} {a[$1,readnext()]=5;print a[\"first\",\"second\"],NR,FNR;exit}"],
            [], "5 2 2\n", "first\nsecond\n"),
        Passing("composite-field-key-evaluates-dynamic-indices-once",
            ["function idx(){return ++n} {a[$idx(),$idx()]=3;print a[\"a\",\"b\"],n}"],
            [], "3 2\n", "a b\n"),
        Passing("array-identity-dynamic-field-prefix-postfix", ["BEGIN{$0=\"k other\";i=1;a[\"k\"]=\"0007\";print a[$i]++,++a[$(i+0)],a[\"k\"]}"], [],
            "7 9 9\n"),
        AwkGoldenCaseBuilder.Create("array-identity-clear-rhs",
            ["function f(){delete a;a[\"other\"]=17;return 3} BEGIN{a[\"k\"]=5;a[\"k\"]+=f();print a[\"k\"],a[\"other\"],length(a)}"], [])
            .WithStdOut("8 17 2\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 3 17 2\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-delete-reinsert-rhs",
            ["function f(){delete a[\"k\"];a[\"k\"]=20;return 3} BEGIN{a[\"k\"]=5;a[\"k\"]+=f();print a[\"k\"],length(a)}"], [])
            .WithStdOut("8 1\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 23 1\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-split-rhs",
            ["function f(){split(\"10:20\",a,\":\");return 3} BEGIN{a[1]=5;a[1]+=f();print a[1],a[2],length(a)}"], [])
            .WithStdOut("8 20 2\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 13 20 2\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-alias-clear-rhs",
            ["function f(p){delete p;p[\"other\"]=17;return 3} BEGIN{a[\"k\"]=5;a[\"k\"]+=f(a);print a[\"k\"],a[\"other\"],length(a)}"], [])
            .WithStdOut("8 17 2\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 3 17 2\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-duplicate-alias-rhs",
            ["function f(p,q){p[\"k\"]+=(q[\"k\"]=20);print p[\"k\"],q[\"k\"]} BEGIN{a[\"k\"]=5;f(a,a);print a[\"k\"]}"], [])
            .WithStdOut("25 25\n25\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 40 40\\n40\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-rehash-rhs",
            ["function f(i){for(i=0;i<2000;i++)a[\"x\" i]=i;return 3} BEGIN{a[\"k\"]=5;a[\"k\"]+=f();print a[\"k\"],length(a),a[\"x1999\"]}"], [])
            .WithStdOut("8 2001 1999\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-key-before-rhs",
            ["BEGIN{i=1;a[1]=5;a[i++]+=(i=9);print i,a[1],length(a)}"], [])
            .WithStdOut("9 14 1\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 10 5 2\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-missing-read-before-rhs",
            ["function f(p){print (\"k\" in p);return 3} BEGIN{a[\"other\"]=1;a[\"k\"]+=f(a);print a[\"k\"],length(a)}"], [])
            .WithStdOut("1\n3 2\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 0\\n3 2\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-alias-postincrement",
            ["function f(p){return p[\"k\"]++} BEGIN{a[\"k\"]=4;print f(a),a[\"k\"],++a[\"k\"]}"], [])
            .WithStdOut("4 5 6\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-duplicate-alias-clear",
            ["function f(p,q){delete p;q[\"new\"]=3;print length(p);return 2} BEGIN{a[\"k\"]=5;a[\"k\"]+=f(a,a);print a[\"k\"],a[\"new\"],length(a)}"], [])
            .WithStdOut("1\n7 3 2\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced 1\\n2 3 2\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-subsep-rhs",
            ["function f(){SUBSEP=\"!\";delete a;return 3} BEGIN{SUBSEP=\":\";a[1,2]=5;a[1,2]+=f();print a[\"1:2\"],a[\"1!2\"],length(a)}"], [])
            .WithStdOut("8  2\n")
            .WithNondeterminismNote("Lokad captures the compound target/key and prior value before the RHS, preserving its documented operand order. GNU awk 5.3.2 produced  3 2\\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-identity-membership-no-insertion",
            ["BEGIN{a[\"existing\"]=3;print (\"missing\" in a),length(a);x=a[\"missing\"];print (\"missing\" in a),length(a),typeof(x)}"], [])
            .WithStdOut("0 1\n1 2 unassigned\n")
            .Build()
    ];
}

