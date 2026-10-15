namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldValueComparisonCases()
    {
        yield return Passing("field-value-comparison-all-operators",
            ["BEGIN{$0=\"alpha tail\";s=\"alpha\";print (s==$1),(s!=$1),(\"a\"<$1),(s<=$1),(\"z\">$1),(s>=$1)}"],
            [], "1 0 1 1 1 1\n");
        yield return Passing("field-value-comparison-input-classification",
            ["{s=\"007\";print s==$1,\"7\"==$1,7==$1,(8>$1);print typeof($1),$1}"],
            [], "1 0 1 1\nstrnum 007\n", "007 tail\n");
        yield return Passing("field-value-comparison-effect-order",
            ["function left(){a++;return $1} function idx(){b++;$0=\"after before\";return 2} BEGIN{$0=\"before tail\";print left()==$(idx()),a,b,$0}"],
            [], "1 1 1 after before\n");
        yield return Passing("field-value-comparison-array-alias-and-retention",
            ["function same(a){return a[\"k\"]==$2} BEGIN{a[\"k\"]=\"old\";$0=\"k old\";print same(a);$0=\"k new\";print same(a),a[\"k\"];a[\"k\"]=7;$0=\"k 007\";print same(a)}"],
            [], "1\n0 old\n1\n");
        yield return Passing("field-value-comparison-mutation-and-empty-fields",
            ["BEGIN{s=\"0007\";$0=\"0007 tail\";v=$1;print s==$1;$1=7;print s==$1,7==$1;$1=s;print s==$1;NF=4;print \"\"==$3,\"\"==$5;FS=\":\";$0=\"a:0007\";print s==$2}"],
            [], "1\n0 1\n1\n1 1\n1\n");
        yield return Passing("field-value-comparison-record-zero-and-formatting",
            ["BEGIN{$0=\"alpha tail\";s=$0;print s==$0;$1=1.25;CONVFMT=\"%.4f\";print \"1.2500\"==$1;print \"1.2500 tail\"==$0}"],
            [], "1\n1\n1\n");
        yield return Passing("field-value-comparison-unicode",
            ["BEGIN{$0=\"café 😀\";s=\"café\";print s==$1,\"😀\"==$2,\"é\"<$2}"],
            [], "1 1 1\n");
        yield return Passing("field-value-comparison-byte-mode",
            ["-b", "BEGIN{$0=\"café 😀\";s=\"café\";print s==$1,\"😀\"==$2,\"é\"<$2}"],
            [], "1 1 1\n");
        yield return Passing("field-value-comparison-ignorecase-and-short-circuit",
            ["BEGIN{$0=\"Alpha tail\";s=\"alpha\";IGNORECASE=1;print s==$1;IGNORECASE=0;print s==$1;print (s==$1&&++n),n+0;$0=\"alpha tail\";print (s==$1&&++n),n}"],
            [], "1\n0\n0 0\n1 1\n");
        const string classification = "{s=\"0x10\";print s==$1,16==$1}";
        yield return Passing("field-value-comparison-decimal", [classification], [], "1 0\n", "0x10 tail\n");
        yield return Passing("field-value-comparison-posix", ["--posix", classification], [], "1 1\n", "0x10 tail\n");
        yield return Passing("field-value-comparison-traditional", ["--traditional", classification], [], "1 0\n", "0x10 tail\n");
        yield return Passing("field-value-comparison-nondecimal", ["--non-decimal-data", classification], [], "1 1\n", "0x10 tail\n");
    }
}
