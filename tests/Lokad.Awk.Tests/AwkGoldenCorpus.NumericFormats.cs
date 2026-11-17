namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NumericFormatAssignmentCases()
    {
        yield return Passing("review-numeric-convfmt-preserves-number-origin",
            ["BEGIN{CONVFMT=0.5;y=0.25;print(y \"\");print typeof(CONVFMT)}"], [], "0.5\nnumber\n");
        yield return Passing("review-numeric-convfmt-captures-previous-format",
            ["BEGIN{CONVFMT=\"%.2f\";CONVFMT=0.123456789;print(0.25 \"\"),typeof(CONVFMT)}"], [], "0.12 number\n");
        yield return Passing("review-numeric-convfmt-keeps-array-keys",
            ["BEGIN{CONVFMT=1/3;a[0.1]=7;for(k in a)print k,a[k];print typeof(CONVFMT)}"], [], "0.333333 7\nnumber\n");
        yield return Passing("review-numeric-ofmt-captures-conversion-format",
            ["BEGIN{CONVFMT=\"%.2f\";OFMT=0.123456789;print 0.25;print typeof(OFMT)}"], [], "0.12\nnumber\n");
        yield return Passing("review-numeric-convfmt-nonfinite-values",
            ["BEGIN{CONVFMT=strtonum(\"+nan\");print(0.25 \"\");CONVFMT=strtonum(\"+inf\");print(0.25 \"\")}"], [], "+nan\n+inf\n");
        yield return Passing("review-numeric-convfmt-input-keeps-spelling",
            ["{CONVFMT=$1;print(0.25 \"\"),typeof(CONVFMT)}"], [], "0.5000 strnum\n", "0.5000\n");
    }
}
