namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Native reference: GNU awk 5.3.2, source 027ebff5. Interfering operand order
    // intentionally retains the qualified Lokad contract; differences are explicit.
    private static IEnumerable<AwkGoldenCase> ArrayIdentityCases() =>
    [
        Passing("nested-owner-windows-keep-untyped-origins-field-rebuilds-and-call-order",
            ["function add(p,k,v){p[k][\"s\"]+=v;p[k][\"n\"]++;return p[k][\"s\"]}BEGIN{print add(a,x,x),a[\"\"][\"n\"],\"[\"x\"]\";print add(a,\"007\",7),add(a,\"007\",3),a[\"007\"][\"n\"];$0=\"left 007\";b[$1][\"s\"]+=+$2;b[$1][\"n\"]++;$1=\"right\";b[$1][\"s\"]+=2;b[$1][\"n\"]++;print b[\"left\"][\"s\"],b[\"right\"][\"s\"],length(b);print \"before\",add(a,\"007\",1),\"after\";print a[\"007\"][\"s\"],a[\"007\"][\"n\"]}"], [], "0 1 []\n7 10 2\n7 2 2\nbefore 11 after\n11 3\n"),
        AwkGoldenCaseBuilder.Create("nested-owner-windows-preserve-pending-csv-diagnostics",
            ["--csv", "function work(p,k,v){p[k][\"s\"]+=v;p[k][\"n\"]++;return p[k][\"s\"]}BEGIN{print work(a,\"007\",FS=\":\"),a[\"007\"][\"n\"];print work(a,\"007\",2),a[\"007\"][\"n\"]}"], [])
            .WithStdOut("0 1\n2 2\n")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n")
            .Build(),
        AwkGoldenCaseBuilder.Create("entry-owned-long-key-survives-delete-and-record-change",
            ["function erase(p,k){delete p[k];$0=\"discarded\";return 3}{a[$1]=5;a[$1]+=erase(a,$1);print a[\"abcdefghijklé\"],length(a),$0}"], [])
            .WithStandardInput("abcdefghijklé tail\n")
            .WithStdOut("8 1 discarded\n")
            .WithNondeterminismNote("The qualified compound-update order captures the owned key and old number before RHS deletion and record mutation.")
            .Build(),
        AwkGoldenCaseBuilder.Create("special-entry-capture-survives-record-and-scalar-mutation",
            ["function change(){v=20;$0=\"gone\";return 3}BEGIN{v=5;$0=\"v tail\";SYMTAB[$1]+=change();print v,$0;v=5;SYMTAB[\"v\"]++;print v}"], [])
            .WithStdOut("8 gone\n6\n")
            .WithNondeterminismNote("The qualified compound-update order captures the special-array key and old number before RHS mutation.")
            .Build(),
        Passing("retained-short-long-utf8-and-nul-keys-survive-record-reuse-and-sorted-traversal",
            ["BEGIN{FS=\"|\";a[\"seed\"]=0;PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"}{a[$1]=$2;$0=\"discarded\"}END{delete a[\"seed\"];for(k in a)print length(k),a[k],typeof(a[k])}"], [],
            "3 09 strnum\n4 007 strnum\n5 08 strnum\n", "abc😀|007\nabc😀x|08\na\0b|09\n"),
        Passing("prepared-literal-keys-keep-byte-boundaries-spelling-and-text-uses",
            ["function bump(p){p[\"abc😀\"][\"0007\"]++;p[\"abc😀x\"][\"0007\"]+=2;p[\"a\\0b\"][\"0007\"]+=3}BEGIN{a[\"abc😀\"][\"0007\"]=7;a[\"abc😀x\"][\"0007\"]=8;a[\"a\\0b\"][\"0007\"]=9;bump(a);print a[\"abc😀\"][\"0007\"],a[\"abc😀x\"][\"0007\"],a[\"a\\0b\"][\"0007\"];print (\"7\" in a[\"abc😀\"]),(\"0007\" in a[\"abc😀\"]);delete a[\"abc😀\"][\"0007\"];print (\"0007\" in a[\"abc😀\"]),length(a[\"abc😀\"]),(\"a\\0b\" in a);CONVFMT=\"%.2f\";x=\"0007\";flat[\"0007\"]=1;flat[\"7\"]=2;flat[\"abc😀\"]=3;flat[\"abc😀x\"]=4;flat[\"a\\0b\"]=5;print flat[x],flat[7],typeof(\"0007\"),\"0007\"+0,sprintf(\"%s\",\"abc😀\");delete flat[\"abc😀x\"];print length(flat),(\"abc😀x\" in flat)}"], [],
            "8 10 12\n0 1\n0 0 1\n1 2 string 7 abc😀\n4 0\n"),
        Passing("complete-nested-operations-preserve-aliases-depth-and-missing-insertion",
            ["function add(p,k){p[k][\"middle\"][\"n\"]+=2;return p[k][\"middle\"][\"n\"]++}BEGIN{a[\"root\"][\"seed\"]=0;print add(a[\"root\"],\"007\"),add(a[\"root\"],\"007\");print a[\"root\"][\"007\"][\"middle\"][\"n\"];print (\"missing\" in a[\"root\"][\"007\"][\"middle\"]),length(a[\"root\"][\"007\"][\"middle\"]);print \"[\"a[\"root\"][\"007\"][\"middle\"][\"missing\"]\"]\";print (\"missing\" in a[\"root\"][\"007\"][\"middle\"]),length(a[\"root\"][\"007\"][\"middle\"]);delete a[\"root\"][\"seed\"];print length(a[\"root\"])}"], [],
            "2 5\n6\n0 1\n[]\n1 2\n1\n"),
        Passing("complete-and-staged-nested-sites-keep-independent-owners",
            ["function key(x){getline x;return x}function update(p){p[\"short\"][\"v\"]++;p[\"staged\"][key()]++;p[\"short\"][\"v\"]+=2;return p[\"short\"][\"v\"]+p[\"staged\"][key()]}BEGIN{for(i=0;i<3;i++)print update(a);print a[\"short\"][\"v\"],a[\"staged\"][\"v\"]}"], [],
            "4\n8\n12\n9 3\n", "v\nv\nv\nv\nv\nv\n"),
        Passing("prepared-owner-bindings-respect-key-record-input-and-array-mutation",
            ["BEGIN{k=\"007\";a[k][\"sum\"]=0;a[k][\"sum\"]+=2;a[k][\"count\"]++;print a[k][\"sum\"],a[k][\"count\"];SYMTAB[\"k\"]=\"next\";a[k][\"sum\"]+=3;a[k][\"count\"]++;print a[k][\"sum\"],a[\"007\"][\"sum\"];delete a[k];print (k in a);getline k;a[k][\"sum\"]+=4;a[k][\"count\"]++;print a[k][\"sum\"],length(a);$0=\"old 1\";b[$1][\"s\"]+=1;b[$1][\"n\"]++;$1=\"new\";print (\"old\" in b),b[$1][\"s\"]+0,length(b)}"], [],
            "2 1\n3 2\n0\n6 1\n1 0 2\n", "007\n"),
        Passing("prepared-owner-bindings-keep-nul-and-utf8-literal-prefixes-distinct",
            ["BEGIN{a[\"a\\0b\"][\"sum\"]=0;a[\"a\\0b\"][\"sum\"]+=1;a[\"a\\0b\"][\"n\"]++;print a[\"a\\0b\"][\"sum\"],a[\"a\\0b\"][\"n\"];b[\"abc😀\"][\"sum\"]+=2;b[\"abc😀\"][\"n\"]++;print b[\"abc😀\"][\"sum\"],b[\"abc😀\"][\"n\"];b[\"abc😀x\"][\"sum\"]+=3;b[\"abc😀x\"][\"n\"]++;print b[\"abc😀x\"][\"sum\"],b[\"abc😀x\"][\"n\"];print length(a)+length(b)}"], [],
            "1 1\n2 1\n3 1\n3\n"),
        Passing("plain-string-results-keep-spelling-through-calls-traversal-and-records",
            ["function carry(x){return x}BEGIN{v=carry(sprintf(\"%03d\",7));print typeof(v),v,(v==7),v+0;a[\"007\"]=1;for(k in a)print typeof(k),k,(k==7),k+0;$0=\"seed\";$1=sprintf(\"%03d\",7);print typeof($0),$0,($0==7),$0+0}"], [],
            "string 007 0 7\nstring 007 0 7\nstring 007 0 7\n"),
        Passing("plain-string-callback-keys-keep-origin-and-sorted-destination",
            ["function compare(i,a,j,b){if(typeof(i)!=\"string\"||typeof(j)!=\"string\")bad++;return a-b}BEGIN{a[\"007\"]=2;a[\"02\"]=1;print asort(a,b,\"compare\"),bad+0;print b[1],b[2];print asorti(a,c,\"@ind_str_asc\");print typeof(c[1]),c[1],typeof(c[2]),c[2]}"], [],
            "2 0\n1 2\n2\nstring 007 string 02\n"),
        Passing("plain-string-produced-nondecimal-errors-stay-deferred",
            ["--non-decimal-data", "function ignore(x){return \"safe\"}BEGIN{v=sprintf(\"%s\",\"0x1.5\");a[v]=1;print ignore(v),typeof(v),length(v);for(k in a)print typeof(k),k,length(k);print sprintf(\"%03d\",10)+0}"], [],
            "safe string 5\nstring 0x1.5 5\n8\n"),
        Passing("plain-string-produced-numbers-use-default-mode",
            ["BEGIN{v=sprintf(\"%s\",\"010\");w=sprintf(\"%s\",\"0x10\");print v+0,w+0,v,w}"], [],
            "10 0 010 0x10\n"),
        Passing("plain-string-produced-numbers-use-posix-mode",
            ["--posix", "BEGIN{v=sprintf(\"%s\",\"010\");w=sprintf(\"%s\",\"0x10\");print v+0,w+0,v,w}"], [],
            "10 16 010 0x10\n"),
        Passing("plain-string-produced-numbers-use-nondecimal-mode",
            ["--non-decimal-data", "BEGIN{v=sprintf(\"%s\",\"010\");w=sprintf(\"%s\",\"0x10\");print v+0,w+0,v,w}"], [],
            "8 16 010 0x10\n"),
        Passing("stored-values-forward-input-and-string-origins",
            ["function carry(x,a){a[\"saved\"]=x;return a[\"saved\"]}function relay(x,a){return carry(x,a)}{v=relay($1,a);w=relay($2,a);print typeof(v),v,v+0,(v==7),typeof(w),w,w+0,(w==7)}"],
            [], "strnum 007 7 1 string 007tail 7 0\nstring word 0 0 strnum 0008 8 0\n", "007 007tail\nword 0008\n"),
        Passing("stored-values-forward-deferred-string-errors",
            ["--non-decimal-data", "function carry(x,a){a[\"saved\"]=x;return a[\"saved\"]}function ignore(x){return \"ok\"}BEGIN{v=carry(\"0x1.5\",a);print ignore(v),typeof(v),length(v);a[\"oct\"]=\"010\";x=a[\"oct\"];print x,x+0,typeof(x)}"],
            [], "ok string 5\n010 8 string\n"),
        Passing("quoted-values-keep-string-origin-through-calls-and-keys",
            ["function echo(x){return x}BEGIN{a[\"007\"]=\"0007\";x=echo(a[\"007\"]);print typeof(x),x,(x==7),x+0;print typeof(\"12tail\"),(\"12tail\"==12),\"12tail\"+0;print length(\"sum\"),index(\"count\",\"ou\"),(\"\"?1:0),(\"0\"?1:0)}"],
            [], "string 0007 0 7\nstring 0 12\n3 2 0 1\n"),
        Passing("quoted-values-nondecimal-errors-stay-deferred",
            ["--non-decimal-data", "function ignore(x){return \"safe\"}BEGIN{print ignore(\"0x1.5\"),typeof(\"0x1.5\"),length(\"0x1.5\");print \"010\"+0,\"0x10\"+0}"],
            [], "safe string 5\n8 16\n"),
        Passing("borrowed-assignment-miss-keeps-escaped-field-spelling",
            ["function put(p){p[$1]=$2;p[$1]=$2 \"!\"}{put(a);saved=$1;$0=\"changed\";print a[saved],saved,(\"changed\" in a)}END{print length(a),a[\"0007\"],a[7],a[\"café\"]}"],
            [], "02! 0007 0\n04! 7 0\n06! café 0\n08! 0007 0\n3 08! 04! 06!\n", "0007 02\n7 04\ncafé 06\n0007 08\n"),
        Passing("borrowed-assignment-miss-keeps-empty-composite-components",
            ["function put(p){p[$1,$2]=$3}BEGIN{FS=\",\";SUBSEP=\"\"}{put(a);print a[$1,$2];$0=\"gone\"}END{print length(a),a[\"0007\"],a[7],a[\"caféx\"]}"],
            [], "02\n04\n06\n3 02 04 06\n", ",0007,02\n,7,04\ncafé,x,06\n"),
        Passing("introspection-groups-preserve-scalar-use",
            ["BEGIN{print typeof(a),typeof((a)),typeof(a);print isarray((b)),typeof(b)}"], [],
            "untyped unassigned unassigned\n0 unassigned\n"),
        Passing("introspection-implicit-length-call",
            ["BEGIN{print typeof(length),typeof(length()),typeof((length));print typeof(FS),typeof(NF)}"], [],
            "number number number\nstring number\n"),
        Passing("introspection-implicit-length-after-split",
            ["BEGIN{split(\"\",parts);print typeof(length),typeof(length()),typeof((length));print typeof(FS),typeof(NF)}"], [],
            "number number number\nstring number\n"),
        Passing("split-captured-array-origin-promoted-by-pattern",
            ["function pattern(){a[1][9]=3;return \":\"}BEGIN{print split(\"x:y\",a[1],pattern()),a[1][1],a[1][2],length(a[1])}"], [],
            "2 x y 2\n"),
        Passing("patsplit-captured-array-origin-promoted-by-pattern",
            ["function pattern(){a[1][9]=3;return \"[a-z]\"}BEGIN{print patsplit(\"x:y\",a[1],pattern()),a[1][1],a[1][2],length(a[1])}"], [],
            "2 x y 2\n"),
        AwkGoldenCaseBuilder.Create("array-cell-update-keeps-prior-value-through-same-key-write",
            ["function rhs(a){a[\"x\"]=20;return 3} BEGIN{a[\"x\"]=5;a[\"x\"]+=rhs(a);print a[\"x\"]}"], [])
            .WithStdOut("8\n")
            .WithNondeterminismNote("Lokad preserves its documented prior-value-before-RHS order; GNU awk 5.3.2 produced 23.")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-cell-update-reacquires-deleted-reinserted-key",
            ["function rhs(a){delete a[\"x\"];a[\"x\"]=20;return 3} BEGIN{a[\"x\"]=5;a[\"x\"]+=rhs(a);print a[\"x\"],length(a)}"], [])
            .WithStdOut("8 1\n")
            .WithNondeterminismNote("Lokad preserves its documented prior-value-before-RHS order; GNU awk 5.3.2 produced 23 1.")
            .Build(),
        AwkGoldenCaseBuilder.Create("array-cell-update-reacquires-replaced-table",
            ["function rhs(a){split(\"10 20\",a);return 3} BEGIN{a[1]=5;a[1]+=rhs(a);print a[1],a[2],length(a)}"], [])
            .WithStdOut("8 20 2\n")
            .WithNondeterminismNote("Lokad preserves its documented prior-value-before-RHS order; GNU awk 5.3.2 produced 13 20 2.")
            .Build(),
        Passing("array-cell-update-survives-growth-and-keeps-key-spelling",
            ["function rhs(a,i){for(i=1;i<=200;i++)a[i]=i;return 3} {a[$1]=5;a[$1]+=rhs(a);print a[\"0007\"],a[7],length(a)}"], [], "8 7 201\n", "0007\n"),
        AwkGoldenCaseBuilder.Create("composite-key-missing-read-before-rhs",
            ["function rhs(p){print ((\"x\",\"y\") in p);return 3} BEGIN{a[\"other\"]=1;a[\"x\",\"y\"]+=rhs(a);print a[\"x\",\"y\"],length(a)}"], [])
            .WithStdOut("1\n3 2\n")
            .WithNondeterminismNote("Lokad preserves the qualified missing-read insertion before the RHS; GNU awk 5.3.2 observes the missing key during the RHS.")
            .Build(),
        Passing("composite-key-missing-membership-and-update",
            ["BEGIN{print ((\"x\",\"y\") in a),length(a);a[\"x\",\"y\"]++;print ((\"x\",\"y\") in a),length(a),a[\"x\",\"y\"]}"],
            [], "0 0\n1 1 1\n"),
        Passing("composite-key-special-array-alias",
            ["function inc(p){p[\"\",1]++} BEGIN{SUBSEP=\"\";inc(ARGV);print ARGV[1],ARGC}", "7"], [], "8 2\n"),
        AwkGoldenCaseBuilder.Create("composite-key-clear-during-rhs",
            ["function refill(p){delete p;p[\"keep\"]=9;return 3} BEGIN{a[\"x\",\"y\"]=5;a[\"x\",\"y\"]+=refill(a);print a[\"x\",\"y\"],a[\"keep\"],length(a)}"], [])
            .WithStdOut("8 9 2\n")
            .WithNondeterminismNote("Lokad captures the key and prior value before the RHS, preserving its qualified operand order; GNU awk 5.3.2 produced 3 9 2\\n after refill.")
            .Build(),
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
        Passing("array-identity-flat-nested-postfix-results-outlive-update",
            ["BEGIN{a[\"k\"]=\"0007\";b[\"r\"][\"c\"]=\"0007\";print a[\"k\"]++,b[\"r\"][\"c\"]++;print --a[\"k\"],--b[\"r\"][\"c\"];print (a[\"k\"]--)+(b[\"r\"][\"c\"]--),a[\"k\"],b[\"r\"][\"c\"];print a[\"new\"]++,b[\"new\"][\"c\"]++,length(a),length(b)}"],
            [], "7 7\n7 7\n14 6 6\n0 0 2 2\n"),
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

