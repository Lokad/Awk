namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU awk 5.2.1 and 5.4.0 agree on numeric conversion at record reconstruction.
    // Indirect OFS writes follow Lokad's shared-variable contract; their expectations
    // come from the matching direct-write programs run on both GNU references.
    private static IEnumerable<AwkGoldenCase> NumericFieldConversionCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-0-1-deferred",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-0-1-observed",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-0-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-0-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;CONVFMT=\"%.6g\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-0-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.6g\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-1-1-deferred",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-1-1-observed",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-1-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-1-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;CONVFMT=\"%.2f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-1-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.2f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-2-1-deferred",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-2-1-observed",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-2-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-2-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-0-2-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.6g\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-0-1-deferred",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-0-1-observed",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-0-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-0-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.6g\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-0-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.6g\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-1-1-deferred",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-1-1-observed",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-1-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-1-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.2f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-1-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.2f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-deferred",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-printed",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;print $1;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("1.25\n[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-concatenated",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$1 \"\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-observed",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.25 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-ofs-before",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.25 b]\n1.25|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-ofs-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-ofs-same-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";OFS=\" \";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-sibling-before",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$2=\"é\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 é]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-sibling-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 é]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-nf-same",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=NF;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-nf-grow",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=4;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b  ]\n1.25 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-nf-drop",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[]\n 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-overwrite-record",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$0=\"new record\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[new record]\nnew 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-0-extend-lvalue",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$4=2.75;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b  2.7500]\n1.25 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-1-deferred",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-1-observed",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-deferred",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-printed",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;print $1;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("2\n[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-concatenated",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$1 \"\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-observed",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-ofs-before",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-ofs-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-ofs-same-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";OFS=\" \";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-sibling-before",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$2=\"é\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 é]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-sibling-after",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 é]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-nf-same",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=NF;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-nf-grow",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=4;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b  ]\n2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-nf-drop",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[]\n 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-overwrite-record",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$0=\"new record\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[new record]\nnew 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-1-2-2-extend-lvalue",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$4=2.75;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b  2.7500]\n2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-0-1-deferred",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-0-1-observed",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-0-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.6g\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-0-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.6g\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-0-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.6g\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.333333 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-1-1-deferred",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-1-1-observed",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-1-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.2f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-1-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.2f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-1-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.2f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.33 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-2-1-deferred",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-2-1-observed",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-2-1-ofs-before",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-2-1-ofs-after",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 b]\n0.333333|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-gnu-2-2-1-sibling-after",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1/3;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[0.3333 é]\n0.333333 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-deferred",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-printed",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;print $1;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("1.25\n[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-concatenated",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$1 \"\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-observed",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.25 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-ofs-before",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.25 b]\n1.25|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-ofs-after",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-ofs-same-after",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";OFS=\" \";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-sibling-before",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$2=\"é\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 é]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-sibling-after",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 é]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-nf-same",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=NF;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-nf-grow",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=4;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b  ]\n1.25 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-nf-drop",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[]\n 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-overwrite-record",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$0=\"new record\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[new record]\nnew 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-0-extend-lvalue",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$4=2.75;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b  2.7500]\n1.25 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-deferred",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-printed",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;print $1;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("2\n[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-concatenated",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$1 \"\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-observed",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-ofs-before",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-ofs-after",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-ofs-same-after",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";OFS=\" \";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-sibling-before",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$2=\"é\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 é]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-sibling-after",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 é]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-nf-same",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=NF;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-nf-grow",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=4;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b  ]\n2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-nf-drop",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[]\n 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-overwrite-record",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$0=\"new record\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[new record]\nnew 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-posix-1-2-2-extend-lvalue",
                ["--posix", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$4=2.75;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b  2.7500]\n2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-deferred",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-printed",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;print $1;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("1.25\n[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-concatenated",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$1 \"\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-observed",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.25 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-ofs-before",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.25 b]\n1.25|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-ofs-after",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-ofs-same-after",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";OFS=\" \";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-sibling-before",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$2=\"é\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 é]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-sibling-after",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 é]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-nf-same",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=NF;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b]\n1.25 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-nf-grow",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=4;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b  ]\n1.25 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-nf-drop",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;NF=0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[]\n 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-overwrite-record",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$0=\"new record\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[new record]\nnew 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-0-extend-lvalue",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;$4=2.75;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[1.2500 b  2.7500]\n1.25 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-deferred",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-printed",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;print $1;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("2\n[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-concatenated",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$1 \"\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-observed",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-ofs-before",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;OFS=\"|\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-ofs-after",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";OFS=\"|\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2|2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-ofs-same-after",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;CONVFMT=\"%.4f\";OFS=\" \";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-sibling-before",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$2=\"é\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 é]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-sibling-after",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;s=$0;CONVFMT=\"%.4f\";$2=\"é\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 é]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-nf-same",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=NF;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b]\n2 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-nf-grow",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=4;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b  ]\n2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-nf-drop",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;NF=0;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[]\n 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-overwrite-record",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$0=\"new record\";CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[new record]\nnew 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-fields-traditional-1-2-2-extend-lvalue",
                ["--traditional", "BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=2;$4=2.75;CONVFMT=\"%.4f\";print \"[\"$0\"]\";print $1,NF}"], [])
            .WithStdOut("[2 b  2.7500]\n2 4\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-types-0",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a 1.2500\";$1=\"1.2500\";CONVFMT=\"%.4f\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\";CONVFMT=\"%.6f\";$2=\"z\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\"}"], [])
            .WithStdOut("string [1.2500] string [1.2500 1.2500]\nstring [1.2500] string [1.2500 z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-types-1",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a 1.2500\";$1=\"a\";CONVFMT=\"%.4f\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\";CONVFMT=\"%.6f\";$2=\"z\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\"}"], [])
            .WithStdOut("string [a] string [a 1.2500]\nstring [a] string [a z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-types-2",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a 1.2500\";$1=x;CONVFMT=\"%.4f\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\";CONVFMT=\"%.6f\";$2=\"z\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\"}"], [])
            .WithStdOut("unassigned [] string [ 1.2500]\nunassigned [] string [ z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-types-3",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a 1.2500\";$1=0+\"1.2500\";CONVFMT=\"%.4f\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\";CONVFMT=\"%.6f\";$2=\"z\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\"}"], [])
            .WithStdOut("number [1.2500] string [1.2500 1.2500]\nnumber [1.250000] string [1.250000 z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-types-4",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a 1.2500\";$1=strtonum(\"1.2500\");CONVFMT=\"%.4f\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\";CONVFMT=\"%.6f\";$2=\"z\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\"}"], [])
            .WithStdOut("number [1.2500] string [1.2500 1.2500]\nnumber [1.250000] string [1.250000 z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-types-5",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a 1.2500\";$1=$2;CONVFMT=\"%.4f\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\";CONVFMT=\"%.6f\";$2=\"z\";print typeof($1),\"[\"$1\"]\",typeof($0),\"[\"$0\"]\"}"], [])
            .WithStdOut("strnum [1.2500] string [1.2500 1.2500]\nstrnum [1.2500] string [1.2500 z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-0",
                ["BEGIN{$0=\"1.2500\";print typeof($0);NF=1;print typeof($0)}"], [])
            .WithStdOut("string\nstring\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-1",
                ["BEGIN{$0=1.25;print typeof($0);$1=$1;print typeof($0)}"], [])
            .WithStdOut("number\nstring\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-2",
                ["BEGIN{$0=\"a b\";$1=1.25;print typeof($1);x=$0;CONVFMT=\"%.4f\";$2=\"c\";print $0}"], [])
            .WithStdOut("number\n1.2500 c\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-3",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.6f\";print $0}"], [])
            .WithStdOut("1.2500 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-4",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";OFMT=\"%.6f\";print $1,$0}"], [])
            .WithStdOut("1.250000 1.2500 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-5",
                ["function f(){OFS=\"|\"}BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;f();CONVFMT=\"%.4f\";print $0}"], [])
            .WithStdOut("1.25 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-6",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=OFS;CONVFMT=\"%.4f\";print $0}"], [])
            .WithStdOut("1.25 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-7",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";NF=1;print $0}"], [])
            .WithStdOut("1.2500\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-controls-8",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;CONVFMT=\"%.4f\";$2=\"z\";CONVFMT=\"%.6f\";print $0}"], [])
            .WithStdOut("1.250000 z\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-0-direct",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.4f\";print $0}"], [])
            .WithStdOut("1.25 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-0-indirect",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.4f\";print $0}"], [])
            .WithStdOut("1.25 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-1-direct",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.2f\";print $0}"], [])
            .WithStdOut("1.2500 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-1-indirect",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.2f\";print $0}"], [])
            .WithStdOut("1.2500 b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-2-direct",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.4f\";print $1,$0,OFS}"], [])
            .WithStdOut("1.25|1.25 b||\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-2-indirect",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.4f\";print $1,$0,OFS}"], [])
            .WithStdOut("1.25|1.25 b||\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-3-direct",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.2f\";print $1,$0,OFS}"], [])
            .WithStdOut("1.25|1.2500 b||\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-3-indirect",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.2f\";print $1,$0,OFS}"], [])
            .WithStdOut("1.25|1.2500 b||\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-4-direct",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.4f\";$2=\"c\";print $0}"], [])
            .WithStdOut("1.2500|c\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-4-indirect",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.4f\";$2=\"c\";print $0}"], [])
            .WithStdOut("1.2500|c\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-5-direct",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.2f\";$2=\"c\";print $0}"], [])
            .WithStdOut("1.25|c\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-5-indirect",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.2f\";$2=\"c\";print $0}"], [])
            .WithStdOut("1.25|c\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-6-direct",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.4f\";NF=NF;print $0}"], [])
            .WithStdOut("1.2500|b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-6-indirect",
                ["BEGIN{CONVFMT=\"%.2f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.4f\";NF=NF;print $0}"], [])
            .WithStdOut("1.2500|b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-7-direct",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;OFS=\"|\";CONVFMT=\"%.2f\";NF=NF;print $0}"], [])
            .WithStdOut("1.25|b\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("numeric-field-ofs-shared-7-indirect",
                ["BEGIN{CONVFMT=\"%.4f\";$0=\"a b\";$1=1.25;SYMTAB[\"OFS\"]=\"|\";CONVFMT=\"%.2f\";NF=NF;print $0}"], [])
            .WithStdOut("1.25|b\n")
            .Build();

    }
}
