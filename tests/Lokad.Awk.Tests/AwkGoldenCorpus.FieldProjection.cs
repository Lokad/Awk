namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> FieldProjectionCases()
    {
        var tail = string.Join(" ", Enumerable.Repeat("last", 61));
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
