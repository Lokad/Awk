namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ScalarDiscardCases()
    {
        yield return Failing("scalar-discard-nested-array",
            ["BEGIN{a[1][2]=3;print \"before\";a[1];print \"after\"}END{print \"end\"}"], [], 2,
            "before\n", "awk: attempt to use array `array element' in a scalar context\n");
        yield return Passing("scalar-discard-untaken",
            ["BEGIN{a[1][2]=3;if(0)a[1];print \"after\"}"], [], "after\n");
        yield return Failing("scalar-discard-root-array",
            ["BEGIN{a[1]=3;print \"before\";a;print \"after\"}"], [], 2,
            "before\n", "awk: attempt to use array `a' in a scalar context\n");
        yield return Failing("scalar-discard-grouped",
            ["BEGIN{a[1][2]=3;print \"before\";(a[1]);print \"after\"}"], [], 2,
            "before\n", "awk: attempt to use array `array element' in a scalar context\n");
        yield return Failing("scalar-discard-conditional",
            ["BEGIN{a[1][2]=3;print \"before\";1?a[1]:0;print \"after\"}"], [], 2,
            "before\n", "awk: attempt to use array `array element' in a scalar context\n");
        yield return Passing("scalar-discard-conditional-untaken",
            ["BEGIN{a[1][2]=3;0?a[1]:0;print \"after\"}"], [], "after\n");
        yield return Failing("scalar-discard-for-initializer",
            ["BEGIN{a[1][2]=3;print \"before\";for(a[1];0;)print \"body\";print \"after\"}"], [], 2,
            "before\n", "awk: attempt to use array `array element' in a scalar context\n");
        yield return Failing("scalar-discard-for-step",
            ["BEGIN{a[1][2]=3;for(i=0;i<1;a[1]){i++;print \"body\"}print \"after\"}"], [], 2,
            "body\n", "awk: attempt to use array `array element' in a scalar context\n");
        yield return Failing("scalar-discard-function-array-return",
            ["function f(){return a[1]}BEGIN{a[1][2]=3;print \"before\";f();print \"after\"}"], [], 2,
            "before\n", "awk: attempt to use array `array element' in a scalar context\n");
        yield return Passing("scalar-discard-array-observers",
            ["function f(p){p[3]=4}BEGIN{a[1][2]=3;typeof(a[1]);length(a[1]);f(a[1]);print typeof(a[1]),length(a[1]),a[1][3]}"], [], "array 2 4\n");
        yield return Passing("scalar-discard-missing-read",
            ["BEGIN{a[1];print length(a),typeof(a[1])}"], [], "1 untyped\n");
        yield return Passing("scalar-discard-conditional-missing-read",
            ["BEGIN{1?a[1]:0;print length(a),typeof(a[1])}"], [], "1 untyped\n");
        yield return Passing("scalar-discard-deleted-child",
            ["BEGIN{a[1][2]=3;delete a[1];a[1];print length(a),typeof(a[1])}"], [], "1 untyped\n");
        yield return Failing("scalar-discard-deeper-array",
            ["BEGIN{a[1][2][3]=4;print \"before\";a[1][2];print \"after\"}"], [], 2,
            "before\n", "awk: attempt to use array `array element' in a scalar context\n");
        yield return Failing("scalar-discard-local-array",
            ["function f(p){p[1]=2;print \"before\";p;print \"after\"}BEGIN{f(a)}"], [], 2,
            "before\n", "awk: attempt to use array `p' in a scalar context\n");
        yield return Passing("scalar-discard-missing-global",
            ["BEGIN{x;print typeof(x)}"], [], "unassigned\n");
        yield return Passing("scalar-discard-missing-parameter",
            ["function f(p){p;print typeof(p)}BEGIN{f(x);print typeof(x)}"], [], "unassigned\nunassigned\n");
        yield return Passing("scalar-discard-call-results",
            ["function inc(){x++;return x}BEGIN{x=0;inc();n=inc();(x?inc():9);print x,n}"], [], "3 2\n");
        yield return Passing("scalar-discard-assignment-results",
            ["function inc(){n++;return n}BEGIN{x=inc();(x=inc());print x,n}"], [], "2 2\n");
        yield return Failing("scalar-discard-conditional-deeper-array",
            ["BEGIN{a[1][2][3]=4;print \"before\";1?a[1][2]:0;print \"after\"}"], [], 2,
            "before\n", "awk: attempt to use array `array element' in a scalar context\n");
    }
}
