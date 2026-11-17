namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> BufferedFieldRepairCases()
    {
        yield return Passing("review-dirty-buffered-single-field-capture",
            ["{$2=\"x\";v=$1;print v,$0,typeof(v)}"], [], "abc abc x string\n", "abc\n");
        yield return Passing("review-dirty-buffered-single-field-comma",
            ["BEGIN{FS=\",\"}{$2=\"x\";v=$1;print v,$0,typeof(v)}"], [], "abc abc x string\n", "abc\n");
        yield return Passing("review-dirty-buffered-single-field-numeric-spelling",
            ["{$2=\"x\";v=$1;print v,$0,typeof(v),(v==5)}"], [], "05 05 x strnum 1\n", "05\n");
    }
}
