namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ArgumentStorageCases()
    {
        yield return Passing("argument-storage-large-calls-evaluate-in-caller-order",
            ["function total(a,b,c,d,e,t){t=a+b+c+d+e;return t}BEGIN{for(i=0;i<3;i++)print total(++x,++x,++x,++x,++x),x}"],
            [], "15 5\n40 10\n65 15\n");
        yield return Passing("argument-storage-large-calls-preserve-aliases-and-spelling",
            ["function use(x,a,b,y,z,missing){a[\"sum\"]+=x;b[\"seen\"]=y;return x \":\" z \":\" length(missing)}BEGIN{a[\"sum\"]=0;for(i=0;i<2;i++)print use(\"0007\",a,a,++n,\"0009\"),a[\"sum\"],a[\"seen\"],n}"],
            [], "0007:0009:0 7 1 1\n0007:0009:0 14 2 2\n");
        yield return Passing("argument-storage-nesting-spelling-and-output",
            ["function value(x){return int(x)+int(int(x))} {print int($1),atan2(0,1),value($1),sprintf(\"%s\",$1),length($1);printf \"%s:%d:%d\\n\",$1,int($1),value($1)}"],
            [], "7 0 14 0007 4\n0007:7:14\n9 0 18 0009 4\n0009:9:18\n", "0007\n0009\n");
        yield return Passing("argument-storage-changing-arity-preserves-effect-order",
            ["function take(){return ++n}BEGIN{for(i=0;i<2;i++){print int(take()),atan2(0,take()),length(\"spelling\"),int(int(take())+take()),n;printf \"%d/%d/%s\\n\",int(take()),int(take()),\"done\"}print n}"],
            [], "1 0 8 7 4\n5/6/done\n7 0 8 19 10\n11/12/done\n12\n");
    }
}
