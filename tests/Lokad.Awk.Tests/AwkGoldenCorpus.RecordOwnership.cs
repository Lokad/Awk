namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RecordOwnershipCases()
    {
        yield return Passing("sub-replacement-formats-after-target-effects",
            ["function key(){replacement=.5;CONVFMT=\"%.1f\";return \"k\"}BEGIN{CONVFMT=\"%.3f\";replacement=.125;a[\"k\"]=\"x\";print sub(/x/,replacement,a[key()]),a[\"k\"],replacement}"], [],
            "1 0.1 0.5\n");
        yield return Passing("gsub-replacement-formats-after-target-effects",
            ["function key(){replacement=.5;CONVFMT=\"%.1f\";return \"k\"}BEGIN{CONVFMT=\"%.3f\";replacement=.125;a[\"k\"]=\"xx\";print gsub(/x/,replacement,a[key()]),a[\"k\"],replacement}"], [],
            "2 0.10.1 0.5\n");
        yield return Passing("sub-target-case-effects-precede-regex-consumption",
            ["function key(){IGNORECASE=1;return \"k\"}BEGIN{a[\"k\"]=\"A\";print sub(/a/,\"x\",a[key()]),a[\"k\"]}"], [],
            "1 x\n");
        yield return Failing("sub-invalid-dynamic-pattern-follows-target-effects",
            ["function key(){print \"target-evaluated\";return \"k\"}BEGIN{pattern=\"[\";sub(pattern,\"x\",a[key()]);print \"unreachable\"}"], [],
            2, "target-evaluated\n", "awk: invalid regular expression\n");
        yield return Passing("match-numeric-input-formats-after-pattern-effects",
            ["function pattern(){CONVFMT=\"%.1f\";return \"^0[.]125$\"}BEGIN{CONVFMT=\"%.3f\";print match(.125,pattern()),RSTART,RLENGTH,CONVFMT}"], [],
            "0 0 -1 %.1f\n");
        yield return Passing("match-captures-numeric-value-before-pattern-effects",
            ["function pattern(){x=.5;CONVFMT=\"%.1f\";return \"^0[.]1$\"}BEGIN{CONVFMT=\"%.3f\";x=.125;print match(x,pattern()),RSTART,RLENGTH,x}"], [],
            "1 1 3 0.5\n");
        yield return Passing("match-pattern-formats-before-capture-target-effects",
            ["function key(){CONVFMT=\"%.1f\";return \"r\"}BEGIN{CONVFMT=\"%.3f\";print match(\"0.125\",.125,caps[key()]),RSTART,RLENGTH,caps[\"r\"][0]}"], [],
            "1 1 5 0.125\n");
        yield return Passing("split-numeric-input-formats-after-pattern-effects",
            ["function pattern(){CONVFMT=\"%.1f\";return \":\"}BEGIN{CONVFMT=\"%.3f\";print split(.125,a,pattern()),a[1]}"], [],
            "1 0.1\n");
        yield return Passing("split-pattern-formats-before-separator-target-effects",
            ["function key(){CONVFMT=\"%.1f\";return \"r\"}BEGIN{CONVFMT=\"%.3f\";print split(\"A0.125B\",a,.125,seps[key()]),a[1],a[2],seps[\"r\"][1]}"], [],
            "2 A B 0.125\n");
        yield return Passing("patsplit-numeric-input-formats-after-pattern-effects",
            ["function pattern(){CONVFMT=\"%.1f\";cache=x\"\";return \"[0-9]+\"}BEGIN{CONVFMT=\"%.3f\";x=.125;print patsplit(x,a,pattern()),a[1],a[2],CONVFMT}"], [],
            "2 0 1 %.1f\n");
        yield return Passing("print-values-format-after-later-operand-effects",
            ["function change(){OFS=\"|\";ORS=\"!\\n\";OFMT=\"%.3f\";$0=\"new row\";return 2.5}{print $1,1.25,change(),$1}"],
            [], "0007|1.250|2.500|new!\n", "0007 old\n");
        yield return Passing("print-values-retain-untyped-conditional-origin-until-formatting",
            ["BEGIN{printf \"%s\\n\",typeof(a[1])}BEGIN{print (1?a[1]:7),0}BEGIN{printf \"%s\\n\",typeof(a[1])}"],
            [], "untyped\n 0\nunassigned\n");
        yield return Passing("print-field-list-owns-values-before-record-reconstruction",
            ["{saved=$1;$2=1.23456;OFMT=\"%.2f\";print $1,$2,$3,$4,$5,$0}"],
            [], "0007 1.23 café tail last 0007 1.23456 café tail last\n", "0007 old café tail last\n");
        yield return Passing("record-regex-immediate-fields-preserve-index-effects",
            ["{i=1;print ($(i++)~/^left$/),i,($2~/^0007$/),typeof($2);$2=12.345;CONVFMT=\"%.2f\";print ($2~/^12[.]35$/);CONVFMT=\"%.3f\";print ($2~/^12[.]345$/)}"], [],
            "1 2 1 strnum\n1\n1\n", "left 0007 tail\n");
        yield return Passing("record-regex-numeric-zero-uses-current-convfmt",
            ["BEGIN{$0=12.345;CONVFMT=\"%.2f\";print ($0~/^12[.]35$/);CONVFMT=\"%.3f\";print ($0~/^12[.]345$/)}"], [],
            "1\n1\n");
        yield return Passing("record-regex-dynamic-rhs-owns-earlier-subject",
            ["function p(){getline;return \"^old 0007$\"}{print (($0\"\")~p()),$0;exit}"], [],
            "1 new 0008\n", "old 0007\nnew 0008\n");
        yield return Passing("record-regex-range-keeps-buffered-subject",
            ["/^start$/,/^end$/{n++}END{print n}"], [],
            "3\n", "skip\nstart\nmiddle\nend\nskip\n");
        yield return Passing("record-regex-unicode-nul-and-mutation",
            ["{print ($0~/é/),($2~/😀/),($3~/^a\\0b$/);$2=\"changed\";print ($0~/changed/),($2!~/😀/)}"], [],
            "1 1 1\n1 1\n", "café 😀 a\0b\n");
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
