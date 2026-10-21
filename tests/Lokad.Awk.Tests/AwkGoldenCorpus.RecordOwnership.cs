namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RecordOwnershipCases()
    {
        yield return Passing("dirty-text-record-keeps-mutation-ofs",
            ["BEGIN{$0=\"left right\";OFS=\":\";$1=$1;OFS=\"!\";print $0}"], [], "left:right\n");
        yield return Passing("dirty-numeric-record-keeps-mutation-ofs",
            ["BEGIN{$0=\"left right\";OFS=\":\";$1=7;OFS=\"!\";print $0}"], [], "7:right\n");
        yield return Passing("dirty-record-remutation-uses-current-ofs",
            ["BEGIN{$0=\"left right\";OFS=\":\";$1=$1;OFS=\"!\";$2=$2;print $0}"], [], "left!right\n");
        yield return Passing("single-field-mutation-does-not-emit-ofs",
            ["BEGIN{$0=\"left\";OFS=\":\";$1=$1;OFS=\"!\";print $0}"], [], "left\n");
        yield return Passing("shrinking-dirty-record-discards-unused-separator",
            ["BEGIN{$0=\"left right\";OFS=\":\";$1=$1;NF=1;OFS=\"!\";print $0;NF=0;print \"[\"$0\"]\"}"], [], "left\n[]\n");
    }
}
