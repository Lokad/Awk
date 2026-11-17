namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> UntypedAliasCases()
    {
        yield return Failing("untyped-alias-global-string",
            ["function f(p){print \"before\";x=\"changed\";p[1]=3;print \"after\"}BEGIN{f(x)}END{print \"end\"}"], [], 2,
            "before\n", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-global-number",
            ["function f(p){x=7;p[1]=3}BEGIN{f(x);print \"after\"}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-global-arithmetic",
            ["function f(p){x+=0;p[1]=3}BEGIN{f(x);print \"after\"}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-global-observation",
            ["function f(p){print x;p[1]=3}BEGIN{f(x);print \"after\"}"], [], 2,
            "\n", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-parameter-origin",
            ["function f(p){x=\"changed\";print typeof(p),(p==0);p[1]=3}BEGIN{f(x)}"], [], 2,
            "untyped 1\n", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-forwarded",
            ["function g(q){x=\"changed\";q[1]=3}function f(p){g(p)}BEGIN{f(x)}"], [], 2,
            "", "awk: attempt to use scalar `q' as an array\n");
        yield return Failing("untyped-alias-same-origin-two-parameters",
            ["function f(p,q){x=\"changed\";q[1]=3}BEGIN{f(x,x)}"], [], 2,
            "", "awk: attempt to use scalar `q' as an array\n");
        yield return Failing("untyped-alias-later-argument",
            ["function f(p,q){p[1]=3}BEGIN{f(x,x=\"changed\")}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Passing("untyped-alias-promotion-control",
            ["function f(p){p[1]=3}BEGIN{f(x);print typeof(x),x[1]}"], [], "array 3\n");
        yield return Failing("untyped-alias-local-assignment-control",
            ["function f(p){p=\"local\";x[1]=3;print p,x[1]}BEGIN{f(x);print typeof(x)}"], [], 2,
            "", "awk: attempt to use scalar `x' as an array\n");
        yield return Passing("untyped-alias-element-assignment",
            ["function f(p){a[1]=\"changed\";p[2]=3;print typeof(p),p[2]}BEGIN{f(a[1]);print a[1]}"], [], "array 3\nchanged\n");
        yield return Passing("untyped-alias-element-read-control",
            ["function f(p){p[2]=3;print p[2]}BEGIN{f(a[1]);print typeof(a[1]),a[1][2]}"], [], "3\narray 3\n");
        yield return Passing("untyped-alias-scalar-observers",
            ["function f(p){x=\"changed\";print typeof(p),isarray(p),length(p)}BEGIN{f(x);print x}"], [], "untyped 0 0\nchanged\n");
        yield return Failing("untyped-alias-membership",
            ["function f(p){x=\"changed\";print (1 in p)}BEGIN{f(x)}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-read",
            ["function f(p){x=\"changed\";print p[1]}BEGIN{f(x)}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-delete-element",
            ["function f(p){x=\"changed\";delete p[1]}BEGIN{f(x)}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-delete-array",
            ["function f(p){x=\"changed\";delete p}BEGIN{f(x)}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Failing("untyped-alias-iteration",
            ["function f(p){x=\"changed\";for(k in p)print k}BEGIN{f(x)}"], [], 2,
            "", "awk: attempt to use scalar `p' as an array\n");
        yield return Passing("untyped-alias-shadowed-global-name",
            ["function f(p,x){x=\"local\";p[1]=3;print x,p[1]}BEGIN{f(x);print typeof(x),x[1]}"], [], "local 3\narray 3\n");
        yield return Passing("untyped-alias-scalar-copy",
            ["function f(p){x=\"changed\";q=p;print typeof(p),typeof(q),q==0;print x}BEGIN{f(x)}"], [], "unassigned unassigned 1\nchanged\n");
    }

    private static IEnumerable<AwkGoldenCase> UntypedAliasOrderingCases()
    {
        // The existing compound-assignment contract captures the target before RHS effects.
        // GNU prints rhs before reporting the scalar parameter in this expression.
        yield return Failing("untyped-alias-compound-target-error-precedes-rhs",
            ["function rhs(){print \"rhs\";return 1}function f(p){x=\"changed\";p[1]+=rhs()}BEGIN{f(x)}"], [], 2, "",
            "awk: attempt to use scalar `p' as an array\n");
    }
}

