namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Small canonical controls for the generated, scaled reference benchmark fixtures.
    private static IEnumerable<AwkGoldenCase> BenchmarkCoreCases()
    {
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
