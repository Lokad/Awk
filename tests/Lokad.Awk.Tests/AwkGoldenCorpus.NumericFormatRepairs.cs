namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NumericFormatRepairCases()
    {
        // GNU 5.3.2 crashes on recursive %s in CONVFMT. Preserve a bounded,
        // round-trip base conversion rather than recursively applying that format.
        yield return Passing("numeric-format-recursive-string-conversion-is-bounded",
            ["BEGIN{CONVFMT=\"%s\";print ((0.1+0.2) \"\"),(1e-7 \"\")}"], [],
            "0.30000000000000004 1E-07\n");
        yield return Passing("numeric-format-output-uses-bounded-conversion-format",
            ["BEGIN{CONVFMT=\"%s\";OFMT=\"%s\";print 1e-7+0,0.1+0.2}"], [],
            "1E-07 0.30000000000000004\n");
        yield return Passing("numeric-format-decorated-string-conversion-is-bounded",
            ["BEGIN{CONVFMT=\"[%s]\";print ((0.1+0.2) \"\")}"], [],
            "[0.30000000000000004]\n");
        yield return Passing("numeric-format-repair-default-numeric-string",
            ["BEGIN{OFMT=\"%s\";print 1e-7+0,0.1+0.2}"], [], "1e-07 0.3\n");
        yield return Passing("numeric-format-repair-custom-numeric-string",
            ["BEGIN{CONVFMT=\"%.2f\";OFMT=\"%s\";print 1e-7+0,0.1+0.2}"], [], "0.00 0.30\n");
        yield return Passing("numeric-format-repair-ordinary-sprintf-conversion",
            ["BEGIN{CONVFMT=\"%.2f\";OFMT=\"%s\";print sprintf(\"%s\",0.1+0.2)}"], [], "0.30\n");
        yield return Passing("numeric-format-repair-large-general-precision",
            ["BEGIN{printf \"%.2000000000g\\n\",1}"], [], "1\n");
        yield return Passing("numeric-format-repair-large-star-general-precision",
            ["BEGIN{printf \"%.*G\\n\",2000000000,1}"], [], "1\n");
        yield return Passing("numeric-format-repair-nonintegral-exact-precision-control",
            ["BEGIN{printf \"%.10000g\\n\",0.1}"], [], "0.1000000000000000055511151231257827021181583404541015625\n");
        yield return Passing("numeric-format-repair-alternate-form-keeps-zeroes",
            ["BEGIN{x=sprintf(\"%#.10000g\",1);print length(x),substr(x,1,4),substr(x,length(x)-2)}"], [], "10001 1.00 000\n");
    }
}
