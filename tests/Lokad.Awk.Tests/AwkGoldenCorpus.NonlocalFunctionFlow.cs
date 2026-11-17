namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NonlocalFunctionFlowCases()
    {
        yield return Passing("nested-next-abandons-captured-array-update",
            ["function skip(){next}function outer(x){return x+skip()}BEGIN{a[1]=3;a[2]=4}{a[NR]+=outer(1);print \"bad\"}END{print NR,length(a),a[1]+0,a[2]+0}"], [], "2 2 3 4\n", "a\nb\n");
        yield return Failing("function-exit-keeps-end-and-skips-assignment",
            ["function stop(){exit 7}BEGIN{print \"begin\";x=1+stop();print \"bad\"}END{print \"end\",x+0}"], [], 7, "begin\nend 0\n", "");
    }
}
