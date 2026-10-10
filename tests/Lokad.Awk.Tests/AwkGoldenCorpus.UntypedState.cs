namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU 5.4 is the type-state reference. Seven element-origin cases use an
    // independent shared-scalar model because native GNU crashes; an array
    // comparison internal error uses the equivalent scalar-context diagnostic.
    private static IEnumerable<AwkGoldenCase> UntypedStateCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-0",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-1",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped unassigned untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-2",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);a[2]=a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped unassigned 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-3",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-4",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print a[1],typeof(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n untyped\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-5",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print typeof(a[1]),a[1],typeof(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped  untyped\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-6",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print a[1],a[1]=7;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n 7\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-7",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);printf \"%s\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-8",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);printf \"%g\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n0after unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-9",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);printf \"skip\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nskipafter untyped untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-10",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=sprintf(\"%s\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-11",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=sprintf(\"%g\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-12",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=sprintf(\"skip\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-13",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=length(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-14",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=int(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-15",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=sqrt(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-16",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=toupper(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-17",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=index(a[1],\"a\");print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-18",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=index(\"a\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-19",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=substr(a[1],1);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-20",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=substr(\"a\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-21",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=match(a[1],/a/);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-22",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=(a[1]~/a/);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-23",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=split(a[1],b);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-24",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=(a[1]?1:2);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-25",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=!a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-26",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=a[1]+0;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-27",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=a[1] \"\";print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-28",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=(a[1]==0);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-29",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=(a[1]==\"\");print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-30",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped unassigned untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-31",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print (1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-32",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print typeof(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped\nafter untyped untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-33",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=typeof(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-34",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=isarray(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-35",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);a[a[1]]=1;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped untyped 1 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-36",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print a[1]+0,a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n0 \nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-37",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=f(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-38",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);x=h(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned unassigned untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-39",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print f(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped\nafter untyped untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-context-40",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{print \"before\",typeof(a[1]);print h(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-0-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-0-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-0-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-1-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){x=1;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nnumber\nafter untyped unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-1-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){x=1;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-1-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){x=1;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nnumber\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-2-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){y=x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nunassigned\nafter untyped unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-2-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){y=x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nunassigned\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-2-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){y=x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nunassigned\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-3-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){x+=0;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nnumber\nafter untyped unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-3-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){x+=0;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-3-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){x+=0;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nnumber\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-4-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){print x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\n\nunassigned\nafter untyped unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-4-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){print x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\n\nunassigned\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-4-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){print x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\n\nunassigned\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-5-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){return x}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nafter untyped unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-5-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){return x}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-5-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){return x}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-6-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){return typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped 1 string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-6-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){return typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped 1 string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-6-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){return typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped 1 string\n")
            .Build();

        // Reference bug; qualified by shared-untyped-scalar.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-7-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){g(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned\nafter untyped unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-7-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){g(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-7-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){g(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned\nafter untyped untyped 1 unassigned\n")
            .Build();

        // Reference bug; qualified by shared-untyped-scalar.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-8-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x,y){print typeof(x),typeof(y);z=x;print typeof(x),typeof(y)}BEGIN{print \"before\",typeof(origin);r=f(a[1],a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped untyped\nunassigned unassigned\nafter untyped unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-8-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x,y){print typeof(x),typeof(y);z=x;print typeof(x),typeof(y)}BEGIN{print \"before\",typeof(origin);r=f(origin,origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped untyped\nunassigned unassigned\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-8-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x,y){print typeof(x),typeof(y);z=x;print typeof(x),typeof(y)}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped untyped\nunassigned untyped\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-9-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){delete a[1];z=x;print typeof(x),typeof(a[1])}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nunassigned untyped\nafter untyped untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-9-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){delete a[1];z=x;print typeof(x),typeof(a[1])}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nunassigned untyped\nafter unassigned untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-9-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){delete a[1];z=x;print typeof(x),typeof(a[1])}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nunassigned untyped\nafter untyped untyped 1 unassigned\n")
            .Build();

        // Reference bug; qualified by shared-untyped-scalar.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-10-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){a[1]=5;print typeof(x);z=x;print typeof(x),a[1]}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned 5\nafter untyped number 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-10-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){a[1]=5;print typeof(x);z=x;print typeof(x),a[1]}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned 5\nafter unassigned number 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-10-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){a[1]=5;print typeof(x);z=x;print typeof(x),a[1]}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned 5\nafter untyped number 1 unassigned\n")
            .Build();

        // Reference bug; qualified by shared-untyped-scalar.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-11-element",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){origin=5;print typeof(x);z=x;print typeof(x),origin}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned 5\nafter number unassigned 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-11-global",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){origin=5;print typeof(x);z=x;print typeof(x),origin}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned 5\nafter number untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-origin-11-omitted",
                ["function g(x){print typeof(x);z=x;print typeof(x)}function f(x){origin=5;print typeof(x);z=x;print typeof(x),origin}BEGIN{print \"before\",typeof(origin);r=f();print \"after\",typeof(origin),typeof(a[1]),(1 in a),typeof(r)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned 5\nafter number untyped 1 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-0",
                ["BEGIN{print typeof(a[1]);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("untyped\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-1",
                ["BEGIN{print isarray(a[1]);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("0\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-2",
                ["BEGIN{print a[1];print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-3",
                ["BEGIN{print a[1]+0;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("0\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-4",
                ["BEGIN{print a[1] \"\";print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-5",
                ["BEGIN{print (a[1]);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-6",
                ["BEGIN{print (a[1]==0);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("1\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-read-7",
                ["BEGIN{print (a[1]==\"\");print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("1\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-0",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped\nreturn unassigned\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-0",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nuntyped\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-0",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nuntyped\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-1",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print isarray(x),typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n0 untyped\nreturn unassigned\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-1",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print isarray(x),typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\n0 untyped\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-1",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print isarray(x),typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\n0 untyped\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-2",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){y=x;print typeof(x),typeof(y)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nreturn unassigned\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-2",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){y=x;print typeof(x),typeof(y)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nreturn unassigned\nunassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-2",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){y=x;print typeof(x),typeof(y)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-3",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){x=1;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nnumber\nreturn unassigned\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-3",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){x=1;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nnumber\nreturn unassigned\nunassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-3",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){x=1;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nnumber\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-4",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n\nunassigned\nreturn unassigned\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-4",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\n\nunassigned\nreturn unassigned\nunassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-4",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print x;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\n\nunassigned\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-5",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){return x}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nreturn unassigned\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-5",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){return x}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nreturn unassigned\nunassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-5",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){return x}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nreturn unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-6",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){return typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nreturn string\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-6",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){return typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nreturn string\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-6",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){return typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nreturn string\nuntyped\n")
            .Build();

        // Reference bug; qualified by shared-untyped-scalar.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-7",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){g(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned\nreturn unassigned\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-7",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){g(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned\nreturn unassigned\nunassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-7",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){g(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned\nreturn unassigned\nuntyped\n")
            .Build();

        // Reference bug; qualified by shared-untyped-scalar.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-8",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x,z){print typeof(x),typeof(z);y=x;print typeof(x),typeof(z)}BEGIN{print \"before\",typeof(origin);r=f(a[1],a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped untyped\nunassigned unassigned\nreturn unassigned\nunassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-8",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x,z){print typeof(x),typeof(z);y=x;print typeof(x),typeof(z)}BEGIN{print \"before\",typeof(origin);r=f(origin,origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nuntyped untyped\nunassigned unassigned\nreturn unassigned\nunassigned\n")
            .Build();

        // Reference bug; qualified by shared-untyped-scalar.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-element-9",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print typeof(x);v=origin;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(a[1]);print \"return\",typeof(r);print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped\nuntyped\nreturn unassigned\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-scalar-9",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print typeof(x);v=origin;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f(origin);print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned\nreturn unassigned\nunassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-call-omitted-9",
                ["function g(x){print typeof(x);y=x;print typeof(x)} function f(x){print typeof(x);v=origin;print typeof(x)}BEGIN{print \"before\",typeof(origin);r=f();print \"return\",typeof(r);print typeof(origin)}"], [])
            .WithStdOut("before untyped\nuntyped\nuntyped\nreturn unassigned\nunassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-effects-0",
                ["function f(){i++;return 1} BEGIN{$0=\"a b\";print isarray(i++),i,NF;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("0 1 2\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-effects-1",
                ["function f(){i++;return 1} BEGIN{$0=\"a b\";print isarray(i=5),i,NF;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("0 5 2\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-effects-2",
                ["function f(){i++;return 1} BEGIN{$0=\"a b\";print isarray(a[++i]),i,NF;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("0 1 2\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-effects-3",
                ["function f(){i++;return 1} BEGIN{$0=\"a b\";print isarray(f()),i,NF;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("0 1 2\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-effects-4",
                ["function f(){i++;return 1} BEGIN{$0=\"a b\";print isarray($3),i,NF;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("0  2\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-effects-5",
                ["function f(){i++;return 1} BEGIN{$0=\"a b\";print typeof(a[++i]),i,NF;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("untyped 1 2\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-proto-effects-6",
                ["function f(){i++;return 1} BEGIN{$0=\"a b\";print typeof((a[++i])),i,NF;print typeof(a[1]),(1 in a),length(a)}"], [])
            .WithStdOut("untyped 1 2\nuntyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-0",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-4",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);print a[1],typeof(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\n0 number\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-8",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);printf \"%g\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\n0after number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-12",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);x=sprintf(\"skip\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-16",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);x=toupper(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-20",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);x=substr(\"a\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-24",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);x=(a[1]?1:2);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-28",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);x=(a[1]==0);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-32",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);print typeof(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nnumber\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-36",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);print a[1]+0,a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\n0 0\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-40",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=0;print \"before\",typeof(a[1]);print h(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\n0\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-44",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);print a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\n7\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-48",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);printf \"%s\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\n7after number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-52",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);x=sprintf(\"%g\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-56",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);x=sqrt(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-60",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);x=substr(a[1],1);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-64",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);x=split(a[1],b);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-68",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);x=a[1] \"\";print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-72",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);print (1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\n7\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-76",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);a[a[1]]=1;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nafter number untyped untyped 1 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-80",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=7;print \"before\",typeof(a[1]);print f(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before number\nnumber\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-84",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);a[2]=a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string untyped string 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-88",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);print a[1],a[1]=7;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\n 7\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-92",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=sprintf(\"%s\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-96",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=int(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-100",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=index(\"a\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-104",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=(a[1]~/a/);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-108",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=a[1]+0;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-112",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-116",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=isarray(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-120",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"\";print \"before\",typeof(a[1]);x=h(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-124",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-128",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);print typeof(a[1]),a[1],typeof(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nstring 00 string\nafter string untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-132",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);printf \"skip\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nskipafter string untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-136",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=length(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-140",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=index(a[1],\"a\");print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-144",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=match(a[1],/a/);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-148",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=!a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-152",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=(a[1]==\"\");print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-156",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=typeof(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-160",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=\"00\";print \"before\",typeof(a[1]);x=f(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before string\nafter string string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-164",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-168",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);print a[1],typeof(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\n unassigned\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-172",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);printf \"%g\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\n0after unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-176",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);x=sprintf(\"skip\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\nafter unassigned string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-180",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);x=toupper(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\nafter unassigned string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-184",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);x=substr(\"a\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\nafter unassigned string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-188",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);x=(a[1]?1:2);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\nafter unassigned number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-192",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);x=(a[1]==0);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\nafter unassigned number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-196",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);print typeof(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\nunassigned\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-200",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);print a[1]+0,a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\n0 \nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-204",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{a[1]=unset;print \"before\",typeof(a[1]);print h(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before unassigned\n\nafter unassigned untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-208",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);print a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\n00\nafter strnum untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-212",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);printf \"%s\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\n00after strnum untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-216",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);x=sprintf(\"%g\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\nafter strnum string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-220",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);x=sqrt(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\nafter strnum number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-224",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);x=substr(a[1],1);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\nafter strnum string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-228",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);x=split(a[1],b);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\nafter strnum number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-232",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);x=a[1] \"\";print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\nafter strnum string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-236",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);print (1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\n00\nafter strnum untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-240",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);a[a[1]]=1;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\nafter strnum untyped untyped 1 3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-244",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{split(\"00\",a);print \"before\",typeof(a[1]);print f(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before strnum\nstrnum\nafter strnum untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-248",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);a[2]=a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped untyped unassigned 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-252",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);print a[1],a[1]=7;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\n 7\nafter number untyped untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-256",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=sprintf(\"%s\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-260",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=int(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-264",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=index(\"a\",a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-268",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=(a[1]~/a/);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-272",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=a[1]+0;print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-276",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped unassigned untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-280",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=isarray(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-284",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{isarray(a[1]);print \"before\",typeof(a[1]);x=h(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned unassigned untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-288",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped unassigned untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-292",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);print typeof(a[1]),a[1],typeof(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nuntyped  untyped\nafter unassigned string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-296",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);printf \"skip\",a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nskipafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-300",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=length(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-304",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=index(a[1],\"a\");print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter unassigned number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-308",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=match(a[1],/a/);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-312",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=!a[1];print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-316",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=(a[1]==\"\");print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped number untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-320",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=typeof(1?a[1]:7);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-seeded-324",
                ["function f(x){return typeof(x)}function h(x){return x}BEGIN{x=typeof(a[1]);print \"before\",typeof(a[1]);x=f(a[1]);print \"after\",typeof(a[1]),typeof(x),typeof(a[2]),(1 in a),length(a)}"], [])
            .WithStdOut("before untyped\nafter untyped string untyped 1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-449",
                ["function f(p){print typeof(p),isarray(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(x);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nuntyped 0\nafter unassigned untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-450",
                ["function f(p){print typeof((p)),isarray((p))}BEGIN{i=0;print \"before\",typeof(x);r=f(x);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-451",
                ["function f(p){q=p;print typeof(p),typeof(q)}BEGIN{i=0;print \"before\",typeof(x);r=f(x);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-452",
                ["function f(p){p=7;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(x);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-453",
                ["function f(p){p+=0;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(x);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-454",
                ["function f(p){return p}BEGIN{i=0;print \"before\",typeof(x);r=f(x);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-455",
                ["function f(p){return typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(x);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter string untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-456",
                ["function f(p){print typeof(p),isarray(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((x));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-457",
                ["function f(p){print typeof((p)),isarray((p))}BEGIN{i=0;print \"before\",typeof(x);r=f((x));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-458",
                ["function f(p){q=p;print typeof(p),typeof(q)}BEGIN{i=0;print \"before\",typeof(x);r=f((x));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-459",
                ["function f(p){p=7;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((x));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-460",
                ["function f(p){p+=0;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((x));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-461",
                ["function f(p){return p}BEGIN{i=0;print \"before\",typeof(x);r=f((x));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-462",
                ["function f(p){return typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((x));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter string unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-463",
                ["function f(p){print typeof(p),isarray(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((x)));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-464",
                ["function f(p){print typeof((p)),isarray((p))}BEGIN{i=0;print \"before\",typeof(x);r=f(((x)));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-465",
                ["function f(p){q=p;print typeof(p),typeof(q)}BEGIN{i=0;print \"before\",typeof(x);r=f(((x)));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-466",
                ["function f(p){p=7;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((x)));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-467",
                ["function f(p){p+=0;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((x)));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-468",
                ["function f(p){return p}BEGIN{i=0;print \"before\",typeof(x);r=f(((x)));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter unassigned unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-469",
                ["function f(p){return typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((x)));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter string unassigned untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-470",
                ["function f(p){print typeof(p),isarray(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[1]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nuntyped 0\nafter unassigned untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-471",
                ["function f(p){print typeof((p)),isarray((p))}BEGIN{i=0;print \"before\",typeof(x);r=f(a[1]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-472",
                ["function f(p){q=p;print typeof(p),typeof(q)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[1]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-473",
                ["function f(p){p=7;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[1]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-474",
                ["function f(p){p+=0;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[1]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-475",
                ["function f(p){return p}BEGIN{i=0;print \"before\",typeof(x);r=f(a[1]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-476",
                ["function f(p){return typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[1]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter string untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-477",
                ["function f(p){print typeof(p),isarray(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((a[1]));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nuntyped 0\nafter unassigned untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-478",
                ["function f(p){print typeof((p)),isarray((p))}BEGIN{i=0;print \"before\",typeof(x);r=f((a[1]));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-479",
                ["function f(p){q=p;print typeof(p),typeof(q)}BEGIN{i=0;print \"before\",typeof(x);r=f((a[1]));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-480",
                ["function f(p){p=7;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((a[1]));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-481",
                ["function f(p){p+=0;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((a[1]));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-482",
                ["function f(p){return p}BEGIN{i=0;print \"before\",typeof(x);r=f((a[1]));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-483",
                ["function f(p){return typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f((a[1]));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter string untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-484",
                ["function f(p){print typeof(p),isarray(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((a[1])));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nuntyped 0\nafter unassigned untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-485",
                ["function f(p){print typeof((p)),isarray((p))}BEGIN{i=0;print \"before\",typeof(x);r=f(((a[1])));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-486",
                ["function f(p){q=p;print typeof(p),typeof(q)}BEGIN{i=0;print \"before\",typeof(x);r=f(((a[1])));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-487",
                ["function f(p){p=7;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((a[1])));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-488",
                ["function f(p){p+=0;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((a[1])));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-489",
                ["function f(p){return p}BEGIN{i=0;print \"before\",typeof(x);r=f(((a[1])));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter unassigned untyped unassigned 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-490",
                ["function f(p){return typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(((a[1])));print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter string untyped untyped 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-491",
                ["function f(p){print typeof(p),isarray(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[++i]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nuntyped 0\nafter unassigned untyped untyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-492",
                ["function f(p){print typeof((p)),isarray((p))}BEGIN{i=0;print \"before\",typeof(x);r=f(a[++i]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned 0\nafter unassigned untyped unassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-493",
                ["function f(p){q=p;print typeof(p),typeof(q)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[++i]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nunassigned unassigned\nafter unassigned untyped unassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-494",
                ["function f(p){p=7;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[++i]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-495",
                ["function f(p){p+=0;print typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[++i]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nnumber\nafter unassigned untyped unassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-496",
                ["function f(p){return p}BEGIN{i=0;print \"before\",typeof(x);r=f(a[++i]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter unassigned untyped unassigned 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-argument-497",
                ["function f(p){return typeof(p)}BEGIN{i=0;print \"before\",typeof(x);r=f(a[++i]);print \"after\",typeof(r),typeof(x),typeof(a[1]),(1 in a),i}"], [])
            .WithStdOut("before untyped\nafter string untyped untyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-498",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(SYMTAB[\"x\"]),isarray(SYMTAB[\"x\"])}"], [])
            .WithStdOut("number 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-499",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((SYMTAB[\"x\"])),isarray((SYMTAB[\"x\"]))}"], [])
            .WithStdOut("number 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-500",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((SYMTAB[\"x\"]))),isarray(((SYMTAB[\"x\"])))}"], [])
            .WithStdOut("number 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-501",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(SYMTAB[\"a\"]),isarray(SYMTAB[\"a\"])}"], [])
            .WithStdOut("array 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-502",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((SYMTAB[\"a\"])),isarray((SYMTAB[\"a\"]))}"], [])
            .WithStdOut("array 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-503",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((SYMTAB[\"a\"]))),isarray(((SYMTAB[\"a\"])))}"], [])
            .WithStdOut("array 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-504",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(SYMTAB[\"ARGV\"]),isarray(SYMTAB[\"ARGV\"])}"], [])
            .WithStdOut("array 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-505",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((SYMTAB[\"ARGV\"])),isarray((SYMTAB[\"ARGV\"]))}"], [])
            .WithStdOut("array 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-506",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((SYMTAB[\"ARGV\"]))),isarray(((SYMTAB[\"ARGV\"])))}"], [])
            .WithStdOut("array 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-507",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(SYMTAB[\"f\"]),isarray(SYMTAB[\"f\"])}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"f\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-508",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((SYMTAB[\"f\"])),isarray((SYMTAB[\"f\"]))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"f\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-509",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((SYMTAB[\"f\"]))),isarray(((SYMTAB[\"f\"])))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"f\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-510",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(SYMTAB[\"length\"]),isarray(SYMTAB[\"length\"])}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"length\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-511",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((SYMTAB[\"length\"])),isarray((SYMTAB[\"length\"]))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"length\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-512",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((SYMTAB[\"length\"]))),isarray(((SYMTAB[\"length\"])))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `SYMTAB[\"length\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-513",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(FUNCTAB[\"x\"]),isarray(FUNCTAB[\"x\"])}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"x\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-514",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((FUNCTAB[\"x\"])),isarray((FUNCTAB[\"x\"]))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"x\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-515",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((FUNCTAB[\"x\"]))),isarray(((FUNCTAB[\"x\"])))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"x\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-516",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(FUNCTAB[\"a\"]),isarray(FUNCTAB[\"a\"])}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-517",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((FUNCTAB[\"a\"])),isarray((FUNCTAB[\"a\"]))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-518",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((FUNCTAB[\"a\"]))),isarray(((FUNCTAB[\"a\"])))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"a\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-519",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(FUNCTAB[\"ARGV\"]),isarray(FUNCTAB[\"ARGV\"])}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"ARGV\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-520",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((FUNCTAB[\"ARGV\"])),isarray((FUNCTAB[\"ARGV\"]))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"ARGV\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-521",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((FUNCTAB[\"ARGV\"]))),isarray(((FUNCTAB[\"ARGV\"])))}"], [])
            .ExitsWith(2)
            .WithStdErr("awk: reference to uninitialized element `FUNCTAB[\"ARGV\"]' is not allowed\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-522",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(FUNCTAB[\"f\"]),isarray(FUNCTAB[\"f\"])}"], [])
            .WithStdOut("string 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-523",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((FUNCTAB[\"f\"])),isarray((FUNCTAB[\"f\"]))}"], [])
            .WithStdOut("string 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-524",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((FUNCTAB[\"f\"]))),isarray(((FUNCTAB[\"f\"])))}"], [])
            .WithStdOut("string 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-525",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(FUNCTAB[\"length\"]),isarray(FUNCTAB[\"length\"])}"], [])
            .WithStdOut("string 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-526",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof((FUNCTAB[\"length\"])),isarray((FUNCTAB[\"length\"]))}"], [])
            .WithStdOut("string 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-virtual-527",
                ["function f(){return 1}BEGIN{a[1]=1;x=7;print typeof(((FUNCTAB[\"length\"]))),isarray(((FUNCTAB[\"length\"])))}"], [])
            .WithStdOut("string 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-return-528",
                ["function f(){}BEGIN{x=f();print typeof(x),x==0,x==\"\",typeof(f());print typeof(a[1])}"], [])
            .WithStdOut("unassigned 1 1 unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-return-529",
                ["function f(){return}BEGIN{x=f();print typeof(x),x==0,x==\"\",typeof(f());print typeof(a[1])}"], [])
            .WithStdOut("unassigned 1 1 unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-return-530",
                ["function f(){return \"\"}BEGIN{x=f();print typeof(x),x==0,x==\"\",typeof(f());print typeof(a[1])}"], [])
            .WithStdOut("string 0 1 string\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-return-531",
                ["function f(){return absent}BEGIN{x=f();print typeof(x),x==0,x==\"\",typeof(f());print typeof(a[1])}"], [])
            .WithStdOut("unassigned 1 1 unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-return-532",
                ["function f(){return a[1]}BEGIN{x=f();print typeof(x),x==0,x==\"\",typeof(f());print typeof(a[1])}"], [])
            .WithStdOut("unassigned 1 1 unassigned\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-return-533",
                ["function f(){return 0}BEGIN{x=f();print typeof(x),x==0,x==\"\",typeof(f());print typeof(a[1])}"], [])
            .WithStdOut("number 1 0 number\nuntyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-alias-534",
                ["function g(p){print typeof(p);print isarray(p[1]);print typeof(p)}function f(p){print typeof(p),isarray(p);print typeof(p[1]),(1 in p)}BEGIN{print typeof(x);f(x);print typeof(x),typeof(x[1]),(1 in x)}"], [])
            .WithStdOut("untyped\nuntyped 0\nuntyped 1\narray untyped 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-alias-535",
                ["function g(p){print typeof(p);print isarray(p[1]);print typeof(p)}function f(p){print typeof(p);p[1]=7;print typeof(p),(1 in p)}BEGIN{print typeof(x);f(x);print typeof(x),typeof(x[1]),(1 in x)}"], [])
            .WithStdOut("untyped\nuntyped\narray 1\narray number 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-alias-536",
                ["function g(p){print typeof(p);print isarray(p[1]);print typeof(p)}function f(p){g(p);print typeof(p)}BEGIN{print typeof(x);f(x);print typeof(x),typeof(x[1]),(1 in x)}"], [])
            .WithStdOut("untyped\nuntyped\n0\narray\narray\narray untyped 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-alias-537",
                ["function g(p){print typeof(p);print isarray(p[1]);print typeof(p)}function f(p,q){print typeof(p),typeof(q);p[1]=7;print typeof(p),typeof(q),q[1]}BEGIN{print typeof(x);f(x,x);print typeof(x),typeof(x[1]),(1 in x)}"], [])
            .WithStdOut("untyped\nuntyped untyped\narray array 7\narray number 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-538",
                ["BEGIN{a[2]=1;print typeof(a[1]),(1 in a),(2 in a);print typeof(a[2])}"], [])
            .WithStdOut("untyped 1 1\nnumber\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-539",
                ["BEGIN{print typeof(a[1]),isarray(a),(1 in a)}"], [])
            .WithStdOut("untyped 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-540",
                ["function set(a){a=1}BEGIN{set(x);print typeof(x)}"], [])
            .WithStdOut("unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-541",
                ["BEGIN{print typeof((x)),isarray((x)),typeof(x)}"], [])
            .WithStdOut("unassigned 0 unassigned\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-542",
                ["BEGIN{print typeof(a[++i]),i,(i in a);print isarray(a[++i]),i,(i in a);print typeof(a[1]),typeof(a[2]),length(a)}"], [])
            .WithStdOut("untyped 1 1\n0 2 1\nuntyped untyped 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-543",
                ["function f(p){i=2;q=p;print typeof(p)}BEGIN{i=1;f(a[i]);print typeof(a[1]),typeof(a[2]),i}"], [])
            .WithStdOut("unassigned\nunassigned untyped 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-544",
                ["function f(p){delete a[1];print typeof(p);q=p;print typeof(p)}BEGIN{f(a[1]);print (1 in a),typeof(a[1])}"], [])
            .WithStdOut("untyped\nunassigned\n0 untyped\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-regression-545",
                ["BEGIN{a[1];delete a[1];print (1 in a);print typeof(a[1]),(1 in a);delete a;print typeof(a[1]),length(a)}"], [])
            .WithStdOut("0\nuntyped 1\nuntyped 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-external-source-546",
                ["function g(p){print typeof(p),p[1]}function f(p){x[1]=3;print typeof(p),isarray(p);g(p)}BEGIN{print \"before\",typeof(x);f(x);print \"after\",typeof(x)}"], [])
            .WithStdOut("before untyped\narray 1\narray 3\nafter array\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-external-source-547",
                ["function g(p){print typeof(p),p[1]}function f(p){x[1]=3;print typeof(p),isarray(p);p=7}BEGIN{print \"before\",typeof(x);f(x);print \"after\",typeof(x)}"], [])
            .ExitsWith(2)
            .WithStdOut("before untyped\narray 1\n")
            .WithStdErr("awk: attempt to use array `p' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-external-source-548",
                ["function g(p){print typeof(p),p[1]}function f(p){x[1]=3;print typeof(p),isarray(p);print p+0}BEGIN{print \"before\",typeof(x);f(x);print \"after\",typeof(x)}"], [])
            .ExitsWith(2)
            .WithStdOut("before untyped\narray 1\n")
            .WithStdErr("awk: attempt to use array `p' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-external-source-549",
                ["function g(p){print typeof(p),p[1]}function f(p){x=7;print typeof(p);q=p;print typeof(p),x}BEGIN{print \"before\",typeof(x);f(x);print \"after\",typeof(x)}"], [])
            .WithStdOut("before untyped\nuntyped\nunassigned 7\nafter number\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-550",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print typeof((a));print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-551",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print isarray((a));print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-552",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print f((a));print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-553",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print f(((a)));print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-554",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print typeof(1?SYMTAB[\"a\"]:7);print \"after\"}"], [])
            .WithStdOut("before\narray\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-555",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print isarray(1?SYMTAB[\"a\"]:7);print \"after\"}"], [])
            .WithStdOut("before\n1\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-556",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print length(1?SYMTAB[\"a\"]:7);print \"after\"}"], [])
            .WithStdOut("before\n1\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-557",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print f(SYMTAB[\"a\"]);print \"after\"}"], [])
            .WithStdOut("before\narray 1\n1\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-558",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print f((SYMTAB[\"a\"]));print \"after\"}"], [])
            .WithStdOut("before\narray 1\n1\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-559",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print f(1?SYMTAB[\"a\"]:7);print \"after\"}"], [])
            .WithStdOut("before\narray 1\n1\nafter\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-560",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print a[2]=SYMTAB[\"a\"];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-561",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print x=SYMTAB[\"a\"];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-562",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print g();print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-563",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print SYMTAB[\"a\"]+0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        // Reference bug; qualified by array-scalar-diagnostic.
        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-564",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print SYMTAB[\"a\"]==0;print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-565",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print !SYMTAB[\"a\"];print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-array-context-566",
                ["function f(p){print typeof(p),isarray(p);return length(p)}function g(){return SYMTAB[\"a\"]}BEGIN{a[1]=7;print \"before\";print SYMTAB[\"a\"] \"\";print \"after\"}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-sort-untyped-567",
                ["BEGIN{t=typeof(a[1]);a[2]=\"\";a[3]=0;n=asort(a,b);print n;for(i=1;i<=n;i++)print typeof(a[i]),typeof(b[i]),\"[\"a[i]\"]\",\"[\"b[i]\"]\"}"], [])
            .WithStdOut("3\nuntyped unassigned [] []\nstring number [] [0]\nnumber string [0] []\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-sort-untyped-568",
                ["BEGIN{t=typeof(a[1]);a[2]=\"\";a[3]=0;n=asort(a);print n;for(i=1;i<=n;i++)print typeof(a[i]),typeof(b[i]),\"[\"a[i]\"]\",\"[\"b[i]\"]\"}"], [])
            .WithStdOut("3\nunassigned untyped [] []\nnumber untyped [0] []\nstring untyped [] []\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-sort-untyped-569",
                ["BEGIN{t=typeof(a[1]);a[2]=\"\";a[3]=0;n=asorti(a,b);print n;for(i=1;i<=n;i++)print typeof(a[i]),typeof(b[i]),\"[\"a[i]\"]\",\"[\"b[i]\"]\"}"], [])
            .WithStdOut("3\nuntyped string [] [1]\nstring string [] [2]\nnumber string [0] [3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-sort-untyped-570",
                ["BEGIN{t=typeof(a[1]);a[2]=\"\";a[3]=0;n=asort(a,b,\"@val_type_asc\");print n;for(i=1;i<=n;i++)print typeof(a[i]),typeof(b[i]),\"[\"a[i]\"]\",\"[\"b[i]\"]\"}"], [])
            .WithStdOut("3\nuntyped unassigned [] []\nstring number [] [0]\nnumber string [0] []\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-571",
                ["function f(p){return p[1]}BEGIN{a[1]=7;print f(a)}"], [])
            .WithStdOut("7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-572",
                ["function f(p){return p[1]}BEGIN{a[1]=7;print \"before\";print f((a))}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-573",
                ["function f(p){return p[1]}BEGIN{a[1]=7;print \"before\";print f(((a)))}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-574",
                ["function f(p){return p+1}BEGIN{x=7;print f(((x))),x}"], [])
            .WithStdOut("8 7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-575",
                ["function f(p){return p+0}BEGIN{print f((x));x[1]=7}"], [])
            .ExitsWith(2)
            .WithStdOut("0\n")
            .WithStdErr("awk: attempt to use scalar `x' as an array\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-576",
                ["function f(p){p[1]=7}BEGIN{f(x);print x[1]}"], [])
            .WithStdOut("7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-577",
                ["function f(p){return p+0}BEGIN{print f(((a[1]))),(1 in a)}"], [])
            .WithStdOut("0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-578",
                ["--posix", "function f(p){return p[1]}BEGIN{a[1]=7;print f(a)}"], [])
            .WithStdOut("7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-579",
                ["--posix", "function f(p){return p[1]}BEGIN{a[1]=7;print \"before\";print f((a))}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-580",
                ["--posix", "function f(p){return p[1]}BEGIN{a[1]=7;print \"before\";print f(((a)))}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-581",
                ["--posix", "function f(p){return p+1}BEGIN{x=7;print f(((x))),x}"], [])
            .WithStdOut("8 7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-582",
                ["--posix", "function f(p){return p+0}BEGIN{print f((x));x[1]=7}"], [])
            .ExitsWith(2)
            .WithStdOut("0\n")
            .WithStdErr("awk: attempt to use scalar `x' as an array\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-583",
                ["--posix", "function f(p){p[1]=7}BEGIN{f(x);print x[1]}"], [])
            .WithStdOut("7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-584",
                ["--posix", "function f(p){return p+0}BEGIN{print f(((a[1]))),(1 in a)}"], [])
            .WithStdOut("0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-585",
                ["--traditional", "function f(p){return p[1]}BEGIN{a[1]=7;print f(a)}"], [])
            .WithStdOut("7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-586",
                ["--traditional", "function f(p){return p[1]}BEGIN{a[1]=7;print \"before\";print f((a))}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-587",
                ["--traditional", "function f(p){return p[1]}BEGIN{a[1]=7;print \"before\";print f(((a)))}"], [])
            .ExitsWith(2)
            .WithStdOut("before\n")
            .WithStdErr("awk: attempt to use array `a' in a scalar context\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-588",
                ["--traditional", "function f(p){return p+1}BEGIN{x=7;print f(((x))),x}"], [])
            .WithStdOut("8 7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-589",
                ["--traditional", "function f(p){return p+0}BEGIN{print f((x));x[1]=7}"], [])
            .ExitsWith(2)
            .WithStdOut("0\n")
            .WithStdErr("awk: attempt to use scalar `x' as an array\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-590",
                ["--traditional", "function f(p){p[1]=7}BEGIN{f(x);print x[1]}"], [])
            .WithStdOut("7\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("untyped-state-grouped-profile-591",
                ["--traditional", "function f(p){return p+0}BEGIN{print f(((a[1]))),(1 in a)}"], [])
            .WithStdOut("0 1\n")
            .Build();

    }
}
