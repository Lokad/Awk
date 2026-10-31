namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldProjectionCases()
    {
        yield return Passing("record-print-dirty-text-keeps-separator-and-owned-escape",
            ["BEGIN{OFS=\"|\";ORS=\"!\"} {$2=\"É\";print;OFS=\":\";print $0;saved=$0;$1=\"new\";print;print saved}"], [],
            "café|É|0007!café|É|0007!new:É:0007!café|É|0007!", "café old 0007\n");
        yield return Passing("record-print-dirty-text-keeps-nf-cache-and-next-record",
            ["{n=$1+0;NF=3;print;print typeof($1),$1;NF=0;print;NF=2;$2=\"last\";print}"], [],
            "0007 old \nstrnum 0007\n\n last\ncafé tail \nstring café\n\n last\n", "0007 old\ncafé tail\n");
        yield return Passing("record-print-numeric-field-freezes-convfmt-before-later-print",
            ["BEGIN{CONVFMT=\"%.2f\";$0=\"a old\";$2=1.2345;print;CONVFMT=\"%.4f\";print $0;$1=\"b\";print}"], [],
            "a 1.23\na 1.23\nb 1.2345\n");
        yield return PassingWithExpectedFiles("record-print-dirty-text-before-getline-and-redirected-escape",
            ["{$2=\"É\";print;saved=$0;print $0 > \"/out.txt\";close(\"/out.txt\");getline;print saved,$0;exit}", "/input.txt"],
            [new("/input.txt", "café old tail\nnext fresh record\n")],
            "café É tail\ncafé É tail next fresh record\n", [new("/out.txt", "café É tail\n")]);
        yield return Passing("field-copy-keeps-input-type-and-source-snapshot",
            ["{$2=$1;saved=$2;$1=\"changed\";print typeof($2),saved,$0}"], [],
            "strnum 0007 changed 0007 tail\nstring café changed café last\n", "0007 old tail\ncafé old last\n");
        yield return Passing("field-copy-value-survives-getline",
            ["{$2=$1;saved=$2;getline;print saved,$0;exit}"], [],
            "café next fresh record\n", "café old tail\nnext fresh record\n");
        yield return Passing("field-copy-returned-value-survives-record-replacement",
            ["function keep(x){return x} {$2=$1;saved=keep($2);$0=\"later\";print saved,typeof(saved)}"], [],
            "0007 strnum\ncafé string\n", "0007 old tail\ncafé old last\n");
        yield return Passing("field-copy-keeps-cached-and-numeric-assignment-values",
            ["{saved=$2;$2=$1;print $0;$1=1.25;$2=$1;print typeof($2),$0}"], [],
            "a a tail\nnumber 1.25 1.25 tail\n", "a old tail\n");
        yield return Passing("field-copy-assignment-result-stays-owned",
            ["{$2=$1;saved=($3=$1);$0=\"later\";print saved;exit}"], [], "café\n", "café old tail\n");
        yield return Passing("field-copy-self-assignment-rebuilds-with-ofs",
            ["BEGIN{OFS=\"|\"} {$1=$1;$1=$1;print NF,$0}"], [], "3|a|b|c\n", "a\tb\tc\n");
        yield return Passing("field-copy-missing-source-and-new-destination",
            ["{$2=$9;print \"[\"$2\"]\",NF;$9=$1;print NF,$1,$9}"], [], "[] 3\n9 a a\n", "a old tail\n");
        yield return Passing("field-copy-empty-destination-keeps-literal-splitting",
            ["-F:", "{$2=$1;print}"], [], "a a c\n", "a::c\n");
        // Numeric $0 follows OFMT, as in mawk; GNU's cached CONVFMT spelling differs.
        yield return Passing("record-print-keeps-format-spelling-and-empty-record",
            ["BEGIN{OFMT=\"%.2f\";CONVFMT=\"%.3f\";ORS=\"|\";$0=1.2345;print;print $0;$0=\"0007\";print;print $0;$0=\"café old\";$2=1.2345;print;print $0;NF=0;print;print $0}"],
            [], "1.23|1.23|0007|0007|café 1.234|café 1.234|||", "");
        yield return Passing("field-capacity-growth-shrink-and-recomposition",
            ["{NF=65;print NF,$1,$64,\"[\"$65\"]\";NF=1;print NF,$0;$129=\"z\";print NF,$1,$129}"],
            [], "65 x x []\n1 x\n129 x z\n", string.Join(' ', Enumerable.Repeat("x", 64)) + "\n");
        var tail = string.Join(" ", Enumerable.Repeat("last", 61));
        yield return Passing("owned-field-prefix-keeps-fs-cached-spelling-and-recomposition",
            ["BEGIN{OFS=\"|\"} {x=$1;FS=\",\";y=$3;print x,y;print NF,$1,$3;NF=3;print $0}"],
            [], "0007|café\n64|0007|café\n0007|worker|café\n", "0007 worker café " + tail + "\n");
        yield return Passing("owned-literal-field-prefix-keeps-fs-and-absent-fields",
            ["BEGIN{FS=\"|\"} {x=$1;FS=\" \";printf \"%s:%s:%s\\n\",x,$3,$65;NF=2;print NF,$0}"],
            [], "head:tail:\n2 head middle\n", "head|middle|tail|" + tail.Replace(' ', '|') + "\n");
        yield return Passing("field-prefix-captured-separator-nf-and-mutation",
            ["BEGIN{OFS=\"|\"} {print $1;FS=\",\";print $3,NF;$2=\"changed\";print NF,$1,$2,$3}"],
            [], "a\nc|64\n64|a|changed|c\n", "a b c " + tail + "\n");
        yield return Passing("field-prefix-literal-plan-survives-fs-change",
            ["BEGIN{FS=\"|\"} {print $1;FS=\" \";print $3,NF,$64}"],
            [], "head\ntail 64 last\n", "head|middle|tail|" + tail.Replace(' ', '|') + "\n");
        yield return Passing("field-prefix-cached-strnum-survives-nf-and-recomposition",
            ["{x=$1;print typeof($1),x;print NF,$1;NF=3;print $0}"],
            [], "strnum 0007\n64 0007\n0007 worker data\n", "0007 worker data " + tail + "\n");
        yield return Passing("field-projection-index-effects-and-input-spelling",
            ["BEGIN{$0=\"0007 alpha\";i=1;print $(i++);print $i;print i,$1,typeof($1);print $3}"],
            [], "0007\nalpha\n2 0007 strnum\n\n");
        yield return Passing("field-projection-unicode-and-numeric-print-format",
            ["BEGIN{$0=\"café 😀\";print $1;print $2;OFMT=\"%.2f\";CONVFMT=\"%.4f\";$1=1.25;print $1;print $0;ORS=\"!\";print $2}"],
            [], "café\n😀\n1.25\n1.2500 😀\n😀!");
        yield return Passing("field-projection-function-index-replaces-record",
            ["function change(){$0=\"new next\";return 2} BEGIN{$0=\"old\";print $(change());print $0}"],
            [], "next\nnew next\n");
    }
}
