namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RecordOwnershipCases()
    {
        yield return Passing("input-record-escapes-survive-buffer-cycles",
            ["NR==1{r=$0;f=$2;a[$1]=$2}END{print r;print f,f+0,typeof(f),a[\"first\"],length(a)}"], [],
            "first 0007\n0007 7 strnum 0007 1\n", "first 0007\nnext 8\nthird 9\nlast 10\n");
        yield return Passing("input-record-getline-destinations-survive-reuse",
            ["{if(NR==1){s=$0;getline x;print $0,x,s;getline;print $0,x,s};print NR,$1}END{print FNR}"], [],
            "first 0007 next 8 first 0007\nthird 9 next 8 first 0007\n3 third\n4 last\n4\n",
            "first 0007\nnext 8\nthird 9\nlast 10\n");
        yield return Passing("input-record-earlier-print-argument-owns-field",
            ["{print $2,($0=\"new row\"),$2}"], [],
            "0007 new row row\n0008 new row row\n0009 new row row\n0010 new row row\n",
            "first 0007\nnext 0008\nthird 0009\nlast 0010\n");
        yield return Passing("input-record-snapshots-separator-before-main-actions",
            ["NR==1{FS=\":\"}{print NF,$1,$2}"], [], "2 one two\n2 three four\n2 last row\n",
            "one two\nthree:four\nlast:row\n");
        yield return Passing("input-record-dirty-fields-survive-buffer-cycles",
            ["BEGIN{OFS=\"|\"}{$3=$2;NF=3;print;if(NR==1)s=$0}END{print s}"], [],
            "café|0007|0007\nnext|0008|0008\nthird|0009|0009\nlast|0010|0010\ncafé|0007|0007\n",
            "café 0007 old\nnext 0008 old\nthird 0009 old\nlast 0010 old\n");
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
