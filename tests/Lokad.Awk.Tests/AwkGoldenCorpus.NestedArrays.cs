namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU awk 5.3.2, source 027ebff5. Exact native receipts are retained locally.
    private static IEnumerable<AwkGoldenCase> NestedArrayCases() =>
    [
        AwkGoldenCaseBuilder.Create("nested-array-scalar-conflict", ["BEGIN{a[1]=7;print \"before\";a[1][2]=3;print \"after\"}"], [])
            .WithStdOut("before\n").WithStdErr("awk: attempt to use scalar `array element' as an array\n").ExitsWith(2).Build(),
        AwkGoldenCaseBuilder.Create("nested-array-scalar-copy-rejects-array", ["BEGIN{a[1][2]=3;print \"before\";x=a[1];print \"after\"}"], [])
            .WithStdOut("before\n").WithStdErr("awk: attempt to use array `array element' in a scalar context\n").ExitsWith(2).Build(),
        AwkGoldenCaseBuilder.Create("nested-array-scalar-return-rejects-array", ["function f(){return a[1]} BEGIN{a[1][2]=3;print \"before\";f();print \"after\"}"], [])
            .WithStdOut("before\n").WithStdErr("awk: attempt to use array `array element' in a scalar context\n").ExitsWith(2).Build(),
        AwkGoldenCaseBuilder.Create("nested-array-match-scalar-target", ["BEGIN{a[1]=7;print \"before\";match(\"x\",/x/,a[1]);print \"after\"}"], [])
            .WithStdOut("before\n").WithStdErr("awk: attempt to use scalar `array element' as an array\n").ExitsWith(2).Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sort-ancestor-overlap", ["BEGIN{a[1][1]=\"b\";a[1][2]=\"a\";print \"before\";asort(a[1],a);print \"after\"}"], [])
            .WithStdOut("before\n").WithStdErr("awk: array arguments must not contain one another\n").ExitsWith(2).Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-ancestor-overlap", ["BEGIN{a[0][0]=1;print \"before\";split(\"a:b\",a,\":\",a[0]);print \"after\"}"], [])
            .WithStdOut("before\n").WithStdErr("awk: array arguments must not contain one another\n").ExitsWith(2).Build(),
        AwkGoldenCaseBuilder.Create("nested-array-getline-child", ["BEGIN{code=(getline a[1][2]);print code,a[1][2],NR,isarray(a[1])}"], [])
            .WithStandardInput("one\n").WithStdOut("1 one 1 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-getline-child-eof", ["BEGIN{code=(getline a[1][2]);print code,length(a),isarray(a[1]),length(a[1])}"], [])
            .WithStdOut("0 1 1 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-getline-deep-index-once", ["function k(){return ++n} BEGIN{code=(getline a[k()][k()][k()]);print code,n,a[1][2][3]}"], [])
            .WithStandardInput("one\n").WithStdOut("1 3 one\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-match-child", ["BEGIN{print match(\"abcd\",/(ab)(cd)/,a[1]);print a[1][0],a[1][1],a[1][2],a[1][2,\"start\"],a[1][2,\"length\"]}"], [])
            .WithStdOut("1\nabcd ab cd 3 2\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-match-no-match-child", ["BEGIN{a[1][9]=7;print match(\"no\",/yes/,a[1]);print isarray(a[1]),length(a[1]),RSTART,RLENGTH}"], [])
            .WithStdOut("0\n1 0 0 -1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-match-deep-index-once", ["function k(){return ++n} BEGIN{print match(\"abcd\",/(ab)(cd)/,a[k()][k()]),n;print a[1][2][1]}"], [])
            .WithStdOut("1 2\nab\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-match-argument-order", ["function v(){t=t \"v\";return \"ab\"} function p(){t=t \"p\";return \"(ab)\"} function k(){t=t \"k\";return 1} BEGIN{print match(v(),p(),a[k()]),t,a[1][1]}"], [])
            .WithStdOut("1 vpk ab\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sub-child", ["BEGIN{a[1][2]=\"abcd\";print sub(/bc/,\"X\",a[1][2]),a[1][2];print gsub(/a/,\"Y\",a[1][2]),a[1][2]}"], [])
            .WithStdOut("1 aXd\n1 YXd\n").Build(),
        // As with flat getline targets, Lokad writes the captured logical path after
        // BEGINFILE changes it; GNU drops this write into the detached child instead.
        AwkGoldenCaseBuilder.Create("nested-array-getline-rebuilds-captured-path", ["BEGIN{s=getline a[\"r\"][\"c\"];print s,a[\"r\"][\"c\"]} BEGINFILE{delete a}"], [])
            .WithStandardInput("one\n").WithStdOut("1 one\n").Build(),
        // Lokad keeps the evaluated LHS/prior value before the compound RHS. GNU's
        // RHS-first result, and native crashes on index deletion, are not the contract here.
        AwkGoldenCaseBuilder.Create("nested-array-compound-parent-clear", ["function wipe(){delete a;return 2} BEGIN{a[\"r\"][\"c\"]=7;a[\"r\"][\"c\"]+=wipe();print a[\"r\"][\"c\"],length(a)}"], [])
            .WithStdOut("9 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-compound-child-replacement", ["function wipe(){delete a[\"r\"];a[\"r\"][\"other\"]=4;return 2} BEGIN{a[\"r\"][\"c\"]=7;a[\"r\"][\"c\"]+=wipe();print a[\"r\"][\"c\"],a[\"r\"][\"other\"]}"], [])
            .WithStdOut("9 4\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-final-index-parent-clear", ["function idx(){n++;delete a;return \"c\"} BEGIN{a[\"r\"][\"old\"]=3;a[\"r\"][idx()]=7;print a[\"r\"][\"c\"],length(a),n}"], [])
            .WithStdOut("7 1 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-deep-index-parent-clear", ["function idx(){n++;delete a[\"r\"];return \"c\"} BEGIN{a[\"r\"][\"s\"][\"old\"]=3;print a[\"r\"][\"s\"][idx()]++;print a[\"r\"][\"s\"][\"c\"],n}"], [])
            .WithStdOut("0\n1 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-compound-captured-keys", ["function wipe(){delete a;i=\"new\";j=\"new\";return 2} BEGIN{i=\"r\";j=\"c\";a[i][j]=7;a[i][j]+=wipe();print a[\"r\"][\"c\"],length(a)}"], [])
            .WithStdOut("9 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-compound-repaired-parameter-root", ["function wipe(){delete a;return 2} function f(p){p[\"s\"][\"c\"]+=wipe();print p[\"s\"][\"c\"],length(p)} BEGIN{a[\"r\"][\"s\"][\"c\"]=7;f(a[\"r\"]);print length(a)}"], [])
            .WithStdOut("9 1\n0\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-read-after-index-parent-clear", ["function idx(){delete a;return \"c\"} BEGIN{a[\"r\"][\"c\"]=7;print a[\"r\"][idx()]+0,isarray(a[\"r\"])}"], [])
            .WithStdOut("0 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-nested-counts", ["{counts[$1][$2]+=$3} END{for(r in counts)for(s in counts[r])total+=counts[r][s];print total,length(counts),length(counts[\"EU\"])}"], [])
            .WithStandardInput("EU A 2\nEU B 3\nUS A 7\n").WithStdOut("12 2 2\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-deep-chain", ["BEGIN{a[1][2][3]=7;print a[1][2][3],isarray(a[1]),isarray(a[1][2]),length(a)}"], [])
            .WithStandardInput("").WithStdOut("7 1 1 1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-composite-distinct", ["BEGIN{a[1,2]=5;a[1][2]=7;print a[1,2],a[1][2],length(a)}"], [])
            .WithStandardInput("").WithStdOut("5 7 2\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-delete-child", ["BEGIN{a[1][2]=3;delete a[1];print length(a);a[1][4]=5;print a[1][4],length(a[1])}"], [])
            .WithStandardInput("").WithStdOut("0\n5 1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-clear-child-alias", ["function clear(p){delete p;p[9]=4} BEGIN{a[1][2]=3;clear(a[1]);print a[1][9],length(a[1])}"], [])
            .WithStandardInput("").WithStdOut("4 1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-delete-parent-element-during-alias", ["function edit(p){delete a[1];p[2]=7;print p[2],length(p)} BEGIN{a[1][2]=3;edit(a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("7 1\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-clear-parent-during-alias", ["function edit(p){delete a;p[2]=7;print p[2],length(p)} BEGIN{a[1][2]=3;edit(a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("7 1\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-forwarded-child-alias", ["function g(q){q[\"x\"]++} function f(p){g(p)} BEGIN{a[1][\"x\"]=7;f(a[1]);print a[1][\"x\"]}"], [])
            .WithStandardInput("").WithStdOut("8\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-parent-alias", ["function f(p){p[1][2]++;return isarray(p[1])} BEGIN{a[1][2]=3;print f(a),a[1][2]}"], [])
            .WithStandardInput("").WithStdOut("1 4\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-child", ["BEGIN{split(\"one two\",a[1]);print a[1][1],length(a[1]),isarray(a[1])}"], [])
            .WithStandardInput("").WithStdOut("one 2 1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-parent", ["BEGIN{a[1][2]=3;split(\"one two\",a);print a[1],a[2],isarray(a[1])}"], [])
            .WithStandardInput("").WithStdOut("one two 0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-length-missing", ["BEGIN{print length(a[1]);print length(a),isarray(a[1])}"], [])
            .WithStandardInput("").WithStdOut("0\n1 0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-membership-promotes-child", ["BEGIN{print (\"k\" in a[1]);print isarray(a[1]),length(a)}"], [])
            .WithStandardInput("").WithStdOut("0\n1 1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-isarray-missing", ["BEGIN{a[9][0]=1;print isarray(a[1]),(1 in a),length(a)}"], [])
            .WithStandardInput("").WithStdOut("0 1 2\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-missing-child-argument", ["function f(p){print isarray(p);p[\"x\"]=1} BEGIN{f(a[1]);print isarray(a[1]),a[1][\"x\"]}"], [])
            .WithStandardInput("").WithStdOut("0\n1 1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-csv-groups", ["--csv", "NR>1{counts[$1][$2]+=$3} END{for(r in counts)for(s in counts[r])total+=counts[r][s];print total,length(counts),counts[\"EU\"][\"A,B\"]}"], [])
            .WithStandardInput("region,sku,qty\r\nEU,\"A,B\",2\r\nUS,A,3\r\nEU,\"A,B\",7\r\n").WithStdOut("12 2 9\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-removed-child-type", ["function f(p){delete a[1];print isarray(p),typeof(p);print (\"old\" in p),p[\"old\"],length(p);p[\"new\"]=7} BEGIN{a[1][\"old\"]=3;f(a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("1 array\n0  1\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-cleared-parent-type", ["function f(p){delete a;print isarray(p),typeof(p);print (\"old\" in p),p[\"old\"],length(p);p[\"new\"]=7} BEGIN{a[1][\"old\"]=3;f(a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("1 array\n0  1\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-duplicate-child-alias", ["function f(p,q){delete p;print isarray(q),length(q);q[\"new\"]=7;print p[\"new\"],length(p)} BEGIN{a[1][\"old\"]=3;f(a[1],a[1]);print a[1][\"new\"]}"], [])
            .WithStandardInput("").WithStdOut("1 0\n7 1\n7\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-parent-and-child-alias", ["function f(p,q){delete p;print isarray(q),typeof(q);q[\"new\"]=7;print length(q),length(p)} BEGIN{a[1][\"old\"]=3;f(a,a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("1 array\n1 0\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-array-sorted-values", ["BEGIN{a[1][1]=\"b\";a[2][1]=\"a\";print asort(a,b);print isarray(b[1]),isarray(b[2])}"], [])
            .WithStandardInput("").WithStdOut("2\n1 1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-nested-missing-alias-detach", ["function f(p){delete a[1];print isarray(p),typeof(p);p[\"new\"]=7;print p[\"new\"]} BEGIN{f(a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("0 untyped\n7\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-duplicate-removed-child", ["function f(p,q){delete a[1];p[\"new\"]=7;print (\"new\" in q),length(q),p[\"new\"]} BEGIN{a[1][\"old\"]=3;f(a[1],a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("0 0 7\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-duplicate-cleared-parent", ["function f(p,q){delete a;p[\"new\"]=7;print (\"new\" in q),length(q),p[\"new\"]} BEGIN{a[1][\"old\"]=3;f(a[1],a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("0 0 7\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-two-missing-child-aliases", ["function f(p,q){p[\"new\"]=7;print isarray(q),q[\"new\"]} BEGIN{f(a[1],a[1]);print a[1][\"new\"]}"], [])
            .WithStandardInput("").WithStdOut("1 7\n7\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-missing-aliases-detach", ["function f(p,q){delete a[1];p[\"new\"]=7;print isarray(q),typeof(q)} BEGIN{f(a[1],a[1]);print length(a)}"], [])
            .WithStandardInput("").WithStdOut("0 untyped\n0\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sorted-children-copy", ["BEGIN{a[1][\"v\"]=3;a[2][\"v\"]=5;asort(a,b);b[1][\"v\"]=9;print a[1][\"v\"],a[2][\"v\"],b[1][\"v\"]}"], [])
            .WithStandardInput("").WithStdOut("3 5 9\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-nested-index-creation-order", ["function one(){print \"one\",(1 in a);return 1} function two(){print \"two\",(1 in a);return 2} BEGIN{a[0]=0;a[one()][two()]=3;print a[1][2]}"], [])
            .WithStandardInput("").WithStdOut("one 0\ntwo 1\n3\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-nested-index-array-type", ["function two(){print isarray(a[1]);return 2} BEGIN{a[0]=0;a[1][two()]=3;print isarray(a[1]),a[1][2]}"], [])
            .WithStandardInput("").WithStdOut("1\n1 3\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-nested-read-index-order", ["function two(){print (1 in a);return 2} BEGIN{a[0]=0;print a[1][two()];print isarray(a[1])}"], [])
            .WithStandardInput("").WithStdOut("1\n\n1\n").WithStdErr("")
            .Build(),
        AwkGoldenCaseBuilder.Create("nested-array-first-index-untyped-root", ["function idx(){print isarray(a);return 1} BEGIN{a[idx()][2]=3;print isarray(a),a[1][2]}"], [])
            .WithStdOut("0\n1 3\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-first-index-root-origin", ["function idx(){print typeof(a);return 1} BEGIN{a[idx()][2]=3;print typeof(a)}"], [])
            .WithStdOut("untyped\narray\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-overwrite-untyped-array-parameter", ["function f(p){a[1]=7;print typeof(p),isarray(p);p[\"new\"]=3;print p[\"new\"]} BEGIN{f(a[1]);print a[1]}"], [])
            .WithStdOut("untyped 0\n3\n7\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-overwrite-untyped-alias-with-untyped-value", ["function f(p){a[1]=z;print typeof(p),isarray(p);p[\"new\"]=3} BEGIN{f(a[1]);print typeof(a[1])}"], [])
            .WithStdOut("untyped 0\nunassigned\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-missing-child-parameter", ["function f(p){print isarray(p);split(\"one two\",p);print isarray(p),length(p)} BEGIN{f(a[1]);print a[1][1],length(a)}"], [])
            .WithStdOut("0\n1 2\none 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-child-argument-order", ["function sep(){print isarray(a[1]);return \":\"} BEGIN{split(\"a:b\",a[1],sep());print a[1][1]}"], [])
            .WithStdOut("0\na\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-root-argument-order", ["function sep(){print isarray(a);return \":\"} BEGIN{split(\"a:b\",a,sep());print a[1]}"], [])
            .WithStdOut("0\na\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sort-root-argument-order", ["function order(){print isarray(a),isarray(b);return \"@ind_str_asc\"} BEGIN{print asort(a,b,order());print length(a),length(b)}"], [])
            .WithStdOut("0 0\n0\n0 0\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-fourth-argument-order", ["function dest(){print isarray(a[1]),length(a[1]);return 2} BEGIN{split(\"a:b\",a[1],\":\",b[dest()]);print a[1][1],length(b[2])}"], [])
            .WithStdOut("0 0\na 1\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-split-child-entry-before-separator", ["function sep(){print (1 in a),isarray(a);return \":\"} BEGIN{split(\"a:b\",a[1],sep());print a[1][1]}"], [])
            .WithStdOut("1 1\na\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-array-sort-val-type-asc", ["BEGIN{a[1][\"id\"]=\"array\";a[2]=\"s\";a[3]=7;n=asort(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)if(isarray(b[i]))print \"array\";else print b[i]}"], [])
            .WithStdOut("7\ns\narray\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-array-sort-val-type-desc", ["BEGIN{a[1][\"id\"]=\"array\";a[2]=\"s\";a[3]=7;n=asort(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)if(isarray(b[i]))print \"array\";else print b[i]}"], [])
            .WithStdOut("array\ns\n7\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-array-sort-val-str-asc", ["BEGIN{a[1][\"id\"]=\"array\";a[2]=\"s\";a[3]=7;n=asort(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)if(isarray(b[i]))print \"array\";else print b[i]}"], [])
            .WithStdOut("7\ns\narray\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-array-sort-val-str-desc", ["BEGIN{a[1][\"id\"]=\"array\";a[2]=\"s\";a[3]=7;n=asort(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)if(isarray(b[i]))print \"array\";else print b[i]}"], [])
            .WithStdOut("array\ns\n7\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-array-sort-val-num-asc", ["BEGIN{a[1][\"id\"]=\"array\";a[2]=\"s\";a[3]=7;n=asort(a,b,\"@val_num_asc\");for(i=1;i<=n;i++)if(isarray(b[i]))print \"array\";else print b[i]}"], [])
            .WithStdOut("s\n7\narray\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-array-sort-val-num-desc", ["BEGIN{a[1][\"id\"]=\"array\";a[2]=\"s\";a[3]=7;n=asort(a,b,\"@val_num_desc\");for(i=1;i<=n;i++)if(isarray(b[i]))print \"array\";else print b[i]}"], [])
            .WithStdOut("array\n7\ns\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-detached-parameter-length", ["function f(p){delete a[1];print isarray(p),length(p)} BEGIN{a[1][2]=3;f(a[1]);print length(a)}"], [])
            .WithStdOut("1 0\n0\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-iterate-child-parameter", ["function sum(p,k,n){for(k in p)n+=p[k];return n} BEGIN{a[1][\"x\"]=2;a[1][\"y\"]=3;print sum(a[1])}"], [])
            .WithStdOut("5\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sorted-child-iteration", ["BEGIN{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";a[1][\"b\"]=2;a[1][\"a\"]=3;for(k in a[1])print k,a[1][k]}"], [])
            .WithStdOut("a 3\nb 2\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sorted-child-parameter", ["function f(p,k){for(k in p)print k,p[k]} BEGIN{PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";a[1][\"b\"]=2;a[1][\"a\"]=3;f(a[1])}"], [])
            .WithStdOut("a 3\nb 2\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-delete-leaf", ["BEGIN{a[1][2]=3;delete a[1][2];print isarray(a[1]),length(a[1])}"], [])
            .WithStdOut("1 0\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-delete-missing-path", ["BEGIN{delete a[1][2];print (1 in a),isarray(a[1]),length(a[1])}"], [])
            .WithStdOut("0 0 0\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sort-child-targets", ["BEGIN{a[1][3]=\"b\";a[1][9]=\"a\";print asort(a[1],b[2]);print b[2][1],b[2][2],length(a[1])}"], [])
            .WithStdOut("2\na b 2\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sort-keys-child-targets", ["BEGIN{a[1][\"b\"]=2;a[1][\"a\"]=3;print asorti(a[1],b[2]);print b[2][1],b[2][2],length(a[1])}"], [])
            .WithStdOut("2\na b 2\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-sort-in-place-index-once", ["function idx(){calls++;return 1} BEGIN{a[1][3]=\"b\";a[1][9]=\"a\";print asort(a[idx()]),calls;print a[1][1],a[1][2]}"], [])
            .WithStdOut("2 1\na b\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-symbol-table-nested-prefix", ["function f(source){SYMTAB[\"source\"][1][2]=9;print SYMTAB[\"source\"][1][2]} BEGIN{source[1][2]=3;f(7);print source[1][2]}"], [])
            .WithStdOut("9\n9\n").Build(),
        AwkGoldenCaseBuilder.Create("nested-array-symbol-table-array-target-shadow", ["function f(source){split(\"x:y\",SYMTAB[\"source\"],\":\");print SYMTAB[\"source\"][1]} BEGIN{source[9]=3;f(7);print source[2]}"], [])
            .WithStdOut("x\ny\n").Build(),
        CompilerDiagnostic(AwkGoldenCaseBuilder.Create("nested-array-posix-rejects", ["--posix", "BEGIN{a[1][2]=3}"], [])
            .ExitsWith(2).WithStdErr("awk: arrays of arrays are a GNU extension\n").WithTags([AwkGoldenTag.Posix]).Build()),
        CompilerDiagnostic(AwkGoldenCaseBuilder.Create("nested-array-traditional-rejects", ["--traditional", "BEGIN{a[1][2]=3}"], [])
            .ExitsWith(2).WithStdErr("awk: arrays of arrays are a GNU extension\n").Build())
    ];
}
