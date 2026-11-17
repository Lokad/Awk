namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> CallUpdateBoundaryCases()
    {
        yield return Passing("review-array-call-update-after-computational-boundary",
            ["function f(){return 2}BEGIN{" + string.Concat(Enumerable.Repeat("x=1;", 63)) + "a[\"k\"]+=f();print a[\"k\"]}"], [], "2\n");
    }
}
